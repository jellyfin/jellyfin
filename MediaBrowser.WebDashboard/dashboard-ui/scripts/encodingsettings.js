define(['jQuery'], function ($) {

    function loadPage(page, config) {

        page.querySelector('#chkEnableThrottle').checked = config.EnableThrottling;

        $('#selectVideoDecoder', page).val(config.HardwareAccelerationType);
        $('#selectThreadCount', page).val(config.EncodingThreadCount);
        $('#txtDownMixAudioBoost', page).val(config.DownMixAudioBoost);
        $('.txtEncoderPath', page).val(config.EncoderAppPath || '');
        $('#txtTranscodingTempPath', page).val(config.TranscodingTempPath || '');

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

        var form = this;

        var onDecoderConfirmed = function () {
            Dashboard.showLoadingMsg();

            ApiClient.getNamedConfiguration("encoding").then(function (config) {

                config.DownMixAudioBoost = $('#txtDownMixAudioBoost', form).val();
                config.TranscodingTempPath = $('#txtTranscodingTempPath', form).val();
                config.EncoderAppPath = $('.txtEncoderPath', form).val();
                config.EncodingThreadCount = $('#selectThreadCount', form).val();
                config.HardwareAccelerationType = $('#selectVideoDecoder', form).val();

                config.EnableThrottling = form.querySelector('#chkEnableThrottle').checked;

                ApiClient.updateNamedConfiguration("encoding", config).then(Dashboard.processServerConfigurationUpdateResult);
            });
        };

        if ($('#selectVideoDecoder', form).val()) {

            require(['alert'], function (alert) {
                alert({
                    title: Globalize.translate('TitleHardwareAcceleration'),
                    text: Globalize.translate('HardwareAccelerationWarning')
                }).then(onDecoderConfirmed);
            });

        } else {
            onDecoderConfirmed();
        }


        // Disable default form submission
        return false;
    }

    function getTabs() {
        return [
        {
            href: 'cinemamodeconfiguration.html',
            name: Globalize.translate('TabCinemaMode')
        },
         {
             href: 'playbackconfiguration.html',
             name: Globalize.translate('TabResumeSettings')
         },
         {
             href: 'streamingsettings.html',
             name: Globalize.translate('TabStreaming')
         },
         {
             href: 'encodingsettings.html',
             name: Globalize.translate('TabTranscoding')
         }];
    }

    $(document).on('pageinit', "#encodingSettingsPage", function () {

        var page = this;

        $('#btnSelectEncoderPath', page).on("click.selectDirectory", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser({
                    includeFiles: true
                });

                picker.show({

                    callback: function (path) {

                        if (path) {
                            $('.txtEncoderPath', page).val(path);
                        }
                        picker.close();
                    }
                });
            });
        });

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

        LibraryMenu.setTabs('playback', 3, getTabs);
        var page = this;

        ApiClient.getNamedConfiguration("encoding").then(function (config) {

            loadPage(page, config);
        });

        ApiClient.getSystemInfo().then(function (systemInfo) {

            if (systemInfo.HasExternalEncoder) {
                page.querySelector('.fldEncoderPath').classList.add('hide');
            } else {
                page.querySelector('.fldEncoderPath').classList.remove('hide');
            }
        });
    });

});
