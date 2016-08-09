define(['dialogHelper', 'jQuery', 'listViewStyle', 'emby-input', 'emby-button', 'paper-icon-button-light', 'css!./directorybrowser'], function (dialogHelper, $) {

    var systemInfo;
    function getSystemInfo() {

        var deferred = jQuery.Deferred();

        if (systemInfo) {
            deferred.resolveWith(null, [systemInfo]);
        } else {
            ApiClient.getPublicSystemInfo().then(function (info) {
                systemInfo = info;
                deferred.resolveWith(null, [systemInfo]);
            });
        }

        return deferred.promise();
    }

    function onDialogClosed() {

        Dashboard.hideLoadingMsg();
    }

    function refreshDirectoryBrowser(page, path, fileOptions) {

        if (path && typeof (path) !== 'string') {
            throw new Error('invalid path');
        }
        Dashboard.showLoadingMsg();

        if (path) {
            $('.networkHeadline').hide();
        } else {
            $('.networkHeadline').show();
        }

        var promises = [];

        if (path === "Network") {
            promises.push(ApiClient.getNetworkDevices());
        }
        else if (path) {

            promises.push(ApiClient.getDirectoryContents(path, fileOptions));
            promises.push(ApiClient.getParentPath(path));
        } else {
            promises.push(ApiClient.getDrives());
        }

        Promise.all(promises).then(function (responses) {

            var folders = responses[0];
            var parentPath = responses[1] || '';

            $('#txtDirectoryPickerPath', page).val(path || "");

            var html = '';

            if (path) {

                html += getItem("lnkPath lnkDirectory", "", parentPath, '...');
            }

            for (var i = 0, length = folders.length; i < length; i++) {

                var folder = folders[i];

                var cssClass = folder.Type == "File" ? "lnkPath lnkFile" : "lnkPath lnkDirectory";

                html += getItem(cssClass, folder.Type, folder.Path, folder.Name);
            }

            if (!path) {
                html += getItem("lnkPath lnkDirectory", "", "Network", Globalize.translate('ButtonNetwork'));
            }

            $('.results', page).html(html);

            Dashboard.hideLoadingMsg();

        }, function () {

            $('#txtDirectoryPickerPath', page).val("");
            $('.results', page).html('');

            Dashboard.hideLoadingMsg();
        });
    }

    function getItem(cssClass, type, path, name) {

        var html = '';
        html += '<div class="listItem ' + cssClass + '" data-type="' + type + '" data-path="' + path + '" style="border-bottom:1px solid #e0e0e0;">';
        html += '<div class="listItemBody" style="min-height:2em;padding-left:0;">';
        html += '<div class="listItemBodyText">';
        html += name;
        html += '</div>';
        html += '</div>';
        html += '<i class="md-icon" style="font-size:inherit;">arrow_forward</i>';
        html += '</div>';

        return html;
    }

    function getEditorHtml(options, systemInfo) {

        var html = '';

        var instruction = options.instruction ? options.instruction + '<br/><br/>' : '';

        html += '<p class="directoryPickerHeadline">';
        html += instruction;
        html += Globalize.translate('MessageDirectoryPickerInstruction')
            .replace('{0}', '<b>\\\\server</b>')
            .replace('{1}', '<b>\\\\192.168.1.101</b>');

        if (systemInfo.OperatingSystem.toLowerCase() == 'bsd') {

            html += '<br/>';
            html += '<br/>';
            html += Globalize.translate('MessageDirectoryPickerBSDInstruction');
            html += '<br/>';
            html += '<a href="http://doc.freenas.org/9.3/freenas_jails.html#add-storage" target="_blank">' + Globalize.translate('ButtonMoreInformation') + '</a>';
        }
        else if (systemInfo.OperatingSystem.toLowerCase() == 'linux') {

            html += '<br/>';
            html += '<br/>';
            html += Globalize.translate('MessageDirectoryPickerLinuxInstruction');
            html += '<br/>';
        }

        html += '</p>';

        html += '<form style="max-width:100%;">';

        html += '<div class="inputContainer" style="display: flex; align-items: center;">';
        html += '<div style="flex-grow:1;">';
        html += '<input is="emby-input" id="txtDirectoryPickerPath" type="text" required="required" label="' + Globalize.translate('LabelCurrentPath') + '"/>';
        html += '</div>';
        html += '<button type="button" is="paper-icon-button-light" class="btnRefreshDirectories" title="' + Globalize.translate('ButtonRefresh') + '"><i class="md-icon">search</i></button>';
        html += '</div>';

        html += '<div class="results paperList" style="height: 180px; overflow-y: auto;"></div>';

        html += '<div>';
        html += '<button is="emby-button" type="submit" class="raised submit block">' + Globalize.translate('ButtonOk') + '</button>';
        html += '</div>';

        html += '</form>';
        html += '</div>';

        return html;
    }

    function initEditor(content, options, fileOptions) {

        $(content).on("click", ".lnkPath", function () {

            var path = this.getAttribute('data-path');

            if ($(this).hasClass('lnkFile')) {
                $('#txtDirectoryPickerPath', content).val(path);
            } else {
                refreshDirectoryBrowser(content, path, fileOptions);
            }
        }).on("click", ".btnRefreshDirectories", function () {

            var path = $('#txtDirectoryPickerPath', content).val();

            refreshDirectoryBrowser(content, path, fileOptions);

        }).on("change", "#txtDirectoryPickerPath", function () {

            refreshDirectoryBrowser(content, this.value, fileOptions);
        });

        $('form', content).on('submit', function () {

            if (options.callback) {
                options.callback(this.querySelector('#txtDirectoryPickerPath').value);
            }

            return false;
        });
    }

    function getDefaultPath(options) {
        if (options.path) {
            return Promise.resolve(options.path);
        }

        return ApiClient.getJSON(ApiClient.getUrl("Environment/DefaultDirectoryBrowser")).then(function (result) {

            return result.Path || '';

        }, function () {
            return '';
        });
    }

    function directoryBrowser() {

        var self = this;
        var currentDialog;

        self.show = function (options) {

            options = options || {};

            var fileOptions = {
                includeDirectories: true
            };

            if (options.includeDirectories != null) {
                fileOptions.includeDirectories = options.includeDirectories;
            }

            if (options.includeFiles != null) {
                fileOptions.includeFiles = options.includeFiles;
            }

            Promise.all([getSystemInfo(), getDefaultPath(options)]).then(function (responses) {

                var systemInfo = responses[0];
                var initialPath = responses[1];

                var dlg = dialogHelper.createDialog({
                    size: 'medium',
                    removeOnClose: true
                });

                dlg.classList.add('ui-body-a');
                dlg.classList.add('background-theme-a');

                dlg.classList.add('directoryPicker');

                var html = '';
                html += '<h2 class="dialogHeader">';
                html += '<button type="button" is="emby-button" icon="arrow-back" class="fab mini btnCloseDialog autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
                html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + (options.header || Globalize.translate('HeaderSelectPath')) + '</div>';
                html += '</h2>';

                html += '<div class="editorContent" style="max-width:800px;margin:auto;">';
                html += getEditorHtml(options, systemInfo);
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                var editorContent = dlg.querySelector('.editorContent');
                initEditor(editorContent, options, fileOptions);

                // Has to be assigned a z-index after the call to .open() 
                $(dlg).on('iron-overlay-opened', function () {
                    this.querySelector('#txtDirectoryPickerPath input').focus();
                });
                $(dlg).on('close', onDialogClosed);

                dialogHelper.open(dlg);

                $('.btnCloseDialog', dlg).on('click', function () {

                    dialogHelper.close(dlg);
                });

                currentDialog = dlg;

                var txtCurrentPath = editorContent.querySelector('#txtDirectoryPickerPath');
                txtCurrentPath.value = initialPath;
                refreshDirectoryBrowser(editorContent, txtCurrentPath.value);

            });
        };

        self.close = function () {
            if (currentDialog) {
                dialogHelper.close(currentDialog);
            }
        };

    }

    return directoryBrowser;
});