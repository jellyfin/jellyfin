(function ($, document, window) {

    function loadPage(page, config, systemInfo) {

        $('#chkSubtitlesMovies', page).checked(config.SubtitleOptions.DownloadMovieSubtitles).checkboxradio("refresh");
        $('#chkSubtitlesEpisodes', page).checked(config.SubtitleOptions.DownloadEpisodeSubtitles).checkboxradio("refresh");

        $('#chkRequireExternalSubtitles', page).checked(config.SubtitleOptions.RequireExternalSubtitles).checkboxradio("refresh");

        $('#txtOpenSubtitleUsername', page).val(config.SubtitleOptions.OpenSubtitlesUsername);
        $('#txtOpenSubtitlePassword', page).val('');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#metadataSubtitlesPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();
        var promise2 = ApiClient.getSystemInfo();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

    }).on('pageinit', "#metadataSubtitlesPage", function () {

        var page = this;


    });

    function metadataSubtitlesPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.SubtitleOptions.DownloadMovieSubtitles = $('#chkSubtitlesMovies', form).checked();
                config.SubtitleOptions.DownloadEpisodeSubtitles = $('#chkSubtitlesEpisodes', form).checked();

                config.SubtitleOptions.RequireExternalSubtitles = $('#chkRequireExternalSubtitles', form).checked();

                config.SubtitleOptions.OpenSubtitlesUsername = $('#txtOpenSubtitleUsername', form).val();

                config.SubtitleOptions.OpenSubtitlesPasswordHash = $('#txtOpenSubtitlePassword', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.MetadataSubtitlesPage = new metadataSubtitlesPage();

})(jQuery, document, window);
