define(['datetime', 'imageLoader', 'connectionManager', 'itemHelper', 'mediaInfo', 'focusManager', 'indicators', 'globalize', 'browser', 'layoutManager', 'emby-button', 'css!./card', 'paper-icon-button-light', 'clearButtonStyle'],
    function (datetime, imageLoader, connectionManager, itemHelper, mediaInfo, focusManager, indicators, globalize, browser, layoutManager) {

        function getCardsHtml(items, options) {

            var apiClient = connectionManager.currentApiClient();

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

        var shapes = ['square', 'portrait', 'banner', 'smallBackdrop', 'backdrop', 'overflowBackdrop', 'overflowPortrait', 'overflowSquare'];
        function getImageWidth(shape) {

            var screenWidth = window.innerWidth;
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

            if (options.shape == 'backdrop') {
                options.width = options.width || 500;
            }
            else if (options.shape == 'portrait') {
                options.width = options.width || 243;
            }
            else if (options.shape == 'square') {
                options.width = options.width || 243;
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
                            html += '<div class="itemsContainer verticalItemsContainer">';
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
                var hasOpenSection = false;

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
                    html += '<button is="emby-button" class="listItemsMoreButton raised" data-parentid="' + options.parentId + '" data-indextype="Genres" data-indexvalue="' + item.Id + '">' + globalize.translate('core#More') + '</button>';
                    html += '</div>';
                }

                html += '</div>';
                html += '</div>';
            }

            return html;
        }

        function getDisplayDateText(date) {

            var weekday = [];
            weekday[0] = globalize.translate('core#OptionSunday');
            weekday[1] = globalize.translate('core#OptionMonday');
            weekday[2] = globalize.translate('core#OptionTuesday');
            weekday[3] = globalize.translate('core#OptionWednesday');
            weekday[4] = globalize.translate('core#OptionThursday');
            weekday[5] = globalize.translate('core#OptionFriday');
            weekday[6] = globalize.translate('core#OptionSaturday');

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

            var width = options.width;
            var height = null;
            var primaryImageAspectRatio = imageLoader.getPrimaryImageAspectRatio([item]);
            var forceName = false;
            var imgUrl = null;
            var coverImage = false;

            if (options.preferThumb && item.ImageTags && item.ImageTags.Thumb) {

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

                imgUrl = apiClient.getImageUrl(item.Id, {
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

                imgUrl = apiClient.getImageUrl(item.Id || item.ItemId, {
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

                imgUrl = apiClient.getImageUrl(item.ParentPrimaryImageItemId, {
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

        function buildCard(index, item, apiClient, options, className) {

            var action = options.action || 'link';

            if (layoutManager.tv) {
                className += " itemAction";
            }

            if (options.scalable) {
                className += " scalableCard";
            }

            var imgInfo = getCardImageUrl(item, apiClient, options);
            var imgUrl = imgInfo.imgUrl;

            var cardImageContainerClass = 'cardImageContainer';
            if (options.coverImage || imgInfo.coverImage) {
                cardImageContainerClass += ' coveredImage';

                if (item.MediaType == 'Photo' || item.Type == 'PhotoAlbum' || item.Type == 'Folder') {
                    cardImageContainerClass += ' noScale';
                }
            }

            if (!imgUrl) {
                cardImageContainerClass += ' emptyCardImageContainer defaultCardColor' + getRandomInt(1, 5);
            }

            var separateCardBox = options.scalable;

            if (!separateCardBox) {
                cardImageContainerClass += " cardBox";
            }

            // cardBox can be it's own separate element if an outer footer is ever needed
            var cardImageContainerOpen = imgUrl ? ('<div class="' + cardImageContainerClass + ' lazy" data-src="' + imgUrl + '">') : ('<div class="' + cardImageContainerClass + '">');
            var cardImageContainerClose = '';
            var cardBoxClose = '</div>';
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
                cardImageContainerOpen = '<div class="cardBox"><div class="cardScalable"><div class="cardPadder"></div>' + cardContentOpen + cardImageContainerOpen;
                cardBoxClose = '</div>';
                cardScalableClose = '</div>';
                cardImageContainerClose = '</div>';
            }

            var indicatorsHtml = '';

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
                cardImageContainerOpen += '<div class="indicators">' + indicatorsHtml + '</div>';
            }

            var showTitle = options.showTitle || imgInfo.forceName || item.Type == 'PhotoAlbum';
            var showParentTitle = options.showParentTitle || (imgInfo.forceName && item.Type == 'Episode');

            if (!imgUrl) {
                var defaultName = item.EpisodeTitle ? item.Name : itemHelper.getDisplayName(item);
                cardImageContainerOpen += '<div class="cardText cardCenteredText">' + defaultName + '</div>';
            }

            var enableOuterFooter = options.overlayText === false;
            var nameHtml = '';

            if (showParentTitle) {
                nameHtml += '<div class="cardText">' + (item.EpisodeTitle ? item.Name : (item.SeriesName || item.Album || item.AlbumArtist || item.GameSystem || "")) + '</div>';
            }

            if (showTitle) {
                var nameClass = 'cardText';
                nameHtml += '<div class="' + nameClass + '">' + itemHelper.getDisplayName(item) + '</div>';
            }

            var innerCardFooterClass = 'innerCardFooter';
            var progressHtml = indicators.getProgressBarHtml(item);

            if (progressHtml) {
                innerCardFooterClass += " fullInnerCardFooter";
            }

            var innerCardFooter = '';

            if (imgUrl && (progressHtml || (nameHtml && !enableOuterFooter))) {
                innerCardFooter += '<div class="' + innerCardFooterClass + '">';

                if (!enableOuterFooter) {
                    innerCardFooter += nameHtml;
                }
                innerCardFooter += progressHtml;
                innerCardFooter += '</div>';
            }

            var outerCardFooter = '';
            if (nameHtml && enableOuterFooter) {
                outerCardFooter += '<div class="cardFooter">';
                outerCardFooter += nameHtml;
                outerCardFooter += '</div>';
            }

            var overlayButtons = '';
            if (!layoutManager.tv) {
                if (options.overlayPlayButton && !item.IsPlaceHolder && (item.LocationType != 'Virtual' || !item.MediaType || item.Type == 'Program') && item.Type != 'Person' && item.PlayAccess == 'Full') {
                    overlayButtons += '<button is="paper-icon-button-light" class="cardOverlayButton itemAction autoSize" data-action="playmenu" onclick="return false;"><i class="md-icon">play_arrow</i></button>';
                }
                if (options.overlayMoreButton) {
                    overlayButtons += '<button is="paper-icon-button-light" class="cardOverlayButton itemAction autoSize" data-action="menu" onclick="return false;"><i class="md-icon">more_vert</i></button>';
                }
            }

            var tagName = layoutManager.tv ? 'button' : 'div';

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

            var positionTicksData = item.UserData && item.UserData.PlaybackPositionTicks ? (' data-positionticks="' + item.UserData.PlaybackPositionTicks + '"') : '';
            var collectionIdData = options.collectionId ? (' data-collectionid="' + options.collectionId + '"') : '';
            var playlistIdData = options.playlistId ? (' data-playlistid="' + options.playlistId + '"') : '';

            var actionAttribute = layoutManager.tv ? (' data-action="' + action + '"') : '';

            return '\
<' + tagName + ' data-index="' + index + '"' + timerAttributes + actionAttribute + ' data-isfolder="' + (item.IsFolder || false) + '" data-serverid="' + (item.ServerId) + '" data-id="' + (item.Id || item.ItemId) + '" data-type="' + item.Type + '" data-mediatype="' + item.MediaType + '"' + positionTicksData + collectionIdData + playlistIdData + ' data-prefix="' + prefix + '" class="' + className + '"> \
' + cardImageContainerOpen + cardImageContainerClose + innerCardFooter + cardContentClose + overlayButtons + cardScalableClose + outerCardFooter + cardBoxClose + '\
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

            var cards = document.querySelectorAll('.card[data-id="' + userData.ItemId + '"]');

            for (var i = 0, length = cards.length; i < length; i++) {
                updateUserData(cards[i], userData);
            }
        }

        return {
            getCardsHtml: getCardsHtml,
            buildCards: buildCards,
            onUserDataChanged: onUserDataChanged
        };
    });