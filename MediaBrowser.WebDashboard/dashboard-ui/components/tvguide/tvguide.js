define(['jQuery', 'livetvcss', 'scripts/livetvcomponents', 'scrollStyles'], function ($) {

    return function (options) {

        var self = this;

        self.refresh = function () {
            reloadPage(options.element);
        };

        // 30 mins
        var cellCurationMinutes = 30;
        var cellDurationMs = cellCurationMinutes * 60 * 1000;
        var msPerDay = 86400000;

        var currentDate;

        var defaultChannels = browserInfo.mobile ? 50 : 100;
        var channelLimit = 1000;

        var channelQuery = {

            StartIndex: 0,
            Limit: defaultChannels,
            EnableFavoriteSorting: true
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

            channelQuery.UserId = Dashboard.getCurrentUserId();

            channelQuery.Limit = Math.min(channelQuery.Limit || defaultChannels, channelLimit);
            channelQuery.AddCurrentProgram = false;

            channelsPromise = channelsPromise || ApiClient.getLiveTvChannels(channelQuery);

            var date = currentDate;
            // Add one second to avoid getting programs that are just ending
            date = new Date(date.getTime() + 1000);

            // Subtract to avoid getting programs that are starting when the grid ends
            var nextDay = new Date(date.getTime() + msPerDay - 2000);

            console.log(nextDay);
            channelsPromise.then(function (channelsResult) {

                ApiClient.getLiveTvPrograms({
                    UserId: Dashboard.getCurrentUserId(),
                    MaxStartDate: nextDay.toISOString(),
                    MinEndDate: date.toISOString(),
                    channelIds: channelsResult.Items.map(function (c) {
                        return c.Id;
                    }).join(','),
                    ImageTypeLimit: 1,
                    EnableImages: false,
                    SortBy: "StartDate"

                }).then(function (programsResult) {

                    renderGuide(page, date, channelsResult.Items, programsResult.Items);

                    Dashboard.hideLoadingMsg();

                    LibraryBrowser.setLastRefreshed(page);

                });

                if (options.enablePaging !== false) {
                    var channelPagingHtml = LibraryBrowser.getQueryPagingHtml({
                        startIndex: channelQuery.StartIndex,
                        limit: channelQuery.Limit,
                        totalRecordCount: channelsResult.TotalRecordCount,
                        updatePageSizeSetting: false,
                        showLimit: true
                    });

                    var channelPaging = page.querySelector('.channelPaging');
                    channelPaging.innerHTML = channelPagingHtml;
                    $(channelPaging);
                }

                page.querySelector('.btnNextPage').addEventListener('click', function () {
                    channelQuery.StartIndex += channelQuery.Limit;
                    reloadChannels(page);
                });

                page.querySelector('.btnPreviousPage').addEventListener('click', function () {
                    channelQuery.StartIndex -= channelQuery.Limit;
                    reloadChannels(page);
                });

                page.querySelector('#selectPageSize').addEventListener('change', function () {
                    channelQuery.Limit = parseInt(this.value);
                    channelQuery.StartIndex = 0;
                    reloadChannels(page);
                });
            });
        }

        function getTimeslotHeadersHtml(startDate, endDateTime) {

            var html = '';

            // clone
            startDate = new Date(startDate.getTime());

            html += '<div class="timeslotHeadersInner">';

            while (startDate.getTime() < endDateTime) {

                html += '<div class="timeslotHeader">';
                html += '<div class="timeslotHeaderInner">';

                html += LibraryBrowser.getDisplayTime(startDate);
                html += '</div>';
                html += '</div>';

                // Add 30 mins
                startDate.setTime(startDate.getTime() + cellDurationMs);
            }
            html += '</div>';

            return html;
        }

        function parseDates(program) {

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

            return null;
        }

        function getChannelProgramsHtml(page, date, channel, programs) {

            var html = '';

            var startMs = date.getTime();
            var endMs = startMs + msPerDay - 1;

            programs = programs.filter(function (curr) {
                return curr.ChannelId == channel.Id;
            });

            html += '<div class="channelPrograms">';

            for (var i = 0, length = programs.length; i < length; i++) {

                var program = programs[i];

                if (program.ChannelId != channel.Id) {
                    continue;
                }

                parseDates(program);

                if (program.EndDateLocal.getTime() < startMs) {
                    continue;
                }

                if (program.StartDateLocal.getTime() > endMs) {
                    break;
                }

                var renderStartMs = Math.max(program.StartDateLocal.getTime(), startMs);
                var startPercent = (program.StartDateLocal.getTime() - startMs) / msPerDay;
                startPercent *= 100;
                startPercent = Math.max(startPercent, 0);

                var renderEndMs = Math.min(program.EndDateLocal.getTime(), endMs);
                var endPercent = (renderEndMs - renderStartMs) / msPerDay;
                endPercent *= 100;

                var cssClass = "programCell";
                var addAccent = true;

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
                    addAccent = false;
                }

                html += '<a href="itemdetails.html?id=' + program.Id + '" class="' + cssClass + '" data-programid="' + program.Id + '" style="left:' + startPercent + '%;width:' + endPercent + '%;">';

                html += '<div class="guideProgramName">';
                html += program.Name;
                html += '</div>';

                html += '<div class="guideProgramTime">';
                if (program.IsLive) {
                    html += '<span class="liveTvProgram">' + Globalize.translate('LabelLiveProgram') + '&nbsp;&nbsp;</span>';
                }
                else if (program.IsPremiere) {
                    html += '<span class="premiereTvProgram">' + Globalize.translate('LabelPremiereProgram') + '&nbsp;&nbsp;</span>';
                }
                else if (program.IsSeries && !program.IsRepeat) {
                    html += '<span class="newTvProgram">' + Globalize.translate('LabelNewProgram') + '&nbsp;&nbsp;</span>';
                }

                html += LibraryBrowser.getDisplayTime(program.StartDateLocal);
                html += ' - ';
                html += LibraryBrowser.getDisplayTime(program.EndDateLocal);

                if (program.SeriesTimerId) {
                    html += '<div class="timerCircle seriesTimerCircle"></div>';
                    html += '<div class="timerCircle seriesTimerCircle"></div>';
                    html += '<div class="timerCircle seriesTimerCircle"></div>';
                }
                else if (program.TimerId) {

                    html += '<div class="timerCircle"></div>';
                }
                html += '</div>';

                if (addAccent) {
                    html += '<div class="programAccent"></div>';
                }

                html += '</a>';
            }

            html += '</div>';

            return html;
        }

        function renderPrograms(page, date, channels, programs) {

            var html = [];

            for (var i = 0, length = channels.length; i < length; i++) {

                html.push(getChannelProgramsHtml(page, date, channels[i], programs));
            }

            var programGrid = page.querySelector('.programGrid');
            programGrid.innerHTML = html.join('');

            $(programGrid).scrollTop(0).scrollLeft(0);
        }

        function renderChannelHeaders(page, channels) {

            var html = '';

            for (var i = 0, length = channels.length; i < length; i++) {

                var channel = channels[i];

                html += '<div class="channelHeaderCellContainer">';

                html += '<a class="channelHeaderCell" href="itemdetails.html?id=' + channel.Id + '">';

                var hasChannelImage = channel.ImageTags.Primary;
                var cssClass = hasChannelImage ? 'guideChannelInfo guideChannelInfoWithImage' : 'guideChannelInfo';

                html += '<div class="' + cssClass + '">' + channel.Number + '</div>';

                if (hasChannelImage) {

                    var url = ApiClient.getScaledImageUrl(channel.Id, {
                        maxHeight: 44,
                        maxWidth: 70,
                        tag: channel.ImageTags.Primary,
                        type: "Primary"
                    });

                    html += '<div class="guideChannelImage lazy" data-src="' + url + '"></div>';
                } else {
                    html += '<div class="guideChannelName">' + channel.Name + '</div>';
                }

                html += '</a>';

                html += '</div>';
            }

            var channelList = page.querySelector('.channelList');
            channelList.innerHTML = html;
            ImageLoader.lazyChildren(channelList);
        }

        function renderGuide(page, date, channels, programs) {

            renderChannelHeaders(page, channels);

            var startDate = date;
            var endDate = new Date(startDate.getTime() + msPerDay);
            page.querySelector('.timeslotHeaders').innerHTML = getTimeslotHeadersHtml(startDate, endDate);
            renderPrograms(page, date, channels, programs);
        }

        var gridScrolling = false;
        var headersScrolling = false;
        function onProgramGridScroll(page, elem) {

            if (!headersScrolling) {
                gridScrolling = true;

                $(page.querySelector('.timeslotHeaders')).scrollLeft($(elem).scrollLeft());
                gridScrolling = false;
            }
        }

        function onTimeslotHeadersScroll(page, elem) {

            if (!gridScrolling) {
                headersScrolling = true;
                $(page.querySelector('.programGrid')).scrollLeft($(elem).scrollLeft());
                headersScrolling = false;
            }
        }

        function changeDate(page, date) {

            currentDate = normalizeDateToTimeslot(date);

            reloadGuide(page);

            var text = LibraryBrowser.getFutureDateText(date);
            text = '<span class="currentDay">' + text.replace(' ', ' </span>');
            page.querySelector('.currentDate').innerHTML = text;
        }

        var dateOptions = [];

        function setDateRange(page, guideInfo) {

            var today = new Date();
            today.setHours(today.getHours(), 0, 0, 0);

            var start = parseISO8601Date(guideInfo.StartDate, { toLocal: true });
            var end = parseISO8601Date(guideInfo.EndDate, { toLocal: true });

            start.setHours(0, 0, 0, 0);
            end.setHours(0, 0, 0, 0);

            if (start.getTime() >= end.getTime()) {
                end.setDate(start.getDate() + 1);
            }

            start = new Date(Math.max(today, start));

            dateOptions = [];

            while (start <= end) {

                dateOptions.push({
                    name: LibraryBrowser.getFutureDateText(start),
                    id: start.getTime(),
                    ironIcon: 'today'
                });

                start.setDate(start.getDate() + 1);
                start.setHours(0, 0, 0, 0);
            }

            var date = new Date();

            if (currentDate) {
                date.setTime(currentDate.getTime());
            }

            changeDate(page, date);
        }

        function reloadPageAfterValidation(page, limit) {

            channelLimit = limit;

            ApiClient.getLiveTvGuideInfo().then(function (guideInfo) {

                setDateRange(page, guideInfo);
            });
        }

        function reloadPage(page) {

            $('.guideRequiresUnlock', page).hide();

            RegistrationServices.validateFeature('livetv').then(function () {
                Dashboard.showLoadingMsg();

                reloadPageAfterValidation(page, 1000);
            }, function () {

                Dashboard.showLoadingMsg();

                var limit = 5;
                $('.guideRequiresUnlock', page).show();
                $('.unlockText', page).html(Globalize.translate('MessageLiveTvGuideRequiresUnlock', limit));

                reloadPageAfterValidation(page, limit);
            });
        }

        function selectDate(page) {

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({
                    items: dateOptions,
                    showCancel: true,
                    title: Globalize.translate('HeaderSelectDate'),
                    callback: function (id) {

                        var date = new Date();
                        date.setTime(parseInt(id));
                        changeDate(page, date);
                    }
                });

            });
        }

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/tvguide/tvguide.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;
            var tabContent = options.element;
            tabContent.innerHTML = Globalize.translateDocument(template);

            tabContent.querySelector('.programGrid').addEventListener('scroll', function (e) {

                onProgramGridScroll(tabContent, e.target);
            });

            if (browserInfo.mobile) {
                tabContent.querySelector('.tvGuide').classList.add('mobileGuide');
            } else {

                tabContent.querySelector('.tvGuide').classList.remove('mobileGuide');

                tabContent.querySelector('.timeslotHeaders').addEventListener('scroll', function (e) {

                    onTimeslotHeadersScroll(tabContent, e.target);
                });
            }

            if (AppInfo.enableHeadRoom && options.enableHeadRoom) {
                requirejs(["headroom"], function () {

                    // construct an instance of Headroom, passing the element
                    var headroom = new Headroom(tabContent.querySelector('.tvGuideHeader'));
                    // initialise
                    headroom.init();
                });
            }

            $('.btnUnlockGuide', tabContent).on('click', function () {

                reloadPage(tabContent);
            });

            $('.btnSelectDate', tabContent).on('click', function () {

                selectDate(tabContent);
            });

            self.refresh();
        }

        xhr.send();
    };
});