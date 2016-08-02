define(['dialogHelper', 'require', 'layoutManager', 'globalize', 'appStorage', 'connectionManager', 'loading', 'focusManager', 'dom', 'apphost', 'emby-select', 'listViewStyle', 'paper-icon-button-light', 'css!./../formdialog', 'material-icons', 'css!./subtitleeditor', 'emby-button'], function (dialogHelper, require, layoutManager, globalize, appStorage, connectionManager, loading, focusManager, dom, appHost) {

    var currentItem;
    var hasChanges;

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

            hasChanges = true;

            require(['toast'], function (toast) {
                toast(globalize.translate('sharedcomponents#MessageDownloadQueued'));
            });

            focusManager.autoFocus(context);
        });
    }

    function deleteLocalSubtitle(context, index) {

        var msg = globalize.translate('sharedcomponents#MessageAreYouSureDeleteSubtitles');

        require(['confirm'], function (confirm) {

            confirm(msg, globalize.translate('sharedcomponents#ConfirmDeletion')).then(function () {

                loading.show();

                var itemId = currentItem.Id;
                var url = 'Videos/' + itemId + '/Subtitles/' + index;

                var apiClient = connectionManager.getApiClient(currentItem.ServerId);

                apiClient.ajax({

                    type: "DELETE",
                    url: apiClient.getUrl(url)

                }).then(function () {

                    hasChanges = true;
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

            html += '<h1>' + globalize.translate('sharedcomponents#MySubtitles') + '</h1>';

            if (layoutManager.tv) {
                html += '<div class="paperList clear">';
            } else {
                html += '<div class="paperList">';
            }

            html += subs.map(function (s) {

                var itemHtml = '';

                var tagName = layoutManager.tv ? 'button' : 'div';
                var className = layoutManager.tv && s.Path ? 'listItem btnDelete' : 'listItem';

                itemHtml += '<' + tagName + ' class="' + className + '" data-index="' + s.Index + '">';

                itemHtml += '<i class="listItemIcon md-icon">closed_caption</i>';

                itemHtml += '<div class="listItemBody two-line">';

                itemHtml += '<div>';
                itemHtml += s.DisplayTitle || '';
                itemHtml += '</div>';

                if (s.Path) {
                    itemHtml += '<div class="secondary listItemBodyText">' + (s.Path) + '</div>';
                }

                itemHtml += '</a>';
                itemHtml += '</div>';

                if (!layoutManager.tv) {
                    if (s.Path) {
                        itemHtml += '<button is="paper-icon-button-light" data-index="' + s.Index + '" title="' + globalize.translate('sharedcomponents#Delete') + '" class="btnDelete"><i class="md-icon">delete</i></button>';
                    }
                }

                itemHtml += '</' + tagName + '>';

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

        var moreIcon = appHost.moreIcon == 'dots-horiz' ? '&#xE5D3;' : '&#xE5D4;';

        for (var i = 0, length = results.length; i < length; i++) {

            var result = results[i];

            var provider = result.ProviderName;

            if (provider != lastProvider) {

                if (i > 0) {
                    html += '</div>';
                }
                html += '<h1>' + provider + '</h1>';
                if (layoutManager.tv) {
                    html += '<div class="paperList clear">';
                } else {
                    html += '<div class="paperList">';
                }
                lastProvider = provider;
            }

            var tagName = layoutManager.tv ? 'button' : 'div';
            var className = layoutManager.tv ? 'listItem btnOptions' : 'listItem';

            html += '<' + tagName + ' class="' + className + '" data-subid="' + result.Id + '">';

            html += '<i class="listItemIcon md-icon">closed_caption</i>';

            html += '<div class="listItemBody two-line">';

            //html += '<a class="btnViewSubtitle" href="#" data-subid="' + result.Id + '">';

            html += '<div>' + (result.Name) + '</div>';
            html += '<div class="secondary listItemBodyText">' + (result.Format) + '</div>';

            if (result.Comment) {
                html += '<div class="secondary listItemBodyText">' + (result.Comment) + '</div>';
            }

            //html += '</a>';

            html += '</div>';

            html += '<div class="secondary">' + /*(result.CommunityRating || 0) + ' / ' +*/ (result.DownloadCount || 0) + '</div>';

            if (!layoutManager.tv) {
                html += '<button type="button" is="paper-icon-button-light" data-subid="' + result.Id + '" class="btnOptions"><i class="md-icon">' + moreIcon + '</i></button>';
            }

            html += '</' + tagName + '>';
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

    function onSearchSubmit(e) {
        var form = this;

        var lang = form.querySelector('#selectLanguage', form).value;

        searchForSubtitles(dom.parentWithClass(form, 'dialogContent'), lang);

        e.preventDefault();
        return false;
    }

    function onSubtitleListClick(e) {

        var btnDelete = dom.parentWithClass(e.target, 'btnDelete');
        if (btnDelete) {
            var index = btnDelete.getAttribute('data-index');
            var context = dom.parentWithClass(btnDelete, 'subtitleEditorDialog');
            deleteLocalSubtitle(context, index);
        }
    }

    function onSubtitleResultsClick(e) {

        var btnOptions = dom.parentWithClass(e.target, 'btnOptions');
        if (btnOptions) {
            var subtitleId = btnOptions.getAttribute('data-subid');
            var context = dom.parentWithClass(btnOptions, 'subtitleEditorDialog');
            showDownloadOptions(btnOptions, context, subtitleId);
        }
    }

    function showDownloadOptions(button, context, subtitleId) {

        var items = [];

        items.push({
            name: Globalize.translate('sharedcomponents#Download'),
            id: 'download'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: items,
                positionTo: button

            }).then(function (id) {

                switch (id) {

                    case 'download':
                        downloadRemoteSubtitles(context, subtitleId);
                        break;
                    default:
                        break;
                }
            });

        });
    }

    function centerFocus(elem, horiz, on) {
        require(['scrollHelper'], function (scrollHelper) {
            var fn = on ? 'on' : 'off';
            scrollHelper.centerFocus[fn](elem, horiz);
        });
    }

    function showEditorInternal(itemId, serverId, template) {

        hasChanges = false;

        var apiClient = connectionManager.getApiClient(serverId);
        return apiClient.getItem(apiClient.getCurrentUserId(), itemId).then(function (item) {

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
            dlg.classList.add('subtitleEditorDialog');

            dlg.innerHTML = globalize.translateDocument(template, 'sharedcomponents');
            document.body.appendChild(dlg);

            dlg.querySelector('.originalSubtitleFileLabel').innerHTML = globalize.translate('sharedcomponents#File');

            dlg.querySelector('.subtitleSearchForm').addEventListener('submit', onSearchSubmit);

            var btnSubmit = dlg.querySelector('.btnSubmit');

            if (layoutManager.tv) {
                centerFocus(dlg.querySelector('.dialogContent'), false, true);
            }

            if (layoutManager.tv) {
                centerFocus(dlg.querySelector('.dialogContent'), false, true);
                dlg.querySelector('.btnSearchSubtitles').classList.add('hide');
            } else {
                btnSubmit.classList.add('hide');
            }

            var editorContent = dlg.querySelector('.dialogContent');

            dlg.querySelector('.subtitleList').addEventListener('click', onSubtitleListClick);
            dlg.querySelector('.subtitleResults').addEventListener('click', onSubtitleResultsClick);

            apiClient.getCultures().then(function (languages) {

                fillLanguages(editorContent, apiClient, languages);
            });

            dlg.querySelector('.btnCancel').addEventListener('click', function () {

                dialogHelper.close(dlg);
            });

            return new Promise(function (resolve, reject) {

                dlg.addEventListener('close', function () {

                    if (layoutManager.tv) {
                        centerFocus(dlg.querySelector('.dialogContent'), false, false);
                    }

                    if (hasChanges) {
                        resolve();
                    } else {
                        reject();
                    }
                });

                dialogHelper.open(dlg);

                reload(editorContent, apiClient, item);
            });
        });
    }

    function showEditor(itemId, serverId) {

        loading.show();

        return new Promise(function (resolve, reject) {

            require(['text!./subtitleeditor.template.html'], function (template) {

                showEditorInternal(itemId, serverId, template).then(resolve, reject);
            });
        });

    }

    return {
        show: showEditor
    };
});