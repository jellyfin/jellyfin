define(['datetime', 'jQuery'], function (datetime, $) {

    var currentItem;

    function deleteTimer(page, id) {

        require(['confirm'], function (confirm) {

            confirm(Globalize.translate('MessageConfirmRecordingCancellation'), Globalize.translate('HeaderConfirmRecordingCancellation')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).then(function () {

                    Dashboard.hideLoadingMsg();
                    require(['toast'], function (toast) {
                        toast(Globalize.translate('MessageRecordingCancelled'));
                    });

                    reload(page);
                });
            });
        });
    }

    function renderTimer(page, item) {

        currentItem = item;

        $('.itemName', page).html(item.Name);

        $('#txtPrePaddingMinutes', page).val(item.PrePaddingSeconds / 60);
        $('#txtPostPaddingMinutes', page).val(item.PostPaddingSeconds / 60);

        $('#chkNewOnly', page).checked(item.RecordNewOnly);
        $('#chkAllChannels', page).checked(item.RecordAnyChannel);
        $('#chkAnyTime', page).checked(item.RecordAnyTime);

        var channelHtml = '';
        if (item.RecordAnyChannel) {
            channelHtml += Globalize.translate('LabelAllChannels');
        }
        else if (item.ChannelId) {
            channelHtml += '<a href="itemdetails.html?id=' + item.ChannelId + '">' + item.ChannelName + '</a>';
        }

        $('.channel', page).html(channelHtml).trigger('create');

        selectDays(page, item.Days);

        if (item.RecordAnyTime) {
            $('.time', page).html(Globalize.translate('LabelAnytime')).trigger('create');
        }
        else if (item.ChannelId) {
            $('.time', page).html(datetime.getDisplayTime(item.StartDate)).trigger('create');
        }

        Dashboard.hideLoadingMsg();
    }

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

            $('#chk' + day, page).checked(days.indexOf(day) != -1);

        }

    }

    function getDays(page) {

        var daysOfWeek = getDaysOfWeek();

        var days = [];

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            if ($('#chk' + day, page).checked()) {
                days.push(day);
            }
        }

        return days;
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getLiveTvSeriesTimer(currentItem.Id).then(function (item) {

            item.PrePaddingSeconds = $('#txtPrePaddingMinutes', form).val() * 60;
            item.PostPaddingSeconds = $('#txtPostPaddingMinutes', form).val() * 60;

            item.RecordNewOnly = $('#chkNewOnly', form).checked();
            item.RecordAnyChannel = $('#chkAllChannels', form).checked();
            item.RecordAnyTime = $('#chkAnyTime', form).checked();

            item.Days = getDays(form);

            ApiClient.updateLiveTvSeriesTimer(item).then(function () {
                Dashboard.hideLoadingMsg();
                require(['toast'], function (toast) {
                    toast(Globalize.translate('MessageRecordingSaved'));
                });
            });
        });

        // Disable default form submission
        return false;

    }

    function renderRecordings(page, result) {

        $('.recordingsTab', page).html(LibraryBrowser.getPosterViewHtml({

            items: result.Items,
            shape: "detailPageSquare",
            showTitle: true,
            centerText: true,
            coverImage: true

        }));
    }

    function renderSchedule(page, result) {

        var timers = result.Items;

        LiveTvHelpers.getTimersHtml(timers).then(function(html) {
            var elem = $('.scheduleTab', page).html(html)[0];

            ImageLoader.lazyChildren(elem);

            $('.btnDeleteTimer', elem).on('click', function () {

                var id = this.getAttribute('data-timerid');

                deleteTimer(page, id);
            });
        });
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        ApiClient.getLiveTvSeriesTimer(id).then(function (result) {

            renderTimer(page, result);

        });

        ApiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            seriesTimerId: id

        }).then(function (recordingResult) {

            renderRecordings(page, recordingResult);

        });

        ApiClient.getLiveTvTimers({

            seriesTimerId: id

        }).then(function (timerResult) {

            renderSchedule(page, timerResult);

        });
    }

    $(document).on('pageinit', "#liveTvSeriesTimerPage", function () {

        var page = this;

        $('.radioSeriesTimerTab', page).on('change', function () {

            $('.tab', page).hide();
            $('.' + this.value + 'Tab', page).show();

        });

        $('.liveTvSeriesTimerForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pagebeforeshow', "#liveTvSeriesTimerPage", function () {

        var page = this;

        $('.radioProfileTab', page).checked(false).checkboxradio('refresh');
        $('#radioSettings', page).checked(true).checkboxradio('refresh').trigger('change');

        reload(page);

    }).on('pagebeforehide', "#liveTvSeriesTimerPage", function () {

        currentItem = null;
    });

});