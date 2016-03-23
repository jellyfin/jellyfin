define(['dialogHelper', 'jQuery', 'paper-checkbox', 'paper-input'], function (dialogHelper, $) {

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var panel = $(this).parents('.dialog')[0];

        var collectionId = $('#selectCollectionToAddTo', panel).val();

        if (collectionId) {
            addToCollection(panel, collectionId);
        } else {
            createCollection(panel);
        }

        return false;
    }

    function createCollection(dlg) {

        var url = ApiClient.getUrl("Collections", {

            Name: $('#txtNewCollectionName', dlg).val(),
            IsLocked: !$('#chkEnableInternetMetadata', dlg).checked(),
            Ids: $('.fldSelectedItemIds', dlg).val() || ''

            //ParentId: getParameterByName('parentId') || LibraryMenu.getTopParentId()

        });

        ApiClient.ajax({
            type: "POST",
            url: url,
            dataType: "json"

        }).then(function (result) {

            Dashboard.hideLoadingMsg();

            var id = result.Id;

            dialogHelper.close(dlg);
            redirectToCollection(id);

        });
    }

    function redirectToCollection(id) {

        var context = getParameterByName('context');

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).then(function (item) {

            Dashboard.navigate(LibraryBrowser.getHref(item, context));

        });
    }

    function addToCollection(dlg, id) {

        var url = ApiClient.getUrl("Collections/" + id + "/Items", {

            Ids: $('.fldSelectedItemIds', dlg).val() || ''
        });

        ApiClient.ajax({
            type: "POST",
            url: url

        }).then(function () {

            Dashboard.hideLoadingMsg();

            dialogHelper.close(dlg);

            require(['toast'], function (toast) {
                toast(Globalize.translate('MessageItemsAdded'));
            });
        });
    }

    function onDialogClosed() {

        $(this).remove();
        Dashboard.hideLoadingMsg();
    }

    function populateCollections(panel) {

        Dashboard.showLoadingMsg();

        var select = $('#selectCollectionToAddTo', panel);

        $('.newCollectionInfo', panel).hide();

        var options = {

            Recursive: true,
            IncludeItemTypes: "BoxSet",
            SortBy: "SortName"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            var html = '';

            html += '<option value="">' + Globalize.translate('OptionNewCollection') + '</option>';

            html += result.Items.map(function (i) {

                return '<option value="' + i.Id + '">' + i.Name + '</option>';
            });

            select.html(html).val('').trigger('change');

            Dashboard.hideLoadingMsg();
        });
    }

    function getEditorHtml() {

        var html = '';

        html += '<form class="newCollectionForm" style="margin:auto;">';

        html += '<div class="fldSelectCollection">';
        html += '<label for="selectCollectionToAddTo" class="selectLabel">' + Globalize.translate('LabelSelectCollection') + '</label>';
        html += '<select id="selectCollectionToAddTo" data-mini="true"></select>';
        html += '</div>';

        html += '<div class="newCollectionInfo">';

        html += '<div>';
        html += '<paper-input type="text" id="txtNewCollectionName" required="required" label="' + Globalize.translate('LabelName') + '"></paper-input>';
        html += '<div class="fieldDescription">' + Globalize.translate('NewCollectionNameExample') + '</div>';
        html += '</div>';

        html += '<br />';
        html += '<br />';

        html += '<div>';
        html += '<paper-checkbox id="chkEnableInternetMetadata">' + Globalize.translate('OptionSearchForInternetMetadata') + '</paper-checkbox>';
        html += '</div>';

        // newCollectionInfo
        html += '</div>';

        html += '<br />';
        html += '<div>';
        html += '<button type="submit" class="clearButton" data-role="none"><paper-button raised class="submit block">' + Globalize.translate('ButtonOk') + '</paper-button></button>';
        html += '</div>';

        html += '<input type="hidden" class="fldSelectedItemIds" />';

        html += '</form>';

        return html;
    }

    function initEditor(content, items) {

        $('#selectCollectionToAddTo', content).on('change', function () {

            if (this.value) {
                $('.newCollectionInfo', content).hide();
                $('#txtNewCollectionName', content).removeAttr('required');
            } else {
                $('.newCollectionInfo', content).show();
                $('#txtNewCollectionName', content).attr('required', 'required');
            }
        });

        $('.newCollectionForm', content).off('submit', onSubmit).on('submit', onSubmit);

        $('.fldSelectedItemIds', content).val(items.join(','));

        if (items.length) {
            $('.fldSelectCollection', content).show();
            populateCollections(content);
        } else {
            $('.fldSelectCollection', content).hide();
            $('#selectCollectionToAddTo', content).html('').val('').trigger('change');
        }
    }

    function collectioneditor() {

        var self = this;

        self.show = function (items) {

            items = items || [];

            var dlg = dialogHelper.createDialog({
                size: 'small'
            });

            dlg.classList.add('ui-body-b');
            dlg.classList.add('background-theme-b');

            var html = '';
            var title = items.length ? Globalize.translate('HeaderAddToCollection') : Globalize.translate('HeaderNewCollection');

            html += '<div class="dialogHeader">';
            html += '<paper-icon-button icon="arrow-back" class="btnCancel" tabindex="-1"></paper-icon-button>';
            html += '<div class="dialogHeaderTitle">';
            html += title;
            html += '</div>';
            html += '</div>';

            html += getEditorHtml();

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            initEditor(dlg, items);

            $(dlg).on('close', onDialogClosed);

            dialogHelper.open(dlg);

            $('.btnCancel', dlg).on('click', function () {

                dialogHelper.close(dlg);
            });
        };
    }

    return collectioneditor;
});