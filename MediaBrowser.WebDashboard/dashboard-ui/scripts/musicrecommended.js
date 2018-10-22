define(["browser", "layoutManager", "userSettings", "inputManager", "loading", "cardBuilder", "dom", "apphost", "imageLoader", "libraryMenu", "playbackManager", "mainTabsManager", "scrollStyles", "emby-itemscontainer", "emby-tabs", "emby-button", "flexStyles"], function(browser, layoutManager, userSettings, inputManager, loading, cardBuilder, dom, appHost, imageLoader, libraryMenu, playbackManager, mainTabsManager) {
    "use strict";

    function itemsPerRow() {
        var screenWidth = dom.getWindowSize().innerWidth;
        return screenWidth >= 1920 ? 9 : screenWidth >= 1200 ? 12 : screenWidth >= 1e3 ? 10 : 8
    }

    function enableScrollX() {
        return !layoutManager.desktop
    }

    function getSquareShape() {
        return enableScrollX() ? "overflowSquare" : "square"
    }

    function loadLatest(page, parentId) {
        loading.show();
        var userId = ApiClient.getCurrentUserId(),
            options = {
                IncludeItemTypes: "Audio",
                Limit: enableScrollX() ? 3 * itemsPerRow() : 2 * itemsPerRow(),
                Fields: "PrimaryImageAspectRatio,BasicSyncInfo",
                ParentId: parentId,
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                EnableTotalRecordCount: !1
            };
        ApiClient.getJSON(ApiClient.getUrl("Users/" + userId + "/Items/Latest", options)).then(function(items) {
            var elem = page.querySelector("#recentlyAddedSongs"),
                supportsImageAnalysis = appHost.supports("imageanalysis");
            supportsImageAnalysis = !1, elem.innerHTML = cardBuilder.getCardsHtml({
                items: items,
                showUnplayedIndicator: !1,
                showLatestItemsPopup: !1,
                shape: getSquareShape(),
                showTitle: !0,
                showParentTitle: !0,
                lazy: !0,
                centerText: !supportsImageAnalysis,
                overlayPlayButton: !supportsImageAnalysis,
                allowBottomPadding: !enableScrollX(),
                cardLayout: supportsImageAnalysis,
                vibrant: supportsImageAnalysis,
                coverImage: !0
            }), imageLoader.lazyChildren(elem), loading.hide()
        })
    }

    function loadRecentlyPlayed(page, parentId) {
        var options = {
            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: itemsPerRow(),
            Recursive: !0,
            Fields: "PrimaryImageAspectRatio,AudioInfo",
            Filters: "IsPlayed",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: !1
        };
        ApiClient.getItems(ApiClient.getCurrentUserId(), options).then(function(result) {
            var elem = page.querySelector("#recentlyPlayed");
            result.Items.length ? elem.classList.remove("hide") : elem.classList.add("hide");
            var itemsContainer = elem.querySelector(".itemsContainer"),
                supportsImageAnalysis = appHost.supports("imageanalysis");
            supportsImageAnalysis = !1, itemsContainer.innerHTML = cardBuilder.getCardsHtml({
                items: result.Items,
                showUnplayedIndicator: !1,
                shape: getSquareShape(),
                showTitle: !0,
                showParentTitle: !0,
                action: "instantmix",
                lazy: !0,
                centerText: !supportsImageAnalysis,
                overlayMoreButton: !supportsImageAnalysis,
                allowBottomPadding: !enableScrollX(),
                cardLayout: supportsImageAnalysis,
                vibrant: supportsImageAnalysis,
                coverImage: !0
            }), imageLoader.lazyChildren(itemsContainer)
        })
    }

    function loadFrequentlyPlayed(page, parentId) {
        var options = {
            SortBy: "PlayCount",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: itemsPerRow(),
            Recursive: !0,
            Fields: "PrimaryImageAspectRatio,AudioInfo",
            Filters: "IsPlayed",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: !1
        };
        ApiClient.getItems(ApiClient.getCurrentUserId(), options).then(function(result) {
            var elem = page.querySelector("#topPlayed");
            result.Items.length ? elem.classList.remove("hide") : elem.classList.add("hide");
            var itemsContainer = elem.querySelector(".itemsContainer"),
                supportsImageAnalysis = appHost.supports("imageanalysis");
            supportsImageAnalysis = !1, itemsContainer.innerHTML = cardBuilder.getCardsHtml({
                items: result.Items,
                showUnplayedIndicator: !1,
                shape: getSquareShape(),
                showTitle: !0,
                showParentTitle: !0,
                action: "instantmix",
                lazy: !0,
                centerText: !supportsImageAnalysis,
                overlayMoreButton: !supportsImageAnalysis,
                allowBottomPadding: !enableScrollX(),
                cardLayout: supportsImageAnalysis,
                vibrant: supportsImageAnalysis,
                coverImage: !0
            }), imageLoader.lazyChildren(itemsContainer)
        })
    }

    function loadSuggestionsTab(page, tabContent, parentId) {
        console.log("loadSuggestionsTab"), loadLatest(tabContent, parentId), loadRecentlyPlayed(tabContent, parentId), loadFrequentlyPlayed(tabContent, parentId), require(["components/favoriteitems"], function(favoriteItems) {
            favoriteItems.render(tabContent, ApiClient.getCurrentUserId(), parentId, ["favoriteArtists", "favoriteAlbums", "favoriteSongs"])
        })
    }

    function getTabs() {
        return [{
            name: Globalize.translate("TabSuggestions")
        }, {
            name: Globalize.translate("TabAlbums")
        }, {
            name: Globalize.translate("TabAlbumArtists")
        }, {
            name: Globalize.translate("TabArtists")
        }, {
            name: Globalize.translate("TabPlaylists")
        }, {
            name: Globalize.translate("TabSongs")
        }, {
            name: Globalize.translate("TabGenres")
        }, {
            name: Globalize.translate("ButtonSearch"),
            cssClass: "searchTabButton"
        }]
    }

    function getDefaultTabIndex(folderId) {
        switch (userSettings.get("landing-" + folderId)) {
            case "albums":
                return 1;
            case "albumartists":
                return 2;
            case "artists":
                return 3;
            case "playlists":
                return 4;
            case "songs":
                return 5;
            case "genres":
                return 6;
            default:
                return 0
        }
    }
    return function(view, params) {
        function reload() {
            loading.show();
            var tabContent = view.querySelector(".pageTabContent[data-index='0']");
            loadSuggestionsTab(view, tabContent, params.topParentId)
        }

        function enableScrollX() {
            return browser.mobile
        }

        function setScrollClasses(elem, scrollX) {
            scrollX ? (elem.classList.add("hiddenScrollX"), layoutManager.tv && elem.classList.add("smoothScrollX"), elem.classList.add("scrollX"), elem.classList.remove("vertical-wrap")) : (elem.classList.remove("hiddenScrollX"), elem.classList.remove("smoothScrollX"), elem.classList.remove("scrollX"), elem.classList.add("vertical-wrap"))
        }

        function onBeforeTabChange(e) {
            preLoadTab(view, parseInt(e.detail.selectedTabIndex))
        }

        function onTabChange(e) {
            loadTab(view, parseInt(e.detail.selectedTabIndex))
        }

        function getTabContainers() {
            return view.querySelectorAll(".pageTabContent")
        }

        function initTabs() {
            mainTabsManager.setTabs(view, currentTabIndex, getTabs, getTabContainers, onBeforeTabChange, onTabChange)
        }

        function getTabController(page, index, callback) {
            var depends = [];
            switch (index) {
                case 0:
                    break;
                case 1:
                    depends.push("scripts/musicalbums");
                    break;
                case 2:
                case 3:
                    depends.push("scripts/musicartists");
                    break;
                case 4:
                    depends.push("scripts/musicplaylists");
                    break;
                case 5:
                    depends.push("scripts/songs");
                    break;
                case 6:
                    depends.push("scripts/musicgenres");
                    break;
                case 7:
                    depends.push("scripts/searchtab")
            }
            require(depends, function(controllerFactory) {
                var tabContent;
                0 == index && (tabContent = view.querySelector(".pageTabContent[data-index='" + index + "']"), self.tabContent = tabContent);
                var controller = tabControllers[index];
                controller || (tabContent = view.querySelector(".pageTabContent[data-index='" + index + "']"), controller = 0 === index ? self : 7 === index ? new controllerFactory(view, tabContent, {
                    collectionType: "music",
                    parentId: params.topParentId
                }) : new controllerFactory(view, params, tabContent), 2 == index ? controller.mode = "albumartists" : 3 == index && (controller.mode = "artists"), tabControllers[index] = controller, controller.initTab && controller.initTab()), callback(controller)
            })
        }

        function preLoadTab(page, index) {
            getTabController(page, index, function(controller) {
                -1 == renderedTabs.indexOf(index) && controller.preRender && controller.preRender()
            })
        }

        function loadTab(page, index) {
            currentTabIndex = index, getTabController(page, index, function(controller) {
                initialTabIndex = null, -1 == renderedTabs.indexOf(index) && (renderedTabs.push(index), controller.renderTab())
            })
        }

        function onInputCommand(e) {
            switch (e.detail.command) {
                case "search":
                    e.preventDefault(), Dashboard.navigate("search.html?collectionType=music&parentId=" + params.topParentId)
            }
        }
        var isViewRestored, self = this,
            currentTabIndex = parseInt(params.tab || getDefaultTabIndex(params.topParentId)),
            initialTabIndex = currentTabIndex;
        self.initTab = function() {
            for (var tabContent = view.querySelector(".pageTabContent[data-index='0']"), containers = tabContent.querySelectorAll(".itemsContainer"), i = 0, length = containers.length; i < length; i++) setScrollClasses(containers[i], enableScrollX())
        }, self.renderTab = function() {
            reload()
        };
        var tabControllers = [],
            renderedTabs = [];
        view.addEventListener("viewshow", function(e) {
            if (isViewRestored = e.detail.isRestored, initTabs(), !view.getAttribute("data-title")) {
                var parentId = params.topParentId;
                parentId ? ApiClient.getItem(ApiClient.getCurrentUserId(), parentId).then(function(item) {
                    view.setAttribute("data-title", item.Name), libraryMenu.setTitle(item.Name)
                }) : (view.setAttribute("data-title", Globalize.translate("TabMusic")), libraryMenu.setTitle(Globalize.translate("TabMusic")))
            }
            inputManager.on(window, onInputCommand)
        }), view.addEventListener("viewbeforehide", function(e) {
            inputManager.off(window, onInputCommand)
        }), view.addEventListener("viewdestroy", function(e) {
            tabControllers.forEach(function(t) {
                t.destroy && t.destroy()
            })
        })
    }
});