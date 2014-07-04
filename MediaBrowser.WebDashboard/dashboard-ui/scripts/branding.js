(function ($, document, window) {

    var brandingConfigKey = "branding";

    function loadPage(page, config) {

        $('#txtLoginDisclaimer', page).val(config.LoginDisclaimer || '');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#dashboardBrandingPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getNamedConfiguration(brandingConfigKey).done(function (config) {

            loadPage(page, config);
        });

    });

    window.BrandingPage = {

        onSubmit: function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getNamedConfiguration(brandingConfigKey).done(function (config) {

                config.LoginDisclaimer = $('#txtLoginDisclaimer', form).val();

                ApiClient.updateNamedConfiguration(brandingConfigKey, config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }

    };

})(jQuery, document, window);
