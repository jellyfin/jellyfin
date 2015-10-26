define([], function () {

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var panel = $(this).parents('paper-dialog')[0];

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

        }).done(function (result) {

            Dashboard.hideLoadingMsg();

            var id = result.Id;

            PaperDialogHelper.close(dlg);
            redirectToCollection(id);

        });
    }

    function redirectToCollection(id) {

        var context = getParameterByName('context');

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

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

        }).done(function () {

            Dashboard.hideLoadingMsg();

            PaperDialogHelper.close(dlg);

            Dashboard.alert(Globalize.translate('MessageItemsAdded'));
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

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

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

        html += '<form class="newCollectionForm" style="max-width:100%;">';

        html += '<br />';

        html += '<div class="fldSelectCollection">';
        html += '<br />';
        html += '<label for="selectCollectionToAddTo">' + Globalize.translate('LabelSelectCollection') + '</label>';
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

            require(['components/paperdialoghelper'], function () {

                var dlg = PaperDialogHelper.createDialog({
                    size: 'small'
                });

                var html = '';
                html += '<h2 class="dialogHeader">';
                html += '<paper-fab icon="arrow-back" mini class="btnCloseDialog"></paper-fab>';

                var title = items.length ? Globalize.translate('HeaderAddToCollection') : Globalize.translate('HeaderNewCollection');

                html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + title + '</div>';
                html += '</h2>';

                html += '<div class="editorContent" style="max-width:800px;margin:auto;">';
                html += getEditorHtml();
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                var editorContent = dlg.querySelector('.editorContent');
                initEditor(editorContent, items);

                $(dlg).on('iron-overlay-closed', onDialogClosed);

                PaperDialogHelper.openWithHash(dlg, 'collectioneditor');

                $('.btnCloseDialog', dlg).on('click', function () {

                    PaperDialogHelper.close(dlg);
                });
            });
        };
    }

    return collectioneditor;
});