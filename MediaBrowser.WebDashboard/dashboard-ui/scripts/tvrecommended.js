define(["events", "inputManager", "libraryMenu", "layoutManager", "loading", "dom", "components/categorysyncbuttons", "userSettings", "cardBuilder", "playbackManager", "mainTabsManager", "scrollStyles", "emby-itemscontainer", "emby-button"], function(events, inputManager, libraryMenu, layoutManager, loading, dom, categorysyncbuttons, userSettings, cardBuilder, playbackManager, mainTabsManager) {
    "use strict";

    function getTabs() {
        return [{
            name: Globalize.translate("TabShows")
        }, {
            name: Globalize.translate("TabSuggestions")
        }, {
            name: Globalize.translate("TabLatest")
        }, {
            name: Globalize.translate("TabUpcoming")
        }, {
            name: Globalize.translate("TabGenres")
        }, {
            name: Globalize.translate("TabNetworks")
        }, {
            name: Globalize.translate("TabEpisodes")
        }, {
            name: Globalize.translate("ButtonSearch"),
            cssClass: "searchTabButton"
        }]
    }

    function getDefaultTabIndex(folderId) {
        switch (userSettings.get("landing-" + folderId)) {
            case "suggestions":
                return 1;
            case "latest":
                return 2;
            case "favorites":
                return 1;
            case "genres":
                return 4;
            default:
                return 0
        }
    }

    function setScrollClasses(elem, scrollX) {
        scrollX ? (elem.classList.add("hiddenScrollX"), layoutManager.tv && elem.classList.add("smoothScrollX"), elem.classList.add("scrollX"), elem.classList.remove("vertical-wrap")) : (elem.classList.remove("hiddenScrollX"), elem.classList.remove("smoothScrollX"), elem.classList.remove("scrollX"), elem.classList.add("vertical-wrap"))
    }
    return function(view, params) {
        function reload() {
            loading.show(), loadResume(), loadNextUp()
        }

        function loadNextUp() {
            var query = {
                Limit: 24,
                Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated,BasicSyncInfo",
                UserId: ApiClient.getCurrentUserId(),
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb",
                EnableTotalRecordCount: !1
            };
            query.ParentId = libraryMenu.getTopParentId(), ApiClient.getNextUpEpisodes(query).then(function(result) {
                result.Items.length ? view.querySelector(".noNextUpItems").classList.add("hide") : view.querySelector(".noNextUpItems").classList.remove("hide");
                var container = view.querySelector("#nextUpItems");
                cardBuilder.buildCards(result.Items, {
                    itemsContainer: container,
                    preferThumb: !0,
                    shape: "backdrop",
                    scalable: !0,
                    showTitle: !0,
                    showParentTitle: !0,
                    overlayText: !1,
                    centerText: !0,
                    overlayPlayButton: !0,
                    cardLayout: !1
                }), loading.hide()
            })
        }

        function enableScrollX() {
            return !layoutManager.desktop
        }

        function getThumbShape() {
            return enableScrollX() ? "overflowBackdrop" : "backdrop"
        }

        function loadResume() {
            var parentId = libraryMenu.getTopParentId(),
                screenWidth = dom.getWindowSize().innerWidth,
                limit = screenWidth >= 1600 ? 5 : 6,
                options = {
                    SortBy: "DatePlayed",
                    SortOrder: "Descending",
                    IncludeItemTypes: "Episode",
                    Filters: "IsResumable",
                    Limit: limit,
                    Recursive: !0,
                    Fields: "PrimaryImageAspectRatio,SeriesInfo,UserData,BasicSyncInfo",
                    ExcludeLocationTypes: "Virtual",
                    ParentId: parentId,
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Thumb",
                    EnableTotalRecordCount: !1
                };
            ApiClient.getItems(ApiClient.getCurrentUserId(), options).then(function(result) {
                result.Items.length ? view.querySelector("#resumableSection").classList.remove("hide") : view.querySelector("#resumableSection").classList.add("hide");
                var allowBottomPadding = !enableScrollX(),
                    container = view.querySelector("#resumableItems");
                cardBuilder.buildCards(result.Items, {
                    itemsContainer: container,
                    preferThumb: !0,
                    shape: getThumbShape(),
                    scalable: !0,
                    showTitle: !0,
                    showParentTitle: !0,
                    overlayText: !1,
                    centerText: !0,
                    overlayPlayButton: !0,
                    allowBottomPadding: allowBottomPadding,
                    cardLayout: !1
                })
            })
        }

        function onBeforeTabChange(e) {
            preLoadTab(view, parseInt(e.detail.selectedTabIndex))
        }

        function onTabChange(e) {
            var newIndex = parseInt(e.detail.selectedTabIndex);
            loadTab(view, newIndex)
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
                    depends.push("scripts/tvshows");
                    break;
                case 1:
                    break;
                case 2:
                    depends.push("scripts/tvlatest");
                    break;
                case 3:
                    depends.push("scripts/tvupcoming");
                    break;
                case 4:
                    depends.push("scripts/tvgenres");
                    break;
                case 5:
                    depends.push("scripts/tvstudios");
                    break;
                case 6:
                    depends.push("scripts/episodes");
                    break;
                case 7:
                    depends.push("scripts/searchtab")
            }
            require(depends, function(controllerFactory) {
                var tabContent;
                1 === index && (tabContent = view.querySelector(".pageTabContent[data-index='" + index + "']"), self.tabContent = tabContent);
                var controller = tabControllers[index];
                controller || (tabContent = view.querySelector(".pageTabContent[data-index='" + index + "']"), controller = 1 === index ? self : 7 === index ? new controllerFactory(view, tabContent, {
                    collectionType: "tvshows",
                    parentId: params.topParentId
                }) : new controllerFactory(view, params, tabContent), tabControllers[index] = controller, controller.initTab && controller.initTab()), callback(controller)
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

        function onPlaybackStop(e, state) {
            state.NowPlayingItem && "Video" == state.NowPlayingItem.MediaType && (renderedTabs = [], mainTabsManager.getTabsElement().triggerTabChange())
        }

        function onWebSocketMessage(e, data) {
            var msg = data;
            "UserDataChanged" === msg.MessageType && msg.Data.UserId == ApiClient.getCurrentUserId() && (renderedTabs = [])
        }

        function onInputCommand(e) {
            switch (e.detail.command) {
                case "search":
                    e.preventDefault(), Dashboard.navigate("search.html?collectionType=tv&parentId=" + params.topParentId)
            }
        }
        var isViewRestored, self = this,
            currentTabIndex = parseInt(params.tab || getDefaultTabIndex(params.topParentId)),
            initialTabIndex = currentTabIndex;
        self.initTab = function() {
            var tabContent = self.tabContent;
            setScrollClasses(tabContent.querySelector("#resumableItems"), enableScrollX()), categorysyncbuttons.init(tabContent)
        }, self.renderTab = function() {
            reload()
        };
        var tabControllers = [],
            renderedTabs = [];
        setScrollClasses(view.querySelector("#resumableItems"), enableScrollX()), view.addEventListener("viewshow", function(e) {
            if (isViewRestored = e.detail.isRestored, initTabs(), !view.getAttribute("data-title")) {
                var parentId = params.topParentId;
                parentId ? ApiClient.getItem(ApiClient.getCurrentUserId(), parentId).then(function(item) {
                    view.setAttribute("data-title", item.Name), libraryMenu.setTitle(item.Name)
                }) : (view.setAttribute("data-title", Globalize.translate("TabShows")), libraryMenu.setTitle(Globalize.translate("TabShows")))
            }
            events.on(playbackManager, "playbackstop", onPlaybackStop), events.on(ApiClient, "message", onWebSocketMessage), inputManager.on(window, onInputCommand)
        }), view.addEventListener("viewbeforehide", function(e) {
            inputManager.off(window, onInputCommand), events.off(playbackManager, "playbackstop", onPlaybackStop), events.off(ApiClient, "message", onWebSocketMessage)
        }), view.addEventListener("viewdestroy", function(e) {
            tabControllers.forEach(function(t) {
                t.destroy && t.destroy()
            })
        })
    }
});