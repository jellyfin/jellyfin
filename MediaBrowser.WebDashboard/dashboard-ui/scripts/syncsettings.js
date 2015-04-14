(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtSyncTempPath', page).val(config.TemporaryPath || '');
        $('#txtUploadSpeedLimit', page).val((config.UploadSpeedLimitBytes / 1000000) || '');
        $('#txtCpuCoreLimit', page).val(config.TranscodingCpuCoreLimit);
        $('#chkEnableFullSpeedConversion', page).checked(config.EnableFullSpeedTranscoding).checkboxradio('refresh');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageinit', "#syncSettingsPage", function () {

        var page = this;

        $('#btnSelectSyncTempPath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtSyncTempPath', page).val(path);
                    }
                    picker.close();
                }
            });
        });

    }).on('pageshow', "#syncSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getNamedConfiguration("sync").done(function (config) {

            loadPage(page, config);

        });
    });

    window.SyncSettingsPage = {

        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getNamedConfiguration("sync").done(function (config) {

                config.TemporaryPath = $('#txtSyncTempPath', form).val();
                config.UploadSpeedLimitBytes = parseInt(parseFloat(($('#txtUploadSpeedLimit', form).val() || '0')) * 1000000);
                config.TranscodingCpuCoreLimit = parseInt($('#txtCpuCoreLimit', form).val());
                config.EnableFullSpeedTranscoding = $('#chkEnableFullSpeedConversion', form).checked();

                ApiClient.updateNamedConfiguration("sync", config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }
    };

})(jQuery, document, window);
