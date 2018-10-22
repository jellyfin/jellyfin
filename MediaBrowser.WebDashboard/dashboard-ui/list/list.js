define(["globalize", "listView", "layoutManager", "userSettings", "focusManager", "cardBuilder", "loading", "connectionManager", "alphaNumericShortcuts", "scroller", "playbackManager", "alphaPicker", "emby-itemscontainer", "emby-scroller"], function(globalize, listView, layoutManager, userSettings, focusManager, cardBuilder, loading, connectionManager, AlphaNumericShortcuts, scroller, playbackManager, alphaPicker) {
    "use strict";

    function getInitialLiveTvQuery(instance, params) {
        var query = {
            UserId: connectionManager.getApiClient(params.serverId).getCurrentUserId(),
            StartIndex: 0,
            Fields: "ChannelInfo,PrimaryImageAspectRatio",
            Limit: 300
        };
        return "Recordings" === params.type ? query.IsInProgress = !1 : query.HasAired = !1, params.genreId && (query.GenreIds = params.genreId), "true" === params.IsMovie ? query.IsMovie = !0 : "false" === params.IsMovie && (query.IsMovie = !1), "true" === params.IsSeries ? query.IsSeries = !0 : "false" === params.IsSeries && (query.IsSeries = !1), "true" === params.IsNews ? query.IsNews = !0 : "false" === params.IsNews && (query.IsNews = !1), "true" === params.IsSports ? query.IsSports = !0 : "false" === params.IsSports && (query.IsSports = !1), "true" === params.IsKids ? query.IsKids = !0 : "false" === params.IsKids && (query.IsKids = !1), "true" === params.IsAiring ? query.IsAiring = !0 : "false" === params.IsAiring && (query.IsAiring = !1), modifyQueryWithFilters(instance, query)
    }

    function modifyQueryWithFilters(instance, query) {
        var sortValues = instance.getSortValues();
        query.SortBy || (query.SortBy = sortValues.sortBy, query.SortOrder = sortValues.sortOrder), query.Fields = query.Fields ? query.Fields + ",PrimaryImageAspectRatio" : "PrimaryImageAspectRatio", query.ImageTypeLimit = 1;
        var hasFilters, queryFilters = [],
            filters = instance.getFilters();
        return filters.IsPlayed && (queryFilters.push("IsPlayed"), hasFilters = !0), filters.IsUnplayed && (queryFilters.push("IsUnplayed"), hasFilters = !0), filters.IsFavorite && (queryFilters.push("IsFavorite"), hasFilters = !0), filters.IsResumable && (queryFilters.push("IsResumable"), hasFilters = !0), filters.VideoTypes && (hasFilters = !0, query.VideoTypes = filters.VideoTypes), filters.GenreIds && (hasFilters = !0, query.GenreIds = filters.GenreIds), filters.Is4K && (query.Is4K = !0, hasFilters = !0), filters.IsHD && (query.IsHD = !0, hasFilters = !0), filters.IsSD && (query.IsHD = !1, hasFilters = !0), filters.Is3D && (query.Is3D = !0, hasFilters = !0), filters.HasSubtitles && (query.HasSubtitles = !0, hasFilters = !0), filters.HasTrailer && (query.HasTrailer = !0, hasFilters = !0), filters.HasSpecialFeature && (query.HasSpecialFeature = !0, hasFilters = !0), filters.HasThemeSong && (query.HasThemeSong = !0, hasFilters = !0), filters.HasThemeVideo && (query.HasThemeVideo = !0, hasFilters = !0), query.Filters = queryFilters.length ? queryFilters.join(",") : null, instance.setFilterStatus(hasFilters), instance.alphaPicker && (query.NameStartsWithOrGreater = instance.alphaPicker.value()), query
    }

    function updateSortText(instance) {
        var btnSortText = instance.btnSortText;
        if (btnSortText) {
            for (var options = instance.getSortMenuOptions(), values = instance.getSortValues(), sortBy = values.sortBy, i = 0, length = options.length; i < length; i++)
                if (sortBy === options[i].value) {
                    btnSortText.innerHTML = globalize.translate("sharedcomponents#SortByValue", options[i].name);
                    break
                } var btnSortIcon = instance.btnSortIcon;
            btnSortIcon && (btnSortIcon.innerHTML = "Descending" === values.sortOrder ? "&#xE5DB;" : "&#xE5D8;")
        }
    }

    function updateItemsContainerForViewType(instance) {
        "list" === instance.getViewSettings().imageType ? (instance.itemsContainer.classList.remove("vertical-wrap"), instance.itemsContainer.classList.add("vertical-list")) : (instance.itemsContainer.classList.add("vertical-wrap"), instance.itemsContainer.classList.remove("vertical-list"))
    }

    function updateAlphaPickerState(instance, numItems) {
        if (instance.alphaPicker) {
            var alphaPicker = instance.alphaPickerElement;
            if (alphaPicker) {
                var values = instance.getSortValues();
                null == numItems && (numItems = 100), "SortName" === values.sortBy && "Ascending" === values.sortOrder && numItems > 40 ? (alphaPicker.classList.remove("hide"), layoutManager.tv ? instance.itemsContainer.parentNode.classList.add("padded-left-withalphapicker") : instance.itemsContainer.parentNode.classList.add("padded-right-withalphapicker")) : (alphaPicker.classList.add("hide"), instance.itemsContainer.parentNode.classList.remove("padded-left-withalphapicker"), instance.itemsContainer.parentNode.classList.remove("padded-right-withalphapicker"))
            }
        }
    }

    function getItems(instance, params, item, sortBy, startIndex, limit) {
        var apiClient = connectionManager.getApiClient(params.serverId);
        if (instance.queryRecursive = !1, "Recordings" === params.type) return apiClient.getLiveTvRecordings(getInitialLiveTvQuery(instance, params));
        if ("Programs" === params.type) return "true" === params.IsAiring ? apiClient.getLiveTvRecommendedPrograms(getInitialLiveTvQuery(instance, params)) : apiClient.getLiveTvPrograms(getInitialLiveTvQuery(instance, params));
        if ("nextup" === params.type) return apiClient.getNextUpEpisodes(modifyQueryWithFilters(instance, {
            Limit: limit,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated,BasicSyncInfo",
            UserId: apiClient.getCurrentUserId(),
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Thumb",
            EnableTotalRecordCount: !1,
            SortBy: sortBy
        }));
        if (!item) {
            instance.queryRecursive = !0;
            var method = "getItems";
            return "MusicArtist" === params.type ? method = "getArtists" : "Person" === params.type && (method = "getPeople"), apiClient[method](apiClient.getCurrentUserId(), modifyQueryWithFilters(instance, {
                StartIndex: startIndex,
                Limit: limit,
                Fields: "PrimaryImageAspectRatio,SortName",
                ImageTypeLimit: 1,
                IncludeItemTypes: "MusicArtist" === params.type || "Person" === params.type ? null : params.type,
                Recursive: !0,
                IsFavorite: "true" === params.IsFavorite || null,
                ArtistIds: params.artistId || null,
                SortBy: sortBy
            }))
        }
        if ("Genre" === item.Type || "GameGenre" === item.Type || "MusicGenre" === item.Type || "Studio" === item.Type || "Person" === item.Type) {
            instance.queryRecursive = !0;
            var query = {
                StartIndex: startIndex,
                Limit: limit,
                Fields: "PrimaryImageAspectRatio,SortName",
                Recursive: !0,
                parentId: params.parentId,
                SortBy: sortBy
            };
            return "Studio" === item.Type ? query.StudioIds = item.Id : "Genre" === item.Type || "GameGenre" === item.Type || "MusicGenre" === item.Type ? query.GenreIds = item.Id : "Person" === item.Type && (query.PersonIds = item.Id), "MusicGenre" === item.Type ? query.IncludeItemTypes = "MusicAlbum" : "GameGenre" === item.Type ? query.IncludeItemTypes = "Game" : "movies" === item.CollectionType ? query.IncludeItemTypes = "Movie" : "tvshows" === item.CollectionType ? query.IncludeItemTypes = "Series" : "Genre" === item.Type ? query.IncludeItemTypes = "Movie,Series,Video" : "Person" === item.Type && (query.IncludeItemTypes = params.type), apiClient.getItems(apiClient.getCurrentUserId(), modifyQueryWithFilters(instance, query))
        }
        return apiClient.getItems(apiClient.getCurrentUserId(), modifyQueryWithFilters(instance, {
            StartIndex: startIndex,
            Limit: limit,
            Fields: "PrimaryImageAspectRatio,SortName",
            ImageTypeLimit: 1,
            ParentId: item.Id,
            SortBy: sortBy
        }))
    }

    function getItem(params) {
        if ("Recordings" === params.type) return Promise.resolve(null);
        if ("Programs" === params.type) return Promise.resolve(null);
        if ("nextup" === params.type) return Promise.resolve(null);
        var apiClient = connectionManager.getApiClient(params.serverId),
            itemId = params.genreId || params.gameGenreId || params.musicGenreId || params.studioId || params.personId || params.parentId;
        return itemId ? apiClient.getItem(apiClient.getCurrentUserId(), itemId) : Promise.resolve(null)
    }

    function showViewSettingsMenu() {
        var instance = this;
        require(["viewSettings"], function(ViewSettings) {
            (new ViewSettings).show({
                settingsKey: instance.getSettingsKey(),
                settings: instance.getViewSettings(),
                visibleSettings: instance.getVisibleViewSettings()
            }).then(function() {
                updateItemsContainerForViewType(instance), instance.itemsContainer.refreshItems()
            })
        })
    }

    function showFilterMenu() {
        var instance = this;
        require(["filterMenu"], function(FilterMenu) {
            (new FilterMenu).show({
                settingsKey: instance.getSettingsKey(),
                settings: instance.getFilters(),
                visibleSettings: instance.getVisibleFilters(),
                onChange: instance.itemsContainer.refreshItems.bind(instance.itemsContainer),
                parentId: instance.params.parentId,
                itemTypes: instance.getItemTypes(),
                serverId: instance.params.serverId,
                filterMenuOptions: instance.getFilterMenuOptions()
            }).then(function() {
                instance.itemsContainer.refreshItems()
            })
        })
    }

    function showSortMenu() {
        var instance = this;
        require(["sortMenu"], function(SortMenu) {
            (new SortMenu).show({
                settingsKey: instance.getSettingsKey(),
                settings: instance.getSortValues(),
                onChange: instance.itemsContainer.refreshItems.bind(instance.itemsContainer),
                serverId: instance.params.serverId,
                sortOptions: instance.getSortMenuOptions()
            }).then(function() {
                updateSortText(instance), updateAlphaPickerState(instance), instance.itemsContainer.refreshItems()
            })
        })
    }

    function onNewItemClick() {
        var instance = this;
        require(["playlistEditor"], function(playlistEditor) {
            (new playlistEditor).show({
                items: [],
                serverId: instance.params.serverId
            })
        })
    }

    function hideOrShowAll(elems, hide) {
        for (var i = 0, length = elems.length; i < length; i++) hide ? elems[i].classList.add("hide") : elems[i].classList.remove("hide")
    }

    function bindAll(elems, eventName, fn) {
        for (var i = 0, length = elems.length; i < length; i++) elems[i].addEventListener(eventName, fn)
    }

    function ItemsView(view, params) {
        function fetchData() {
            return getItems(self, params, self.currentItem).then(function(result) {
                return null == self.totalItemCount && (self.totalItemCount = result.Items ? result.Items.length : result.length), updateAlphaPickerState(self, self.totalItemCount), result
            })
        }

        function getItemsHtml(items) {
            var settings = self.getViewSettings();
            if ("list" === settings.imageType) return listView.getListViewHtml({
                items: items
            });
            var shape, preferThumb, preferDisc, preferLogo, defaultShape, item = self.currentItem,
                lines = settings.showTitle ? 2 : 0;
            "banner" === settings.imageType ? shape = "banner" : "disc" === settings.imageType ? (shape = "square", preferDisc = !0) : "logo" === settings.imageType ? (shape = "backdrop", preferLogo = !0) : "thumb" === settings.imageType ? (shape = "backdrop", preferThumb = !0) : "nextup" === params.type ? (shape = "backdrop", preferThumb = "thumb" === settings.imageType) : "Programs" === params.type || "Recordings" === params.type ? (shape = "true" === params.IsMovie ? "portrait" : "autoVertical", preferThumb = "true" !== params.IsMovie && "auto", defaultShape = "true" === params.IsMovie ? "portrait" : "backdrop") : shape = "autoVertical";
            var posterOptions = {
                shape: shape,
                showTitle: settings.showTitle,
                showYear: settings.showTitle,
                centerText: !0,
                coverImage: !0,
                preferThumb: preferThumb,
                preferDisc: preferDisc,
                preferLogo: preferLogo,
                overlayPlayButton: !1,
                overlayMoreButton: !0,
                overlayText: !settings.showTitle,
                defaultShape: defaultShape,
                action: "Audio" === params.type ? "playallfromhere" : null
            };
            if ("nextup" === params.type) posterOptions.showParentTitle = settings.showTitle;
            else if ("Person" === params.type) posterOptions.showYear = !1, posterOptions.showParentTitle = !1, lines = 1;
            else if ("Audio" === params.type) posterOptions.showParentTitle = settings.showTitle;
            else if ("MusicAlbum" === params.type) posterOptions.showParentTitle = settings.showTitle;
            else if ("Episode" === params.type) posterOptions.showParentTitle = settings.showTitle;
            else if ("MusicArtist" === params.type) posterOptions.showYear = !1, lines = 1;
            else if ("Programs" === params.type) {
                lines = settings.showTitle ? 1 : 0;
                var showParentTitle = settings.showTitle && "true" !== params.IsMovie;
                showParentTitle && lines++;
                var showAirTime = settings.showTitle && "Recordings" !== params.type;
                showAirTime && lines++;
                var showYear = settings.showTitle && "true" === params.IsMovie && "Recordings" === params.type;
                showYear && lines++, posterOptions = Object.assign(posterOptions, {
                    inheritThumb: "Recordings" === params.type,
                    context: "livetv",
                    showParentTitle: showParentTitle,
                    showAirTime: showAirTime,
                    showAirDateTime: showAirTime,
                    overlayPlayButton: !1,
                    overlayMoreButton: !0,
                    showYear: showYear,
                    coverImage: !0
                })
            } else posterOptions.showParentTitle = settings.showTitle;
            return posterOptions.lines = lines, posterOptions.items = items, item && "folders" === item.CollectionType && (posterOptions.context = "folders"), cardBuilder.getCardsHtml(posterOptions)
        }

        function initAlphaPicker() {
            self.scroller = view.querySelector(".scrollFrameY");
            var alphaPickerElement = self.alphaPickerElement;
            layoutManager.tv ? (alphaPickerElement.classList.add("alphaPicker-fixed-left"), alphaPickerElement.classList.add("focuscontainer-left"), self.itemsContainer.parentNode.classList.add("padded-left-withalphapicker")) : (alphaPickerElement.classList.add("alphaPicker-fixed-right"), alphaPickerElement.classList.add("focuscontainer-right"), self.itemsContainer.parentNode.classList.add("padded-right-withalphapicker")), self.alphaPicker = new alphaPicker({
                element: alphaPickerElement,
                itemsContainer: layoutManager.tv ? self.itemsContainer : null,
                itemClass: "card",
                valueChangeEvent: layoutManager.tv ? null : "click"
            }), self.alphaPicker.on("alphavaluechanged", onAlphaPickerValueChanged)
        }

        function onAlphaPickerValueChanged() {
            self.alphaPicker.value();
            self.itemsContainer.refreshItems()
        }

        function setTitle(item) {
            Emby.Page.setTitle(getTitle(item) || ""), item && "playlists" === item.CollectionType ? hideOrShowAll(view.querySelectorAll(".btnNewItem"), !1) : hideOrShowAll(view.querySelectorAll(".btnNewItem"), !0)
        }

        function getTitle(item) {
            return "Recordings" === params.type ? globalize.translate("Recordings") : "Programs" === params.type ? "true" === params.IsMovie ? globalize.translate("Movies") : "true" === params.IsSports ? globalize.translate("Sports") : "true" === params.IsKids ? globalize.translate("HeaderForKids") : "true" === params.IsAiring ? globalize.translate("HeaderOnNow") : "true" === params.IsSeries ? globalize.translate("Shows") : "true" === params.IsNews ? globalize.translate("News") : globalize.translate("Programs") : "nextup" === params.type ? globalize.translate("NextUp") : "favoritemovies" === params.type ? globalize.translate("FavoriteMovies") : item ? item.Name : "Movie" === params.type ? globalize.translate("sharedcomponents#Movies") : "Series" === params.type ? globalize.translate("sharedcomponents#Shows") : "Season" === params.type ? globalize.translate("sharedcomponents#Seasons") : "Episode" === params.type ? globalize.translate("sharedcomponents#Episodes") : "MusicArtist" === params.type ? globalize.translate("sharedcomponents#Artists") : "MusicAlbum" === params.type ? globalize.translate("sharedcomponents#Albums") : "Audio" === params.type ? globalize.translate("sharedcomponents#Songs") : "Game" === params.type ? globalize.translate("sharedcomponents#Games") : "Video" === params.type ? globalize.translate("sharedcomponents#Videos") : void 0
        }

        function play() {
            var currentItem = self.currentItem;
            if (currentItem && !self.hasFilters) return void playbackManager.play({
                items: [currentItem]
            });
            getItems(self, self.params, currentItem, null, null, 300).then(function(result) {
                playbackManager.play({
                    items: result.Items
                })
            })
        }

        function queue() {
            var currentItem = self.currentItem;
            if (currentItem && !self.hasFilters) return void playbackManager.queue({
                items: [currentItem]
            });
            getItems(self, self.params, currentItem, null, null, 300).then(function(result) {
                playbackManager.queue({
                    items: result.Items
                })
            })
        }

        function shuffle() {
            var currentItem = self.currentItem;
            if (currentItem && !self.hasFilters) return void playbackManager.shuffle(currentItem);
            getItems(self, self.params, currentItem, "Random", null, 300).then(function(result) {
                playbackManager.play({
                    items: result.Items
                })
            })
        }
        var self = this;
        self.params = params, this.itemsContainer = view.querySelector(".itemsContainer"), params.parentId ? this.itemsContainer.setAttribute("data-parentid", params.parentId) : "nextup" === params.type ? this.itemsContainer.setAttribute("data-monitor", "videoplayback") : "favoritemovies" === params.type ? this.itemsContainer.setAttribute("data-monitor", "markfavorite") : "Programs" === params.type && this.itemsContainer.setAttribute("data-refreshinterval", "300000");
        var i, length, btnViewSettings = view.querySelectorAll(".btnViewSettings");
        for (i = 0, length = btnViewSettings.length; i < length; i++) btnViewSettings[i].addEventListener("click", showViewSettingsMenu.bind(this));
        var filterButtons = view.querySelectorAll(".btnFilter");
        this.filterButtons = filterButtons;
        var hasVisibleFilters = this.getVisibleFilters().length;
        for (i = 0, length = filterButtons.length; i < length; i++) {
            var btnFilter = filterButtons[i];
            btnFilter.addEventListener("click", showFilterMenu.bind(this)), hasVisibleFilters ? btnFilter.classList.remove("hide") : btnFilter.classList.add("hide")
        }
        var sortButtons = view.querySelectorAll(".btnSort");
        for (this.sortButtons = sortButtons, i = 0, length = sortButtons.length; i < length; i++) {
            var sortButton = sortButtons[i];
            sortButton.addEventListener("click", showSortMenu.bind(this)), "nextup" !== params.type && sortButton.classList.remove("hide")
        }
        this.btnSortText = view.querySelector(".btnSortText"), this.btnSortIcon = view.querySelector(".btnSortIcon"), bindAll(view.querySelectorAll(".btnNewItem"), "click", onNewItemClick.bind(this)), this.alphaPickerElement = view.querySelector(".alphaPicker"), self.itemsContainer.fetchData = fetchData, self.itemsContainer.getItemsHtml = getItemsHtml, view.addEventListener("viewshow", function(e) {
            var isRestored = e.detail.isRestored;
            isRestored || (loading.show(), updateSortText(self), updateItemsContainerForViewType(self)), setTitle(null), getItem(params).then(function(item) {
                setTitle(item), self.currentItem = item;
                var refresh = !isRestored;
                self.itemsContainer.resume({
                    refresh: refresh
                }).then(function() {
                    loading.hide(), refresh && focusManager.autoFocus(self.itemsContainer)
                }), isRestored || item && "PhotoAlbum" !== item.Type && initAlphaPicker();
                var itemType = item ? item.Type : null;
                "MusicGenre" === itemType || "Programs" !== params.type && "Channel" !== itemType ? hideOrShowAll(view.querySelectorAll(".btnPlay"), !1) : hideOrShowAll(view.querySelectorAll(".btnPlay"), !0), "MusicGenre" === itemType || "Programs" !== params.type && "nextup" !== params.type && "Channel" !== itemType ? hideOrShowAll(view.querySelectorAll(".btnShuffle"), !1) : hideOrShowAll(view.querySelectorAll(".btnShuffle"), !0), item && playbackManager.canQueue(item) ? hideOrShowAll(view.querySelectorAll(".btnQueue"), !1) : hideOrShowAll(view.querySelectorAll(".btnQueue"), !0)
            }), isRestored || (bindAll(view.querySelectorAll(".btnPlay"), "click", play), bindAll(view.querySelectorAll(".btnQueue"), "click", queue), bindAll(view.querySelectorAll(".btnShuffle"), "click", shuffle)), this.alphaNumericShortcuts = new AlphaNumericShortcuts({
                itemsContainer: self.itemsContainer
            })
        }), view.addEventListener("viewhide", function(e) {
            var itemsContainer = self.itemsContainer;
            itemsContainer && itemsContainer.pause();
            var alphaNumericShortcuts = self.alphaNumericShortcuts;
            alphaNumericShortcuts && (alphaNumericShortcuts.destroy(), self.alphaNumericShortcuts = null)
        }), view.addEventListener("viewdestroy", function() {
            self.listController && self.listController.destroy(), self.alphaPicker && (self.alphaPicker.off("alphavaluechanged", onAlphaPickerValueChanged), self.alphaPicker.destroy()), self.currentItem = null, self.scroller = null, self.itemsContainer = null, self.filterButtons = null, self.sortButtons = null, self.btnSortText = null, self.btnSortIcon = null, self.alphaPickerElement = null
        })
    }
    return ItemsView.prototype.getFilters = function() {
        var basekey = this.getSettingsKey();
        return {
            IsPlayed: "true" === userSettings.getFilter(basekey + "-filter-IsPlayed"),
            IsUnplayed: "true" === userSettings.getFilter(basekey + "-filter-IsUnplayed"),
            IsFavorite: "true" === userSettings.getFilter(basekey + "-filter-IsFavorite"),
            IsResumable: "true" === userSettings.getFilter(basekey + "-filter-IsResumable"),
            Is4K: "true" === userSettings.getFilter(basekey + "-filter-Is4K"),
            IsHD: "true" === userSettings.getFilter(basekey + "-filter-IsHD"),
            IsSD: "true" === userSettings.getFilter(basekey + "-filter-IsSD"),
            Is3D: "true" === userSettings.getFilter(basekey + "-filter-Is3D"),
            VideoTypes: userSettings.getFilter(basekey + "-filter-VideoTypes"),
            SeriesStatus: userSettings.getFilter(basekey + "-filter-SeriesStatus"),
            HasSubtitles: userSettings.getFilter(basekey + "-filter-HasSubtitles"),
            HasTrailer: userSettings.getFilter(basekey + "-filter-HasTrailer"),
            HasSpecialFeature: userSettings.getFilter(basekey + "-filter-HasSpecialFeature"),
            HasThemeSong: userSettings.getFilter(basekey + "-filter-HasThemeSong"),
            HasThemeVideo: userSettings.getFilter(basekey + "-filter-HasThemeVideo"),
            GenreIds: userSettings.getFilter(basekey + "-filter-GenreIds")
        }
    }, ItemsView.prototype.getSortValues = function() {
        var basekey = this.getSettingsKey();
        return {
            sortBy: userSettings.getFilter(basekey + "-sortby") || this.getDefaultSortBy(),
            sortOrder: "Descending" === userSettings.getFilter(basekey + "-sortorder") ? "Descending" : "Ascending"
        }
    }, ItemsView.prototype.getDefaultSortBy = function() {
        var params = this.params,
            sortNameOption = this.getNameSortOption(params);
        return params.type ? sortNameOption.value : "IsFolder," + sortNameOption.value
    }, ItemsView.prototype.getSortMenuOptions = function() {
        var sortBy = [],
            params = this.params;
        "Programs" === params.type && sortBy.push({
            name: globalize.translate("sharedcomponents#AirDate"),
            value: "StartDate,SortName"
        });
        var option = this.getNameSortOption(params);
        return option && sortBy.push(option), option = this.getCommunityRatingSortOption(), option && sortBy.push(option), option = this.getCriticRatingSortOption(), option && sortBy.push(option), "Programs" !== params.type && sortBy.push({
            name: globalize.translate("sharedcomponents#DateAdded"),
            value: "DateCreated,SortName"
        }), option = this.getDatePlayedSortOption(), option && sortBy.push(option), params.type || (option = this.getNameSortOption(params), sortBy.push({
            name: globalize.translate("sharedcomponents#Folders"),
            value: "IsFolder," + option.value
        })), sortBy.push({
            name: globalize.translate("sharedcomponents#ParentalRating"),
            value: "OfficialRating,SortName"
        }), option = this.getPlayCountSortOption(), option && sortBy.push(option), sortBy.push({
            name: globalize.translate("sharedcomponents#ReleaseDate"),
            value: "ProductionYear,PremiereDate,SortName"
        }), sortBy.push({
            name: globalize.translate("sharedcomponents#Runtime"),
            value: "Runtime,SortName"
        }), sortBy
    }, ItemsView.prototype.getNameSortOption = function(params) {
        return "Episode" === params.type ? {
            name: globalize.translate("sharedcomponents#Name"),
            value: "SeriesName,SortName"
        } : {
            name: globalize.translate("sharedcomponents#Name"),
            value: "SortName"
        }
    }, ItemsView.prototype.getPlayCountSortOption = function() {
        return "Programs" === this.params.type ? null : {
            name: globalize.translate("sharedcomponents#PlayCount"),
            value: "PlayCount,SortName"
        }
    }, ItemsView.prototype.getDatePlayedSortOption = function() {
        return "Programs" === this.params.type ? null : {
            name: globalize.translate("sharedcomponents#DatePlayed"),
            value: "DatePlayed,SortName"
        }
    }, ItemsView.prototype.getCriticRatingSortOption = function() {
        return "Programs" === this.params.type ? null : {
            name: globalize.translate("sharedcomponents#CriticRating"),
            value: "CriticRating,SortName"
        }
    }, ItemsView.prototype.getCommunityRatingSortOption = function() {
        return {
            name: globalize.translate("sharedcomponents#CommunityRating"),
            value: "CommunityRating,SortName"
        }
    }, ItemsView.prototype.getVisibleFilters = function() {
        var filters = [],
            params = this.params;
        return "nextup" === params.type || ("Programs" === params.type ? filters.push("Genres") : (params.type, filters.push("IsUnplayed"), filters.push("IsPlayed"), params.IsFavorite || filters.push("IsFavorite"), filters.push("IsResumable"), filters.push("VideoType"), filters.push("HasSubtitles"), filters.push("HasTrailer"), filters.push("HasSpecialFeature"), filters.push("HasThemeSong"), filters.push("HasThemeVideo"))), filters
    }, ItemsView.prototype.setFilterStatus = function(hasFilters) {
        this.hasFilters = hasFilters;
        var filterButtons = this.filterButtons;
        if (filterButtons.length)
            for (var i = 0, length = filterButtons.length; i < length; i++) {
                var btnFilter = filterButtons[i],
                    bubble = btnFilter.querySelector(".filterButtonBubble");
                if (!bubble) {
                    if (!hasFilters) continue;
                    btnFilter.insertAdjacentHTML("afterbegin", '<div class="filterButtonBubble">!</div>'), btnFilter.classList.add("btnFilterWithBubble"), bubble = btnFilter.querySelector(".filterButtonBubble")
                }
                hasFilters ? bubble.classList.remove("hide") : bubble.classList.add("hide")
            }
    }, ItemsView.prototype.getFilterMenuOptions = function() {
        var params = this.params;
        return {
            IsAiring: params.IsAiring,
            IsMovie: params.IsMovie,
            IsSports: params.IsSports,
            IsKids: params.IsKids,
            IsNews: params.IsNews,
            IsSeries: params.IsSeries,
            Recursive: this.queryRecursive
        }
    }, ItemsView.prototype.getVisibleViewSettings = function() {
        var item = (this.params, this.currentItem),
            fields = ["showTitle"];
        return (!item || "PhotoAlbum" !== item.Type && "ChannelFolderItem" !== item.Type) && fields.push("imageType"), fields.push("viewType"), fields
    }, ItemsView.prototype.getViewSettings = function() {
        var basekey = this.getSettingsKey(),
            params = this.params,
            item = this.currentItem,
            showTitle = userSettings.get(basekey + "-showTitle");
        "true" === showTitle ? showTitle = !0 : "false" === showTitle ? showTitle = !1 : "Programs" === params.type || "Recordings" === params.type || "Person" === params.type || "nextup" === params.type || "Audio" === params.type || "MusicAlbum" === params.type || "MusicArtist" === params.type ? showTitle = !0 : item && "PhotoAlbum" !== item.Type && (showTitle = !0);
        var imageType = userSettings.get(basekey + "-imageType");
        return imageType || "nextup" === params.type && (imageType = "thumb"), {
            showTitle: showTitle,
            showYear: "false" !== userSettings.get(basekey + "-showYear"),
            imageType: imageType || "primary",
            viewType: userSettings.get(basekey + "-viewType") || "images"
        }
    }, ItemsView.prototype.getItemTypes = function() {
        var params = this.params;
        return "nextup" === params.type ? ["Episode"] : "Programs" === params.type ? ["Program"] : []
    }, ItemsView.prototype.getSettingsKey = function() {
        var values = [];
        values.push("items");
        var params = this.params;
        return params.type ? values.push(params.type) : params.parentId && values.push(params.parentId), params.IsAiring && values.push("IsAiring"), params.IsMovie && values.push("IsMovie"), params.IsKids && values.push("IsKids"), params.IsSports && values.push("IsSports"), params.IsNews && values.push("IsNews"), params.IsSeries && values.push("IsSeries"), params.IsFavorite && values.push("IsFavorite"), params.genreId && values.push("Genre"), params.gameGenreId && values.push("GameGenre"), params.musicGenreId && values.push("MusicGenre"), params.studioId && values.push("Studio"), params.personId && values.push("Person"), params.parentId && values.push("Folder"), values.join("-")
    }, ItemsView
});