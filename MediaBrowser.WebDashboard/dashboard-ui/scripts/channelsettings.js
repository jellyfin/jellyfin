(function ($, document, window) {

    function loadPage(page, config) {

        $('#selectChannelResolution', page).val(config.ChannelOptions.PreferredStreamingWidth || '')
            .selectmenu("refresh");

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#channelSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });

    });

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().done(function (config) {

            // This should be null if empty
            config.ChannelOptions.PreferredStreamingWidth = $('#selectChannelResolution', form).val() || null;

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    window.ChannelSettingsPage = {
        onSubmit: onSubmit
    };

})(jQuery, document, window);
