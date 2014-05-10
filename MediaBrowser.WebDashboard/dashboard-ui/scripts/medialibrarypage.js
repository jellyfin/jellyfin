var MediaLibraryPage = {

    onPageShow: function () {

        var page = this;
        MediaLibraryPage.lastVirtualFolderName = "";

        MediaLibraryPage.reloadLibrary(page);
    },
    
    reloadLibrary: function (page) {

        Dashboard.showLoadingMsg();

        ApiClient.getVirtualFolders().done(function (result) {
            MediaLibraryPage.reloadVirtualFolders(page, result);
        });

        $('#divMediaLibrary', page).show();
        Dashboard.setPageTitle("Media Library");
    },

    shouldRefreshLibraryAfterChanges: function () {

        return $($.mobile.activePage).is('#mediaLibraryPage');
    },

    reloadVirtualFolders: function (page, virtualFolders) {

        if (virtualFolders) {
            MediaLibraryPage.virtualFolders = virtualFolders;
        } else {
            virtualFolders = MediaLibraryPage.virtualFolders;
        }

        var html = '';

        var addPathMappingInfo = $(page).is('#mediaLibraryPage');

        for (var i = 0, length = virtualFolders.length; i < length; i++) {

            var virtualFolder = virtualFolders[i];

            var isCollapsed = MediaLibraryPage.lastVirtualFolderName != virtualFolder.Name;


            html += MediaLibraryPage.getVirtualFolderHtml(virtualFolder, isCollapsed, i, addPathMappingInfo);
        }

        $('#divVirtualFolders', page).html(html).trigger('create');

        Dashboard.hideLoadingMsg();
    },

    changeCollectionType: function () {

        Dashboard.alert({
            message: "To change the folder type, please remove and rebuild the collection with the new type.",
            title: "Change Folder Type"
        });
    },

    getVirtualFolderHtml: function (virtualFolder, isCollapsed, index, addPathMappingInfo) {

        isCollapsed = isCollapsed ? "true" : "false";
        var html = '<div class="collapsibleVirtualFolder" data-mini="true" data-role="collapsible" data-collapsed="' + isCollapsed + '">';

        html += '<h3>' + virtualFolder.Name + '</h3>';

        var typeName = MediaLibraryPage.getCollectionTypeOptions().filter(function (t) {

            return t.value == virtualFolder.CollectionType;

        })[0];

        typeName = typeName ? typeName.name : "Mixed movies & tv";

        html += '<p style="padding-left:.5em;">Folder type: <b>' + typeName + '</b></p>';

        html += '<ul class="mediaFolderLocations" data-inset="true" data-role="listview" data-split-icon="minus">';

        html += '<li data-role="list-divider" class="mediaLocationsHeader">Media Locations';
        html += '<button type="button" data-icon="plus" data-mini="true" data-inline="true" data-iconpos="notext" onclick="MediaLibraryPage.addMediaLocation(' + index + ');"></button>';
        html += '</li>';

        for (var i = 0, length = virtualFolder.Locations.length; i < length; i++) {

            var location = virtualFolder.Locations[i];
            html += '<li>';
            html += '<a style="font-size:14px;" class="lnkMediaLocation" href="#">' + location + '</a>';
            html += '<a href="#" data-index="' + i + '" data-folderindex="' + index + '" onclick="MediaLibraryPage.deleteMediaLocation(this);"></a>';
            html += '</li>';
        }
        html += '</ul>';

        if (addPathMappingInfo) {
            html += '<p style="margin:1.5em 0;">Optional: <a href="librarypathmapping.html">Path substitution</a> can map server paths to network shares that clients can access for direct playback.</p>';
        }

        html += '<p>';
        html += '<button type="button" data-inline="true" data-icon="minus" data-folderindex="' + index + '" onclick="MediaLibraryPage.deleteVirtualFolder(this);" data-mini="true">Remove</button>';
        html += '<button type="button" data-inline="true" data-icon="edit" data-folderindex="' + index + '" onclick="MediaLibraryPage.renameVirtualFolder(this);" data-mini="true">Rename</button>';
        html += '<button type="button" data-inline="true" data-icon="edit" data-folderindex="' + index + '" onclick="MediaLibraryPage.changeCollectionType(this);" data-mini="true">Change type</button>';
        html += '</p>';

        html += '</div>';

        return html;
    },

    addVirtualFolder: function () {

        $('.collectionTypeFieldDescription').show();

        MediaLibraryPage.getTextValue("Add Media Folder", "Name (Movies, Music, TV, etc):", "", true, function (name, type) {

            MediaLibraryPage.lastVirtualFolderName = name;

            var refreshAfterChange = MediaLibraryPage.shouldRefreshLibraryAfterChanges();

            ApiClient.addVirtualFolder(name, type, refreshAfterChange).done(MediaLibraryPage.processOperationResult);

        });
    },

    addMediaLocation: function (virtualFolderIndex) {

        MediaLibraryPage.selectDirectory(function (path) {

            if (path) {

                var virtualFolder = MediaLibraryPage.virtualFolders[virtualFolderIndex];

                MediaLibraryPage.lastVirtualFolderName = virtualFolder.Name;

                var refreshAfterChange = MediaLibraryPage.shouldRefreshLibraryAfterChanges();

                ApiClient.addMediaPath(virtualFolder.Name, path, refreshAfterChange).done(MediaLibraryPage.processOperationResult);
            }

        });
    },

    selectDirectory: function (callback) {

        var picker = new DirectoryBrowser($.mobile.activePage);

        picker.show({ callback: callback });

        MediaLibraryPage.directoryPicker = picker;
    },

    getTextValue: function (header, label, initialValue, showCollectionType, callback) {

        var page = $.mobile.activePage;

        var popup = $('#popupEnterText', page);

        $('h3', popup).html(header);
        $('#lblValue', popup).html(label);
        $('#txtValue', popup).val(initialValue);

        if (showCollectionType) {
            $('#fldCollectionType', popup).show();
            $('#selectCollectionType', popup).attr('required', 'required').selectmenu('refresh');
        } else {
            $('#fldCollectionType', popup).hide();
            $('#selectCollectionType', popup).removeAttr('required').selectmenu('refresh');
        }

        $('#selectCollectionType', popup).html(MediaLibraryPage.getCollectionTypeOptionsHtml()).val('').selectmenu('refresh');

        popup.on("popupafteropen", function () {
            $('#textEntryForm input:first', this).focus();
        }).on("popupafterclose", function () {
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
    },

    getCollectionTypeOptionsHtml: function () {

        return MediaLibraryPage.getCollectionTypeOptions().filter(function (i) {

            return i.isSelectable !== false;

        }).map(function (i) {

            return '<option value="' + i.value + '">' + i.name + '</option>';

        }).join("");
    },

    getCollectionTypeOptions: function () {

        return [

            { name: "", value: "" },
            { name: "Movies", value: "movies" },
            { name: "Music", value: "music" },
            { name: "TV shows", value: "tvshows" },
            { name: "Books*", value: "books" },
            { name: "Collections", value: "boxsets" },
            { name: "Games*", value: "games" },
            { name: "Home videos", value: "homevideos" },
            { name: "Music videos", value: "musicvideos" },
            { name: "Photos", value: "photos" },
            { name: "Trailers", value: "trailers" },
            { name: "Adult videos", value: "adultvideos" },
            { name: "Mixed movies & tv", value: "mixed" }
        ];

    },

    renameVirtualFolder: function (button) {

        var folderIndex = button.getAttribute('data-folderindex');
        var virtualFolder = MediaLibraryPage.virtualFolders[folderIndex];

        MediaLibraryPage.lastVirtualFolderName = virtualFolder.Name;

        $('.collectionTypeFieldDescription').hide();

        MediaLibraryPage.getTextValue("Rename Media Folder", "New name:", virtualFolder.Name, false, function (newName) {

            if (virtualFolder.Name != newName) {

                var refreshAfterChange = MediaLibraryPage.shouldRefreshLibraryAfterChanges();

                ApiClient.renameVirtualFolder(virtualFolder.Name, newName, refreshAfterChange).done(MediaLibraryPage.processOperationResult);
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

                var refreshAfterChange = MediaLibraryPage.shouldRefreshLibraryAfterChanges();

                ApiClient.removeVirtualFolder(virtualFolder.Name, refreshAfterChange).done(MediaLibraryPage.processOperationResult);
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

                var refreshAfterChange = MediaLibraryPage.shouldRefreshLibraryAfterChanges();

                ApiClient.removeMediaPath(virtualFolder.Name, location, refreshAfterChange).done(MediaLibraryPage.processOperationResult);
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

(function ($, document, window) {

    function pollTasks(page) {

        ApiClient.getScheduledTasks().done(function (tasks) {

            updateTasks(page, tasks);
        });

    }

    function updateTasks(page, tasks) {

        $('.refreshLibraryPanel', page).removeClass('hide');

        var task = tasks.filter(function (t) {

            return t.Name == 'Scan media library';

        })[0];

        $('.btnRefresh', page).buttonEnabled(task.State == 'Idle').attr('data-taskid', task.Id);

        var progress = (task.CurrentProgressPercentage || 0).toFixed(1);
        var progressElem = $('.refreshProgress', page).val(progress);

        if (task.State == 'Running') {
            progressElem.show();
        } else {
            progressElem.hide();
        }

        var lastResult = task.LastExecutionResult ? task.LastExecutionResult.Status : '';

        if (lastResult == "Failed") {
            $('.lastRefreshResult', page).html('<span style="color:#FF0000;">(failed)</span>');
        }
        else if (lastResult == "Cancelled") {
            $('.lastRefreshResult', page).html('<span style="color:#0026FF;">(cancelled)</span>');
        }
        else if (lastResult == "Aborted") {
            $('.lastRefreshResult', page).html('<span style="color:#FF0000;">(Aborted by server shutdown)</span>');
        } else {
            $('.lastRefreshResult', page).html(lastResult);
        }
    }

    function onWebSocketMessage(e, msg) {

        if (msg.MessageType == "ScheduledTasksInfo") {

            var tasks = msg.Data;

            var page = $.mobile.activePage;

            updateTasks(page, tasks);
        }
    }

    $(document).on('pageinit', "#mediaLibraryPage", function () {

        var page = this;

        $('.btnRefresh', page).on('click', function () {

            var button = this;
            var id = button.getAttribute('data-taskid');

            ApiClient.startScheduledTask(id).done(function () {

                pollTasks(page);
            });

        });

    }).on('pageshow', "#mediaLibraryPage", function () {

        var page = this;

        $('.refreshLibraryPanel', page).addClass('hide');

        pollTasks(page);

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "1000,1000");
        }

        $(ApiClient).on("websocketmessage", onWebSocketMessage).on('websocketopen', function () {

            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "1000,1000");
            }
        });

    }).on('pagehide', "#mediaLibraryPage", function () {

        var page = this;

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("ScheduledTasksInfoStop");
        }

        $(ApiClient).off("websocketmessage", onWebSocketMessage);
    });

})(jQuery, document, window);
