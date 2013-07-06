var MediaLibraryPage = {

    onPageShow: function () {

        MediaLibraryPage.lastVirtualFolderName = "";

        MediaLibraryPage.reloadLibrary(this);
    },

    reloadLibrary: function (page) {

        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");

        if (userId) {

            $('#userProfileNavigation', page).show();
            $('#defaultNavigation', page).hide();

            ApiClient.getUser(userId).done(function (user) {
                Dashboard.setPageTitle(user.Name);

                $('#fldUseDefaultLibrary', page).show();

                $('#chkUseDefaultLibrary', page).checked(!user.Configuration.UseCustomLibrary).checkboxradio("refresh");

                if (user.Configuration.UseCustomLibrary) {

                    ApiClient.getVirtualFolders(userId).done(function (result) {
                        MediaLibraryPage.reloadVirtualFolders(page, result);
                    });

                    $(".editing_default", page).hide();
                    $('#divMediaLibrary', page).show();
                } else {
                    $('#divMediaLibrary', page).hide();
                    Dashboard.hideLoadingMsg();
                }

            });

        } else {

            $('#userProfileNavigation', page).hide();
            $('#defaultNavigation', page).show();

            ApiClient.getVirtualFolders().done(function (result) {
                MediaLibraryPage.reloadVirtualFolders(page, result);
            });

            $('#fldUseDefaultLibrary', page).hide();
            $('#divMediaLibrary', page).show();
            Dashboard.setPageTitle("Media Library");
        }
    },

    reloadVirtualFolders: function (page, virtualFolders) {

        if (virtualFolders) {
            MediaLibraryPage.virtualFolders = virtualFolders;
        } else {
            virtualFolders = MediaLibraryPage.virtualFolders;
        }

        var html = '';

        for (var i = 0, length = virtualFolders.length; i < length; i++) {

            var virtualFolder = virtualFolders[i];

            var isCollapsed = MediaLibraryPage.lastVirtualFolderName != virtualFolder.Name;

            html += MediaLibraryPage.getVirtualFolderHtml(virtualFolder, isCollapsed, i);
        }

        $('#divVirtualFolders', page).html(html).trigger('create');

        Dashboard.hideLoadingMsg();
    },

    getVirtualFolderHtml: function (virtualFolder, isCollapsed, index) {

        isCollapsed = isCollapsed ? "true" : "false";
        var html = '<div class="collapsibleVirtualFolder" data-role="collapsible" data-collapsed="' + isCollapsed + '" data-content-theme="c">';

        html += '<h3>' + virtualFolder.Name + '</h3>';

        html += '<ul class="mediaFolderLocations" data-inset="true" data-role="listview" data-split-icon="minus">';

        html += '<li data-role="list-divider" class="mediaLocationsHeader">Media Locations';
        html += '<button type="button" data-icon="plus" data-mini="true" data-theme="c" data-inline="true" data-iconpos="notext" onclick="MediaLibraryPage.addMediaLocation(' + index + ');"></button>';
        html += '</li>';

        for (var i = 0, length = virtualFolder.Locations.length; i < length; i++) {

            var location = virtualFolder.Locations[i];
            html += '<li>';
            html += '<a class="lnkMediaLocation" href="#">' + location + '</a>';
            html += '<a href="#" data-index="' + i + '" data-folderindex="' + index + '" onclick="MediaLibraryPage.deleteMediaLocation(this);"></a>';
            html += '</li>';
        }
        html += '</ul>';

        html += '<p>';
        html += '<button type="button" data-inline="true" data-icon="minus" data-folderindex="' + index + '" onclick="MediaLibraryPage.deleteVirtualFolder(this);" data-mini="true">Remove collection</button>';
        html += '<button type="button" data-inline="true" data-icon="pencil" data-folderindex="' + index + '" onclick="MediaLibraryPage.renameVirtualFolder(this);" data-mini="true">Rename collection</button>';
        html += '</p>';

        html += '</div>';

        return html;
    },

    setUseDefaultMediaLibrary: function (useDefaultLibrary) {

        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");
        var page = $.mobile.activePage;

        ApiClient.getUser(userId).done(function (user) {

            user.Configuration.UseCustomLibrary = !useDefaultLibrary;

            ApiClient.updateUser(user).done(function () {
                MediaLibraryPage.reloadLibrary(page);
            });

            $(".editing_default", page).hide();
        });
    },

    addVirtualFolder: function () {

        MediaLibraryPage.getTextValue("Add Media Collection", "Name (Movies, Music, TV, etc):", "", function (name) {

            var userId = getParameterByName("userId");

            MediaLibraryPage.lastVirtualFolderName = name;

            ApiClient.addVirtualFolder(name, userId).done(MediaLibraryPage.processOperationResult);

        });
    },

    addMediaLocation: function (virtualFolderIndex) {

        MediaLibraryPage.selectDirectory(function (path) {

            if (path) {

                var virtualFolder = MediaLibraryPage.virtualFolders[virtualFolderIndex];

                MediaLibraryPage.lastVirtualFolderName = virtualFolder.Name;

                var userId = getParameterByName("userId");

                ApiClient.addMediaPath(virtualFolder.Name, path, userId).done(MediaLibraryPage.processOperationResult);
            }

        });
    },

    selectDirectory: function (callback) {

        var picker = new DirectoryBrowser($.mobile.activePage);

        picker.show({ callback: callback });

        MediaLibraryPage.directoryPicker = picker;
    },

    getTextValue: function (header, label, initialValue, callback) {

        var page = $.mobile.activePage;

        var popup = $('#popupEnterText', page);

        $('h3', popup).html(header);
        $('label', popup).html(label);
        $('#txtValue', popup).val(initialValue);

        popup.on("popupafteropen", function () {
            $('#textEntryForm input:first', this).focus();
        }).on("popupafterclose", function () {
            $(this).off("popupafterclose").off("click");
            $('#textEntryForm', this).off("submit");
        }).popup("open");

        $('#textEntryForm', popup).on('submit', function () {

            if (callback) {
                callback($('#txtValue', popup).val());
            }

            return false;
        });
    },

    renameVirtualFolder: function (button) {

        var folderIndex = button.getAttribute('data-folderindex');
        var virtualFolder = MediaLibraryPage.virtualFolders[folderIndex];

        MediaLibraryPage.lastVirtualFolderName = virtualFolder.Name;

        MediaLibraryPage.getTextValue(virtualFolder.Name, "Rename " + virtualFolder.Name, virtualFolder.Name, function (newName) {

            if (virtualFolder.Name != newName) {

                var userId = getParameterByName("userId");

                ApiClient.renameVirtualFolder(virtualFolder.Name, newName, userId).done(MediaLibraryPage.processOperationResult);
            }
        });
    },

    deleteVirtualFolder: function (button) {

        var folderIndex = button.getAttribute('data-folderindex');
        var virtualFolder = MediaLibraryPage.virtualFolders[folderIndex];

        var parent = $(button).parents('.collapsibleVirtualFolder');

        var locations = $('.lnkMediaLocation', parent).map(function () {
            return this.innerHTML;
        }).get();

        var msg = "Are you sure you wish to remove " + virtualFolder.Name + "?";

        if (locations.length) {
            msg += "<br/><br/>The following media locations will be removed from your library:<br/><br/>";
            msg += locations.join("<br/>");
        }

        MediaLibraryPage.lastVirtualFolderName = virtualFolder.Name;

        Dashboard.confirm(msg, "Remove Media Folder", function (confirmResult) {

            if (confirmResult) {

                var userId = getParameterByName("userId");

                ApiClient.removeVirtualFolder(virtualFolder.Name, userId).done(MediaLibraryPage.processOperationResult);
            }

        });
    },

    deleteMediaLocation: function (button) {

        var folderIndex = button.getAttribute('data-folderindex');
        var index = parseInt(button.getAttribute('data-index'));

        var virtualFolder = MediaLibraryPage.virtualFolders[folderIndex];

        MediaLibraryPage.lastVirtualFolderName = virtualFolder.Name;

        var location = virtualFolder.Locations[index];

        Dashboard.confirm("Are you sure you wish to remove " + location + "?", "Remove Media Location", function (confirmResult) {

            if (confirmResult) {

                var userId = getParameterByName("userId");

                ApiClient.removeMediaPath(virtualFolder.Name, location, userId).done(MediaLibraryPage.processOperationResult);
            }
        });
    },

    processOperationResult: function (result) {
        Dashboard.hideLoadingMsg();

        var page = $.mobile.activePage;

        $('#popupEnterText', page).popup("close");
        
        if (MediaLibraryPage.directoryPicker) {
            MediaLibraryPage.directoryPicker.close();
            MediaLibraryPage.directoryPicker = null;
        }

        MediaLibraryPage.reloadLibrary(page);
    }
};

$(document).on('pageshow', ".mediaLibraryPage", MediaLibraryPage.onPageShow);