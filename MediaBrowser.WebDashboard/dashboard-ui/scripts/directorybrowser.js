(function (window, document, $) {

    function refreshDirectoryBrowser(page, path, fileOptions) {

        Dashboard.showLoadingMsg();

        if (path) {
            $('.networkHeadline').hide();
        } else {
            $('.networkHeadline').show();
        }

        var promise;

        var parentPathPromise = null;

        if (path === "Network") {
            promise = ApiClient.getNetworkDevices();
        }
        else if (path) {
            promise = ApiClient.getDirectoryContents(path, fileOptions);
            parentPathPromise = ApiClient.getParentPath(path);
        } else {
            promise = ApiClient.getDrives();
        }

        if (!parentPathPromise) {
            parentPathPromise = $.Deferred();
            parentPathPromise.resolveWith(null, []);
            parentPathPromise = parentPathPromise.promise();
        }

        $.when(promise, parentPathPromise).done(function (response1, response2) {

            var folders = response1[0];
            var parentPath = response2 && response2.length ? response2[0] || '' : '';

            $('#txtDirectoryPickerPath', page).val(path || "");

            var html = '';

            if (path) {

                html += '<li><a class="lnkPath lnkDirectory" data-path="' + parentPath + '" href="#">..</a></li>';
            }

            for (var i = 0, length = folders.length; i < length; i++) {

                var folder = folders[i];

                var cssClass = folder.Type == "File" ? "lnkPath lnkFile" : "lnkPath lnkDirectory";

                html += '<li><a class="' + cssClass + '" data-type="' + folder.Type + '" data-path="' + folder.Path + '" href="#">' + folder.Name + '</a></li>';
            }

            if (!path) {
                html += '<li><a class="lnkPath lnkDirectory" data-path="Network" href="#">' + Globalize.translate('ButtonNetwork') + '</a></li>';
            }

            $('#ulDirectoryPickerList', page).html(html).listview('refresh');

            Dashboard.hideLoadingMsg();

        }).fail(function () {

            $('#txtDirectoryPickerPath', page).val("");
            $('#ulDirectoryPickerList', page).html('').listview('refresh');

            Dashboard.hideLoadingMsg();
        });
    }

    window.DirectoryBrowser = function (page) {

        var self = this;

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

            options.header = options.header || Globalize.translate('HeaderSelectPath');
            options.instruction = options.instruction || "";

            var html = '<div data-role="popup" id="popupDirectoryPicker" class="popup" style="min-width:65%;">';

            html += '<div class="ui-bar-a" style="text-align: center; padding: 0 20px;">';
            html += '<h3>' + options.header + '</h3>';
            html += '</div>';

            html += '<div data-role="content" class="ui-content">';
            html += '<form>';

            var instruction = options.instruction ? options.instruction + '<br/><br/>' : '';

            html += '<p class="directoryPickerHeadline">';
            html += instruction;
            html += Globalize.translate('MessageDirectoryPickerInstruction')
                .replace('{0}', '<b>\\\\server</b>')
                .replace('{1}', '<b>\\\\192.168.1.101</b>');
            html += '</p>';

            html += '<div style="margin:20px 0 0;">';
            html += '<label for="txtDirectoryPickerPath" class="lblDirectoryPickerPath">' + Globalize.translate('LabelCurrentPath') + '</label>';
            html += '<div style="width:82%;display:inline-block;"><input id="txtDirectoryPickerPath" name="txtDirectoryPickerPath" type="text" required="required" style="font-weight:bold;" /></div>';
            html += '<button class="btnRefreshDirectories" type="button" data-icon="refresh" data-inline="true" data-mini="true" data-iconpos="notext">' + Globalize.translate('ButtonRefresh') + '</button>';
            html += '</div>';

            html += '<div style="height: 180px; overflow-y: auto;">';
            html += '<ul id="ulDirectoryPickerList" data-role="listview" data-inset="true" data-auto-enhanced="false"></ul>';


            html += '</div>';


            html += '<p>';
            html += '<button type="submit" data-theme="b" data-icon="check" data-mini="true">' + Globalize.translate('ButtonOk') + '</button>';
            html += '<button type="button" data-icon="delete" onclick="$(this).parents(\'.popup\').popup(\'close\');" data-mini="true">' + Globalize.translate('ButtonCancel') + '</button>';
            html += '</p>';
            html += '</form>';
            html += '</div>';
            html += '</div>';

            $(page).append(html);

            var popup = $('#popupDirectoryPicker').popup().trigger('create').on("popupafteropen", function () {

                $('#popupDirectoryPicker input:first', this).focus();

            }).popup("open").on("popupafterclose", function () {

                $('form', this).off("submit");

                $(this).off("click").off("change").off("popupafterclose").remove();

            }).on("click", ".lnkPath", function () {

                var path = this.getAttribute('data-path');

                if ($(this).hasClass('lnkFile')) {
                    $('#txtDirectoryPickerPath', page).val(path);
                } else {
                    refreshDirectoryBrowser(page, path, fileOptions);
                }


            }).on("click", ".btnRefreshDirectories", function () {

                var path = $('#txtDirectoryPickerPath', page).val();

                refreshDirectoryBrowser(page, path, fileOptions);

            }).on("change", "#txtDirectoryPickerPath", function () {

                refreshDirectoryBrowser(page, this.value, fileOptions);
            });

            var txtCurrentPath = $('#txtDirectoryPickerPath', popup);

            if (options.path) {
                txtCurrentPath.val(options.path);
            }

            $('form', popup).on('submit', function () {

                if (options.callback) {
                    options.callback($('#txtDirectoryPickerPath', this).val());
                }

                return false;
            });

            refreshDirectoryBrowser(page, txtCurrentPath.val());

        };

        self.close = function () {
            $('#popupDirectoryPicker', page).popup("close");
        };
    };

})(window, document, jQuery);