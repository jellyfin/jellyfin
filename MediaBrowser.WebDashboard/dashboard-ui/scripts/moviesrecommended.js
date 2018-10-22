define(["events", "layoutManager", "inputManager", "userSettings", "libraryMenu", "mainTabsManager", "components/categorysyncbuttons", "cardBuilder", "dom", "imageLoader", "playbackManager", "emby-itemscontainer", "emby-tabs", "emby-button"], function(events, layoutManager, inputManager, userSettings, libraryMenu, mainTabsManager, categorysyncbuttons, cardBuilder, dom, imageLoader, playbackManager) {
    "use strict";

    function enableScrollX() {
        return !layoutManager.desktop
    }

    function getPortraitShape() {
        return enableScrollX() ? "overflowPortrait" : "portrait"
    }

    function getThumbShape() {
        return enableScrollX() ? "overflowBackdrop" : "backdrop"
    }

    function loadLatest(page, userId, parentId) {
        var options = {
            IncludeItemTypes: "Movie",
            Limit: 18,
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,BasicSyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: !1
        };
        ApiClient.getJSON(ApiClient.getUrl("Users/" + userId + "/Items/Latest", options)).then(function(items) {
            var allowBottomPadding = !enableScrollX(),
                container = page.querySelector("#recentlyAddedItems");
            cardBuilder.buildCards(items, {
                itemsContainer: container,
                shape: getPortraitShape(),
                scalable: !0,
                overlayPlayButton: !0,
                allowBottomPadding: allowBottomPadding,
                showTitle: !0,
                showYear: !0,
                centerText: !0
            })
        })
    }

    function loadResume(page, userId, parentId) {
        var screenWidth = dom.getWindowSize().innerWidth,
            options = {
                SortBy: "DatePlayed",
                SortOrder: "Descending",
                IncludeItemTypes: "Movie",
                Filters: "IsResumable",
                Limit: screenWidth >= 1920 ? 5 : screenWidth >= 1600 ? 5 : 3,
                Recursive: !0,
                Fields: "PrimaryImageAspectRatio,MediaSourceCount,BasicSyncInfo",
                CollapseBoxSetItems: !1,
                ParentId: parentId,
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                EnableTotalRecordCount: !1
            };
        ApiClient.getItems(userId, options).then(function(result) {
            result.Items.length ? page.querySelector("#resumableSection").classList.remove("hide") : page.querySelector("#resumableSection").classList.add("hide");
            var allowBottomPadding = !enableScrollX(),
                container = page.querySelector("#resumableItems");
            cardBuilder.buildCards(result.Items, {
                itemsContainer: container,
                preferThumb: !0,
                shape: getThumbShape(),
                scalable: !0,
                overlayPlayButton: !0,
                allowBottomPadding: allowBottomPadding,
                cardLayout: !1,
                showTitle: !0,
                showYear: !0,
                centerText: !0
            })
        })
    }

    function getRecommendationHtml(recommendation) {
        var html = "",
            title = "";
        switch (recommendation.RecommendationType) {
            case "SimilarToRecentlyPlayed":
                title = Globalize.translate("RecommendationBecauseYouWatched").replace("{0}", recommendation.BaselineItemName);
                break;
            case "SimilarToLikedItem":
                title = Globalize.translate("RecommendationBecauseYouLike").replace("{0}", recommendation.BaselineItemName);
                break;
            case "HasDirectorFromRecentlyPlayed":
            case "HasLikedDirector":
                title = Globalize.translate("RecommendationDirectedBy").replace("{0}", recommendation.BaselineItemName);
                break;
            case "HasActorFromRecentlyPlayed":
            case "HasLikedActor":
                title = Globalize.translate("RecommendationStarring").replace("{0}", recommendation.BaselineItemName)
        }
        html += '<div class="verticalSection">', html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + title + "</h2>";
        var allowBottomPadding = !0;
        return enableScrollX() ? (allowBottomPadding = !1, html += '<div is="emby-itemscontainer" class="itemsContainer scrollX hiddenScrollX padded-left padded-right">') : html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap padded-left padded-right">', html += cardBuilder.getCardsHtml(recommendation.Items, {
            shape: getPortraitShape(),
            scalable: !0,
            overlayPlayButton: !0,
            allowBottomPadding: allowBottomPadding
        }), html += "</div>", html += "</div>"
    }

    function loadSuggestions(page, userId, parentId) {
        var screenWidth = dom.getWindowSize().innerWidth,
            url = ApiClient.getUrl("Movies/Recommendations", {
                userId: userId,
                categoryLimit: 6,
                ItemLimit: screenWidth >= 1920 ? 8 : screenWidth >= 1600 ? 8 : screenWidth >= 1200 ? 6 : 5,
                Fields: "PrimaryImageAspectRatio,MediaSourceCount,BasicSyncInfo",
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
            });
        ApiClient.getJSON(url).then(function(recommendations) {
            if (!recommendations.length) return page.querySelector(".noItemsMessage").classList.remove("hide"), void(page.querySelector(".recommendations").innerHTML = "");
            var html = recommendations.map(getRecommendationHtml).join("");
            page.querySelector(".noItemsMessage").classList.add("hide");
            var recs = page.querySelector(".recommendations");
            recs.innerHTML = html, imageLoader.lazyChildren(recs)
        })
    }

    function setScrollClasses(elem, scrollX) {
        scrollX ? (elem.classList.add("hiddenScrollX"), layoutManager.tv && elem.classList.add("smoothScrollX"), elem.classList.add("scrollX"), elem.classList.remove("vertical-wrap")) : (elem.classList.remove("hiddenScrollX"), elem.classList.remove("smoothScrollX"), elem.classList.remove("scrollX"), elem.classList.add("vertical-wrap"))
    }

    function initSuggestedTab(page, tabContent) {
        for (var containers = tabContent.querySelectorAll(".itemsContainer"), i = 0, length = containers.length; i < length; i++) setScrollClasses(containers[i], enableScrollX())
    }

    function loadSuggestionsTab(view, params, tabContent) {
        var parentId = params.topParentId,
            userId = ApiClient.getCurrentUserId();
        console.log("loadSuggestionsTab"), loadResume(tabContent, userId, parentId), loadLatest(tabContent, userId, parentId), loadSuggestions(tabContent, userId, parentId)
    }

    function getTabs() {
        return [{
            name: Globalize.translate("sharedcomponents#Movies")
        }, {
            name: Globalize.translate("TabSuggestions")
        }, {
            name: Globalize.translate("TabTrailers")
        }, {
            name: Globalize.translate("TabFavorites")
        }, {
            name: Globalize.translate("TabCollections")
        }, {
            name: Globalize.translate("TabGenres")
        }, {
            name: Globalize.translate("ButtonSearch"),
            cssClass: "searchTabButton"
        }]
    }

    function getDefaultTabIndex(folderId) {
        switch (userSettings.get("landing-" + folderId)) {
            case "suggestions":
                return 1;
            case "favorites":
                return 3;
            case "collections":
                return 4;
            case "genres":
                return 5;
            default:
                return 0
        }
    }
    return function(view, params) {
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
                    depends.push("scripts/movies");
                    break;
                case 1:
                    break;
                case 2:
                    depends.push("scripts/movietrailers");
                    break;
                case 3:
                    depends.push("scripts/movies");
                    break;
                case 4:
                    depends.push("scripts/moviecollections");
                    break;
                case 5:
                    depends.push("scripts/moviegenres");
                    break;
                case 6:
                    depends.push("scripts/searchtab")
            }
            require(depends, function(controllerFactory) {
                var tabContent;
                index === suggestionsTabIndex && (tabContent = view.querySelector(".pageTabContent[data-index='" + index + "']"), self.tabContent = tabContent);
                var controller = tabControllers[index];
                controller || (tabContent = view.querySelector(".pageTabContent[data-index='" + index + "']"), controller = index === suggestionsTabIndex ? self : 6 === index ? new controllerFactory(view, tabContent, {
                    collectionType: "movies",
                    parentId: params.topParentId
                }) : 0 === index || 3 === index ? new controllerFactory(view, params, tabContent, {
                    mode: index ? "favorites" : "movies"
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

        function onInputCommand(e) {
            switch (e.detail.command) {
                case "search":
                    e.preventDefault(), Dashboard.navigate("search.html?collectionType=movies&parentId=" + params.topParentId)
            }
        }
        var isViewRestored, self = this,
            currentTabIndex = parseInt(params.tab || getDefaultTabIndex(params.topParentId)),
            initialTabIndex = currentTabIndex,
            suggestionsTabIndex = 1;
        self.initTab = function() {
            var tabContent = view.querySelector(".pageTabContent[data-index='" + suggestionsTabIndex + "']");
            categorysyncbuttons.init(tabContent), initSuggestedTab(view, tabContent)
        }, self.renderTab = function() {
            var tabContent = view.querySelector(".pageTabContent[data-index='" + suggestionsTabIndex + "']");
            loadSuggestionsTab(view, params, tabContent)
        };
        var tabControllers = [],
            renderedTabs = [];
        view.addEventListener("viewshow", function(e) {
            if (isViewRestored = e.detail.isRestored, initTabs(), !view.getAttribute("data-title")) {
                var parentId = params.topParentId;
                parentId ? ApiClient.getItem(ApiClient.getCurrentUserId(), parentId).then(function(item) {
                    view.setAttribute("data-title", item.Name), libraryMenu.setTitle(item.Name)
                }) : (view.setAttribute("data-title", Globalize.translate("TabMovies")), libraryMenu.setTitle(Globalize.translate("TabMovies")))
            }
            events.on(playbackManager, "playbackstop", onPlaybackStop), inputManager.on(window, onInputCommand)
        }), view.addEventListener("viewbeforehide", function(e) {
            inputManager.off(window, onInputCommand)
        }), view.addEventListener("viewdestroy", function(e) {
            tabControllers.forEach(function(t) {
                t.destroy && t.destroy()
            })
        })
    }
});