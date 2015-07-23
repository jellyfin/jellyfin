(function ($, document, window) {

    function loadPage(page, config) {

        $('.liveTvSettingsForm', page).show();
        $('.noLiveTvServices', page).hide();

        $('#selectGuideDays', page).val(config.GuideDays || '').selectmenu('refresh');

        $('#chkMovies', page).checked(config.EnableMovieProviders).checkboxradio("refresh");

        $('#txtRecordingPath', page).val(config.RecordingPath || '');

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

			Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getNamedConfiguration("livetv").done(function (config) {

                config.GuideDays = $('#selectGuideDays', form).val() || null;
                config.EnableMovieProviders = $('#chkMovies', form).checked();
                config.RecordingPath = $('#txtRecordingPath', form).val() || null;

                ApiClient.updateNamedConfiguration("livetv", config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
    }

    $(document).on('pageinitdepends', "#liveTvSettingsPage", function () {

        var page = this;

        $('.liveTvSettingsForm').off('submit', onSubmit).on('submit', onSubmit);

        $('#btnSelectRecordingPath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtRecordingPath', page).val(path);
                    }
                    picker.close();
                }
            });
        });

    }).on('pageshowready', "#liveTvSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getNamedConfiguration("livetv").done(function (config) {

            loadPage(page, config);

        });

    });

})(jQuery, document, window);
