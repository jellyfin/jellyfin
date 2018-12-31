define(['require', 'inputManager', 'browser', 'globalize', 'connectionManager', 'scrollHelper', 'serverNotifications', 'loading', 'datetime', 'focusManager', 'playbackManager', 'userSettings', 'imageLoader', 'events', 'layoutManager', 'itemShortcuts', 'dom', 'css!./guide.css', 'programStyles', 'material-icons', 'scrollStyles', 'emby-button', 'paper-icon-button-light', 'emby-tabs', 'emby-scroller', 'flexStyles', 'registerElement'], function (require, inputManager, browser, globalize, connectionManager, scrollHelper, serverNotifications, loading, datetime, focusManager, playbackManager, userSettings, imageLoader, events, layoutManager, itemShortcuts, dom) {
    'use strict';

    function showViewSettings(instance) {

        require(['guide-settings-dialog'], function (guideSettingsDialog) {
            guideSettingsDialog.show(instance.categoryOptions).then(function () {
                instance.refresh();
            });
        });
    }

    function updateProgramCellOnScroll(cell, scrollPct) {

        var left = cell.posLeft;
        if (!left) {
            left = parseFloat(cell.style.left.replace('%', ''));
            cell.posLeft = left;
        }
        var width = cell.posWidth;
        if (!width) {
            width = parseFloat(cell.style.width.replace('%', ''));
            cell.posWidth = width;
        }

        var right = left + width;
        var newPct = Math.max(Math.min(scrollPct, right), left);

        var offset = newPct - left;
        var pctOfWidth = (offset / width) * 100;

        //console.log(pctOfWidth);
        var guideProgramName = cell.guideProgramName;
        if (!guideProgramName) {
            guideProgramName = cell.querySelector('.guideProgramName');
            cell.guideProgramName = guideProgramName;
        }

        var caret = cell.caret;
        if (!caret) {
            caret = cell.querySelector('.guide-programNameCaret');
            cell.caret = caret;
        }

        if (guideProgramName) {
            if (pctOfWidth > 0 && pctOfWidth <= 100) {
                //guideProgramName.style.marginLeft = pctOfWidth + '%';
                guideProgramName.style.transform = 'translateX(' + pctOfWidth + '%)';
                caret.classList.remove('hide');
            } else {
                //guideProgramName.style.marginLeft = '0';
                guideProgramName.style.transform = 'none';
                caret.classList.add('hide');
            }
        }
    }

    var isUpdatingProgramCellScroll = false;
    function updateProgramCellsOnScroll(programGrid, programCells) {

        if (isUpdatingProgramCellScroll) {
            return;
        }

        isUpdatingProgramCellScroll = true;

        requestAnimationFrame(function () {

            var scrollLeft = programGrid.scrollLeft;

            var scrollPct = scrollLeft ? (scrollLeft / programGrid.scrollWidth) * 100 : 0;

            for (var i = 0, length = programCells.length; i < length; i++) {

                updateProgramCellOnScroll(programCells[i], scrollPct);
            }

            isUpdatingProgramCellScroll = false;
        });
    }

    function onProgramGridClick(e) {

        if (!layoutManager.tv) {
            return;
        }

        var programCell = dom.parentWithClass(e.target, 'programCell');
        if (programCell) {

            var startDate = programCell.getAttribute('data-startdate');
            var endDate = programCell.getAttribute('data-enddate');
            startDate = datetime.parseISO8601Date(startDate, { toLocal: true }).getTime();
            endDate = datetime.parseISO8601Date(endDate, { toLocal: true }).getTime();

            var now = new Date().getTime();
            if (now >= startDate && now < endDate) {

                var channelId = programCell.getAttribute('data-channelid');
                var serverId = programCell.getAttribute('data-serverid');

                e.preventDefault();
                e.stopPropagation();

                playbackManager.play({
                    ids: [channelId],
                    serverId: serverId
                });
            }
        }
    }

    function Guide(options) {

        var self = this;
        var items = {};

        self.options = options;
        self.categoryOptions = { categories: [] };

        // 30 mins
        var cellCurationMinutes = 30;
        var cellDurationMs = cellCurationMinutes * 60 * 1000;
        var msPerDay = 86400000;
        var totalRendererdMs = msPerDay;

        var currentDate;
        var currentStartIndex = 0;
        var currentChannelLimit = 0;
        var autoRefreshInterval;
        var programCells;
        var lastFocusDirection;
        var programGrid;

        self.refresh = function () {

            currentDate = null;
            reloadPage(options.element);
            restartAutoRefresh();
        };

        self.pause = function () {
            stopAutoRefresh();
        };

        self.resume = function (refreshData) {
            if (refreshData) {
                self.refresh();
            } else {
                restartAutoRefresh();
            }
        };

        self.destroy = function () {

            stopAutoRefresh();

            events.off(serverNotifications, 'TimerCreated', onTimerCreated);
            events.off(serverNotifications, 'SeriesTimerCreated', onSeriesTimerCreated);
            events.off(serverNotifications, 'TimerCancelled', onTimerCancelled);
            events.off(serverNotifications, 'SeriesTimerCancelled', onSeriesTimerCancelled);

            setScrollEvents(options.element, false);
            itemShortcuts.off(options.element);
            items = {};
        };

        function restartAutoRefresh() {

            stopAutoRefresh();

            var intervalMs = 60000 * 15; // (minutes)

            autoRefreshInterval = setInterval(function () {
                self.refresh();
            }, intervalMs);
        }

        function stopAutoRefresh() {
            if (autoRefreshInterval) {
                clearInterval(autoRefreshInterval);
                autoRefreshInterval = null;
            }
        }

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

        function reloadGuide(context, newStartDate, scrollToTimeMs, focusToTimeMs, startTimeOfDayMs, focusProgramOnRender) {

            var apiClient = connectionManager.getApiClient(options.serverId);

            var channelQuery = {

                StartIndex: 0,
                EnableFavoriteSorting: userSettings.get('livetv-favoritechannelsattop') !== 'false'
            };

            channelQuery.UserId = apiClient.getCurrentUserId();

            var channelLimit = 500;
            currentChannelLimit = channelLimit;

            showLoading();

            channelQuery.StartIndex = currentStartIndex;
            channelQuery.Limit = channelLimit;
            channelQuery.AddCurrentProgram = false;
            channelQuery.EnableUserData = false;
            channelQuery.EnableImageTypes = "Primary";

            var categories = self.categoryOptions.categories || [];
            var displayMovieContent = !categories.length || categories.indexOf('movies') !== -1;
            var displaySportsContent = !categories.length || categories.indexOf('sports') !== -1;
            var displayNewsContent = !categories.length || categories.indexOf('news') !== -1;
            var displayKidsContent = !categories.length || categories.indexOf('kids') !== -1;
            var displaySeriesContent = !categories.length || categories.indexOf('series') !== -1;

            if (displayMovieContent && displaySportsContent && displayNewsContent && displayKidsContent) {
                channelQuery.IsMovie = null;
                channelQuery.IsSports = null;
                channelQuery.IsKids = null;
                channelQuery.IsNews = null;
                channelQuery.IsSeries = null;
            } else {
                if (displayNewsContent) {
                    channelQuery.IsNews = true;
                }
                if (displaySportsContent) {
                    channelQuery.IsSports = true;
                }
                if (displayKidsContent) {
                    channelQuery.IsKids = true;
                }
                if (displayMovieContent) {
                    channelQuery.IsMovie = true;
                }
                if (displaySeriesContent) {
                    channelQuery.IsSeries = true;
                }
            }

            if (userSettings.get('livetv-channelorder') === 'DatePlayed') {
                channelQuery.SortBy = "DatePlayed";
                channelQuery.SortOrder = "Descending";
            } else {
                channelQuery.SortBy = null;
                channelQuery.SortOrder = null;
            }

            var date = newStartDate;
            // Add one second to avoid getting programs that are just ending
            date = new Date(date.getTime() + 1000);

            // Subtract to avoid getting programs that are starting when the grid ends
            var nextDay = new Date(date.getTime() + msPerDay - 2000);

            // Normally we'd want to just let responsive css handle this,
            // but since mobile browsers are often underpowered, 
            // it can help performance to get them out of the markup
            var allowIndicators = dom.getWindowSize().innerWidth >= 600;

            var renderOptions = {
                showHdIcon: allowIndicators && userSettings.get('guide-indicator-hd') === 'true',
                showLiveIndicator: allowIndicators && userSettings.get('guide-indicator-live') !== 'false',
                showPremiereIndicator: allowIndicators && userSettings.get('guide-indicator-premiere') !== 'false',
                showNewIndicator: allowIndicators && userSettings.get('guide-indicator-new') !== 'false',
                showRepeatIndicator: allowIndicators && userSettings.get('guide-indicator-repeat') === 'true',
                showEpisodeTitle: layoutManager.tv ? false : true
            };

            apiClient.getLiveTvChannels(channelQuery).then(function (channelsResult) {

                var btnPreviousPage = context.querySelector('.btnPreviousPage');
                var btnNextPage = context.querySelector('.btnNextPage');

                if (channelsResult.TotalRecordCount > channelLimit) {

                    context.querySelector('.guideOptions').classList.remove('hide');

                    btnPreviousPage.classList.remove('hide');
                    btnNextPage.classList.remove('hide');

                    if (channelQuery.StartIndex) {
                        context.querySelector('.btnPreviousPage').disabled = false;
                    } else {
                        context.querySelector('.btnPreviousPage').disabled = true;
                    }

                    if ((channelQuery.StartIndex + channelLimit) < channelsResult.TotalRecordCount) {
                        btnNextPage.disabled = false;
                    } else {
                        btnNextPage.disabled = true;
                    }

                } else {
                    context.querySelector('.guideOptions').classList.add('hide');
                }

                var programFields = [];

                var programQuery = {
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
                };

                if (renderOptions.showHdIcon) {
                    programFields.push('IsHD');
                }

                if (programFields.length) {
                    programQuery.Fields = programFields.join('');
                }

                apiClient.getLiveTvPrograms(programQuery).then(function (programsResult) {

                    renderGuide(context, date, channelsResult.Items, programsResult.Items, renderOptions, apiClient, scrollToTimeMs, focusToTimeMs, startTimeOfDayMs, focusProgramOnRender);

                    hideLoading();

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

        function getTimerIndicator(item) {

            var status;

            if (item.Type === 'SeriesTimer') {
                return '<i class="md-icon programIcon seriesTimerIcon">&#xE062;</i>';
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
                    return '<i class="md-icon programIcon seriesTimerIcon">&#xE062;</i>';
                }

                return '<i class="md-icon programIcon seriesTimerIcon seriesTimerIcon-inactive">&#xE062;</i>';
            }

            return '<i class="md-icon programIcon timerIcon">&#xE061;</i>';
        }

        function getChannelProgramsHtml(context, date, channel, programs, options, listInfo) {

            var html = '';

            var startMs = date.getTime();
            var endMs = startMs + msPerDay - 1;

            var outerCssClass = layoutManager.tv ? 'channelPrograms channelPrograms-tv' : 'channelPrograms';

            html += '<div class="' + outerCssClass + '" data-channelid="' + channel.Id + '">';

            var clickAction = layoutManager.tv ? 'link' : 'programdialog';

            var categories = self.categoryOptions.categories || [];
            var displayMovieContent = !categories.length || categories.indexOf('movies') !== -1;
            var displaySportsContent = !categories.length || categories.indexOf('sports') !== -1;
            var displayNewsContent = !categories.length || categories.indexOf('news') !== -1;
            var displayKidsContent = !categories.length || categories.indexOf('kids') !== -1;
            var displaySeriesContent = !categories.length || categories.indexOf('series') !== -1;
            var enableColorCodedBackgrounds = userSettings.get('guide-colorcodedbackgrounds') === 'true';

            var programsFound;
            var now = new Date().getTime();

            for (var i = listInfo.startIndex, length = programs.length; i < length; i++) {

                var program = programs[i];

                if (program.ChannelId !== channel.Id) {

                    if (programsFound) {
                        break;
                    }

                    continue;
                }

                programsFound = true;
                listInfo.startIndex++;

                parseDates(program);

                var startDateLocalMs = program.StartDateLocal.getTime();
                var endDateLocalMs = program.EndDateLocal.getTime();

                if (endDateLocalMs < startMs) {
                    continue;
                }

                if (startDateLocalMs > endMs) {
                    break;
                }

                items[program.Id] = program;

                var renderStartMs = Math.max(startDateLocalMs, startMs);
                var startPercent = (startDateLocalMs - startMs) / msPerDay;
                startPercent *= 100;
                startPercent = Math.max(startPercent, 0);

                var renderEndMs = Math.min(endDateLocalMs, endMs);
                var endPercent = (renderEndMs - renderStartMs) / msPerDay;
                endPercent *= 100;

                var cssClass = "programCell itemAction";
                var accentCssClass = null;
                var displayInnerContent = true;

                if (program.IsKids) {
                    displayInnerContent = displayKidsContent;
                    accentCssClass = 'kids';
                } else if (program.IsSports) {
                    displayInnerContent = displaySportsContent;
                    accentCssClass = 'sports';
                } else if (program.IsNews) {
                    displayInnerContent = displayNewsContent;
                    accentCssClass = 'news';
                } else if (program.IsMovie) {
                    displayInnerContent = displayMovieContent;
                    accentCssClass = 'movie';
                }
                else if (program.IsSeries) {
                    displayInnerContent = displaySeriesContent;
                }
                else {
                    displayInnerContent = displayMovieContent && displayNewsContent && displaySportsContent && displayKidsContent && displaySeriesContent;
                }

                if (displayInnerContent && enableColorCodedBackgrounds && accentCssClass) {
                    cssClass += " programCell-" + accentCssClass;
                }

                if (now >= startDateLocalMs && now < endDateLocalMs) {
                    cssClass += " programCell-active";
                }

                var timerAttributes = '';
                if (program.TimerId) {
                    timerAttributes += ' data-timerid="' + program.TimerId + '"';
                }
                if (program.SeriesTimerId) {
                    timerAttributes += ' data-seriestimerid="' + program.SeriesTimerId + '"';
                }

                var isAttribute = endPercent >= 2 ? ' is="emby-programcell"' : '';

                html += '<button' + isAttribute + ' data-action="' + clickAction + '"' + timerAttributes + ' data-channelid="' + program.ChannelId + '" data-id="' + program.Id + '" data-serverid="' + program.ServerId + '" data-startdate="' + program.StartDate + '" data-enddate="' + program.EndDate + '" data-type="' + program.Type + '" class="' + cssClass + '" style="left:' + startPercent + '%;width:' + endPercent + '%;">';

                if (displayInnerContent) {
                    var guideProgramNameClass = "guideProgramName";

                    html += '<div class="' + guideProgramNameClass + '">';

                    html += '<div class="guide-programNameCaret hide"><i class="guideProgramNameCaretIcon md-icon">&#xE314;</i></div>';

                    html += '<div class="guideProgramNameText">' + program.Name;

                    var indicatorHtml = null;
                    if (program.IsLive && options.showLiveIndicator) {
                        indicatorHtml = '<span class="liveTvProgram guideProgramIndicator">' + globalize.translate('sharedcomponents#Live') + '</span>';
                    }
                    else if (program.IsPremiere && options.showPremiereIndicator) {
                        indicatorHtml = '<span class="premiereTvProgram guideProgramIndicator">' + globalize.translate('sharedcomponents#Premiere') + '</span>';
                    }
                    else if (program.IsSeries && !program.IsRepeat && options.showNewIndicator) {
                        indicatorHtml = '<span class="newTvProgram guideProgramIndicator">' + globalize.translate('sharedcomponents#AttributeNew') + '</span>';
                    }
                    else if (program.IsSeries && program.IsRepeat && options.showRepeatIndicator) {
                        indicatorHtml = '<span class="repeatTvProgram guideProgramIndicator">' + globalize.translate('sharedcomponents#Repeat') + '</span>';
                    }
                    html += indicatorHtml || '';

                    if ((program.EpisodeTitle && options.showEpisodeTitle)) {
                        html += '<div class="guideProgramSecondaryInfo">';

                        if (program.EpisodeTitle && options.showEpisodeTitle) {
                            html += '<span class="programSecondaryTitle">' + program.EpisodeTitle + '</span>';
                        }
                        html += '</div>';
                    }

                    html += '</div>';

                    if (program.IsHD && options.showHdIcon) {
                        //html += '<i class="guideHdIcon md-icon programIcon">hd</i>';
                        if (layoutManager.tv) {
                            html += '<div class="programIcon guide-programTextIcon guide-programTextIcon-tv">HD</div>';
                        } else {
                            html += '<div class="programIcon guide-programTextIcon">HD</div>';
                        }
                    }

                    html += getTimerIndicator(program);

                    html += '</div>';
                }

                html += '</button>';
            }

            html += '</div>';

            return html;
        }


        function renderChannelHeaders(context, channels, apiClient) {

            var html = '';

            for (var i = 0, length = channels.length; i < length; i++) {

                var channel = channels[i];
                var hasChannelImage = channel.ImageTags.Primary;

                var cssClass = 'guide-channelHeaderCell itemAction';

                if (layoutManager.tv) {
                    cssClass += ' guide-channelHeaderCell-tv';
                }

                var title = [];
                if (channel.ChannelNumber) {

                    title.push(channel.ChannelNumber);
                }
                if (channel.Name) {

                    title.push(channel.Name);
                }

                html += '<button title="' + title.join(' ') + '" type="button" class="' + cssClass + '"' + ' data-action="link" data-isfolder="' + channel.IsFolder + '" data-id="' + channel.Id + '" data-serverid="' + channel.ServerId + '" data-type="' + channel.Type + '">';

                if (hasChannelImage) {

                    var url = apiClient.getScaledImageUrl(channel.Id, {
                        maxHeight: 220,
                        tag: channel.ImageTags.Primary,
                        type: "Primary"
                    });

                    html += '<div class="guideChannelImage lazy" data-src="' + url + '"></div>';
                }

                if (channel.ChannelNumber) {

                    html += '<h3 class="guideChannelNumber">' + channel.ChannelNumber + '</h3>';
                }

                if (!hasChannelImage && channel.Name) {
                    html += '<div class="guideChannelName">' + channel.Name + '</div>';
                }

                html += '</button>';
            }

            var channelList = context.querySelector('.channelsContainer');
            channelList.innerHTML = html;
            imageLoader.lazyChildren(channelList);
        }

        function renderPrograms(context, date, channels, programs, options) {

            var listInfo = {
                startIndex: 0
            };

            var html = [];

            for (var i = 0, length = channels.length; i < length; i++) {

                html.push(getChannelProgramsHtml(context, date, channels[i], programs, options, listInfo));
            }

            programGrid.innerHTML = html.join('');

            programCells = programGrid.querySelectorAll('[is=emby-programcell]');

            updateProgramCellsOnScroll(programGrid, programCells);
        }

        function getProgramSortOrder(program, channels) {

            var channelId = program.ChannelId;
            var channelIndex = -1;

            for (var i = 0, length = channels.length; i < length; i++) {
                if (channelId === channels[i].Id) {
                    channelIndex = i;
                    break;
                }
            }

            var start = datetime.parseISO8601Date(program.StartDate, { toLocal: true });

            return (channelIndex * 10000000) + (start.getTime() / 60000);
        }

        function renderGuide(context, date, channels, programs, renderOptions, apiClient, scrollToTimeMs, focusToTimeMs, startTimeOfDayMs, focusProgramOnRender) {

            programs.sort(function (a, b) {
                return getProgramSortOrder(a, channels) - getProgramSortOrder(b, channels);
            });

            var activeElement = document.activeElement;
            var itemId = activeElement && activeElement.getAttribute ? activeElement.getAttribute('data-id') : null;
            var channelRowId = null;

            if (activeElement) {
                channelRowId = dom.parentWithClass(activeElement, 'channelPrograms');
                channelRowId = channelRowId && channelRowId.getAttribute ? channelRowId.getAttribute('data-channelid') : null;
            }

            renderChannelHeaders(context, channels, apiClient);

            var startDate = date;
            var endDate = new Date(startDate.getTime() + msPerDay);
            context.querySelector('.timeslotHeaders').innerHTML = getTimeslotHeadersHtml(startDate, endDate);
            items = {};
            renderPrograms(context, date, channels, programs, renderOptions);

            if (focusProgramOnRender) {
                focusProgram(context, itemId, channelRowId, focusToTimeMs, startTimeOfDayMs);
            }

            scrollProgramGridToTimeMs(context, scrollToTimeMs, startTimeOfDayMs);
        }

        function scrollProgramGridToTimeMs(context, scrollToTimeMs, startTimeOfDayMs) {

            scrollToTimeMs -= startTimeOfDayMs;

            var pct = scrollToTimeMs / msPerDay;

            programGrid.scrollTop = 0;

            var scrollPos = pct * programGrid.scrollWidth;

            nativeScrollTo(programGrid, scrollPos, true);
        }

        function focusProgram(context, itemId, channelRowId, focusToTimeMs, startTimeOfDayMs) {

            var focusElem;
            if (itemId) {
                focusElem = context.querySelector('[data-id="' + itemId + '"]');
            }

            if (focusElem) {
                focusManager.focus(focusElem);
            } else {

                var autoFocusParent;

                if (channelRowId) {
                    autoFocusParent = context.querySelector('[data-channelid="' + channelRowId + '"]');
                }

                if (!autoFocusParent) {
                    autoFocusParent = programGrid;
                }

                focusToTimeMs -= startTimeOfDayMs;

                var pct = (focusToTimeMs / msPerDay) * 100;

                var programCell = autoFocusParent.querySelector('.programCell');

                while (programCell) {

                    var left = (programCell.style.left || '').replace('%', '');
                    left = left ? parseFloat(left) : 0;
                    var width = (programCell.style.width || '').replace('%', '');
                    width = width ? parseFloat(width) : 0;

                    if (left >= pct || (left + width) >= pct) {
                        break;
                    }
                    programCell = programCell.nextSibling;
                }

                if (programCell) {
                    focusManager.focus(programCell);
                } else {
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
        var scrollXPct = 0;
        function onProgramGridScroll(context, elem, timeslotHeaders) {

            if ((new Date().getTime() - lastHeaderScroll) >= 1000) {
                lastGridScroll = new Date().getTime();

                var scrollLeft = elem.scrollLeft;
                scrollXPct = (scrollLeft * 100) / elem.scrollWidth;
                nativeScrollTo(timeslotHeaders, scrollLeft, true);
            }

            updateProgramCellsOnScroll(elem, programCells);
        }

        function onTimeslotHeadersScroll(context, elem) {

            if ((new Date().getTime() - lastGridScroll) >= 1000) {
                lastHeaderScroll = new Date().getTime();
                nativeScrollTo(programGrid, elem.scrollLeft, true);
            }
        }

        function changeDate(page, date, scrollToTimeMs, focusToTimeMs, startTimeOfDayMs, focusProgramOnRender) {

            var newStartDate = normalizeDateToTimeslot(date);
            currentDate = newStartDate;

            reloadGuide(page, newStartDate, scrollToTimeMs, focusToTimeMs, startTimeOfDayMs, focusProgramOnRender);
        }

        function getDateTabText(date, isActive, tabIndex) {

            var cssClass = isActive ? 'emby-tab-button guide-date-tab-button emby-tab-button-active' : 'emby-tab-button guide-date-tab-button';

            var html = '<button is="emby-button" class="' + cssClass + '" data-index="' + tabIndex + '" data-date="' + date.getTime() + '">';
            var tabText = datetime.toLocaleDateString(date, { weekday: 'short' });

            tabText += '<br/>';
            tabText += date.getDate();
            html += '<div class="emby-button-foreground">' + tabText + '</div>';
            html += '</button>';

            return html;
        }

        function setDateRange(page, guideInfo) {

            var today = new Date();
            var nowHours = today.getHours();
            today.setHours(nowHours, 0, 0, 0);

            var start = datetime.parseISO8601Date(guideInfo.StartDate, { toLocal: true });
            var end = datetime.parseISO8601Date(guideInfo.EndDate, { toLocal: true });

            start.setHours(nowHours, 0, 0, 0);
            end.setHours(0, 0, 0, 0);

            if (start.getTime() >= end.getTime()) {
                end.setDate(start.getDate() + 1);
            }

            start = new Date(Math.max(today, start));

            var dateTabsHtml = '';
            var tabIndex = 0;

            var date = new Date();

            if (currentDate) {
                date.setTime(currentDate.getTime());
            }

            date.setHours(nowHours, 0, 0, 0);
            //start.setHours(0, 0, 0, 0);

            var startTimeOfDayMs = (start.getHours() * 60 * 60 * 1000);
            startTimeOfDayMs += start.getMinutes() * 60 * 1000;

            while (start <= end) {

                var isActive = date.getDate() === start.getDate() && date.getMonth() === start.getMonth() && date.getFullYear() === start.getFullYear();

                dateTabsHtml += getDateTabText(start, isActive, tabIndex);

                start.setDate(start.getDate() + 1);
                start.setHours(0, 0, 0, 0);
                tabIndex++;
            }

            page.querySelector('.emby-tabs-slider').innerHTML = dateTabsHtml;
            page.querySelector('.guideDateTabs').refresh();

            var newDate = new Date();
            var newDateHours = newDate.getHours();
            var scrollToTimeMs = newDateHours * 60 * 60 * 1000;

            var minutes = newDate.getMinutes();
            if (minutes >= 30) {
                scrollToTimeMs += 30 * 60 * 1000;
            }

            var focusToTimeMs = ((newDateHours * 60) + minutes) * 60 * 1000;
            changeDate(page, date, scrollToTimeMs, focusToTimeMs, startTimeOfDayMs, layoutManager.tv);
        }

        function reloadPage(page) {

            showLoading();

            var apiClient = connectionManager.getApiClient(options.serverId);

            apiClient.getLiveTvGuideInfo().then(function (guideInfo) {

                setDateRange(page, guideInfo);
            });
        }

        function getChannelProgramsFocusableElements(container) {

            var elements = container.querySelectorAll('.programCell');

            var list = [];
            // add 1 to avoid programs that are out of view to the left
            var currentScrollXPct = scrollXPct + 1;

            for (var i = 0, length = elements.length; i < length; i++) {

                var elem = elements[i];

                var left = (elem.style.left || '').replace('%', '');
                left = left ? parseFloat(left) : 0;
                var width = (elem.style.width || '').replace('%', '');
                width = width ? parseFloat(width) : 0;

                if ((left + width) >= currentScrollXPct) {
                    list.push(elem);
                }
            }

            return list;
        }

        function onInputCommand(e) {

            var target = e.target;
            var programCell = dom.parentWithClass(target, 'programCell');
            var container;
            var channelPrograms;
            var focusableElements;
            var newRow;

            var scrollX = false;

            switch (e.detail.command) {

                case 'up':
                    if (programCell) {
                        container = programGrid;
                        channelPrograms = dom.parentWithClass(programCell, 'channelPrograms');

                        newRow = channelPrograms.previousSibling;
                        if (newRow) {
                            focusableElements = getChannelProgramsFocusableElements(newRow);
                            if (focusableElements.length) {
                                container = newRow;
                            } else {
                                focusableElements = null;
                            }
                        } else {
                            container = null;
                        }
                    } else {
                        container = null;
                    }
                    lastFocusDirection = e.detail.command;

                    focusManager.moveUp(target, {
                        container: container,
                        focusableElements: focusableElements
                    });
                    break;
                case 'down':
                    if (programCell) {
                        container = programGrid;
                        channelPrograms = dom.parentWithClass(programCell, 'channelPrograms');

                        newRow = channelPrograms.nextSibling;
                        if (newRow) {
                            focusableElements = getChannelProgramsFocusableElements(newRow);
                            if (focusableElements.length) {
                                container = newRow;
                            } else {
                                focusableElements = null;
                            }
                        } else {
                            container = null;
                        }
                    } else {
                        container = null;
                    }
                    lastFocusDirection = e.detail.command;

                    focusManager.moveDown(target, {
                        container: container,
                        focusableElements: focusableElements
                    });
                    break;
                case 'left':
                    container = programCell ? dom.parentWithClass(programCell, 'channelPrograms') : null;
                    // allow left outside the channelProgramsContainer when the first child is currently focused
                    if (container && !programCell.previousSibling) {
                        container = null;
                    }
                    lastFocusDirection = e.detail.command;

                    focusManager.moveLeft(target, {
                        container: container
                    });
                    scrollX = true;
                    break;
                case 'right':
                    container = programCell ? dom.parentWithClass(programCell, 'channelPrograms') : null;
                    lastFocusDirection = e.detail.command;

                    focusManager.moveRight(target, {
                        container: container
                    });
                    scrollX = true;
                    break;
                default:
                    return;
            }

            e.preventDefault();
            e.stopPropagation();
        }

        function onScrollerFocus(e) {

            var target = e.target;
            var programCell = dom.parentWithClass(target, 'programCell');

            if (programCell) {
                var focused = target;

                var id = focused.getAttribute('data-id');
                var item = items[id];

                if (item) {
                    events.trigger(self, 'focus', [
                        {
                            item: item
                        }]);
                }
            }

            if (lastFocusDirection === 'left') {

                if (programCell) {

                    scrollHelper.toStart(programGrid, programCell, true, true);
                }
            }

            else if (lastFocusDirection === 'right') {

                if (programCell) {

                    scrollHelper.toCenter(programGrid, programCell, true, true);
                }
            }

            else if (lastFocusDirection === 'up' || lastFocusDirection === 'down') {

                var verticalScroller = dom.parentWithClass(target, 'guideVerticalScroller');
                if (verticalScroller) {

                    var focusedElement = programCell || dom.parentWithTag(target, 'BUTTON');
                    verticalScroller.toCenter(focusedElement, true);
                }
            }
        }

        function setScrollEvents(view, enabled) {

            if (layoutManager.tv) {
                var guideVerticalScroller = view.querySelector('.guideVerticalScroller');

                if (enabled) {
                    inputManager.on(guideVerticalScroller, onInputCommand);
                } else {
                    inputManager.off(guideVerticalScroller, onInputCommand);
                }
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
                    cell.querySelector('.guideProgramName').insertAdjacentHTML('beforeend', '<i class="timerIcon md-icon programIcon">&#xE061;</i>');
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
                var cell = cells[i];
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
                var cell = cells[i];
                var icon = cell.querySelector('.seriesTimerIcon');
                if (icon) {
                    icon.parentNode.removeChild(icon);
                }
                cell.removeAttribute('data-seriestimerid');
            }
        }

        require(['text!./tvguide.template.html'], function (template) {

            var context = options.element;

            context.classList.add('tvguide');

            context.innerHTML = globalize.translateDocument(template, 'sharedcomponents');

            programGrid = context.querySelector('.programGrid');
            var timeslotHeaders = context.querySelector('.timeslotHeaders');

            if (layoutManager.tv) {
                dom.addEventListener(context.querySelector('.guideVerticalScroller'), 'focus', onScrollerFocus, {
                    capture: true,
                    passive: true
                });
            } else if (layoutManager.desktop) {
                timeslotHeaders.classList.add('timeslotHeaders-desktop');
            }

            if (browser.iOS || browser.osx) {
                context.querySelector('.channelsContainer').classList.add('noRubberBanding');

                programGrid.classList.add('noRubberBanding');
            }

            dom.addEventListener(programGrid, 'scroll', function (e) {
                onProgramGridScroll(context, this, timeslotHeaders);
            }, {
                    passive: true
                });

            dom.addEventListener(timeslotHeaders, 'scroll', function () {
                onTimeslotHeadersScroll(context, this);
            }, {
                    passive: true
                });

            programGrid.addEventListener('click', onProgramGridClick);

            context.querySelector('.btnNextPage').addEventListener('click', function () {
                currentStartIndex += currentChannelLimit;
                reloadPage(context);
                restartAutoRefresh();
            });

            context.querySelector('.btnPreviousPage').addEventListener('click', function () {
                currentStartIndex = Math.max(currentStartIndex - currentChannelLimit, 0);
                reloadPage(context);
                restartAutoRefresh();
            });

            context.querySelector('.btnGuideViewSettings').addEventListener('click', function () {
                showViewSettings(self);
                restartAutoRefresh();
            });

            context.querySelector('.guideDateTabs').addEventListener('tabchange', function (e) {

                var allTabButtons = e.target.querySelectorAll('.guide-date-tab-button');

                var tabButton = allTabButtons[parseInt(e.detail.selectedTabIndex)];
                if (tabButton) {

                    var previousButton = e.detail.previousIndex == null ? null : allTabButtons[parseInt(e.detail.previousIndex)];

                    var date = new Date();
                    date.setTime(parseInt(tabButton.getAttribute('data-date')));

                    var scrollWidth = programGrid.scrollWidth;
                    var scrollToTimeMs;
                    if (scrollWidth) {
                        scrollToTimeMs = (programGrid.scrollLeft / scrollWidth) * msPerDay;
                    } else {
                        scrollToTimeMs = 0;
                    }

                    if (previousButton) {

                        var previousDate = new Date();
                        previousDate.setTime(parseInt(previousButton.getAttribute('data-date')));

                        scrollToTimeMs += (previousDate.getHours() * 60 * 60 * 1000);
                        scrollToTimeMs += (previousDate.getMinutes() * 60 * 1000);
                    }

                    var startTimeOfDayMs = (date.getHours() * 60 * 60 * 1000);
                    startTimeOfDayMs += (date.getMinutes() * 60 * 1000);

                    changeDate(context, date, scrollToTimeMs, scrollToTimeMs, startTimeOfDayMs, false);
                }
            });

            setScrollEvents(context, true);
            itemShortcuts.on(context);

            events.trigger(self, 'load');

            events.on(serverNotifications, 'TimerCreated', onTimerCreated);
            events.on(serverNotifications, 'SeriesTimerCreated', onSeriesTimerCreated);
            events.on(serverNotifications, 'TimerCancelled', onTimerCancelled);
            events.on(serverNotifications, 'SeriesTimerCancelled', onSeriesTimerCancelled);

            self.refresh();
        });
    }

    var ProgramCellPrototype = Object.create(HTMLButtonElement.prototype);

    ProgramCellPrototype.detachedCallback = function () {
        this.posLeft = null;
        this.posWidth = null;
        this.guideProgramName = null;
    };

    document.registerElement('emby-programcell', {
        prototype: ProgramCellPrototype,
        extends: 'button'
    });

    return Guide;
});