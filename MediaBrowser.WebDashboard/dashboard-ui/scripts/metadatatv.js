var MetadataTVPage = {

    onPageShow: function () {
        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (result) {

            MetadataTVPage.load(page, result);
        });
    },

    load: function (page, config) {

        var chkEnableTvdbUpdates = $('#chkEnableTvdbUpdates', page).checked(config.EnableTvDbUpdates).checkboxradio("refresh");
        var chkCreateMissingEpisodes = $('#chkCreateMissingEpisodes', page).checked(config.CreateVirtualMissingEpisodes).checkboxradio("refresh");
        var chkCreateFutureEpisodes = $('#chkCreateFutureEpisodes', page).checked(config.CreateVirtualFutureEpisodes).checkboxradio("refresh");

        if (config.EnableInternetProviders) {
            chkEnableTvdbUpdates.checkboxradio("enable");
            chkCreateMissingEpisodes.checkboxradio("enable");
            chkCreateFutureEpisodes.checkboxradio("enable");
        } else {
            chkEnableTvdbUpdates.checkboxradio("disable");
            chkCreateMissingEpisodes.checkboxradio("disable");
            chkCreateFutureEpisodes.checkboxradio("disable");
        }

        Dashboard.hideLoadingMsg();
    },

    onSubmit: function () {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.EnableTvDbUpdates = $('#chkEnableTvdbUpdates', form).checked();
            config.CreateVirtualMissingEpisodes = $('#chkCreateMissingEpisodes', form).checked();
            config.CreateVirtualFutureEpisodes = $('#chkCreateFutureEpisodes', form).checked();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#metadataTvPage", MetadataTVPage.onPageShow);