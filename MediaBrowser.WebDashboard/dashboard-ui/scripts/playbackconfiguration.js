(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtMinResumePct', page).val(config.MinResumePct);
        $('#txtMaxResumePct', page).val(config.MaxResumePct);
        $('#txtMinResumeDuration', page).val(config.MinResumeDurationSeconds);

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().then(function (config) {

            config.MinResumePct = $('#txtMinResumePct', form).val();
            config.MaxResumePct = $('#txtMaxResumePct', form).val();
            config.MinResumeDurationSeconds = $('#txtMinResumeDuration', form).val();

            ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#playbackConfigurationPage", function () {

        $('.playbackConfigurationForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#playbackConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().then(function (config) {

            loadPage(page, config);

        });

    });

})(jQuery, document, window);
