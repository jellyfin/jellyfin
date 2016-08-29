define(['datetime', 'imageLoader', 'connectionManager', 'itemHelper', 'mediaInfo', 'focusManager', 'indicators', 'globalize', 'layoutManager', 'apphost', 'dom', 'emby-button', 'css!./card', 'paper-icon-button-light', 'clearButtonStyle'],
    function (datetime, imageLoader, connectionManager, itemHelper, mediaInfo, focusManager, indicators, globalize, layoutManager, appHost, dom) {

        // Regular Expressions for parsing tags and attributes
        var SURROGATE_PAIR_REGEXP = /[\uD800-\uDBFF][\uDC00-\uDFFF]/g,
          // Match everything outside of normal chars and " (quote character)
          NON_ALPHANUMERIC_REGEXP = /([^\#-~| |!])/g;

        /**
         * Escapes all potentially dangerous characters, so that the
         * resulting string can be safely inserted into attribute or
         * element text.
         * @param value
         * @returns {string} escaped text
         */
        function htmlEncode(value) {
            return value.
              replace(/&/g, '&amp;').
              replace(SURROGATE_PAIR_REGEXP, function (value) {
                  var hi = value.charCodeAt(0);
                  var low = value.charCodeAt(1);
                  return '&#' + (((hi - 0xD800) * 0x400) + (low - 0xDC00) + 0x10000) + ';';
              }).
              replace(NON_ALPHANUMERIC_REGEXP, function (value) {
                  return '&#' + value.charCodeAt(0) + ';';
              }).
              replace(/</g, '&lt;').
              replace(/>/g, '&gt;');
        }

        function getCardsHtml(items, options) {

            var apiClient = connectionManager.currentApiClient();

            if (arguments.length == 1) {

                options = arguments[0];
                items = options.items;
            }

            var html = buildCardsHtmlInternal(items, apiClient, options);

            return html;
        }

        function getPostersPerRow(shape, screenWidth) {

            switch (shape) {

                case 'portrait':
                    if (screenWidth >= 2200) return 10;
                    if (screenWidth >= 2100) return 9;
                    if (screenWidth >= 1600) return 8;
                    if (screenWidth >= 1400) return 7;
                    if (screenWidth >= 1200) return 6;
                    if (screenWidth >= 800) return 5;
                    if (screenWidth >= 640) return 4;
                    return 3;
                case 'square':
                    if (screenWidth >= 2100) return 9;
                    if (screenWidth >= 1800) return 8;
                    if (screenWidth >= 1400) return 7;
                    if (screenWidth >= 1200) return 6;
                    if (screenWidth >= 900) return 5;
                    if (screenWidth >= 700) return 4;
                    if (screenWidth >= 500) return 3;
                    return 2;
                case 'banner':
                    if (screenWidth >= 2200) return 4;
                    if (screenWidth >= 1200) return 3;
                    if (screenWidth >= 800) return 2;
                    return 1;
                case 'backdrop':
                    if (screenWidth >= 2500) return 6;
                    if (screenWidth >= 2100) return 5;
                    if (screenWidth >= 1200) return 4;
                    if (screenWidth >= 770) return 3;
                    if (screenWidth >= 420) return 2;
                    return 1;
                case 'smallBackdrop':
                    if (screenWidth >= 1440) return 8;
                    if (screenWidth >= 1100) return 6;
                    if (screenWidth >= 800) return 5;
                    if (screenWidth >= 600) return 4;
                    if (screenWidth >= 540) return 3;
                    if (screenWidth >= 420) return 2;
                    return 1;
                case 'overflowPortrait':
                    if (screenWidth >= 1000) return 100 / 23;
                    if (screenWidth >= 640) return 100 / 36;
                    return 2.5;
                case 'overflowSquare':
                    if (screenWidth >= 1000) return 100 / 22;
                    if (screenWidth >= 640) return 100 / 30;
                    return 100 / 42;
                case 'overflowBackdrop':
                    if (screenWidth >= 1000) return 100 / 40;
                    if (screenWidth >= 640) return 100 / 60;
                    return 100 / 84;
                default:
                    return 4;
            }
        }

        function isResizable(windowWidth) {

            var screen = window.screen;
            if (screen) {
                var screenWidth = screen.availWidth;

                if ((screenWidth - windowWidth) > 20) {
                    return true;
                }
            }

            return false;
        }

        function getImageWidth(shape) {

            var screenWidth = dom.getWindowSize().innerWidth;

            if (isResizable(screenWidth)) {
                var roundScreenTo = 100;
                screenWidth = Math.ceil(screenWidth / roundScreenTo) * roundScreenTo;
            }

            if (window.screen) {
                screenWidth = Math.min(screenWidth, screen.availWidth || screenWidth);
            }

            var imagesPerRow = getPostersPerRow(shape, screenWidth);

            var shapeWidth = screenWidth / imagesPerRow;

            return Math.round(shapeWidth);
        }

        function setCardData(items, options) {

            options.shape = options.shape || "auto";

            var primaryImageAspectRatio = imageLoader.getPrimaryImageAspectRatio(items);

            var isThumbAspectRatio = primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 1.777777778) < .3;
            var isSquareAspectRatio = primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 1) < .33 ||
                primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 1.3333334) < .01;

            if (options.shape == 'auto' || options.shape == 'autohome' || options.shape == 'autooverflow' || options.shape == 'autoVertical') {

                if (options.preferThumb || isThumbAspectRatio) {
                    options.shape = options.shape == 'autooverflow' ? 'overflowBackdrop' : 'backdrop';
                } else if (isSquareAspectRatio) {
                    options.coverImage = true;
                    options.shape = options.shape == 'autooverflow' ? 'overflowSquare' : 'square';
                } else if (primaryImageAspectRatio && primaryImageAspectRatio > 1.9) {
                    options.shape = 'banner';
                    options.coverImage = true;
                } else if (primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 0.6666667) < .2) {
                    options.shape = options.shape == 'autooverflow' ? 'overflowPortrait' : 'portrait';
                } else {
                    options.shape = options.defaultShape || (options.shape == 'autooverflow' ? 'overflowSquare' : 'square');
                }
            }

            options.uiAspect = getDesiredAspect(options.shape);
            options.primaryImageAspectRatio = primaryImageAspectRatio;

            if (!options.width && options.widths) {
                options.width = options.widths[options.shape];
            }

            if (options.rows && typeof (options.rows) !== 'number') {
                options.rows = options.rows[options.shape];
            }

            if (layoutManager.tv) {
                if (options.shape == 'backdrop') {
                    options.width = options.width || 500;
                }
                else if (options.shape == 'portrait') {
                    options.width = options.width || 243;
                }
                else if (options.shape == 'square') {
                    options.width = options.width || 243;
                }
            }

            options.width = options.width || getImageWidth(options.shape);
        }

        function buildCardsHtmlInternal(items, apiClient, options) {

            var isVertical;

            if (options.shape == 'autoVertical') {
                isVertical = true;
            }

            setCardData(items, options);

            if (options.indexBy == 'Genres') {
                return buildCardsByGenreHtmlInternal(items, apiClient, options);
            }

            var className = 'card';

            if (options.shape) {
                className += ' ' + options.shape + 'Card';
            }

            var html = '';
            var itemsInRow = 0;

            var currentIndexValue;
            var hasOpenRow;
            var hasOpenSection;

            var sectionTitleTagName = options.sectionTitleTagName || 'div';

            for (var i = 0, length = items.length; i < length; i++) {

                var item = items[i];

                if (options.indexBy) {
                    var newIndexValue = '';

                    if (options.indexBy == 'PremiereDate') {
                        if (item.PremiereDate) {
                            try {

                                newIndexValue = getDisplayDateText(datetime.parseISO8601Date(item.PremiereDate));

                            } catch (err) {
                            }
                        }
                    }

                    else if (options.indexBy == 'Genres') {
                        newIndexValue = item.Name;
                    }

                    else if (options.indexBy == 'ProductionYear') {
                        newIndexValue = item.ProductionYear;
                    }

                    else if (options.indexBy == 'CommunityRating') {
                        newIndexValue = item.CommunityRating ? (Math.floor(item.CommunityRating) + (item.CommunityRating % 1 >= .5 ? .5 : 0)) + '+' : null;
                    }

                    if (newIndexValue != currentIndexValue) {

                        if (hasOpenRow) {
                            html += '</div>';
                            hasOpenRow = false;
                            itemsInRow = 0;
                        }

                        if (hasOpenSection) {

                            html += '</div>';

                            if (isVertical) {
                                html += '</div>';
                            }
                            hasOpenSection = false;
                        }

                        if (isVertical) {
                            html += '<div class="verticalSection">';
                        } else {
                            html += '<div class="horizontalSection">';
                        }
                        html += '<' + sectionTitleTagName + ' class="sectionTitle">' + newIndexValue + '</' + sectionTitleTagName + '>';
                        if (isVertical) {
                            html += '<div class="itemsContainer vertical-wrap">';
                        }
                        currentIndexValue = newIndexValue;
                        hasOpenSection = true;
                    }
                }

                if (options.rows && itemsInRow == 0) {

                    if (hasOpenRow) {
                        html += '</div>';
                        hasOpenRow = false;
                    }

                    html += '<div class="cardColumn">';
                    hasOpenRow = true;
                }

                var cardClass = className;
                html += buildCard(i, item, apiClient, options, cardClass);

                itemsInRow++;

                if (options.rows && itemsInRow >= options.rows) {
                    html += '</div>';
                    hasOpenRow = false;
                    itemsInRow = 0;
                }
            }

            if (hasOpenRow) {
                html += '</div>';
            }

            if (hasOpenSection) {
                html += '</div>';

                if (isVertical) {
                    html += '</div>';
                }
            }

            return html;
        }

        function buildCardsByGenreHtmlInternal(items, apiClient, options) {

            var className = 'card';

            if (options.shape) {
                className += ' ' + options.shape + 'Card';
            }

            var html = '';

            var loopItems = options.genres;

            for (var i = 0, length = loopItems.length; i < length; i++) {

                var item = loopItems[i];

                var genreLower = item.Name.toLowerCase();
                var renderItems = items.filter(function (currentItem) {

                    return currentItem.Genres.filter(function (g) {

                        return g.toLowerCase() == genreLower;

                    }).length > 0;
                });

                if (!renderItems.length) {
                    continue;
                }

                html += '<div class="horizontalSection focuscontainer-down">';
                html += '<div class="sectionTitle">' + item.Name + '</div>';

                var showMoreButton = false;
                if (renderItems.length > options.indexLimit) {
                    renderItems.length = Math.min(renderItems.length, options.indexLimit);
                    showMoreButton = true;
                }

                var itemsInRow = 0;
                var hasOpenRow = false;

                html += renderItems.map(function (renderItem) {

                    var currentItemHtml = '';

                    if (options.rows && itemsInRow == 0) {

                        if (hasOpenRow) {
                            currentItemHtml += '</div>';
                            hasOpenRow = false;
                        }

                        currentItemHtml += '<div class="cardColumn">';
                        hasOpenRow = true;
                    }

                    var cardClass = className;
                    currentItemHtml += buildCard(i, renderItem, apiClient, options, cardClass);

                    itemsInRow++;

                    if (options.rows && itemsInRow >= options.rows) {
                        currentItemHtml += '</div>';
                        hasOpenRow = false;
                        itemsInRow = 0;
                    }

                    return currentItemHtml;

                }).join('');


                if (showMoreButton) {
                    html += '<div class="listItemsMoreButtonContainer">';
                    html += '<button is="emby-button" class="listItemsMoreButton raised" data-parentid="' + options.parentId + '" data-indextype="Genres" data-indexvalue="' + item.Id + '">' + globalize.translate('sharedcomponents#More') + '</button>';
                    html += '</div>';
                }

                html += '</div>';
                html += '</div>';
            }

            return html;
        }

        function getDisplayDateText(date) {

            var weekday = [];
            weekday[0] = globalize.translate('sharedcomponents#Sunday');
            weekday[1] = globalize.translate('sharedcomponents#Monday');
            weekday[2] = globalize.translate('sharedcomponents#Tuesday');
            weekday[3] = globalize.translate('sharedcomponents#Wednesday');
            weekday[4] = globalize.translate('sharedcomponents#Thursday');
            weekday[5] = globalize.translate('sharedcomponents#Friday');
            weekday[6] = globalize.translate('sharedcomponents#Saturday');

            var day = weekday[date.getDay()];
            date = date.toLocaleDateString();

            if (date.toLowerCase().indexOf(day.toLowerCase()) == -1) {
                return day + " " + date;
            }

            return date;
        }

        function getDesiredAspect(shape) {

            if (shape) {
                shape = shape.toLowerCase();
                if (shape.indexOf('portrait') != -1) {
                    return (2 / 3);
                }
                if (shape.indexOf('backdrop') != -1) {
                    return (16 / 9);
                }
                if (shape.indexOf('square') != -1) {
                    return 1;
                }
            }
            return null;
        }

        function getCardImageUrl(item, apiClient, options) {

            var imageItem = item.ProgramInfo || item;
            item = imageItem;

            var width = options.width;
            var height = null;
            var primaryImageAspectRatio = imageLoader.getPrimaryImageAspectRatio([item]);
            var forceName = false;
            var imgUrl = null;
            var coverImage = false;

            if (options.autoThumb && item.ImageTags && item.ImageTags.Primary && item.PrimaryImageAspectRatio && item.PrimaryImageAspectRatio >= 1.34) {

                height = primaryImageAspectRatio ? Math.round(width / primaryImageAspectRatio) : null;

                imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                    type: "Primary",
                    maxHeight: height,
                    maxWidth: width,
                    tag: item.ImageTags.Primary
                });

                if (primaryImageAspectRatio) {
                    if (uiAspect) {
                        if (Math.abs(primaryImageAspectRatio - uiAspect) <= .2) {
                            coverImage = true;
                        }
                    }
                }

            } else if (options.autoThumb && item.ImageTags && item.ImageTags.Thumb) {

                imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                    type: "Thumb",
                    maxWidth: width,
                    tag: item.ImageTags.Thumb
                });

            } else if (options.preferThumb && item.ImageTags && item.ImageTags.Thumb) {

                imgUrl = apiClient.getScaledImageUrl(item.Id, {
                    type: "Thumb",
                    maxWidth: width,
                    tag: item.ImageTags.Thumb
                });

            } else if (options.preferBanner && item.ImageTags && item.ImageTags.Banner) {

                imgUrl = apiClient.getScaledImageUrl(item.Id, {
                    type: "Banner",
                    maxWidth: width,
                    tag: item.ImageTags.Banner
                });

            } else if (options.preferThumb && item.SeriesThumbImageTag && options.inheritThumb !== false) {

                imgUrl = apiClient.getScaledImageUrl(item.SeriesId, {
                    type: "Thumb",
                    maxWidth: width,
                    tag: item.SeriesThumbImageTag
                });

            } else if (options.preferThumb && item.ParentThumbItemId && options.inheritThumb !== false) {

                imgUrl = apiClient.getScaledImageUrl(item.ParentThumbItemId, {
                    type: "Thumb",
                    maxWidth: width,
                    tag: item.ParentThumbImageTag
                });

            } else if (options.preferThumb && item.BackdropImageTags && item.BackdropImageTags.length) {

                imgUrl = apiClient.getScaledImageUrl(item.Id, {
                    type: "Backdrop",
                    maxWidth: width,
                    tag: item.BackdropImageTags[0]
                });

                forceName = true;

            } else if (item.ImageTags && item.ImageTags.Primary) {

                height = width && primaryImageAspectRatio ? Math.round(width / primaryImageAspectRatio) : null;

                imgUrl = apiClient.getScaledImageUrl(item.Id, {
                    type: "Primary",
                    maxHeight: height,
                    maxWidth: width,
                    tag: item.ImageTags.Primary
                });

                if (options.preferThumb && options.showTitle !== false) {
                    forceName = true;
                }

                if (primaryImageAspectRatio) {
                    var uiAspect = getDesiredAspect(options.shape);
                    if (uiAspect) {
                        coverImage = Math.abs(primaryImageAspectRatio - uiAspect) <= .2;
                    }
                }

            } else if (item.PrimaryImageTag) {

                height = width && primaryImageAspectRatio ? Math.round(width / primaryImageAspectRatio) : null;

                imgUrl = apiClient.getScaledImageUrl(item.Id || item.ItemId, {
                    type: "Primary",
                    maxHeight: height,
                    maxWidth: width,
                    tag: item.PrimaryImageTag
                });

                if (options.preferThumb && options.showTitle !== false) {
                    forceName = true;
                }

                if (primaryImageAspectRatio) {
                    var uiAspect = getDesiredAspect(options.shape);
                    if (uiAspect) {
                        coverImage = Math.abs(primaryImageAspectRatio - uiAspect) <= .2;
                    }
                }
            }
            else if (item.ParentPrimaryImageTag) {

                imgUrl = apiClient.getScaledImageUrl(item.ParentPrimaryImageItemId, {
                    type: "Primary",
                    maxWidth: width,
                    tag: item.ParentPrimaryImageTag
                });
            }
            else if (item.AlbumId && item.AlbumPrimaryImageTag) {

                width = primaryImageAspectRatio ? Math.round(height * primaryImageAspectRatio) : null;

                imgUrl = apiClient.getScaledImageUrl(item.AlbumId, {
                    type: "Primary",
                    maxHeight: height,
                    maxWidth: width,
                    tag: item.AlbumPrimaryImageTag
                });

                if (primaryImageAspectRatio) {
                    var uiAspect = getDesiredAspect(options.shape);
                    if (uiAspect) {
                        coverImage = Math.abs(primaryImageAspectRatio - uiAspect) <= .2;
                    }
                }
            }
            else if (item.Type == 'Season' && item.ImageTags && item.ImageTags.Thumb) {

                imgUrl = apiClient.getScaledImageUrl(item.Id, {
                    type: "Thumb",
                    maxWidth: width,
                    tag: item.ImageTags.Thumb
                });

            }
            else if (item.BackdropImageTags && item.BackdropImageTags.length) {

                imgUrl = apiClient.getScaledImageUrl(item.Id, {
                    type: "Backdrop",
                    maxWidth: width,
                    tag: item.BackdropImageTags[0]
                });

            } else if (item.ImageTags && item.ImageTags.Thumb) {

                imgUrl = apiClient.getScaledImageUrl(item.Id, {
                    type: "Thumb",
                    maxWidth: width,
                    tag: item.ImageTags.Thumb
                });

            } else if (item.SeriesThumbImageTag) {

                imgUrl = apiClient.getScaledImageUrl(item.SeriesId, {
                    type: "Thumb",
                    maxWidth: width,
                    tag: item.SeriesThumbImageTag
                });

            } else if (item.ParentThumbItemId) {

                imgUrl = apiClient.getThumbImageUrl(item.ParentThumbItemId, {
                    type: "Thumb",
                    maxWidth: width,
                    tag: item.ParentThumbImageTag
                });

            }

            return {
                imgUrl: imgUrl,
                forceName: forceName,
                coverImage: coverImage
            };
        }

        function getRandomInt(min, max) {
            return Math.floor(Math.random() * (max - min + 1)) + min;
        }

        var numRandomColors = 5;
        function getDefaultColorIndex(str) {

            if (str) {
                var charIndex = Math.floor(str.length / 2);
                var character = String(str.substr(charIndex, 1).charCodeAt());
                var sum = 0;
                for (var i = 0; i < character.length; i++) {
                    sum += parseInt(character.charAt(i));
                }
                var index = String(sum).substr(-1);

                return (index % numRandomColors) + 1;
            } else {
                return getRandomInt(1, numRandomColors);
            }
        }

        function getDefaultColorClass(str) {
            return 'defaultCardColor' + getDefaultColorIndex(str);
        }

        function getCardTextLines(lines, cssClass, forceLines, addSecondaryClass) {

            var html = '';

            var valid = 0;
            var i, length;

            for (i = 0, length = lines.length; i < length; i++) {

                var text = lines[i];

                if (i == 1 && addSecondaryClass) {
                    cssClass += ' cardText-secondary';
                }

                if (text) {
                    html += "<div class='" + cssClass + "'>";
                    html += text;
                    html += "</div>";
                    valid++;
                }
            }

            if (forceLines) {
                while (valid < length) {
                    html += "<div class='" + cssClass + "'>&nbsp;</div>";
                    valid++;
                }
            }

            return html;
        }

        function getCardFooterText(item, options, showTitle, forceName, overlayText, imgUrl, footerClass, progressHtml, isOuterFooter) {

            var html = '';

            var showOtherText = isOuterFooter ? !overlayText : overlayText;

            if (isOuterFooter && options.cardLayout && !layoutManager.tv) {
                var moreIcon = appHost.moreIcon == 'dots-horiz' ? '&#xE5D3;' : '&#xE5D4;';
                html += '<button is="paper-icon-button-light" class="itemAction btnCardOptions autoSize" data-action="menu"><i class="md-icon">' + moreIcon + '</i></button>';
            }

            var cssClass = options.centerText && !options.cardLayout ? "cardText cardTextCentered" : "cardText";

            var lines = [];

            if (showOtherText) {
                var parentTitleUnderneath = item.Type == 'MusicAlbum' || item.Type == 'Audio' || item.Type == 'MusicVideo';
                if ((options.showParentTitle || options.showParentTitleOrTitle) && !parentTitleUnderneath) {

                    if (isOuterFooter && item.Type == 'Episode' && item.SeriesName && item.SeriesId) {

                        lines.push(getTextActionButton({
                            Id: item.SeriesId,
                            Name: item.SeriesName,
                            Type: 'Series',
                            IsFolder: true
                        }));
                    }
                    else {

                        var parentTitle = item.EpisodeTitle ? item.Name : (item.SeriesName || item.Album || item.AlbumArtist || item.GameSystem || "");

                        if (parentTitle || options.showParentTitle) {
                            lines.push(parentTitle);
                        }
                    }
                }
            }

            if (showTitle || forceName || (options.showParentTitleOrTitle && !lines.length)) {

                var name = options.showTitle == 'auto' && !item.IsFolder && item.MediaType == 'Photo' ? '' : itemHelper.getDisplayName(item);

                lines.push(htmlEncode(name));
            }

            if (showOtherText) {
                if (options.showParentTitle && parentTitleUnderneath) {

                    if (isOuterFooter && item.AlbumArtists && item.AlbumArtists.length) {
                        item.AlbumArtists[0].Type = 'MusicArtist';
                        item.AlbumArtists[0].IsFolder = true;
                        lines.push(getTextActionButton(item.AlbumArtists[0]));
                    } else {
                        lines.push(item.EpisodeTitle ? item.Name : (item.SeriesName || item.Album || item.AlbumArtist || item.GameSystem || ""));
                    }
                }

                if (options.showItemCounts) {

                    var itemCountHtml = getItemCountsHtml(options, item);

                    lines.push(itemCountHtml);
                }

                if (options.textLines) {
                    var additionalLines = options.textLines(item);
                    for (var i = 0, length = additionalLines.length; i < length; i++) {
                        lines.push(additionalLines[i]);
                    }
                }

                if (options.showSongCount) {

                    var songLine = '';

                    if (item.SongCount) {
                        songLine = item.SongCount == 1 ?
                        globalize.translate('sharedcomponents#ValueOneSong') :
                        globalize.translate('sharedcomponents#ValueSongCount', item.SongCount);
                    }

                    lines.push(songLine);
                }

                if (options.showPremiereDate) {

                    if (item.PremiereDate) {
                        try {

                            lines.push(getPremiereDateText(item));

                        } catch (err) {
                            lines.push('');

                        }
                    } else {
                        lines.push('');
                    }
                }

                if (options.showYear) {

                    lines.push(item.ProductionYear || '');
                }

                if (options.showChannelName) {

                    lines.push(item.ChannelName || '');
                }

                if (options.showAirTime) {

                    var airTimeText;
                    if (item.StartDate) {

                        try {
                            var date = datetime.parseISO8601Date(item.StartDate);

                            airTimeText = date.toLocaleDateString();

                            airTimeText += ', ' + datetime.getDisplayTime(date);

                            if (item.EndDate) {
                                date = datetime.parseISO8601Date(item.EndDate);
                                airTimeText += ' - ' + datetime.getDisplayTime(date);
                            }
                        }
                        catch (e) {
                            console.log("Error parsing date: " + item.PremiereDate);
                        }
                    }

                    lines.push(airTimeText || '');
                }

                if (options.showCurrentProgram && item.Type == 'TvChannel') {

                    if (item.CurrentProgram) {
                        lines.push(itemHelper.getDisplayName(item.CurrentProgram));
                    } else {
                        lines.push('');
                    }
                }

                if (options.showSeriesYear) {

                    if (item.Status == "Continuing") {

                        lines.push(globalize.translate('sharedcomponents#SeriesYearToPresent', item.ProductionYear || ''));

                    } else {
                        lines.push(item.ProductionYear || '');
                    }

                }

                if (options.showProgramAirInfo) {

                    var date = datetime.parseISO8601Date(item.StartDate, true);

                    var text = item.StartDate ?
                        date.toLocaleString() :
                        '';

                    lines.push(text || '&nbsp;');

                    lines.push(item.ChannelName || '&nbsp;');
                }
            }

            if ((showTitle || !imgUrl) && forceName && overlayText && lines.length == 1) {
                lines = [];
            }

            html += getCardTextLines(lines, cssClass, !options.overlayText, isOuterFooter);

            if (progressHtml) {
                html += progressHtml;
            }

            if (html) {
                html = '<div class="' + footerClass + '">' + html;

                //cardFooter
                html += "</div>";
            }

            return html;
        }

        function getTextActionButton(item, text) {

            if (!text) {
                text = itemHelper.getDisplayName(item);
            }

            var html = '<button data-id="' + item.Id + '" data-type="' + item.Type + '" data-mediatype="' + item.MediaType + '" data-channelid="' + item.ChannelId + '" data-isfolder="' + item.IsFolder + '" type="button" class="itemAction textActionButton" data-action="link">';
            html += text;
            html += '</button>';

            return html;
        }

        function getItemCountsHtml(options, item) {

            var counts = [];

            var childText;

            if (item.Type == 'Playlist') {

                childText = '';

                if (item.CumulativeRunTimeTicks) {

                    var minutes = item.CumulativeRunTimeTicks / 600000000;

                    minutes = minutes || 1;

                    childText += globalize.translate('ValueMinutes', Math.round(minutes));

                } else {
                    childText += globalize.translate('ValueMinutes', 0);
                }

                counts.push(childText);

            }
            else if (item.Type == 'Genre' || item.Type == 'Studio') {

                if (item.MovieCount) {

                    childText = item.MovieCount == 1 ?
                    globalize.translate('ValueOneMovie') :
                    globalize.translate('ValueMovieCount', item.MovieCount);

                    counts.push(childText);
                }

                if (item.SeriesCount) {

                    childText = item.SeriesCount == 1 ?
                    globalize.translate('ValueOneSeries') :
                    globalize.translate('ValueSeriesCount', item.SeriesCount);

                    counts.push(childText);
                }
                if (item.EpisodeCount) {

                    childText = item.EpisodeCount == 1 ?
                    globalize.translate('ValueOneEpisode') :
                    globalize.translate('ValueEpisodeCount', item.EpisodeCount);

                    counts.push(childText);
                }
                if (item.GameCount) {

                    childText = item.GameCount == 1 ?
                    globalize.translate('ValueOneGame') :
                    globalize.translate('ValueGameCount', item.GameCount);

                    counts.push(childText);
                }

            } else if (item.Type == 'GameGenre') {

                if (item.GameCount) {

                    childText = item.GameCount == 1 ?
                    globalize.translate('sharedcomponents#ValueOneGame') :
                    globalize.translate('sharedcomponents#ValueGameCount', item.GameCount);

                    counts.push(childText);
                }
            } else if (item.Type == 'MusicGenre' || options.context == "MusicArtist") {

                if (item.AlbumCount) {

                    childText = item.AlbumCount == 1 ?
                    globalize.translate('sharedcomponents#ValueOneAlbum') :
                    globalize.translate('sharedcomponents#ValueAlbumCount', item.AlbumCount);

                    counts.push(childText);
                }
                if (item.SongCount) {

                    childText = item.SongCount == 1 ?
                    globalize.translate('sharedcomponents#ValueOneSong') :
                    globalize.translate('sharedcomponents#ValueSongCount', item.SongCount);

                    counts.push(childText);
                }
                if (item.MusicVideoCount) {

                    childText = item.MusicVideoCount == 1 ?
                    globalize.translate('sharedcomponents#ValueOneMusicVideo') :
                    globalize.translate('sharedcomponents#ValueMusicVideoCount', item.MusicVideoCount);

                    counts.push(childText);
                }
            }

            return counts.join(', ');
        }

        function buildCard(index, item, apiClient, options, className) {

            var action = options.action || 'link';

            var scalable = options.scalable !== false;
            if (scalable) {
                className += " scalableCard " + options.shape + "Card-scalable";
            }

            var imgInfo = getCardImageUrl(item, apiClient, options);
            var imgUrl = imgInfo.imgUrl;

            var forceName = imgInfo.forceName || !imgUrl;

            var showTitle = options.showTitle == 'auto' ? true : (options.showTitle || item.Type == 'PhotoAlbum' || item.Type == 'Folder');
            var overlayText = options.overlayText;

            if (forceName && !options.cardLayout) {

                if (overlayText == null) {
                    overlayText = true;
                }
            }

            var cardImageContainerClass = 'cardImageContainer';
            if (options.coverImage || imgInfo.coverImage) {
                cardImageContainerClass += ' coveredImage';

                if (item.MediaType == 'Photo' || item.Type == 'PhotoAlbum' || item.Type == 'Folder') {
                    cardImageContainerClass += ' coveredImage-noScale';
                }
            }

            if (!imgUrl) {
                cardImageContainerClass += ' ' + getDefaultColorClass(item.Name);
            }

            var separateCardBox = scalable;
            var cardBoxClass = options.cardLayout ? 'cardBox visualCardBox' : 'cardBox';

            if (!layoutManager.tv) {
                cardBoxClass += ' cardBox-mobile';
            } else {
                cardBoxClass += ' cardBox-focustransform';
            }

            var footerCssClass;
            var progressHtml = indicators.getProgressBarHtml(item);

            var innerCardFooter = '';

            var footerOverlayed = false;

            if (overlayText) {

                footerCssClass = progressHtml ? 'innerCardFooter fullInnerCardFooter' : 'innerCardFooter';
                innerCardFooter += getCardFooterText(item, options, showTitle, forceName, overlayText, imgUrl, footerCssClass, progressHtml, false);
                footerOverlayed = true;
            }
            else if (progressHtml) {
                innerCardFooter += '<div class="innerCardFooter fullInnerCardFooter innerCardFooterClear">';
                innerCardFooter += progressHtml;
                innerCardFooter += '</div>';

                progressHtml = '';
            }

            var mediaSourceCount = item.MediaSourceCount || 1;
            if (mediaSourceCount > 1) {
                innerCardFooter += '<div class="mediaSourceIndicator">' + mediaSourceCount + '</div>';
            }

            var outerCardFooter = '';
            if (!overlayText && !footerOverlayed) {
                footerCssClass = options.cardLayout ? 'cardFooter visualCardBox-cardFooter' : 'cardFooter transparent';
                outerCardFooter = getCardFooterText(item, options, showTitle, forceName, overlayText, imgUrl, footerCssClass, progressHtml, true);
            }

            if (outerCardFooter && !options.cardLayout && options.allowBottomPadding !== false) {
                cardBoxClass += ' cardBox-bottompadded';
            }

            if (!separateCardBox) {
                cardImageContainerClass += " " + cardBoxClass;
            }

            var overlayButtons = '';
            if (!layoutManager.tv) {

                var overlayPlayButton = options.overlayPlayButton;

                if (overlayPlayButton == null && !options.overlayMoreButton && !options.cardLayout) {
                    overlayPlayButton = item.MediaType == 'Video';
                }

                if (overlayPlayButton && !item.IsPlaceHolder && (item.LocationType != 'Virtual' || !item.MediaType || item.Type == 'Program') && item.Type != 'Person' && item.PlayAccess == 'Full') {
                    overlayButtons += '<button is="paper-icon-button-light" class="cardOverlayButton itemAction autoSize" data-action="playmenu" onclick="return false;"><i class="md-icon">play_arrow</i></button>';
                }
                if (options.overlayMoreButton) {

                    var moreIcon = appHost.moreIcon == 'dots-horiz' ? '&#xE5D3;' : '&#xE5D4;';

                    overlayButtons += '<button is="paper-icon-button-light" class="cardOverlayButton itemAction autoSize" data-action="menu" onclick="return false;"><i class="md-icon">' + moreIcon + '</i></button>';
                }
            }

            if (options.showChildCountIndicator && item.ChildCount) {
                className += ' groupedCard';
            }

            // cardBox can be it's own separate element if an outer footer is ever needed
            var cardImageContainerOpen;
            var cardImageContainerClose = '';
            var cardBoxClose = '';
            var cardContentClose = '';
            var cardScalableClose = '';

            if (separateCardBox) {
                var cardContentOpen;

                if (layoutManager.tv) {
                    cardContentOpen = '<div class="cardContent">';
                    cardContentClose = '</div>';
                } else {
                    cardContentOpen = '<button type="button" class="clearButton cardContent itemAction" data-action="' + action + '">';
                    cardContentClose = '</button>';
                }
                cardImageContainerOpen = imgUrl ? ('<div class="' + cardImageContainerClass + ' lazy" data-src="' + imgUrl + '">') : ('<div class="' + cardImageContainerClass + '">');

                var cardScalableClass = options.cardLayout ? 'cardScalable visualCardBox-cardScalable' : 'cardScalable';
                cardImageContainerOpen = '<div class="' + cardBoxClass + '"><div class="' + cardScalableClass + '"><div class="cardPadder-' + options.shape + '"></div>' + cardContentOpen + cardImageContainerOpen;
                cardBoxClose = '</div>';
                cardScalableClose = '</div>';
                cardImageContainerClose = '</div>';
            } else {

                if (overlayButtons && !separateCardBox) {
                    cardImageContainerClass += ' cardImageContainerClass-button';
                    cardImageContainerOpen = imgUrl ? ('<button type="button" data-action="' + action + '" class="itemAction ' + cardImageContainerClass + ' lazy" data-src="' + imgUrl + '">') : ('<button type="button" data-action="' + action + '" class="itemAction ' + cardImageContainerClass + '">');
                    cardImageContainerClose = '</button>';

                    className += ' forceRelative';
                } else {
                    cardImageContainerOpen = imgUrl ? ('<div class="' + cardImageContainerClass + ' lazy" data-src="' + imgUrl + '">') : ('<div class="' + cardImageContainerClass + '">');
                    cardImageContainerClose = '</div>';
                }
            }

            var indicatorsHtml = '';

            indicatorsHtml += indicators.getSyncIndicator(item);
            indicatorsHtml += indicators.getTimerIndicator(item);

            if (options.showGroupCount) {

                indicatorsHtml += indicators.getChildCountIndicatorHtml(item, {
                    minCount: 1
                });
            }
            else {
                indicatorsHtml += indicators.getPlayedIndicatorHtml(item);
            }

            if (indicatorsHtml) {
                cardImageContainerOpen += '<div class="cardIndicators ' + options.shape + 'CardIndicators">' + indicatorsHtml + '</div>';
            }

            if (!imgUrl) {
                var defaultName = item.EpisodeTitle ? item.Name : itemHelper.getDisplayName(item);
                cardImageContainerOpen += '<div class="cardText cardCenteredText">' + defaultName + '</div>';
            }

            var tagName = (layoutManager.tv || !scalable) && !overlayButtons ? 'button' : 'div';

            var prefix = (item.SortName || item.Name || '')[0];

            if (prefix) {
                prefix = prefix.toUpperCase();
            }

            var timerAttributes = '';
            if (item.TimerId) {
                timerAttributes += ' data-timerid="' + item.TimerId + '"';
            }
            if (item.SeriesTimerId) {
                timerAttributes += ' data-seriestimerid="' + item.SeriesTimerId + '"';
            }

            var actionAttribute;

            if (tagName == 'button') {
                className += " itemAction";
                actionAttribute = ' data-action="' + action + '"';
            } else {
                actionAttribute = '';
            }

            className += ' card-withuserdata';

            var positionTicksData = item.UserData && item.UserData.PlaybackPositionTicks ? (' data-positionticks="' + item.UserData.PlaybackPositionTicks + '"') : '';
            var collectionIdData = options.collectionId ? (' data-collectionid="' + options.collectionId + '"') : '';
            var playlistIdData = options.playlistId ? (' data-playlistid="' + options.playlistId + '"') : '';
            var mediaTypeData = item.MediaType ? (' data-mediatype="' + item.MediaType + '"') : '';
            var collectionTypeData = item.CollectionType ? (' data-collectiontype="' + item.CollectionType + '"') : '';
            var channelIdData = item.ChannelId ? (' data-channelid="' + item.ChannelId + '"') : '';
            var contextData = options.context ? (' data-context="' + options.context + '"') : '';

            return '\
<' + tagName + ' data-index="' + index + '"' + timerAttributes + actionAttribute + ' data-isfolder="' + (item.IsFolder || false) + '" data-serverid="' + (item.ServerId) + '" data-id="' + (item.Id || item.ItemId) + '" data-type="' + item.Type + '"' + mediaTypeData + collectionTypeData + channelIdData + positionTicksData + collectionIdData + playlistIdData + contextData + ' data-prefix="' + prefix + '" class="' + className + '"> \
' + cardImageContainerOpen + innerCardFooter + cardImageContainerClose + cardContentClose + overlayButtons + cardScalableClose + outerCardFooter + cardBoxClose + '\
</' + tagName + '>';
        }

        function buildCards(items, options) {

            // Abort if the container has been disposed
            if (!document.body.contains(options.itemsContainer)) {
                return;
            }

            if (options.parentContainer) {
                if (items.length) {
                    options.parentContainer.classList.remove('hide');
                } else {
                    options.parentContainer.classList.add('hide');
                    return;
                }
            }

            var apiClient = connectionManager.currentApiClient();

            var html = buildCardsHtmlInternal(items, apiClient, options);

            if (html) {

                if (options.itemsContainer.cardBuilderHtml != html) {
                    options.itemsContainer.innerHTML = html;

                    if (items.length < 50) {
                        options.itemsContainer.cardBuilderHtml = html;
                    } else {
                        options.itemsContainer.cardBuilderHtml = null;
                    }
                }

                imageLoader.lazyChildren(options.itemsContainer);
            } else {

                options.itemsContainer.innerHTML = html;
                options.itemsContainer.cardBuilderHtml = null;
            }

            if (options.autoFocus) {
                focusManager.autoFocus(options.itemsContainer, true);
            }

            if (options.indexBy == 'Genres') {
                options.itemsContainer.addEventListener('click', onItemsContainerClick);
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

        function onItemsContainerClick(e) {

            var listItemsMoreButton = parentWithClass(e.target, 'listItemsMoreButton');

            if (listItemsMoreButton) {

                var value = listItemsMoreButton.getAttribute('data-indexvalue');
                var parentid = listItemsMoreButton.getAttribute('data-parentid');

                Emby.Page.showGenre({
                    ParentId: parentid,
                    Id: value
                });
            }
        }

        function ensureIndicators(card, indicatorsElem) {

            if (indicatorsElem) {
                return indicatorsElem;
            }

            indicatorsElem = card.querySelector('.indicators');

            if (!indicatorsElem) {

                var cardImageContainer = card.querySelector('.cardImageContainer');
                indicatorsElem = document.createElement('div');
                indicatorsElem.classList.add('indicators');
                cardImageContainer.appendChild(indicatorsElem);
            }

            return indicatorsElem;
        }

        function updateUserData(card, userData) {

            var type = card.getAttribute('data-type');
            var enableCountIndicator = type == 'Series' || type == 'BoxSet' || type == 'Season';
            var indicatorsElem;

            if (userData.Played) {

                var playedIndicator = card.querySelector('.playedIndicator');

                if (!playedIndicator) {

                    playedIndicator = document.createElement('div');
                    playedIndicator.classList.add('playedIndicator');
                    indicatorsElem = ensureIndicators(card, indicatorsElem);
                    indicatorsElem.appendChild(playedIndicator);
                }
                playedIndicator.innerHTML = '<i class="md-icon">check</i>';
            } else {

                var playedIndicator = card.querySelector('.playedIndicator');
                if (playedIndicator) {

                    playedIndicator.parentNode.removeChild(playedIndicator);
                }
            }
            if (userData.UnplayedItemCount) {
                var countIndicator = card.querySelector('.countIndicator');

                if (!countIndicator) {

                    countIndicator = document.createElement('div');
                    countIndicator.classList.add('countIndicator');
                    indicatorsElem = ensureIndicators(card, indicatorsElem);
                    indicatorsElem.appendChild(countIndicator);
                }
                countIndicator.innerHTML = userData.UnplayedItemCount;
            } else if (enableCountIndicator) {

                var countIndicator = card.querySelector('.countIndicator');
                if (countIndicator) {

                    countIndicator.parentNode.removeChild(countIndicator);
                }
            }

            var progressHtml = indicators.getProgressBarHtml({
                Type: type,
                UserData: userData,
                MediaType: 'Video'
            });

            if (progressHtml) {

                var itemProgressBar = card.querySelector('.itemProgressBar');

                if (!itemProgressBar) {
                    itemProgressBar = document.createElement('div');
                    itemProgressBar.classList.add('itemProgressBar');

                    var innerCardFooter = card.querySelector('.innerCardFooter');
                    if (!innerCardFooter) {
                        innerCardFooter = document.createElement('div');
                        innerCardFooter.classList.add('innerCardFooter');
                        var cardImageContainer = card.querySelector('.cardImageContainer');
                        cardImageContainer.appendChild(innerCardFooter);
                    }
                    innerCardFooter.appendChild(itemProgressBar);
                }

                itemProgressBar.innerHTML = progressHtml;
            }
            else {

                var itemProgressBar = card.querySelector('.itemProgressBar');
                if (itemProgressBar) {
                    itemProgressBar.parentNode.removeChild(itemProgressBar);
                }
            }
        }

        function onUserDataChanged(userData) {

            var cards = document.querySelectorAll('.card-withuserdata[data-id="' + userData.ItemId + '"]');

            for (var i = 0, length = cards.length; i < length; i++) {
                updateUserData(cards[i], userData);
            }
        }

        return {
            getCardsHtml: getCardsHtml,
            buildCards: buildCards,
            onUserDataChanged: onUserDataChanged,
            getDefaultColorClass: getDefaultColorClass
        };
    });