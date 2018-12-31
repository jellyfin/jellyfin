define(['datetime', 'itemHelper', 'css!./indicators.css', 'material-icons'], function (datetime, itemHelper) {
    'use strict';

    function enableProgressIndicator(item) {

        if (item.MediaType === 'Video') {
            if (item.Type !== 'TvChannel') {
                return true;
            }
        }

        if (item.Type === 'AudioBook' || item.Type === 'AudioPodcast') {
            return true;
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

    function getAutoTimeProgressHtml(pct, options, isRecording, start, end) {

        var containerClass = 'itemProgressBar';

        if (options) {
            if (options.containerClass) {
                containerClass += ' ' + options.containerClass;
            }
        }

        var foregroundClass = 'itemProgressBarForeground';

        if (isRecording) {
            foregroundClass += ' itemProgressBarForeground-recording';
        }

        return '<div is="emby-progressbar" data-automode="time" data-starttime="' + start + '" data-endtime="' + end + '" class="' + containerClass + '"><div class="' + foregroundClass + '" style="width:' + pct + '%;"></div></div>';
    }

    function getProgressBarHtml(item, options) {

        var pct;

        if (enableProgressIndicator(item) && item.Type !== "Recording") {

            var userData = options ? (options.userData || item.UserData) : item.UserData;
            if (userData) {
                pct = userData.PlayedPercentage;

                if (pct && pct < 100) {

                    return getProgressHtml(pct, options);
                }
            }
        }

        if ((item.Type === 'Program' || item.Type === 'Timer' || item.Type === 'Recording') && item.StartDate && item.EndDate) {

            var startDate = 0;
            var endDate = 1;

            try {

                startDate = datetime.parseISO8601Date(item.StartDate).getTime();

            } catch (err) {
            }

            try {

                endDate = datetime.parseISO8601Date(item.EndDate).getTime();

            } catch (err) {
            }

            var now = new Date().getTime();
            var total = endDate - startDate;
            pct = 100 * ((now - startDate) / total);

            if (pct > 0 && pct < 100) {

                var isRecording = item.Type === 'Timer' || item.Type === 'Recording' || item.TimerId;

                return getAutoTimeProgressHtml(pct, options, isRecording, startDate, endDate);
            }
        }

        return '';
    }

    function enablePlayedIndicator(item) {

        return itemHelper.canMarkPlayed(item);
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
            return '<div class="syncIndicator indicator fullSyncIndicator"><i class="md-icon indicatorIcon">&#xE2C4;</i></div>';
        } else if (item.SyncPercent != null) {
            return '<div class="syncIndicator indicator emptySyncIndicator"><i class="md-icon indicatorIcon">&#xE2C4;</i></div>';
        }

        return '';
    }

    function getTypeIndicator(item) {

        if (item.Type === 'Video') {

            return '<div class="indicator videoIndicator"><i class="md-icon indicatorIcon">&#xE04B;</i></div>';
        }
        if (item.Type === 'Folder' || item.Type === 'PhotoAlbum') {

            return '<div class="indicator videoIndicator"><i class="md-icon indicatorIcon">&#xE2C7;</i></div>';
        }
        if (item.Type === 'Photo') {

            return '<div class="indicator videoIndicator"><i class="md-icon indicatorIcon">&#xE410;</i></div>';
            //return '<div class="indicator videoIndicator"><i class="md-icon indicatorIcon">&#xE412;</i></div>';
        }

        return '';
    }

    function getMissingIndicator(item) {

        if (item.Type === 'Episode' && item.LocationType === 'Virtual') {

            if (item.PremiereDate) {
                try {

                    var premiereDate = datetime.parseISO8601Date(item.PremiereDate).getTime();

                    if (premiereDate > new Date().getTime()) {
                        return '<div class="unairedIndicator">Unaired</div>';
                    }

                } catch (err) {
                }
            }

            return '<div class="missingIndicator">Missing</div>';
        }

        return '';
    }

    var ProgressBarPrototype = Object.create(HTMLDivElement.prototype);

    function onAutoTimeProgress() {

        var start = parseInt(this.getAttribute('data-starttime'));
        var end = parseInt(this.getAttribute('data-endtime'));

        var now = new Date().getTime();
        var total = end - start;
        var pct = 100 * ((now - start) / total);

        pct = Math.min(100, pct);
        pct = Math.max(0, pct);

        var itemProgressBarForeground = this.querySelector('.itemProgressBarForeground');
        itemProgressBarForeground.style.width = pct + '%';
    }

    ProgressBarPrototype.attachedCallback = function () {

        if (this.timeInterval) {
            clearInterval(this.timeInterval);
        }

        if (this.getAttribute('data-automode') === 'time') {
            this.timeInterval = setInterval(onAutoTimeProgress.bind(this), 60000);
        }
    };

    ProgressBarPrototype.detachedCallback = function () {

        if (this.timeInterval) {
            clearInterval(this.timeInterval);
            this.timeInterval = null;
        }
    };

    document.registerElement('emby-progressbar', {
        prototype: ProgressBarPrototype,
        extends: 'div'
    });

    return {
        getProgressBarHtml: getProgressBarHtml,
        getPlayedIndicatorHtml: getPlayedIndicator,
        getChildCountIndicatorHtml: getChildCountIndicatorHtml,
        enableProgressIndicator: enableProgressIndicator,
        getTimerIndicator: getTimerIndicator,
        enablePlayedIndicator: enablePlayedIndicator,
        getSyncIndicator: getSyncIndicator,
        getTypeIndicator: getTypeIndicator,
        getMissingIndicator: getMissingIndicator
    };
});