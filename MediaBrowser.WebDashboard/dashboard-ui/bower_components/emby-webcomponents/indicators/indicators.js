define(['css!./indicators.css', 'material-icons'], function () {
    'use strict';

    function enableProgressIndicator(item) {

        if (item.MediaType === 'Video') {
            if (item.Type !== 'TvChannel') {
                return true;
            }
        }

        return false;
    }

    function getProgressHtml(pct, options) {

        var containerClass = 'itemProgressBar';

        if (options) {
            if (options.containerClass) {
                containerClass += ' ' + options.containerClass;
            }
        }

        return '<div class="' + containerClass + '"><div class="itemProgressBarForeground" style="width:' + pct + '%;"></div></div>';
    }

    function getProgressBarHtml(item, options) {

        if (enableProgressIndicator(item)) {
            if (item.Type === "Recording" && item.CompletionPercentage) {

                return getProgressHtml(item.CompletionPercentage, options);
            }

            var userData = options ? (options.userData || item.UserData) : item.UserData;
            if (userData) {
                var pct = userData.PlayedPercentage;

                if (pct && pct < 100) {

                    return getProgressHtml(pct, options);
                }
            }
        }

        return '';
    }

    function enablePlayedIndicator(item) {

        if (item.Type === "Series" || item.Type === "Season" || item.Type === "BoxSet" || item.MediaType === "Video" || item.MediaType === "Game" || item.MediaType === "Book") {

            if (item.Type !== 'TvChannel') {
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

        var status;

        if (item.Type === 'SeriesTimer') {
            return '<i class="md-icon timerIndicator indicatorIcon">&#xE062;</i>';
        }
        else if (item.TimerId || item.SeriesTimerId) {

            status = item.Status || 'Cancelled';
        }
        else if (item.Type === 'Timer') {

            status = item.Status;
        }
        else {
            return '';
        }

        if (item.SeriesTimerId) {

            if (status !== 'Cancelled') {
                return '<i class="md-icon timerIndicator indicatorIcon">&#xE062;</i>';
            }

            return '<i class="md-icon timerIndicator timerIndicator-inactive indicatorIcon">&#xE062;</i>';
        }

        return '<i class="md-icon timerIndicator indicatorIcon">&#xE061;</i>';
    }

    function getSyncIndicator(item) {

        if (item.SyncPercent === 100) {
            return '<div class="syncIndicator indicator fullSyncIndicator"><i class="md-icon indicatorIcon">file_download</i></div>';
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