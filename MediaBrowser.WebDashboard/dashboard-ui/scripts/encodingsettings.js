(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkEnableDebugEncodingLogging', page).checked(config.EnableDebugEncodingLogging).checkboxradio('refresh');
        
        $('.radioEncodingQuality', page).each(function() {

            this.checked = config.MediaEncodingQuality == this.value;

        }).checkboxradio('refresh');
        
        $('#chkAllowUpscaling', page).checked(config.AllowVideoUpscaling).checkboxradio("refresh");

        $('#txtDownMixAudioBoost', page).val(config.DownMixAudioBoost);

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#encodingSettingsPage", function () {

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

                config.AllowVideoUpscaling = $('#chkAllowUpscaling', form).checked();
                config.EnableDebugEncodingLogging = $('#chkEnableDebugEncodingLogging', form).checked();
                config.MediaEncodingQuality = $('.radioEncodingQuality:checked', form).val();
                config.DownMixAudioBoost = $('#txtDownMixAudioBoost', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }
    };

})(jQuery, document, window);
