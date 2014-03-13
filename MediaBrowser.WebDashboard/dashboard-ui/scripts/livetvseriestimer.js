(function (window, $, document, apiClient) {

    var currentItem;

    function deleteTimer(page, id) {

        Dashboard.confirm("Are you sure you wish to cancel this recording?", "Confirm Recording Cancellation", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).done(function () {

                    Dashboard.alert('Recording cancelled.');

                    reload(page);
                });
            }

        });
    }

    function renderTimer(page, item) {

        currentItem = item;

        $('.itemName', page).html(item.Name);

        $('#txtPrePaddingSeconds', page).val(item.PrePaddingSeconds / 60);
        $('#txtPostPaddingSeconds', page).val(item.PostPaddingSeconds / 60);
        $('#chkPrePaddingRequired', page).checked(item.IsPrePaddingRequired).checkboxradio('refresh');
        $('#chkPostPaddingRequired', page).checked(item.IsPostPaddingRequired).checkboxradio('refresh');

        $('#chkNewOnly', page).checked(item.RecordNewOnly).checkboxradio('refresh');
        $('#chkAllChannels', page).checked(item.RecordAnyChannel).checkboxradio('refresh');
        $('#chkAnyTime', page).checked(item.RecordAnyTime).checkboxradio('refresh');

        var channelHtml = '';

        if (item.RecurrenceType == 'NewProgramEventsAllChannels' || item.RecurrenceType == 'AllProgramEventsAllChannels') {
            channelHtml += 'All Channels';
        }
        else if (item.ChannelId) {
            channelHtml += '<a href="livetvchannel.html?id=' + item.ChannelId + '">' + item.ChannelName + '</a>';
        }

        $('.channel', page).html('Channel:&nbsp;&nbsp;&nbsp;' + channelHtml).trigger('create');

        selectDays(page, item.Days);


        $('.time', page).html('Time:&nbsp;&nbsp;&nbsp;' + LiveTvHelpers.getDisplayTime(item.StartDate));

        Dashboard.hideLoadingMsg();
    }

    function selectDays(page, days) {

        var daysOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            $('#chk' + day, page).checked(days.indexOf(day) != -1).checkboxradio('refresh');

        }

    }

    function getDays(page) {

        var daysOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

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

        apiClient.getLiveTvSeriesTimer(currentItem.Id).done(function (item) {

            item.PrePaddingSeconds = $('#txtPrePaddingSeconds', form).val() * 60;
            item.PostPaddingSeconds = $('#txtPostPaddingSeconds', form).val() * 60;
            item.IsPrePaddingRequired = $('#chkPrePaddingRequired', form).checked();
            item.IsPostPaddingRequired = $('#chkPostPaddingRequired', form).checked();

            item.RecordNewOnly = $('#chkNewOnly', form).checked();
            item.RecordAnyChannel = $('#chkAllChannels', form).checked();
            item.RecordAnyTime = $('#chkAnyTime', form).checked();

            item.Days = getDays(form);

            ApiClient.updateLiveTvSeriesTimer(item).done(function () {
                Dashboard.alert('Timer Saved');
            });
        });

        // Disable default form submission
        return false;

    }
    
    function renderRecordings(page, result) {
        
        $('.recordingsTab', page).html(LibraryBrowser.getPosterViewHtml({

            items: result.Items,
            shape: "smallBackdrop",
            showTitle: true,
            overlayText: true,
            coverImage: true

        })).createPosterItemHoverMenu();
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

                imgUrl = ApiClient.getImageUrl(program.Id, {
                    height: 160,
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
                html += '<span class="liveTvProgram">LIVE&nbsp;&nbsp;</span>';
            }
            else if (program.IsPremiere) {
                html += '<span class="premiereTvProgram">PREMIERE&nbsp;&nbsp;</span>';
            }
            else if (program.IsSeries && !program.IsRepeat) {
                html += '<span class="newTvProgram">NEW&nbsp;&nbsp;</span>';
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

            html += '<a data-timerid="' + timer.Id + '" href="#" title="Cancel Recording" class="btnCancelTimer">Cancel Recording</a>';

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

        apiClient.getLiveTvSeriesTimer(id).done(function (result) {

            renderTimer(page, result);

        });

        apiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            seriesTimerId: id

        }).done(function (recordingResult) {

            renderRecordings(page, recordingResult);

        });

        apiClient.getLiveTvTimers({

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

        $('.radioSeriesTimerTab', page).checked(false).checkboxradio('refresh');
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

})(window, jQuery, document, ApiClient);