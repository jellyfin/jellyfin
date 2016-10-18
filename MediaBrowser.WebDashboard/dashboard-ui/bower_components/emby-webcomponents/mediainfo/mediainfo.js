define(['datetime', 'globalize', 'embyRouter', 'itemHelper', 'material-icons', 'css!./mediainfo.css', 'programStyles'], function (datetime, globalize, embyRouter, itemHelper) {
    'use strict';

    function getTimerIndicator(item) {

        var status;

        if (item.Type === 'SeriesTimer') {
            return '<i class="md-icon mediaInfoItem mediaInfoIconItem mediaInfoTimerIcon">&#xE062;</i>';
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
                return '<i class="md-icon mediaInfoItem mediaInfoIconItem mediaInfoTimerIcon">&#xE062;</i>';
            }

            return '<i class="md-icon mediaInfoItem mediaInfoIconItem">&#xE062;</i>';
        }

        return '<i class="md-icon mediaInfoItem mediaInfoIconItem mediaInfoTimerIcon">&#xE061;</i>';
    }

    function getProgramInfoHtml(item, options) {
        var html = '';

        var miscInfo = [];
        var text, date;

        if (item.StartDate) {

            try {
                date = datetime.parseISO8601Date(item.StartDate);

                text = datetime.toLocaleDateString(date, { weekday: 'short', month: 'short', day: 'numeric' });

                text += ' ' + datetime.getDisplayTime(date);

                if (item.EndDate) {
                    date = datetime.parseISO8601Date(item.EndDate);
                    text += ' - ' + datetime.getDisplayTime(date);
                }

                miscInfo.push(text);
            }
            catch (e) {
                console.log("Error parsing date: " + item.PremiereDate);
            }
        }

        if (item.ChannelNumber) {
            miscInfo.push('CH ' + item.ChannelNumber);
        }

        if (item.ChannelName) {

            if (options.interactive && item.ChannelId) {
                miscInfo.push('<a class="lnkChannel" data-id="' + item.ChannelId + '" data-serverid="' + item.ServerId + '" href="#">' + item.ChannelName + '</a>');
            } else {
                miscInfo.push(item.ChannelName);
            }
        }

        if (options.timerIndicator !== false) {
            var timerHtml = getTimerIndicator(item);
            if (timerHtml) {
                miscInfo.push({
                    html: timerHtml
                });
            }
        }

        html += miscInfo.map(function (m) {
            return getMediaInfoItem(m);
        }).join('');

        return html;
    }

    function getMediaInfoHtml(item, options) {
        var html = '';

        var miscInfo = [];
        options = options || {};
        var text, date, minutes;
        var count;

        var showFolderRuntime = item.Type === "MusicAlbum" || item.MediaType === 'MusicArtist' || item.MediaType === 'Playlist' || item.MediaType === 'MusicGenre';

        if (showFolderRuntime) {

            count = item.SongCount || item.ChildCount;

            if (count) {

                miscInfo.push(globalize.translate('sharedcomponents#TrackCount', count));
            }

            if (item.RunTimeTicks) {
                miscInfo.push(datetime.getDisplayRunningTime(item.RunTimeTicks));
            }
        }

        else if (item.Type === "PhotoAlbum" || item.Type === "BoxSet") {

            count = item.ChildCount;

            if (count) {

                miscInfo.push(globalize.translate('sharedcomponents#ItemCount', count));
            }
        }

        if (item.Type === "Episode" || item.MediaType === 'Photo') {

            if (item.PremiereDate) {

                try {
                    date = datetime.parseISO8601Date(item.PremiereDate);

                    text = datetime.toLocaleDateString(date);
                    miscInfo.push(text);
                }
                catch (e) {
                    console.log("Error parsing date: " + item.PremiereDate);
                }
            }
        }

        if (item.StartDate && item.Type !== 'Program') {

            try {
                date = datetime.parseISO8601Date(item.StartDate);

                text = datetime.toLocaleDateString(date);
                miscInfo.push(text);

                if (item.Type !== "Recording") {
                    text = datetime.getDisplayTime(date);
                    miscInfo.push(text);
                }
            }
            catch (e) {
                console.log("Error parsing date: " + item.PremiereDate);
            }
        }

        if (options.year !== false && item.ProductionYear && item.Type === "Series") {

            if (item.Status === "Continuing") {
                miscInfo.push(globalize.translate('sharedcomponents#SeriesYearToPresent', item.ProductionYear));

            }
            else if (item.ProductionYear) {

                text = item.ProductionYear;

                if (item.EndDate) {

                    try {

                        var endYear = datetime.parseISO8601Date(item.EndDate).getFullYear();

                        if (endYear !== item.ProductionYear) {
                            text += "-" + datetime.parseISO8601Date(item.EndDate).getFullYear();
                        }

                    }
                    catch (e) {
                        console.log("Error parsing date: " + item.EndDate);
                    }
                }

                miscInfo.push(text);
            }
        }

        if (item.Type === 'Program') {

            if (item.IsLive) {
                miscInfo.push({
                    html: '<div class="mediaInfoProgramAttribute mediaInfoItem liveTvProgram">' + globalize.translate('sharedcomponents#Live') + '</div>'
                });
            }
            else if (item.IsPremiere) {
                miscInfo.push({
                    html: '<div class="mediaInfoProgramAttribute mediaInfoItem premiereTvProgram">' + globalize.translate('sharedcomponents#Premiere') + '</div>'
                });
            }
            else if (item.IsSeries && !item.IsRepeat) {
                miscInfo.push({
                    html: '<div class="mediaInfoProgramAttribute mediaInfoItem newTvProgram">' + globalize.translate('sharedcomponents#AttributeNew') + '</div>'
                });
            }
            else if (item.IsSeries && item.IsRepeat) {
                miscInfo.push({
                    html: '<div class="mediaInfoProgramAttribute mediaInfoItem repeatTvProgram">' + globalize.translate('sharedcomponents#Repeat') + '</div>'
                });
            }

            if (item.IsSeries && item.EpisodeTitle && options.episodeTitle !== false) {
                miscInfo.push(itemHelper.getDisplayName(item));
            }

            else if (item.PremiereDate && options.originalAirDate !== false) {

                try {
                    date = datetime.parseISO8601Date(item.PremiereDate);
                    text = globalize.translate('sharedcomponents#OriginalAirDateValue', datetime.toLocaleDateString(date));
                    miscInfo.push(text);
                }
                catch (e) {
                    console.log("Error parsing date: " + item.PremiereDate);
                }
            } else if (item.ProductionYear) {
                text = globalize.translate('sharedcomponents#ReleaseYearValue', item.ProductionYear);
                miscInfo.push(text);
            }
        }

        if (options.year !== false) {
            if (item.Type !== "Series" && item.Type !== "Episode" && item.Type !== "Person" && item.MediaType !== 'Photo' && item.Type !== 'Program') {

                if (item.ProductionYear) {

                    miscInfo.push(item.ProductionYear);
                }
                else if (item.PremiereDate) {

                    try {
                        text = datetime.parseISO8601Date(item.PremiereDate).getFullYear();
                        miscInfo.push(text);
                    }
                    catch (e) {
                        console.log("Error parsing date: " + item.PremiereDate);
                    }
                }
            }
        }

        if (item.RunTimeTicks && item.Type !== "Series" && item.Type !== 'Program' && !showFolderRuntime && options.runtime !== false) {

            if (item.Type === "Audio") {

                miscInfo.push(datetime.getDisplayRunningTime(item.RunTimeTicks));

            } else {
                minutes = item.RunTimeTicks / 600000000;

                minutes = minutes || 1;

                miscInfo.push(Math.round(minutes) + " mins");
            }
        }

        if (item.OfficialRating && item.Type !== "Season" && item.Type !== "Episode") {
            miscInfo.push({
                text: item.OfficialRating,
                cssClass: 'mediaInfoOfficialRating'
            });
        }

        if (item.Video3DFormat) {
            miscInfo.push("3D");
        }

        if (item.MediaType === 'Photo' && item.Width && item.Height) {
            miscInfo.push(item.Width + "x" + item.Height);
        }

        if (options.container !== false && item.Type === 'Audio' && item.Container) {
            miscInfo.push(item.Container);
        }

        html += miscInfo.map(function (m) {
            return getMediaInfoItem(m);
        }).join('');

        html += getStarIconsHtml(item);

        if (item.HasSubtitles && options.subtitles !== false) {
            html += '<i class="md-icon mediaInfoItem closedCaptionIcon mediaInfoIconItem">&#xE01C;</i>';
        }

        if (item.CriticRating && options.criticRating !== false) {

            if (item.CriticRating >= 60) {
                html += '<div class="mediaInfoItem mediaInfoCriticRating mediaInfoCriticRatingFresh">' + item.CriticRating + '</div>';
            } else {
                html += '<div class="mediaInfoItem mediaInfoCriticRating mediaInfoCriticRatingRotten">' + item.CriticRating + '</div>';
            }
        }

        if (options.endsAt !== false) {

            var endsAt = getEndsAt(item);
            if (endsAt) {
                html += getMediaInfoItem(endsAt, 'endsAt');
            }
        }

        return html;
    }

    function getEndsAt(item) {

        if (item.MediaType === 'Video' && item.RunTimeTicks) {

            if (!item.StartDate) {
                var endDate = new Date().getTime() + (item.RunTimeTicks / 10000);
                endDate = new Date(endDate);

                var displayTime = datetime.getDisplayTime(endDate);
                return globalize.translate('sharedcomponents#EndsAtValue', displayTime);
            }
        }

        return null;
    }

    function getEndsAtFromPosition(runtimeTicks, positionTicks, includeText) {

        var endDate = new Date().getTime() + ((runtimeTicks - (positionTicks || 0)) / 10000);
        endDate = new Date(endDate);

        var displayTime = datetime.getDisplayTime(endDate);

        if (includeText === false) {
            return displayTime;
        }
        return globalize.translate('sharedcomponents#EndsAtValue', displayTime);
    }

    function getMediaInfoItem(m, cssClass) {

        cssClass = cssClass ? (cssClass + ' mediaInfoItem') : 'mediaInfoItem';
        var mediaInfoText = m;

        if (typeof (m) !== 'string' && typeof (m) !== 'number') {

            if (m.html) {
                return m.html;
            }
            mediaInfoText = m.text;
            cssClass += ' ' + m.cssClass;
        }
        return '<div class="' + cssClass + '">' + mediaInfoText + '</div>';
    }

    function getStarIconsHtml(item) {

        var html = '';

        var rating = item.CommunityRating;

        if (rating) {
            html += '<div class="starRatingContainer mediaInfoItem">';

            html += '<i class="md-icon starIcon">&#xE838;</i>';
            html += rating;
            html += '</div>';
        }

        return html;
    }

    function dynamicEndTime(elem, item) {

        var interval = setInterval(function () {

            if (!document.body.contains(elem)) {

                clearInterval(interval);
                return;
            }

            elem.innerHTML = getEndsAt(item);

        }, 60000);
    }

    function fillPrimaryMediaInfo(elem, item, options) {
        var html = getPrimaryMediaInfoHtml(item, options);

        elem.innerHTML = html;
        afterFill(elem, item, options);
    }

    function fillSecondaryMediaInfo(elem, item, options) {
        var html = getSecondaryMediaInfoHtml(item, options);

        elem.innerHTML = html;
        afterFill(elem, item, options);
    }

    function afterFill(elem, item, options) {

        if (options.endsAt !== false) {
            var endsAtElem = elem.querySelector('.endsAt');
            if (endsAtElem) {
                dynamicEndTime(endsAtElem, item);
            }
        }

        var lnkChannel = elem.querySelector('.lnkChannel');
        if (lnkChannel) {
            lnkChannel.addEventListener('click', onChannelLinkClick);
        }
    }

    function onChannelLinkClick(e) {

        var channelId = this.getAttribute('data-id');
        var serverId = this.getAttribute('data-serverid');

        embyRouter.showItem(channelId, serverId);

        e.preventDefault();
        return false;
    }

    function getPrimaryMediaInfoHtml(item, options) {

        options = options || {};
        if (options.interactive == null) {
            options.interactive = false;
        }

        return getMediaInfoHtml(item, options);
    }

    function getSecondaryMediaInfoHtml(item, options) {

        options = options || {};
        if (options.interactive == null) {
            options.interactive = false;
        }
        if (item.Type === 'Program') {
            return getProgramInfoHtml(item, options);
        }

        return '';
    }

    return {
        getMediaInfoHtml: getPrimaryMediaInfoHtml,
        fill: fillPrimaryMediaInfo,
        getEndsAt: getEndsAt,
        getEndsAtFromPosition: getEndsAtFromPosition,
        getPrimaryMediaInfoHtml: getPrimaryMediaInfoHtml,
        getSecondaryMediaInfoHtml: getSecondaryMediaInfoHtml,
        fillPrimaryMediaInfo: fillPrimaryMediaInfo,
        fillSecondaryMediaInfo: fillSecondaryMediaInfo
    };
});