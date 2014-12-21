(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkEnableDebugEncodingLogging', page).checked(config.EnableDebugLogging).checkboxradio('refresh');
        
        $('.radioEncodingQuality', page).each(function() {

            this.checked = config.MediaEncodingQuality == this.value;

        }).checkboxradio('refresh');
        
        $('#txtDownMixAudioBoost', page).val(config.DownMixAudioBoost);
        $('#txtTranscodingTempPath', page).val(config.TranscodingTempPath || '');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageinit', "#encodingSettingsPage", function () {

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

    }).on('pageshow', "#encodingSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getNamedConfiguration("encoding").done(function (config) {

            loadPage(page, config);

        });
    });

    window.EncodingSettingsPage = {

        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getNamedConfiguration("encoding").done(function (config) {

                config.EnableDebugLogging = $('#chkEnableDebugEncodingLogging', form).checked();
                config.MediaEncodingQuality = $('.radioEncodingQuality:checked', form).val();
                config.DownMixAudioBoost = $('#txtDownMixAudioBoost', form).val();
                config.TranscodingTempPath = $('#txtTranscodingTempPath', form).val();

                ApiClient.updateNamedConfiguration("encoding", config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }
    };

})(jQuery, document, window);
