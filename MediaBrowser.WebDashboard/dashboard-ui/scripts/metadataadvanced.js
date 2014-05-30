var AdvancedMetadataConfigurationPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (configuration) {

            AdvancedMetadataConfigurationPage.load(page, configuration);

        });
    },

    load: function (page, config) {

        $('#chkChaptersMovies', page).checked(config.EnableMovieChapterImageExtraction).checkboxradio("refresh");
        $('#chkChaptersEpisodes', page).checked(config.EnableEpisodeChapterImageExtraction).checkboxradio("refresh");
        $('#chkChaptersOtherVideos', page).checked(config.EnableOtherVideoChapterImageExtraction).checkboxradio("refresh");

        $('#chkEnableTmdbPersonUpdates', page).checked(config.EnableTmdbUpdates).checkboxradio("refresh");
        $('#chkEnableTvdbUpdates', page).checked(config.EnableTvDbUpdates).checkboxradio("refresh");
        $('#chkEnableFanartUpdates', page).checked(config.EnableFanArtUpdates).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    },

    onSubmit: function () {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.EnableMovieChapterImageExtraction = $('#chkChaptersMovies', form).checked();
            config.EnableEpisodeChapterImageExtraction = $('#chkChaptersEpisodes', form).checked();
            config.EnableOtherVideoChapterImageExtraction = $('#chkChaptersOtherVideos', form).checked();

            config.EnableTvDbUpdates = $('#chkEnableTvdbUpdates', form).checked();
            config.EnableTmdbUpdates = $('#chkEnableTmdbPersonUpdates', form).checked();
            config.EnableFanArtUpdates = $('#chkEnableFanartUpdates', form).checked();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#advancedMetadataConfigurationPage", AdvancedMetadataConfigurationPage.onPageShow);