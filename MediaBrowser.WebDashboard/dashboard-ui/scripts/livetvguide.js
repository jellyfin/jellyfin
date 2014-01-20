(function ($, document, apiClient) {

    // 30 mins
    var cellCurationMinutes = 30;
    var cellDurationMs = cellCurationMinutes * 60 * 1000;

    var gridLocalStartDateMs;
    var gridLocalEndDateMs;

    var currentDate;

    var channelQuery = {

        StartIndex: 0,
        Limit: 20
    };
    var channelsPromise;

    function normalizeDateToTimeslot(date) {

        var minutesOffset = date.getMinutes() - cellCurationMinutes;

        if (minutesOffset >= 0) {

            date.setHours(date.getHours(), cellCurationMinutes, 0, 0);

        } else {

            date.setHours(date.getHours(), 0, 0, 0);
        }

        return date;
    }

    function reloadChannels(page) {
        channelsPromise = null;
        reloadGuide(page);
    }

    function reloadGuide(page) {

        Dashboard.showLoadingMsg();

        channelQuery.userId = Dashboard.getCurrentUserId();

        channelsPromise = channelsPromise || apiClient.getLiveTvChannels(channelQuery);

        var date = currentDate;

        var nextDay = new Date(date.getTime());
        nextDay.setHours(0, 0, 0, 0);
        nextDay.setDate(nextDay.getDate() + 1);
        console.log(nextDay);
        channelsPromise.done(function (channelsResult) {

            apiClient.getLiveTvPrograms({
                UserId: Dashboard.getCurrentUserId(),
                MaxStartDate: nextDay.toISOString(),
                MinEndDate: date.toISOString(),
                channelIds: channelsResult.Items.map(function (c) {
                    return c.Id;
                }).join(',')

            }).done(function (programsResult) {

                renderGuide(page, date, channelsResult.Items, programsResult.Items);
                Dashboard.hideLoadingMsg();
            });

            var channelPagingHtml = LibraryBrowser.getPagingHtml(channelQuery, channelsResult.TotalRecordCount, false, [10, 20, 30, 50, 100]);
            $('.channelPaging', page).html(channelPagingHtml).trigger('create');

            $('.selectPage', page).on('change', function () {
                channelQuery.StartIndex = (parseInt(this.value) - 1) * channelQuery.Limit;
                reloadChannels(page);
            });

            $('.btnNextPage', page).on('click', function () {
                channelQuery.StartIndex += channelQuery.Limit;
                reloadChannels(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                channelQuery.StartIndex -= channelQuery.Limit;
                reloadChannels(page);
            });

            $('.selectPageSize', page).on('change', function () {
                channelQuery.Limit = parseInt(this.value);
                channelQuery.StartIndex = 0;
                reloadChannels(page);
            });
        });
    }

    function getTimeslotHeadersHtml(date) {

        var html = '';

        date = new Date(date.getTime());
        var dateNumber = date.getDate();

        while (date.getDate() == dateNumber) {

            html += '<div class="timeslotHeader">';
            html += '<div class="timeslotHeaderInner">';
            html += LiveTvHelpers.getDisplayTime(date);
            html += '</div>';
            html += '</div>';

            // Add 30 mins
            date.setTime(date.getTime() + cellDurationMs);
        }

        return html;
    }

    function findProgramStartingInCell(programs, startIndex, cellStart, cellEnd, cellIndex) {

        for (var i = startIndex, length = programs.length; i < length; i++) {

            var program = programs[i];

            if (!program.StartDateLocal) {
                try {

                    program.StartDateLocal = parseISO8601Date(program.StartDate, { toLocal: true });

                } catch (err) {

                }

            }

            if (!program.EndDateLocal) {
                try {

                    program.EndDateLocal = parseISO8601Date(program.EndDate, { toLocal: true });

                } catch (err) {

                }

            }

            var localTime = program.StartDateLocal.getTime();
            if ((localTime >= cellStart || cellIndex == 0) && localTime < cellEnd && program.EndDateLocal > cellStart) {

                return {

                    index: i,
                    program: program
                };

            }
        }

        return null;
    }

    function getProgramWidth(program) {

        var end = Math.min(gridLocalEndDateMs, program.EndDateLocal.getTime());
        var start = Math.max(gridLocalStartDateMs, program.StartDateLocal.getTime());

        var ms = end - start;

        var width = 100 * ms / cellDurationMs;

        // Round to the nearest cell
        var overlap = width % 100;

        if (overlap) {
            width = width - overlap + 100;
        }

        if (width > 300) {
            width += (width / 100) - 3;
        }

        return width;
    }

    function getChannelProgramsHtml(page, date, channel, programs) {

        var html = '';

        var dateNumber = date.getDate();

        programs = programs.filter(function (curr) {
            return curr.ChannelId == channel.Id;
        });

        html += '<div class="channelPrograms">';

        var programIndex = 0;
        var cellIndex = 0;

        while (date.getDate() == dateNumber) {

            // Add 30 mins
            var cellEndDate = new Date(date.getTime() + cellDurationMs);

            var program = findProgramStartingInCell(programs, programIndex, date, cellEndDate, cellIndex);

            if (program) {
                programIndex = program.index + 1;
                program = program.program;
            }

            html += '<div class="timeslotCell">';

            var cellTagName;
            var href;
            var cssClass = "timeslotCellInner";
            var style;
            var dataProgramId;

            if (program) {
                if (program.IsKids) {
                    cssClass += " childProgramInfo";
                } else if (program.IsSports) {
                    cssClass += " sportsProgramInfo";
                } else if (program.IsNews) {
                    cssClass += " newsProgramInfo";
                } else if (program.IsMovie) {
                    cssClass += " movieProgramInfo";
                }
                else {
                    cssClass += " plainProgramInfo";
                }

                cssClass += " timeslotCellInnerWithProgram";

                cellTagName = "a";
                href = ' href="livetvprogram.html?id=' + program.Id + '"';

                var width = getProgramWidth(program);

                if (width && width != 100) {
                    style = ' style="width:' + width + '%;"';
                } else {
                    style = '';
                }
                dataProgramId = ' data-programid="' + program.Id + '"';
            } else {
                cellTagName = "div";
                href = '';
                style = '';
                dataProgramId = '';
            }

            html += '<' + cellTagName + dataProgramId + ' class="' + cssClass + '"' + href + style + '>';

            if (program) {

                html += '<div class="guideProgramName">';
                html += program.Name;

                html += '</div>';

                html += '<div class="guideProgramTime">';

                if (program.IsLive) {
                    html += '<span class="liveTvProgram">LIVE&nbsp;&nbsp;</span>';
                }
                else if (program.IsPremiere) {
                    html += '<span class="premiereTvProgram">PREMIERE&nbsp;&nbsp;</span>';
                }
                else if (program.IsSeries && !program.IsRepeat) {
                    html += '<span class="newTvProgram">NEW&nbsp;&nbsp;</span>';
                }

                html += LiveTvHelpers.getDisplayTime(program.StartDateLocal);
                html += ' - ';
                html += LiveTvHelpers.getDisplayTime(program.EndDateLocal);

                if (program.SeriesTimerId) {
                    html += '<div class="timerCircle seriesTimerCircle"></div>';
                    html += '<div class="timerCircle seriesTimerCircle"></div>';
                    html += '<div class="timerCircle seriesTimerCircle"></div>';
                }
                else if (program.TimerId) {

                    html += '<div class="timerCircle"></div>';
                }

                html += '</div>';

            } else {
                html += '&nbsp;';
            }

            html += '</' + cellTagName + '>';
            html += '</div>';

            date = cellEndDate;
            cellIndex++;
        }
        html += '</div>';

        return html;
    }

    function renderPrograms(page, date, channels, programs) {

        var html = [];

        for (var i = 0, length = channels.length; i < length; i++) {

            html.push(getChannelProgramsHtml(page, date, channels[i], programs));
        }

        $('.programGrid', page).html(html.join('')).scrollTop(0).scrollLeft(0)
            .createGuideHoverMenu('.timeslotCellInnerWithProgram');
    }

    function renderChannelHeaders(page, channels) {

        var html = '';

        for (var i = 0, length = channels.length; i < length; i++) {

            var channel = channels[i];

            html += '<div class="channelHeaderCellContainer">';

            html += '<div class="channelHeaderCell">';
            html += '<a class="channelHeaderCellInner" href="livetvchannel.html?id=' + channel.Id + '">';

            html += '<div class="guideChannelInfo">' + channel.Name + '<br/>' + channel.Number + '</div>';

            if (channel.ImageTags.Primary) {

                var url = ApiClient.getImageUrl(channel.Id, {
                    maxheight: 200,
                    maxwidth: 200,
                    tag: channel.ImageTags.Primary,
                    type: "Primary"
                });

                html += '<img class="guideChannelImage" src="' + url + '" />';
            }

            html += '</a>';
            html += '</div>';

            html += '</div>';
        }

        $('.channelList', page).html(html);
    }

    function renderGuide(page, date, channels, programs) {

        renderChannelHeaders(page, channels);
        $('.timeslotHeaders', page).html(getTimeslotHeadersHtml(date));
        renderPrograms(page, date, channels, programs);
    }

    function onProgramGridScroll(page, elem) {

        var grid = $(elem);

        grid.prev().scrollTop(grid.scrollTop());
        $('.timeslotHeaders', page).scrollLeft(grid.scrollLeft());
    }

    function changeDate(page, date) {

        currentDate = normalizeDateToTimeslot(date);

        gridLocalStartDateMs = currentDate.getTime();

        var clone = new Date(gridLocalStartDateMs);
        clone.setHours(0, 0, 0, 0);
        clone.setDate(clone.getDate() + 1);
        gridLocalEndDateMs = clone.getTime() - 1;

        reloadGuide(page);
    }

    function setDateRange(page, guideInfo) {

        var today = new Date();
        today.setHours(today.getHours(), 0, 0, 0);

        var start = parseISO8601Date(guideInfo.StartDate, { toLocal: true });
        var end = parseISO8601Date(guideInfo.EndDate, { toLocal: true });

        start.setHours(0, 0, 0, 0);
        end.setHours(0, 0, 0, 0);

        start = new Date(Math.max(today, start));

        var html = '';

        while (start <= end) {


            html += '<option value="' + start.getTime() + '">' + LibraryBrowser.getFutureDateText(start) + '</option>';

            start.setDate(start.getDate() + 1);
            start.setHours(0, 0, 0, 0);
        }

        var elem = $('#selectDate', page).html(html).selectmenu('refresh');

        if (currentDate) {
            elem.val(currentDate.getTime()).selectmenu('refresh');
        }

        var val = elem.val();
        var date = new Date();
        date.setTime(parseInt(val));

        changeDate(page, date);
    }

    $(document).on('pageinit', "#liveTvGuidePage", function () {

        var page = this;

        $('.programGrid', page).on('scroll', function () {

            onProgramGridScroll(page, this);
        });

        $('#selectDate', page).on('change', function () {

            var date = new Date();
            date.setTime(parseInt(this.value));

            changeDate(page, date);

        });

    }).on('pagebeforeshow', "#liveTvGuidePage", function () {

        var page = this;

        apiClient.getLiveTvGuideInfo().done(function (guideInfo) {

            setDateRange(page, guideInfo);
        });
    });

})(jQuery, document, ApiClient);

(function ($, document, window) {

    var showOverlayTimeout;
    var hideOverlayTimeout;
    var currentPosterItem;

    function onOverlayMouseOver() {

        if (hideOverlayTimeout) {
            clearTimeout(hideOverlayTimeout);
            hideOverlayTimeout = null;
        }
    }

    function onOverlayMouseOut() {

        startHideOverlayTimer();
    }

    function getOverlayHtml(item) {

        var html = '';

        html += '<div class="itemOverlayContent">';

        if (item.EpisodeTitle) {
            html += '<p>';
            html += item.EpisodeTitle;
            html += '</p>';
        }

        html += '<p class="itemMiscInfo miscTvProgramInfo"></p>';

        html += '<p style="margin: 1.25em 0;">';
        html += '<div class="itemCommunityRating" style="display:inline-block;">';
        html += LibraryBrowser.getRatingHtml(item);
        html += '</div>';
        html += '<span class="userDataIcons">';
        html += LibraryBrowser.getUserDataIconsHtml(item);
        html += '</span>';
        html += '</p>';

        html += '<p class="itemGenres"></p>';

        html += '<p class="itemOverlayHtml">';
        html += (item.OverviewHtml || item.Overview || '');
        html += '</p>';

        html += '</div>';

        return html;
    }

    function showOverlay(elem, item) {

        $('.itemFlyout').popup('close').remove();

        var html = '<div data-role="popup" class="itemFlyout" data-theme="b" data-arrow="true" data-history="false">';

        html += '<div class="ui-bar-b" style="text-align:center;">';
        html += '<h3 style="margin: .5em 0;padding:0 1em;font-weight:normal;">' + item.Name + '</h3>';
        html += '</div>';

        html += '<div style="padding: 0 1em;">';
        html += getOverlayHtml(item);
        html += '</div>';

        html += '</div>';

        $('.itemFlyout').popup('close').popup('destroy').remove();

        $(document.body).append(html);

        var popup = $('.itemFlyout').on('mouseenter', onOverlayMouseOver).on('mouseleave', onOverlayMouseOut).popup({

            positionTo: elem

        }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").off("mouseenter").off("mouseleave").remove();
        });

        LibraryBrowser.renderGenres($('.itemGenres', popup), {
            Type: item.type,
            Genres: item.Genres.splice(0, 3)
        }, 'livetv');
        LiveTvHelpers.renderMiscProgramInfo($('.miscTvProgramInfo', popup), item);

        popup.parents().prev('.ui-popup-screen').remove();
        currentPosterItem = elem;
    }

    function onProgramClicked() {

        if (showOverlayTimeout) {
            clearTimeout(showOverlayTimeout);
            showOverlayTimeout = null;
        }

        if (hideOverlayTimeout) {
            clearTimeout(hideOverlayTimeout);
            hideOverlayTimeout = null;
        }

        hideOverlay();
    }

    function hideOverlay() {

        $('.itemFlyout').popup('close').remove();

        if (currentPosterItem) {

            $(currentPosterItem).off('click.overlay');
            currentPosterItem = null;
        }
    }

    function startHideOverlayTimer() {

        if (hideOverlayTimeout) {
            clearTimeout(hideOverlayTimeout);
            hideOverlayTimeout = null;
        }

        hideOverlayTimeout = setTimeout(hideOverlay, 200);
    }

    function onHoverOut() {

        if (showOverlayTimeout) {
            clearTimeout(showOverlayTimeout);
            showOverlayTimeout = null;
        }

        startHideOverlayTimer();
    }

    $.fn.createGuideHoverMenu = function (childSelector) {

        function onShowTimerExpired(elem) {

            var id = elem.getAttribute('data-programid');

            ApiClient.getLiveTvProgram(id, Dashboard.getCurrentUserId()).done(function (item) {

                showOverlay(elem, item);

            });
        }

        function onHoverIn() {

            if (showOverlayTimeout) {
                clearTimeout(showOverlayTimeout);
                showOverlayTimeout = null;
            }

            if (hideOverlayTimeout) {
                clearTimeout(hideOverlayTimeout);
                hideOverlayTimeout = null;
            }

            var elem = this;

            if (currentPosterItem) {
                if (currentPosterItem && currentPosterItem == elem) {
                    return;
                } else {
                    hideOverlay();
                }
            }

            showOverlayTimeout = setTimeout(function () {

                onShowTimerExpired(elem);

            }, 1000);
        }

        // https://hacks.mozilla.org/2013/04/detecting-touch-its-the-why-not-the-how/

        if (('ontouchstart' in window) || (navigator.maxTouchPoints > 0) || (navigator.msMaxTouchPoints > 0)) {
            /* browser with either Touch Events of Pointer Events
               running on touch-capable device */
            return this;
        }

        return this.on('mouseenter', childSelector, onHoverIn)
            .on('mouseleave', childSelector, onHoverOut)
            .on('click', childSelector, onProgramClicked);
    };

})(jQuery, document, window);
