var MetadataImagesPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function(result) {
            MetadataImagesPage.load(page, result);
        });
    },

    load: function (page, config) {

        // Movie options
        $('#txtMaxMovieBackdrops', page).val(config.MovieOptions.MaxBackdrops);
        $('#txtMinMovieBackdropDownloadWidth', page).val(config.MovieOptions.MinBackdropWidth);
        
        // Tv options
        $('#txtMaxTvBackdrops', page).val(config.TvOptions.MaxBackdrops);
        $('#txtMinTvBackdropDownloadWidth', page).val(config.TvOptions.MinBackdropWidth);

        // Music options
        $('#txtMaxMusicBackdrops', page).val(config.MusicOptions.MaxBackdrops);
        $('#txtMinMusicBackdropDownloadWidth', page).val(config.MusicOptions.MinBackdropWidth);

        // Game options
        $('#txtMaxGameBackdrops', page).val(config.GameOptions.MaxBackdrops);
        $('#txtMinGameBackdropDownloadWidth', page).val(config.GameOptions.MinBackdropWidth);

        // Book options
        $('#txtMaxBookBackdrops', page).val(config.BookOptions.MaxBackdrops);
        $('#txtMinBookBackdropDownloadWidth', page).val(config.BookOptions.MinBackdropWidth);

        $('#chkDownloadMovieArt', page).checked(config.DownloadMovieImages.Art).checkboxradio("refresh");
        $('#chkDownloadMovieBackdrops', page).checked(config.DownloadMovieImages.Backdrops).checkboxradio("refresh");
        $('#chkDownloadMovieBanner', page).checked(config.DownloadMovieImages.Banner).checkboxradio("refresh");
        $('#chkDownloadMovieDisc', page).checked(config.DownloadMovieImages.Disc).checkboxradio("refresh");
        $('#chkDownloadMovieLogo', page).checked(config.DownloadMovieImages.Logo).checkboxradio("refresh");
        $('#chkDownloadMovieThumb', page).checked(config.DownloadMovieImages.Thumb).checkboxradio("refresh");
        
        $('#chKDownloadTVArt', page).checked(config.DownloadSeriesImages.Art).checkboxradio("refresh");
        $('#chkDownloadTVBackdrops', page).checked(config.DownloadSeriesImages.Backdrops).checkboxradio("refresh");
        $('#chkDownloadTVBanner', page).checked(config.DownloadSeriesImages.Banner).checkboxradio("refresh");
        $('#chkDownloadTVLogo', page).checked(config.DownloadSeriesImages.Logo).checkboxradio("refresh");
        $('#chkDownloadTVThumb', page).checked(config.DownloadSeriesImages.Thumb).checkboxradio("refresh");
        
        $('#chkDownloadSeasonBanner', page).checked(config.DownloadSeasonImages.Banner).checkboxradio("refresh");
        $('#chkDownloadSeasonThumb', page).checked(config.DownloadSeasonImages.Thumb).checkboxradio("refresh");
        $('#chkDownloadSeasonBackdrops', page).checked(config.DownloadSeasonImages.Backdrops).checkboxradio("refresh");
        
        $('#chkDownloadArtistThumb', page).checked(config.DownloadMusicArtistImages.Primary).checkboxradio("refresh");
        $('#chkDownloadArtistBackdrops', page).checked(config.DownloadMusicArtistImages.Backdrops).checkboxradio("refresh");
        $('#chkDownloadArtistLogo', page).checked(config.DownloadMusicArtistImages.Logo).checkboxradio("refresh");
        $('#chkDownloadArtistBanner', page).checked(config.DownloadMusicArtistImages.Banner).checkboxradio("refresh");

        $('#chkDownloadAlbumPrimary', page).checked(config.DownloadMusicAlbumImages.Primary).checkboxradio("refresh");
        $('#chkDownloadAlbumBackdrops', page).checked(config.DownloadMusicAlbumImages.Backdrops).checkboxradio("refresh");
        $('#chkMusicAlbumDisc', page).checked(config.DownloadMusicAlbumImages.Disc).checkboxradio("refresh");

        $('#selectImageSavingConvention', page).val(config.ImageSavingConvention).selectmenu("refresh");

        Dashboard.hideLoadingMsg();
    },

    onSubmit: function () {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.ImageSavingConvention = $('#selectImageSavingConvention', form).val();

            // Movie options
            config.MovieOptions.MaxBackdrops = $('#txtMaxMovieBackdrops', form).val();
            config.MovieOptions.MinBackdropWidth = $('#txtMinMovieBackdropDownloadWidth', form).val();
            config.DownloadMovieImages.Art = $('#chkDownloadMovieArt', form).checked();
            config.DownloadMovieImages.Backdrops = $('#chkDownloadMovieBackdrops', form).checked();
            config.DownloadMovieImages.Banner = $('#chkDownloadMovieBanner', form).checked();
            config.DownloadMovieImages.Disc = $('#chkDownloadMovieDisc', form).checked();
            config.DownloadMovieImages.Logo = $('#chkDownloadMovieLogo', form).checked();
            config.DownloadMovieImages.Thumb = $('#chkDownloadMovieThumb', form).checked();

            // Tv options
            config.TvOptions.MaxBackdrops = $('#txtMaxTvBackdrops', form).val();
            config.TvOptions.MinBackdropWidth = $('#txtMinTvBackdropDownloadWidth', form).val();
            config.DownloadSeriesImages.Art = $('#chKDownloadTVArt', form).checked();
            config.DownloadSeriesImages.Backdrops = $('#chkDownloadMovieBackdrops', form).checked();
            config.DownloadSeriesImages.Banner = $('#chkDownloadTVBanner', form).checked();
            config.DownloadSeriesImages.Logo = $('#chkDownloadTVLogo', form).checked();
            config.DownloadSeriesImages.Thumb = $('#chkDownloadTVThumb', form).checked();
            config.DownloadSeasonImages.Banner = $('#chkDownloadSeasonBanner', form).checked();
            config.DownloadSeasonImages.Thumb = $('#chkDownloadSeasonThumb', form).checked();
            config.DownloadSeasonImages.Backdrops = $('#chkDownloadSeasonBackdrops', form).checked();

            // Music options
            config.MusicOptions.MaxBackdrops = $('#txtMaxMusicBackdrops', form).val();
            config.MusicOptions.MinBackdropWidth = $('#txtMinMusicBackdropDownloadWidth', form).val();
            config.DownloadMusicArtistImages.Backdrops = $('#chkDownloadArtistBackdrops', form).checked();
            config.DownloadMusicArtistImages.Logo = $('#chkDownloadArtistLogo', form).checked();
            config.DownloadMusicArtistImages.Primary = $('#chkDownloadArtistThumb', form).checked();
            config.DownloadMusicArtistImages.Banner = $('#chkDownloadArtistBanner', form).checked();
            config.DownloadMusicAlbumImages.Primary = $('#chkDownloadAlbumPrimary', form).checked();
            config.DownloadMusicAlbumImages.Backdrops = $('#chkDownloadAlbumBackdrops', form).checked();
            config.DownloadMusicAlbumImages.Disc = $('#chkMusicAlbumDisc', form).checked();

            // Game options
            config.GameOptions.MaxBackdrops = $('#txtMaxGameBackdrops', form).val();
            config.GameOptions.MinBackdropWidth = $('#txtMinGameBackdropDownloadWidth', form).val();

            // Book options
            config.BookOptions.MaxBackdrops = $('#txtMaxBookBackdrops', form).val();
            config.BookOptions.MinBackdropWidth = $('#txtMinBookBackdropDownloadWidth', form).val();


            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#metadataImagesConfigurationPage", MetadataImagesPage.onPageShow);