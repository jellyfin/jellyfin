define(["itemHelper", "mediaInfo", "indicators", "connectionManager", "layoutManager", "globalize", "datetime", "apphost", "css!./listview", "emby-ratingbutton", "emby-playstatebutton"], function(itemHelper, mediaInfo, indicators, connectionManager, layoutManager, globalize, datetime, appHost) {
    "use strict";

    function getIndex(item, options) {
        if ("disc" === options.index) return null == item.ParentIndexNumber ? "" : globalize.translate("sharedcomponents#ValueDiscNumber", item.ParentIndexNumber);
        var code, name, sortBy = (options.sortBy || "").toLowerCase();
        return 0 === sortBy.indexOf("sortname") ? "Episode" === item.Type ? "" : (name = (item.SortName || item.Name || "?")[0].toUpperCase(), code = name.charCodeAt(0), code < 65 || code > 90 ? "#" : name.toUpperCase()) : 0 === sortBy.indexOf("officialrating") ? item.OfficialRating || globalize.translate("sharedcomponents#Unrated") : 0 === sortBy.indexOf("communityrating") ? null == item.CommunityRating ? globalize.translate("sharedcomponents#Unrated") : Math.floor(item.CommunityRating) : 0 === sortBy.indexOf("criticrating") ? null == item.CriticRating ? globalize.translate("sharedcomponents#Unrated") : Math.floor(item.CriticRating) : 0 === sortBy.indexOf("albumartist") && item.AlbumArtist ? (name = item.AlbumArtist[0].toUpperCase(), code = name.charCodeAt(0), code < 65 || code > 90 ? "#" : name.toUpperCase()) : ""
    }

    function getImageUrl(item, width) {
        var apiClient = connectionManager.getApiClient(item.ServerId),
            options = {
                width: width,
                type: "Primary"
            };
        return item.ImageTags && item.ImageTags.Primary ? (options.tag = item.ImageTags.Primary, apiClient.getScaledImageUrl(item.Id, options)) : item.AlbumId && item.AlbumPrimaryImageTag ? (options.tag = item.AlbumPrimaryImageTag, apiClient.getScaledImageUrl(item.AlbumId, options)) : item.SeriesId && item.SeriesPrimaryImageTag ? (options.tag = item.SeriesPrimaryImageTag, apiClient.getScaledImageUrl(item.SeriesId, options)) : item.ParentPrimaryImageTag ? (options.tag = item.ParentPrimaryImageTag, apiClient.getScaledImageUrl(item.ParentPrimaryImageItemId, options)) : null
    }

    function getChannelImageUrl(item, width) {
        var apiClient = connectionManager.getApiClient(item.ServerId),
            options = {
                width: width,
                type: "Primary"
            };
        return item.ChannelId && item.ChannelPrimaryImageTag ? (options.tag = item.ChannelPrimaryImageTag, apiClient.getScaledImageUrl(item.ChannelId, options)) : null
    }

    function getTextLinesHtml(textlines, isLargeStyle) {
        for (var html = "", largeTitleTagName = layoutManager.tv ? "h2" : "div", i = 0, length = textlines.length; i < length; i++) {
            textlines[i] && (html += 0 === i ? isLargeStyle ? "<" + largeTitleTagName + ' class="listItemBodyText">' : '<div class="listItemBodyText">' : '<div class="secondary listItemBodyText">', html += textlines[i] || "&nbsp;", html += 0 === i && isLargeStyle ? "</" + largeTitleTagName + ">" : "</div>")
        }
        return html
    }

    function getRightButtonsHtml(options) {
        for (var html = "", i = 0, length = options.rightButtons.length; i < length; i++) {
            var button = options.rightButtons[i];
            html += '<button is="paper-icon-button-light" class="listItemButton itemAction" data-action="custom" data-customaction="' + button.id + '" title="' + button.title + '"><i class="md-icon">' + button.icon + "</i></button>"
        }
        return html
    }

    function getId(item) {
        return item.Id
    }

    function getListViewHtml(options) {
        for (var items = options.items, groupTitle = "", action = options.action || "link", isLargeStyle = "large" === options.imageSize, enableOverview = options.enableOverview, clickEntireItem = !!layoutManager.tv, outerTagName = clickEntireItem ? "button" : "div", enableSideMediaInfo = null == options.enableSideMediaInfo || options.enableSideMediaInfo, outerHtml = "", enableContentWrapper = options.enableOverview && !layoutManager.tv, containerAlbumArtistIds = (options.containerAlbumArtists || []).map(getId), i = 0, length = items.length; i < length; i++) {
            var item = items[i],
                html = "";
            if (options.showIndex) {
                var itemGroupTitle = getIndex(item, options);
                itemGroupTitle !== groupTitle && (html && (html += "</div>"), html += 0 === i ? '<h2 class="listGroupHeader listGroupHeader-first">' : '<h2 class="listGroupHeader">', html += itemGroupTitle, html += "</h2>", html += "<div>", groupTitle = itemGroupTitle)
            }
            var cssClass = "listItem";
            (options.border || !1 !== options.highlight && !layoutManager.tv) && (cssClass += " listItem-border"), clickEntireItem && (cssClass += " itemAction listItem-button"), layoutManager.tv && (cssClass += " listItem-focusscale");
            var downloadWidth = 80;
            isLargeStyle && (cssClass += " listItem-largeImage", downloadWidth = 500);
            var playlistItemId = item.PlaylistItemId ? ' data-playlistitemid="' + item.PlaylistItemId + '"' : "",
                positionTicksData = item.UserData && item.UserData.PlaybackPositionTicks ? ' data-positionticks="' + item.UserData.PlaybackPositionTicks + '"' : "",
                collectionIdData = options.collectionId ? ' data-collectionid="' + options.collectionId + '"' : "",
                playlistIdData = options.playlistId ? ' data-playlistid="' + options.playlistId + '"' : "",
                mediaTypeData = item.MediaType ? ' data-mediatype="' + item.MediaType + '"' : "",
                collectionTypeData = item.CollectionType ? ' data-collectiontype="' + item.CollectionType + '"' : "",
                channelIdData = item.ChannelId ? ' data-channelid="' + item.ChannelId + '"' : "";
            if (enableContentWrapper && (cssClass += " listItem-withContentWrapper"), html += "<" + outerTagName + ' class="' + cssClass + '"' + playlistItemId + ' data-action="' + action + '" data-isfolder="' + item.IsFolder + '" data-id="' + item.Id + '" data-serverid="' + item.ServerId + '" data-type="' + item.Type + '"' + mediaTypeData + collectionTypeData + channelIdData + positionTicksData + collectionIdData + playlistIdData + ">", enableContentWrapper && (html += '<div class="listItem-content">'), !clickEntireItem && options.dragHandle && (html += '<i class="listViewDragHandle md-icon listItemIcon listItemIcon-transparent">&#xE25D;</i>'), !1 !== options.image) {
                var imgUrl = "channel" === options.imageSource ? getChannelImageUrl(item, downloadWidth) : getImageUrl(item, downloadWidth);
                console.log(imgUrl);
                var imageClass = isLargeStyle ? "listItemImage listItemImage-large" : "listItemImage";
                isLargeStyle && layoutManager.tv && (imageClass += " listItemImage-large-tv");
                var playOnImageClick = options.imagePlayButton && !layoutManager.tv;
                clickEntireItem || (imageClass += " itemAction");
                var imageAction = playOnImageClick ? "resume" : action;
                html += imgUrl ? '<div data-action="' + imageAction + '" class="' + imageClass + ' lazy" data-src="' + imgUrl + '" item-icon>' : '<div class="' + imageClass + '">';
                var indicatorsHtml = "";
                indicatorsHtml += indicators.getPlayedIndicatorHtml(item), indicatorsHtml && (html += '<div class="indicators listItemIndicators">' + indicatorsHtml + "</div>"), playOnImageClick && (html += '<button is="paper-icon-button-light" class="listItemImageButton itemAction" data-action="resume"><i class="md-icon listItemImageButton-icon">&#xE037;</i></button>');
                var progressHtml = indicators.getProgressBarHtml(item, {
                    containerClass: "listItemProgressBar"
                });
                progressHtml && (html += progressHtml), html += "</div>"
            }
            options.showIndexNumberLeft && (html += '<div class="listItem-indexnumberleft">', html += item.IndexNumber || "&nbsp;", html += "</div>");
            var textlines = [];
            options.showProgramDateTime && textlines.push(datetime.toLocaleString(datetime.parseISO8601Date(item.StartDate), {
                weekday: "long",
                month: "short",
                day: "numeric",
                hour: "numeric",
                minute: "2-digit"
            })), options.showProgramTime && textlines.push(datetime.getDisplayTime(datetime.parseISO8601Date(item.StartDate))), options.showChannel && item.ChannelName && textlines.push(item.ChannelName);
            var parentTitle = null;
            options.showParentTitle && ("Episode" === item.Type ? parentTitle = item.SeriesName : (item.IsSeries || item.EpisodeTitle && item.Name) && (parentTitle = item.Name));
            var displayName = itemHelper.getDisplayName(item, {
                includeParentInfo: options.includeParentInfoInTitle
            });
            if (options.showIndexNumber && null != item.IndexNumber && (displayName = item.IndexNumber + ". " + displayName), options.showParentTitle && options.parentTitleWithTitle ? (displayName && (parentTitle && (parentTitle += " - "), parentTitle = (parentTitle || "") + displayName), textlines.push(parentTitle || "")) : options.showParentTitle && textlines.push(parentTitle || ""), displayName && !options.parentTitleWithTitle && textlines.push(displayName), item.IsFolder) !1 !== options.artist && item.AlbumArtist && "MusicAlbum" === item.Type && textlines.push(item.AlbumArtist);
            else {
                var showArtist = !0 === options.artist,
                    artistItems = item.ArtistItems;
                showArtist || !1 === options.artist || (artistItems && artistItems.length ? (artistItems.length > 1 || -1 === containerAlbumArtistIds.indexOf(artistItems[0].Id)) && (showArtist = !0) : showArtist = !0), showArtist && artistItems && "MusicAlbum" !== item.Type && textlines.push(artistItems.map(function(a) {
                    return a.Name
                }).join(", "))
            }
            "Game" === item.Type && textlines.push(item.GameSystem), "TvChannel" === item.Type && item.CurrentProgram && textlines.push(itemHelper.getDisplayName(item.CurrentProgram)), cssClass = "listItemBody", clickEntireItem || (cssClass += " itemAction"), !1 === options.image && (cssClass += " listItemBody-noleftpadding"), html += '<div class="' + cssClass + '">';
            if (html += getTextLinesHtml(textlines, isLargeStyle), !1 !== options.mediaInfo && !enableSideMediaInfo) {
                html += '<div class="secondary listItemMediaInfo listItemBodyText">' + mediaInfo.getPrimaryMediaInfoHtml(item, {
                    episodeTitle: !1,
                    originalAirDate: !1,
                    subtitles: !1
                }) + "</div>"
            }
            if (enableOverview && item.Overview && (html += '<div class="secondary listItem-overview listItemBodyText">', html += item.Overview, html += "</div>"), html += "</div>", !1 !== options.mediaInfo && enableSideMediaInfo && (html += '<div class="secondary listItemMediaInfo">' + mediaInfo.getPrimaryMediaInfoHtml(item, {
                    year: !1,
                    container: !1,
                    episodeTitle: !1,
                    criticRating: !1,
                    endsAt: !1
                }) + "</div>"), options.recordButton || "Timer" !== item.Type && "Program" !== item.Type || (html += indicators.getTimerIndicator(item).replace("indicatorIcon", "indicatorIcon listItemAside")), !clickEntireItem && (options.addToListButton && (html += '<button is="paper-icon-button-light" class="listItemButton itemAction" data-action="addtoplaylist"><i class="md-icon">&#xE03B;</i></button>'), !1 !== options.moreButton && (html += '<button is="paper-icon-button-light" class="listItemButton itemAction" data-action="menu"><i class="md-icon">&#xE5D3;</i></button>'), options.infoButton && (html += '<button is="paper-icon-button-light" class="listItemButton itemAction" data-action="link"><i class="md-icon">&#xE88F;</i></button>'), options.rightButtons && (html += getRightButtonsHtml(options)), !1 !== options.enableUserDataButtons)) {
                html += '<span class="listViewUserDataButtons flex align-items-center">';
                var userData = item.UserData || {},
                    likes = null == userData.Likes ? "" : userData.Likes;
                itemHelper.canMarkPlayed(item) && (html += '<button is="emby-playstatebutton" type="button" class="listItemButton paper-icon-button-light" data-id="' + item.Id + '" data-serverid="' + item.ServerId + '" data-itemtype="' + item.Type + '" data-played="' + userData.Played + '"><i class="md-icon">&#xE5CA;</i></button>'), itemHelper.canRate(item) && (html += '<button is="emby-ratingbutton" type="button" class="listItemButton paper-icon-button-light" data-id="' + item.Id + '" data-serverid="' + item.ServerId + '" data-itemtype="' + item.Type + '" data-likes="' + likes + '" data-isfavorite="' + userData.IsFavorite + '"><i class="md-icon">&#xE87D;</i></button>'), html += "</span>"
            }
            enableContentWrapper && (html += "</div>", enableOverview && item.Overview && (html += '<div class="listItem-bottomoverview secondary">', html += item.Overview, html += "</div>")), html += "</" + outerTagName + ">", outerHtml += html
        }
        return outerHtml
    }
    return {
        getListViewHtml: getListViewHtml
    }
});