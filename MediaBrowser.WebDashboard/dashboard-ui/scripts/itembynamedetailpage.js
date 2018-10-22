define(["connectionManager", "listView", "cardBuilder", "imageLoader", "libraryBrowser", "emby-itemscontainer", "emby-linkbutton"], function(connectionManager, listView, cardBuilder, imageLoader, libraryBrowser) {
    "use strict";

    function renderItems(page, item) {
        var sections = [];
        item.ArtistCount && sections.push({
            name: Globalize.translate("TabArtists"),
            type: "MusicArtist"
        }), item.ProgramCount && "Person" == item.Type && sections.push({
            name: Globalize.translate("HeaderUpcomingOnTV"),
            type: "Program"
        }), item.MovieCount && sections.push({
            name: Globalize.translate("TabMovies"),
            type: "Movie"
        }), item.SeriesCount && sections.push({
            name: Globalize.translate("TabShows"),
            type: "Series"
        }), item.EpisodeCount && sections.push({
            name: Globalize.translate("TabEpisodes"),
            type: "Episode"
        }), item.TrailerCount && sections.push({
            name: Globalize.translate("TabTrailers"),
            type: "Trailer"
        }), item.GameCount && sections.push({
            name: Globalize.translate("TabGames"),
            type: "Game"
        }), item.AlbumCount && sections.push({
            name: Globalize.translate("TabAlbums"),
            type: "MusicAlbum"
        }), item.MusicVideoCount && sections.push({
            name: Globalize.translate("TabMusicVideos"),
            type: "MusicVideo"
        });
        var elem = page.querySelector("#childrenContent");
        elem.innerHTML = sections.map(function(section) {
            var html = "",
                sectionClass = "verticalSection";
            return "Audio" === section.type && (sectionClass += " verticalSection-extrabottompadding"), html += '<div class="' + sectionClass + '" data-type="' + section.type + '">', html += '<div class="sectionTitleContainer sectionTitleContainer-cards">', html += '<h2 class="sectionTitle sectionTitle-cards padded-left">', html += section.name, html += "</h2>", html += '<a is="emby-linkbutton" href="#" class="clearLink hide" style="margin-left:1em;vertical-align:middle;"><button is="emby-button" type="button" class="raised more raised-mini noIcon">' + Globalize.translate("ButtonMore") + "</button></a>", html += "</div>", html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right">', html += "</div>", html += "</div>"
        }).join("");
        for (var sectionElems = elem.querySelectorAll(".verticalSection"), i = 0, length = sectionElems.length; i < length; i++) renderSection(page, item, sectionElems[i], sectionElems[i].getAttribute("data-type"))
    }

    function renderSection(page, item, element, type) {
        switch (type) {
            case "Program":
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Program",
                    PersonTypes: "",
                    ArtistIds: "",
                    AlbumArtistIds: "",
                    Limit: 10,
                    SortBy: "StartDate"
                }, {
                    shape: "backdrop",
                    showTitle: !0,
                    centerText: !0,
                    overlayMoreButton: !0,
                    preferThumb: !0,
                    overlayText: !1,
                    showAirTime: !0,
                    showAirDateTime: !0,
                    showChannelName: !0
                });
                break;
            case "Movie":
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Movie",
                    PersonTypes: "",
                    ArtistIds: "",
                    AlbumArtistIds: "",
                    Limit: 10,
                    SortBy: "SortName"
                }, {
                    shape: "portrait",
                    showTitle: !0,
                    centerText: !0,
                    overlayMoreButton: !0,
                    overlayText: !1,
                    showYear: !0
                });
                break;
            case "MusicVideo":
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "MusicVideo",
                    PersonTypes: "",
                    ArtistIds: "",
                    AlbumArtistIds: "",
                    Limit: 10,
                    SortBy: "SortName"
                }, {
                    shape: "portrait",
                    showTitle: !0,
                    centerText: !0,
                    overlayPlayButton: !0
                });
                break;
            case "Game":
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Game",
                    PersonTypes: "",
                    ArtistIds: "",
                    AlbumArtistIds: "",
                    Limit: 10,
                    SortBy: "SortName"
                }, {
                    shape: "portrait",
                    showTitle: !0,
                    centerText: !0,
                    overlayMoreButton: !0
                });
                break;
            case "Trailer":
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Trailer",
                    PersonTypes: "",
                    ArtistIds: "",
                    AlbumArtistIds: "",
                    Limit: 10,
                    SortBy: "SortName"
                }, {
                    shape: "portrait",
                    showTitle: !0,
                    centerText: !0,
                    overlayPlayButton: !0
                });
                break;
            case "Series":
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Series",
                    PersonTypes: "",
                    ArtistIds: "",
                    AlbumArtistIds: "",
                    Limit: 10,
                    SortBy: "SortName"
                }, {
                    shape: "portrait",
                    showTitle: !0,
                    centerText: !0,
                    overlayMoreButton: !0
                });
                break;
            case "MusicAlbum":
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "MusicAlbum",
                    PersonTypes: "",
                    ArtistIds: "",
                    AlbumArtistIds: ""
                }, {
                    shape: "square",
                    playFromHere: !0,
                    showTitle: !0,
                    showYear: !0,
                    coverImage: !0,
                    centerText: !0,
                    overlayPlayButton: !0
                });
                break;
            case "MusicArtist":
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "MusicArtist",
                    PersonTypes: "",
                    ArtistIds: "",
                    AlbumArtistIds: "",
                    Limit: 8,
                    SortBy: "SortName"
                }, {
                    shape: "square",
                    playFromHere: !0,
                    showTitle: !0,
                    showParentTitle: !0,
                    coverImage: !0,
                    centerText: !0,
                    overlayPlayButton: !0
                });
                break;
            case "Episode":
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Episode",
                    PersonTypes: "",
                    ArtistIds: "",
                    AlbumArtistIds: "",
                    Limit: 6,
                    SortBy: "SortName"
                }, {
                    shape: "backdrop",
                    showTitle: !0,
                    showParentTitle: !0,
                    centerText: !0,
                    overlayPlayButton: !0
                });
                break;
            case "Audio":
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Audio",
                    PersonTypes: "",
                    ArtistIds: "",
                    AlbumArtistIds: "",
                    SortBy: "AlbumArtist,Album,SortName"
                }, {
                    playFromHere: !0,
                    action: "playallfromhere",
                    smallIcon: !0,
                    artist: !0
                })
        }
    }

    function loadItems(element, item, type, query, listOptions) {
        query = getQuery(query, item), getItemsFunction(query, item)(query.StartIndex, query.Limit, query.Fields).then(function(result) {
            var html = "";
            if (query.Limit && result.TotalRecordCount > query.Limit) {
                var link = element.querySelector("a");
                link.classList.remove("hide"), link.setAttribute("href", getMoreItemsHref(item, type))
            } else element.querySelector("a").classList.add("hide");
            listOptions.items = result.Items;
            var itemsContainer = element.querySelector(".itemsContainer");
            "Audio" == type ? (html = listView.getListViewHtml(listOptions), itemsContainer.classList.remove("vertical-wrap"), itemsContainer.classList.add("vertical-list")) : (html = cardBuilder.getCardsHtml(listOptions), itemsContainer.classList.add("vertical-wrap"), itemsContainer.classList.remove("vertical-list")), itemsContainer.innerHTML = html, imageLoader.lazyChildren(itemsContainer)
        })
    }

    function getMoreItemsHref(item, type) {
        return "Genre" == item.Type ? "list/list.html?type=" + type + "&genreId=" + item.Id + "&serverId=" + item.ServerId : "MusicGenre" == item.Type ? "list/list.html?type=" + type + "&musicGenreId=" + item.Id + "&serverId=" + item.ServerId : "GameGenre" == item.Type ? "list/list.html?type=" + type + "&gameGenreId=" + item.Id + "&serverId=" + item.ServerId : "Studio" == item.Type ? "list/list.html?type=" + type + "&studioId=" + item.Id + "&serverId=" + item.ServerId : "MusicArtist" == item.Type ? "list/list.html?type=" + type + "&artistId=" + item.Id + "&serverId=" + item.ServerId : "Person" == item.Type ? "list/list.html?type=" + type + "&personId=" + item.Id + "&serverId=" + item.ServerId : "list/list.html?type=" + type + "&parentId=" + item.Id + "&serverId=" + item.ServerId
    }

    function addCurrentItemToQuery(query, item) {
        "Person" == item.Type ? query.PersonIds = item.Id : "Genre" == item.Type ? query.GenreIds = item.Id : "MusicGenre" == item.Type ? query.GenreIds = item.Id : "GameGenre" == item.Type ? query.GenreIds = item.Id : "Studio" == item.Type ? query.StudioIds = item.Id : "MusicArtist" == item.Type && (connectionManager.getApiClient(item.ServerId).isMinServerVersion("3.4.1.18") ? query.AlbumArtistIds = item.Id : query.ArtistIds = item.Id)
    }

    function getQuery(options, item) {
        var query = {
            SortOrder: "Ascending",
            IncludeItemTypes: "",
            Recursive: !0,
            Fields: "AudioInfo,SeriesInfo,ParentId,PrimaryImageAspectRatio,BasicSyncInfo",
            Limit: 100,
            StartIndex: 0,
            CollapseBoxSetItems: !1
        };
        return query = Object.assign(query, options || {}), addCurrentItemToQuery(query, item), query
    }

    function getItemsFunction(options, item) {
        var query = getQuery(options, item);
        return function(index, limit, fields) {
            query.StartIndex = index, query.Limit = limit, fields && (query.Fields += "," + fields);
            var apiClient = connectionManager.getApiClient(item.ServerId);
            return "MusicArtist" === query.IncludeItemTypes ? (query.IncludeItemTypes = null, apiClient.getAlbumArtists(apiClient.getCurrentUserId(), query)) : apiClient.getItems(apiClient.getCurrentUserId(), query)
        }
    }
    window.ItemsByName = {
        renderItems: renderItems
    }
});