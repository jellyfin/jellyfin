(function ($, document, window) {

    function loadPage(page, config) {

        page.querySelector('#chkEnableThrottle').checked = config.EnableThrottling;

        $('#selectVideoDecoder', page).val(config.HardwareAccelerationType);
        $('#selectThreadCount', page).val(config.EncodingThreadCount);
        $('#txtDownMixAudioBoost', page).val(config.DownMixAudioBoost);
        $('#txtTranscodingTempPath', page).val(config.TranscodingTempPath || '');

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

        var form = this;

        var onDecoderConfirmed = function() {
            Dashboard.showLoadingMsg();

            ApiClient.getNamedConfiguration("encoding").then(function (config) {

                config.DownMixAudioBoost = $('#txtDownMixAudioBoost', form).val();
                config.TranscodingTempPath = $('#txtTranscodingTempPath', form).val();
                config.EncodingThreadCount = $('#selectThreadCount', form).val();
                config.HardwareAccelerationType = $('#selectVideoDecoder', form).val();

                config.EnableThrottling = form.querySelector('#chkEnableThrottle').checked;

                ApiClient.updateNamedConfiguration("encoding", config).then(Dashboard.processServerConfigurationUpdateResult);
            });
        };

        if ($('#selectVideoDecoder', form).val()) {

            Dashboard.alert({
                callback: onDecoderConfirmed,
                title: Globalize.translate('TitleHardwareAcceleration'),
                message: Globalize.translate('HardwareAccelerationWarning')
            });

        } else {
            onDecoderConfirmed();
        }


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
