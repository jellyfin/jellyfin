use std::ffi::{c_char, CStr, CString};
use std::panic::{catch_unwind, AssertUnwindSafe};

use chrono::{Datelike, NaiveDate, NaiveDateTime};
use fancy_regex::Regex;
use serde::{Deserialize, Serialize};

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct ParseRequest {
    path: String,
    is_directory: bool,
    is_named: Option<bool>,
    is_optimistic: Option<bool>,
    supports_absolute_numbers: Option<bool>,
    fill_extended_info: bool,
    episode_expressions: Vec<EpisodeExpression>,
    multiple_episode_expressions: Vec<EpisodeExpression>,
}

#[derive(Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
struct EpisodeExpression {
    expression: String,
    is_by_date: bool,
    is_optimistic: bool,
    is_named: bool,
    supports_absolute_episode_numbers: bool,
    date_time_formats: Vec<String>,
}

#[derive(Default, Serialize)]
#[serde(rename_all = "PascalCase")]
struct EpisodePathParserResult {
    #[serde(skip_serializing_if = "Option::is_none")]
    season_number: Option<i32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    episode_number: Option<i32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    ending_episode_number: Option<i32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    series_name: Option<String>,
    success: bool,
    is_by_date: bool,
    #[serde(skip_serializing_if = "Option::is_none")]
    year: Option<i32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    month: Option<i32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    day: Option<i32>,
}

#[no_mangle]
pub extern "C" fn jellyfin_episode_path_parse_json(input: *const c_char) -> *mut c_char {
    let result = catch_unwind(AssertUnwindSafe(|| parse_json(input)));

    match result {
        Ok(json) => into_raw_c_string(json),
        Err(_) => into_raw_c_string(
            r#"{"Success":false,"IsByDate":false,"Error":"panic in Rust episode path parser"}"#
                .to_string(),
        ),
    }
}

#[no_mangle]
pub extern "C" fn jellyfin_free_string(value: *mut c_char) {
    if !value.is_null() {
        unsafe {
            drop(CString::from_raw(value));
        }
    }
}

fn parse_json(input: *const c_char) -> String {
    if input.is_null() {
        return serialize_error("null input");
    }

    let request = unsafe { CStr::from_ptr(input) };
    let Ok(request) = request.to_str() else {
        return serialize_error("input was not valid UTF-8");
    };

    match serde_json::from_str::<ParseRequest>(request) {
        Ok(request) => match serde_json::to_string(&parse_request(&request)) {
            Ok(result) => result,
            Err(err) => serialize_error(&format!("failed to serialize result: {err}")),
        },
        Err(err) => serialize_error(&format!("failed to deserialize request: {err}")),
    }
}

fn parse_request(request: &ParseRequest) -> EpisodePathParserResult {
    let mut path = request.path.clone();

    if request.is_directory {
        path.push_str(".mp4");
    }

    let mut result = request
        .episode_expressions
        .iter()
        .filter(|expression| {
            request.supports_absolute_numbers.map_or(true, |value| {
                expression.supports_absolute_episode_numbers == value
            }) && request
                .is_named
                .map_or(true, |value| expression.is_named == value)
                && request
                    .is_optimistic
                    .map_or(true, |value| expression.is_optimistic == value)
        })
        .find_map(|expression| {
            let result = parse_with_expression(&path, expression);
            result.success.then_some(result)
        });

    if let Some(ref mut result) = result {
        if request.fill_extended_info {
            fill_additional(&path, result, request);

            if let Some(series_name) = &result.series_name {
                result.series_name = Some(trim_series_name(series_name));
            }
        }
    }

    result.unwrap_or_default()
}

