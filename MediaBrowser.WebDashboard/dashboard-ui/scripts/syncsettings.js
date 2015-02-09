(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtSyncTempPath', page).val(config.TemporaryPath || '');

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

                ApiClient.updateNamedConfiguration("sync", config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }
    };

})(jQuery, document, window);
