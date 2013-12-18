var MetadataImageExtractionPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (result) {
            MetadataImageExtractionPage.load(page, result);
        });
    },

    load: function (page, config) {

        $('#chkVIdeoImages', page).checked(config.EnableVideoImageExtraction).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    },

    onSubmit: function () {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.EnableVideoImageExtraction = $('#chkVIdeoImages', form).checked();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#metadataImageExtractionPage", MetadataImageExtractionPage.onPageShow);