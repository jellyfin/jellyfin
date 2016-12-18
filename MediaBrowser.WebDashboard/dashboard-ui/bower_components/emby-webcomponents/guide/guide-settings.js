define(['dialogHelper', 'globalize', 'userSettings', 'layoutManager', 'connectionManager', 'require', 'loading', 'scrollHelper', 'emby-checkbox', 'emby-radio', 'css!./../formdialog', 'material-icons'], function (dialogHelper, globalize, userSettings, layoutManager, connectionManager, require, loading, scrollHelper) {
    'use strict';

    function saveCategories(context, options) {

        var categories = [];

        var chkCategorys = context.querySelectorAll('.chkCategory');
        for (var i = 0, length = chkCategorys.length; i < length; i++) {

            var type = chkCategorys[i].getAttribute('data-type');

            if (chkCategorys[i].checked) {
                categories.push(type);
            }
        }

        if (categories.length >= 4) {
            categories.push('series');
        }

        // differentiate between none and all
        categories.push('all');
        options.categories = categories;
    }

    function loadCategories(context, options) {

        var selectedCategories = options.categories || [];

        var chkCategorys = context.querySelectorAll('.chkCategory');
        for (var i = 0, length = chkCategorys.length; i < length; i++) {

            var type = chkCategorys[i].getAttribute('data-type');

            chkCategorys[i].checked = !selectedCategories.length || selectedCategories.indexOf(type) !== -1;
        }
    }

    function save(context) {

        var i, length;

        var chkIndicators = context.querySelectorAll('.chkIndicator');
        for (i = 0, length = chkIndicators.length; i < length; i++) {

            var type = chkIndicators[i].getAttribute('data-type');
            userSettings.set('guide-indicator-' + type, chkIndicators[i].checked);
        }

        userSettings.set('guide-colorcodedbackgrounds', context.querySelector('.chkColorCodedBackgrounds').checked);
        userSettings.set('livetv-favoritechannelsattop', context.querySelector('.chkFavoriteChannelsAtTop').checked);

        var sortBys = context.querySelectorAll('.chkSortOrder');
        for (i = 0, length = sortBys.length; i < length; i++) {
            if (sortBys[i].checked) {
                userSettings.set('livetv-channelorder', sortBys[i].value);
                break;
            }
        }
    }

    function load(context) {

        var i, length;

        var chkIndicators = context.querySelectorAll('.chkIndicator');
        for (i = 0, length = chkIndicators.length; i < length; i++) {

            var type = chkIndicators[i].getAttribute('data-type');

            if (chkIndicators[i].getAttribute('data-default') === 'true') {
                chkIndicators[i].checked = userSettings.get('guide-indicator-' + type) !== 'false';
            } else {
                chkIndicators[i].checked = userSettings.get('guide-indicator-' + type) === 'true';
            }
        }

        context.querySelector('.chkColorCodedBackgrounds').checked = userSettings.get('guide-colorcodedbackgrounds') === 'true';
        context.querySelector('.chkFavoriteChannelsAtTop').checked = userSettings.get('livetv-favoritechannelsattop') !== 'false';

        var sortByValue = userSettings.get('livetv-channelorder') || 'Number';

        var sortBys = context.querySelectorAll('.chkSortOrder');
        for (i = 0, length = sortBys.length; i < length; i++) {
            sortBys[i].checked = sortBys[i].value === sortByValue;
        }
    }

    function onSortByChange() {
        var newValue = this.value;
        if (this.checked) {
            var changed = options.query.SortBy !== newValue;

            options.query.SortBy = newValue.replace('_', ',');
            options.query.StartIndex = 0;

            if (options.callback && changed) {
                options.callback();
            }
        }
    }

    function showEditor(options) {

        return new Promise(function (resolve, reject) {

            var settingsChanged = false;

            require(['text!./guide-settings.template.html'], function (template) {

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

                html += globalize.translateDocument(template, 'sharedcomponents');

                dlg.innerHTML = html;

                dlg.addEventListener('change', function () {

                    settingsChanged = true;
                });

                dlg.addEventListener('close', function () {

                    if (layoutManager.tv) {
                        scrollHelper.centerFocus.off(dlg.querySelector('.formDialogContent'), false);
                    }

                    save(dlg);
                    saveCategories(dlg, options);

                    if (settingsChanged) {
                        resolve();
                    } else {
                        reject();
                    }
                });

                dlg.querySelector('.btnCancel').addEventListener('click', function () {
                    dialogHelper.close(dlg);
                });

                if (layoutManager.tv) {
                    scrollHelper.centerFocus.on(dlg.querySelector('.formDialogContent'), false);
                }

                load(dlg);
                loadCategories(dlg, options);
                dialogHelper.open(dlg);
            });
        });
    }

    return {
        show: showEditor
    };
});