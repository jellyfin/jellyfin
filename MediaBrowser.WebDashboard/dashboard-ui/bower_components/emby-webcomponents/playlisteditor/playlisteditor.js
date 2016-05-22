define(['shell', 'dialogHelper', 'loading', 'layoutManager', 'connectionManager', 'scrollHelper', 'embyRouter', 'globalize', 'paper-checkbox', 'paper-input', 'paper-icon-button-light', 'emby-select', 'html!./../icons/nav.html', 'css!./../formdialog'], function (shell, dialogHelper, loading, layoutManager, connectionManager, scrollHelper, embyRouter, globalize) {

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

        html += '<div class="dialogContent smoothScrollY">';
        html += '<div class="dialogContentInner centeredContent">';
        html += '<form style="margin:auto;">';

        html += '<div class="fldSelectPlaylist">';
        html += '<select is="emby-select" id="selectPlaylistToAddTo" label="' + globalize.translate('sharedcomponents#LabelPlaylist') + '" autofocus></select>';
        html += '</div>';

        html += '<div class="newPlaylistInfo">';

        html += '<div>';
        html += '<paper-input type="text" id="txtNewPlaylistName" required="required" label="' + globalize.translate('sharedcomponents#LabelName') + '"></paper-input>';
        html += '</div>';

        html += '<br />';

        // newPlaylistInfo
        html += '</div>';

        html += '<br />';
        html += '<div>';
        html += '<paper-button raised class="btnSubmit block">' + globalize.translate('sharedcomponents#ButtonOk') + '</paper-button>';
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

        populatePlaylists(content);

        content.querySelector('.btnSubmit').addEventListener('click', function () {
            // Do a fake form submit this the button isn't a real submit button
            var fakeSubmit = document.createElement('input');
            fakeSubmit.setAttribute('type', 'submit');
            fakeSubmit.style.display = 'none';
            var form = content.querySelector('form');
            form.appendChild(fakeSubmit);
            fakeSubmit.click();

            // Seeing issues in smart tv browsers where the form does not get submitted if the button is removed prior to the submission actually happening
            setTimeout(function () {
                form.removeChild(fakeSubmit);
            }, 500);
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
            var title = globalize.translate('sharedcomponents#AddToPlaylist');

            html += '<div class="dialogHeader" style="margin:0 0 2em;">';
            html += '<button is="paper-icon-button-light" class="btnCancel" tabindex="-1"><iron-icon icon="nav:arrow-back"></iron-icon></button>';
            html += '<div class="dialogHeaderTitle">';
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
                scrollHelper.centerFocus.on(dlg.querySelector('.dialogContent'), false);
            }

            return new Promise(function (resolve, reject) {

                dlg.addEventListener('close', resolve);
                dialogHelper.open(dlg);
            });
        };
    }

    return playlisteditor;
});