define(['itemHelper', 'mediaInfo', 'indicators', 'connectionManager', 'layoutManager', 'globalize', 'userdataButtons', 'apphost', 'css!./listview'], function (itemHelper, mediaInfo, indicators, connectionManager, layoutManager, globalize, userdataButtons, appHost) {

    function getIndex(item, options) {

        if (options.index == 'disc') {

            return item.ParentIndexNumber == null ? '' : globalize.translate('sharedcomponents#ValueDiscNumber', item.ParentIndexNumber);
        }

        var sortBy = (options.sortBy || '').toLowerCase();
        var code, name;

        if (sortBy.indexOf('sortname') == 0) {

            if (item.Type == 'Episode') return '';

            // SortName
            name = (item.SortName || item.Name || '?')[0].toUpperCase();

            code = name.charCodeAt(0);
            if (code < 65 || code > 90) {
                return '#';
            }

            return name.toUpperCase();
        }
        if (sortBy.indexOf('officialrating') == 0) {

            return item.OfficialRating || globalize.translate('sharedcomponents#Unrated');
        }
        if (sortBy.indexOf('communityrating') == 0) {

            if (item.CommunityRating == null) {
                return globalize.translate('sharedcomponents#Unrated');
            }

            return Math.floor(item.CommunityRating);
        }
        if (sortBy.indexOf('criticrating') == 0) {

            if (item.CriticRating == null) {
                return globalize.translate('sharedcomponents#Unrated');
            }

            return Math.floor(item.CriticRating);
        }
        if (sortBy.indexOf('metascore') == 0) {

            if (item.Metascore == null) {
                return globalize.translate('sharedcomponents#Unrated');
            }

            return Math.floor(item.Metascore);
        }
        if (sortBy.indexOf('albumartist') == 0) {

            // SortName
            if (!item.AlbumArtist) return '';

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

        if (item.ImageTags && item.ImageTags['Primary']) {

            options.tag = item.ImageTags['Primary'];
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

            if (i === 0) {
                if (isLargeStyle) {
                    html += '<h2>';
                } else {
                    html += '<div>';
                }
            } else {
                html += '<div class="secondary">';
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

        var isLargeStyle = options.imageSize == 'large';
        var enableOverview = options.enableOverview;

        var clickEntireItem = layoutManager.tv ? true : false;
        var outerTagName = clickEntireItem ? 'button' : 'div';
        var enableSideMediaInfo = options.enableSideMediaInfo != null ? options.enableSideMediaInfo : clickEntireItem;

        var outerHtml = '';

        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var html = '';

            if (options.showIndex) {

                var itemGroupTitle = getIndex(item, options);

                if (itemGroupTitle != groupTitle) {

                    if (html) {
                        html += '</div>';
                    }

                    if (i == 0) {
                        html += '<h1 class="listGroupHeader first">';
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

            if (clickEntireItem) {
                cssClass += ' itemAction';
            }

            var downloadWidth = 80;

            if (isLargeStyle) {
                cssClass += " largeImage";
                downloadWidth = 500;
            }

            var playlistItemId = item.PlaylistItemId ? (' data-playlistitemid="' + item.PlaylistItemId + '"') : '';

            var positionTicksData = item.UserData && item.UserData.PlaybackPositionTicks ? (' data-positionticks="' + item.UserData.PlaybackPositionTicks + '"') : '';
            var collectionIdData = options.collectionId ? (' data-collectionid="' + options.collectionId + '"') : '';
            var playlistIdData = options.playlistId ? (' data-playlistid="' + options.playlistId + '"') : '';
            var seriesIdData = item.SeriesId ? (' data-seriesid="' + item.SeriesId + '"') : '';

            html += '<' + outerTagName + ' class="' + cssClass + '" data-index="' + i + '"' + playlistItemId + ' data-action="' + action + '" data-isfolder="' + item.IsFolder + '" data-id="' + item.Id + '" data-serverid="' + item.ServerId + '" data-mediatype="' + item.MediaType + '" data-type="' + item.Type + '"' + positionTicksData + collectionIdData + playlistIdData + seriesIdData + '>';

            if (!clickEntireItem && options.dragHandle) {
                html += '<button is="paper-icon-button-light" class="listViewDragHandle autoSize"><i class="md-icon">&#xE25D;</i></button>';
            }

            var imgUrl = getImageUrl(item, downloadWidth);

            if (imgUrl) {
                html += '<div class="listItemImage lazy" data-src="' + imgUrl + '" item-icon>';
            } else {
                html += '<div class="listItemImage">';
            }

            var indicatorsHtml = '';
            indicatorsHtml += indicators.getPlayedIndicatorHtml(item);

            if (indicatorsHtml) {
                html += '<div class="indicators">' + indicatorsHtml + '</div>';
            }

            var progressHtml = indicators.getProgressBarHtml(item);

            if (progressHtml) {
                html += progressHtml;
            }
            html += '</div>';

            var textlines = [];

            if (options.showParentTitle) {
                if (item.Type == 'Episode') {
                    textlines.push(item.SeriesName || '&nbsp;');
                }
            }

            var displayName = itemHelper.getDisplayName(item);

            if (options.showIndexNumber && item.IndexNumber != null) {
                displayName = item.IndexNumber + ". " + displayName;
            }
            textlines.push(displayName);

            if (item.ArtistItems && item.Type != 'MusicAlbum') {
                textlines.push(item.ArtistItems.map(function (a) {
                    return a.Name;

                }).join(', ') || '&nbsp;');
            }

            if (item.AlbumArtist && item.Type == 'MusicAlbum') {
                textlines.push(item.AlbumArtist || '&nbsp;');
            }

            if (item.Type == 'Game') {
                textlines.push(item.GameSystem || '&nbsp;');
            }

            if (item.Type == 'TvChannel') {

                if (item.CurrentProgram) {
                    textlines.push(itemHelper.getDisplayName(item.CurrentProgram));
                }
            }

            cssClass = 'listItemBody';
            if (!clickEntireItem) {
                cssClass += ' itemAction';
            }

            html += '<div class="' + cssClass + '">';

            var moreIcon = appHost.moreIcon == 'dots-horiz' ? '&#xE5D3;' : '&#xE5D4;';

            html += getTextLinesHtml(textlines, isLargeStyle);

            if (!enableSideMediaInfo) {
                html += '<div class="secondary listItemMediaInfo">' + mediaInfo.getPrimaryMediaInfoHtml(item) + '</div>';
            }

            if (enableOverview && item.Overview) {
                html += '<div class="secondary overview">';
                html += item.Overview;
                html += '</div>';
            }

            html += '</div>';

            if (enableSideMediaInfo) {
                html += '<div class="secondary listItemMediaInfo">' + mediaInfo.getPrimaryMediaInfoHtml(item, {

                    year: false,
                    container: false

                }) + '</div>';
            }

            if (!clickEntireItem) {
                html += '<button is="paper-icon-button-light" class="itemAction autoSize" data-action="menu"><i class="md-icon">' + moreIcon + '</i></button>';
                html += '<span class="listViewUserDataButtons">';
                html += userdataButtons.getIconsHtml({
                    item: item,
                    includePlayed: false
                });
                html += '</span>';
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