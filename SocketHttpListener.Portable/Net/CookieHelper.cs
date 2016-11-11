using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketHttpListener.Net
{
    public static class CookieHelper
    {
        internal static CookieCollection Parse(string value, bool response)
        {
            return response
                ? parseResponse(value)
                : null;
        }

        private static string[] splitCookieHeaderValue(string value)
        {
            return new List<string>(value.SplitHeaderValue(',', ';')).ToArray();
        }

        private static CookieCollection parseResponse(string value)
        {
            var cookies = new CookieCollection();

            Cookie cookie = null;
            var pairs = splitCookieHeaderValue(value);
            for (int i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i].Trim();
                if (pair.Length == 0)
                    continue;

                if (pair.StartsWith("version", StringComparison.OrdinalIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Version = Int32.Parse(pair.GetValueInternal("=").Trim('"'));
                }
                else if (pair.StartsWith("expires", StringComparison.OrdinalIgnoreCase))
                {
                    var buffer = new StringBuilder(pair.GetValueInternal("="), 32);
                    if (i < pairs.Length - 1)
                        buffer.AppendFormat(", {0}", pairs[++i].Trim());

                    DateTime expires;
                    if (!DateTime.TryParseExact(
                      buffer.ToString(),
                      new[] { "ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'GMT'", "r" },
                      new CultureInfo("en-US"),
                      DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                      out expires))
                        expires = DateTime.Now;

                    if (cookie != null && cookie.Expires == DateTime.MinValue)
                        cookie.Expires = expires.ToLocalTime();
                }
                else if (pair.StartsWith("max-age", StringComparison.OrdinalIgnoreCase))
                {
                    var max = Int32.Parse(pair.GetValueInternal("=").Trim('"'));
                    var expires = DateTime.Now.AddSeconds((double)max);
                    if (cookie != null)
                        cookie.Expires = expires;
                }
                else if (pair.StartsWith("path", StringComparison.OrdinalIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Path = pair.GetValueInternal("=");
                }
                else if (pair.StartsWith("domain", StringComparison.OrdinalIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Domain = pair.GetValueInternal("=");
                }
                else if (pair.StartsWith("port", StringComparison.OrdinalIgnoreCase))
                {
                    var port = pair.Equals("port", StringComparison.OrdinalIgnoreCase)
                               ? "\"\""
                               : pair.GetValueInternal("=");

                    if (cookie != null)
                        cookie.Port = port;
                }
                else if (pair.StartsWith("comment", StringComparison.OrdinalIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Comment = pair.GetValueInternal("=").UrlDecode();
                }
                else if (pair.StartsWith("commenturl", StringComparison.OrdinalIgnoreCase))
                {
                    if (cookie != null)
                        cookie.CommentUri = pair.GetValueInternal("=").Trim('"').ToUri();
                }
                else if (pair.StartsWith("discard", StringComparison.OrdinalIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Discard = true;
                }
                else if (pair.StartsWith("secure", StringComparison.OrdinalIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Secure = true;
                }
                else if (pair.StartsWith("httponly", StringComparison.OrdinalIgnoreCase))
                {
                    if (cookie != null)
                        cookie.HttpOnly = true;
                }
                else
                {
                    if (cookie != null)
                        cookies.Add(cookie);

                    string name;
                    string val = String.Empty;

                    var pos = pair.IndexOf('=');
                    if (pos == -1)
                    {
                        name = pair;
                    }
                    else if (pos == pair.Length - 1)
                    {
                        name = pair.Substring(0, pos).TrimEnd(' ');
                    }
                    else
                    {
                        name = pair.Substring(0, pos).TrimEnd(' ');
                        val = pair.Substring(pos + 1).TrimStart(' ');
                    }

                    cookie = new Cookie(name, val);
                }
            }

            if (cookie != null)
                cookies.Add(cookie);

            return cookies;
        }
    }
}
