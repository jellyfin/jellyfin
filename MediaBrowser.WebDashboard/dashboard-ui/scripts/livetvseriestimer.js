define(['datetime', 'dom', 'emby-itemscontainer'], function (datetime, dom) {

    function getDaysOfWeek() {

        // Do not localize. These are used as values, not text.
        return LiveTvHelpers.getDaysOfWeek().map(function (d) {
            return d.value;
        });

    }

    function selectDays(page, days) {

        var daysOfWeek = getDaysOfWeek();

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            page.querySelector('#chk' + day).checked = days.indexOf(day) != -1;

        }

    }

    function getDays(page) {

        var daysOfWeek = getDaysOfWeek();

        var days = [];

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            if (page.querySelector('#chk' + day).checked) {
                days.push(day);
            }
        }

        return days;
    }

    return function (view, params) {

        function renderTimer(page, item) {

            page.querySelector('.itemName').innerHTML = item.Name;

            page.querySelector('#txtPrePaddingMinutes').value = item.PrePaddingSeconds / 60;
            page.querySelector('#txtPostPaddingMinutes').value = item.PostPaddingSeconds / 60;

            page.querySelector('#chkNewOnly').checked = item.RecordNewOnly;
            page.querySelector('#chkAllChannels').checked = item.RecordAnyChannel;
            page.querySelector('#chkAnyTime').checked = item.RecordAnyTime;

            selectDays(page, item.Days);

            Dashboard.hideLoadingMsg();
        }

        function onSubmit(e) {

            var form = this;

            var id = params.id;

            ApiClient.getLiveTvSeriesTimer(id).then(function (item) {

                item.PrePaddingSeconds = form.querySelector('#txtPrePaddingMinutes').value * 60;
                item.PostPaddingSeconds = form.querySelector('#txtPostPaddingMinutes').value * 60;

                item.RecordNewOnly = form.querySelector('#chkNewOnly').checked;
                item.RecordAnyChannel = form.querySelector('#chkAllChannels').checked;
                item.RecordAnyTime = form.querySelector('#chkAnyTime').checked;

                item.Days = getDays(form);

                ApiClient.updateLiveTvSeriesTimer(item);
            });

            e.preventDefault();
            // Disable default form submission
            return false;
        }

        function renderSchedule(page, result) {

            var timers = result.Items;

            LiveTvHelpers.getTimersHtml(timers).then(function (html) {

                var scheduleTab = page.querySelector('.scheduleTab');
                scheduleTab.innerHTML = html;

                ImageLoader.lazyChildren(scheduleTab);
            });
        }

        function reload() {

            var id = params.id;
            Dashboard.showLoadingMsg();

            ApiClient.getLiveTvSeriesTimer(id).then(function (result) {

                renderTimer(view, result);

            });

            ApiClient.getLiveTvTimers({

                seriesTimerId: id

            }).then(function (timerResult) {

                renderSchedule(view, timerResult);

            });
        }

        view.querySelector('form').addEventListener('change', function () {
            view.querySelector('.btnSubmit').click();
        });

        view.querySelector('.liveTvSeriesTimerForm').addEventListener('submit', onSubmit);
        view.querySelector('.scheduleTab').addEventListener('timercancelled', reload);
        view.addEventListener('viewbeforeshow', reload);
    };
});