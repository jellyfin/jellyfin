(function ($, document) {

    function getNewCollectionPanel(createIfNeeded) {

        var panel = $('.newCollectionPanel');

        if (createIfNeeded && !panel.length) {

            var html = '';

            html += '<div>';
            html += '<div data-role="panel" class="newCollectionPanel" data-position="right" data-display="overlay" data-position-fixed="true" data-theme="a">';
            html += '<form class="newCollectionForm">';

            html += '<h3>' + Globalize.translate('HeaderAddToCollection') + '</h3>';

            html += '<div class="fldSelectCollection">';
            html += '<br />';
            html += '<label for="selectCollectionToAddTo">' + Globalize.translate('LabelSelectCollection') + '</label>';
            html += '<select id="selectCollectionToAddTo" data-mini="true"></select>';
            html += '</div>';

            html += '<div class="newCollectionInfo">';
            html += '<br />';

            html += '<div>';
            html += '<label for="txtNewCollectionName">' + Globalize.translate('LabelName') + '</label>';
            html += '<input type="text" id="txtNewCollectionName" required="required" />';
            html += '<div class="fieldDescription">' + Globalize.translate('NewCollectionNameExample') + '</div>';
            html += '</div>';

            html += '<br />';

            html += '<div>';
            html += '<label for="chkEnableInternetMetadata">' + Globalize.translate('OptionSearchForInternetMetadata') + '</label>';
            html += '<input type="checkbox" id="chkEnableInternetMetadata" data-mini="true" />';
            html += '</div>';

            // newCollectionInfo
            html += '</div>';

            html += '<br />';
            html += '<p>';
            html += '<input class="fldSelectedItemIds" type="hidden" />';
            html += '<button type="submit" data-icon="plus" data-mini="true" data-theme="b">' + Globalize.translate('ButtonSubmit') + '</button>';
            html += '</p>';

            html += '</form>';
            html += '</div>';
            html += '</div>';

            panel = $(html).appendTo(document.body).trigger('create').find('.newCollectionPanel');

            $('#selectCollectionToAddTo', panel).on('change', function () {

                if (this.value) {
                    $('.newCollectionInfo', panel).hide();
                    $('#txtNewCollectionName', panel).removeAttr('required');
                } else {
                    $('.newCollectionInfo', panel).show();
                    $('#txtNewCollectionName', panel).attr('required', 'required');
                }
            });

            $('.newCollectionForm', panel).off('submit', onSubmit).on('submit', onSubmit);
        }
        return panel;
    }

    function showCollectionPanel(items) {

        var panel = getNewCollectionPanel(true).panel('toggle');

        $('.fldSelectedItemIds', panel).val(items.join(','));


        require(['jqmicons']);

        if (items.length) {
            $('.fldSelectCollection', panel).show();
            populateCollections(panel);
        } else {
            $('.fldSelectCollection', panel).hide();
            $('#selectCollectionToAddTo', panel).html('').val('').trigger('change');
        }
    }

    function populateCollections(panel) {

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

        });
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var panel = getNewCollectionPanel(false);

        var collectionId = $('#selectCollectionToAddTo', panel).val();

        if (collectionId) {
            addToCollection(panel, collectionId);
        } else {
            createCollection(panel);
        }

        return false;
    }

    $(document).on('pageinit', ".collectionEditorPage", function () {

        var page = this;

        // The button is created dynamically
        $(page).on('click', '.btnNewCollection', function () {

            BoxSetEditor.showPanel([]);
        });
    });

    function redirectToCollection(id) {

        var context = getParameterByName('context');

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            Dashboard.navigate(LibraryBrowser.getHref(item, context));

        });
    }

    function createCollection(panel) {

        var url = ApiClient.getUrl("Collections", {

            Name: $('#txtNewCollectionName', panel).val(),
            IsLocked: !$('#chkEnableInternetMetadata', panel).checked(),
            Ids: $('.fldSelectedItemIds', panel).val() || ''

            //ParentId: getParameterByName('parentId') || LibraryMenu.getTopParentId()

        });

        ApiClient.ajax({
            type: "POST",
            url: url,
            dataType: "json"

        }).done(function (result) {

            Dashboard.hideLoadingMsg();

            var id = result.Id;

            panel.panel('toggle');
            redirectToCollection(id);

        });
    }

    function addToCollection(panel, id) {

        var url = ApiClient.getUrl("Collections/" + id + "/Items", {

            Ids: $('.fldSelectedItemIds', panel).val() || ''
        });

        ApiClient.ajax({
            type: "POST",
            url: url

        }).done(function () {

            Dashboard.hideLoadingMsg();

            panel.panel('toggle');

            Dashboard.alert(Globalize.translate('MessageItemsAdded'));
        });
    }

    window.BoxSetEditor = {

        showPanel: function (items) {
            require(['jqmpanel'], function () {
                showCollectionPanel(items);
            });
        },

        supportsAddingToCollection: function (item) {

            var invalidTypes = ['Person', 'Genre', 'MusicGenre', 'Studio', 'GameGenre', 'BoxSet', 'Playlist', 'UserView', 'CollectionFolder', 'Audio', 'Episode'];

            return item.LocationType == 'FileSystem' && !item.CollectionType && invalidTypes.indexOf(item.Type) == -1 && item.MediaType != 'Photo';
        }
    };

})(jQuery, document);