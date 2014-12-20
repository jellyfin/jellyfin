var MediaLibraryPage = {

    onPageInit: function () {

        var page = this;
        $('#selectCollectionType', page).on('change', function () {

            var index = this.selectedIndex;
            if (index != -1) {

                var name = this.options[index].innerHTML
                    .replace('*', '')
                    .replace('&amp;', '&');

                var value = this.value;

                $('#txtValue', page).val(name);

                var folderOption = MediaLibraryPage.getCollectionTypeOptions().filter(function (i) {

                    return i.value == value;

                })[0];

                $('.collectionTypeFieldDescription', page).html(folderOption.message || '');
            }
        });
    },

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
            message: Globalize.translate('HeaderChangeFolderTypeHelp'),
            title: Globalize.translate('HeaderChangeFolderType')
        });
    },

    getVirtualFolderHtml: function (virtualFolder, isCollapsed, index, addPathMappingInfo) {

        isCollapsed = isCollapsed ? "true" : "false";
        var html = '<div class="collapsibleVirtualFolder" data-mini="true" data-role="collapsible" data-collapsed="' + isCollapsed + '">';

        html += '<h3>' + virtualFolder.Name + '</h3>';

        var typeName = MediaLibraryPage.getCollectionTypeOptions().filter(function (t) {

            return t.value == virtualFolder.CollectionType;

        })[0];

        typeName = typeName ? typeName.name : Globalize.translate('FolderTypeMixed');

        html += '<p style="padding-left:.5em;">';

        html += Globalize.translate('LabelFolderTypeValue').replace('{0}', '<b>' + typeName + '</b>');
        html += '</p><ul class="mediaFolderLocations" data-inset="true" data-role="listview" data-split-icon="minus">';

        html += '<li data-role="list-divider" class="mediaLocationsHeader">' + Globalize.translate('HeaderMediaLocations');
        html += '<button type="button" data-icon="plus" data-mini="true" data-iconpos="notext" data-inline="true" onclick="MediaLibraryPage.addMediaLocation(' + index + ');">' + Globalize.translate('ButtonAdd') + '</button>';
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
            html += '<div class="fieldDescription" style="margin:.5em 0 1.5em;">' + Globalize.translate('LabelPathSubstitutionHelp') + '</div>';
        }

        html += '<p>';
        html += '<button type="button" data-inline="true" data-icon="minus" data-folderindex="' + index + '" onclick="MediaLibraryPage.deleteVirtualFolder(this);" data-mini="true">' + Globalize.translate('ButtonRemove') + '</button>';
        html += '<button type="button" data-inline="true" data-icon="edit" data-folderindex="' + index + '" onclick="MediaLibraryPage.renameVirtualFolder(this);" data-mini="true">' + Globalize.translate('ButtonRename') + '</button>';
        html += '<button type="button" data-inline="true" data-icon="edit" data-folderindex="' + index + '" onclick="MediaLibraryPage.changeCollectionType(this);" data-mini="true">' + Globalize.translate('ButtonChangeType') + '</button>';
        html += '</p>';

        html += '</div>';

        return html;
    },

    addVirtualFolder: function () {

        $('.collectionTypeFieldDescription').show();

        MediaLibraryPage.getTextValue(Globalize.translate('HeaderAddMediaFolder'), Globalize.translate('LabelName'), "", true, function (name, type) {

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
            { name: Globalize.translate('FolderTypeMovies'), value: "movies" },
            { name: Globalize.translate('FolderTypeMusic'), value: "music" },
            { name: Globalize.translate('FolderTypeTvShows'), value: "tvshows" },
            { name: Globalize.translate('FolderTypeBooks'), value: "books", message: Globalize.translate('MessageBookPluginRequired') },
            { name: Globalize.translate('FolderTypeGames'), value: "games", message: Globalize.translate('MessageGamePluginRequired') },
            { name: Globalize.translate('FolderTypeHomeVideos'), value: "homevideos" },
            { name: Globalize.translate('FolderTypeMusicVideos'), value: "musicvideos" },
            { name: Globalize.translate('FolderTypePhotos'), value: "photos" },
            { name: Globalize.translate('FolderTypeMixed'), value: "mixed", message: Globalize.translate('MessageMixedContentHelp') }
        ];

    },

    renameVirtualFolder: function (button) {

        var folderIndex = button.getAttribute('data-folderindex');
        var virtualFolder = MediaLibraryPage.virtualFolders[folderIndex];

        MediaLibraryPage.lastVirtualFolderName = virtualFolder.Name;

        $('.collectionTypeFieldDescription').hide();

        MediaLibraryPage.getTextValue(Globalize.translate('HeaderRenameMediaFolder'), Globalize.translate('LabelNewName'), virtualFolder.Name, false, function (newName) {

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

        var msg = Globalize.translate('MessageAreYouSureYouWishToRemoveMediaFolder');

        if (locations.length) {
            msg += "<br/><br/>" + Globalize.translate("MessageTheFollowingLocationWillBeRemovedFromLibrary") + "<br/><br/>";
            msg += locations.join("<br/>");
        }

        MediaLibraryPage.lastVirtualFolderName = virtualFolder.Name;

        Dashboard.confirm(msg, Globalize.translate('HeaderRemoveMediaFolder'), function (confirmResult) {

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

        Dashboard.confirm(Globalize.translate('MessageConfirmRemoveMediaLocation'), Globalize.translate('HeaderRemoveMediaLocation'), function (confirmResult) {

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

$(document).on('pageinit', ".mediaLibraryPage", MediaLibraryPage.onPageInit).on('pageshow', ".mediaLibraryPage", MediaLibraryPage.onPageShow);

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

    function pollTasks(page) {

        ApiClient.getScheduledTasks().done(function (tasks) {

            updateTasks(page, tasks);
        });

    }

    function updateTasks(page, tasks) {

        $('.refreshLibraryPanel', page).removeClass('hide');

        var task = tasks.filter(function (t) {

            return t.Key == 'RefreshLibrary';

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
            $('.lastRefreshResult', page).html('<span style="color:#FF0000;">' + Globalize.translate('LabelFailed') + '</span>');
        }
        else if (lastResult == "Cancelled") {
            $('.lastRefreshResult', page).html('<span style="color:#0026FF;">' + Globalize.translate('LabelCancelled') + '</span>');
        }
        else if (lastResult == "Aborted") {
            $('.lastRefreshResult', page).html('<span style="color:#FF0000;">' + Globalize.translate('LabelAbortedByServerShutdown') + '</span>');
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

        var apiClient = ApiClient;

        if (apiClient.isWebSocketOpen()) {
            apiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "1000,1000");
        }

        $(apiClient).on("websocketmessage", onWebSocketMessage).on('websocketopen', function () {

            if (apiClient.isWebSocketOpen()) {
                apiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "1000,1000");
            }
        });

    }).on('pagehide', "#mediaLibraryPage", function () {

        var page = this;

        var apiClient = ApiClient;

        if (apiClient.isWebSocketOpen()) {
            apiClient.sendWebSocketMessage("ScheduledTasksInfoStop");
        }

        $(apiClient).off("websocketmessage", onWebSocketMessage);
    });

})(jQuery, document, window);