fn parse_with_expression(name: &str, expression: &EpisodeExpression) -> EpisodePathParserResult {
    let mut result = EpisodePathParserResult::default();
    let match_name = if expression.is_by_date {
        name.replace('_', "-")
    } else {
        name.to_string()
    };

    let Ok(regex) = compile_regex(&expression.expression) else {
        return result;
    };

    let Ok(Some(captures)) = regex.captures(&match_name) else {
        return result;
    };

    if captures.len() < 3 {
        return result;
    }

    if expression.is_by_date {
        if let Some(value) = capture_value(&captures, 0) {
            if let Some(date) = parse_date(&value, &expression.date_time_formats) {
                result.year = Some(date.year());
                result.month = Some(date.month() as i32);
                result.day = Some(date.day() as i32);
            }
        }

        result.success = true;
    } else if expression.is_named {
        result.season_number = parse_named_i32(&captures, "seasonnumber");
        result.episode_number = parse_named_i32(&captures, "epnumber");

        if let Some(ending_match) = captures.name("endingepnumber") {
            let next_index = ending_match.end();
            if next_index >= match_name.len()
                || !matches!(
                    match_name.as_bytes()[next_index],
                    b'0'..=b'9' | b'i' | b'I' | b'p' | b'P'
                )
            {
                result.ending_episode_number = ending_match.as_str().parse::<i32>().ok();
            }
        }

        result.series_name = Some(
            captures
                .name("seriesname")
                .map_or(String::new(), |value| value.as_str().to_string()),
        );
        result.success = result.episode_number.is_some();
    } else {
        result.season_number = capture_value(&captures, 1).and_then(|value| value.parse().ok());
        result.episode_number = capture_value(&captures, 2).and_then(|value| value.parse().ok());
        result.success = result.episode_number.is_some();
    }

    if matches!(result.season_number, Some(200..=1927) | Some(2501..)) {
        result.success = false;
    }

    result.is_by_date = expression.is_by_date;
    result
}

fn fill_additional(path: &str, info: &mut EpisodePathParserResult, request: &ParseRequest) {
    let mut expressions: Vec<EpisodeExpression> = request
        .multiple_episode_expressions
        .iter()
        .filter(|expression| expression.is_named)
        .cloned()
        .collect();

    if info
        .series_name
        .as_ref()
        .map_or(true, |series_name| series_name.is_empty())
    {
        let mut named_episode_expressions: Vec<EpisodeExpression> = request
            .episode_expressions
            .iter()
            .filter(|expression| expression.is_named)
            .cloned()
            .collect();
        named_episode_expressions.extend(expressions);
        expressions = named_episode_expressions;
    }

    for expression in expressions {
        let result = parse_with_expression(path, &expression);

        if !result.success {
            continue;
        }

        if info
            .series_name
            .as_ref()
            .map_or(true, |series_name| series_name.is_empty())
        {
            info.series_name = result.series_name;
        }

        if info.ending_episode_number.is_none() && info.episode_number.is_some() {
            info.ending_episode_number = result.ending_episode_number;
        }

        if info
            .series_name
            .as_ref()
            .map_or(false, |series_name| !series_name.is_empty())
            && (info.episode_number.is_none() || info.ending_episode_number.is_some())
        {
            break;
        }
    }
}

fn capture_value(captures: &fancy_regex::Captures<'_>, index: usize) -> Option<String> {
    captures.get(index).map(|value| value.as_str().to_string())
}

fn compile_regex(expression: &str) -> Result<Regex, fancy_regex::Error> {
    Regex::new(&format!("(?i){}", normalize_dotnet_regex(expression)))
}

fn normalize_dotnet_regex(expression: &str) -> String {
    let expression = expression
        .replace("[][", r"[\]\[")
        .replace(r"[^[\]]", r"[^\[\]]");
    let mut normalized = String::with_capacity(expression.len());
    let mut chars = expression.chars().peekable();

    while let Some(current) = chars.next() {
        if current == '(' && chars.peek() == Some(&'?') {
            chars.next();
            if chars.peek() == Some(&'<') {
                chars.next();
                if matches!(chars.peek(), Some('=') | Some('!')) {
                    normalized.push_str("(?<");
                    continue;
                }

                normalized.push_str("(?P<");
                continue;
            }

            normalized.push_str("(?");
            continue;
        }

        normalized.push(current);
    }

    normalized
}

