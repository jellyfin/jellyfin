(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkEnableDebugEncodingLogging', page).checked(config.EnableDebugEncodingLogging).checkboxradio('refresh');
        
        $('.radioEncodingQuality', page).each(function() {

            this.checked = config.MediaEncodingQuality == this.value;

        }).checkboxradio('refresh');
        
        $('#txtTranscodingTempPath', page).val(config.TranscodingTempPath || '');

        var transcodingTempPath = config.TranscodingTempPath ? true : false;
        $('#chkEnableCustomTranscodingTempPath', page).checked(transcodingTempPath).checkboxradio("refresh");

        if (transcodingTempPath) {
            $('#fldEnterTranscodingTempPath', page).show();
            $('#txtTranscodingTempPath', page).attr("required", "required");
        } else {
            $('#fldEnterTranscodingTempPath', page).hide();
            $('#txtTranscodingTempPath', page).removeAttr("required");
        }

        $('#chkAllowUpscaling', page).checked(config.AllowVideoUpscaling).checkboxradio("refresh");

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

                header: "Select Transcoding Temporary Path",

                instruction: "Browse or enter the path to use for transcoding temporary files. The folder must be writeable."
            });
        });

        $('#chkEnableCustomTranscodingTempPath', page).on("change.showTranscodingTempPathText", function () {

            if (this.checked) {
                $('#fldEnterTranscodingTempPath', page).show();
                $('#txtTranscodingTempPath', page).attr("required", "required");
            } else {
                $('#fldEnterTranscodingTempPath', page).hide();
                $('#txtTranscodingTempPath', page).removeAttr("required");
            }

        });
    }).on('pageshow', "#encodingSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });
    });

    window.EncodingSettingsPage = {

        onSubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                if ($('#chkEnableCustomTranscodingTempPath', form).checked()) {
                    config.TranscodingTempPath = $('#txtTranscodingTempPath', form).val();
                } else {
                    config.TranscodingTempPath = '';
                }

                config.AllowVideoUpscaling = $('#chkAllowUpscaling', form).checked();
                config.EnableDebugEncodingLogging = $('#chkEnableDebugEncodingLogging', form).checked();
                config.MediaEncodingQuality = $('.radioEncodingQuality:checked', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }
    };

})(jQuery, document, window);
