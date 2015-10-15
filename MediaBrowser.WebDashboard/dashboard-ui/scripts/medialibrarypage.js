var MediaLibraryPage = {

    addMediaLocation: function (virtualFolderIndex) {

        MediaLibraryPage.selectDirectory(function (path) {

            if (path) {

                var virtualFolder = MediaLibraryPage.virtualFolders[virtualFolderIndex];

                MediaLibraryPage.lastVirtualFolderName = virtualFolder.Name;

                var refreshAfterChange = shouldRefreshLibraryAfterChanges();

                ApiClient.addMediaPath(virtualFolder.Name, path, refreshAfterChange).done(MediaLibraryPage.processOperationResult);
            }

        });
    },

    selectDirectory: function (callback) {

        require(['directorybrowser'], function (directoryBrowser) {

            var picker = new directoryBrowser();

            picker.show({

                callback: callback
            });

            MediaLibraryPage.directoryPicker = picker;
        });
    },

    deleteMediaLocation: function (button) {

        var folderIndex = button.getAttribute('data-folderindex');
        var index = parseInt(button.getAttribute('data-index'));

        var virtualFolder = MediaLibraryPage.virtualFolders[folderIndex];

        MediaLibraryPage.lastVirtualFolderName = virtualFolder.Name;

        var location = virtualFolder.Locations[index];

        Dashboard.confirm(Globalize.translate('MessageConfirmRemoveMediaLocation'), Globalize.translate('HeaderRemoveMediaLocation'), function (confirmResult) {

            if (confirmResult) {

                var refreshAfterChange = shouldRefreshLibraryAfterChanges();

                ApiClient.removeMediaPath(virtualFolder.Name, location, refreshAfterChange).done(MediaLibraryPage.processOperationResult);
            }
        });
    }

};

