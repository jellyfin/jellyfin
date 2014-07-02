(function ($, document, window) {

    function loadPage(page, config, languageOptions) {

        $('#txtServerName', page).val(config.ServerName || '');

        $('#selectLocalizationLanguage', page).html(languageOptions.map(function (l) {

            return '<option value="' + l.Value + '">' + l.Name + '</option>';

        })).val(config.UICulture).selectmenu('refresh');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#dashboardGeneralPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();

        var promise2 = ApiClient.getJSON(ApiClient.getUrl("Localization/Options"));

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

    });

    window.DashboardGeneralPage = {

        onSubmit: function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.ServerName = $('#txtServerName', form).val();
                config.UICulture = $('#selectLocalizationLanguage', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }

    };

})(jQuery, document, window);
