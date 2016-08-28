define(['jQuery', 'apphost', 'scripts/taskbutton', 'cardStyle'], function ($, appHost, taskButton) {

    function changeCollectionType(page, virtualFolder) {

        require(['alert'], function (alert) {
            alert({
                title: Globalize.translate('HeaderChangeFolderType'),
                text: Globalize.translate('HeaderChangeFolderTypeHelp')
            });
        });
    }

    function addVirtualFolder(page) {

        require(['medialibrarycreator'], function (medialibrarycreator) {

            new medialibrarycreator().show({

                collectionTypeOptions: getCollectionTypeOptions().filter(function (f) {
                    return !f.hidden;
                }),
                refresh: shouldRefreshLibraryAfterChanges(page)

            }).then(function (hasChanges) {

                if (hasChanges) {
                    reloadLibrary(page);
                }
            });
        });
    }

    function editVirtualFolder(page, virtualFolder) {

        require(['medialibraryeditor'], function (medialibraryeditor) {

            new medialibraryeditor().show({

                refresh: shouldRefreshLibraryAfterChanges(page),
                library: virtualFolder

            }).then(function (hasChanges) {

                if (hasChanges) {
                    reloadLibrary(page);
                }
            });
        });
    }

    function deleteVirtualFolder(page, virtualFolder) {

        var msg = Globalize.translate('MessageAreYouSureYouWishToRemoveMediaFolder');

        if (virtualFolder.Locations.length) {
            msg += "<br/><br/>" + Globalize.translate("MessageTheFollowingLocationWillBeRemovedFromLibrary") + "<br/><br/>";
            msg += virtualFolder.Locations.join("<br/>");
        }

        require(['confirm'], function (confirm) {

            confirm(msg, Globalize.translate('HeaderRemoveMediaFolder')).then(function () {

                var refreshAfterChange = shouldRefreshLibraryAfterChanges(page);

                ApiClient.removeVirtualFolder(virtualFolder.Name, refreshAfterChange).then(function () {
                    reloadLibrary(page);
                });
            });
        });
    }

    function renameVirtualFolder(page, virtualFolder) {

        require(['prompt'], function (prompt) {

            prompt({
                label: Globalize.translate('LabelNewName')

            }).then(function (newName) {
                if (newName && newName != virtualFolder.Name) {

                    var refreshAfterChange = shouldRefreshLibraryAfterChanges(page);

                    ApiClient.renameVirtualFolder(virtualFolder.Name, newName, refreshAfterChange).then(function () {
                        reloadLibrary(page);
                    });
                }
            });

        });
    }

    function showCardMenu(page, elem, virtualFolders) {

        var card = $(elem).parents('.card')[0];
        var index = parseInt(card.getAttribute('data-index'));
        var virtualFolder = virtualFolders[index];

        var menuItems = [];

        menuItems.push({
            name: Globalize.translate('ButtonChangeContentType'),
            id: 'changetype',
            ironIcon: 'videocam'
        });

        menuItems.push({
            name: Globalize.translate('ButtonEditImages'),
            id: 'editimages',
            ironIcon: 'photo'
        });

        menuItems.push({
            name: Globalize.translate('ButtonManageFolders'),
            id: 'edit',
            ironIcon: 'folder_open'
        });

        menuItems.push({
            name: Globalize.translate('ButtonRemove'),
            id: 'delete',
            ironIcon: 'remove'
        });

        menuItems.push({
            name: Globalize.translate('ButtonRename'),
            id: 'rename',
            ironIcon: 'mode_edit'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: elem,
                callback: function (resultId) {

                    switch (resultId) {

                        case 'changetype':
                            changeCollectionType(page, virtualFolder);
                            break;
                        case 'edit':
                            editVirtualFolder(page, virtualFolder);
                            break;
                        case 'editimages':
                            editImages(page, virtualFolder);
                            break;
                        case 'rename':
                            renameVirtualFolder(page, virtualFolder);
                            break;
                        case 'delete':
                            deleteVirtualFolder(page, virtualFolder);
                            break;
                        default:
                            break;
                    }
                }
            });

        });
    }

    function reloadLibrary(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getVirtualFolders().then(function (result) {
            reloadVirtualFolders(page, result);
        });
    }

    function shouldRefreshLibraryAfterChanges(page) {

        return $(page).is('#mediaLibraryPage');
    }

    function reloadVirtualFolders(page, virtualFolders) {

        var html = '';

        virtualFolders.push({
            Name: Globalize.translate('ButtonAddMediaLibrary'),
            icon: 'add_circle',
            Locations: [],
            showType: false,
            showLocations: false,
            showMenu: false,
            showNameWithIcon: true
        });

        for (var i = 0, length = virtualFolders.length; i < length; i++) {

            var virtualFolder = virtualFolders[i];

            html += getVirtualFolderHtml(page, virtualFolder, i);
        }

        var divVirtualFolders = page.querySelector('#divVirtualFolders');
        divVirtualFolders.innerHTML = html;
        divVirtualFolders.classList.add('itemsContainer');
        divVirtualFolders.classList.add('vertical-wrap');

        $('.btnCardMenu', divVirtualFolders).on('click', function () {
            showCardMenu(page, this, virtualFolders);
        });

        $('.addLibrary', divVirtualFolders).on('click', function () {
            addVirtualFolder(page);
        });

        $('.editLibrary', divVirtualFolders).on('click', function () {
            var card = $(this).parents('.card')[0];
            var index = parseInt(card.getAttribute('data-index'));
            var virtualFolder = virtualFolders[index];

            if (!virtualFolder.ItemId) {
                return;
            }

            editVirtualFolder(page, virtualFolder);
        });

        Dashboard.hideLoadingMsg();
    }

    function editImages(page, virtualFolder) {

        require(['components/imageeditor/imageeditor'], function (ImageEditor) {

            ImageEditor.show(virtualFolder.ItemId, {
                theme: 'a'
            }).then(function () {
                reloadLibrary(page);
            });
        });
    }

    function getCollectionTypeOptions() {

        return [

            { name: "", value: "" },
            { name: Globalize.translate('FolderTypeMovies'), value: "movies" },
            { name: Globalize.translate('FolderTypeMusic'), value: "music" },
            { name: Globalize.translate('FolderTypeTvShows'), value: "tvshows" },
            { name: Globalize.translate('FolderTypeBooks'), value: "books", message: Globalize.translate('MessageBookPluginRequired') },
            { name: Globalize.translate('FolderTypeGames'), value: "games", message: Globalize.translate('MessageGamePluginRequired') },
            { name: Globalize.translate('OptionHomeVideos'), value: "homevideos" },
            { name: Globalize.translate('FolderTypeMusicVideos'), value: "musicvideos" },
            { name: Globalize.translate('FolderTypePhotos'), value: "photos" },
            { name: Globalize.translate('FolderTypeUnset'), value: "mixed", message: Globalize.translate('MessageUnsetContentHelp') }
        ];

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
                return "live_tv";
            case "tvshows":
                return "live_tv";
            case "games":
                return "folder";
            case "trailers":
                return "local_movies";
            case "homevideos":
                return "video_library";
            case "musicvideos":
                return "video_library";
            case "books":
                return "folder";
            case "channels":
                return "folder";
            case "playlists":
                return "folder";
            default:
                return "folder";
        }
    }

    function getVirtualFolderHtml(page, virtualFolder, index) {

        var html = '';

        var style = "";

        if (page.classList.contains('wizardPage')) {
            style += "min-width:33.3%;";
        }

        html += '<div class="card backdropCard scalableCard backdropCard-scalable" style="' + style + '" data-index="' + index + '">';

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable visualCardBox-cardScalable">';

        html += '<div class="cardPadder cardPadder-backdrop"></div>';

        html += '<div class="cardContent">';
        var imgUrl = '';

        if (virtualFolder.PrimaryImageItemId) {
            imgUrl = ApiClient.getScaledImageUrl(virtualFolder.PrimaryImageItemId, {
                type: 'Primary'
            });
        }

        if (imgUrl) {
            html += '<div class="cardImageContainer editLibrary" style="cursor:pointer;background-image:url(\'' + imgUrl + '\');"></div>';
        } else if (!virtualFolder.showNameWithIcon) {
            html += '<div class="cardImageContainer editLibrary" style="cursor:pointer;">';
            html += '<i class="cardImageIcon md-icon" style="color:#444;">' + (virtualFolder.icon || getIcon(virtualFolder.CollectionType)) + '</i>';

            html += '</div>';
        }

        if (!imgUrl && virtualFolder.showNameWithIcon) {
            html += '<h1 class="cardImageContainer addLibrary" style="position:absolute;top:0;left:0;right:0;bottom:0;cursor:pointer;flex-direction:column;">';

            html += '<i class="cardImageIcon md-icon" style="font-size:300%;color:#888;height:auto;width:auto;">' + (virtualFolder.icon || getIcon(virtualFolder.CollectionType)) + '</i>';

            if (virtualFolder.showNameWithIcon) {
                html += '<div style="margin:1em 0;position:width:100%;font-weight:500;color:#444;">';
                html += virtualFolder.Name;
                html += "</div>";
            }

            html += '</h1>';
        }

        // cardContent
        html += "</div>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter visualCardBox-cardFooter">';

        if (virtualFolder.showMenu !== false) {
            var moreIcon = appHost.moreIcon == 'dots-horiz' ? '&#xE5D3;' : '&#xE5D4;';

            html += '<div style="text-align:right; float:right;padding-top:5px;">';
            html += '<button type="button" is="paper-icon-button-light" class="btnCardMenu autoSize"><i class="md-icon">' + moreIcon + '</i></button>';
            html += "</div>";
        }

        html += "<div class='cardText'>";
        if (virtualFolder.showNameWithIcon) {
            html += '&nbsp;';
        } else {
            html += virtualFolder.Name;
        }
        html += "</div>";

        var typeName = getCollectionTypeOptions().filter(function (t) {

            return t.value == virtualFolder.CollectionType;

        })[0];

        typeName = typeName ? typeName.name : Globalize.translate('FolderTypeUnset');

        html += "<div class='cardText'>";
        if (virtualFolder.showType === false) {
            html += '&nbsp;';
        } else {
            html += typeName;
        }
        html += "</div>";

        if (virtualFolder.showLocations === false) {
            html += "<div class='cardText'>";
            html += '&nbsp;';
            html += "</div>";
        } else if (!virtualFolder.Locations.length) {
            html += "<div class='cardText' style='color:#cc3333;'>";
            html += Globalize.translate('NumLocationsValue', virtualFolder.Locations.length);
            html += "</div>";
        }
        else if (virtualFolder.Locations.length == 1) {
            html += "<div class='cardText'>";
            html += virtualFolder.Locations[0];
            html += "</div>";
        }
        else {
            html += "<div class='cardText'>";
            html += Globalize.translate('NumLocationsValue', virtualFolder.Locations.length);
            html += "</div>";
        }

        // cardFooter
        html += "</div>";

        // cardBox
        html += "</div>";

        // card
        html += "</div>";

        return html;
    }

    window.WizardLibraryPage = {

        next: function () {

            Dashboard.showLoadingMsg();

            var apiClient = ApiClient;

            apiClient.ajax({
                type: "POST",
                url: apiClient.getUrl('System/Configuration/MetadataPlugins/Autoset')

            }).then(function () {

                Dashboard.hideLoadingMsg();
                Dashboard.navigate('wizardsettings.html');
            });
        }
    };

    function getTabs() {
        return [
        {
            href: 'library.html',
            name: Globalize.translate('TabFolders')
        },
         {
             href: 'librarydisplay.html',
             name: Globalize.translate('TabDisplay')
         },
         {
             href: 'librarypathmapping.html',
             name: Globalize.translate('TabPathSubstitution')
         },
         {
             href: 'librarysettings.html',
             name: Globalize.translate('TabAdvanced')
         }];
    }

    pageClassOn('pageshow', "mediaLibraryPage", function () {

        var page = this;
        reloadLibrary(page);

    });

    pageIdOn('pageshow', "mediaLibraryPage", function () {

        LibraryMenu.setTabs('librarysetup', 0, getTabs);
        var page = this;

        // on here
        taskButton({
            mode: 'on',
            progressElem: page.querySelector('.refreshProgress'),
            taskKey: 'RefreshLibrary',
            button: page.querySelector('.btnRefresh')
        });

    });

    pageIdOn('pagebeforehide', "mediaLibraryPage", function () {

        var page = this;

        // off here
        taskButton({
            mode: 'off',
            progressElem: page.querySelector('.refreshProgress'),
            taskKey: 'RefreshLibrary',
            button: page.querySelector('.btnRefresh')
        });

    });

});