(function ($, document, window) {

    function loadPage(page, config, languages) {

        $('#chkSubtitlesMovies', page).checked(config.DownloadMovieSubtitles).checkboxradio("refresh");
        $('#chkSubtitlesEpisodes', page).checked(config.DownloadEpisodeSubtitles).checkboxradio("refresh");

        $('#chkSkipIfGraphicalSubsPresent', page).checked(config.SkipIfGraphicalSubtitlesPresent).checkboxradio("refresh");
        $('#chkSkipIfAudioTrackPresent', page).checked(config.SkipIfAudioTrackMatches).checkboxradio("refresh");

        $('#txtOpenSubtitleUsername', page).val(config.OpenSubtitlesUsername);
        $('#txtOpenSubtitlePassword', page).val('');

        populateLanguages(page, config, languages);

        Dashboard.hideLoadingMsg();
    }

    function populateLanguages(page, config, languages) {

        var html = '<div data-role="controlgroup" style="margin:0;">';

        for (var i = 0, length = languages.length; i < length; i++) {

            var culture = languages[i];

            var id = "chkSubtitleLanguage" + i;

            html += '<label style="font-size:13px;" for="' + id + '">' + culture.DisplayName + '</label>';
            html += '<input class="chkLang" data-lang="' + culture.ThreeLetterISOLanguageName.toLowerCase() + '" type="checkbox" id="' + id + '" />';
        }

        html += '</div>';

        $('.downloadLanguages', page).html(html).trigger('create');

        var langs = config.DownloadLanguages || [];

        $('.chkLang', page).each(function () {

            this.checked = langs.indexOf(this.getAttribute('data-lang')) != -1;

        }).checkboxradio('refresh');
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration("subtitles").then(function (config) {

            config.DownloadMovieSubtitles = $('#chkSubtitlesMovies', form).checked();
            config.DownloadEpisodeSubtitles = $('#chkSubtitlesEpisodes', form).checked();

            config.SkipIfGraphicalSubtitlesPresent = $('#chkSkipIfGraphicalSubsPresent', form).checked();
            config.SkipIfAudioTrackMatches = $('#chkSkipIfAudioTrackPresent', form).checked();

            config.OpenSubtitlesUsername = $('#txtOpenSubtitleUsername', form).val();

            var newPassword = $('#txtOpenSubtitlePassword', form).val();

            if (newPassword) {
                config.OpenSubtitlesPasswordHash = newPassword;
            }

            config.DownloadLanguages = $('.chkLang:checked', form).get().map(function (c) {

                return c.getAttribute('data-lang');

            });

            ApiClient.updateNamedConfiguration("subtitles", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#metadataSubtitlesPage", function () {

        $('.metadataSubtitlesForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#metadataSubtitlesPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getNamedConfiguration("subtitles");
        var promise2 = ApiClient.getCultures();

        Promise.all([promise1, promise2]).then(function (responses) {

            loadPage(page, responses[0], responses[1]);

        });

    });

})(jQuery, document, window);
