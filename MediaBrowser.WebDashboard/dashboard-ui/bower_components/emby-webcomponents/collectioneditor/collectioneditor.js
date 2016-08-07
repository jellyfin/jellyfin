define(['shell', 'dialogHelper', 'loading', 'layoutManager', 'connectionManager', 'embyRouter', 'globalize', 'emby-checkbox', 'emby-input', 'paper-icon-button-light', 'emby-select', 'material-icons', 'css!./../formdialog', 'emby-button'], function (shell, dialogHelper, loading, layoutManager, connectionManager, embyRouter, globalize) {

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

        var collectionId = panel.querySelector('#selectCollectionToAddTo').value;

        var apiClient = connectionManager.getApiClient(currentServerId);

        if (collectionId) {
            addToCollection(apiClient, panel, collectionId);
        } else {
            createCollection(apiClient, panel);
        }

        e.preventDefault();
        return false;
    }

    function createCollection(apiClient, dlg) {

        var url = apiClient.getUrl("Collections", {

            Name: dlg.querySelector('#txtNewCollectionName').value,
            IsLocked: !dlg.querySelector('#chkEnableInternetMetadata').checked,
            Ids: dlg.querySelector('.fldSelectedItemIds').value || ''

            //ParentId: getParameterByName('parentId') || LibraryMenu.getTopParentId()

        });

        apiClient.ajax({
            type: "POST",
            url: url,
            dataType: "json"

        }).then(function (result) {

            loading.hide();

            var id = result.Id;

            dialogHelper.close(dlg);
            redirectToCollection(apiClient, id);

        });
    }

    function redirectToCollection(apiClient, id) {

        apiClient.getItem(apiClient.getCurrentUserId(), id).then(function (item) {

            embyRouter.showItem(item);
        });
    }

    function addToCollection(apiClient, dlg, id) {

        var url = apiClient.getUrl("Collections/" + id + "/Items", {

            Ids: dlg.querySelector('.fldSelectedItemIds').value || ''
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

    function populateCollections(panel) {

        loading.show();

        var select = panel.querySelector('#selectCollectionToAddTo');

        panel.querySelector('.newCollectionInfo').classList.add('hide');

        var options = {

            Recursive: true,
            IncludeItemTypes: "BoxSet",
            SortBy: "SortName"
        };

        var apiClient = connectionManager.getApiClient(currentServerId);
        apiClient.getItems(apiClient.getCurrentUserId(), options).then(function (result) {

            var html = '';

            html += '<option value="">' + globalize.translate('sharedcomponents#OptionNew') + '</option>';

            html += result.Items.map(function (i) {

                return '<option value="' + i.Id + '">' + i.Name + '</option>';
            });

            select.innerHTML = html;
            select.value = '';
            triggerChange(select);

            loading.hide();
        });
    }

    function getEditorHtml() {

        var html = '';

        html += '<div class="formDialogContent smoothScrollY" style="padding-top:2em;">';
        html += '<div class="dialogContentInner dialog-content-centered">';
        html += '<form class="newCollectionForm" style="margin:auto;">';

        html += '<div>';
        html += globalize.translate('sharedcomponents#NewCollectionHelp');
        html += '</div>';

        html += '<div class="fldSelectCollection">';
        html += '<br/>';
        html += '<br/>';
        html += '<select is="emby-select" label="' + globalize.translate('sharedcomponents#LabelCollection') + '" id="selectCollectionToAddTo" autofocus></select>';
        html += '</div>';

        html += '<div class="newCollectionInfo">';

        html += '<div class="inputContainer">';
        html += '<input is="emby-input" type="text" id="txtNewCollectionName" required="required" label="' + globalize.translate('sharedcomponents#LabelName') + '" />';
        html += '<div class="fieldDescription">' + globalize.translate('sharedcomponents#NewCollectionNameExample') + '</div>';
        html += '</div>';

        html += '<label class="checkboxContainer">';
        html += '<input is="emby-checkbox" type="checkbox" id="chkEnableInternetMetadata" />';
        html += '<span>' + globalize.translate('sharedcomponents#SearchForCollectionInternetMetadata') + '</span>';
        html += '</label>';

        // newCollectionInfo
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

    function onHelpClick(e) {

        shell.openUrl(this.href);
        e.preventDefault();
        return false;
    }

    function initEditor(content, items) {

        content.querySelector('#selectCollectionToAddTo').addEventListener('change', function () {
            if (this.value) {
                content.querySelector('.newCollectionInfo').classList.add('hide');
                content.querySelector('#txtNewCollectionName').removeAttribute('required');
            } else {
                content.querySelector('.newCollectionInfo').classList.remove('hide');
                content.querySelector('#txtNewCollectionName').setAttribute('required', 'required');
            }
        });

        content.querySelector('form').addEventListener('submit', onSubmit);

        content.querySelector('.fldSelectedItemIds', content).value = items.join(',');

        if (items.length) {
            content.querySelector('.fldSelectCollection').classList.remove('hide');
            populateCollections(content);
        } else {
            content.querySelector('.fldSelectCollection').classList.add('hide');

            var selectCollectionToAddTo = content.querySelector('#selectCollectionToAddTo');
            selectCollectionToAddTo.innerHTML = '';
            selectCollectionToAddTo.value = '';
            triggerChange(selectCollectionToAddTo);
        }
    }

    function centerFocus(elem, horiz, on) {
        require(['scrollHelper'], function (scrollHelper) {
            var fn = on ? 'on' : 'off';
            scrollHelper.centerFocus[fn](elem, horiz);
        });
    }

    function collectioneditor() {

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
            var title = items.length ? globalize.translate('sharedcomponents#HeaderAddToCollection') : globalize.translate('sharedcomponents#NewCollection');

            html += '<div class="formDialogHeader">';
            html += '<button is="paper-icon-button-light" class="btnCancel autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
            html += '<div class="formDialogHeaderTitle">';
            html += title;
            html += '</div>';

            html += '<a class="btnHelp" href="https://github.com/MediaBrowser/Wiki/wiki/Collections" target="_blank" style="margin-left:auto;margin-right:.5em;display:inline-block;padding:.25em;display:flex;align-items:center;" title="' + globalize.translate('sharedcomponents#Help') + '"><i class="md-icon">info</i><span style="margin-left:.25em;">' + globalize.translate('sharedcomponents#Help') + '</span></a>';

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

    return collectioneditor;
});