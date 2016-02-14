define(['paperdialoghelper'], function (paperDialogHelper) {

    return {
        show: function (person) {
            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/metadataeditor/personeditor.template.html', true);

                xhr.onload = function (e) {

                    var template = this.response;
                    var dlg = paperDialogHelper.createDialog({
                        removeOnClose: true,
                        size: 'medium'
                    });

                    dlg.classList.add('ui-body-b');
                    dlg.classList.add('background-theme-b');

                    dlg.classList.add('formDialog');

                    var html = '';
                    var submitted = false;

                    html += Globalize.translateDocument(template);

                    dlg.innerHTML = html;
                    document.body.appendChild(dlg);

                    $('.txtPersonName', dlg).val(person.Name || '');
                    $('.selectPersonType', dlg).val(person.Type || '');
                    $('.txtPersonRole', dlg).val(person.Role || '');

                    paperDialogHelper.open(dlg);

                    dlg.addEventListener('iron-overlay-closed', function () {

                        if (submitted) {
                            resolve(person);
                        } else {
                            reject();
                        }
                    });

                    dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                        paperDialogHelper.close(dlg);
                    });

                    dlg.querySelector('form').addEventListener('submit', function (e) {

                        submitted = true;

                        person.Name = $('.txtPersonName', dlg).val();
                        person.Type = $('.selectPersonType', dlg).val();
                        person.Role = $('.txtPersonRole', dlg).val() || null;

                        paperDialogHelper.close(dlg);

                        e.preventDefault();
                        return false;
                    });
                }

                xhr.send();
            });
        }
    };
});