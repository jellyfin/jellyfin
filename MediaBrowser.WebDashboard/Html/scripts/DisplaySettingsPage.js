var DisplaySettingsPage = {

    onPageShow: function () {
        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            $('#txtWeatherLocation', page).val(config.WeatherLocation);
            $('#txtMinResumePct', page).val(config.MinResumePct);
            $('#txtMaxResumePct', page).val(config.MaxResumePct);
            $('#txtMinResumeDuration', page).val(config.MinResumeDurationSeconds);
            $('#selectWeatherUnit', page).val(config.WeatherUnit).selectmenu("refresh");

            Dashboard.hideLoadingMsg();
        });

    },
    
    submit: function() {

        $('.btnSubmit', $.mobile.activePage)[0].click();

    },

    onSubmit: function () {
        var form = this;

        ApiClient.getServerConfiguration().done(function (config) {

            config.WeatherLocation = $('#txtWeatherLocation', form).val();
            config.WeatherUnit = $('#selectWeatherUnit', form).val();
            config.MinResumePct = $('#txtMinResumePct', form).val();
            config.MaxResumePct = $('#txtMaxResumePct', form).val();
            config.MinResumeDurationSeconds = $('#txtMinResumeDuration', form).val();

            ApiClient.updateServerConfiguration(config);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#displaySettingsPage", DisplaySettingsPage.onPageShow);
