(function ($, document, apiClient) {

    var currentItem;
    var programs;

    function renderPrograms(page, result) {

        var html = '';

        var currentIndexValue;

        var now = new Date();

        for (var i = 0, length = result.Items.length; i < length; i++) {

            var program = result.Items[i];

            var startDate = parseISO8601Date(program.StartDate, { toLocal: true });
            var startDateText = LibraryBrowser.getFutureDateText(startDate);

            var endDate = parseISO8601Date(program.EndDate, { toLocal: true });

            if (startDateText != currentIndexValue) {

                html += '<h2 class="detailSectionHeader tvProgramSectionHeader">' + startDateText + '</h2>';
                currentIndexValue = startDateText;
            }

            html += '<a href="livetvprogram.html?id=' + program.Id + '" class="tvProgram">';

            var cssClass = "tvProgramTimeSlot";

            if (now >= startDate && now < endDate) {
                cssClass += " tvProgramCurrentTimeSlot";
            }

            html += '<div class="' + cssClass + '">';
            html += '<div class="tvProgramTimeSlotInner">' + LiveTvHelpers.getDisplayTime(startDate) + '</div>';
            html += '</div>';

            cssClass = "tvProgramInfo";

            if (program.IsKids) {
                cssClass += " childProgramInfo";
            }
            else if (program.IsSports) {
                cssClass += " sportsProgramInfo";
            }
            else if (program.IsNews) {
                cssClass += " newsProgramInfo";
            }
            else if (program.IsMovie) {
                cssClass += " movieProgramInfo";
            }

            html += '<div class="' + cssClass + '">';

            var name = program.Name;
            
            if (program.IsRepeat) {
                name += " (R)";
            }
            html += '<div class="tvProgramName">' + name + '</div>';

            html += '<div class="tvProgramTime">';

            if (program.IsLive) {
                html += '<span class="liveTvProgram">LIVE&nbsp;&nbsp;</span>';
            }
            else if (program.IsPremiere) {
                html += '<span class="premiereTvProgram">PREMIERE&nbsp;&nbsp;</span>';
            }
            else if (program.IsSeries && !program.IsRepeat) {
                html += '<span class="newTvProgram">NEW&nbsp;&nbsp;</span>';
            }

            var minutes = program.RunTimeTicks / 600000000;

            minutes = Math.round(minutes || 1) + ' min';

            if (program.EpisodeTitle) {

                html += program.EpisodeTitle + '&nbsp;&nbsp;(' + minutes + ')';
            } else {
                html += minutes;
            }

            if (program.SeriesTimerId) {
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
            }
            else if (program.TimerId) {

                html += '<div class="timerCircle"></div>';
            }

            html += '</div>';
            html += '</div>';

            html += '</a>';
        }

        $('#programList', page).html(html).trigger('create');
    }

    function loadPrograms(page) {

        ApiClient.getLiveTvPrograms({

            ChannelIds: currentItem.Id,
            UserId: Dashboard.getCurrentUserId()

        }).done(function (result) {

            renderPrograms(page, result);
            programs = result.Items;

            Dashboard.hideLoadingMsg();
        });
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvChannel(getParameterByName('id'), Dashboard.getCurrentUserId()).done(function (item) {

            currentItem = item;

            var name = item.Name;

            $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

            Dashboard.setPageTitle(name);

            $('.itemName', page).html(item.Number + ' ' + name);

            $('.userDataIcons', page).html(LibraryBrowser.getUserDataIconsHtml(item));

            if (ApiClient.isWebSocketOpen()) {

                var vals = [item.Type, item.Id, item.Name];

                vals.push('livetv');

                ApiClient.sendWebSocketMessage("Context", vals.join('|'));
            }

            if (MediaPlayer.canPlay(item)) {
                $('#playButtonContainer', page).show();
            } else {
                $('#playButtonContainer', page).hide();
            }

            Dashboard.getCurrentUser().done(function (user) {

                if (user.Configuration.IsAdministrator && item.LocationType !== "Offline") {
                    $('#editButtonContainer', page).show();
                } else {
                    $('#editButtonContainer', page).hide();
                }

            });

            loadPrograms(page);

        });
    }

    window.LiveTvHelpers = {

        getDisplayTime: function (date) {

            if ((typeof date).toString().toLowerCase() === 'string') {
                try {

                    date = parseISO8601Date(date, { toLocal: true });

                } catch (err) {
                    return date;
                }
            }

            date = date.toLocaleTimeString();

            date = date.replace('0:00', '0').replace(':00 ', '').replace(' ', '');

            return date;
        },

        renderMiscProgramInfo: function (elem, obj) {

            var html = [];

            if (obj.IsSeries && !obj.IsRepeat) {

                html.push('<span class="newTvProgram">NEW</span>');

            }

            if (obj.IsLive) {

                html.push('<span class="liveTvProgram">LIVE</span>');

            }

            if (obj.ChannelId) {
                html.push('<a class="textlink" href="livetvchannel.html?id=' + obj.ChannelId + '">' + obj.ChannelName + '</a>');
            }

            if (obj.IsHD) {

                html.push('HD');

            }

            if (obj.Audio) {

                html.push(obj.Audio);

            }

            html = html.join('&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;');

            if (obj.SeriesTimerId) {
                html += '<a href="livetvseriestimer.html?id=' + obj.SeriesTimerId + '" title="View Series Recording">';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '</a>';
            }
            else if (obj.TimerId) {

                html += '<a href="livetvtimer.html?id=' + obj.TimerId + '">';
                html += '<div class="timerCircle"></div>';
                html += '</a>';
            }

            elem.html(html).trigger('create');
        },

        renderOriginalAirDate: function (elem, item) {

            var airDate = item.OriginalAirDate;

            if (airDate) {

                try {
                    airDate = parseISO8601Date(airDate, { toLocal: true }).toLocaleDateString();
                }
                catch (e) {
                    console.log("Error parsing date: " + airDate);
                }


                elem.html('Original air date:&nbsp;&nbsp;' + airDate).show();
            } else {
                elem.hide();
            }
        }

    };

    $(document).on('pageinit', "#liveTvChannelPage", function () {

        var page = this;

        $('#btnPlay', page).on('click', function () {
            var userdata = currentItem.UserData || {};
            LibraryBrowser.showPlayMenu(this, currentItem.Id, currentItem.Type, currentItem.MediaType, userdata.PlaybackPositionTicks);
        });

        $('#btnRemote', page).on('click', function () {

            RemoteControl.showMenuForItem({ item: currentItem, context: 'livetv' });
        });

        $('#btnEdit', page).on('click', function () {

            Dashboard.navigate("edititemmetadata.html?channelid=" + currentItem.Id);
        });

    }).on('pageshow', "#liveTvChannelPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvChannelPage", function () {

        currentItem = null;
        programs = null;
    });

})(jQuery, document, ApiClient);