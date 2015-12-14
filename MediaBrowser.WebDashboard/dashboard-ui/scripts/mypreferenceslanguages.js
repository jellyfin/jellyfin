(function ($, window, document) {

    function populateLanguages(select, languages) {

        var html = "";

        html += "<option value=''></option>";

        for (var i = 0, length = languages.length; i < length; i++) {

            var culture = languages[i];

            html += "<option value='" + culture.ThreeLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        $(select).html(html);
    }

    function loadForm(page, user, loggedInUser, allCulturesPromise) {

        allCulturesPromise.then(function (allCultures) {

            populateLanguages($('#selectAudioLanguage', page), allCultures);
            populateLanguages($('#selectSubtitleLanguage', page), allCultures);

            $('#selectAudioLanguage', page).val(user.Configuration.AudioLanguagePreference || "");
            $('#selectSubtitleLanguage', page).val(user.Configuration.SubtitleLanguagePreference || "");
        });

        $('#selectSubtitlePlaybackMode', page).val(user.Configuration.SubtitleMode || "").trigger('change');

        page.querySelector('.chkPlayDefaultAudioTrack').checked = user.Configuration.PlayDefaultAudioTrack || false;
        page.querySelector('.chkEnableCinemaMode').checked = AppSettings.enableCinemaMode();
        page.querySelector('.chkEnableChromecastAc3').checked = AppSettings.enableChromecastAc3();
        page.querySelector('.chkExternalVideoPlayer').checked = AppSettings.enableExternalPlayers();

        var bitrateOptions = MediaPlayer.getVideoQualityOptions().map(function (i) {

            return '<option value="' + i.bitrate + '">' + i.name + '</option>';

        }).join('');

        bitrateOptions = '<option value="">' + Globalize.translate('OptionAutomatic') + '</option>' + bitrateOptions;

        $('#selectMaxBitrate', page).html(bitrateOptions);

        if (AppSettings.enableAutomaticBitrateDetection()) {
            $('#selectMaxBitrate', page).val('');
        } else {
            $('#selectMaxBitrate', page).val(AppSettings.maxStreamingBitrate());
        }

        $('#selectMaxChromecastBitrate', page).val(AppSettings.maxChromecastBitrate());

        Dashboard.hideLoadingMsg();
    }

    function loadPage(page) {

        Dashboard.showLoadingMsg();

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        var promise1 = ApiClient.getUser(userId);

        var promise2 = Dashboard.getCurrentUser();

        var allCulturesPromise = ApiClient.getCultures();

        Promise.all([promise1, promise2]).then(function (responses) {

            loadForm(page, responses[1], responses[0], allCulturesPromise);

        });

        ApiClient.getNamedConfiguration("cinemamode").then(function (cinemaConfig) {

            if (cinemaConfig.EnableIntrosForMovies || cinemaConfig.EnableIntrosForEpisodes) {
                $('.cinemaModeOptions', page).show();
            } else {
                $('.cinemaModeOptions', page).hide();
            }
        });
    }

    function saveUser(page, user) {

        user.Configuration.AudioLanguagePreference = $('#selectAudioLanguage', page).val();
        user.Configuration.SubtitleLanguagePreference = $('#selectSubtitleLanguage', page).val();

        user.Configuration.SubtitleMode = $('#selectSubtitlePlaybackMode', page).val();
        user.Configuration.PlayDefaultAudioTrack = page.querySelector('.chkPlayDefaultAudioTrack').checked;

        AppSettings.enableCinemaMode(page.querySelector('.chkEnableCinemaMode').checked);

        ApiClient.updateUserConfiguration(user.Id, user.Configuration).then(function () {

            Dashboard.hideLoadingMsg();
            Dashboard.alert(Globalize.translate('SettingsSaved'));

        }, function () {
            Dashboard.hideLoadingMsg();
        });
    }

    function onSubmit() {

        var page = $(this).parents('.page')[0];

        Dashboard.showLoadingMsg();

        AppSettings.enableExternalPlayers(page.querySelector('.chkExternalVideoPlayer').checked);

        if ($('#selectMaxBitrate', page).val()) {
            AppSettings.maxStreamingBitrate($('#selectMaxBitrate', page).val());
            AppSettings.enableAutomaticBitrateDetection(false);
        } else {
            AppSettings.enableAutomaticBitrateDetection(true);
        }

        AppSettings.maxChromecastBitrate($('#selectMaxChromecastBitrate', page).val());
        AppSettings.enableChromecastAc3(page.querySelector('.chkEnableChromecastAc3').checked);

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        ApiClient.getUser(userId).then(function (result) {

            saveUser(page, result);

        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#languagePreferencesPage", function () {

        var page = this;

        $('#selectSubtitlePlaybackMode', page).on('change', function () {

            $('.subtitlesHelp', page).hide();
            $('.subtitles' + this.value + 'Help', page).show();
        });

        $('.languagePreferencesForm').off('submit', onSubmit).on('submit', onSubmit);


    }).on('pageshow', "#languagePreferencesPage", function () {

        var page = this;

        if (AppInfo.supportsExternalPlayers) {
            $('.fldExternalPlayer', page).show();
        } else {
            $('.fldExternalPlayer', page).hide();
        }

        if (AppInfo.supportsExternalPlayerMenu) {
            $('.labelNativeExternalPlayers', page).show();
            $('.labelGenericExternalPlayers', page).hide();
        } else {
            $('.labelGenericExternalPlayers', page).show();
            $('.labelNativeExternalPlayers', page).hide();
        }

        loadPage(page);
    });

})(jQuery, window, document);