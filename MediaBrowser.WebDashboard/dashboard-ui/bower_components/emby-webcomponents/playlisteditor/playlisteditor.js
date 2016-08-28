define(['shell', 'dialogHelper', 'loading', 'layoutManager', 'connectionManager', 'embyRouter', 'globalize', 'emby-input', 'paper-icon-button-light', 'emby-select', 'material-icons', 'css!./../formdialog', 'emby-button'], function (shell, dialogHelper, loading, layoutManager, connectionManager, embyRouter, globalize) {

    var lastPlaylistId = '';
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

        loading.show();

        var panel = parentWithClass(this, 'dialog');

        var playlistId = panel.querySelector('#selectPlaylistToAddTo').value;
        var apiClient = connectionManager.getApiClient(currentServerId);

        if (playlistId) {
            lastPlaylistId = playlistId;
            addToPlaylist(apiClient, panel, playlistId);
        } else {
            createPlaylist(apiClient, panel);
        }

        e.preventDefault();
        return false;
    }

    function createPlaylist(apiClient, dlg) {

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

        var url = apiClient.getUrl("Playlists/" + id + "/Items", {

            Ids: dlg.querySelector('.fldSelectedItemIds').value || '',
            userId: apiClient.getCurrentUserId()
        });

        apiClient.ajax({
            type: "POST",
            url: url

        }).then(function () {

            loading.hide();

            dialogHelper.close(dlg);

            require(['toast'], function (toast) {
                toast(globalize.translate('sharedcomponents#MessageItemsAdded'));
            });
        });
    }

    function triggerChange(select) {
        select.dispatchEvent(new CustomEvent('change', {}));
    }

    function populatePlaylists(panel) {

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

            html += '<option value="">' + globalize.translate('sharedcomponents#OptionNew') + '</option>';

            html += result.Items.map(function (i) {

                return '<option value="' + i.Id + '">' + i.Name + '</option>';
            });

            select.innerHTML = html;
            select.value = lastPlaylistId || '';
            triggerChange(select);

            loading.hide();
        });
    }

    function getEditorHtml() {

        var html = '';

        html += '<div class="formDialogContent smoothScrollY" style="padding-top:2em;">';
        html += '<div class="dialogContentInner dialog-content-centered">';
        html += '<form style="margin:auto;">';

        html += '<div class="fldSelectPlaylist">';
        html += '<select is="emby-select" id="selectPlaylistToAddTo" label="' + globalize.translate('sharedcomponents#LabelPlaylist') + '" autofocus></select>';
        html += '</div>';

        html += '<div class="newPlaylistInfo">';

        html += '<div class="inputContainer">';
        html += '<input is="emby-input" type="text" id="txtNewPlaylistName" required="required" label="' + globalize.translate('sharedcomponents#LabelName') + '" />';
        html += '</div>';

        // newPlaylistInfo
        html += '</div>';

        html += '<div>';
        html += '<button is="emby-button" type="submit" class="raised btnSubmit block">' + globalize.translate('sharedcomponents#ButtonOk') + '</button>';
        html += '</div>';

        html += '<input type="hidden" class="fldSelectedItemIds" />';

        html += '</form>';
        html += '</div>';
        html += '</div>';

        return html;
    }

    function initEditor(content, items) {

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
            populatePlaylists(content);
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
            html += '<div class="formDialogHeaderTitle">';
            html += title;
            html += '</div>';

            html += '</div>';

            html += getEditorHtml();

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            initEditor(dlg, items);

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