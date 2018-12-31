define(['require', 'dom', 'focusManager', 'dialogHelper', 'loading', 'layoutManager', 'connectionManager', 'globalize', 'userSettings', 'emby-select', 'paper-icon-button-light', 'material-icons', 'css!./../formdialog', 'emby-button', 'emby-linkbutton', 'flexStyles'], function (require, dom, focusManager, dialogHelper, loading, layoutManager, connectionManager, globalize, userSettings) {
    'use strict';

    function onSubmit(e) {

        e.preventDefault();
        return false;
    }

    function initEditor(context, settings) {

        context.querySelector('form').addEventListener('submit', onSubmit);

        context.querySelector('.selectSortOrder').value = settings.sortOrder;
        context.querySelector('.selectSortBy').value = settings.sortBy;
    }

    function centerFocus(elem, horiz, on) {
        require(['scrollHelper'], function (scrollHelper) {
            var fn = on ? 'on' : 'off';
            scrollHelper.centerFocus[fn](elem, horiz);
        });
    }

    function fillSortBy(context, options) {
        var selectSortBy = context.querySelector('.selectSortBy');

        selectSortBy.innerHTML = options.map(function (o) {

            return '<option value="' + o.value + '">' + o.name + '</option>';

        }).join('');
    }

    function saveValues(context, settings, settingsKey) {

        userSettings.setFilter(settingsKey + '-sortorder', context.querySelector('.selectSortOrder').value);
        userSettings.setFilter(settingsKey + '-sortby', context.querySelector('.selectSortBy').value);
    }

    function SortMenu() {

    }

    SortMenu.prototype.show = function (options) {

        return new Promise(function (resolve, reject) {

            require(['text!./sortmenu.template.html'], function (template) {

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

                html += '<div class="formDialogHeader">';
                html += '<button is="paper-icon-button-light" class="btnCancel hide-mouse-idle-tv" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
                html += '<h3 class="formDialogHeaderTitle">${Sort}</h3>';

                html += '</div>';

                html += template;

                dlg.innerHTML = globalize.translateDocument(html, 'sharedcomponents');

                fillSortBy(dlg, options.sortOptions);
                initEditor(dlg, options.settings);

                dlg.querySelector('.btnCancel').addEventListener('click', function () {

                    dialogHelper.close(dlg);
                });

                if (layoutManager.tv) {
                    centerFocus(dlg.querySelector('.formDialogContent'), false, true);
                }

                var submitted;

                dlg.querySelector('form').addEventListener('change', function () {

                    submitted = true;
                    //if (options.onChange) {
                    //    saveValues(dlg, options.settings, options.settingsKey);
                    //    options.onChange();
                    //}

                }, true);

                dialogHelper.open(dlg).then(function () {

                    if (layoutManager.tv) {
                        centerFocus(dlg.querySelector('.formDialogContent'), false, false);
                    }

                    if (submitted) {

                        //if (!options.onChange) {
                        saveValues(dlg, options.settings, options.settingsKey);
                        resolve();
                        //}
                        return;
                    }

                    reject();
                });
            });
        });
    };

    return SortMenu;
});