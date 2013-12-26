(function ($, document, window) {

    $(document).on('pageshow', "#appsWeatherPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            $('#txtWeatherLocation', page).val(config.WeatherLocation);
            $('#selectWeatherUnit', page).val(config.WeatherUnit).selectmenu("refresh");

            $('input:first', page).focus();

            Dashboard.hideLoadingMsg();
        });
    });

    window.AppsWeatherPage = {

        onSubmit: function () {
            var form = this;

            Dashboard.showLoadingMsg();

            ApiClient.getServerConfiguration().done(function (config) {

                config.WeatherLocation = $('#txtWeatherLocation', form).val();
                config.WeatherUnit = $('#selectWeatherUnit', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        }

    };

})($, document, window);