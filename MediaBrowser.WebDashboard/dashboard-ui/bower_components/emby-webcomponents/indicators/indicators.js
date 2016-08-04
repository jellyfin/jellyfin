define(['css!./indicators.css', 'material-icons'], function () {

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
                return '<div class="playedIndicator indicator"><i class="md-icon indicatorIcon">&#xE5CA;</i></div>';
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
            return '<i class="md-icon timerIndicator indicator">fiber_smart_record</i>';
        }
        if (item.TimerId) {
            return '<i class="md-icon timerIndicator indicator">fiber_manual_record</i>';
        }

        return '';
    }

    function getSyncIndicator(item) {

        if (item.SyncPercent == 100) {
            return '<div class="syncIndicator indicator fullSyncIndicator"><i class="md-icon indicatorIcon fullSyncIndicatorIcon">offline_pin</i></div>';
        } else if (item.SyncPercent != null) {
            return '<div class="syncIndicator indicator emptySyncIndicator"><i class="md-icon indicatorIcon">file_download</i></div>';
        }

        return '';
    }

    return {
        getProgressBarHtml: getProgressBarHtml,
        getPlayedIndicatorHtml: getPlayedIndicator,
        getChildCountIndicatorHtml: getChildCountIndicatorHtml,
        enableProgressIndicator: enableProgressIndicator,
        getTimerIndicator: getTimerIndicator,
        enablePlayedIndicator: enablePlayedIndicator,
        getSyncIndicator: getSyncIndicator
    };
});