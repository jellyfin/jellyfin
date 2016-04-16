define(['jQuery'], function ($) {

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

    function getTabs() {
        return [
        {
            href: 'cinemamodeconfiguration.html',
            name: Globalize.translate('TabCinemaMode')
        },
         {
             href: 'playbackconfiguration.html',
             name: Globalize.translate('TabResumeSettings')
         },
         {
             href: 'streamingsettings.html',
             name: Globalize.translate('TabStreaming')
         },
         {
             href: 'encodingsettings.html',
             name: Globalize.translate('TabTranscoding')
         }];
    }

    $(document).on('pageinit', "#playbackConfigurationPage", function () {

        $('.playbackConfigurationForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#playbackConfigurationPage", function () {

        LibraryMenu.setTabs('playback', 1, getTabs);
        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().then(function (config) {

            loadPage(page, config);

        });

    });

});
