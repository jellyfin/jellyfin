define(['require', 'browser', 'globalize', 'connectionManager', 'serverNotifications', 'loading', 'datetime', 'focusManager', 'imageLoader', 'events', 'layoutManager', 'itemShortcuts', 'registrationservices', 'dom', 'clearButtonStyle', 'css!./guide.css', 'material-icons', 'scrollStyles', 'emby-button', 'paper-icon-button-light'], function (require, browser, globalize, connectionManager, serverNotifications, loading, datetime, focusManager, imageLoader, events, layoutManager, itemShortcuts, registrationServices, dom) {

    function Guide(options) {

        var self = this;
        var items = {};

        self.options = options;

        // 30 mins
        var cellCurationMinutes = 30;
        var cellDurationMs = cellCurationMinutes * 60 * 1000;
        var msPerDay = 86400000;
        var totalRendererdMs = msPerDay;

        var currentDate;
        var currentStartIndex = 0;
        var currentChannelLimit = 0;

        var channelQuery = {

            StartIndex: 0,
            EnableFavoriteSorting: true
        };

        var channelsPromise;

        self.refresh = function () {

            currentDate = null;
            reloadPage(options.element);
        };

        self.destroy = function () {

            events.off(serverNotifications, 'TimerCreated', onTimerCreated);
            events.off(serverNotifications, 'SeriesTimerCreated', onSeriesTimerCreated);
            events.off(serverNotifications, 'TimerCancelled', onTimerCancelled);
            events.off(serverNotifications, 'SeriesTimerCancelled', onSeriesTimerCancelled);

            clearCurrentTimeUpdateInterval();
            setScrollEvents(options.element, false);
            itemShortcuts.off(options.element);
            items = {};
        };

        function normalizeDateToTimeslot(date) {

            var minutesOffset = date.getMinutes() - cellCurationMinutes;

            if (minutesOffset >= 0) {

                date.setHours(date.getHours(), cellCurationMinutes, 0, 0);

            } else {

                date.setHours(date.getHours(), 0, 0, 0);
            }

            return date;
        }

        function showLoading() {
            loading.show();
        }

        function hideLoading() {
            loading.hide();
        }

        var currentTimeUpdateInterval;
        var currentTimeIndicatorBar;
        var currentTimeIndicatorArrow;
        function startCurrentTimeUpdateInterval() {
            clearCurrentTimeUpdateInterval();

            //currentTimeUpdateInterval = setInterval(updateCurrentTimeIndicator, 1000);
            currentTimeUpdateInterval = setInterval(updateCurrentTimeIndicator, 60000);
            updateCurrentTimeIndicator();
        }

        function clearCurrentTimeUpdateInterval() {
            var interval = currentTimeUpdateInterval;
            if (interval) {
                clearInterval(interval);
            }
            currentTimeUpdateInterval = null;
            currentTimeIndicatorBar = null;
            currentTimeIndicatorArrow = null;
        }

        function updateCurrentTimeIndicator() {

            if (!currentTimeIndicatorBar) {
                currentTimeIndicatorBar = options.element.querySelector('.currentTimeIndicatorBar');
            }
            if (!currentTimeIndicatorArrow) {
                currentTimeIndicatorArrow = options.element.querySelector('.currentTimeIndicatorArrowContainer');
            }

            var dateDifference = new Date().getTime() - currentDate.getTime();
            var pct = dateDifference > 0 ? (dateDifference / totalRendererdMs) : 0;
            pct = Math.min(pct, 1);

            if (pct <= 0 || pct >= 1) {
                currentTimeIndicatorBar.classList.add('hide');
                currentTimeIndicatorArrow.classList.add('hide');
            } else {
                currentTimeIndicatorBar.classList.remove('hide');
                currentTimeIndicatorArrow.classList.remove('hide');

                //pct *= 100;
                //pct = 100 - pct;
                //currentTimeIndicatorElement.style.width = (pct * 100) + '%';
                currentTimeIndicatorBar.style.transform = 'scaleX(' + pct + ')';
                currentTimeIndicatorArrow.style.transform = 'translateX(' + (pct * 100) + '%)';
            }
        }

        function getChannelLimit(context) {

            return registrationServices.validateFeature('livetv').then(function () {

                var limit = browser.slow ? 100 : 500;

                context.querySelector('.guideRequiresUnlock').classList.add('hide');

                return limit;

            }, function () {

                var limit = 5;

                context.querySelector('.guideRequiresUnlock').classList.remove('hide');
                context.querySelector('.unlockText').innerHTML = globalize.translate('sharedcomponents#LiveTvGuideRequiresUnlock', limit);

                return limit;
            });
        }

        function reloadGuide(context, newStartDate) {

            var apiClient = connectionManager.currentApiClient();

            channelQuery.UserId = apiClient.getCurrentUserId();

            getChannelLimit(context).then(function (channelLimit) {

                currentChannelLimit = channelLimit;

                showLoading();

                channelQuery.StartIndex = currentStartIndex;
                channelQuery.Limit = channelLimit;
                channelQuery.AddCurrentProgram = false;
                channelQuery.EnableUserData = false;
                channelQuery.EnableImageTypes = "Primary";

                channelsPromise = channelsPromise || apiClient.getLiveTvChannels(channelQuery);

                var date = newStartDate;
                // Add one second to avoid getting programs that are just ending
                date = new Date(date.getTime() + 1000);

                // Subtract to avoid getting programs that are starting when the grid ends
                var nextDay = new Date(date.getTime() + msPerDay - 2000);

                console.log(nextDay);
                channelsPromise.then(function (channelsResult) {

                    if (channelsResult.TotalRecordCount > channelLimit) {
                        context.querySelector('.guidePaging').classList.remove('hide');

                        if (channelQuery.StartIndex) {
                            context.querySelector('.btnPreviousPage').disabled = false;
                        } else {
                            context.querySelector('.btnPreviousPage').disabled = true;
                        }

                        if ((channelQuery.StartIndex + channelLimit) < channelsResult.TotalRecordCount) {
                            context.querySelector('.btnNextPage').disabled = false;
                        } else {
                            context.querySelector('.btnNextPage').disabled = true;
                        }

                    } else {
                        context.querySelector('.guidePaging').classList.add('hide');
                    }

                    apiClient.getLiveTvPrograms({
                        UserId: apiClient.getCurrentUserId(),
                        MaxStartDate: nextDay.toISOString(),
                        MinEndDate: date.toISOString(),
                        channelIds: channelsResult.Items.map(function (c) {
                            return c.Id;
                        }).join(','),
                        ImageTypeLimit: 1,
                        EnableImages: false,
                        //EnableImageTypes: layoutManager.tv ? "Primary,Backdrop" : "Primary",
                        SortBy: "StartDate",
                        EnableTotalRecordCount: false,
                        EnableUserData: false

                    }).then(function (programsResult) {

                        renderGuide(context, date, channelsResult.Items, programsResult.Items, apiClient);

                        hideLoading();

                    });
                });
            });
        }

        function getDisplayTime(date) {

            if ((typeof date).toString().toLowerCase() === 'string') {
                try {

                    date = datetime.parseISO8601Date(date, { toLocal: true });

                } catch (err) {
                    return date;
                }
            }

            return datetime.getDisplayTime(date).toLowerCase();
        }

        function getTimeslotHeadersHtml(startDate, endDateTime) {

            var html = '';

            // clone
            startDate = new Date(startDate.getTime());

            html += '<div class="timeslotHeadersInner">';

            while (startDate.getTime() < endDateTime) {

                html += '<div class="timeslotHeader">';

                html += getDisplayTime(startDate);
                html += '</div>';

                // Add 30 mins
                startDate.setTime(startDate.getTime() + cellDurationMs);
            }

            html += '<div class="currentTimeIndicatorBar hide">';
            html += '</div>';
            html += '<div class="currentTimeIndicatorArrowContainer hide">';
            html += '<i class="currentTimeIndicatorArrow md-icon">arrow_drop_down</i>';
            html += '</div>';

            return html;
        }

        function parseDates(program) {

            if (!program.StartDateLocal) {
                try {

                    program.StartDateLocal = datetime.parseISO8601Date(program.StartDate, { toLocal: true });

                } catch (err) {

                }

            }

            if (!program.EndDateLocal) {
                try {

                    program.EndDateLocal = datetime.parseISO8601Date(program.EndDate, { toLocal: true });

                } catch (err) {

                }

            }

            return null;
        }

        function getChannelProgramsHtml(context, date, channel, programs, options) {

            var html = '';

            var startMs = date.getTime();
            var endMs = startMs + msPerDay - 1;

            programs = programs.filter(function (curr) {
                return curr.ChannelId == channel.Id;
            });

            var cssClass = layoutManager.tv ? 'channelPrograms channelPrograms-tv' : 'channelPrograms';

            html += '<div class="' + cssClass + '" data-channelid="' + channel.Id + '">';

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

                items[program.Id] = program;

                var renderStartMs = Math.max(program.StartDateLocal.getTime(), startMs);
                var startPercent = (program.StartDateLocal.getTime() - startMs) / msPerDay;
                startPercent *= 100;
                startPercent = Math.max(startPercent, 0);

                var renderEndMs = Math.min(program.EndDateLocal.getTime(), endMs);
                var endPercent = (renderEndMs - renderStartMs) / msPerDay;
                endPercent *= 100;

                var cssClass = "programCell clearButton itemAction";
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

                var timerAttributes = '';
                if (program.TimerId) {
                    timerAttributes += ' data-timerid="' + program.TimerId + '"';
                }
                if (program.SeriesTimerId) {
                    timerAttributes += ' data-seriestimerid="' + program.SeriesTimerId + '"';
                }
                html += '<button data-action="link"' + timerAttributes + ' data-isfolder="' + program.IsFolder + '" data-id="' + program.Id + '" data-serverid="' + program.ServerId + '" data-type="' + program.Type + '" class="' + cssClass + '" style="left:' + startPercent + '%;width:' + endPercent + '%;">';

                var guideProgramNameClass = "guideProgramName";

                html += '<div class="' + guideProgramNameClass + '">';

                if (program.IsLive && options.showLiveIndicator) {
                    html += '<span class="liveTvProgram">' + globalize.translate('sharedcomponents#AttributeLive') + '&nbsp;</span>';
                }
                else if (program.IsPremiere && options.showPremiereIndicator) {
                    html += '<span class="premiereTvProgram">' + globalize.translate('sharedcomponents#AttributePremiere') + '&nbsp;</span>';
                }
                else if (program.IsSeries && !program.IsRepeat && options.showNewIndicator) {
                    html += '<span class="newTvProgram">' + globalize.translate('sharedcomponents#AttributeNew') + '&nbsp;</span>';
                }

                html += program.Name;
                html += '</div>';

                if (program.IsHD && options.showHdIcon) {
                    html += '<i class="guideHdIcon md-icon programIcon">hd</i>';
                }

                if (program.SeriesTimerId) {
                    html += '<i class="seriesTimerIcon md-icon programIcon">fiber_smart_record</i>';
                }
                else if (program.TimerId) {
                    html += '<i class="timerIcon md-icon programIcon">fiber_manual_record</i>';
                }

                if (addAccent) {

                    if (program.IsKids) {
                        html += '<div class="programAccent childAccent"></div>';
                    } else if (program.IsSports) {
                        html += '<div class="programAccent sportsAccent"></div>';
                    } else if (program.IsNews) {
                        html += '<div class="programAccent newsAccent"></div>';
                    } else if (program.IsMovie) {
                        html += '<div class="programAccent movieAccent"></div>';
                    }
                }

                html += '</button>';
            }

            html += '</div>';

            return html;
        }

        function renderPrograms(context, date, channels, programs) {

            var html = [];

            // Normally we'd want to just let responsive css handle this,
            // but since mobile browsers are often underpowered, 
            // it can help performance to get them out of the markup
            var showIndicators = false;

            var options = {
                showHdIcon: showIndicators,
                showLiveIndicator: showIndicators,
                showPremiereIndicator: showIndicators,
                showNewIndicator: showIndicators
            };

            for (var i = 0, length = channels.length; i < length; i++) {

                html.push(getChannelProgramsHtml(context, date, channels[i], programs, options));
            }

            var programGrid = context.querySelector('.programGrid');
            programGrid.innerHTML = html.join('');

            programGrid.scrollTop = 0;
            programGrid.scrollLeft = 0;
        }

        function renderChannelHeaders(context, channels, apiClient) {

            var html = '';

            for (var i = 0, length = channels.length; i < length; i++) {

                var channel = channels[i];
                var hasChannelImage = channel.ImageTags.Primary;
                var dataSrc = '';
                if (hasChannelImage) {

                    var url = apiClient.getScaledImageUrl(channel.Id, {
                        maxHeight: 200,
                        tag: channel.ImageTags.Primary,
                        type: "Primary"
                    });

                    dataSrc = ' data-src="' + url + '"';
                }

                var cssClass = 'channelHeaderCell clearButton itemAction lazy';

                if (layoutManager.tv) {
                    cssClass += ' channelHeaderCell-tv';
                }

                html += '<button type="button" class="' + cssClass + '"' + dataSrc + ' data-action="link" data-isfolder="' + channel.IsFolder + '" data-id="' + channel.Id + '" data-serverid="' + channel.ServerId + '" data-type="' + channel.Type + '">';

                cssClass = 'guideChannelNumber';
                if (hasChannelImage) {
                    cssClass += ' guideChannelNumberWithImage';
                }

                html += '<div class="' + cssClass + '">' + channel.Number + '</div>';

                if (!hasChannelImage) {
                    html += '<div class="guideChannelName">' + channel.Name + '</div>';
                }

                html += '</button>';
            }

            var channelList = context.querySelector('.channelList');
            channelList.innerHTML = html;
            imageLoader.lazyChildren(channelList);
        }

        function parentWithClass(elem, className) {

            while (!elem.classList || !elem.classList.contains(className)) {
                elem = elem.parentNode;

                if (!elem) {
                    return null;
                }
            }

            return elem;
        }

        function renderGuide(context, date, channels, programs, apiClient) {

            //var list = [];
            //channels.forEach(function(i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels.forEach(function (i) {
            //    list.push(i);
            //});
            //channels = list;
            var activeElement = document.activeElement;
            var itemId = activeElement && activeElement.getAttribute ? activeElement.getAttribute('data-id') : null;
            var channelRowId = null;

            if (activeElement) {
                channelRowId = parentWithClass(activeElement, 'channelPrograms');
                channelRowId = channelRowId && channelRowId.getAttribute ? channelRowId.getAttribute('data-channelid') : null;
            }

            renderChannelHeaders(context, channels, apiClient);

            var startDate = date;
            var endDate = new Date(startDate.getTime() + msPerDay);
            context.querySelector('.timeslotHeaders').innerHTML = getTimeslotHeadersHtml(startDate, endDate);
            startCurrentTimeUpdateInterval();
            items = {};
            renderPrograms(context, date, channels, programs);

            if (layoutManager.tv) {

                var focusElem;
                if (itemId) {
                    focusElem = context.querySelector('[data-id="' + itemId + '"]')
                }

                if (focusElem) {
                    focusManager.focus(focusElem);
                } else {

                    var autoFocusParent;

                    if (channelRowId) {
                        autoFocusParent = context.querySelector('[data-channelid="' + channelRowId + '"]')
                    }

                    if (!autoFocusParent) {
                        autoFocusParent = context.querySelector('.programGrid');
                    }
                    focusManager.autoFocus(autoFocusParent, true);
                }
            }
        }

        function nativeScrollTo(container, pos, horizontal) {

            if (container.scrollTo) {
                if (horizontal) {
                    container.scrollTo(pos, 0);
                } else {
                    container.scrollTo(0, pos);
                }
            } else {
                if (horizontal) {
                    container.scrollLeft = Math.round(pos);
                } else {
                    container.scrollTop = Math.round(pos);
                }
            }
        }

        var lastGridScroll = 0;
        var lastHeaderScroll = 0;
        function onProgramGridScroll(context, elem, timeslotHeaders) {

            if ((new Date().getTime() - lastHeaderScroll) >= 1000) {
                lastGridScroll = new Date().getTime();
                nativeScrollTo(timeslotHeaders, elem.scrollLeft, true);
            }
        }

        function onTimeslotHeadersScroll(context, elem, programGrid) {

            if ((new Date().getTime() - lastGridScroll) >= 1000) {
                lastHeaderScroll = new Date().getTime();
                nativeScrollTo(programGrid, elem.scrollLeft, true);
            }
        }

        function getFutureDateText(date) {

            var weekday = [];
            weekday[0] = globalize.translate('sharedcomponents#OptionSundayShort');
            weekday[1] = globalize.translate('sharedcomponents#OptionMondayShort');
            weekday[2] = globalize.translate('sharedcomponents#OptionTuesdayShort');
            weekday[3] = globalize.translate('sharedcomponents#OptionWednesdayShort');
            weekday[4] = globalize.translate('sharedcomponents#OptionThursdayShort');
            weekday[5] = globalize.translate('sharedcomponents#OptionFridayShort');
            weekday[6] = globalize.translate('sharedcomponents#OptionSaturdayShort');

            var day = weekday[date.getDay()];
            date = datetime.toLocaleDateString(date);

            if (date.toLowerCase().indexOf(day.toLowerCase()) == -1) {
                return day + " " + date;
            }

            return date;
        }

        function changeDate(page, date) {

            clearCurrentTimeUpdateInterval();

            var newStartDate = normalizeDateToTimeslot(date);
            currentDate = newStartDate;

            reloadGuide(page, newStartDate);

            var text = getFutureDateText(date);
            text = '<span class="guideCurrentDay">' + text.replace(' ', ' </span>');
            page.querySelector('.btnSelectDate').innerHTML = text;
        }

        var dateOptions = [];

        function setDateRange(page, guideInfo) {

            var today = new Date();
            today.setHours(today.getHours(), 0, 0, 0);

            var start = datetime.parseISO8601Date(guideInfo.StartDate, { toLocal: true });
            var end = datetime.parseISO8601Date(guideInfo.EndDate, { toLocal: true });

            start.setHours(0, 0, 0, 0);
            end.setHours(0, 0, 0, 0);

            if (start.getTime() >= end.getTime()) {
                end.setDate(start.getDate() + 1);
            }

            start = new Date(Math.max(today, start));

            dateOptions = [];

            while (start <= end) {

                dateOptions.push({
                    name: getFutureDateText(start),
                    id: start.getTime()
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

        function reloadPage(page) {

            showLoading();

            var apiClient = connectionManager.currentApiClient();

            apiClient.getLiveTvGuideInfo().then(function (guideInfo) {

                setDateRange(page, guideInfo);
            });
        }

        function selectDate(page) {

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({
                    items: dateOptions,
                    title: globalize.translate('sharedcomponents#HeaderSelectDate'),
                    callback: function (id) {

                        var date = new Date();
                        date.setTime(parseInt(id));
                        changeDate(page, date);
                    }
                });

            });
        }

        function setScrollEvents(view, enabled) {

            if (layoutManager.tv) {
                require(['scrollHelper'], function (scrollHelper) {

                    var fn = enabled ? 'on' : 'off';
                    scrollHelper.centerFocus[fn](view.querySelector('.smoothScrollY'), false);
                    scrollHelper.centerFocus[fn](view.querySelector('.programGrid'), true);
                });
            }
        }

        function parentWithClass(elem, className) {

            while (!elem.classList || !elem.classList.contains(className)) {
                elem = elem.parentNode;

                if (!elem) {
                    return null;
                }
            }

            return elem;
        }

        var selectedMediaInfoTimeout;
        function onProgramGridFocus(e) {

            var programCell = parentWithClass(e.target, 'programCell');

            if (!programCell) {
                return;
            }

            var focused = e.target;
            var id = focused.getAttribute('data-id');
            var item = items[id];

            if (item) {
                events.trigger(self, 'focus', [
                {
                    item: item
                }]);
            }
        }

        function onTimerCreated(e, apiClient, data) {

            var programId = data.ProgramId;
            // This could be null, not supported by all tv providers
            var newTimerId = data.Id;

            // find guide cells by program id, ensure timer icon
            var cells = options.element.querySelectorAll('.programCell[data-id="' + programId + '"]');
            for (var i = 0, length = cells.length; i < length; i++) {
                var cell = cells[i];

                var icon = cell.querySelector('.timerIcon');
                if (!icon) {
                    cell.insertAdjacentHTML('beforeend', '<i class="timerIcon md-icon">fiber_manual_record</i>');
                }

                if (newTimerId) {
                    cell.setAttribute('data-timerid', newTimerId);
                }
            }
        }

        function onSeriesTimerCreated(e, apiClient, data) {
        }

        function onTimerCancelled(e, apiClient, data) {
            var id = data.Id;
            // find guide cells by timer id, remove timer icon
            var cells = options.element.querySelectorAll('.programCell[data-timerid="' + id + '"]');
            for (var i = 0, length = cells.length; i < length; i++) {
                var cells = cells[i];
                var icon = cell.querySelector('.timerIcon');
                if (icon) {
                    icon.parentNode.removeChild(icon);
                }
                cell.removeAttribute('data-timerid');
            }
        }

        function onSeriesTimerCancelled(e, apiClient, data) {
            var id = data.Id;
            // find guide cells by timer id, remove timer icon
            var cells = options.element.querySelectorAll('.programCell[data-seriestimerid="' + id + '"]');
            for (var i = 0, length = cells.length; i < length; i++) {
                var cells = cells[i];
                var icon = cell.querySelector('.seriesTimerIcon');
                if (icon) {
                    icon.parentNode.removeChild(icon);
                }
                cell.removeAttribute('data-seriestimerid');
            }
        }

        require(['text!./tvguide.template.html'], function (template) {
            var context = options.element;
            context.innerHTML = globalize.translateDocument(template, 'sharedcomponents');

            var programGrid = context.querySelector('.programGrid');
            var timeslotHeaders = context.querySelector('.timeslotHeaders');

            programGrid.addEventListener('focus', onProgramGridFocus, true);

            dom.addEventListener(programGrid, 'scroll', function (e) {
                onProgramGridScroll(context, this, timeslotHeaders);
            }, {
                passive: true
            });

            dom.addEventListener(timeslotHeaders, 'scroll', function () {
                onTimeslotHeadersScroll(context, this, programGrid);
            }, {
                passive: true
            });

            context.querySelector('.btnSelectDate').addEventListener('click', function () {
                selectDate(context);
            });

            context.querySelector('.btnSelectDateIcon').addEventListener('click', function () {
                selectDate(context);
            });

            context.querySelector('.btnUnlockGuide').addEventListener('click', function () {
                currentStartIndex = 0;
                channelsPromise = null;
                reloadPage(context);
            });

            context.querySelector('.btnNextPage').addEventListener('click', function () {
                currentStartIndex += currentChannelLimit;
                channelsPromise = null;
                reloadPage(context);
            });

            context.querySelector('.btnPreviousPage').addEventListener('click', function () {
                currentStartIndex = Math.max(currentStartIndex - currentChannelLimit, 0);
                channelsPromise = null;
                reloadPage(context);
            });

            context.classList.add('tvguide');

            setScrollEvents(context, true);
            itemShortcuts.on(context);

            events.trigger(self, 'load');

            events.on(serverNotifications, 'TimerCreated', onTimerCreated);
            events.on(serverNotifications, 'SeriesTimerCreated', onSeriesTimerCreated);
            events.on(serverNotifications, 'TimerCancelled', onTimerCancelled);
            events.on(serverNotifications, 'SeriesTimerCancelled', onSeriesTimerCancelled);

            self.refresh();
        });
    };

    return Guide;
});