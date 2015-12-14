(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkEnableDebugEncodingLogging', page).checked(config.EnableDebugLogging).checkboxradio('refresh');
        $('#chkEnableThrottle', page).checked(config.EnableThrottling).checkboxradio('refresh');

        $('.radioEncodingQuality', page).each(function () {

            this.checked = config.EncodingQuality == this.value;

        }).checkboxradio('refresh');

        $('#selectVideoDecoder', page).val(config.HardwareAccelerationType);
        $('#selectThreadCount', page).val(config.EncodingThreadCount);
        $('#txtDownMixAudioBoost', page).val(config.DownMixAudioBoost);
        $('#txtTranscodingTempPath', page).val(config.TranscodingTempPath || '');

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration("encoding").then(function (config) {

            config.EnableDebugLogging = $('#chkEnableDebugEncodingLogging', form).checked();
            config.EncodingQuality = $('.radioEncodingQuality:checked', form).val();
            config.DownMixAudioBoost = $('#txtDownMixAudioBoost', form).val();
            config.TranscodingTempPath = $('#txtTranscodingTempPath', form).val();
            config.EnableThrottling = $('#chkEnableThrottle', form).checked();
            config.EncodingThreadCount = $('#selectThreadCount', form).val();
            config.HardwareAccelerationType = $('#selectVideoDecoder', form).val();

            ApiClient.updateNamedConfiguration("encoding", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#encodingSettingsPage", function () {

        var page = this;

        $('#btnSelectTranscodingTempPath', page).on("click.selectDirectory", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

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
        });

        $('.encodingSettingsForm').off('submit', onSubmit).on('submit', onSubmit);


    }).on('pageshow', "#encodingSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getNamedConfiguration("encoding").then(function (config) {

            loadPage(page, config);

        });
    });

})(jQuery, document, window);
