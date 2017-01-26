define(['shell', 'dialogHelper', 'loading', 'layoutManager', 'playbackManager', 'connectionManager', 'userSettings', 'embyRouter', 'globalize', 'emby-input', 'paper-icon-button-light', 'emby-select', 'material-icons', 'css!./../formdialog', 'emby-button'], function (shell, dialogHelper, loading, layoutManager, playbackManager, connectionManager, userSettings, embyRouter, globalize) {
    'use strict';

    var currentServerId;

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function onSubmit(e) {

        var panel = parentWithClass(this, 'dialog');

        var playlistId = panel.querySelector('#selectPlaylistToAddTo').value;
        var apiClient = connectionManager.getApiClient(currentServerId);

        if (playlistId) {
            userSettings.set('playlisteditor-lastplaylistid', playlistId);
            addToPlaylist(apiClient, panel, playlistId);
        } else {
            createPlaylist(apiClient, panel);
        }

        e.preventDefault();
        return false;
    }

    function createPlaylist(apiClient, dlg) {

        loading.show();

        var url = apiClient.getUrl("Playlists", {

            Name: dlg.querySelector('#txtNewPlaylistName').value,
            Ids: dlg.querySelector('.fldSelectedItemIds').value || '',
            userId: apiClient.getCurrentUserId()

        });

        apiClient.ajax({
            type: "POST",
            url: url,
            dataType: "json"

        }).then(function (result) {

            loading.hide();

            var id = result.Id;

            dialogHelper.close(dlg);
            redirectToPlaylist(apiClient, id);
        });
    }

    function redirectToPlaylist(apiClient, id) {

        apiClient.getItem(apiClient.getCurrentUserId(), id).then(function (item) {

            embyRouter.showItem(item);
        });
    }

    function addToPlaylist(apiClient, dlg, id) {

        var itemIds = dlg.querySelector('.fldSelectedItemIds').value || '';

        if (id === 'queue') {

            playbackManager.queue({
                serverId: apiClient.serverId(),
                ids: itemIds.split(',')
            });
            dialogHelper.close(dlg);
            return;
        }

        loading.show();

        var url = apiClient.getUrl("Playlists/" + id + "/Items", {

            Ids: itemIds,
            userId: apiClient.getCurrentUserId()
        });

        apiClient.ajax({
            type: "POST",
            url: url

        }).then(function () {

            loading.hide();

            dialogHelper.close(dlg);
        });
    }

    function triggerChange(select) {
        select.dispatchEvent(new CustomEvent('change', {}));
    }

    function populatePlaylists(editorOptions, panel) {

        var select = panel.querySelector('#selectPlaylistToAddTo');

        loading.hide();

        panel.querySelector('.newPlaylistInfo').classList.add('hide');

        var options = {

            Recursive: true,
            IncludeItemTypes: "Playlist",
            SortBy: 'SortName'
        };

        var apiClient = connectionManager.getApiClient(currentServerId);
        apiClient.getItems(apiClient.getCurrentUserId(), options).then(function (result) {

            var html = '';

            if (editorOptions.enableAddToPlayQueue !== false && playbackManager.isPlaying()) {
                html += '<option value="queue">' + globalize.translate('sharedcomponents#AddToPlayQueue') + '</option>';
            }

            html += '<option value="">' + globalize.translate('sharedcomponents#OptionNew') + '</option>';

            html += result.Items.map(function (i) {

                return '<option value="' + i.Id + '">' + i.Name + '</option>';
            });

            select.innerHTML = html;

            var defaultValue = editorOptions.defaultValue;
            if (!defaultValue) {
                defaultValue = userSettings.get('playlisteditor-lastplaylistid') || '';
            }
            select.value = defaultValue === 'new' ? '' : defaultValue;

            // If the value is empty set it again, in case we tried to set a lastplaylistid that is no longer valid
            if (!select.value) {
                select.value = '';
            }

            triggerChange(select);

            loading.hide();
        });
    }

    function getEditorHtml() {

        var html = '';

        html += '<div class="formDialogContent smoothScrollY" style="padding-top:2em;">';
        html += '<div class="dialogContentInner dialog-content-centered">';
        html += '<form style="margin:auto;">';

        html += '<div class="fldSelectPlaylist selectContainer">';
        html += '<select is="emby-select" id="selectPlaylistToAddTo" label="' + globalize.translate('sharedcomponents#LabelPlaylist') + '" autofocus></select>';
        html += '</div>';

        html += '<div class="newPlaylistInfo">';

        html += '<div class="inputContainer">';
        html += '<input is="emby-input" type="text" id="txtNewPlaylistName" required="required" label="' + globalize.translate('sharedcomponents#LabelName') + '" />';
        html += '</div>';

        // newPlaylistInfo
        html += '</div>';

        html += '<div class="formDialogFooter">';
        html += '<button is="emby-button" type="submit" class="raised btnSubmit block formDialogFooterItem button-submit">' + globalize.translate('sharedcomponents#Add') + '</button>';
        html += '</div>';

        html += '<input type="hidden" class="fldSelectedItemIds" />';

        html += '</form>';
        html += '</div>';
        html += '</div>';

        return html;
    }

    function initEditor(content, options, items) {

        content.querySelector('#selectPlaylistToAddTo').addEventListener('change', function () {
            if (this.value) {
                content.querySelector('.newPlaylistInfo').classList.add('hide');
                content.querySelector('#txtNewPlaylistName').removeAttribute('required');
            } else {
                content.querySelector('.newPlaylistInfo').classList.remove('hide');
                content.querySelector('#txtNewPlaylistName').setAttribute('required', 'required');
            }
        });

        content.querySelector('form').addEventListener('submit', onSubmit);

        content.querySelector('.fldSelectedItemIds', content).value = items.join(',');

        if (items.length) {
            content.querySelector('.fldSelectPlaylist').classList.remove('hide');
            populatePlaylists(options, content);
        } else {
            content.querySelector('.fldSelectPlaylist').classList.add('hide');

            var selectPlaylistToAddTo = content.querySelector('#selectPlaylistToAddTo');
            selectPlaylistToAddTo.innerHTML = '';
            selectPlaylistToAddTo.value = '';
            triggerChange(selectPlaylistToAddTo);
        }
    }

    function centerFocus(elem, horiz, on) {
        require(['scrollHelper'], function (scrollHelper) {
            var fn = on ? 'on' : 'off';
            scrollHelper.centerFocus[fn](elem, horiz);
        });
    }

    function playlisteditor() {

        var self = this;

        self.show = function (options) {

            var items = options.items || {};
            currentServerId = options.serverId;

            var dialogOptions = {
                removeOnClose: true,
                scrollY: false
            };

            if (layoutManager.tv) {
                dialogOptions.size = 'fullscreen';
            } else {
                dialogOptions.size = 'small';
            }

            var dlg = dialogHelper.createDialog(dialogOptions);

            dlg.classList.add('formDialog');

            var html = '';
            var title = globalize.translate('sharedcomponents#HeaderAddToPlaylist');

            html += '<div class="formDialogHeader">';
            html += '<button is="paper-icon-button-light" class="btnCancel autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
            html += '<h3 class="formDialogHeaderTitle">';
            html += title;
            html += '</h3>';

            html += '</div>';

            html += getEditorHtml();

            dlg.innerHTML = html;

            initEditor(dlg, options, items);

            dlg.querySelector('.btnCancel').addEventListener('click', function () {

                dialogHelper.close(dlg);
            });

            if (layoutManager.tv) {
                centerFocus(dlg.querySelector('.formDialogContent'), false, true);
            }

            return new Promise(function (resolve, reject) {

                if (layoutManager.tv) {
                    centerFocus(dlg.querySelector('.formDialogContent'), false, false);
                }

                dlg.addEventListener('close', resolve);
                dialogHelper.open(dlg);
            });
        };
    }

    return playlisteditor;
});