(function ($, document, window) {

    function loadPage(page, config, languages) {

        $('#chkSubtitlesMovies', page).checked(config.SubtitleOptions.DownloadMovieSubtitles).checkboxradio("refresh");
        $('#chkSubtitlesEpisodes', page).checked(config.SubtitleOptions.DownloadEpisodeSubtitles).checkboxradio("refresh");

        $('#chkSkipIfGraphicalSubsPresent', page).checked(config.SubtitleOptions.SkipIfGraphicalSubtitlesPresent).checkboxradio("refresh");
        $('#chkSkipIfAudioTrackPresent', page).checked(config.SubtitleOptions.SkipIfAudioTrackMatches).checkboxradio("refresh");

        $('#txtOpenSubtitleUsername', page).val(config.SubtitleOptions.OpenSubtitlesUsername);
        $('#txtOpenSubtitlePassword', page).val('');

        populateLanguages(page, config, languages);

        Dashboard.hideLoadingMsg();
    }

    function populateLanguages(page, config, languages) {

        var html = '<div data-role="controlgroup" data-corners="false" style="margin:0;">';

        for (var i = 0, length = languages.length; i < length; i++) {

            var culture = languages[i];

            var id = "chkSubtitleLanguage" + i;

            html += '<label style="font-size:13px;" for="' + id + '">' + culture.DisplayName + '</label>';
            html += '<input class="chkLang" data-lang="' + culture.ThreeLetterISOLanguageName.toLowerCase() + '" type="checkbox" id="' + id + '" />';
        }

        html += '</div>';

        $('.downloadLanguages', page).html(html).trigger("create");

        var langs = config.SubtitleOptions.DownloadLanguages || [];

        $('.chkLang', page).each(function () {

            this.checked = langs.indexOf(this.getAttribute('data-lang')) != -1;

        }).checkboxradio('refresh');
    }

    $(document).on('pageshow', "#metadataSubtitlesPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();
        var promise2 = ApiClient.getCultures();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

    });

    function metadataSubtitlesPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.SubtitleOptions.DownloadMovieSubtitles = $('#chkSubtitlesMovies', form).checked();
                config.SubtitleOptions.DownloadEpisodeSubtitles = $('#chkSubtitlesEpisodes', form).checked();

                config.SubtitleOptions.SkipIfGraphicalSubtitlesPresent = $('#chkSkipIfGraphicalSubsPresent', form).checked();
                config.SubtitleOptions.SkipIfAudioTrackMatches = $('#chkSkipIfAudioTrackPresent', form).checked();

                config.SubtitleOptions.OpenSubtitlesUsername = $('#txtOpenSubtitleUsername', form).val();

                var newPassword = $('#txtOpenSubtitlePassword', form).val();
                
                if (newPassword) {
                    config.SubtitleOptions.OpenSubtitlesPasswordHash = newPassword;
                }

                config.SubtitleOptions.DownloadLanguages = $('.chkLang:checked', form).get().map(function (c) {

                    return c.getAttribute('data-lang');

                });

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.MetadataSubtitlesPage = new metadataSubtitlesPage();

})(jQuery, document, window);
