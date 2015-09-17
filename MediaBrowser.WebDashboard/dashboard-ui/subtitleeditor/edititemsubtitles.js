(function ($, window, document) {

    var currentItem;
    var currentDialog;

    function showLocalSubtitles(page, index) {

        Dashboard.showLoadingMsg();

        var popup = $('.popupSubtitleViewer', page).popup('open');
        $('.subtitleContent', page).html('');

        var url = 'Videos/' + currentItem.Id + '/Subtitles/' + index;

        $.get(ApiClient.getUrl(url)).done(function (result) {

            $('.subtitleContent', page).html(result);

            Dashboard.hideLoadingMsg();

            popup.popup('reposition', {});
        });
    }

    function showRemoteSubtitles(page, id) {

        Dashboard.showLoadingMsg();

        var popup = $('.popupSubtitleViewer', page).popup('open');
        $('.subtitleContent', page).html('\nLoading...\n\n\n');

        var url = 'Providers/Subtitles/Subtitles/' + id;

        ApiClient.get(ApiClient.getUrl(url)).done(function (result) {

            $('.subtitleContent', page).html(result);

            Dashboard.hideLoadingMsg();

            popup.popup('reposition', {});
        });
    }

    function downloadRemoteSubtitles(page, id) {

        var url = 'Items/' + currentItem.Id + '/RemoteSearch/Subtitles/' + id;

        ApiClient.ajax({

            type: "POST",
            url: ApiClient.getUrl(url)

        }).done(function () {

            Dashboard.alert(Globalize.translate('MessageDownloadQueued'));
        });
    }

    function deleteLocalSubtitle(page, index) {

        var msg = Globalize.translate('MessageAreYouSureDeleteSubtitles');

        Dashboard.confirm(msg, Globalize.translate('HeaderConfirmDeletion'), function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                var itemId = currentItem.Id;
                var url = 'Videos/' + itemId + '/Subtitles/' + index;

                ApiClient.ajax({

                    type: "DELETE",
                    url: ApiClient.getUrl(url)

                }).done(function () {

                    reload(page, itemId);
                });

            }
        });
    }

    function fillSubtitleList(page, item) {

        var streams = item.MediaStreams || [];

        var subs = streams.filter(function (s) {

            return s.Type == 'Subtitle';
        });

        var html = '';

        if (subs.length) {

            html += '<h1>' + Globalize.translate('HeaderCurrentSubtitles') + '</h1>';
            html += '<div class="paperList">';

            html += subs.map(function (s) {

                var itemHtml = '';

                itemHtml += '<paper-icon-item>';

                itemHtml += '<paper-fab class="listAvatar blue" icon="closed-caption" item-icon></paper-fab>';

                itemHtml += '<paper-item-body three-line>';

                itemHtml += '<div>';
                itemHtml += (s.Language || Globalize.translate('LabelUnknownLanaguage'));
                itemHtml += '</div>';

                var atts = [];

                atts.push(s.Codec);
                if (s.IsDefault) {

                    atts.push('Default');
                }
                if (s.IsForced) {

                    atts.push('Forced');
                }

                itemHtml += '<div secondary>' + atts.join(', ') + '</div>';

                if (s.Path) {
                    itemHtml += '<div secondary>' + (s.Path) + '</div>';
                }

                html += '</a>';
                itemHtml += '</paper-item-body>';

                if (s.Path) {
                    itemHtml += '<paper-icon-button icon="delete" data-index="' + s.Index + '" title="' + Globalize.translate('Delete') + '" class="btnDelete"></paper-icon-button>';
                }

                itemHtml += '</paper-icon-item>';

                return itemHtml;

            }).join('');

            html += '</div>';
        }

        var elem = $('.subtitleList', page).html(html).trigger('create');

        $('.btnViewSubtitles', elem).on('click', function () {

            var index = this.getAttribute('data-index');

            showLocalSubtitles(page, index);

        });

        $('.btnDelete', elem).on('click', function () {

            var index = this.getAttribute('data-index');

            deleteLocalSubtitle(page, index);

        });
    }

    function fillLanguages(page, languages) {

        $('#selectLanguage', page).html(languages.map(function (l) {

            return '<option value="' + l.ThreeLetterISOLanguageName + '">' + l.DisplayName + '</option>';

        }));

        Dashboard.getCurrentUser().done(function (user) {

            var lang = user.Configuration.SubtitleLanguagePreference;

            if (lang) {
                $('#selectLanguage', page).val(lang);
            }
        });
    }

    function renderSearchResults(page, results) {

        var lastProvider = '';
        var html = '';

        if (!results.length) {

            $('.noSearchResults', page).show();
            $('.subtitleResults', page).html('');
            Dashboard.hideLoadingMsg();
            return;
        }

        $('.noSearchResults', page).hide();

        html += '<ul data-role="listview">';

        for (var i = 0, length = results.length; i < length; i++) {

            var result = results[i];

            var provider = result.ProviderName;

            if (provider != lastProvider) {
                html += '<li data-role="list-divider">' + provider + '<span class="ui-li-count ui-body-inherit">' + Globalize.translate('HeaderRatingsDownloads') + '</span></li>';
                lastProvider = provider;
            }

            html += '<li><a class="btnViewSubtitle" href="#" data-subid="' + result.Id + '">';

            html += '<h3>' + (result.Name) + '</h3>';
            html += '<p>' + (result.Format) + '</p>';

            if (result.Comment) {
                html += '<p>' + (result.Comment) + '</p>';
            }

            html += '<div class="ui-li-count">' + (result.CommunityRating || 0) + ' / ' + (result.DownloadCount || 0) + '</div>';

            html += '</a>';

            html += '<a href="#" class="btnDownload" data-icon="plus" data-subid="' + result.Id + '">' + Globalize.translate('ButtonDownload') + '</a>';

            html += '</li>';
        }

        html += '</ul>';
        var elem = $('.subtitleResults', page).html(html).trigger('create');

        $('.btnViewSubtitle', elem).on('click', function () {

            var id = this.getAttribute('data-subid');
            showRemoteSubtitles(page, id);
        });

        $('.btnDownload', elem).on('click', function () {

            var id = this.getAttribute('data-subid');
            downloadRemoteSubtitles(page, id);
        });

        Dashboard.hideLoadingMsg();
    }

    function searchForSubtitles(page, language) {

        Dashboard.showLoadingMsg();

        var url = ApiClient.getUrl('Items/' + currentItem.Id + '/RemoteSearch/Subtitles/' + language);

        ApiClient.getJSON(url).done(function (results) {

            renderSearchResults(page, results);
        });
    }

    function reload(page, itemId) {

        $('.noSearchResults', page).hide();

        function onGetItem(item) {
            currentItem = item;

            fillSubtitleList(page, item);

            Dashboard.hideLoadingMsg();
        }

        if (typeof itemId == 'string') {
            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).done(onGetItem);
        }
        else {
            onGetItem(itemId);
        }
    }

    function onSearchSubmit() {
        var form = this;

        var lang = $('#selectLanguage', form).val();

        searchForSubtitles($(form).parents('.editorContent'), lang);

        return false;
    }

    function showEditor(itemId) {

        Dashboard.showLoadingMsg();

        ApiClient.ajax({

            type: 'GET',
            url: 'subtitleeditor/subtitleeditor.template.html'

        }).done(function (template) {

            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).done(function (item) {

                var dlg = document.createElement('paper-dialog');

                dlg.entryAnimation = 'scale-up-animation';
                dlg.exitAnimation = 'fade-out-animation';
                dlg.classList.add('fullscreen-editor-paper-dialog');
                dlg.classList.add('ui-body-b');

                var html = '';
                html += '<h2 class="dialogHeader">' + item.Name + '</h2>';

                html += '<div class="editorContent">';
                html += Globalize.translateDocument(template);
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                $('.subtitleSearchForm', dlg).off('submit', onSearchSubmit).on('submit', onSearchSubmit);

                // Has to be assigned a z-index after the call to .open() 
                $(dlg).on('iron-overlay-closed', onDialogClosed);

                document.body.classList.add('bodyWithPopupOpen');
                dlg.open();

                window.location.hash = 'subtitleeditor?id=' + itemId;

                // We need to use a timeout or onHashChange will fire immediately while opening
                setTimeout(function () {

                    window.addEventListener('hashchange', onHashChange);

                    currentDialog = dlg;

                    var editorContent = dlg.querySelector('.editorContent');
                    reload(editorContent, item);

                    fillLanguages(editorContent);

                }, 0);
            });
        });
    }

    function onHashChange() {

        if (currentDialog) {
            closeDialog(false);
        }
    }

    function closeDialog(updateHash) {

        window.removeEventListener('hashchange', onHashChange);

        if (updateHash) {
            window.location.hash = '';
        }

        if (currentDialog) {
            currentDialog.close();
        }
    }

    function onDialogClosed() {
        currentDialog = null;
        window.removeEventListener('hashchange', onHashChange);
        document.body.classList.remove('bodyWithPopupOpen');
        $(this).remove();
    }

    function fillLanguages(editorContent) {
        ApiClient.getCultures().done(function (languages) {

            fillLanguages(editorContent, languages);
        });
    }

    window.SubtitleEditor = {
        show: showEditor
    };

})(jQuery, window, document);