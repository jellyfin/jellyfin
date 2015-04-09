(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtRemoteClientBitrateLimit', page).val((config.RemoteClientBitrateLimit / 1000000) || '');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageinit', "#streamingSettingsPage", function () {

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

    }).on('pageshow', "#streamingSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });
    });

    window.StreamingSettingsPage = {

        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.RemoteClientBitrateLimit = parseInt(parseFloat(($('#txtRemoteClientBitrateLimit', form).val() || '0')) * 1000000);

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }
    };

})(jQuery, document, window);
