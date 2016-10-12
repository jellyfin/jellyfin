define(['jQuery'], function ($) {

    function loadPage(page, config, systemInfo) {

        page.querySelector('#chkEnableThrottle').checked = config.EnableThrottling;

        $('#selectVideoDecoder', page).val(config.HardwareAccelerationType);
        $('#selectThreadCount', page).val(config.EncodingThreadCount);
        $('#txtDownMixAudioBoost', page).val(config.DownMixAudioBoost);
        page.querySelector('.txtEncoderPath').value = config.EncoderAppPath || '';
        $('#txtTranscodingTempPath', page).val(config.TranscodingTempPath || '');
        $('#txtVaapiDevice', page).val(config.VaapiDevice || '');

        page.querySelector('#selectH264Preset').value = config.H264Preset || '';
        page.querySelector('#txtH264Crf').value = config.H264Crf || '';

        var selectEncoderPath = page.querySelector('#selectEncoderPath');

        selectEncoderPath.value = systemInfo.EncoderLocationType;
        onSelectEncoderPathChange.call(selectEncoderPath);

        page.querySelector('#selectVideoDecoder').dispatchEvent(new CustomEvent('change', {
            bubbles: true
        }));

        Dashboard.hideLoadingMsg();
    }

    function onSaveEncodingPathFailure(response) {

        Dashboard.hideLoadingMsg();

        var msg = '';

        // This is a fallback that handles both 404 and 400 (no path entered)
        msg = Globalize.translate('FFmpegSavePathNotFound');

        require(['alert'], function (alert) {
            alert(msg);
        });
    }

    function updateEncoder(form) {

        return ApiClient.getSystemInfo().then(function (systemInfo) {

            return ApiClient.ajax({
                url: ApiClient.getUrl('System/MediaEncoder/Path'),
                type: 'POST',
                data: {
                    Path: form.querySelector('.txtEncoderPath').value,
                    PathType: form.querySelector('#selectEncoderPath').value
                }
            }).then(Dashboard.processServerConfigurationUpdateResult, onSaveEncodingPathFailure);
        });
    }

    function onSubmit() {

        var form = this;

        var onDecoderConfirmed = function () {
            Dashboard.showLoadingMsg();

            ApiClient.getNamedConfiguration("encoding").then(function (config) {

                config.DownMixAudioBoost = $('#txtDownMixAudioBoost', form).val();
                config.TranscodingTempPath = $('#txtTranscodingTempPath', form).val();
                config.EncodingThreadCount = $('#selectThreadCount', form).val();
                config.HardwareAccelerationType = $('#selectVideoDecoder', form).val();
                config.VaapiDevice = $('#txtVaapiDevice', form).val();

                config.H264Preset = form.querySelector('#selectH264Preset').value;
                config.H264Crf = parseInt(form.querySelector('#txtH264Crf').value || '0');

                config.EnableThrottling = form.querySelector('#chkEnableThrottle').checked;

                ApiClient.updateNamedConfiguration("encoding", config).then(function () {

                    updateEncoder(form);
                });
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

    function onSelectEncoderPathChange(e) {

        var page = $(this).parents('.page')[0];

        if (this.value == 'Custom') {
            page.querySelector('.fldEncoderPath').classList.remove('hide');
            page.querySelector('.txtEncoderPath').setAttribute('required', 'required');
        } else {
            page.querySelector('.fldEncoderPath').classList.add('hide');
            page.querySelector('.txtEncoderPath').removeAttribute('required');
        }
    }

    $(document).on('pageinit', "#encodingSettingsPage", function () {

        var page = this;

        page.querySelector('#selectVideoDecoder').addEventListener('change', function () {

            if (this.value == 'vaapi') {

                page.querySelector('.fldVaapiDevice').classList.remove('hide');
                page.querySelector('#txtVaapiDevice').setAttribute('required', 'required');

            } else {
                page.querySelector('.fldVaapiDevice').classList.add('hide');
                page.querySelector('#txtVaapiDevice').removeAttribute('required');
            }
        });

        $('#btnSelectEncoderPath', page).on("click.selectDirectory", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    includeFiles: true,
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

        page.querySelector('#selectEncoderPath').addEventListener('change', onSelectEncoderPathChange);

    }).on('pageshow', "#encodingSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getNamedConfiguration("encoding").then(function (config) {

            ApiClient.getSystemInfo().then(function (systemInfo) {

                page.querySelector('.fldSelectEncoderPathType').classList.remove('hide');
                loadPage(page, config, systemInfo);
            });
        });

    });

});
