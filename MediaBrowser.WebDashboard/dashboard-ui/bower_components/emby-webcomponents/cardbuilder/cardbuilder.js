define(["datetime", "imageLoader", "connectionManager", "itemHelper", "focusManager", "indicators", "globalize", "layoutManager", "apphost", "dom", "browser", "playbackManager", "itemShortcuts", "css!./card", "paper-icon-button-light", "programStyles"], function(datetime, imageLoader, connectionManager, itemHelper, focusManager, indicators, globalize, layoutManager, appHost, dom, browser, playbackManager, itemShortcuts) {
    "use strict";

    function getCardsHtml(items, options) {
        return 1 === arguments.length && (options = arguments[0], items = options.items), buildCardsHtmlInternal(items, options)
    }

    function getPostersPerRow(shape, screenWidth, isOrientationLandscape) {
        switch (shape) {
            case "portrait":
                return layoutManager.tv ? 5.9999999988 : screenWidth >= 2200 ? 10 : screenWidth >= 1920 ? 9.000000000009 : screenWidth >= 1600 ? 8 : screenWidth >= 1400 ? 7.0000000000021 : screenWidth >= 1200 ? 5.9999999988 : screenWidth >= 800 ? 5 : screenWidth >= 700 ? 4 : 3.0000000003;
            case "square":
                return layoutManager.tv ? 5.9999999988 : screenWidth >= 2200 ? 10 : screenWidth >= 1920 ? 9.000000000009 : screenWidth >= 1600 ? 8 : screenWidth >= 1400 ? 7.0000000000021 : screenWidth >= 1200 ? 5.9999999988 : screenWidth >= 800 ? 5 : screenWidth >= 700 ? 4 : screenWidth >= 500 ? 3.0000000003 : 2;
            case "banner":
                return screenWidth >= 2200 ? 4 : screenWidth >= 1200 ? 3.0000000003 : screenWidth >= 800 ? 2 : 1;
            case "backdrop":
                return layoutManager.tv ? 4 : screenWidth >= 2500 ? 6 : screenWidth >= 1600 ? 5 : screenWidth >= 1200 ? 4 : screenWidth >= 770 ? 3 : screenWidth >= 420 ? 2 : 1;
            case "smallBackdrop":
                return screenWidth >= 1600 ? 8 : screenWidth >= 1400 ? 7.000000000007001 : screenWidth >= 1200 ? 6 : screenWidth >= 1e3 ? 5 : screenWidth >= 800 ? 4 : screenWidth >= 500 ? 3.0000000003 : 2;
            case "overflowSmallBackdrop":
                return layoutManager.tv ? 100 / 18.9 : isOrientationLandscape ? screenWidth >= 800 ? 100 / 15.5 : 100 / 23.3 : screenWidth >= 540 ? 100 / 30 : 100 / 72;
            case "overflowPortrait":
                return layoutManager.tv ? 100 / 15.5 : isOrientationLandscape ? screenWidth >= 1700 ? 100 / 11.6 : 100 / 15.5 : screenWidth >= 1400 ? 100 / 15 : screenWidth >= 1200 ? 100 / 18 : screenWidth >= 760 ? 100 / 23 : screenWidth >= 400 ? 100 / 31.5 : 100 / 42;
            case "overflowSquare":
                return layoutManager.tv ? 100 / 15.5 : isOrientationLandscape ? screenWidth >= 1700 ? 100 / 11.6 : 100 / 15.5 : screenWidth >= 1400 ? 100 / 15 : screenWidth >= 1200 ? 100 / 18 : screenWidth >= 760 ? 100 / 23 : screenWidth >= 540 ? 100 / 31.5 : 100 / 42;
            case "overflowBackdrop":
                return layoutManager.tv ? 100 / 23.3 : isOrientationLandscape ? screenWidth >= 1700 ? 100 / 18.5 : 100 / 23.3 : screenWidth >= 1800 ? 100 / 23.5 : screenWidth >= 1400 ? 100 / 30 : screenWidth >= 760 ? 2.5 : screenWidth >= 640 ? 100 / 56 : 100 / 72;
            default:
                return 4
        }
    }

    function isResizable(windowWidth) {
        var screen = window.screen;
        if (screen) {
            if (screen.availWidth - windowWidth > 20) return !0
        }
        return !1
    }

    function getImageWidth(shape, screenWidth, isOrientationLandscape) {
        var imagesPerRow = getPostersPerRow(shape, screenWidth, isOrientationLandscape),
            shapeWidth = screenWidth / imagesPerRow;
        return Math.round(shapeWidth)
    }

    function setCardData(items, options) {
        options.shape = options.shape || "auto";
        var primaryImageAspectRatio = imageLoader.getPrimaryImageAspectRatio(items);
        if ("auto" === options.shape || "autohome" === options.shape || "autooverflow" === options.shape || "autoVertical" === options.shape) {
            var requestedShape = options.shape;
            options.shape = null, primaryImageAspectRatio && (primaryImageAspectRatio >= 3 ? (options.shape = "banner", options.coverImage = !0) : options.shape = primaryImageAspectRatio >= 1.33 ? "autooverflow" === requestedShape ? "overflowBackdrop" : "backdrop" : primaryImageAspectRatio > .71 ? "autooverflow" === requestedShape ? "overflowSquare" : "square" : "autooverflow" === requestedShape ? "overflowPortrait" : "portrait"), options.shape || (options.shape = options.defaultShape || ("autooverflow" === requestedShape ? "overflowSquare" : "square"))
        }
        if ("auto" === options.preferThumb && (options.preferThumb = "backdrop" === options.shape || "overflowBackdrop" === options.shape), options.uiAspect = getDesiredAspect(options.shape), options.primaryImageAspectRatio = primaryImageAspectRatio, !options.width && options.widths && (options.width = options.widths[options.shape]), options.rows && "number" != typeof options.rows && (options.rows = options.rows[options.shape]), !options.width) {
            var screenWidth = dom.getWindowSize().innerWidth,
                screenHeight = dom.getWindowSize().innerHeight;
            if (isResizable(screenWidth)) {
                screenWidth = 100 * Math.floor(screenWidth / 100)
            }
            options.width = getImageWidth(options.shape, screenWidth, screenWidth > 1.3 * screenHeight)
        }
    }

    function buildCardsHtmlInternal(items, options) {
        var isVertical;
        "autoVertical" === options.shape && (isVertical = !0), setCardData(items, options);
        var currentIndexValue, hasOpenRow, hasOpenSection, apiClient, lastServerId, i, length, html = "",
            itemsInRow = 0,
            sectionTitleTagName = options.sectionTitleTagName || "div";
        for (i = 0, length = items.length; i < length; i++) {
            var item = items[i],
                serverId = item.ServerId || options.serverId;
            if (serverId !== lastServerId && (lastServerId = serverId, apiClient = connectionManager.getApiClient(lastServerId)), options.indexBy) {
                var newIndexValue = "";
                if ("PremiereDate" === options.indexBy) {
                    if (item.PremiereDate) try {
                        newIndexValue = datetime.toLocaleDateString(datetime.parseISO8601Date(item.PremiereDate), {
                            weekday: "long",
                            month: "long",
                            day: "numeric"
                        })
                    } catch (err) {}
                } else "ProductionYear" === options.indexBy ? newIndexValue = item.ProductionYear : "CommunityRating" === options.indexBy && (newIndexValue = item.CommunityRating ? Math.floor(item.CommunityRating) + (item.CommunityRating % 1 >= .5 ? .5 : 0) + "+" : null);
                newIndexValue !== currentIndexValue && (hasOpenRow && (html += "</div>", hasOpenRow = !1, itemsInRow = 0), hasOpenSection && (html += "</div>", isVertical && (html += "</div>"), hasOpenSection = !1), html += isVertical ? '<div class="verticalSection">' : '<div class="horizontalSection">', html += "<" + sectionTitleTagName + ' class="sectionTitle">' + newIndexValue + "</" + sectionTitleTagName + ">", isVertical && (html += '<div class="itemsContainer vertical-wrap">'), currentIndexValue = newIndexValue, hasOpenSection = !0)
            }
            options.rows && 0 === itemsInRow && (hasOpenRow && (html += "</div>", hasOpenRow = !1), html += '<div class="cardColumn">', hasOpenRow = !0), html += buildCard(i, item, apiClient, options), itemsInRow++, options.rows && itemsInRow >= options.rows && (html += "</div>", hasOpenRow = !1, itemsInRow = 0)
        }
        hasOpenRow && (html += "</div>"), hasOpenSection && (html += "</div>", isVertical && (html += "</div>"));
        var cardFooterHtml = "";
        for (i = 0, length = options.lines || 0; i < length; i++) cardFooterHtml += 0 === i ? '<div class="cardText cardTextCentered cardText-first">&nbsp;</div>' : '<div class="cardText cardTextCentered cardText-secondary">&nbsp;</div>';
        return html
    }

    function getDesiredAspect(shape) {
        if (shape) {
            if (shape = shape.toLowerCase(), -1 !== shape.indexOf("portrait")) return 2 / 3;
            if (-1 !== shape.indexOf("backdrop")) return 16 / 9;
            if (-1 !== shape.indexOf("square")) return 1;
            if (-1 !== shape.indexOf("banner")) return 1e3 / 185
        }
        return null
    }

    function getCardImageUrl(item, apiClient, options, shape) {
        item = item.ProgramInfo || item;
        var width = options.width,
            height = null,
            primaryImageAspectRatio = item.PrimaryImageAspectRatio,
            forceName = !1,
            imgUrl = null,
            coverImage = !1,
            uiAspect = null;
        return options.preferThumb && item.ImageTags && item.ImageTags.Thumb ? imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Thumb",
            maxWidth: width,
            tag: item.ImageTags.Thumb
        }) : (options.preferBanner || "banner" === shape) && item.ImageTags && item.ImageTags.Banner ? imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Banner",
            maxWidth: width,
            tag: item.ImageTags.Banner
        }) : options.preferDisc && item.ImageTags && item.ImageTags.Disc ? imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Disc",
            maxWidth: width,
            tag: item.ImageTags.Disc
        }) : options.preferLogo && item.ImageTags && item.ImageTags.Logo ? imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Logo",
            maxWidth: width,
            tag: item.ImageTags.Logo
        }) : options.preferLogo && item.ParentLogoImageTag && item.ParentLogoItemId ? imgUrl = apiClient.getScaledImageUrl(item.ParentLogoItemId, {
            type: "Logo",
            maxWidth: width,
            tag: item.ParentLogoImageTag
        }) : options.preferThumb && item.SeriesThumbImageTag && !1 !== options.inheritThumb ? imgUrl = apiClient.getScaledImageUrl(item.SeriesId, {
            type: "Thumb",
            maxWidth: width,
            tag: item.SeriesThumbImageTag
        }) : options.preferThumb && item.ParentThumbItemId && !1 !== options.inheritThumb && "Photo" !== item.MediaType ? imgUrl = apiClient.getScaledImageUrl(item.ParentThumbItemId, {
            type: "Thumb",
            maxWidth: width,
            tag: item.ParentThumbImageTag
        }) : options.preferThumb && item.BackdropImageTags && item.BackdropImageTags.length ? (imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Backdrop",
            maxWidth: width,
            tag: item.BackdropImageTags[0]
        }), forceName = !0) : options.preferThumb && item.ParentBackdropImageTags && item.ParentBackdropImageTags.length && !1 !== options.inheritThumb && "Episode" === item.Type ? imgUrl = apiClient.getScaledImageUrl(item.ParentBackdropItemId, {
            type: "Backdrop",
            maxWidth: width,
            tag: item.ParentBackdropImageTags[0]
        }) : item.ImageTags && item.ImageTags.Primary ? (height = width && primaryImageAspectRatio ? Math.round(width / primaryImageAspectRatio) : null, imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Primary",
            maxHeight: height,
            maxWidth: width,
            tag: item.ImageTags.Primary
        }), options.preferThumb && !1 !== options.showTitle && (forceName = !0), primaryImageAspectRatio && (uiAspect = getDesiredAspect(shape)) && (coverImage = Math.abs(primaryImageAspectRatio - uiAspect) / uiAspect <= .2)) : item.PrimaryImageTag ? (height = width && primaryImageAspectRatio ? Math.round(width / primaryImageAspectRatio) : null, imgUrl = apiClient.getScaledImageUrl(item.PrimaryImageItemId || item.Id || item.ItemId, {
            type: "Primary",
            maxHeight: height,
            maxWidth: width,
            tag: item.PrimaryImageTag
        }), options.preferThumb && !1 !== options.showTitle && (forceName = !0), primaryImageAspectRatio && (uiAspect = getDesiredAspect(shape)) && (coverImage = Math.abs(primaryImageAspectRatio - uiAspect) / uiAspect <= .2)) : item.ParentPrimaryImageTag ? imgUrl = apiClient.getScaledImageUrl(item.ParentPrimaryImageItemId, {
            type: "Primary",
            maxWidth: width,
            tag: item.ParentPrimaryImageTag
        }) : item.SeriesPrimaryImageTag ? imgUrl = apiClient.getScaledImageUrl(item.SeriesId, {
            type: "Primary",
            maxWidth: width,
            tag: item.SeriesPrimaryImageTag
        }) : item.AlbumId && item.AlbumPrimaryImageTag ? (width = primaryImageAspectRatio ? Math.round(height * primaryImageAspectRatio) : null, imgUrl = apiClient.getScaledImageUrl(item.AlbumId, {
            type: "Primary",
            maxHeight: height,
            maxWidth: width,
            tag: item.AlbumPrimaryImageTag
        }), primaryImageAspectRatio && (uiAspect = getDesiredAspect(shape)) && (coverImage = Math.abs(primaryImageAspectRatio - uiAspect) / uiAspect <= .2)) : "Season" === item.Type && item.ImageTags && item.ImageTags.Thumb ? imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Thumb",
            maxWidth: width,
            tag: item.ImageTags.Thumb
        }) : item.BackdropImageTags && item.BackdropImageTags.length ? imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Backdrop",
            maxWidth: width,
            tag: item.BackdropImageTags[0]
        }) : item.ImageTags && item.ImageTags.Thumb ? imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Thumb",
            maxWidth: width,
            tag: item.ImageTags.Thumb
        }) : item.SeriesThumbImageTag && !1 !== options.inheritThumb ? imgUrl = apiClient.getScaledImageUrl(item.SeriesId, {
            type: "Thumb",
            maxWidth: width,
            tag: item.SeriesThumbImageTag
        }) : item.ParentThumbItemId && !1 !== options.inheritThumb ? imgUrl = apiClient.getScaledImageUrl(item.ParentThumbItemId, {
            type: "Thumb",
            maxWidth: width,
            tag: item.ParentThumbImageTag
        }) : item.ParentBackdropImageTags && item.ParentBackdropImageTags.length && !1 !== options.inheritThumb && (imgUrl = apiClient.getScaledImageUrl(item.ParentBackdropItemId, {
            type: "Backdrop",
            maxWidth: width,
            tag: item.ParentBackdropImageTags[0]
        })), {
            imgUrl: imgUrl,
            forceName: forceName,
            coverImage: coverImage
        }
    }

    function getRandomInt(min, max) {
        return Math.floor(Math.random() * (max - min + 1)) + min
    }

    function getDefaultColorIndex(str) {
        if (str) {
            for (var charIndex = Math.floor(str.length / 2), character = String(str.substr(charIndex, 1).charCodeAt()), sum = 0, i = 0; i < character.length; i++) sum += parseInt(character.charAt(i));
            return String(sum).substr(-1) % numRandomColors + 1
        }
        return getRandomInt(1, numRandomColors)
    }

    function getCardTextLines(lines, cssClass, forceLines, isOuterFooter, cardLayout, addRightMargin, maxLines) {
        var i, length, html = "",
            valid = 0;
        for (i = 0, length = lines.length; i < length; i++) {
            var currentCssClass = cssClass,
                text = lines[i];
            if (valid > 0 && isOuterFooter ? currentCssClass += " cardText-secondary" : 0 === valid && isOuterFooter && (currentCssClass += " cardText-first"), addRightMargin && (currentCssClass += " cardText-rightmargin"), text && (html += "<div class='" + currentCssClass + "'>", html += text, html += "</div>", valid++, maxLines && valid >= maxLines)) break
        }
        if (forceLines)
            for (length = maxLines || Math.min(lines.length, maxLines || lines.length); valid < length;) html += "<div class='" + cssClass + "'>&nbsp;</div>", valid++;
        return html
    }

    function isUsingLiveTvNaming(item) {
        return "Program" === item.Type || "Timer" === item.Type || "Recording" === item.Type
    }

    function getAirTimeText(item, showAirDateTime, showAirEndTime) {
        var airTimeText = "";
        if (item.StartDate) try {
            var date = datetime.parseISO8601Date(item.StartDate);
            showAirDateTime && (airTimeText += datetime.toLocaleDateString(date, {
                weekday: "short",
                month: "short",
                day: "numeric"
            }) + " "), airTimeText += datetime.getDisplayTime(date), item.EndDate && showAirEndTime && (date = datetime.parseISO8601Date(item.EndDate), airTimeText += " - " + datetime.getDisplayTime(date))
        } catch (e) {
            console.log("Error parsing date: " + item.StartDate)
        }
        return airTimeText
    }

    function getCardFooterText(item, apiClient, options, showTitle, forceName, overlayText, imgUrl, footerClass, progressHtml, logoUrl, isOuterFooter) {
        var html = "";
        logoUrl && (html += '<div class="lazy cardFooterLogo" data-src="' + logoUrl + '"></div>');
        var showOtherText = isOuterFooter ? !overlayText : overlayText;
        isOuterFooter && options.cardLayout && layoutManager.mobile && "none" !== options.cardFooterAside && (html += '<button is="paper-icon-button-light" class="itemAction btnCardOptions cardText-secondary" data-action="menu"><i class="md-icon">&#xE5D3;</i></button>');
        var titleAdded, cssClass = options.centerText ? "cardText cardTextCentered" : "cardText",
            lines = [],
            parentTitleUnderneath = "MusicAlbum" === item.Type || "Audio" === item.Type || "MusicVideo" === item.Type;
        if (showOtherText && (options.showParentTitle || options.showParentTitleOrTitle) && !parentTitleUnderneath)
            if (isOuterFooter && "Episode" === item.Type && item.SeriesName) item.SeriesId ? lines.push(getTextActionButton({
                Id: item.SeriesId,
                ServerId: item.ServerId,
                Name: item.SeriesName,
                Type: "Series",
                IsFolder: !0
            })) : lines.push(item.SeriesName);
            else if (isUsingLiveTvNaming(item)) lines.push(item.Name), item.EpisodeTitle || (titleAdded = !0);
        else {
            var parentTitle = item.SeriesName || item.Series || item.Album || item.AlbumArtist || item.GameSystem || "";
            (parentTitle || showTitle) && lines.push(parentTitle)
        }
        var showMediaTitle = showTitle && !titleAdded || options.showParentTitleOrTitle && !lines.length;
        if (showMediaTitle || titleAdded || !showTitle && !forceName || (showMediaTitle = !0), showMediaTitle) {
            var name = "auto" !== options.showTitle || item.IsFolder || "Photo" !== item.MediaType ? itemHelper.getDisplayName(item, {
                includeParentInfo: options.includeParentInfoInTitle
            }) : "";
            lines.push(name)
        }
        if (showOtherText) {
            if (options.showParentTitle && parentTitleUnderneath && (isOuterFooter && item.AlbumArtists && item.AlbumArtists.length ? (item.AlbumArtists[0].Type = "MusicArtist", item.AlbumArtists[0].IsFolder = !0, lines.push(getTextActionButton(item.AlbumArtists[0], null, item.ServerId))) : lines.push(isUsingLiveTvNaming(item) ? item.Name : item.SeriesName || item.Series || item.Album || item.AlbumArtist || item.GameSystem || "")), options.showItemCounts) {
                var itemCountHtml = getItemCountsHtml(options, item);
                lines.push(itemCountHtml)
            }
            if (options.textLines)
                for (var additionalLines = options.textLines(item), i = 0, length = additionalLines.length; i < length; i++) lines.push(additionalLines[i]);
            if (options.showSongCount) {
                var songLine = "";
                item.SongCount && (songLine = 1 === item.SongCount ? globalize.translate("sharedcomponents#ValueOneSong") : globalize.translate("sharedcomponents#ValueSongCount", item.SongCount)), lines.push(songLine)
            }
            if (options.showPremiereDate)
                if (item.PremiereDate) try {
                    lines.push(getPremiereDateText(item))
                } catch (err) {
                    lines.push("")
                } else lines.push("");
            (options.showYear || options.showSeriesYear) && ("Series" === item.Type ? "Continuing" === item.Status ? lines.push(globalize.translate("sharedcomponents#SeriesYearToPresent", item.ProductionYear || "")) : item.EndDate && item.ProductionYear ? lines.push(item.ProductionYear + " - " + datetime.parseISO8601Date(item.EndDate).getFullYear()) : lines.push(item.ProductionYear || "") : lines.push(item.ProductionYear || "")), options.showRuntime && (item.RunTimeTicks ? lines.push(datetime.getDisplayRunningTime(item.RunTimeTicks)) : lines.push("")), options.showAirTime && lines.push(getAirTimeText(item, options.showAirDateTime, options.showAirEndTime) || ""), options.showChannelName && (item.ChannelId ? lines.push(getTextActionButton({
                Id: item.ChannelId,
                ServerId: item.ServerId,
                Name: item.ChannelName,
                Type: "TvChannel",
                MediaType: item.MediaType,
                IsFolder: !1
            }, item.ChannelName)) : lines.push(item.ChannelName || "&nbsp;")), options.showCurrentProgram && "TvChannel" === item.Type && (item.CurrentProgram ? lines.push(item.CurrentProgram.Name) : lines.push("")), options.showCurrentProgramTime && "TvChannel" === item.Type && (item.CurrentProgram ? lines.push(getAirTimeText(item.CurrentProgram, !1, !0) || "") : lines.push("")), options.showSeriesTimerTime && (item.RecordAnyTime ? lines.push(globalize.translate("sharedcomponents#Anytime")) : lines.push(datetime.getDisplayTime(item.StartDate))), options.showSeriesTimerChannel && (item.RecordAnyChannel ? lines.push(globalize.translate("sharedcomponents#AllChannels")) : lines.push(item.ChannelName || globalize.translate("sharedcomponents#OneChannel"))), options.showPersonRoleOrType && (item.Role ? lines.push("as " + item.Role) : item.Type ? lines.push(globalize.translate("sharedcomponents#" + item.Type)) : lines.push(""))
        }(showTitle || !imgUrl) && forceName && overlayText && 1 === lines.length && (lines = []);
        var addRightTextMargin = isOuterFooter && options.cardLayout && !options.centerText && "none" !== options.cardFooterAside && layoutManager.mobile;
        return html += getCardTextLines(lines, cssClass, !options.overlayText, isOuterFooter, options.cardLayout, addRightTextMargin, options.lines), progressHtml && (html += progressHtml), html && (!isOuterFooter || logoUrl || options.cardLayout) && (html = '<div class="' + footerClass + '">' + html, html += "</div>"), html
    }

    function getTextActionButton(item, text, serverId) {
        if (text || (text = itemHelper.getDisplayName(item)), layoutManager.tv) return text;
        var html = "<button " + itemShortcuts.getShortcutAttributesHtml(item, serverId) + ' type="button" class="itemAction textActionButton" data-action="link">';
        return html += text, html += "</button>"
    }

    function getItemCountsHtml(options, item) {
        var childText, counts = [];
        if ("Playlist" === item.Type) {
            if (childText = "", item.RunTimeTicks) {
                var minutes = item.RunTimeTicks / 6e8;
                minutes = minutes || 1, childText += globalize.translate("sharedcomponents#ValueMinutes", Math.round(minutes))
            } else childText += globalize.translate("sharedcomponents#ValueMinutes", 0);
            counts.push(childText)
        } else "Genre" === item.Type || "Studio" === item.Type ? (item.MovieCount && (childText = 1 === item.MovieCount ? globalize.translate("sharedcomponents#ValueOneMovie") : globalize.translate("sharedcomponents#ValueMovieCount", item.MovieCount), counts.push(childText)), item.SeriesCount && (childText = 1 === item.SeriesCount ? globalize.translate("sharedcomponents#ValueOneSeries") : globalize.translate("sharedcomponents#ValueSeriesCount", item.SeriesCount), counts.push(childText)), item.EpisodeCount && (childText = 1 === item.EpisodeCount ? globalize.translate("sharedcomponents#ValueOneEpisode") : globalize.translate("sharedcomponents#ValueEpisodeCount", item.EpisodeCount), counts.push(childText)), item.GameCount && (childText = 1 === item.GameCount ? globalize.translate("sharedcomponents#ValueOneGame") : globalize.translate("sharedcomponents#ValueGameCount", item.GameCount), counts.push(childText))) : "GameGenre" === item.Type ? item.GameCount && (childText = 1 === item.GameCount ? globalize.translate("sharedcomponents#ValueOneGame") : globalize.translate("sharedcomponents#ValueGameCount", item.GameCount), counts.push(childText)) : "MusicGenre" === item.Type || "MusicArtist" === options.context ? (item.AlbumCount && (childText = 1 === item.AlbumCount ? globalize.translate("sharedcomponents#ValueOneAlbum") : globalize.translate("sharedcomponents#ValueAlbumCount", item.AlbumCount), counts.push(childText)), item.SongCount && (childText = 1 === item.SongCount ? globalize.translate("sharedcomponents#ValueOneSong") : globalize.translate("sharedcomponents#ValueSongCount", item.SongCount), counts.push(childText)), item.MusicVideoCount && (childText = 1 === item.MusicVideoCount ? globalize.translate("sharedcomponents#ValueOneMusicVideo") : globalize.translate("sharedcomponents#ValueMusicVideoCount", item.MusicVideoCount), counts.push(childText))) : "Series" === item.Type && (childText = 1 === item.RecursiveItemCount ? globalize.translate("sharedcomponents#ValueOneEpisode") : globalize.translate("sharedcomponents#ValueEpisodeCount", item.RecursiveItemCount), counts.push(childText));
        return counts.join(", ")
    }

    function requireRefreshIndicator() {
        refreshIndicatorLoaded || (refreshIndicatorLoaded = !0, require(["emby-itemrefreshindicator"]))
    }

    function getDefaultBackgroundClass(str) {
        return "defaultCardBackground defaultCardBackground" + getDefaultColorIndex(str)
    }

    function buildCard(index, item, apiClient, options) {
        var action = options.action || "link";
        "play" === action && item.IsFolder ? action = "link" : "Photo" === item.MediaType && (action = "play");
        var shape = options.shape;
        if ("mixed" === shape) {
            shape = null;
            var primaryImageAspectRatio = item.PrimaryImageAspectRatio;
            primaryImageAspectRatio && (shape = primaryImageAspectRatio >= 1.33 ? "mixedBackdrop" : primaryImageAspectRatio > .71 ? "mixedSquare" : "mixedPortrait"), shape = shape || "mixedSquare"
        }
        var className = "card";
        shape && (className += " " + shape + "Card"), options.cardCssClass && (className += " " + options.cardCssClass), options.cardClass && (className += " " + options.cardClass), layoutManager.desktop && (className += " card-hoverable"), enableFocusTransfrom && layoutManager.tv || (className += " card-nofocustransform");
        var imgInfo = getCardImageUrl(item, apiClient, options, shape),
            imgUrl = imgInfo.imgUrl,
            forceName = imgInfo.forceName,
            showTitle = "auto" === options.showTitle || (options.showTitle || "PhotoAlbum" === item.Type || "Folder" === item.Type),
            overlayText = options.overlayText;
        forceName && !options.cardLayout && null == overlayText && (overlayText = !0);
        var cardImageContainerClass = "cardImageContainer";
        (options.coverImage || imgInfo.coverImage) && (cardImageContainerClass += " coveredImage", ("Photo" === item.MediaType || "PhotoAlbum" === item.Type || "Folder" === item.Type || item.ProgramInfo || "Program" === item.Type || "Recording" === item.Type) && (cardImageContainerClass += " coveredImage-noScale")), imgUrl || (cardImageContainerClass += " " + getDefaultBackgroundClass(item.Name));
        var cardBoxClass = options.cardLayout ? "cardBox visualCardBox" : "cardBox";
        layoutManager.tv && (cardBoxClass += enableFocusTransfrom ? " cardBox-focustransform cardBox-withfocuscontent" : " cardBox-withfocuscontent-large", options.cardLayout && (cardBoxClass += " card-focuscontent", enableFocusTransfrom || (cardBoxClass += " card-focuscontent-large")));
        var footerCssClass, logoUrl, progressHtml = indicators.getProgressBarHtml(item),
            innerCardFooter = "",
            footerOverlayed = !1;
        options.showChannelLogo && item.ChannelPrimaryImageTag ? logoUrl = apiClient.getScaledImageUrl(item.ChannelId, {
            type: "Primary",
            height: 40,
            tag: item.ChannelPrimaryImageTag
        }) : options.showLogo && item.ParentLogoImageTag && (logoUrl = apiClient.getScaledImageUrl(item.ParentLogoItemId, {
            type: "Logo",
            height: 40,
            tag: item.ParentLogoImageTag
        })), overlayText ? (logoUrl = null, footerCssClass = progressHtml ? "innerCardFooter fullInnerCardFooter" : "innerCardFooter", innerCardFooter += getCardFooterText(item, apiClient, options, showTitle, forceName, overlayText, imgUrl, footerCssClass, progressHtml, logoUrl, !1), footerOverlayed = !0) : progressHtml && (innerCardFooter += '<div class="innerCardFooter fullInnerCardFooter innerCardFooterClear">', innerCardFooter += progressHtml, innerCardFooter += "</div>", progressHtml = "");
        var mediaSourceCount = item.MediaSourceCount || 1;
        mediaSourceCount > 1 && (innerCardFooter += '<div class="mediaSourceIndicator">' + mediaSourceCount + "</div>");
        var outerCardFooter = "";
        overlayText || footerOverlayed || (footerCssClass = options.cardLayout ? "cardFooter" : "cardFooter cardFooter-transparent", logoUrl && (footerCssClass += " cardFooter-withlogo"), options.cardLayout || (logoUrl = null), outerCardFooter = getCardFooterText(item, apiClient, options, showTitle, forceName, overlayText, imgUrl, footerCssClass, progressHtml, logoUrl, !0)), outerCardFooter && !options.cardLayout && (cardBoxClass += " cardBox-bottompadded");
        var overlayButtons = "";
        if (layoutManager.mobile) {
            var overlayPlayButton = options.overlayPlayButton;
            null != overlayPlayButton || options.overlayMoreButton || options.overlayInfoButton || options.cardLayout || (overlayPlayButton = "Video" === item.MediaType);
            var btnCssClass = "cardOverlayButton cardOverlayButton-br itemAction";
            options.centerPlayButton && (overlayButtons += '<button is="paper-icon-button-light" class="' + btnCssClass + ' cardOverlayButton-centered" data-action="play"><i class="md-icon cardOverlayButtonIcon">&#xE037;</i></button>'), !overlayPlayButton || item.IsPlaceHolder || "Virtual" === item.LocationType && item.MediaType && "Program" !== item.Type || "Person" === item.Type || (overlayButtons += '<button is="paper-icon-button-light" class="' + btnCssClass + '" data-action="play"><i class="md-icon cardOverlayButtonIcon">&#xE037;</i></button>'), options.overlayMoreButton && (overlayButtons += '<button is="paper-icon-button-light" class="' + btnCssClass + '" data-action="menu"><i class="md-icon cardOverlayButtonIcon">&#xE5D3;</i></button>')
        }
        options.showChildCountIndicator && item.ChildCount && (className += " groupedCard");
        var cardImageContainerOpen, cardImageContainerClose = "",
            cardBoxClose = "",
            cardScalableClose = "",
            cardContentClass = "cardContent";
        options.cardLayout || (cardContentClass += " cardContent-shadow"), layoutManager.tv ? (cardImageContainerOpen = imgUrl ? '<div class="' + cardImageContainerClass + " " + cardContentClass + ' lazy" data-src="' + imgUrl + '">' : '<div class="' + cardImageContainerClass + " " + cardContentClass + '">', cardImageContainerClose = "</div>") : (cardImageContainerOpen = imgUrl ? '<button data-action="' + action + '" class="cardContent-button ' + cardImageContainerClass + " " + cardContentClass + ' itemAction lazy" data-src="' + imgUrl + '">' : '<button data-action="' + action + '" class="cardContent-button ' + cardImageContainerClass + " " + cardContentClass + ' itemAction">', cardImageContainerClose = "</button>");
        var cardScalableClass = "cardScalable";
        layoutManager.tv && !options.cardLayout && (cardScalableClass += " card-focuscontent", enableFocusTransfrom || (cardScalableClass += " card-focuscontent-large")), cardImageContainerOpen = '<div class="' + cardBoxClass + '"><div class="' + cardScalableClass + '"><div class="cardPadder-' + shape + '"></div>' + cardImageContainerOpen, cardBoxClose = "</div>", cardScalableClose = "</div>";
        var indicatorsHtml = "";
        if (!1 !== options.missingIndicator && (indicatorsHtml += indicators.getMissingIndicator(item)), indicatorsHtml += indicators.getSyncIndicator(item), indicatorsHtml += indicators.getTimerIndicator(item), indicatorsHtml += indicators.getTypeIndicator(item), options.showGroupCount ? indicatorsHtml += indicators.getChildCountIndicatorHtml(item, {
                minCount: 1
            }) : indicatorsHtml += indicators.getPlayedIndicatorHtml(item), "CollectionFolder" === item.Type || item.CollectionType) {
            indicatorsHtml += '<div is="emby-itemrefreshindicator"' + (item.RefreshProgress || item.RefreshStatus && "Idle" !== virtualFolder.item ? "" : ' class="hide"') + ' data-progress="' + (item.RefreshProgress || 0) + '" data-status="' + item.RefreshStatus + '"></div>', requireRefreshIndicator()
        }
        indicatorsHtml && (cardImageContainerOpen += '<div class="cardIndicators">' + indicatorsHtml + "</div>"), imgUrl || (cardImageContainerOpen += getCardDefaultText(item, options));
        var tagName = layoutManager.tv && !overlayButtons ? "button" : "div",
            nameWithPrefix = item.SortName || item.Name || "",
            prefix = nameWithPrefix.substring(0, Math.min(3, nameWithPrefix.length));
        prefix && (prefix = prefix.toUpperCase());
        var timerAttributes = "";
        item.TimerId && (timerAttributes += ' data-timerid="' + item.TimerId + '"'), item.SeriesTimerId && (timerAttributes += ' data-seriestimerid="' + item.SeriesTimerId + '"');
        var actionAttribute;
        "button" === tagName ? (className += " itemAction", actionAttribute = ' data-action="' + action + '"') : actionAttribute = "", "MusicAlbum" !== item.Type && "MusicArtist" !== item.Type && "Audio" !== item.Type && (className += " card-withuserdata");
        var positionTicksData = item.UserData && item.UserData.PlaybackPositionTicks ? ' data-positionticks="' + item.UserData.PlaybackPositionTicks + '"' : "",
            collectionIdData = options.collectionId ? ' data-collectionid="' + options.collectionId + '"' : "",
            playlistIdData = options.playlistId ? ' data-playlistid="' + options.playlistId + '"' : "",
            mediaTypeData = item.MediaType ? ' data-mediatype="' + item.MediaType + '"' : "",
            collectionTypeData = item.CollectionType ? ' data-collectiontype="' + item.CollectionType + '"' : "",
            channelIdData = item.ChannelId ? ' data-channelid="' + item.ChannelId + '"' : "",
            contextData = options.context ? ' data-context="' + options.context + '"' : "",
            parentIdData = options.parentId ? ' data-parentid="' + options.parentId + '"' : "",
            additionalCardContent = "";
        return layoutManager.desktop && (additionalCardContent += getHoverMenuHtml(item, action)), "<" + tagName + ' data-index="' + index + '"' + timerAttributes + actionAttribute + ' data-isfolder="' + (item.IsFolder || !1) + '" data-serverid="' + (item.ServerId || options.serverId) + '" data-id="' + (item.Id || item.ItemId) + '" data-type="' + item.Type + '"' + mediaTypeData + collectionTypeData + channelIdData + positionTicksData + collectionIdData + playlistIdData + contextData + parentIdData + ' data-prefix="' + prefix + '" class="' + className + '">' + cardImageContainerOpen + innerCardFooter + cardImageContainerClose + overlayButtons + additionalCardContent + cardScalableClose + outerCardFooter + cardBoxClose + "</" + tagName + ">"
    }

    function getHoverMenuHtml(item, action) {
        var html = "";
        html += '<div class="cardOverlayContainer itemAction" data-action="' + action + '">';
        var btnCssClass = "cardOverlayButton cardOverlayButton-hover itemAction";
        playbackManager.canPlay(item) && (html += '<button is="paper-icon-button-light" class="' + btnCssClass + ' cardOverlayFab-primary" data-action="resume"><i class="md-icon cardOverlayButtonIcon">&#xE037;</i></button>'), html += '<div class="cardOverlayButton-br">';
        var userData = item.UserData || {};
        if (itemHelper.canMarkPlayed(item) && (require(["emby-playstatebutton"]), html += '<button is="emby-playstatebutton" type="button" data-action="none" class="' + btnCssClass + '" data-id="' + item.Id + '" data-serverid="' + item.ServerId + '" data-itemtype="' + item.Type + '" data-played="' + userData.Played + '"><i class="md-icon cardOverlayButtonIcon cardOverlayButtonIcon-hover">&#xE5CA;</i></button>'), itemHelper.canRate(item)) {
            var likes = null == userData.Likes ? "" : userData.Likes;
            require(["emby-ratingbutton"]), html += '<button is="emby-ratingbutton" type="button" data-action="none" class="' + btnCssClass + '" data-id="' + item.Id + '" data-serverid="' + item.ServerId + '" data-itemtype="' + item.Type + '" data-likes="' + likes + '" data-isfavorite="' + userData.IsFavorite + '"><i class="md-icon cardOverlayButtonIcon cardOverlayButtonIcon-hover">&#xE87D;</i></button>'
        }
        return html += '<button is="paper-icon-button-light" class="' + btnCssClass + '" data-action="menu"><i class="md-icon cardOverlayButtonIcon cardOverlayButtonIcon-hover">&#xE5D3;</i></button>', html += "</div>", html += "</div>"
    }

    function getCardDefaultText(item, options) {
        var collectionType = item.CollectionType;
        return "livetv" === collectionType ? '<i class="cardImageIcon md-icon">&#xE1B2;</i>' : "homevideos" === collectionType || "photos" === collectionType ? '<i class="cardImageIcon md-icon">&#xE412;</i>' : "music" === collectionType ? '<i class="cardImageIcon md-icon">&#xE310;</i>' : "MusicAlbum" === item.Type ? '<i class="cardImageIcon md-icon">&#xE019;</i>' : "MusicArtist" === item.Type || "Person" === item.Type ? '<i class="cardImageIcon md-icon">&#xE7FD;</i>' : options.defaultCardImageIcon ? '<i class="cardImageIcon md-icon">' + options.defaultCardImageIcon + "</i>" : '<div class="cardText cardDefaultText">' + (isUsingLiveTvNaming(item) ? item.Name : itemHelper.getDisplayName(item)) + "</div>"
    }

    function buildCards(items, options) {
        if (document.body.contains(options.itemsContainer)) {
            if (options.parentContainer) {
                if (!items.length) return void options.parentContainer.classList.add("hide");
                options.parentContainer.classList.remove("hide")
            }
            var html = buildCardsHtmlInternal(items, options);
            html ? (options.itemsContainer.cardBuilderHtml !== html && (options.itemsContainer.innerHTML = html, items.length < 50 ? options.itemsContainer.cardBuilderHtml = html : options.itemsContainer.cardBuilderHtml = null), imageLoader.lazyChildren(options.itemsContainer)) : (options.itemsContainer.innerHTML = html, options.itemsContainer.cardBuilderHtml = null), options.autoFocus && focusManager.autoFocus(options.itemsContainer, !0)
        }
    }

    function ensureIndicators(card, indicatorsElem) {
        if (indicatorsElem) return indicatorsElem;
        if (!(indicatorsElem = card.querySelector(".cardIndicators"))) {
            var cardImageContainer = card.querySelector(".cardImageContainer");
            indicatorsElem = document.createElement("div"),
                indicatorsElem.classList.add("cardIndicators"), cardImageContainer.appendChild(indicatorsElem)
        }
        return indicatorsElem
    }

    function updateUserData(card, userData) {
        var type = card.getAttribute("data-type"),
            enableCountIndicator = "Series" === type || "BoxSet" === type || "Season" === type,
            indicatorsElem = null,
            playedIndicator = null,
            countIndicator = null,
            itemProgressBar = null;
        userData.Played ? (playedIndicator = card.querySelector(".playedIndicator"), playedIndicator || (playedIndicator = document.createElement("div"), playedIndicator.classList.add("playedIndicator"), playedIndicator.classList.add("indicator"), indicatorsElem = ensureIndicators(card, indicatorsElem), indicatorsElem.appendChild(playedIndicator)), playedIndicator.innerHTML = '<i class="md-icon indicatorIcon">&#xE5CA;</i>') : (playedIndicator = card.querySelector(".playedIndicator")) && playedIndicator.parentNode.removeChild(playedIndicator), userData.UnplayedItemCount ? (countIndicator = card.querySelector(".countIndicator"), countIndicator || (countIndicator = document.createElement("div"), countIndicator.classList.add("countIndicator"), indicatorsElem = ensureIndicators(card, indicatorsElem), indicatorsElem.appendChild(countIndicator)), countIndicator.innerHTML = userData.UnplayedItemCount) : enableCountIndicator && (countIndicator = card.querySelector(".countIndicator")) && countIndicator.parentNode.removeChild(countIndicator);
        var progressHtml = indicators.getProgressBarHtml({
            Type: type,
            UserData: userData,
            MediaType: "Video"
        });
        if (progressHtml) {
            if (!(itemProgressBar = card.querySelector(".itemProgressBar"))) {
                itemProgressBar = document.createElement("div"), itemProgressBar.classList.add("itemProgressBar");
                var innerCardFooter = card.querySelector(".innerCardFooter");
                if (!innerCardFooter) {
                    innerCardFooter = document.createElement("div"), innerCardFooter.classList.add("innerCardFooter");
                    card.querySelector(".cardImageContainer").appendChild(innerCardFooter)
                }
                innerCardFooter.appendChild(itemProgressBar)
            }
            itemProgressBar.innerHTML = progressHtml
        } else(itemProgressBar = card.querySelector(".itemProgressBar")) && itemProgressBar.parentNode.removeChild(itemProgressBar)
    }

    function onUserDataChanged(userData, scope) {
        for (var cards = (scope || document.body).querySelectorAll('.card-withuserdata[data-id="' + userData.ItemId + '"]'), i = 0, length = cards.length; i < length; i++) updateUserData(cards[i], userData)
    }

    function onTimerCreated(programId, newTimerId, itemsContainer) {
        for (var cells = itemsContainer.querySelectorAll('.card[data-id="' + programId + '"]'), i = 0, length = cells.length; i < length; i++) {
            var cell = cells[i];
            if (!cell.querySelector(".timerIndicator")) {
                ensureIndicators(cell).insertAdjacentHTML("beforeend", '<i class="md-icon timerIndicator indicatorIcon">&#xE061;</i>')
            }
            cell.setAttribute("data-timerid", newTimerId)
        }
    }

    function onTimerCancelled(id, itemsContainer) {
        for (var cells = itemsContainer.querySelectorAll('.card[data-timerid="' + id + '"]'), i = 0, length = cells.length; i < length; i++) {
            var cell = cells[i],
                icon = cell.querySelector(".timerIndicator");
            icon && icon.parentNode.removeChild(icon), cell.removeAttribute("data-timerid")
        }
    }

    function onSeriesTimerCancelled(id, itemsContainer) {
        for (var cells = itemsContainer.querySelectorAll('.card[data-seriestimerid="' + id + '"]'), i = 0, length = cells.length; i < length; i++) {
            var cell = cells[i],
                icon = cell.querySelector(".timerIndicator");
            icon && icon.parentNode.removeChild(icon), cell.removeAttribute("data-seriestimerid")
        }
    }
    var refreshIndicatorLoaded, enableFocusTransfrom = (window.devicePixelRatio, !browser.slow && !browser.edge),
        numRandomColors = 5;
    return {
        getCardsHtml: getCardsHtml,
        buildCards: buildCards,
        onUserDataChanged: onUserDataChanged,
        onTimerCreated: onTimerCreated,
        onTimerCancelled: onTimerCancelled,
        onSeriesTimerCancelled: onSeriesTimerCancelled
    }
});