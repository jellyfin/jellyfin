define(['appSettings', 'userSettingsBuilder'], function (appSettings, userSettingsBuilder) {

    function populateLanguages(select, languages) {

        var html = "";

        html += "<option value=''></option>";

        for (var i = 0, length = languages.length; i < length; i++) {

            var culture = languages[i];

            html += "<option value='" + culture.ThreeLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        select.innerHTML = html;
    }

    return function (view, params) {

        var userId = params.userId || Dashboard.getCurrentUserId();
        var userSettings = new userSettingsBuilder();
        var userSettingsLoaded;

        function loadForm(page, user, loggedInUser, allCulturesPromise) {

            userSettings.setUserInfo(userId, ApiClient).then(function () {
                userSettingsLoaded = true;
                allCulturesPromise.then(function (allCultures) {

                    populateLanguages(page.querySelector('#selectAudioLanguage'), allCultures);
                    populateLanguages(page.querySelector('#selectSubtitleLanguage'), allCultures);

                    page.querySelector('#selectAudioLanguage', page).value = user.Configuration.AudioLanguagePreference || "";
                    page.querySelector('#selectSubtitleLanguage', page).value = user.Configuration.SubtitleLanguagePreference || "";
                    page.querySelector('.chkEpisodeAutoPlay').checked = user.Configuration.EnableNextEpisodeAutoPlay || false;
                });

                page.querySelector('#selectSubtitlePlaybackMode').value = user.Configuration.SubtitleMode || "";

                page.querySelector('.chkPlayDefaultAudioTrack').checked = user.Configuration.PlayDefaultAudioTrack || false;
                page.querySelector('.chkEnableCinemaMode').checked = userSettings.enableCinemaMode();
                page.querySelector('.chkExternalVideoPlayer').checked = appSettings.enableExternalPlayers();

                require(['qualityoptions'], function (qualityoptions) {

                    var bitrateOptions = qualityoptions.getVideoQualityOptions(appSettings.maxStreamingBitrate()).map(function (i) {

                        return '<option value="' + i.bitrate + '">' + i.name + '</option>';

                    }).join('');

                    bitrateOptions = '<option value="">' + Globalize.translate('OptionAutomatic') + '</option>' + bitrateOptions;

                    page.querySelector('#selectMaxBitrate').innerHTML = bitrateOptions;
                    page.querySelector('#selectMaxChromecastBitrate').innerHTML = bitrateOptions;

                    if (appSettings.enableAutomaticBitrateDetection()) {
                        page.querySelector('#selectMaxBitrate').value = '';
                    } else {
                        page.querySelector('#selectMaxBitrate').value = appSettings.maxStreamingBitrate();
                    }

                    page.querySelector('#selectMaxChromecastBitrate').value = appSettings.maxChromecastBitrate() || '';

                    Dashboard.hideLoadingMsg();
                });
            });
        }

        function loadPage(page) {

            Dashboard.showLoadingMsg();

            var promise1 = ApiClient.getUser(userId);

            var promise2 = Dashboard.getCurrentUser();

            var allCulturesPromise = ApiClient.getCultures();

            Promise.all([promise1, promise2]).then(function (responses) {

                loadForm(page, responses[1], responses[0], allCulturesPromise);

            });

            ApiClient.getNamedConfiguration("cinemamode").then(function (cinemaConfig) {

                if (cinemaConfig.EnableIntrosForMovies || cinemaConfig.EnableIntrosForEpisodes) {
                    page.querySelector('.cinemaModeOptions').classList.remove('hide');
                } else {
                    page.querySelector('.cinemaModeOptions').classList.add('hide');
                }
            });
        }

        function saveUser(page, user) {

            user.Configuration.AudioLanguagePreference = page.querySelector('#selectAudioLanguage').value;
            user.Configuration.SubtitleLanguagePreference = page.querySelector('#selectSubtitleLanguage').value;

            user.Configuration.SubtitleMode = page.querySelector('#selectSubtitlePlaybackMode').value;
            user.Configuration.PlayDefaultAudioTrack = page.querySelector('.chkPlayDefaultAudioTrack').checked;
            user.Configuration.EnableNextEpisodeAutoPlay = page.querySelector('.chkEpisodeAutoPlay').checked;
            if (userSettingsLoaded) {
                userSettings.enableCinemaMode(page.querySelector('.chkEnableCinemaMode').checked);
            }

            return ApiClient.updateUserConfiguration(user.Id, user.Configuration);
        }

        function save(page) {

            appSettings.enableExternalPlayers(page.querySelector('.chkExternalVideoPlayer').checked);

            if (page.querySelector('#selectMaxBitrate').value) {
                appSettings.maxStreamingBitrate(page.querySelector('#selectMaxBitrate').value);
                appSettings.enableAutomaticBitrateDetection(false);
            } else {
                appSettings.enableAutomaticBitrateDetection(true);
            }

            appSettings.maxChromecastBitrate(page.querySelector('#selectMaxChromecastBitrate').value);

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

        view.querySelector('#selectSubtitlePlaybackMode').addEventListener('change', function () {

            var subtitlesHelp = view.querySelectorAll('.subtitlesHelp');
            for (var i = 0, length = subtitlesHelp.length; i < length; i++) {
                subtitlesHelp[i].classList.add('hide');
            }
            view.querySelector('.subtitles' + this.value + 'Help').classList.remove('hide');
        });

        view.querySelector('.languagePreferencesForm').addEventListener('submit', function (e) {
            save(view);

            // Disable default form submission
            e.preventDefault();
            return false;
        });

        if (AppInfo.enableAutoSave) {
            view.querySelector('.btnSave').classList.add('hide');
        } else {
            view.querySelector('.btnSave').classList.remove('hide');
        }

        view.addEventListener('viewshow', function () {

            if (AppInfo.supportsExternalPlayers) {
                view.querySelector('.fldExternalPlayer').classList.remove('hide');
            } else {
                view.querySelector('.fldExternalPlayer').classList.add('hide');
            }

            if (AppInfo.supportsExternalPlayerMenu) {
                view.querySelector('.labelNativeExternalPlayers').classList.remove('hide');
                view.querySelector('.labelGenericExternalPlayers').classList.add('hide');
            } else {
                view.querySelector('.labelGenericExternalPlayers').classList.remove('hide');
                view.querySelector('.labelNativeExternalPlayers').classList.add('hide');
            }

            loadPage(view);
        });

        view.addEventListener('viewbeforehide', function () {
            var page = this;

            if (AppInfo.enableAutoSave) {
                save(page);
            }
        });
    };

});