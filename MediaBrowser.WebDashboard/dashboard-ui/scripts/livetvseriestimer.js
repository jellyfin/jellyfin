(function (window, $, document) {

    var currentItem;

    function deleteTimer(page, id) {

        Dashboard.confirm(Globalize.translate('MessageConfirmRecordingCancellation'), Globalize.translate('HeaderConfirmRecordingCancellation'), function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).done(function () {

                    Dashboard.alert(Globalize.translate('MessageRecordingCancelled'));

                    reload(page);
                });
            }

        });
    }

    function renderTimer(page, item) {

        currentItem = item;

        $('.itemName', page).html(item.Name);

        $('#txtPrePaddingMinutes', page).val(item.PrePaddingSeconds / 60);
        $('#txtPostPaddingMinutes', page).val(item.PostPaddingSeconds / 60);
        $('#chkPrePaddingRequired', page).checked(item.IsPrePaddingRequired).checkboxradio('refresh');
        $('#chkPostPaddingRequired', page).checked(item.IsPostPaddingRequired).checkboxradio('refresh');

        $('#chkNewOnly', page).checked(item.RecordNewOnly).checkboxradio('refresh');
        $('#chkAllChannels', page).checked(item.RecordAnyChannel).checkboxradio('refresh');
        $('#chkAnyTime', page).checked(item.RecordAnyTime).checkboxradio('refresh');

        var channelHtml = '';
        if (item.RecordAnyChannel) {
            channelHtml += Globalize.translate('LabelAllChannels');
        }
        else if (item.ChannelId) {
            channelHtml += '<a href="livetvchannel.html?id=' + item.ChannelId + '">' + item.ChannelName + '</a>';
        }

        $('.channel', page).html(channelHtml).trigger('create');

        selectDays(page, item.Days);

        if (item.RecordAnyTime) {
            $('.time', page).html(Globalize.translate('LabelAnytime')).trigger('create');
        }
        else if (item.ChannelId) {
            $('.time', page).html(LiveTvHelpers.getDisplayTime(item.StartDate)).trigger('create');
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

            $('#chk' + day, page).checked(days.indexOf(day) != -1).checkboxradio('refresh');

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

        ApiClient.getLiveTvSeriesTimer(currentItem.Id).done(function (item) {

            item.PrePaddingSeconds = $('#txtPrePaddingMinutes', form).val() * 60;
            item.PostPaddingSeconds = $('#txtPostPaddingMinutes', form).val() * 60;
            item.IsPrePaddingRequired = $('#chkPrePaddingRequired', form).checked();
            item.IsPostPaddingRequired = $('#chkPostPaddingRequired', form).checked();

            item.RecordNewOnly = $('#chkNewOnly', form).checked();
            item.RecordAnyChannel = $('#chkAllChannels', form).checked();
            item.RecordAnyTime = $('#chkAnyTime', form).checked();

            item.Days = getDays(form);

            ApiClient.updateLiveTvSeriesTimer(item).done(function () {
                Dashboard.alert(Globalize.translate('MessageRecordingSaved'));
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
            overlayText: true,
            coverImage: true

        })).createCardMenus();
    }

    function renderSchedule(page, result) {

        var timers = result.Items;

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-split-icon="delete">';

        var index = '';

        for (var i = 0, length = timers.length; i < length; i++) {

            var timer = timers[i];

            var startDateText = LibraryBrowser.getFutureDateText(parseISO8601Date(timer.StartDate, { toLocal: true }));

            if (startDateText != index) {
                html += '<li data-role="list-divider">' + startDateText + '</li>';
                index = startDateText;
            }

            html += '<li><a href="livetvtimer.html?id=' + timer.Id + '">';

            var program = timer.ProgramInfo || {};
            var imgUrl;

            var programImages = program.ImageTags || {};
            if (programImages.Primary) {

                imgUrl = ApiClient.getScaledImageUrl(program.Id, {
                    height: 80,
                    tag: programImages.Primary,
                    type: "Primary"
                });
            } else {
                imgUrl = "css/images/items/searchhintsv2/tv.png";
            }

            html += '<img src="css/images/items/searchhintsv2/tv.png" style="display:none;">';
            html += '<div class="ui-li-thumb" style="background-image:url(\'' + imgUrl + '\');width:5em;height:5em;background-repeat:no-repeat;background-position:center center;background-size: cover;"></div>';

            html += '<h3>';
            html += program.EpisodeTitle || timer.Name;
            html += '</h3>';

            html += '<p>';

            if (program.IsLive) {
                html += '<span class="liveTvProgram">' + Globalize.translate('LabelLiveProgram') + '&nbsp;&nbsp;</span>';
            }
            else if (program.IsPremiere) {
                html += '<span class="premiereTvProgram">' + Globalize.translate('LabelPremiereProgram') + '&nbsp;&nbsp;</span>';
            }
            else if (program.IsSeries && !program.IsRepeat) {
                html += '<span class="newTvProgram">' + Globalize.translate('LabelNewProgram') + '&nbsp;&nbsp;</span>';
            }

            html += LiveTvHelpers.getDisplayTime(timer.StartDate);
            html += ' - ' + LiveTvHelpers.getDisplayTime(timer.EndDate);
            html += '</p>';


            if (timer.SeriesTimerId) {
                html += '<div class="ui-li-aside" style="right:0;">';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '</div>';
            }

            html += '</a>';

            html += '<a data-timerid="' + timer.Id + '" href="#" title="' + Globalize.translate('ButonCancelRecording') + '" class="btnCancelTimer">' + Globalize.translate('ButonCancelRecording') + '</a>';

            html += '</li>';
        }

        html += '</ul>';

        var elem = $('.scheduleTab', page).html(html).trigger('create');

        $('.btnCancelTimer', elem).on('click', function () {

            deleteTimer(page, this.getAttribute('data-timerid'));

        });
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        ApiClient.getLiveTvSeriesTimer(id).done(function (result) {

            renderTimer(page, result);

        });

        ApiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            seriesTimerId: id

        }).done(function (recordingResult) {

            renderRecordings(page, recordingResult);

        });

        ApiClient.getLiveTvTimers({

            seriesTimerId: id

        }).done(function (timerResult) {

            renderSchedule(page, timerResult);

        });
    }

    $(document).on('pageinit', "#liveTvSeriesTimerPage", function () {

        var page = this;

        $('.radioSeriesTimerTab', page).on('change', function () {

            $('.tab', page).hide();
            $('.' + this.value + 'Tab', page).show();

        });

    }).on('pagebeforeshow', "#liveTvSeriesTimerPage", function () {

        var page = this;

        $('.radioProfileTab', page).checked(false).checkboxradio('refresh');
        $('#radioSettings', page).checked(true).checkboxradio('refresh').trigger('change');

        reload(page);

    }).on('pagehide', "#liveTvSeriesTimerPage", function () {

        currentItem = null;
    });

    function liveTvSeriesTimerPage() {

        var self = this;

        self.onSubmit = onSubmit;
    }

    window.LiveTvSeriesTimerPage = new liveTvSeriesTimerPage();

})(window, jQuery, document);