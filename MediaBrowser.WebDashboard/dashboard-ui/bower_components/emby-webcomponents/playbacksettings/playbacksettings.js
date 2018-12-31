define(['require', 'browser', 'appSettings', 'apphost', 'focusManager', 'qualityoptions', 'globalize', 'loading', 'connectionManager', 'dom', 'events', 'emby-select', 'emby-checkbox'], function (require, browser, appSettings, appHost, focusManager, qualityoptions, globalize, loading, connectionManager, dom, events) {
    "use strict";

    function fillSkipLengths(select) {

        var options = [5, 10, 15, 20, 25, 30];

        select.innerHTML = options.map(function (option) {
            return {
                name: globalize.translate('sharedcomponents#ValueSeconds', option),
                value: option * 1000
            };
        }).map(function (o) {
            return '<option value="' + o.value + '">' + o.name + '</option>';
        }).join('');
    }

    function populateLanguages(select, languages) {

        var html = "";

        html += "<option value=''>" + globalize.translate('sharedcomponents#AnyLanguage') + "</option>";

        for (var i = 0, length = languages.length; i < length; i++) {

            var culture = languages[i];

            html += "<option value='" + culture.ThreeLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        select.innerHTML = html;
    }

    function setMaxBitrateIntoField(select, isInNetwork, mediatype) {

        var options = mediatype === 'Audio' ? qualityoptions.getAudioQualityOptions({

            currentMaxBitrate: appSettings.maxStreamingBitrate(isInNetwork, mediatype),
            isAutomaticBitrateEnabled: appSettings.enableAutomaticBitrateDetection(isInNetwork, mediatype),
            enableAuto: true

        }) : qualityoptions.getVideoQualityOptions({

            currentMaxBitrate: appSettings.maxStreamingBitrate(isInNetwork, mediatype),
            isAutomaticBitrateEnabled: appSettings.enableAutomaticBitrateDetection(isInNetwork, mediatype),
            enableAuto: true

        });

        select.innerHTML = options.map(function (i) {

            // render empty string instead of 0 for the auto option
            return '<option value="' + (i.bitrate || '') + '">' + i.name + '</option>';
        }).join('');

        if (appSettings.enableAutomaticBitrateDetection(isInNetwork, mediatype)) {
            select.value = '';
        } else {
            select.value = appSettings.maxStreamingBitrate(isInNetwork, mediatype);
        }
    }

    function fillChromecastQuality(select) {

        var options = qualityoptions.getVideoQualityOptions({

            currentMaxBitrate: appSettings.maxChromecastBitrate(),
            isAutomaticBitrateEnabled: !appSettings.maxChromecastBitrate(),
            enableAuto: true
        });

        select.innerHTML = options.map(function (i) {

            // render empty string instead of 0 for the auto option
            return '<option value="' + (i.bitrate || '') + '">' + i.name + '</option>';
        }).join('');

        select.value = appSettings.maxChromecastBitrate() || '';
    }

    function setMaxBitrateFromField(select, isInNetwork, mediatype, value) {

        if (select.value) {
            appSettings.maxStreamingBitrate(isInNetwork, mediatype, select.value);
            appSettings.enableAutomaticBitrateDetection(isInNetwork, mediatype, false);
        } else {
            appSettings.enableAutomaticBitrateDetection(isInNetwork, mediatype, true);
        }
    }

    function showHideQualityFields(context, user, apiClient) {

        if (user.Policy.EnableVideoPlaybackTranscoding) {
            context.querySelector('.videoQualitySection').classList.remove('hide');
        } else {
            context.querySelector('.videoQualitySection').classList.add('hide');
        }

        if (appHost.supports('multiserver')) {

            context.querySelector('.fldVideoInNetworkQuality').classList.remove('hide');
            context.querySelector('.fldVideoInternetQuality').classList.remove('hide');

            if (user.Policy.EnableAudioPlaybackTranscoding) {
                context.querySelector('.musicQualitySection').classList.remove('hide');
            } else {
                context.querySelector('.musicQualitySection').classList.add('hide');
            }

            return;
        }

        apiClient.getEndpointInfo().then(function (endpointInfo) {

            if (endpointInfo.IsInNetwork) {

                context.querySelector('.fldVideoInNetworkQuality').classList.remove('hide');

                context.querySelector('.fldVideoInternetQuality').classList.add('hide');
                context.querySelector('.musicQualitySection').classList.add('hide');
            } else {

                context.querySelector('.fldVideoInNetworkQuality').classList.add('hide');

                context.querySelector('.fldVideoInternetQuality').classList.remove('hide');

                if (user.Policy.EnableAudioPlaybackTranscoding) {
                    context.querySelector('.musicQualitySection').classList.remove('hide');
                } else {
                    context.querySelector('.musicQualitySection').classList.add('hide');
                }
            }
        });
    }

    function showOrHideEpisodesField(context, user, apiClient) {

        if (browser.tizen || browser.web0s) {
            context.querySelector('.fldEpisodeAutoPlay').classList.add('hide');
            return;
        }

        context.querySelector('.fldEpisodeAutoPlay').classList.remove('hide');
    }

    function loadForm(context, user, userSettings, apiClient) {

        var loggedInUserId = apiClient.getCurrentUserId();
        var userId = user.Id;

        showHideQualityFields(context, user, apiClient);

        apiClient.getCultures().then(function (allCultures) {

            populateLanguages(context.querySelector('#selectAudioLanguage'), allCultures);

            context.querySelector('#selectAudioLanguage', context).value = user.Configuration.AudioLanguagePreference || "";
            context.querySelector('.chkEpisodeAutoPlay').checked = user.Configuration.EnableNextEpisodeAutoPlay || false;
        });

        // hide cinema mode options if disabled at server level
        apiClient.getNamedConfiguration("cinemamode").then(function (cinemaConfig) {

            if (cinemaConfig.EnableIntrosForMovies || cinemaConfig.EnableIntrosForEpisodes) {
                context.querySelector('.cinemaModeOptions').classList.remove('hide');
            } else {
                context.querySelector('.cinemaModeOptions').classList.add('hide');
            }
        });

        if (appHost.supports('externalplayerintent') && userId === loggedInUserId) {
            context.querySelector('.fldExternalPlayer').classList.remove('hide');
        } else {
            context.querySelector('.fldExternalPlayer').classList.add('hide');
        }

        if (userId === loggedInUserId && (user.Policy.EnableVideoPlaybackTranscoding || user.Policy.EnableAudioPlaybackTranscoding)) {
            context.querySelector('.qualitySections').classList.remove('hide');

            if (appHost.supports('chromecast') && user.Policy.EnableVideoPlaybackTranscoding) {
                context.querySelector('.fldChromecastQuality').classList.remove('hide');
            } else {
                context.querySelector('.fldChromecastQuality').classList.add('hide');
            }
        } else {
            context.querySelector('.qualitySections').classList.add('hide');
            context.querySelector('.fldChromecastQuality').classList.add('hide');
        }

        if (browser.tizen || browser.web0s) {
            context.querySelector('.fldEnableNextVideoOverlay').classList.add('hide');
        } else {
            context.querySelector('.fldEnableNextVideoOverlay').classList.remove('hide');
        }

        context.querySelector('.chkPlayDefaultAudioTrack').checked = user.Configuration.PlayDefaultAudioTrack || false;
        context.querySelector('.chkEnableCinemaMode').checked = userSettings.enableCinemaMode();
        context.querySelector('.chkEnableNextVideoOverlay').checked = userSettings.enableNextVideoInfoOverlay();
        context.querySelector('.chkExternalVideoPlayer').checked = appSettings.enableSystemExternalPlayers();

        setMaxBitrateIntoField(context.querySelector('.selectVideoInNetworkQuality'), true, 'Video');
        setMaxBitrateIntoField(context.querySelector('.selectVideoInternetQuality'), false, 'Video');
        setMaxBitrateIntoField(context.querySelector('.selectMusicInternetQuality'), false, 'Audio');

        fillChromecastQuality(context.querySelector('.selectChromecastVideoQuality'));

        var selectSkipForwardLength = context.querySelector('.selectSkipForwardLength');
        fillSkipLengths(selectSkipForwardLength);
        selectSkipForwardLength.value = userSettings.skipForwardLength();

        var selectSkipBackLength = context.querySelector('.selectSkipBackLength');
        fillSkipLengths(selectSkipBackLength);
        selectSkipBackLength.value = userSettings.skipBackLength();

        showOrHideEpisodesField(context, user, apiClient);

        loading.hide();
    }

    function saveUser(context, user, userSettingsInstance, apiClient) {

        appSettings.enableSystemExternalPlayers(context.querySelector('.chkExternalVideoPlayer').checked);

        appSettings.maxChromecastBitrate(context.querySelector('.selectChromecastVideoQuality').value);

        setMaxBitrateFromField(context.querySelector('.selectVideoInNetworkQuality'), true, 'Video');
        setMaxBitrateFromField(context.querySelector('.selectVideoInternetQuality'), false, 'Video');
        setMaxBitrateFromField(context.querySelector('.selectMusicInternetQuality'), false, 'Audio');

        user.Configuration.AudioLanguagePreference = context.querySelector('#selectAudioLanguage').value;
        user.Configuration.PlayDefaultAudioTrack = context.querySelector('.chkPlayDefaultAudioTrack').checked;
        user.Configuration.EnableNextEpisodeAutoPlay = context.querySelector('.chkEpisodeAutoPlay').checked;

        userSettingsInstance.enableCinemaMode(context.querySelector('.chkEnableCinemaMode').checked);

        userSettingsInstance.enableNextVideoInfoOverlay(context.querySelector('.chkEnableNextVideoOverlay').checked);
        userSettingsInstance.skipForwardLength(context.querySelector('.selectSkipForwardLength').value);
        userSettingsInstance.skipBackLength(context.querySelector('.selectSkipBackLength').value);

        return apiClient.updateUserConfiguration(user.Id, user.Configuration);
    }

    function save(instance, context, userId, userSettings, apiClient, enableSaveConfirmation) {

        loading.show();

        apiClient.getUser(userId).then(function (user) {

            saveUser(context, user, userSettings, apiClient).then(function () {

                loading.hide();
                if (enableSaveConfirmation) {
                    require(['toast'], function (toast) {
                        toast(globalize.translate('sharedcomponents#SettingsSaved'));
                    });
                }

                events.trigger(instance, 'saved');

            }, function () {
                loading.hide();
            });
        });
    }

    function onSubmit(e) {

        var self = this;
        var apiClient = connectionManager.getApiClient(self.options.serverId);
        var userId = self.options.userId;
        var userSettings = self.options.userSettings;

        userSettings.setUserInfo(userId, apiClient).then(function () {

            var enableSaveConfirmation = self.options.enableSaveConfirmation;
            save(self, self.options.element, userId, userSettings, apiClient, enableSaveConfirmation);
        });

        // Disable default form submission
        if (e) {
            e.preventDefault();
        }
        return false;
    }

    function embed(options, self) {

        require(['text!./playbacksettings.template.html'], function (template) {

            options.element.innerHTML = globalize.translateDocument(template, 'sharedcomponents');

            options.element.querySelector('form').addEventListener('submit', onSubmit.bind(self));

            if (options.enableSaveButton) {
                options.element.querySelector('.btnSave').classList.remove('hide');
            }

            self.loadData();

            if (options.autoFocus) {
                focusManager.autoFocus(options.element);
            }
        });
    }

    function PlaybackSettings(options) {

        this.options = options;

        embed(options, this);
    }

    PlaybackSettings.prototype.loadData = function () {

        var self = this;
        var context = self.options.element;

        loading.show();

        var userId = self.options.userId;
        var apiClient = connectionManager.getApiClient(self.options.serverId);
        var userSettings = self.options.userSettings;

        apiClient.getUser(userId).then(function (user) {

            userSettings.setUserInfo(userId, apiClient).then(function () {

                self.dataLoaded = true;

                loadForm(context, user, userSettings, apiClient);
            });
        });
    };

    PlaybackSettings.prototype.submit = function () {
        onSubmit.call(this);
    };

    PlaybackSettings.prototype.destroy = function () {

        this.options = null;
    };

    return PlaybackSettings;
});