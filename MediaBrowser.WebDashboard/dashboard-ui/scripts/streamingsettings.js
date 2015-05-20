(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtRemoteClientBitrateLimit', page).val((config.RemoteClientBitrateLimit / 1000000) || '');

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().done(function (config) {

            config.RemoteClientBitrateLimit = parseInt(parseFloat(($('#txtRemoteClientBitrateLimit', form).val() || '0')) * 1000000);

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinitdepends', "#streamingSettingsPage", function () {

        var page = this;

        $('#btnSelectTranscodingTempPath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtTranscodingTempPath', page).val(path);
                    }
                    picker.close();
                },

                header: Globalize.translate('HeaderSelectTranscodingPath'),

                instruction: Globalize.translate('HeaderSelectTranscodingPathHelp')
            });
        });

        $('.streamingSettingsForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshowready', "#streamingSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });
    });

})(jQuery, document, window);
