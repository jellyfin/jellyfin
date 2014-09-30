(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkMovies', page).checked(config.EnableIntrosForMovies).checkboxradio('refresh');
        $('#chkEpisodes', page).checked(config.EnableIntrosForEpisodes).checkboxradio('refresh');

        $('#chkMyMovieTrailers', page).checked(config.EnableIntrosFromMoviesInLibrary).checkboxradio('refresh');

        $('#chkUpcomingTheaterTrailers', page).checked(config.EnableIntrosFromUpcomingTrailers).checkboxradio('refresh');
        $('#chkUpcomingDvdTrailers', page).checked(config.EnableIntrosFromUpcomingDvdMovies).checkboxradio('refresh');
        $('#chkUpcomingStreamingTrailers', page).checked(config.EnableIntrosFromUpcomingStreamingMovies).checkboxradio('refresh');

        $('#chkUnwatchedOnly', page).checked(!config.EnableIntrosForWatchedContent).checkboxradio('refresh');
        $('#chkEnableParentalControl', page).checked(config.EnableIntrosParentalControl).checkboxradio('refresh');

        $('#txtCustomIntrosPath', page).val(config.CustomIntroPath || '');

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageinit', "#cinemaModeConfigurationPage", function () {

        var page = this;

        $('#btnSelectCustomIntrosPath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtCustomIntrosPath', page).val(path);
                    }
                    picker.close();
                },

                header: Globalize.translate('HeaderSelectCustomIntrosPath')
            });
        });

    }).on('pageshow', "#cinemaModeConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getNamedConfiguration("cinemamode").done(function (config) {

            loadPage(page, config);

        });

    });

    function cinemaModeConfigurationPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            var page = $(form).parents('.page');

            ApiClient.getNamedConfiguration("cinemamode").done(function (config) {

                config.CustomIntroPath = $('#txtCustomIntrosPath', page).val();

                config.EnableIntrosForMovies = $('#chkMovies', page).checked();
                config.EnableIntrosForEpisodes = $('#chkEpisodes', page).checked();
                config.EnableIntrosFromMoviesInLibrary = $('#chkMyMovieTrailers', page).checked();
                config.EnableIntrosForWatchedContent = !$('#chkUnwatchedOnly', page).checked();
                config.EnableIntrosParentalControl = $('#chkEnableParentalControl', page).checked();

                config.EnableIntrosFromUpcomingTrailers = $('#chkUpcomingTheaterTrailers', page).checked();
                config.EnableIntrosFromUpcomingDvdMovies = $('#chkUpcomingDvdTrailers', page).checked();
                config.EnableIntrosFromUpcomingStreamingMovies = $('#chkUpcomingStreamingTrailers', page).checked();

                ApiClient.updateNamedConfiguration("cinemamode", config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.CinemaModeConfigurationPage = new cinemaModeConfigurationPage();

})(jQuery, document, window);
