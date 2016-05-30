define(['dialogHelper', 'require', 'layoutManager', 'globalize', 'appStorage', 'connectionManager', 'loading', 'paper-fab', 'listViewStyle', 'paper-icon-button-light', 'css!./../formdialog'], function (dialogHelper, require, layoutManager, globalize, appStorage, connectionManager, loading) {

    var currentItem;

    function showLocalSubtitles(context, index) {

        loading.show();

        var subtitleContent = context.querySelector('.subtitleContent');
        subtitleContent.innerHTML = '';

        var apiClient = connectionManager.getApiClient(currentItem.ServerId);
        var url = 'Videos/' + currentItem.Id + '/Subtitles/' + index;

        apiClient.ajax({

            type: 'GET',
            url: url

        }).then(function (result) {

            subtitleContent.innerHTML = result;

            loading.hide();
        });
    }

    function showRemoteSubtitles(context, id) {

        loading.show();

        var url = 'Providers/Subtitles/Subtitles/' + id;

        ApiClient.get(ApiClient.getUrl(url)).then(function (result) {

            // show result

            loading.hide();
        });
    }

    function downloadRemoteSubtitles(context, id) {

        var url = 'Items/' + currentItem.Id + '/RemoteSearch/Subtitles/' + id;

        var apiClient = connectionManager.getApiClient(currentItem.ServerId);
        apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl(url)

        }).then(function () {

            require(['toast'], function (toast) {
                toast(globalize.translate('MessageDownloadQueued'));
            });
        });
    }

    function deleteLocalSubtitle(context, index) {

        var msg = globalize.translate('MessageAreYouSureDeleteSubtitles');

        require(['confirm'], function (confirm) {

            confirm(msg, globalize.translate('HeaderConfirmDeletion')).then(function () {

                loading.show();

                var itemId = currentItem.Id;
                var url = 'Videos/' + itemId + '/Subtitles/' + index;

                var apiClient = connectionManager.getApiClient(currentItem.ServerId);

                apiClient.ajax({

                    type: "DELETE",
                    url: apiClient.getUrl(url)

                }).then(function () {

                    reload(context, apiClient, itemId);
                });
            });
        });
    }

    function fillSubtitleList(context, item) {

        var streams = item.MediaStreams || [];

        var subs = streams.filter(function (s) {

            return s.Type == 'Subtitle';
        });

        var html = '';

        if (subs.length) {

            html += '<h1 style="margin-top:1.5em;">' + globalize.translate('HeaderCurrentSubtitles') + '</h1>';

            if (layoutManager.tv) {
                html += '<div>';
            } else {
                html += '<div class="paperList">';
            }

            html += subs.map(function (s) {

                var itemHtml = '';

                itemHtml += '<div class="listItem">';

                itemHtml += '<paper-fab mini class="blue" icon="closed-caption" item-icon></paper-fab>';

                var atts = [];

                atts.push(s.Codec);
                if (s.IsDefault) {

                    atts.push('Default');
                }
                if (s.IsForced) {

                    atts.push('Forced');
                }

                itemHtml += '<div class="listItemBody">';

                itemHtml += '<h3 class="listItemBodyText">';
                itemHtml += (s.Language || globalize.translate('LabelUnknownLanaguage'));
                itemHtml += '</h3>';

                itemHtml += '<div class="secondary listItemBodyText">' + atts.join(' - ') + '</div>';

                if (s.Path) {
                    itemHtml += '<div class="secondary listItemBodyText">' + (s.Path) + '</div>';
                }

                itemHtml += '</a>';
                itemHtml += '</div>';

                if (s.Path) {
                    itemHtml += '<button is="paper-icon-button-light" data-index="' + s.Index + '" title="' + globalize.translate('Delete') + '" class="btnDelete"><iron-icon icon="delete"></iron-icon></button>';
                }

                itemHtml += '</div>';

                return itemHtml;

            }).join('');

            html += '</div>';
        }

        var elem = context.querySelector('.subtitleList');

        if (subs.length) {
            elem.classList.remove('hide');
        } else {
            elem.classList.add('hide');
        }
        elem.innerHTML = html;

        //('.btnViewSubtitles', elem).on('click', function () {

        //    var index = this.getAttribute('data-index');

        //    showLocalSubtitles(context, index);

        //});
    }

    function fillLanguages(context, apiClient, languages) {

        var selectLanguage = context.querySelector('#selectLanguage');

        selectLanguage.innerHTML = languages.map(function (l) {

            return '<option value="' + l.ThreeLetterISOLanguageName + '">' + l.DisplayName + '</option>';
        });

        var lastLanguage = appStorage.getItem('subtitleeditor-language');
        if (lastLanguage) {
            selectLanguage.value = lastLanguage;
        }
        else {

            apiClient.getCurrentUser().then(function (user) {

                var lang = user.Configuration.SubtitleLanguagePreference;

                if (lang) {
                    selectLanguage.value = lang;
                }
            });
        }
    }

    function renderSearchResults(context, results) {

        var lastProvider = '';
        var html = '';

        if (!results.length) {

            context.querySelector('.noSearchResults').classList.remove('hide');
            context.querySelector('.subtitleResults').innerHTML = '';
            loading.hide();
            return;
        }

        context.querySelector('.noSearchResults').classList.add('hide');

        for (var i = 0, length = results.length; i < length; i++) {

            var result = results[i];

            var provider = result.ProviderName;

            if (provider != lastProvider) {

                if (i > 0) {
                    html += '</div>';
                }
                html += '<h1>' + provider + '</h1>';
                if (layoutManager.tv) {
                    html += '<div>';
                } else {
                    html += '<div class="paperList">';
                }
                lastProvider = provider;
            }

            html += '<div class="listItem">';

            html += '<paper-fab mini class="blue" icon="closed-caption" item-icon></paper-fab>';

            html += '<div class="listItemBody">';

            //html += '<a class="btnViewSubtitle" href="#" data-subid="' + result.Id + '">';

            html += '<h3 class="listItemBodyText">' + (result.Name) + '</h3>';
            html += '<div class="secondary listItemBodyText">' + (result.Format) + '</div>';

            if (result.Comment) {
                html += '<div class="secondary listItemBodyText">' + (result.Comment) + '</div>';
            }

            //html += '</a>';

            html += '</div>';

            html += '<div class="secondary">' + /*(result.CommunityRating || 0) + ' / ' +*/ (result.DownloadCount || 0) + '</div>';

            html += '<button type="button" is="paper-icon-button-light" data-subid="' + result.Id + '" title="' + globalize.translate('ButtonDownload') + '" class="btnDownload"><iron-icon icon="cloud-download"></iron-icon></button>';

            html += '</div>';
        }

        if (results.length) {
            html += '</div>';
        }

        var elem = context.querySelector('.subtitleResults');
        elem.innerHTML = html;

        //('.btnViewSubtitle', elem).on('click', function () {

        //    var id = this.getAttribute('data-subid');
        //    showRemoteSubtitles(context, id);
        //});

        loading.hide();
    }

    function searchForSubtitles(context, language) {

        appStorage.setItem('subtitleeditor-language', language);

        loading.show();

        var apiClient = connectionManager.getApiClient(currentItem.ServerId);
        var url = apiClient.getUrl('Items/' + currentItem.Id + '/RemoteSearch/Subtitles/' + language);

        apiClient.getJSON(url).then(function (results) {

            renderSearchResults(context, results);
        });
    }

    function reload(context, apiClient, itemId) {

        context.querySelector('.noSearchResults').classList.add('hide');

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

            loading.hide();
        }

        if (typeof itemId == 'string') {
            apiClient.getItem(apiClient.getCurrentUserId(), itemId).then(onGetItem);
        }
        else {
            onGetItem(itemId);
        }
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function onSearchSubmit(e) {
        var form = this;

        var lang = form.querySelector('#selectLanguage', form).value;

        searchForSubtitles(parentWithClass(form, 'dialogContent'), lang);

        e.preventDefault();
        return false;
    }

    function onSubtitleListClick(e) {

        var btnDelete = parentWithClass(e.target, 'btnDelete');
        if (btnDelete) {
            var index = btnDelete.getAttribute('data-index');
            var context = parentWithClass(btnDelete, 'subtitleEditorDialog');
            deleteLocalSubtitle(context, index);
        }
    }

    function onSubtitleResultsClick(e) {

        var btnDownload = parentWithClass(e.target, 'btnDownload');
        if (btnDownload) {
            var id = btnDownload.getAttribute('data-subid');
            var context = parentWithClass(btnDownload, 'subtitleEditorDialog');
            downloadRemoteSubtitles(context, id);
        }
    }

    function showEditor(itemId, serverId) {

        loading.show();

        require(['text!./subtitleeditor.template.html'], function (template) {

            var apiClient = connectionManager.getApiClient(serverId);

            apiClient.getItem(apiClient.getCurrentUserId(), itemId).then(function (item) {

                var dialogOptions = {
                    removeOnClose: true
                };

                if (layoutManager.tv) {
                    dialogOptions.size = 'fullscreen';
                } else {
                    dialogOptions.size = 'small';
                }

                var dlg = dialogHelper.createDialog(dialogOptions);

                dlg.classList.add('formDialog');
                dlg.classList.add('subtitleEditorDialog');

                dlg.innerHTML = globalize.translateDocument(template);
                document.body.appendChild(dlg);

                dlg.querySelector('.pathLabel').innerHTML = globalize.translate('MediaInfoFile');

                dlg.querySelector('.subtitleSearchForm').addEventListener('submit', onSearchSubmit);

                dialogHelper.open(dlg);

                var editorContent = dlg.querySelector('.dialogContent');

                dlg.querySelector('.subtitleList').addEventListener('click', onSubtitleListClick);
                dlg.querySelector('.subtitleResults').addEventListener('click', onSubtitleResultsClick);

                reload(editorContent, apiClient, item);

                apiClient.getCultures().then(function (languages) {

                    fillLanguages(editorContent, apiClient, languages);
                });

                dlg.querySelector('.btnCancel').addEventListener('click', function () {

                    dialogHelper.close(dlg);
                });
            });
        });
    }

    return {
        show: showEditor
    };
});