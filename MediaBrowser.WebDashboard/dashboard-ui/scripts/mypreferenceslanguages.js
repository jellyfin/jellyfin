define(['appSettings', 'userSettings', 'jQuery'], function (appSettings, userSettings, $) {

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
            page.querySelector('.chkEpisodeAutoPlay').checked = user.Configuration.EnableNextEpisodeAutoPlay || false;
        });

        $('#selectSubtitlePlaybackMode', page).val(user.Configuration.SubtitleMode || "").trigger('change');

        page.querySelector('.chkPlayDefaultAudioTrack').checked = user.Configuration.PlayDefaultAudioTrack || false;
        page.querySelector('.chkEnableCinemaMode').checked = userSettings.enableCinemaMode();
        page.querySelector('.chkExternalVideoPlayer').checked = appSettings.enableExternalPlayers();

        require(['qualityoptions'], function (qualityoptions) {

            var bitrateOptions = qualityoptions.getVideoQualityOptions(appSettings.maxStreamingBitrate()).map(function (i) {

                return '<option value="' + i.bitrate + '">' + i.name + '</option>';

            }).join('');

            bitrateOptions = '<option value="">' + Globalize.translate('OptionAutomatic') + '</option>' + bitrateOptions;

            $('#selectMaxBitrate', page).html(bitrateOptions);
            $('#selectMaxChromecastBitrate', page).html(bitrateOptions);

            if (appSettings.enableAutomaticBitrateDetection()) {
                $('#selectMaxBitrate', page).val('');
            } else {
                $('#selectMaxBitrate', page).val(appSettings.maxStreamingBitrate());
            }

            $('#selectMaxChromecastBitrate', page).val(appSettings.maxChromecastBitrate());

            Dashboard.hideLoadingMsg();
        });
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
        user.Configuration.EnableNextEpisodeAutoPlay = page.querySelector('.chkEpisodeAutoPlay').checked;
        userSettings.enableCinemaMode(page.querySelector('.chkEnableCinemaMode').checked);

        return ApiClient.updateUserConfiguration(user.Id, user.Configuration);
    }

    function save(page) {

        appSettings.enableExternalPlayers(page.querySelector('.chkExternalVideoPlayer').checked);

        if ($('#selectMaxBitrate', page).val()) {
            appSettings.maxStreamingBitrate($('#selectMaxBitrate', page).val());
            appSettings.enableAutomaticBitrateDetection(false);
        } else {
            appSettings.enableAutomaticBitrateDetection(true);
        }

        appSettings.maxChromecastBitrate($('#selectMaxChromecastBitrate', page).val());

        var userId = getParameterByName('userId') || Dashboard.getCurrentUserId();

        if (!AppInfo.enableAutoSave) {
            Dashboard.showLoadingMsg();
        }

        ApiClient.getUser(userId).then(function (result) {

            saveUser(page, result).then(function () {

                Dashboard.hideLoadingMsg();
                if (!AppInfo.enableAutoSave) {
                    require(['toast'], function (toast) {
                        toast(Globalize.translate('SettingsSaved'));
                    });
                }

            }, function () {
                Dashboard.hideLoadingMsg();
            });

        });
    }

    function onSubmit() {

        var page = $(this).parents('.page')[0];

        save(page);

        // Disable default form submission
        return false;
    }

    pageIdOn('pageinit', "languagePreferencesPage", function () {

        var page = this;

        $('#selectSubtitlePlaybackMode', page).on('change', function () {

            $('.subtitlesHelp', page).hide();
            $('.subtitles' + this.value + 'Help', page).show();
        });

        $('.languagePreferencesForm').off('submit', onSubmit).on('submit', onSubmit);

        if (AppInfo.enableAutoSave) {
            page.querySelector('.btnSave').classList.add('hide');
        } else {
            page.querySelector('.btnSave').classList.remove('hide');
        }
    });

    pageIdOn('pageshow', "languagePreferencesPage", function () {

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

    pageIdOn('pagebeforehide', "languagePreferencesPage", function () {

        var page = this;

        if (AppInfo.enableAutoSave) {
            save(page);
        }
    });

});