(function ($, document) {

    var lastPlaylistId = '';

    function redirectToPlaylist(id) {

        var context = getParameterByName('context');

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            Dashboard.navigate(LibraryBrowser.getHref(item, context));

        });
    }

    function onAddToPlaylistFormSubmit() {

        Dashboard.showLoadingMsg();

        var panel = $(this).parents('.newPlaylistPanel');

        var playlistId = $('select.selectPlaylistToAddTo', panel).val();

        if (playlistId) {
            lastPlaylistId = playlistId;
            addToPlaylist(panel, playlistId);
        } else {
            createPlaylist(panel);
        }

        return false;
    }

    function getNewPlaylistPanel() {

        Dashboard.showLoadingMsg();
        $('.newPlaylistPanel').panel('destroy').remove();

        var html = '<div data-role="panel" data-position="right" data-display="overlay" class="newPlaylistPanel" data-position-fixed="true" data-theme="a">';

        html += '<h3>' + Globalize.translate('HeaderAddToPlaylist') + '</h3>';

        html += '<br />';

        html += '<form class="addToPlaylistForm">';

        var selectId = 'selectPlaylistToAddTo' + new Date().getTime();

        html += '<div>';
        html += '<label for="' + selectId + '">' + Globalize.translate('LabelSelectPlaylist') + '</label>';
        html += '<select id="' + selectId + '" class="selectPlaylistToAddTo" data-mini="true">';
        html += '</select>';
        html += '</div>';

        html += '<br />';

        html += '<div class="fldNewPlaylist" style="display:none;">';
        html += '<label for="txtNewPlaylistName">' + Globalize.translate('LabelName') + '</label>';
        html += '<input type="text" id="txtNewPlaylistName" />';
        html += '</div>';

        html += '<p>';
        html += '<input class="fldSelectedItemIds" type="hidden" />';
        html += '<button type="submit" data-icon="plus" data-mini="true" data-theme="b">' + Globalize.translate('ButtonSubmit') + '</button>';
        html += '</p>';

        html += '</form>';
        html += '</div>';

        $(document.body).append(html);

        var elem = $('.newPlaylistPanel').panel({}).trigger('create').on("panelclose", function () {

            $(this).off("panelclose").remove();
        });

        var select = $('#' + selectId, elem).on('change', function () {

            if (this.value) {
                $('.fldNewPlaylist', elem).hide();
                $('input', elem).removeAttr('required');
            } else {
                $('.fldNewPlaylist', elem).show();
                $('input', elem).attr('required', 'required');
            }

        }).trigger('change');

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            IncludeItemTypes: 'Playlist',
            recursive: true,
            SortBy: 'SortName'

        }).done(function (result) {

            var selectHtml = '<option value="">' + Globalize.translate('OptionNewPlaylist') + '</option>';
            selectHtml += result.Items.map(function (o) {

                return '<option value="' + o.Id + '">' + o.Name + '</option>';

            }).join('');

            select.html(selectHtml).selectmenu('refresh');

            select.val(lastPlaylistId || '').selectmenu('refresh').trigger('change');
            Dashboard.hideLoadingMsg();
        });

        $('form', elem).on('submit', onAddToPlaylistFormSubmit);

        return elem;
    }

    function showNewPlaylistPanel(items) {

        var panel = getNewPlaylistPanel().panel('toggle');

        $('.fldSelectedItemIds', panel).val(items.join(','));

        populatePlaylists(panel);
    }

    function populatePlaylists(panel) {

        var select = $('select.selectPlaylistToAddTo', panel);

        if (!select.length) {

            $('#txtNewPlaylistName', panel).val('').focus();
            return;
        }

        $('.newPlaylistInfo', panel).hide();

        var options = {

            Recursive: true,
            IncludeItemTypes: "Playlist"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            var html = '';

            html += '<option value="">' + Globalize.translate('OptionNewPlaylist') + '</option>';

            html += result.Items.map(function (i) {

                return '<option value="' + i.Id + '">' + i.Name + '</option>';
            });

            select.html(html).val('').selectmenu('refresh').trigger('change');

        });
    }

    //$(document).on('pageinit', ".playlistEditorPage", function () {

    //    var page = this;

    //    $('.itemsContainer', page).on('itemsrendered', function () {

    //        $('.btnNewPlaylist', page).off('click.newplaylistpanel').on('click.newplaylistpanel', function () {

    //            showNewPlaylistPanel(page, []);
    //        });

    //    });

    //    $('#selectPlaylistToAddTo', page).on('change', function () {

    //        if (this.value) {
    //            $('.newPlaylistInfo', page).hide();
    //            $('#txtNewPlaylistName', page).removeAttr('required');
    //        } else {
    //            $('.newPlaylistInfo', page).show();
    //            $('#txtNewPlaylistName', page).attr('required', 'required');
    //        }
    //    });
    //});

    function createPlaylist(panel) {

        var url = ApiClient.getUrl("Playlists", {

            Name: $('#txtNewPlaylistName', panel).val(),
            Ids: $('.fldSelectedItemIds', panel).val() || '',
            userId: Dashboard.getCurrentUserId()

        });

        ApiClient.ajax({
            type: "POST",
            url: url,
            dataType: "json"

        }).done(function (result) {

            Dashboard.hideLoadingMsg();

            var id = result.Id;

            panel.panel('toggle');
            redirectToPlaylist(id);
        });
    }

    function addToPlaylist(panel, id) {

        var url = ApiClient.getUrl("Playlists/" + id + "/Items", {

            Ids: $('.fldSelectedItemIds', panel).val() || '',
            userId: Dashboard.getCurrentUserId()
        });

        ApiClient.ajax({
            type: "POST",
            url: url

        }).done(function () {

            Dashboard.hideLoadingMsg();

            panel.panel('toggle');
            Dashboard.alert(Globalize.translate('MessageAddedToPlaylistSuccess'));

        });
    }

    window.PlaylistManager = {

        showPanel: function (items) {
            showNewPlaylistPanel(items);
        },

        supportsPlaylists: function (item) {

            return item.SupportsPlaylists;
        }
    };

})(jQuery, document);