(function ($, document, window) {

    function loadPage(page, config) {

        $('#selectChannelResolution', page).val(config.PreferredStreamingWidth || '');

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration("channels").then(function (config) {

            // This should be null if empty
            config.PreferredStreamingWidth = $('#selectChannelResolution', form).val() || null;

            ApiClient.updateNamedConfiguration("channels", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#channelSettingsPage", function () {

        var page = this;

        $('.channelSettingsForm', page).off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#channelSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getNamedConfiguration("channels").then(function (config) {

            loadPage(page, config);

        });

    });

})(jQuery, document, window);
