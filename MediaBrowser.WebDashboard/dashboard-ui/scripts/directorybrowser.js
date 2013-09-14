(function (window, document, $) {

    function refreshDirectoryBrowser(page, path) {

        Dashboard.showLoadingMsg();

        if (path) {
            $('.networkHeadline').hide();
        } else {
            $('.networkHeadline').show();
        }

        var promise;

        if (path === "Network") {
            promise = ApiClient.getNetworkDevices();
        }
        else if (path) {
            promise = ApiClient.getDirectoryContents(path, { includeDirectories: true });
        } else {
            promise = ApiClient.getDrives();
        }

        promise.done(function (folders) {

            $('#txtDirectoryPickerPath', page).val(path || "");

            var html = '';

            if (path) {

                var parentPath = path;

                if (parentPath.endsWith('\\')) {
                    parentPath = parentPath.substring(0, parentPath.length - 1);
                }

                var lastIndex = parentPath.lastIndexOf('\\');
                parentPath = lastIndex == -1 ? "" : parentPath.substring(0, lastIndex);

                if (parentPath.endsWith(':')) {
                    parentPath += "\\";
                }

                if (parentPath == '\\') {
                    parentPath = "Network";
                }

                html += '<li><a class="lnkDirectory" data-path="' + parentPath + '" href="#">..</a></li>';
            }

            for (var i = 0, length = folders.length; i < length; i++) {

                var folder = folders[i];

                html += '<li><a class="lnkDirectory" data-path="' + folder.Path + '" href="#">' + folder.Name + '</a></li>';
            }

            if (!path) {
                html += '<li><a class="lnkDirectory" data-path="Network" href="#">Network</a></li>';
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

            options.header = options.header || "Select Media Path";
            options.instruction = options.instruction || "Any path will do, but for optimal playback of bluray, dvd folders, and games, <b>network paths (UNC)</b> are recommended.";

            var html = '<div data-role="popup" id="popupDirectoryPicker" class="ui-corner-all popup" style="min-width:65%;">';

            html += '<div class="ui-corner-top ui-bar-a" style="text-align: center; padding: 0 20px;">';
            html += '<h3>' + options.header + '</h3>';
            html += '</div>';

            html += '<div data-role="content" class="ui-corner-bottom ui-content">';
            html += '<form>';
            html += '<p class="directoryPickerHeadline">' + options.instruction + '</p>';

            html += '<div style="margin:0;">';
            html += '<label for="txtDirectoryPickerPath" class="lblDirectoryPickerPath">Current Folder:</label>';
            html += '<div style="width:92%;display:inline-block;"><input id="txtDirectoryPickerPath" name="txtDirectoryPickerPath" type="text" required="required" style="font-weight:bold;" /></div>';
            html += '<button class="btnRefreshDirectories" type="button" data-icon="refresh" data-inline="true" data-mini="true" data-iconpos="notext">Refresh</button>';
            html += '</div>';

            html += '<div class="directoryPickerHeadline networkHeadline" style="margin:5px 0 1em;padding:.5em;max-width:95%;">Network paths <b>can be entered manually</b> in the event the Network button fails to locate your devices. For example, <b>\\\\my-server</b> or <b>\\\\192.168.1.101</b>.</div>';

            html += '<div style="height: 320px; overflow-y: auto;">';
            html += '<ul id="ulDirectoryPickerList" data-role="listview" data-inset="true" data-auto-enhanced="false"></ul>';


            html += '</div>';
            

            html += '<p>';
            html += '<button type="submit" data-theme="b" data-icon="ok">OK</button>';
            html += '<button type="button" data-icon="delete" onclick="$(this).parents(\'.popup\').popup(\'close\');">Cancel</button>';
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

            }).on("click", ".lnkDirectory", function () {

                var path = this.getAttribute('data-path');

                refreshDirectoryBrowser(page, path);

            }).on("click", ".btnRefreshDirectories", function () {

                var path = $('#txtDirectoryPickerPath', page).val();

                refreshDirectoryBrowser(page, path);

            }).on("change", "#txtDirectoryPickerPath", function () {

                refreshDirectoryBrowser(page, this.value);
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