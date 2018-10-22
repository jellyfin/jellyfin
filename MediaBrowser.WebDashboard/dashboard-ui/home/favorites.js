define(["appRouter", "cardBuilder", "dom", "globalize", "connectionManager", "apphost", "layoutManager", "focusManager", "emby-itemscontainer", "emby-scroller"], function(appRouter, cardBuilder, dom, globalize, connectionManager, appHost, layoutManager, focusManager) {
    "use strict";

    function enableScrollX() {
        return !0
    }

    function getThumbShape() {
        return enableScrollX() ? "overflowBackdrop" : "backdrop"
    }

    function getPosterShape() {
        return enableScrollX() ? "overflowPortrait" : "portrait"
    }

    function getSquareShape() {
        return enableScrollX() ? "overflowSquare" : "square"
    }

    function getSections() {
        return [{
            name: "sharedcomponents#HeaderFavoriteMovies",
            types: "Movie",
            shape: getPosterShape(),
            showTitle: !0,
            showYear: !0,
            overlayPlayButton: !0,
            overlayText: !1,
            centerText: !0
        }, {
            name: "sharedcomponents#HeaderFavoriteShows",
            types: "Series",
            shape: getPosterShape(),
            showTitle: !0,
            showYear: !0,
            overlayPlayButton: !0,
            overlayText: !1,
            centerText: !0
        }, {
            name: "sharedcomponents#HeaderFavoriteEpisodes",
            types: "Episode",
            shape: getThumbShape(),
            preferThumb: !1,
            showTitle: !0,
            showParentTitle: !0,
            overlayPlayButton: !0,
            overlayText: !1,
            centerText: !0
        }, {
            name: "sharedcomponents#HeaderFavoriteVideos",
            types: "Video",
            shape: getThumbShape(),
            preferThumb: !0,
            showTitle: !0,
            overlayPlayButton: !0,
            overlayText: !1,
            centerText: !0
        }, {
            name: "sharedcomponents#HeaderFavoriteCollections",
            types: "BoxSet",
            shape: getPosterShape(),
            showTitle: !0,
            overlayPlayButton: !0,
            overlayText: !1,
            centerText: !0
        }, {
            name: "sharedcomponents#HeaderFavoritePlaylists",
            types: "Playlist",
            shape: getSquareShape(),
            preferThumb: !1,
            showTitle: !0,
            overlayText: !1,
            showParentTitle: !1,
            centerText: !0,
            overlayPlayButton: !0,
            coverImage: !0
        }, {
            name: "sharedcomponents#HeaderFavoriteArtists",
            types: "MusicArtist",
            shape: getSquareShape(),
            preferThumb: !1,
            showTitle: !0,
            overlayText: !1,
            showParentTitle: !1,
            centerText: !0,
            overlayPlayButton: !0,
            coverImage: !0
        }, {
            name: "sharedcomponents#HeaderFavoriteAlbums",
            types: "MusicAlbum",
            shape: getSquareShape(),
            preferThumb: !1,
            showTitle: !0,
            overlayText: !1,
            showParentTitle: !0,
            centerText: !0,
            overlayPlayButton: !0,
            coverImage: !0
        }, {
            name: "sharedcomponents#HeaderFavoriteSongs",
            types: "Audio",
            shape: getSquareShape(),
            preferThumb: !1,
            showTitle: !0,
            overlayText: !1,
            showParentTitle: !0,
            centerText: !0,
            overlayMoreButton: !0,
            action: "instantmix",
            coverImage: !0
        }, {
            name: "sharedcomponents#HeaderFavoriteGames",
            types: "Game",
            shape: getSquareShape(),
            preferThumb: !1,
            showTitle: !0
        }]
    }

    function getFetchDataFn(section) {
        return function() {
            var apiClient = this.apiClient,
                options = {
                    SortBy: (section.types, "SeriesName,SortName"),
                    SortOrder: "Ascending",
                    Filters: "IsFavorite",
                    Recursive: !0,
                    Fields: "PrimaryImageAspectRatio,BasicSyncInfo",
                    CollapseBoxSetItems: !1,
                    ExcludeLocationTypes: "Virtual",
                    EnableTotalRecordCount: !1
                };
            options.Limit = 20;
            var userId = apiClient.getCurrentUserId();
            return "MusicArtist" === section.types ? apiClient.getArtists(userId, options) : (options.IncludeItemTypes = section.types, apiClient.getItems(userId, options))
        }
    }

    function getRouteUrl(section, serverId) {
        return appRouter.getRouteUrl("list", {
            serverId: serverId,
            itemTypes: section.types,
            isFavorite: !0
        })
    }

    function getItemsHtmlFn(section) {
        return function(items) {
            var supportsImageAnalysis = appHost.supports("imageanalysis"),
                cardLayout = (appHost.preferVisualCards || supportsImageAnalysis) && section.autoCardLayout && section.showTitle;
            cardLayout = !1;
            var serverId = this.apiClient.serverId(),
                leadingButtons = layoutManager.tv ? [{
                    name: globalize.translate("sharedcomponents#All"),
                    id: "more",
                    icon: "&#xE87D;",
                    routeUrl: getRouteUrl(section, serverId)
                }] : null,
                lines = 0;
            return section.showTitle && lines++, section.showYear && lines++, section.showParentTitle && lines++, cardBuilder.getCardsHtml({
                items: items,
                preferThumb: section.preferThumb,
                shape: section.shape,
                centerText: section.centerText && !cardLayout,
                overlayText: !1 !== section.overlayText,
                showTitle: section.showTitle,
                showYear: section.showYear,
                showParentTitle: section.showParentTitle,
                scalable: !0,
                coverImage: section.coverImage,
                overlayPlayButton: section.overlayPlayButton,
                overlayMoreButton: section.overlayMoreButton && !cardLayout,
                action: section.action,
                allowBottomPadding: !enableScrollX(),
                cardLayout: cardLayout,
                vibrant: supportsImageAnalysis && cardLayout,
                leadingButtons: leadingButtons,
                lines: lines
            })
        }
    }

    function FavoritesTab(view, params) {
        this.view = view, this.params = params, this.apiClient = connectionManager.currentApiClient(), this.sectionsContainer = view.querySelector(".sections"), createSections(this, this.sectionsContainer, this.apiClient)
    }

    function createSections(instance, elem, apiClient) {
        var i, length, sections = getSections(),
            html = "";
        for (i = 0, length = sections.length; i < length; i++) {
            var section = sections[i],
                sectionClass = "verticalSection";
            section.showTitle || (sectionClass += " verticalSection-extrabottompadding"), html += '<div class="' + sectionClass + ' hide">', html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">', layoutManager.tv ? html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate(section.name) + "</h2>" : (html += '<a is="emby-linkbutton" href="' + getRouteUrl(section, apiClient.serverId()) + '" class="more button-flat button-flat-mini sectionTitleTextButton">', html += '<h2 class="sectionTitle sectionTitle-cards">', html += globalize.translate(section.name), html += "</h2>", html += '<i class="md-icon">&#xE5CC;</i>', html += "</a>"), html += "</div>", html += '<div is="emby-scroller" class="padded-top-focusscale padded-bottom-focusscale" data-mousewheel="false" data-centerfocus="true"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right" data-monitor="markfavorite"></div></div>', html += "</div>"
        }
        elem.innerHTML = html;
        var elems = elem.querySelectorAll(".itemsContainer");
        for (i = 0, length = elems.length; i < length; i++) {
            var itemsContainer = elems[i];
            itemsContainer.fetchData = getFetchDataFn(sections[i]).bind(instance), itemsContainer.getItemsHtml = getItemsHtmlFn(sections[i]).bind(instance), itemsContainer.parentContainer = dom.parentWithClass(itemsContainer, "verticalSection")
        }
    }
    return FavoritesTab.prototype.onResume = function(options) {
        for (var promises = (this.apiClient, []), view = this.view, elems = this.sectionsContainer.querySelectorAll(".itemsContainer"), i = 0, length = elems.length; i < length; i++) promises.push(elems[i].resume(options));
        Promise.all(promises).then(function() {
            options.autoFocus && focusManager.autoFocus(view)
        })
    }, FavoritesTab.prototype.onPause = function() {
        for (var elems = this.sectionsContainer.querySelectorAll(".itemsContainer"), i = 0, length = elems.length; i < length; i++) elems[i].pause()
    }, FavoritesTab.prototype.destroy = function() {
        this.view = null, this.params = null, this.apiClient = null;
        for (var elems = this.sectionsContainer.querySelectorAll(".itemsContainer"), i = 0, length = elems.length; i < length; i++) elems[i].fetchData = null, elems[i].getItemsHtml = null, elems[i].parentContainer = null;
        this.sectionsContainer = null
    }, FavoritesTab
});