(function () {

    function changeCollectionType(page, virtualFolder) {

        Dashboard.alert({
            message: Globalize.translate('HeaderChangeFolderTypeHelp'),
            title: Globalize.translate('HeaderChangeFolderType')
        });
    }

    function processOperationResult(result) {

        var page = $($.mobile.activePage)[0];

        reloadLibrary(page);
    }

    function getTextValue(header, label, initialValue, showCollectionType, callback) {

        var page = $.mobile.activePage;

        var popup = $('#popupEnterText', page);

        $('h3', popup).html(header);
        $('#lblValue', popup).html(label);
        $('#txtValue', popup).val(initialValue);

        if (showCollectionType) {
            $('#fldCollectionType', popup).show();
            $('#selectCollectionType', popup).attr('required', 'required');
        } else {
            $('#fldCollectionType', popup).hide();
            $('#selectCollectionType', popup).removeAttr('required');
        }

        $('#selectCollectionType', popup).html(getCollectionTypeOptionsHtml()).val('');

        popup.on("popupafterclose", function () {
            $(this).off("popupafterclose").off("click");
            $('#textEntryForm', this).off("submit");
        }).popup("open");

        $('#textEntryForm', popup).on('submit', function () {

            if (callback) {

                if (showCollectionType) {

                    var collectionType = $('#selectCollectionType', popup).val();

                    // The server expects an empty value for mixed
                    if (collectionType == 'mixed') {
                        collectionType = '';
                    }

                    callback($('#txtValue', popup).val(), collectionType);
                } else {
                    callback($('#txtValue', popup).val());
                }

            }

            return false;
        });
    }

    function addVirtualFolder(page) {

        getTextValue(Globalize.translate('HeaderAddMediaFolder'), Globalize.translate('LabelName'), "", true, function (name, type) {

            var refreshAfterChange = shouldRefreshLibraryAfterChanges();

            ApiClient.addVirtualFolder(name, type, refreshAfterChange).done(processOperationResult);

        });
    }

    function deleteVirtualFolder(page, virtualFolder) {

        var msg = Globalize.translate('MessageAreYouSureYouWishToRemoveMediaFolder');

        if (virtualFolder.Locations.length) {
            msg += "<br/><br/>" + Globalize.translate("MessageTheFollowingLocationWillBeRemovedFromLibrary") + "<br/><br/>";
            msg += virtualFolder.Locations.join("<br/>");
        }

        Dashboard.confirm(msg, Globalize.translate('HeaderRemoveMediaFolder'), function (confirmResult) {

            if (confirmResult) {

                var refreshAfterChange = shouldRefreshLibraryAfterChanges();

                ApiClient.removeVirtualFolder(virtualFolder.Name, refreshAfterChange).done(processOperationResult);
            }

        });
    }

    function renameVirtualFolder(page, virtualFolder) {

        require(['prompt'], function (prompt) {

            prompt({
                text: Globalize.translate('LabelNewName'),
                title: Globalize.translate('HeaderRenameMediaFolder'),
                callback: function (newName) {

                    if (newName && newName != virtualFolder.Name) {

                        var refreshAfterChange = shouldRefreshLibraryAfterChanges();

                        ApiClient.renameVirtualFolder(virtualFolder.Name, newName, refreshAfterChange).done(processOperationResult);
                    }
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
            name: Globalize.translate('ButtonChangeType'),
            id: 'changetype',
            ironIcon: 'folder'
        });

        menuItems.push({
            name: Globalize.translate('ButtonRemove'),
            id: 'delete',
            ironIcon: 'remove'
        });

        menuItems.push({
            name: Globalize.translate('ButtonRename'),
            id: 'rename',
            ironIcon: 'mode-edit'
        });

        require(['actionsheet'], function () {

            ActionSheetElement.show({
                items: menuItems,
                positionTo: elem,
                callback: function (resultId) {

                    switch (resultId) {

                        case 'changetype':
                            changeCollectionType(page, virtualFolder);
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

        ApiClient.getVirtualFolders().done(function (result) {
            reloadVirtualFolders(page, result);
        });
    }

    function shouldRefreshLibraryAfterChanges() {

        return $($.mobile.activePage).is('#mediaLibraryPage');
    }

    function reloadVirtualFolders(page, virtualFolders) {

        var html = '';

        virtualFolders.push({
            Name: 'Add Media Library',
            icon: 'add-circle',
            Locations: [],
            showType: false,
            showLocations: false,
            showMenu: false,
            showNameWithIcon: true,
            color: 'green',
            contentClass: 'addLibrary'
        });

        for (var i = 0, length = virtualFolders.length; i < length; i++) {

            var virtualFolder = virtualFolders[i];

            html += getVirtualFolderHtml(virtualFolder, i);
        }

        var divVirtualFolders = page.querySelector('#divVirtualFolders');
        divVirtualFolders.innerHTML = html;

        $('.btnCardMenu', divVirtualFolders).on('click', function () {
            showCardMenu(page, this, virtualFolders);
        });

        $('.addLibrary', divVirtualFolders).on('click', function () {
            addVirtualFolder(page);
        });

        Dashboard.hideLoadingMsg();
    }

    function getCollectionTypeOptionsHtml() {

        return getCollectionTypeOptions().filter(function (i) {

            return i.isSelectable !== false;

        }).map(function (i) {

            return '<option value="' + i.value + '">' + i.name + '</option>';

        }).join("");
    }

    function getCollectionTypeOptions() {

        return [

            { name: "", value: "" },
            { name: Globalize.translate('FolderTypeMovies'), value: "movies" },
            { name: Globalize.translate('FolderTypeMusic'), value: "music" },
            { name: Globalize.translate('FolderTypeTvShows'), value: "tvshows" },
            { name: Globalize.translate('FolderTypeBooks'), value: "books", message: Globalize.translate('MessageBookPluginRequired') },
            { name: Globalize.translate('FolderTypeGames'), value: "games", message: Globalize.translate('MessageGamePluginRequired') },
            { name: Globalize.translate('FolderTypeHomeVideos'), value: "homevideos" },
            { name: Globalize.translate('FolderTypeMusicVideos'), value: "musicvideos" },
            { name: Globalize.translate('FolderTypePhotos'), value: "photos" },
            { name: Globalize.translate('FolderTypeUnset'), value: "mixed", message: Globalize.translate('MessageUnsetContentHelp') }
        ];

    }

    function getIcon(type) {

        switch (type) {
            case "movies":
                return "local-movies";
            case "music":
                return "library-music";
            case "photos":
                return "photo";
            case "livetv":
                return "live-tv";
            case "tvshows":
                return "live-tv";
            case "games":
                return "folder";
            case "trailers":
                return "local-movies";
            case "homevideos":
                return "video-library";
            case "musicvideos":
                return "video-library";
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

    function getVirtualFolderHtml(virtualFolder, index) {

        var html = '';

        html += '<div class="card backdropCard" style="max-width:300px;" data-index="' + index + '">';

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        var contentClass = "cardContent";
        if (virtualFolder.contentClass) {
            contentClass += " " + virtualFolder.contentClass;
        }

        html += '<div class="' + contentClass + '">';
        var imgUrl = '';
        if (imgUrl) {
            html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');"></div>';
        } else {
            html += '<div class="cardImage iconCardImage">';

            if (virtualFolder.color) {
                html += '<div style="color:' + virtualFolder.color + ';cursor:pointer;">';
            } else {
                html += '<div>';
            }
            html += '<iron-icon icon="' + (virtualFolder.icon || getIcon(virtualFolder.CollectionType)) + '"></iron-icon>';

            if (virtualFolder.showNameWithIcon) {
                html += '<div style="margin-top:1em;position:absolute;width:100%;">';
                html += virtualFolder.Name;
                html += "</div>";
            }
            html += "</div>";

            html += '</div>';
        }

        // cardContent
        html += "</div>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        if (virtualFolder.showMenu !== false) {
            html += '<div class="cardText" style="text-align:right; float:right;padding-top:5px;">';
            html += '<paper-icon-button icon="' + AppInfo.moreIcon + '" class="btnCardMenu"></paper-icon-button>';
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

    pageClassOn('pageinit', "mediaLibraryPage", function () {

        var page = this;
        $('#selectCollectionType', page).on('change', function () {

            var index = this.selectedIndex;
            if (index != -1) {

                var name = this.options[index].innerHTML
                    .replace('*', '')
                    .replace('&amp;', '&');

                var value = this.value;

                $('#txtValue', page).val(name);

                var folderOption = getCollectionTypeOptions().filter(function (i) {

                    return i.value == value;

                })[0];

                $('.collectionTypeFieldDescription', page).html(folderOption.message || '');
            }
        });
    });

    pageClassOn('pageshow', "mediaLibraryPage", function () {

        var page = this;
        reloadLibrary(page);

    });

})();

var WizardLibraryPage = {

    next: function () {

        Dashboard.showLoadingMsg();

        var apiClient = ApiClient;

        apiClient.ajax({
            type: "POST",
            url: apiClient.getUrl('System/Configuration/MetadataPlugins/Autoset')

        }).done(function () {

            Dashboard.hideLoadingMsg();
            Dashboard.navigate('wizardsettings.html');
        });
    }
};

(function ($, document, window) {

    pageIdOn('pageshow', "mediaLibraryPage", function () {

        var page = this;

        // on here
        $('.btnRefresh', page).taskButton({
            mode: 'on',
            progressElem: page.querySelector('.refreshProgress'),
            lastResultElem: $('.lastRefreshResult', page),
            taskKey: 'RefreshLibrary'
        });

    });

    pageIdOn('pagebeforehide', "mediaLibraryPage", function () {

        var page = this;

        // off here
        $('.btnRefresh', page).taskButton({
            mode: 'off'
        });

    });

})(jQuery, document, window);