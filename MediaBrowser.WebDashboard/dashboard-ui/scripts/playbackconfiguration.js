(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtMinResumePct', page).val(config.MinResumePct);
        $('#txtMaxResumePct', page).val(config.MaxResumePct);
        $('#txtMinResumeDuration', page).val(config.MinResumeDurationSeconds);

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#playbackConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });

    });

    function playbackConfigurationPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.MinResumePct = $('#txtMinResumePct', form).val();
                config.MaxResumePct = $('#txtMaxResumePct', form).val();
                config.MinResumeDurationSeconds = $('#txtMinResumeDuration', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.PlaybackConfigurationPage = new playbackConfigurationPage();

})(jQuery, document, window);