fn parse_named_i32(captures: &fancy_regex::Captures<'_>, name: &str) -> Option<i32> {
    captures
        .name(name)
        .and_then(|value| value.as_str().parse::<i32>().ok())
}

fn parse_date(value: &str, formats: &[String]) -> Option<NaiveDate> {
    if formats.is_empty() {
        return NaiveDateTime::parse_from_str(value, "%Y-%m-%d %H:%M:%S")
            .map(|value| value.date())
            .ok()
            .or_else(|| NaiveDate::parse_from_str(value, "%Y-%m-%d").ok());
    }

    formats
        .iter()
        .find_map(|format| NaiveDate::parse_from_str(value, dotnet_date_format(format)).ok())
}

fn dotnet_date_format(format: &str) -> &str {
    match format {
        "yyyy.MM.dd" => "%Y.%m.%d",
        "yyyy-MM-dd" => "%Y-%m-%d",
        "yyyy_MM_dd" => "%Y_%m_%d",
        "yyyy MM dd" => "%Y %m %d",
        "dd.MM.yyyy" => "%d.%m.%Y",
        "dd-MM-yyyy" => "%d-%m-%Y",
        "dd_MM_yyyy" => "%d_%m_%Y",
        "dd MM yyyy" => "%d %m %Y",
        _ => format,
    }
}

fn trim_series_name(series_name: &str) -> String {
    series_name
        .trim()
        .trim_matches(|value| matches!(value, '_' | '.' | '-'))
        .trim()
        .to_string()
}

fn serialize_error(message: &str) -> String {
    serde_json::json!({
        "Success": false,
        "IsByDate": false,
        "Error": message
    })
    .to_string()
}

fn into_raw_c_string(value: String) -> *mut c_char {
    CString::new(value)
        .unwrap_or_else(|_| CString::new(r#"{"Success":false,"IsByDate":false}"#).unwrap())
        .into_raw()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn parses_kodi_season_episode_expression() {
        let expression = EpisodeExpression {
            expression: r#".*(\\|\/)(?<seriesname>((?![Ss]([0-9]+)[][ ._-]*[Ee]([0-9]+))[^\\\/])*)?[Ss](?<seasonnumber>[0-9]+)[][ ._-]*[Ee](?<epnumber>[0-9]+)([^\\/]*)$"#.to_string(),
            is_by_date: false,
            is_optimistic: false,
            is_named: true,
            supports_absolute_episode_numbers: true,
            date_time_formats: Vec::new(),
        };

        let regex = compile_regex(&expression.expression).unwrap();
        let captures = regex
            .captures("/media/Foo/Foo-S01E01.mp4")
            .unwrap()
            .unwrap();
        assert_eq!(
            Some("01"),
            captures.name("seasonnumber").map(|value| value.as_str())
        );

        let result = parse_with_expression("/media/Foo/Foo-S01E01.mp4", &expression);

        assert!(result.success);
        assert_eq!(Some(1), result.season_number);
        assert_eq!(Some(1), result.episode_number);
    }

    #[test]
    fn parses_anime_bracket_expression() {
        let expression = EpisodeExpression {
            expression: r#"(?:\[(?:[^\]]+)\]\s*)?(?<seriesname>\[[^\]]+\]|[^[\]]+)\s*\[(?<epnumber>[0-9]+)\]"#.to_string(),
            is_by_date: false,
            is_optimistic: false,
            is_named: true,
            supports_absolute_episode_numbers: true,
            date_time_formats: Vec::new(),
        };

        let result = parse_with_expression(
            "[VCB-Studio] Re Zero kara Hajimeru Isekai Seikatsu [21][Ma10p_1080p][x265_flac].mkv",
            &expression,
        );

        assert!(result.success);
        assert_eq!(Some(21), result.episode_number);
    }
}
