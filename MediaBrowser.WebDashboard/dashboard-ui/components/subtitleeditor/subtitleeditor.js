define(['dialogHelper', 'appStorage', 'jQuery', 'paper-fab', 'paper-item-body', 'paper-icon-item', 'paper-icon-button-light'], function (dialogHelper, appStorage, $) {

    var currentItem;

    function showLocalSubtitles(page, index) {

        Dashboard.showLoadingMsg();

        var popup = $('.popupSubtitleViewer', page).popup('open');
        $('.subtitleContent', page).html('');

        var url = 'Videos/' + currentItem.Id + '/Subtitles/' + index;

        ApiClient.ajax({

            type: 'GET',
            url: url

        }).then(function (result) {

            $('.subtitleContent', page).html(result);

            Dashboard.hideLoadingMsg();

            popup.popup('reposition', {});
        });
    }

    function showRemoteSubtitles(page, id) {

        Dashboard.showLoadingMsg();

        var popup = $('.popupSubtitleViewer', page).popup('open');
        $('.subtitleContent', page).html('');

        var url = 'Providers/Subtitles/Subtitles/' + id;

        ApiClient.get(ApiClient.getUrl(url)).then(function (result) {

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

        }).then(function () {

            require(['toast'], function (toast) {
                toast(Globalize.translate('MessageDownloadQueued'));
            });
        });
    }

    function deleteLocalSubtitle(page, index) {

        var msg = Globalize.translate('MessageAreYouSureDeleteSubtitles');

        require(['confirm'], function (confirm) {

            confirm(msg, Globalize.translate('HeaderConfirmDeletion')).then(function () {

                Dashboard.showLoadingMsg();

                var itemId = currentItem.Id;
                var url = 'Videos/' + itemId + '/Subtitles/' + index;

                ApiClient.ajax({

                    type: "DELETE",
                    url: ApiClient.getUrl(url)

                }).then(function () {

                    reload(page, itemId);
                });
            });
        });
    }

    function fillSubtitleList(page, item) {

        var streams = item.MediaStreams || [];

        var subs = streams.filter(function (s) {

            return s.Type == 'Subtitle';
        });

        var html = '';

        if (subs.length) {

            html += '<h1 style="margin-top:1.5em;">' + Globalize.translate('HeaderCurrentSubtitles') + '</h1>';
            html += '<div class="paperList">';

            html += subs.map(function (s) {

                var itemHtml = '';

                itemHtml += '<paper-icon-item>';

                itemHtml += '<paper-fab mini class="blue" icon="closed-caption" item-icon></paper-fab>';

                var atts = [];

                atts.push(s.Codec);
                if (s.IsDefault) {

                    atts.push('Default');
                }
                if (s.IsForced) {

                    atts.push('Forced');
                }

                if (atts.length == 3) {
                    itemHtml += '<paper-item-body three-line>';
                }
                else {
                    itemHtml += '<paper-item-body two-line>';
                }

                itemHtml += '<div>';
                itemHtml += (s.Language || Globalize.translate('LabelUnknownLanaguage'));
                itemHtml += '</div>';

                itemHtml += '<div secondary>' + atts.join(' - ') + '</div>';

                if (s.Path) {
                    itemHtml += '<div secondary>' + (s.Path) + '</div>';
                }

                html += '</a>';
                itemHtml += '</paper-item-body>';

                if (s.Path) {
                    itemHtml += '<button is="paper-icon-button-light" data-index="' + s.Index + '" title="' + Globalize.translate('Delete') + '" class="btnDelete"><iron-icon icon="delete"></iron-icon></button>';
                }

                itemHtml += '</paper-icon-item>';

                return itemHtml;

            }).join('');

            html += '</div>';
        }

        var elem = $('.subtitleList', page).html(html);

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

        var lastLanguage = appStorage.getItem('subtitleeditor-language');
        if (lastLanguage) {
            $('#selectLanguage', page).val(lastLanguage);
        }
        else {

            Dashboard.getCurrentUser().then(function (user) {

                var lang = user.Configuration.SubtitleLanguagePreference;

                if (lang) {
                    $('#selectLanguage', page).val(lang);
                }
            });
        }
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

        for (var i = 0, length = results.length; i < length; i++) {

            var result = results[i];

            var provider = result.ProviderName;

            if (provider != lastProvider) {

                if (i > 0) {
                    html += '</div>';
                }
                html += '<h1>' + provider + '</h1>';
                html += '<div class="paperList">';
                lastProvider = provider;
            }

            html += '<paper-icon-item>';

            html += '<paper-fab mini class="blue" icon="closed-caption" item-icon></paper-fab>';

            if (result.Comment) {
                html += '<paper-item-body three-line>';
            }
            else {
                html += '<paper-item-body two-line>';
            }

            //html += '<a class="btnViewSubtitle" href="#" data-subid="' + result.Id + '">';

            html += '<div>' + (result.Name) + '</div>';
            html += '<div secondary>' + (result.Format) + '</div>';

            if (result.Comment) {
                html += '<div secondary>' + (result.Comment) + '</div>';
            }

            //html += '</a>';

            html += '</paper-item-body>';

            html += '<div style="font-size:86%;opacity:.7;">' + /*(result.CommunityRating || 0) + ' / ' +*/ (result.DownloadCount || 0) + '</div>';

            html += '<button type="button" is="paper-icon-button-light" data-subid="' + result.Id + '" title="' + Globalize.translate('ButtonDownload') + '" class="btnDownload"><iron-icon icon="cloud-download"></iron-icon></button>';

            html += '</paper-icon-item>';
        }

        if (results.length) {
            html += '</div>';
        }

        var elem = $('.subtitleResults', page).html(html);

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

        appStorage.setItem('subtitleeditor-language', language);

        Dashboard.showLoadingMsg();

        var url = ApiClient.getUrl('Items/' + currentItem.Id + '/RemoteSearch/Subtitles/' + language);

        ApiClient.getJSON(url).then(function (results) {

            renderSearchResults(page, results);
        });
    }

    function reload(context, itemId) {

        $('.noSearchResults', context).hide();

        function onGetItem(item) {
            currentItem = item;

            fillSubtitleList(context, item);
            var file = item.Path || '';
            var index = Math.max(file.lastIndexOf('/'), file.lastIndexOf('\\'));
            if (index > -1) {
                file = file.substring(index + 1);
            }

            if (file) {
                context.querySelector('.pathValue').innerHTML = file;
                context.querySelector('.originalFile').classList.remove('hide');
            } else {
                context.querySelector('.pathValue').innerHTML = '';
                context.querySelector('.originalFile').classList.add('hide');
            }

            Dashboard.hideLoadingMsg();
        }

        if (typeof itemId == 'string') {
            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).then(onGetItem);
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

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/subtitleeditor/subtitleeditor.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;
            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).then(function (item) {

                var dlg = dialogHelper.createDialog({
                    size: 'small',
                    removeOnClose: true
                });

                dlg.classList.add('ui-body-b');
                dlg.classList.add('background-theme-b');

                var html = '';
                html += '<div class="dialogHeader" style="margin:0 0 2em;">';
                html += '<button is="paper-icon-button-light" class="btnCancel" tabindex="-1"><iron-icon icon="arrow-back"></iron-icon></button>';
                html += '<div class="dialogHeaderTitle">';
                html += item.Name;
                html += '</div>';
                html += '</div>';

                html += '<div class="editorContent">';
                html += Globalize.translateDocument(template);
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                dlg.querySelector('.pathLabel').innerHTML = Globalize.translate('MediaInfoFile');

                $('.subtitleSearchForm', dlg).off('submit', onSearchSubmit).on('submit', onSearchSubmit);

                dialogHelper.open(dlg);

                var editorContent = dlg.querySelector('.editorContent');
                reload(editorContent, item);

                ApiClient.getCultures().then(function (languages) {

                    fillLanguages(editorContent, languages);
                });

                $('.btnCancel', dlg).on('click', function () {

                    dialogHelper.close(dlg);
                });
            });
        }

        xhr.send();
    }

    return {
        show: showEditor
    };
});