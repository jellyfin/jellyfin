(function ($, document) {

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
            html += '<div class="tvProgramTimeSlotInner">' + LibraryBrowser.getDisplayTime(startDate) + '</div>';
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

            html += '<div data-programid="' + program.Id + '" class="' + cssClass + '">';

            var name = program.Name;

            html += '<div class="tvProgramName">' + name + '</div>';

            html += '<div class="tvProgramTime">';

            if (program.IsLive) {
                html += '<span class="liveTvProgram">'+Globalize.translate('LabelLiveProgram')+'&nbsp;&nbsp;</span>';
            }
            else if (program.IsPremiere) {
                html += '<span class="premiereTvProgram">'+Globalize.translate('LabelPremiereProgram')+'&nbsp;&nbsp;</span>';
            }
            else if (program.IsSeries && !program.IsRepeat) {
                html += '<span class="newTvProgram">'+Globalize.translate('LabelNewProgram')+'&nbsp;&nbsp;</span>';
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

        $('#programList', page).html(html).trigger('create').createGuideHoverMenu('.tvProgramInfo');
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

            $(page).trigger('displayingitem', [{

                item: item,
                context: 'livetv'
            }]);

            Dashboard.getCurrentUser().done(function (user) {

                if (MediaController.canPlay(item)) {
                    $('#playButtonContainer', page).show();
                } else {
                    $('#playButtonContainer', page).hide();
                }

                if (user.Policy.IsAdministrator && item.LocationType !== "Offline") {
                    $('#editButtonContainer', page).show();
                } else {
                    $('#editButtonContainer', page).hide();
                }

            });

            loadPrograms(page);

        });
    }

    $(document).on('pageinitdepends', "#liveTvChannelPage", function () {

        var page = this;

        $('#btnPlay', page).on('click', function () {
            var userdata = currentItem.UserData || {};
            LibraryBrowser.showPlayMenu(null, currentItem.Id, currentItem.Type, false, currentItem.MediaType, userdata.PlaybackPositionTicks);
        });

        $('#btnEdit', page).on('click', function () {

            Dashboard.navigate("edititemmetadata.html?channelid=" + currentItem.Id);
        });

    }).on('pagebeforeshowready', "#liveTvChannelPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvChannelPage", function () {

        currentItem = null;
        programs = null;
    });

})(jQuery, document);