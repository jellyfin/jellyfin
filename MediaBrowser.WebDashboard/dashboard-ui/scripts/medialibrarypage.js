define(["jQuery", "apphost", "scripts/taskbutton", "loading", "libraryMenu", "globalize", "dom", "indicators", "cardStyle", "emby-itemrefreshindicator"], function($, appHost, taskButton, loading, libraryMenu, globalize, dom, indicators) {
    "use strict";

    function changeCollectionType(page, virtualFolder) {
        require(["alert"], function(alert) {
            alert({
                title: globalize.translate("HeaderChangeFolderType"),
                text: globalize.translate("HeaderChangeFolderTypeHelp")
            })
        })
    }

    function addVirtualFolder(page) {
        require(["medialibrarycreator"], function(medialibrarycreator) {
            (new medialibrarycreator).show({
                collectionTypeOptions: getCollectionTypeOptions().filter(function(f) {
                    return !f.hidden
                }),
                refresh: shouldRefreshLibraryAfterChanges(page)
            }).then(function(hasChanges) {
                hasChanges && reloadLibrary(page)
            })
        })
    }

    function editVirtualFolder(page, virtualFolder) {
        require(["medialibraryeditor"], function(medialibraryeditor) {
            (new medialibraryeditor).show({
                refresh: shouldRefreshLibraryAfterChanges(page),
                library: virtualFolder
            }).then(function(hasChanges) {
                hasChanges && reloadLibrary(page)
            })
        })
    }

    function deleteVirtualFolder(page, virtualFolder) {
        var msg = globalize.translate("MessageAreYouSureYouWishToRemoveMediaFolder");
        virtualFolder.Locations.length && (msg += "<br/><br/>" + globalize.translate("MessageTheFollowingLocationWillBeRemovedFromLibrary") + "<br/><br/>", msg += virtualFolder.Locations.join("<br/>")), require(["confirm"], function(confirm) {
            confirm(msg, globalize.translate("HeaderRemoveMediaFolder")).then(function() {
                var refreshAfterChange = shouldRefreshLibraryAfterChanges(page);
                ApiClient.removeVirtualFolder(virtualFolder.Name, refreshAfterChange).then(function() {
                    reloadLibrary(page)
                })
            })
        })
    }

    function refreshVirtualFolder(page, virtualFolder) {
        require(["refreshDialog"], function(refreshDialog) {
            new refreshDialog({
                itemIds: [virtualFolder.ItemId],
                serverId: ApiClient.serverId(),
                mode: "scan"
            }).show()
        })
    }

    function renameVirtualFolder(page, virtualFolder) {
        require(["prompt"], function(prompt) {
            prompt({
                label: globalize.translate("LabelNewName"),
                confirmText: globalize.translate("ButtonRename")
            }).then(function(newName) {
                if (newName && newName != virtualFolder.Name) {
                    var refreshAfterChange = shouldRefreshLibraryAfterChanges(page);
                    ApiClient.renameVirtualFolder(virtualFolder.Name, newName, refreshAfterChange).then(function() {
                        reloadLibrary(page)
                    })
                }
            })
        })
    }

    function showCardMenu(page, elem, virtualFolders) {
        var card = dom.parentWithClass(elem, "card"),
            index = parseInt(card.getAttribute("data-index")),
            virtualFolder = virtualFolders[index],
            menuItems = [];
        menuItems.push({
            name: globalize.translate("ButtonChangeContentType"),
            id: "changetype",
            ironIcon: "videocam"
        }), menuItems.push({
            name: globalize.translate("ButtonEditImages"),
            id: "editimages",
            ironIcon: "photo"
        }), menuItems.push({
            name: globalize.translate("ManageLibrary"),
            id: "edit",
            ironIcon: "folder_open"
        }), menuItems.push({
            name: globalize.translate("ButtonRemove"),
            id: "delete",
            ironIcon: "remove"
        }), menuItems.push({
            name: globalize.translate("ButtonRename"),
            id: "rename",
            ironIcon: "mode_edit"
        }), menuItems.push({
            name: globalize.translate("ScanLibrary"),
            id: "refresh",
            ironIcon: "refresh"
        }), require(["actionsheet"], function(actionsheet) {
            actionsheet.show({
                items: menuItems,
                positionTo: elem,
                callback: function(resultId) {
                    switch (resultId) {
                        case "changetype":
                            changeCollectionType(page, virtualFolder);
                            break;
                        case "edit":
                            editVirtualFolder(page, virtualFolder);
                            break;
                        case "editimages":
                            editImages(page, virtualFolder);
                            break;
                        case "rename":
                            renameVirtualFolder(page, virtualFolder);
                            break;
                        case "delete":
                            deleteVirtualFolder(page, virtualFolder);
                            break;
                        case "refresh":
                            refreshVirtualFolder(page, virtualFolder)
                    }
                }
            })
        })
    }

    function reloadLibrary(page) {
        loading.show(), ApiClient.getVirtualFolders().then(function(result) {
            reloadVirtualFolders(page, result)
        })
    }

    function shouldRefreshLibraryAfterChanges(page) {
        return "mediaLibraryPage" === page.id
    }

    function reloadVirtualFolders(page, virtualFolders) {
        var html = "";
        virtualFolders.push({
            Name: globalize.translate("ButtonAddMediaLibrary"),
            icon: "add_circle",
            Locations: [],
            showType: !1,
            showLocations: !1,
            showMenu: !1,
            showNameWithIcon: !0
        });
        for (var i = 0, length = virtualFolders.length; i < length; i++) {
            var virtualFolder = virtualFolders[i];
            html += getVirtualFolderHtml(page, virtualFolder, i)
        }
        var divVirtualFolders = page.querySelector("#divVirtualFolders");
        divVirtualFolders.innerHTML = html, divVirtualFolders.classList.add("itemsContainer"), divVirtualFolders.classList.add("vertical-wrap"), $(".btnCardMenu", divVirtualFolders).on("click", function() {
            showCardMenu(page, this, virtualFolders)
        }), divVirtualFolders.querySelector(".addLibrary").addEventListener("click", function() {
            addVirtualFolder(page)
        }), $(".editLibrary", divVirtualFolders).on("click", function() {
            var card = $(this).parents(".card")[0],
                index = parseInt(card.getAttribute("data-index")),
                virtualFolder = virtualFolders[index];
            virtualFolder.ItemId && editVirtualFolder(page, virtualFolder)
        }), loading.hide()
    }

    function editImages(page, virtualFolder) {
        require(["imageEditor"], function(imageEditor) {
            imageEditor.show({
                itemId: virtualFolder.ItemId,
                serverId: ApiClient.serverId()
            }).then(function() {
                reloadLibrary(page)
            })
        })
    }

    function getLink(text, url) {
        return globalize.translate(text, '<a is="emby-linkbutton" class="button-link" href="' + url + '" target="_blank" data-autohide="true">', "</a>")
    }

    function getCollectionTypeOptions() {
        return [{
            name: "",
            value: ""
        }, {
            name: globalize.translate("FolderTypeMovies"),
            value: "movies",
            message: getLink("MovieLibraryHelp", "https://web.archive.org/web/20181216120305/https://github.com/MediaBrowser/Wiki/wiki/Movie-naming")
        }, {
            name: globalize.translate("FolderTypeMusic"),
            value: "music"
        }, {
            name: globalize.translate("FolderTypeTvShows"),
            value: "tvshows",
            message: getLink("TvLibraryHelp", "https://web.archive.org/web/20181216120305/https://github.com/MediaBrowser/Wiki/wiki/TV-naming")
        }, {
            name: globalize.translate("FolderTypeBooks"),
            value: "books",
            message: getLink("BookLibraryHelp", "https://web.archive.org/web/20181216120305/https://github.com/MediaBrowser/Wiki/wiki/Book-naming")
        }, {
            name: globalize.translate("FolderTypeGames"),
            value: "games",
            message: globalize.translate("MessageGamePluginRequired")
        }, {
            name: globalize.translate("OptionHomeVideos"),
            value: "homevideos"
        }, {
            name: globalize.translate("FolderTypeMusicVideos"),
            value: "musicvideos"
        }, {
            name: globalize.translate("FolderTypeUnset"),
            value: "mixed",
            message: globalize.translate("MessageUnsetContentHelp")
        }]
    }

    function getIcon(type) {
        switch (type) {
            case "movies":
                return "local_movies";
            case "music":
                return "library_music";
            case "photos":
                return "photo";
            case "livetv":
            case "tvshows":
                return "live_tv";
            case "games":
                return "folder";
            case "trailers":
                return "local_movies";
            case "homevideos":
            case "musicvideos":
                return "video_library";
            case "books":
            case "channels":
            case "playlists":
            default:
                return "folder"
        }
    }

    function getVirtualFolderHtml(page, virtualFolder, index) {
        var html = "",
            style = "";
        page.classList.contains("wizardPage") && (style += "min-width:33.3%;"), html += '<div class="card backdropCard scalableCard backdropCard-scalable" style="' + style + '" data-index="' + index + '" data-id="' + virtualFolder.ItemId + '">', html += '<div class="cardBox visualCardBox">', html += '<div class="cardScalable visualCardBox-cardScalable">', html += '<div class="cardPadder cardPadder-backdrop"></div>', html += '<div class="cardContent">';
        var imgUrl = "";
        virtualFolder.PrimaryImageItemId && (imgUrl = ApiClient.getScaledImageUrl(virtualFolder.PrimaryImageItemId, {
            type: "Primary"
        }));
        var hasCardImageContainer;
        if (imgUrl ? (html += '<div class="cardImageContainer editLibrary" style="cursor:pointer;background-image:url(\'' + imgUrl + "');\">", hasCardImageContainer = !0) : virtualFolder.showNameWithIcon || (html += '<div class="cardImageContainer editLibrary" style="cursor:pointer;">', html += '<i class="cardImageIcon-small md-icon">' + (virtualFolder.icon || getIcon(virtualFolder.CollectionType)) + "</i>", hasCardImageContainer = !0), hasCardImageContainer) {
            html += '<div class="cardIndicators backdropCardIndicators">';
            html += '<div is="emby-itemrefreshindicator"' + (virtualFolder.RefreshProgress || virtualFolder.RefreshStatus && "Idle" !== virtualFolder.RefreshStatus ? "" : ' class="hide"') + ' data-progress="' + (virtualFolder.RefreshProgress || 0) + '" data-status="' + virtualFolder.RefreshStatus + '"></div>', html += "</div>", html += "</div>"
        }
        if (!imgUrl && virtualFolder.showNameWithIcon && (html += '<h3 class="cardImageContainer addLibrary" style="position:absolute;top:0;left:0;right:0;bottom:0;cursor:pointer;flex-direction:column;">', html += '<i class="cardImageIcon-small md-icon">' + (virtualFolder.icon || getIcon(virtualFolder.CollectionType)) + "</i>", virtualFolder.showNameWithIcon && (html += '<div style="margin:1em 0;position:width:100%;">', html += virtualFolder.Name, html += "</div>"), html += "</h3>"), html += "</div>", html += "</div>", html += '<div class="cardFooter visualCardBox-cardFooter">', !1 !== virtualFolder.showMenu) {
            html += '<div style="text-align:right; float:right;padding-top:5px;">', html += '<button type="button" is="paper-icon-button-light" class="btnCardMenu autoSize"><i class="md-icon">&#xE5D3;</i></button>', html += "</div>"
        }
        html += "<div class='cardText'>", virtualFolder.showNameWithIcon ? html += "&nbsp;" : html += virtualFolder.Name, html += "</div>";
        var typeName = getCollectionTypeOptions().filter(function(t) {
            return t.value == virtualFolder.CollectionType
        })[0];
        return typeName = typeName ? typeName.name : globalize.translate("FolderTypeUnset"), html += "<div class='cardText cardText-secondary'>", !1 === virtualFolder.showType ? html += "&nbsp;" : html += typeName, html += "</div>", !1 === virtualFolder.showLocations ? (html += "<div class='cardText cardText-secondary'>", html += "&nbsp;", html += "</div>") : virtualFolder.Locations.length && 1 == virtualFolder.Locations.length ? (html += "<div class='cardText cardText-secondary'>", html += virtualFolder.Locations[0], html += "</div>") : (html += "<div class='cardText cardText-secondary'>", html += globalize.translate("NumLocationsValue", virtualFolder.Locations.length), html += "</div>"), html += "</div>", html += "</div>", html += "</div>"
    }

    function getTabs() {
        return [{
            href: "library.html",
            name: globalize.translate("HeaderLibraries")
        }, {
            href: "librarydisplay.html",
            name: globalize.translate("TabDisplay")
        }, {
            href: "metadataimages.html",
            name: globalize.translate("TabMetadata")
        }, {
            href: "metadatanfo.html",
            name: globalize.translate("TabNfoSettings")
        }, {
            href: "librarysettings.html",
            name: globalize.translate("TabAdvanced")
        }]
    }
    window.WizardLibraryPage = {
        next: function() {
            Dashboard.navigate("wizardsettings.html")
        }
    }, pageClassOn("pageshow", "mediaLibraryPage", function() {
        reloadLibrary(this)
    }), pageIdOn("pageshow", "mediaLibraryPage", function() {
        libraryMenu.setTabs("librarysetup", 0, getTabs);
        var page = this;
        taskButton({
            mode: "on",
            progressElem: page.querySelector(".refreshProgress"),
            taskKey: "RefreshLibrary",
            button: page.querySelector(".btnRefresh")
        })
    }), pageIdOn("pagebeforehide", "mediaLibraryPage", function() {
        var page = this;
        taskButton({
            mode: "off",
            progressElem: page.querySelector(".refreshProgress"),
            taskKey: "RefreshLibrary",
            button: page.querySelector(".btnRefresh")
        })
    })
});