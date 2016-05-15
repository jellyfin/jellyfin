define(['dialogHelper', 'jQuery', 'paper-input', 'paper-icon-button-light'], function (dialogHelper, $) {

    var lastPlaylistId = '';

    function redirectToPlaylist(id) {

        var context = getParameterByName('context');

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).then(function (item) {

            Dashboard.navigate(LibraryBrowser.getHref(item, context));

        });
    }

    function onAddToPlaylistFormSubmit() {

        Dashboard.showLoadingMsg();

        var panel = $(this).parents('.dialog')[0];

        var playlistId = $('#selectPlaylistToAddTo', panel).val();

        if (playlistId) {
            lastPlaylistId = playlistId;
            addToPlaylist(panel, playlistId);
        } else {
            createPlaylist(panel);
        }

        return false;
    }

    function createPlaylist(dlg) {

        var url = ApiClient.getUrl("Playlists", {

            Name: $('#txtNewPlaylistName', dlg).val(),
            Ids: $('.fldSelectedItemIds', dlg).val() || '',
            userId: Dashboard.getCurrentUserId()

        });

        ApiClient.ajax({
            type: "POST",
            url: url,
            dataType: "json"

        }).then(function (result) {

            Dashboard.hideLoadingMsg();

            var id = result.Id;

            dialogHelper.close(dlg);
            redirectToPlaylist(id);
        });
    }

    function addToPlaylist(dlg, id) {

        var url = ApiClient.getUrl("Playlists/" + id + "/Items", {

            Ids: $('.fldSelectedItemIds', dlg).val() || '',
            userId: Dashboard.getCurrentUserId()
        });

        ApiClient.ajax({
            type: "POST",
            url: url

        }).then(function () {

            Dashboard.hideLoadingMsg();

            dialogHelper.close(dlg);
            require(['toast'], function (toast) {
                toast(Globalize.translate('MessageAddedToPlaylistSuccess'));
            });

        });
    }

    function onDialogClosed() {

        $(this).remove();
        Dashboard.hideLoadingMsg();
    }

    function populatePlaylists(panel) {

        var select = $('#selectPlaylistToAddTo', panel);

        if (!select.length) {

            $('#txtNewPlaylistName', panel).val('').focus();
            return;
        }

        Dashboard.showLoadingMsg();

        $('.newPlaylistInfo', panel).hide();

        var options = {

            Recursive: true,
            IncludeItemTypes: "Playlist",
            SortBy: 'SortName'
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            var html = '';

            html += '<option value="">' + Globalize.translate('OptionNewPlaylist') + '</option>';

            html += result.Items.map(function (i) {

                return '<option value="' + i.Id + '">' + i.Name + '</option>';
            });

            select.html(html).val(lastPlaylistId || '').trigger('change');

            Dashboard.hideLoadingMsg();
        });
    }

    function getEditorHtml() {

        var html = '';

        html += '<form style="margin:auto;">';

        html += '<div class="fldSelectPlaylist">';
        html += '<label for="selectPlaylistToAddTo" class="selectLabel">' + Globalize.translate('LabelSelectPlaylist') + '</label>';
        html += '<select id="selectPlaylistToAddTo" data-mini="true"></select>';
        html += '</div>';

        html += '<div class="newPlaylistInfo">';

        html += '<div>';
        html += '<paper-input type="text" id="txtNewPlaylistName" required="required" label="' + Globalize.translate('LabelName') + '"></paper-input>';
        html += '</div>';

        html += '<br />';

        // newPlaylistInfo
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

        $('#selectPlaylistToAddTo', content).on('change', function () {

            if (this.value) {
                $('.newPlaylistInfo', content).hide();
                $('input', content).removeAttr('required');
            } else {
                $('.newPlaylistInfo', content).show();
                $('input', content).attr('required', 'required');
            }

        }).trigger('change');

        populatePlaylists(content);

        $('form', content).on('submit', onAddToPlaylistFormSubmit);

        $('.fldSelectedItemIds', content).val(items.join(','));

        if (items.length) {
            $('.fldSelectPlaylist', content).show();
            populatePlaylists(content);
        } else {
            $('.fldSelectPlaylist', content).hide();
            $('#selectPlaylistToAddTo', content).html('').val('').trigger('change');
        }
    }

    function playlisteditor() {

        var self = this;

        self.show = function (items) {

            items = items || [];

            var dlg = dialogHelper.createDialog({
                size: 'small'
            });

            dlg.classList.add('ui-body-b');
            dlg.classList.add('background-theme-b');

            var html = '';

            var title = Globalize.translate('HeaderAddToPlaylist');

            html += '<div class="dialogHeader" style="margin:0 0 2em;">';
            html += '<button is="paper-icon-button-light" class="btnCancel" tabindex="-1"><iron-icon icon="arrow-back"></iron-icon></button>';
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

    return playlisteditor;
});