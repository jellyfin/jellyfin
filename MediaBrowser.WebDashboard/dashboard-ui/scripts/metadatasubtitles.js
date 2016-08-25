define(['jQuery', 'fnchecked'], function ($) {

    function loadPage(page, config, languages) {

        $('#chkSubtitlesMovies', page).checked(config.DownloadMovieSubtitles);
        $('#chkSubtitlesEpisodes', page).checked(config.DownloadEpisodeSubtitles);

        $('#chkSkipIfGraphicalSubsPresent', page).checked(config.SkipIfEmbeddedSubtitlesPresent);
        $('#chkSkipIfAudioTrackPresent', page).checked(config.SkipIfAudioTrackMatches);
        $('#chkRequirePerfectMatch', page).checked(config.RequirePerfectMatch);

        $('#txtOpenSubtitleUsername', page).val(config.OpenSubtitlesUsername);
        $('#txtOpenSubtitlePassword', page).val('');

        populateLanguages(page, config, languages);

        Dashboard.hideLoadingMsg();
    }

    function populateLanguages(page, config, languages) {

        var html = '';

        for (var i = 0, length = languages.length; i < length; i++) {

            var culture = languages[i];

            html += '<label><input type="checkbox" is="emby-checkbox" class="chkLang" data-lang="' + culture.ThreeLetterISOLanguageName.toLowerCase() + '" /><span>' + culture.DisplayName + '</span></label>';
        }

        $('.downloadLanguages', page).html(html);

        var langs = config.DownloadLanguages || [];

        $('.chkLang', page).each(function () {

            this.checked = langs.indexOf(this.getAttribute('data-lang')) != -1;

        });
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration("subtitles").then(function (config) {

            config.DownloadMovieSubtitles = $('#chkSubtitlesMovies', form).checked();
            config.DownloadEpisodeSubtitles = $('#chkSubtitlesEpisodes', form).checked();

            config.SkipIfEmbeddedSubtitlesPresent = $('#chkSkipIfGraphicalSubsPresent', form).checked();
            config.SkipIfAudioTrackMatches = $('#chkSkipIfAudioTrackPresent', form).checked();
            config.RequirePerfectMatch = $('#chkRequirePerfectMatch', form).checked();

            config.OpenSubtitlesUsername = $('#txtOpenSubtitleUsername', form).val();

            var newPassword = $('#txtOpenSubtitlePassword', form).val();

            if (newPassword) {
                config.OpenSubtitlesPasswordHash = newPassword;
            }

            config.DownloadLanguages = $('.chkLang', form).get().filter(function (c) {

                return c.checked;

            }).map(function (c) {

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

});
