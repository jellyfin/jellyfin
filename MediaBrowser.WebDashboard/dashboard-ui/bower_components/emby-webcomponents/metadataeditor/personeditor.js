define(['dialogHelper', 'layoutManager', 'globalize', 'require', 'paper-icon-button-light', 'emby-input', 'emby-select', 'css!./../formdialog'], function (dialogHelper, layoutManager, globalize, require) {

    function centerFocus(elem, horiz, on) {
        require(['scrollHelper'], function (scrollHelper) {
            var fn = on ? 'on' : 'off';
            scrollHelper.centerFocus[fn](elem, horiz);
        });
    }

    function show(person) {
        return new Promise(function (resolve, reject) {

            require(['text!./personeditor.template.html'], function (template) {

                var dialogOptions = {
                    removeOnClose: true,
                    scrollY: false
                };

                if (layoutManager.tv) {
                    dialogOptions.size = 'fullscreen';
                } else {
                    dialogOptions.size = 'medium';
                }

                var dlg = dialogHelper.createDialog(dialogOptions);

                dlg.classList.add('ui-body-b');
                dlg.classList.add('background-theme-b');

                dlg.classList.add('formDialog');

                var html = '';
                var submitted = false;

                html += globalize.translateDocument(template, 'sharedcomponents');

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                dlg.querySelector('.txtPersonName', dlg).value = person.Name || '';
                dlg.querySelector('.selectPersonType', dlg).value = person.Type || '';
                dlg.querySelector('.txtPersonRole', dlg).value = person.Role || '';

                if (layoutManager.tv) {
                    centerFocus(dlg.querySelector('.dialogContent'), false, true);
                }

                dialogHelper.open(dlg);

                dlg.addEventListener('close', function () {

                    if (layoutManager.tv) {
                        centerFocus(dlg.querySelector('.dialogContent'), false, false);
                    }

                    if (submitted) {
                        resolve(person);
                    } else {
                        reject();
                    }
                });

                dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                    dialogHelper.close(dlg);
                });

                dlg.querySelector('form').addEventListener('submit', function (e) {

                    submitted = true;

                    person.Name = dlg.querySelector('.txtPersonName', dlg).value;
                    person.Type = dlg.querySelector('.selectPersonType', dlg).value;
                    person.Role = dlg.querySelector('.txtPersonRole', dlg).value || null;

                    dialogHelper.close(dlg);

                    e.preventDefault();
                    return false;
                });
            });
        });
    }

    return {
        show: show
    };
});