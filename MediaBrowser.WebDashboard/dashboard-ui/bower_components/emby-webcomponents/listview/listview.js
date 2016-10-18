define(['itemHelper', 'mediaInfo', 'indicators', 'connectionManager', 'layoutManager', 'globalize', 'datetime', 'userdataButtons', 'apphost', 'css!./listview'], function (itemHelper, mediaInfo, indicators, connectionManager, layoutManager, globalize, datetime, userdataButtons, appHost) {
    'use strict';

    function getIndex(item, options) {

        if (options.index === 'disc') {

            return item.ParentIndexNumber == null ? '' : globalize.translate('sharedcomponents#ValueDiscNumber', item.ParentIndexNumber);
        }

        var sortBy = (options.sortBy || '').toLowerCase();
        var code, name;

        if (sortBy.indexOf('sortname') === 0) {

            if (item.Type === 'Episode') {
                return '';
            }

            // SortName
            name = (item.SortName || item.Name || '?')[0].toUpperCase();

            code = name.charCodeAt(0);
            if (code < 65 || code > 90) {
                return '#';
            }

            return name.toUpperCase();
        }
        if (sortBy.indexOf('officialrating') === 0) {

            return item.OfficialRating || globalize.translate('sharedcomponents#Unrated');
        }
        if (sortBy.indexOf('communityrating') === 0) {

            if (item.CommunityRating == null) {
                return globalize.translate('sharedcomponents#Unrated');
            }

            return Math.floor(item.CommunityRating);
        }
        if (sortBy.indexOf('criticrating') === 0) {

            if (item.CriticRating == null) {
                return globalize.translate('sharedcomponents#Unrated');
            }

            return Math.floor(item.CriticRating);
        }
        if (sortBy.indexOf('metascore') === 0) {

            if (item.Metascore == null) {
                return globalize.translate('sharedcomponents#Unrated');
            }

            return Math.floor(item.Metascore);
        }
        if (sortBy.indexOf('albumartist') === 0) {

            // SortName
            if (!item.AlbumArtist) {
                return '';
            }

            name = item.AlbumArtist[0].toUpperCase();

            code = name.charCodeAt(0);
            if (code < 65 || code > 90) {
                return '#';
            }

            return name.toUpperCase();
        }
        return '';
    }

    function getImageUrl(item, width) {

        var apiClient = connectionManager.getApiClient(item.ServerId);

        var options = {
            width: width,
            type: "Primary"
        };

        if (item.ImageTags && item.ImageTags.Primary) {

            options.tag = item.ImageTags.Primary;
            return apiClient.getScaledImageUrl(item.Id, options);
        }

        if (item.AlbumId && item.AlbumPrimaryImageTag) {

            options.tag = item.AlbumPrimaryImageTag;
            return apiClient.getScaledImageUrl(item.AlbumId, options);
        }

        else if (item.SeriesId && item.SeriesPrimaryImageTag) {

            options.tag = item.SeriesPrimaryImageTag;
            return apiClient.getScaledImageUrl(item.SeriesId, options);

        }
        else if (item.ParentPrimaryImageTag) {

            options.tag = item.ParentPrimaryImageTag;
            return apiClient.getScaledImageUrl(item.ParentPrimaryImageItemId, options);
        }

        return null;
    }

    function getTextLinesHtml(textlines, isLargeStyle) {

        var html = '';

        for (var i = 0, length = textlines.length; i < length; i++) {

            var text = textlines[i];

            if (!text) {
                continue;
            }

            if (i === 0) {
                if (isLargeStyle) {
                    html += '<h2 class="listItemBodyText">';
                } else {
                    html += '<div class="listItemBodyText">';
                }
            } else {
                html += '<div class="secondary listItemBodyText">';
            }
            html += (textlines[i] || '&nbsp;');
            if (i === 0 && isLargeStyle) {
                html += '</h2>';
            } else {
                html += '</div>';
            }
        }

        return html;
    }

    function getListViewHtml(options) {

        var items = options.items;

        var groupTitle = '';
        var action = options.action || 'link';

        var isLargeStyle = options.imageSize === 'large';
        var enableOverview = options.enableOverview;

        var clickEntireItem = layoutManager.tv ? true : false;
        var outerTagName = clickEntireItem ? 'button' : 'div';
        var enableSideMediaInfo = options.enableSideMediaInfo != null ? options.enableSideMediaInfo : true;

        var outerHtml = '';

        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var html = '';

            if (options.showIndex) {

                var itemGroupTitle = getIndex(item, options);

                if (itemGroupTitle !== groupTitle) {

                    if (html) {
                        html += '</div>';
                    }

                    if (i === 0) {
                        html += '<h1 class="listGroupHeader listGroupHeader-first">';
                    }
                    else {
                        html += '<h1 class="listGroupHeader">';
                    }
                    html += itemGroupTitle;
                    html += '</h1>';

                    html += '<div>';

                    groupTitle = itemGroupTitle;
                }
            }

            var cssClass = "listItem";

            if (options.highlight !== false) {
                if (i % 2 === 1) {
                    cssClass += ' listItem-odd';
                }
            }

            if (clickEntireItem) {
                cssClass += ' itemAction listItem-button';
            }

            if (layoutManager.tv) {
                cssClass += ' listItem-focusscale';
            }

            var downloadWidth = 80;

            if (isLargeStyle) {
                cssClass += " listItem-largeImage";
                downloadWidth = 500;
            }

            var playlistItemId = item.PlaylistItemId ? (' data-playlistitemid="' + item.PlaylistItemId + '"') : '';

            var positionTicksData = item.UserData && item.UserData.PlaybackPositionTicks ? (' data-positionticks="' + item.UserData.PlaybackPositionTicks + '"') : '';
            var collectionIdData = options.collectionId ? (' data-collectionid="' + options.collectionId + '"') : '';
            var playlistIdData = options.playlistId ? (' data-playlistid="' + options.playlistId + '"') : '';
            var mediaTypeData = item.MediaType ? (' data-mediatype="' + item.MediaType + '"') : '';
            var collectionTypeData = item.CollectionType ? (' data-collectiontype="' + item.CollectionType + '"') : '';
            var channelIdData = item.ChannelId ? (' data-channelid="' + item.ChannelId + '"') : '';

            html += '<' + outerTagName + ' class="' + cssClass + '" data-index="' + i + '"' + playlistItemId + ' data-action="' + action + '" data-isfolder="' + item.IsFolder + '" data-id="' + item.Id + '" data-serverid="' + item.ServerId + '" data-type="' + item.Type + '"' + mediaTypeData + collectionTypeData + channelIdData + positionTicksData + collectionIdData + playlistIdData + '>';

            if (!clickEntireItem && options.dragHandle) {
                html += '<button is="paper-icon-button-light" class="listViewDragHandle autoSize listItemButton"><i class="md-icon">&#xE25D;</i></button>';
            }

            if (options.image !== false) {
                var imgUrl = getImageUrl(item, downloadWidth);

                var imageClass = isLargeStyle ? 'listItemImage listItemImage-large' : 'listItemImage';

                if (imgUrl) {
                    html += '<div class="' + imageClass + ' lazy" data-src="' + imgUrl + '" item-icon>';
                } else {
                    html += '<div class="' + imageClass + '">';
                }

                var indicatorsHtml = '';
                indicatorsHtml += indicators.getPlayedIndicatorHtml(item);

                if (indicatorsHtml) {
                    html += '<div class="indicators listItemIndicators">' + indicatorsHtml + '</div>';
                }

                var progressHtml = indicators.getProgressBarHtml(item, {
                    containerClass: 'listItemProgressBar'
                });

                if (progressHtml) {
                    html += progressHtml;
                }
                html += '</div>';
            }

            var textlines = [];

            if (options.showProgramDateTime) {
                textlines.push(datetime.toLocaleString(datetime.parseISO8601Date(item.StartDate), {

                    weekday: 'long',
                    month: 'short',
                    day: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit'
                }));
            }

            if (options.showProgramTime) {
                textlines.push(datetime.getDisplayTime(datetime.parseISO8601Date(item.StartDate)));
            }

            var parentTitle = null;

            if (options.showParentTitle) {
                if (item.Type === 'Episode') {
                    parentTitle = item.SeriesName;
                }

                else if (item.IsSeries) {
                    parentTitle = item.Name;
                }
            }

            var displayName = itemHelper.getDisplayName(item);

            if (options.showIndexNumber && item.IndexNumber != null) {
                displayName = item.IndexNumber + ". " + displayName;
            }

            if (options.showParentTitle && options.parentTitleWithTitle) {

                if (displayName) {

                    if (parentTitle) {
                        parentTitle += ' - ';
                    }
                    parentTitle = (parentTitle || '') + displayName;
                }

                textlines.push(parentTitle || '');
            }
            else if (options.showParentTitle) {
                textlines.push(parentTitle || '');
            }

            if (displayName && !options.parentTitleWithTitle) {
                textlines.push(displayName);
            }

            if (options.artist !== false) {
                if (item.ArtistItems && item.Type !== 'MusicAlbum') {
                    textlines.push(item.ArtistItems.map(function (a) {
                        return a.Name;

                    }).join(', '));
                }

                if (item.AlbumArtist && item.Type === 'MusicAlbum') {
                    textlines.push(item.AlbumArtist);
                }
            }

            if (item.Type === 'Game') {
                textlines.push(item.GameSystem);
            }

            if (item.Type === 'TvChannel') {

                if (item.CurrentProgram) {
                    textlines.push(itemHelper.getDisplayName(item.CurrentProgram));
                }
            }

            cssClass = 'listItemBody';
            if (!clickEntireItem) {
                cssClass += ' itemAction';
            }

            if (options.image === false) {
                cssClass += ' itemAction listItemBody-noleftpadding';
            }

            html += '<div class="' + cssClass + '">';

            var moreIcon = appHost.moreIcon === 'dots-horiz' ? '&#xE5D3;' : '&#xE5D4;';

            html += getTextLinesHtml(textlines, isLargeStyle);

            if (options.mediaInfo !== false) {
                if (!enableSideMediaInfo) {

                    var mediaInfoClass = 'secondary listItemMediaInfo listItemBodyText';

                    html += '<div class="' + mediaInfoClass + '">' + mediaInfo.getPrimaryMediaInfoHtml(item, {
                        episodeTitle: false,
                        originalAirDate: false
                    }) + '</div>';
                }
            }

            if (enableOverview && item.Overview) {
                html += '<div class="secondary overview listItemBodyText">';
                html += item.Overview;
                html += '</div>';
            }

            html += '</div>';

            if (options.mediaInfo !== false) {
                if (enableSideMediaInfo) {
                    html += '<div class="secondary listItemMediaInfo">' + mediaInfo.getPrimaryMediaInfoHtml(item, {

                        year: false,
                        container: false,
                        episodeTitle: false

                    }) + '</div>';
                }
            }

            if (!options.recordButton && (item.Type === 'Timer' || item.Type === 'Program')) {
                html += indicators.getTimerIndicator(item).replace('indicatorIcon', 'indicatorIcon listItemAside');
            }

            if (!clickEntireItem) {

                if (options.moreButton !== false) {
                    html += '<button is="paper-icon-button-light" class="listItemButton itemAction autoSize" data-action="menu"><i class="md-icon">' + moreIcon + '</i></button>';
                }

                if (options.recordButton) {

                    html += '<button is="paper-icon-button-light" class="listItemButton itemAction autoSize" data-action="programdialog">' + indicators.getTimerIndicator(item) + '</button>';
                }

                if (options.enableUserDataButtons !== false) {
                    html += '<span class="listViewUserDataButtons">';
                    html += userdataButtons.getIconsHtml({
                        item: item,
                        includePlayed: false,
                        cssClass: 'listItemButton'
                    });
                    html += '</span>';
                }
            }

            html += '</' + outerTagName + '>';

            outerHtml += html;
        }

        return outerHtml;
    }

    return {
        getListViewHtml: getListViewHtml
    };
});