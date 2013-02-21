var MetadataImagesPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function(result) {
            MetadataImagesPage.load(page, result);
        });
    },

    load: function (page, config) {

        $('#selectTmdbPersonImageDownloadSize', page).val(config.TmdbFetchedProfileSize).selectmenu("refresh");
        $('#selectTmdbPosterDownloadSize', page).val(config.TmdbFetchedPosterSize).selectmenu("refresh");
        $('#selectTmdbBackdropDownloadSize', page).val(config.TmdbFetchedBackdropSize).selectmenu("refresh");
        
        $('#chkRefreshItemImages', page).checked(config.RefreshItemImages).checkboxradio("refresh");
        $('#txtNumbackdrops', page).val(config.MaxBackdrops);

        $('#chkDownloadMovieArt', page).checked(config.DownloadMovieArt).checkboxradio("refresh");
        $('#chkDownloadMovieBanner', page).checked(config.DownloadMovieBanner).checkboxradio("refresh");
        $('#chkDownloadMovieDisc', page).checked(config.DownloadMovieDisc).checkboxradio("refresh");
        $('#chkDownloadMovieLogo', page).checked(config.DownloadMovieLogo).checkboxradio("refresh");
        $('#chkDownloadMovieThumb', page).checked(config.DownloadMovieThumb).checkboxradio("refresh");
        $('#chKDownloadTVArt', page).checked(config.DownloadTVArt).checkboxradio("refresh");
        $('#chkDownloadTVBanner', page).checked(config.DownloadTVBanner).checkboxradio("refresh");
        $('#chkDownloadTVLogo', page).checked(config.DownloadTVLogo).checkboxradio("refresh");
        $('#chkDownloadTVThumb', page).checked(config.DownloadTVThumb).checkboxradio("refresh");
        $('#chkDownloadSeasonBanner', page).checked(config.DownloadTVSeasonBanner).checkboxradio("refresh");
        $('#chkDownloadSeasonThumb', page).checked(config.DownloadTVSeasonThumb).checkboxradio("refresh");
        $('#chkDownloadSeasonBackdrops', page).checked(config.DownloadTVSeasonBackdrops).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    },

    onSubmit: function () {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().done(function (config) {

            config.TmdbFetchedProfileSize = $('#selectTmdbPersonImageDownloadSize', form).val();
            config.TmdbFetchedPosterSize = $('#selectTmdbPosterDownloadSize', form).val();
            config.TmdbFetchedBackdropSize = $('#selectTmdbBackdropDownloadSize', form).val();

            config.RefreshItemImages = $('#chkRefreshItemImages', form).checked();
            config.MaxBackdrops = $('#txtNumbackdrops', form).val();

            config.DownloadMovieArt = $('#chkDownloadMovieArt', form).checked();
            config.DownloadMovieBanner = $('#chkDownloadMovieBanner', form).checked();
            config.DownloadMovieDisc = $('#chkDownloadMovieDisc', form).checked();
            config.DownloadMovieLogo = $('#chkDownloadMovieLogo', form).checked();
            config.DownloadMovieThumb = $('#chkDownloadMovieThumb', form).checked();
            config.DownloadTVArt = $('#chKDownloadTVArt', form).checked();
            config.DownloadTVBanner = $('#chkDownloadTVBanner', form).checked();
            config.DownloadTVLogo = $('#chkDownloadTVLogo', form).checked();
            config.DownloadTVThumb = $('#chkDownloadTVThumb', form).checked();
            config.DownloadTVSeasonBanner = $('#chkDownloadSeasonBanner', form).checked();
            config.DownloadTVSeasonThumb = $('#chkDownloadSeasonThumb', form).checked();
            config.DownloadTVSeasonBackdrops = $('#chkDownloadSeasonBackdrops', form).checked();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#metadataImagesConfigurationPage", MetadataImagesPage.onPageShow);