define(['css!./indicators.css', 'html!./../icons/mediainfo.html', 'html!./../icons/nav.html'], function () {

    function enableProgressIndicator(item) {

        if (item.MediaType == 'Video') {
            if (item.Type != 'TvChannel') {
                return true;
            }
        }

        return false;
    }

    function getProgressHtml(pct) {

        return '<div class="itemProgressBar"><div class="itemProgressBarForeground" style="width:' + pct + '%;"></div></div>';
    }

    function getProgressBarHtml(item) {

        if (enableProgressIndicator(item)) {
            if (item.Type == "Recording" && item.CompletionPercentage) {

                return getProgressHtml(item.CompletionPercentage);
            }

            var userData = item.UserData;
            if (userData) {
                var pct = userData.PlayedPercentage;

                if (pct && pct < 100) {

                    return getProgressHtml(pct);
                }
            }
        }

        return '';
    }

    function enablePlayedIndicator(item) {

        if (item.Type == "Series" || item.Type == "Season" || item.Type == "BoxSet" || item.MediaType == "Video" || item.MediaType == "Game" || item.MediaType == "Book") {

            if (item.Type != 'TvChannel') {
                return true;
            }
        }

        return false;
    }

    function getPlayedIndicator(item) {

        if (enablePlayedIndicator(item)) {

            var userData = item.UserData || {};

            if (userData.UnplayedItemCount) {
                return '<div class="countIndicator indicator">' + userData.UnplayedItemCount + '</div>';
            }

            if (userData.PlayedPercentage && userData.PlayedPercentage >= 100 || (userData.Played)) {
                return '<div class="playedIndicator indicator"><iron-icon icon="nav:check"></iron-icon></div>';
            }
        }

        return '';
    }

    function getCountIndicatorHtml(count) {

        return '<div class="countIndicator indicator">' + count + '</div>';
    }

    function getChildCountIndicatorHtml(item, options) {

        var minCount = 0;

        if (options) {
            minCount = options.minCount || minCount;
        }

        if (item.ChildCount && item.ChildCount > minCount) {
            return getCountIndicatorHtml(item.ChildCount);
        }

        return '';
    }

    function getTimerIndicator(item) {
        
        if (item.SeriesTimerId) {
            return '<iron-icon class="timerIndicator indicator" icon="mediainfo:fiber-smart-record"></iron-icon>';
        }
        if (item.TimerId) {
            return '<iron-icon class="timerIndicator indicator" icon="mediainfo:fiber-manual-record"></iron-icon>';
        }

        return '';
    }

    return {
        getProgressBarHtml: getProgressBarHtml,
        getPlayedIndicatorHtml: getPlayedIndicator,
        getChildCountIndicatorHtml: getChildCountIndicatorHtml,
        enableProgressIndicator: enableProgressIndicator,
        getTimerIndicator: getTimerIndicator,
        enablePlayedIndicator: enablePlayedIndicator
    };
});