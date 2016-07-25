define(['datetime', 'globalize', 'embyRouter', 'material-icons', 'css!./mediainfo.css'], function (datetime, globalize, embyRouter) {

    function getProgramInfoHtml(item, options) {
        var html = '';

        var miscInfo = [];
        var text, date;

        if (item.ChannelName) {

            if (options.interactive && item.ChannelId) {
                miscInfo.push('<a class="lnkChannel" data-id="' + item.ChannelId + '" data-serverid="' + item.ServerId + '" href="#">' + item.ChannelName + '</a>');
            } else {
                miscInfo.push(item.ChannelName);
            }
        }

        if (item.StartDate) {

            try {
                date = datetime.parseISO8601Date(item.StartDate);

                text = date.toLocaleDateString();

                text += ', ' + datetime.getDisplayTime(date);

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

        if (item.SeriesTimerId) {
            miscInfo.push({
                html: '<i class="md-icon mediaInfoItem timerIcon">&#xE062;</i>'
            });
        }
        else if (item.TimerId) {
            miscInfo.push({
                html: '<i class="md-icon mediaInfoItem timerIcon">&#xE061;</i>'
            });
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

        var showFolderRuntime = item.Type == "MusicAlbum" || item.MediaType == 'MusicArtist' || item.MediaType == 'Playlist' || item.MediaType == 'MusicGenre';

        if (showFolderRuntime) {

            var count = item.SongCount || item.ChildCount;

            if (count) {

                miscInfo.push(globalize.translate('sharedcomponents#TrackCount', count));
            }

            if (item.RunTimeTicks) {
                miscInfo.push(datetime.getDisplayRunningTime(item.RunTimeTicks));
            }
        }

        else if (item.Type == "PhotoAlbum" || item.Type == "BoxSet") {

            var count = item.ChildCount;

            if (count) {

                miscInfo.push(globalize.translate('sharedcomponents#ItemCount', count));
            }
        }

        if (item.Type == "Episode" || item.MediaType == 'Photo') {

            if (item.PremiereDate) {

                try {
                    date = datetime.parseISO8601Date(item.PremiereDate);

                    text = date.toLocaleDateString();
                    miscInfo.push(text);
                }
                catch (e) {
                    console.log("Error parsing date: " + item.PremiereDate);
                }
            }
        }

        if (item.StartDate && item.Type != 'Program') {

            try {
                date = datetime.parseISO8601Date(item.StartDate);

                text = date.toLocaleDateString();
                miscInfo.push(text);

                if (item.Type != "Recording") {
                    text = datetime.getDisplayTime(date);
                    miscInfo.push(text);
                }
            }
            catch (e) {
                console.log("Error parsing date: " + item.PremiereDate);
            }
        }

        if (options.year !== false && item.ProductionYear && item.Type == "Series") {

            if (item.Status == "Continuing") {
                miscInfo.push(globalize.translate('sharedcomponents#ValueSeriesYearToPresent', item.ProductionYear));

            }
            else if (item.ProductionYear) {

                text = item.ProductionYear;

                if (item.EndDate) {

                    try {

                        var endYear = datetime.parseISO8601Date(item.EndDate).getFullYear();

                        if (endYear != item.ProductionYear) {
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

        if (item.Type == 'Program') {

            if (item.IsLive) {
                miscInfo.push({
                    html: '<div class="mediaInfoProgramAttribute mediaInfoItem">' + globalize.translate('sharedcomponents#AttributeLive') + '</div>'
                });
            }
            else if (item.IsPremiere) {
                miscInfo.push({
                    html: '<div class="mediaInfoProgramAttribute mediaInfoItem">' + globalize.translate('sharedcomponents#AttributePremiere') + '</div>'
                });
            }
            else if (item.IsSeries && !item.IsRepeat) {
                miscInfo.push({
                    html: '<div class="mediaInfoProgramAttribute mediaInfoItem">' + globalize.translate('sharedcomponents#AttributeNew') + '</div>'
                });
            }

            if (item.PremiereDate) {

                try {
                    date = datetime.parseISO8601Date(item.PremiereDate);
                    text = globalize.translate('sharedcomponents#OriginalAirDateValue', date.toLocaleDateString());
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

        if (item.Type != "Series" && item.Type != "Episode" && item.Type != "Person" && item.MediaType != 'Photo' && item.Type != 'Program') {

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

        if (item.RunTimeTicks && item.Type != "Series" && item.Type != 'Program' && !showFolderRuntime && options.runtime !== false) {

            if (item.Type == "Audio") {

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

        if (item.MediaType == 'Photo' && item.Width && item.Height) {
            miscInfo.push(item.Width + "x" + item.Height);
        }

        if (options.container !== false && item.Type == 'Audio' && item.Container) {
            miscInfo.push(item.Container);
        }

        html += miscInfo.map(function (m) {
            return getMediaInfoItem(m);
        }).join('');

        html += getStarIconsHtml(item);

        if (item.HasSubtitles && options.subtitles !== false) {
            html += '<i class="md-icon mediaInfoItem closedCaptionIcon">&#xE01C;</i>';
        }

        if (item.CriticRating && options.criticRating !== false) {

            if (item.CriticRating >= 60) {
                html += '<div class="mediaInfoItem criticRating criticRatingFresh">' + item.CriticRating + '</div>';
            } else {
                html += '<div class="mediaInfoItem criticRating criticRatingRotten">' + item.CriticRating + '</div>';
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

        if (item.MediaType == 'Video' && item.RunTimeTicks) {

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

            html += '<i class="md-icon">&#xE838;</i>';
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

    function getDisplayName(item, options) {

        if (!item) {
            throw new Error("null item passed into getDisplayName");
        }

        options = options || {};

        var name = item.EpisodeTitle || item.Name || '';

        if (item.Type == "TvChannel") {

            if (item.Number) {
                return item.Number + ' ' + name;
            }
            return name;
        }
        if (options.isInlineSpecial && item.Type == "Episode" && item.ParentIndexNumber == 0) {

            name = globalize.translate('sharedcomponents#ValueSpecialEpisodeName', name);

        } else if (item.Type == "Episode" && item.IndexNumber != null && item.ParentIndexNumber != null) {

            var displayIndexNumber = item.IndexNumber;

            var number = "E" + displayIndexNumber;

            if (options.includeParentInfo !== false) {
                number = "S" + item.ParentIndexNumber + ", " + number;
            }

            if (item.IndexNumberEnd) {

                displayIndexNumber = item.IndexNumberEnd;
                number += "-" + displayIndexNumber;
            }

            name = number + " - " + name;

        }

        return name;
    }

    function getPrimaryMediaInfoHtml(item, options) {

        options = options || {};
        if (options.interactive == null) {
            options.interactive = false;
        }
        if (item.Type == 'Program') {
            return getProgramInfoHtml(item, options);
        }

        return getMediaInfoHtml(item, options);
    }

    function getSecondaryMediaInfoHtml(item, options) {

        options = options || {};
        if (options.interactive == null) {
            options.interactive = false;
        }
        if (item.Type == 'Program') {
            return getMediaInfoHtml(item, options);
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