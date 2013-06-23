(function ($, document, window) {

    $(document).on('pageshow', "#clientSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            $('#txtMinResumePct', page).val(config.MinResumePct);
            $('#txtMaxResumePct', page).val(config.MaxResumePct);
            $('#txtMinResumeDuration', page).val(config.MinResumeDurationSeconds);

            $('input:first', page).focus();

            Dashboard.hideLoadingMsg();
        });
    });

    function clientSettingsPage() {

        var self = this;

        self.onSubmit = function () {
            var form = this;

            Dashboard.showLoadingMsg();

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

    window.ClientSettingsPage = new clientSettingsPage();

})($, document, window);
