define(["connectionManager", "cardBuilder", "registrationServices", "appSettings", "dom", "apphost", "layoutManager", "imageLoader", "globalize", "itemShortcuts", "itemHelper", "appRouter", "emby-button", "paper-icon-button-light", "emby-itemscontainer", "emby-scroller", "emby-linkbutton", "css!./homesections"], function(connectionManager, cardBuilder, registrationServices, appSettings, dom, appHost, layoutManager, imageLoader, globalize, itemShortcuts, itemHelper, appRouter) {
    "use strict";

    function getDefaultSection(index) {
        switch (index) {
            case 0:
                return "smalllibrarytiles";
            case 1:
                return "resume";
            case 2:
                return "resumeaudio";
            case 3:
                return "livetv";
            case 4:
                return "nextup";
            case 5:
                return "latestmedia";
            case 6:
                return "none";
            default:
                return ""
        }
    }

    function getAllSectionsToShow(userSettings, sectionCount) {
        for (var sections = [], i = 0, length = sectionCount; i < length; i++) {
            var section = userSettings.get("homesection" + i) || getDefaultSection(i);
            "folders" === section && (section = getDefaultSection(0)), sections.push(section)
        }
        return sections
    }

    function loadSections(elem, apiClient, user, userSettings) {
        return getUserViews(apiClient, user.Id).then(function(userViews) {
            var i, length, html = "";
            for (i = 0, length = 7; i < length; i++) html += '<div class="verticalSection section' + i + '"></div>';
            elem.innerHTML = html, elem.classList.add("homeSectionsContainer");
            var promises = [],
                sections = getAllSectionsToShow(userSettings, 7);
            for (i = 0, length = sections.length; i < length; i++) promises.push(loadSection(elem, apiClient, user, userSettings, userViews, sections, i));
            return Promise.all(promises).then(function() {
                html = "";
                var style = "margin-top:4em;";
                return layoutManager.tv && (style += "padding: 0 7.5%;"), html += '<div class="verticalSection padded-left padded-right customizeSection hide" style="' + style + '">', html += '<a href="' + appRouter.getRouteUrl("settings") + '" is="emby-linkbutton" class="raised block"><span>' + globalize.translate("sharedcomponents#HeaderCustomizeHomeScreen") + "</span></a>", html += "</div>", elem.insertAdjacentHTML("beforeend", html), resume(elem, {
                    refresh: !0,
                    returnPromise: !1
                })
            })
        })
    }

    function destroySections(elem) {
        var i, length, elems = elem.querySelectorAll(".itemsContainer");
        for (i = 0, length = elems.length; i < length; i++) elems[i].fetchData = null, elems[i].parentContainer = null, elems[i].getItemsHtml = null;
        elem.innerHTML = ""
    }

    function pause(elem) {
        var i, length, elems = elem.querySelectorAll(".itemsContainer");
        for (i = 0, length = elems.length; i < length; i++) elems[i].pause()
    }

    function resume(elem, options) {
        var i, length, elems = elem.querySelectorAll(".itemsContainer"),
            promises = [];
        for (i = 0, length = elems.length; i < length; i++) promises.push(elems[i].resume(options));
        var promise = Promise.all(promises).then(function() {
            elem.querySelector(".customizeSection").classList.remove("hide")
        });
        if (!options || !1 !== options.returnPromise) return promise
    }

    function loadSection(page, apiClient, user, userSettings, userViews, allSections, index) {
        var section = allSections[index],
            userId = user.Id,
            elem = page.querySelector(".section" + index);
        if ("latestmedia" === section) loadRecentlyAdded(elem, apiClient, user, userViews);
        else {
            if ("librarytiles" === section || "smalllibrarytiles" === section || "smalllibrarytiles-automobile" === section || "librarytiles-automobile" === section) return loadLibraryTiles(elem, apiClient, user, userSettings, "smallBackdrop", userViews, allSections);
            if ("librarybuttons" === section) return loadlibraryButtons(elem, apiClient, user, userSettings, userViews);
            if ("resume" === section) loadResumeVideo(elem, apiClient, userId);
            else if ("resumeaudio" === section) loadResumeAudio(elem, apiClient, userId);
            else if ("activerecordings" === section) loadLatestLiveTvRecordings(elem, !0, apiClient, userId);
            else {
                if ("nextup" !== section) return "onnow" === section || "livetv" === section ? loadOnNow(elem, apiClient, user) : (elem.innerHTML = "", Promise.resolve());
                loadNextUp(elem, apiClient, userId)
            }
        }
        return Promise.resolve()
    }

    function getUserViews(apiClient, userId) {
        return apiClient.getUserViews({}, userId || apiClient.getCurrentUserId()).then(function(result) {
            return result.Items
        })
    }

    function enableScrollX() {
        return !0
    }

    function getSquareShape() {
        return enableScrollX() ? "overflowSquare" : "square"
    }

    function getThumbShape() {
        return enableScrollX() ? "overflowBackdrop" : "backdrop"
    }

    function getPortraitShape() {
        return enableScrollX() ? "autooverflow" : "auto"
    }

    function getLibraryButtonsHtml(items) {
        var html = "";
        html += '<div class="verticalSection verticalSection-extrabottompadding">', html += '<div class="sectionTitleContainer sectionTitleContainer-cards">', html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + globalize.translate("sharedcomponents#HeaderMyMedia") + "</h2>", layoutManager.tv || (html += '<button type="button" is="paper-icon-button-light" class="sectionTitleIconButton btnHomeScreenSettings"><i class="md-icon">&#xE5D3;</i></button>'), html += "</div>", html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x" data-multiselect="false">';
        for (var i = 0, length = items.length; i < length; i++) {
            var icon, item = items[i];
            switch (item.CollectionType) {
                case "movies":
                    icon = "local_movies";
                    break;
                case "music":
                    icon = "library_music";
                    break;
                case "photos":
                    icon = "photo";
                    break;
                case "livetv":
                case "tvshows":
                    icon = "live_tv";
                    break;
                case "games":
                    icon = "folder";
                    break;
                case "trailers":
                    icon = "local_movies";
                    break;
                case "homevideos":
                case "musicvideos":
                    icon = "video_library";
                    break;
                case "books":
                case "channels":
                case "playlists":
                default:
                    icon = "folder"
            }
            html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl(item) + '" class="raised homeLibraryButton"><i class="md-icon">' + icon + "</i><span>" + item.Name + "</span></a>"
        }
        return html += "</div>", html += "</div>"
    }

    function loadlibraryButtons(elem, apiClient, user, userSettings, userViews) {
        return Promise.all([getAppInfo(apiClient), getDownloadsSectionHtml(apiClient, user, userSettings)]).then(function(responses) {
            var infoHtml = responses[0],
                downloadsHtml = responses[1];
            elem.classList.remove("verticalSection");
            var html = getLibraryButtonsHtml(userViews);
            elem.innerHTML = html + downloadsHtml + infoHtml, bindHomeScreenSettingsIcon(elem, apiClient, user.Id, userSettings), infoHtml && bindAppInfoEvents(elem), imageLoader.lazyChildren(elem)
        })
    }

    function bindAppInfoEvents(elem) {
        elem.querySelector(".appInfoSection").addEventListener("click", function(e) {
            dom.parentWithClass(e.target, "card") && registrationServices.showPremiereInfo()
        })
    }

    function getRandomInt(min, max) {
        return Math.floor(Math.random() * (max - min + 1)) + min
    }

    function getAppInfo(apiClient) {
        var cacheKey = "lastappinfopresent5",
            lastDatePresented = parseInt(appSettings.get(cacheKey) || "0");
        return lastDatePresented ? (new Date).getTime() - lastDatePresented < 1728e5 ? Promise.resolve("") : registrationServices.validateFeature("dvr", {
            showDialog: !1,
            viewOnly: !0
        }).then(function() {
            return appSettings.set(cacheKey, (new Date).getTime()), ""
        }, function() {
            appSettings.set(cacheKey, (new Date).getTime());
            var infos = [getPremiereInfo];
            return appHost.supports("otherapppromotions") && infos.push(getTheaterInfo), infos[getRandomInt(0, infos.length - 1)]()
        }) : (appSettings.set(cacheKey, (new Date).getTime()), Promise.resolve(""))
    }

    function getCard(img, shape) {
        shape = shape || "backdropCard";
        var html = '<div class="card scalableCard ' + shape + " " + shape + '-scalable"><div class="cardBox"><div class="cardScalable"><div class="cardPadder cardPadder-backdrop"></div>';
        return html += '<div class="cardContent">', html += '<div class="cardImage lazy" data-src="' + img + '"></div>', html += "</div>", html += "</div></div></div>"
    }

    function getTheaterInfo() {
        var html = "";
        html += '<div class="verticalSection appInfoSection">', html += '<div class="sectionTitleContainer sectionTitleContainer-cards">', html += '<h2 class="sectionTitle sectionTitle-cards padded-left">Discover Jellyfin Theater</h2>', html += '<button is="paper-icon-button-light" class="sectionTitleButton" onclick="this.parentNode.parentNode.remove();" class="autoSize"><i class="md-icon">close</i></button>', html += "</div>";
        return html += '<div class="padded-left padded-right">', html += '<p class="sectionTitle-cards">A beautiful app for your TV and large screen tablet. Jellyfin Theater runs on Windows, Xbox One, Raspberry Pi, Samsung Smart TVs, Sony PS4, Web Browsers, and more.</p>', html += '<div class="itemsContainer vertical-wrap" is="emby-itemscontainer">', html += getCard("https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater1.png"), html += getCard("https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater2.png"), html += getCard("https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater3.png"), html += "</div>", html += "</div>", html += "</div>"
    }

    function getPremiereInfo() {
        var html = "";
        return html += '<div class="verticalSection appInfoSection">', html += '<div class="sectionTitleContainer sectionTitleContainer-cards">', html += '<h2 class="sectionTitle sectionTitle-cards padded-left">Discover Jellyfin Premiere</h2>', html += '<button is="paper-icon-button-light" class="sectionTitleButton" onclick="this.parentNode.parentNode.remove();" class="autoSize"><i class="md-icon">close</i></button>', html += "</div>", html += '<div class="padded-left padded-right">', html += '<p class="sectionTitle-cards">Enjoy Jellyfin DVR, get free access to Jellyfin apps, and more.</p>', html += '<div class="itemsContainer vertical-wrap" is="emby-itemscontainer">', html += getCard("https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater1.png"), html += getCard("https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater2.png"), html += getCard("https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater3.png"), html += "</div>", html += "</div>", html += "</div>"
    }

    function getFetchLatestItemsFn(serverId, parentId, collectionType) {
        return function() {
            var apiClient = connectionManager.getApiClient(serverId),
                limit = 16;
            enableScrollX() ? "music" === collectionType && (limit = 30) : limit = "tvshows" === collectionType ? 5 : "music" === collectionType ? 9 : 8;
            var options = {
                Limit: limit,
                Fields: "PrimaryImageAspectRatio,BasicSyncInfo",
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb",
                ParentId: parentId
            };
            return apiClient.getLatestItems(options)
        }
    }

    function getLatestItemsHtmlFn(itemType, viewType) {
        return function(items) {
            var shape = "Channel" === itemType || "movies" === viewType ? getPortraitShape() : "music" === viewType ? getSquareShape() : getThumbShape();
            return cardBuilder.getCardsHtml({
                items: items,
                shape: shape,
                preferThumb: "movies" !== viewType && "Channel" !== itemType && "music" !== viewType ? "auto" : null,
                showUnplayedIndicator: !1,
                showChildCountIndicator: !0,
                context: "home",
                overlayText: !1,
                centerText: !0,
                overlayPlayButton: "photos" !== viewType,
                allowBottomPadding: !enableScrollX() && !0,
                cardLayout: !1,
                showTitle: "photos" !== viewType,
                showYear: "movies" === viewType || "tvshows" === viewType || !viewType,
                showParentTitle: "music" === viewType || "tvshows" === viewType || !viewType || !1,
                lines: 2
            })
        }
    }

    function renderLatestSection(elem, apiClient, user, parent) {
        var html = "";
        html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">', layoutManager.tv ? html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate("sharedcomponents#LatestFromLibrary", parent.Name) + "</h2>" : (html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl(parent, {
            section: "latest"
        }) + '" class="more button-flat button-flat-mini sectionTitleTextButton">', html += '<h2 class="sectionTitle sectionTitle-cards">', html += globalize.translate("sharedcomponents#LatestFromLibrary", parent.Name), html += "</h2>", html += '<i class="md-icon">&#xE5CC;</i>', html += "</a>"), html += "</div>", enableScrollX() ? html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right">' : html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">', enableScrollX() && (html += "</div>"), html += "</div>", elem.innerHTML = html;
        var itemsContainer = elem.querySelector(".itemsContainer");
        itemsContainer.fetchData = getFetchLatestItemsFn(apiClient.serverId(), parent.Id, parent.CollectionType), itemsContainer.getItemsHtml = getLatestItemsHtmlFn(parent.Type, parent.CollectionType), itemsContainer.parentContainer = elem
    }

    function loadRecentlyAdded(elem, apiClient, user, userViews) {
        elem.classList.remove("verticalSection");
        for (var excludeViewTypes = ["playlists", "livetv", "boxsets", "channels"], i = 0, length = userViews.length; i < length; i++) {
            var item = userViews[i];
            if (-1 === user.Configuration.LatestItemsExcludes.indexOf(item.Id) && -1 === excludeViewTypes.indexOf(item.CollectionType || [])) {
                var frag = document.createElement("div");
                frag.classList.add("verticalSection"), frag.classList.add("hide"), elem.appendChild(frag), renderLatestSection(frag, apiClient, user, item)
            }
        }
    }

    function getRequirePromise(deps) {
        return new Promise(function(resolve, reject) {
            require(deps, resolve)
        })
    }

    function showHomeScreenSettings(elem, options) {
        return getRequirePromise(["homescreenSettingsDialog"]).then(function(homescreenSettingsDialog) {
            return homescreenSettingsDialog.show(options).then(function() {
                dom.parentWithClass(elem, "homeSectionsContainer").dispatchEvent(new CustomEvent("settingschange", {
                    cancelable: !1
                }))
            })
        })
    }

    function bindHomeScreenSettingsIcon(elem, apiClient, userId, userSettings) {
        var btnHomeScreenSettings = elem.querySelector(".btnHomeScreenSettings");
        btnHomeScreenSettings && btnHomeScreenSettings.addEventListener("click", function() {
            showHomeScreenSettings(elem, {
                serverId: apiClient.serverId(),
                userId: userId,
                userSettings: userSettings
            })
        })
    }

    function getDownloadsSectionHtml(apiClient, user, userSettings) {
        return appHost.supports("sync") && user.Policy.EnableContentDownloading ? (apiClient.getLatestOfflineItems ? apiClient.getLatestOfflineItems({
            Limit: 20,
            Filters: "IsNotFolder"
        }) : Promise.resolve([])).then(function(items) {
            var html = "";
            return html += '<div class="verticalSection">', html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">', layoutManager.tv ? html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate("sharedcomponents#HeaderMyDownloads") + "</h2>" : (html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl("downloads") + '" class="more button-flat button-flat-mini sectionTitleTextButton">', html += '<h2 class="sectionTitle sectionTitle-cards">', html += globalize.translate("sharedcomponents#HeaderMyDownloads"), html += "</h2>", html += '<i class="md-icon">&#xE5CC;</i>', html += "</a>", html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl("managedownloads") + '" class="sectionTitleIconButton"><i class="md-icon">&#xE8B8;</i></a>'), html += "</div>", html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="scrollSlider focuscontainer-x padded-left padded-right">', html += cardBuilder.getCardsHtml({
                items: items,
                preferThumb: "auto",
                shape: "autooverflow",
                overlayText: !1,
                showTitle: !0,
                showParentTitle: !0,
                lazy: !0,
                showDetailsMenu: !0,
                overlayPlayButton: !0,
                context: "home",
                centerText: !0,
                allowBottomPadding: !1,
                cardLayout: !1,
                showYear: !0,
                lines: 2
            }), html += "</div>", html += "</div>"
        }) : Promise.resolve("")
    }

    function loadLibraryTiles(elem, apiClient, user, userSettings, shape, userViews, allSections) {
        elem.classList.remove("verticalSection");
        var html = "",
            scrollX = !layoutManager.desktop;
        return userViews.length && (html += '<div class="verticalSection">', html += '<div class="sectionTitleContainer sectionTitleContainer-cards">', html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + globalize.translate("sharedcomponents#HeaderMyMedia") + "</h2>", layoutManager.tv || (html += '<button type="button" is="paper-icon-button-light" class="sectionTitleIconButton btnHomeScreenSettings"><i class="md-icon">&#xE5D3;</i></button>'), html += "</div>", html += scrollX ? '<div is="emby-scroller" class="padded-top-focusscale padded-bottom-focusscale" data-mousewheel="false" data-centerfocus="true"><div is="emby-itemscontainer" class="scrollSlider focuscontainer-x padded-left padded-right">' : '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">', html += cardBuilder.getCardsHtml({
            items: userViews,
            shape: scrollX ? "overflowSmallBackdrop" : shape,
            showTitle: !0,
            centerText: !0,
            overlayText: !1,
            lazy: !0,
            transition: !1,
            allowBottomPadding: !scrollX
        }), scrollX && (html += "</div>"), html += "</div>", html += "</div>"), Promise.all([getAppInfo(apiClient), getDownloadsSectionHtml(apiClient, user, userSettings)]).then(function(responses) {
            var infoHtml = responses[0],
                downloadsHtml = responses[1];
            elem.innerHTML = html + downloadsHtml + infoHtml, bindHomeScreenSettingsIcon(elem, apiClient, user.Id, userSettings), infoHtml && bindAppInfoEvents(elem), imageLoader.lazyChildren(elem)
        })
    }

    function getContinueWatchingFetchFn(serverId) {
        return function() {
            var limit, apiClient = connectionManager.getApiClient(serverId),
                screenWidth = dom.getWindowSize().innerWidth;
            enableScrollX() ? limit = 12 : (limit = screenWidth >= 1920 ? 8 : screenWidth >= 1600 ? 8 : screenWidth >= 1200 ? 9 : 6, limit = Math.min(limit, 5));
            var options = {
                Limit: limit,
                Recursive: !0,
                Fields: "PrimaryImageAspectRatio,BasicSyncInfo",
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb",
                EnableTotalRecordCount: !1,
                MediaTypes: "Video"
            };
            return apiClient.getResumableItems(apiClient.getCurrentUserId(), options)
        }
    }

    function getContinueWatchingItemsHtml(items) {
        return cardBuilder.getCardsHtml({
            items: items,
            preferThumb: !0,
            shape: getThumbShape(),
            overlayText: !1,
            showTitle: !0,
            showParentTitle: !0,
            lazy: !0,
            showDetailsMenu: !0,
            overlayPlayButton: !0,
            context: "home",
            centerText: !0,
            allowBottomPadding: !1,
            cardLayout: !1,
            showYear: !0,
            lines: 2
        })
    }

    function loadResumeVideo(elem, apiClient, userId) {
        var html = "";
        html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + globalize.translate("sharedcomponents#HeaderContinueWatching") + "</h2>", enableScrollX() ? html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right" data-monitor="videoplayback,markplayed">' : html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x" data-monitor="videoplayback,markplayed">', enableScrollX() && (html += "</div>"), html += "</div>", elem.classList.add("hide"), elem.innerHTML = html;
        var itemsContainer = elem.querySelector(".itemsContainer");
        itemsContainer.fetchData = getContinueWatchingFetchFn(apiClient.serverId()), itemsContainer.getItemsHtml = getContinueWatchingItemsHtml, itemsContainer.parentContainer = elem
    }

    function getContinueListeningFetchFn(serverId) {
        return function() {
            var limit, apiClient = connectionManager.getApiClient(serverId),
                screenWidth = dom.getWindowSize().innerWidth;
            enableScrollX() ? limit = 12 : (limit = screenWidth >= 1920 ? 8 : screenWidth >= 1600 ? 8 : screenWidth >= 1200 ? 9 : 6, limit = Math.min(limit, 5));
            var options = {
                Limit: limit,
                Recursive: !0,
                Fields: "PrimaryImageAspectRatio,BasicSyncInfo",
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb",
                EnableTotalRecordCount: !1,
                MediaTypes: "Audio"
            };
            return apiClient.getResumableItems(apiClient.getCurrentUserId(), options)
        }
    }

    function getContinueListeningItemsHtml(items) {
        return cardBuilder.getCardsHtml({
            items: items,
            preferThumb: !0,
            shape: getThumbShape(),
            overlayText: !1,
            showTitle: !0,
            showParentTitle: !0,
            lazy: !0,
            showDetailsMenu: !0,
            overlayPlayButton: !0,
            context: "home",
            centerText: !0,
            allowBottomPadding: !1,
            cardLayout: !1,
            showYear: !0,
            lines: 2
        })
    }

    function loadResumeAudio(elem, apiClient, userId) {
        var html = "";
        html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + globalize.translate("sharedcomponents#HeaderContinueWatching") + "</h2>", enableScrollX() ? html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right" data-monitor="audioplayback,markplayed">' : html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x" data-monitor="audioplayback,markplayed">', enableScrollX() && (html += "</div>"), html += "</div>", elem.classList.add("hide"), elem.innerHTML = html;
        var itemsContainer = elem.querySelector(".itemsContainer");
        itemsContainer.fetchData = getContinueListeningFetchFn(apiClient.serverId()), itemsContainer.getItemsHtml = getContinueListeningItemsHtml, itemsContainer.parentContainer = elem
    }

    function bindUnlockClick(elem) {
        var btnUnlock = elem.querySelector(".btnUnlock");
        btnUnlock && btnUnlock.addEventListener("click", function(e) {
            registrationServices.validateFeature("livetv", {
                viewOnly: !0
            }).then(function() {
                dom.parentWithClass(elem, "homeSectionsContainer").dispatchEvent(new CustomEvent("settingschange", {
                    cancelable: !1
                }))
            })
        })
    }

    function getOnNowFetchFn(serverId) {
        return function() {
            var apiClient = connectionManager.getApiClient(serverId);
            return apiClient.getLiveTvRecommendedPrograms({
                userId: apiClient.getCurrentUserId(),
                IsAiring: !0,
                limit: 24,
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Thumb,Backdrop",
                EnableTotalRecordCount: !1,
                Fields: "ChannelInfo,PrimaryImageAspectRatio"
            })
        }
    }

    function getOnNowItemsHtml(items) {
        return cardBuilder.getCardsHtml({
            items: items,
            preferThumb: "auto",
            inheritThumb: !1,
            shape: enableScrollX() ? "autooverflow" : "auto",
            showParentTitleOrTitle: !0,
            showTitle: !0,
            centerText: !0,
            coverImage: !0,
            overlayText: !1,
            allowBottomPadding: !enableScrollX(),
            showAirTime: !0,
            showChannelName: !1,
            showAirDateTime: !1,
            showAirEndTime: !0,
            defaultShape: getThumbShape(),
            lines: 3,
            overlayPlayButton: !0
        })
    }

    function loadOnNow(elem, apiClient, user) {
        if (!user.Policy.EnableLiveTvAccess) return Promise.resolve();
        var promises = [];
        promises.push(registrationServices.validateFeature("livetv", {
            viewOnly: !0,
            showDialog: !1
        }).then(function() {
            return !0
        }, function() {
            return !1
        }));
        user.Id;
        return promises.push(apiClient.getLiveTvRecommendedPrograms({
            userId: apiClient.getCurrentUserId(),
            IsAiring: !0,
            limit: 1,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Thumb,Backdrop",
            EnableTotalRecordCount: !1,
            Fields: "ChannelInfo,PrimaryImageAspectRatio"
        })), Promise.all(promises).then(function(responses) {
            var registered = responses[0],
                result = responses[1],
                html = "";
            if (result.Items.length && registered) {
                elem.classList.remove("padded-left"), elem.classList.remove("padded-right"), elem.classList.remove("padded-bottom"), elem.classList.remove("verticalSection"), html += '<div class="verticalSection">', html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">', html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate("sharedcomponents#LiveTV") + "</h2>", html += "</div>", enableScrollX() ? (html += '<div is="emby-scroller" class="padded-top-focusscale padded-bottom-focusscale" data-mousewheel="false" data-centerfocus="true" data-scrollbuttons="false">', html += '<div class="scrollSlider padded-left padded-right padded-top padded-bottom focuscontainer-x">') : html += '<div class="padded-left padded-right padded-top focuscontainer-x">', html += '<a style="margin-left:.8em;margin-right:0;" is="emby-linkbutton" href="' + appRouter.getRouteUrl("livetv", {
                    serverId: apiClient.serverId()
                }) + '" class="raised"><span>' + globalize.translate("sharedcomponents#Programs") + "</span></a>", html += '<a style="margin-left:.5em;margin-right:0;" is="emby-linkbutton" href="' + appRouter.getRouteUrl("livetv", {
                    serverId: apiClient.serverId(),
                    section: "guide"
                }) + '" class="raised"><span>' + globalize.translate("sharedcomponents#Guide") + "</span></a>", html += '<a style="margin-left:.5em;margin-right:0;" is="emby-linkbutton" href="' + appRouter.getRouteUrl("recordedtv", {
                    serverId: apiClient.serverId()
                }) + '" class="raised"><span>' + globalize.translate("sharedcomponents#Recordings") + "</span></a>", html += '<a style="margin-left:.5em;margin-right:0;" is="emby-linkbutton" href="' + appRouter.getRouteUrl("livetv", {
                    serverId: apiClient.serverId(),
                    section: "dvrschedule"
                }) + '" class="raised"><span>' + globalize.translate("sharedcomponents#Schedule") + "</span></a>", html += "</div>", enableScrollX() && (html += "</div>"), html += "</div>", html += "</div>", html += '<div class="verticalSection">', html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">', layoutManager.tv ? html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate("sharedcomponents#HeaderOnNow") + "</h2>" : (html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl("livetv", {
                    serverId: apiClient.serverId(),
                    section: "onnow"
                }) + '" class="more button-flat button-flat-mini sectionTitleTextButton">', html += '<h2 class="sectionTitle sectionTitle-cards">', html += globalize.translate("sharedcomponents#HeaderOnNow"), html += "</h2>", html += '<i class="md-icon">&#xE5CC;</i>', html += "</a>"), html += "</div>", enableScrollX() ? html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right" data-refreshinterval="300000">' : html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x" data-refreshinterval="300000">', enableScrollX() && (html += "</div>"), html += "</div>", html += "</div>", elem.innerHTML = html;
                var itemsContainer = elem.querySelector(".itemsContainer");
                itemsContainer.parentContainer = elem, itemsContainer.fetchData = getOnNowFetchFn(apiClient.serverId()), itemsContainer.getItemsHtml = getOnNowItemsHtml
            } else result.Items.length && !registered && (elem.classList.add("padded-left"), elem.classList.add("padded-right"), elem.classList.add("padded-bottom"), html += '<h2 class="sectionTitle">' + globalize.translate("sharedcomponents#LiveTvRequiresUnlock") + "</h2>", html += '<button is="emby-button" type="button" class="raised button-submit block btnUnlock">', html += "<span>" + globalize.translate("sharedcomponents#HeaderBecomeProjectSupporter") + "</span>", html += "</button>", elem.innerHTML = html);
            bindUnlockClick(elem)
        })
    }

    function getNextUpFetchFn(serverId) {
        return function() {
            var apiClient = connectionManager.getApiClient(serverId);
            return apiClient.getNextUpEpisodes({
                Limit: enableScrollX() ? 24 : 15,
                Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated,BasicSyncInfo",
                UserId: apiClient.getCurrentUserId(),
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                EnableTotalRecordCount: !1
            })
        }
    }

    function getNextUpItemsHtml(items) {
        return cardBuilder.getCardsHtml({
            items: items,
            preferThumb: !0,
            shape: getThumbShape(),
            overlayText: !1,
            showTitle: !0,
            showParentTitle: !0,
            lazy: !0,
            overlayPlayButton: !0,
            context: "home",
            centerText: !0,
            allowBottomPadding: !enableScrollX(),
            cardLayout: !1
        })
    }

    function loadNextUp(elem, apiClient, userId) {
        var html = "";
        html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">', layoutManager.tv ? html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate("sharedcomponents#HeaderNextUp") + "</h2>" : (html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl("nextup", {
            serverId: apiClient.serverId()
        }) + '" class="button-flat button-flat-mini sectionTitleTextButton">', html += '<h2 class="sectionTitle sectionTitle-cards">', html += globalize.translate("sharedcomponents#HeaderNextUp"), html += "</h2>", html += '<i class="md-icon">&#xE5CC;</i>', html += "</a>"), html += "</div>", enableScrollX() ? html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right" data-monitor="videoplayback,markplayed">' : html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x" data-monitor="videoplayback,markplayed">', enableScrollX() && (html += "</div>"), html += "</div>", elem.classList.add("hide"), elem.innerHTML = html;
        var itemsContainer = elem.querySelector(".itemsContainer");
        itemsContainer.fetchData = getNextUpFetchFn(apiClient.serverId()), itemsContainer.getItemsHtml = getNextUpItemsHtml, itemsContainer.parentContainer = elem
    }

    function getLatestRecordingsFetchFn(serverId, activeRecordingsOnly) {
        return function() {
            var apiClient = connectionManager.getApiClient(serverId);
            return apiClient.getLiveTvRecordings({
                userId: apiClient.getCurrentUserId(),
                Limit: enableScrollX() ? 12 : 5,
                Fields: "PrimaryImageAspectRatio,BasicSyncInfo",
                EnableTotalRecordCount: !1,
                IsLibraryItem: !!activeRecordingsOnly && null,
                IsInProgress: !!activeRecordingsOnly || null
            })
        }
    }

    function getLatestRecordingItemsHtml(activeRecordingsOnly) {
        return function(items) {
            return cardBuilder.getCardsHtml({
                items: items,
                shape: enableScrollX() ? "autooverflow" : "auto",
                showTitle: !0,
                showParentTitle: !0,
                coverImage: !0,
                lazy: !0,
                showDetailsMenu: !0,
                centerText: !0,
                overlayText: !1,
                showYear: !0,
                lines: 2,
                overlayPlayButton: !activeRecordingsOnly,
                allowBottomPadding: !enableScrollX(),
                preferThumb: !0,
                cardLayout: !1,
                overlayMoreButton: activeRecordingsOnly,
                action: activeRecordingsOnly ? "none" : null,
                centerPlayButton: activeRecordingsOnly
            })
        }
    }

    function loadLatestLiveTvRecordings(elem, activeRecordingsOnly, apiClient, userId) {
        var title = activeRecordingsOnly ? globalize.translate("sharedcomponents#HeaderActiveRecordings") : globalize.translate("sharedcomponents#HeaderLatestRecordings"),
            html = "";
        html += '<div class="sectionTitleContainer sectionTitleContainer-cards">', html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + title + "</h2>", layoutManager.tv, html += "</div>", enableScrollX() ? html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right">' : html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">', enableScrollX() && (html += "</div>"), html += "</div>", elem.classList.add("hide"), elem.innerHTML = html;
        var itemsContainer = elem.querySelector(".itemsContainer");
        itemsContainer.fetchData = getLatestRecordingsFetchFn(apiClient.serverId(), activeRecordingsOnly), itemsContainer.getItemsHtml = getLatestRecordingItemsHtml(activeRecordingsOnly), itemsContainer.parentContainer = elem
    }
    return {
        loadLibraryTiles: loadLibraryTiles,
        getDefaultSection: getDefaultSection,
        loadSections: loadSections,
        destroySections: destroySections,
        pause: pause,
        resume: resume
    }
});
