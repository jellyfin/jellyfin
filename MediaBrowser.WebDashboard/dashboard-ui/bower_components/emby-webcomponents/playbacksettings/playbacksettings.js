define(["require", "browser", "appSettings", "apphost", "focusManager", "qualityoptions", "globalize", "loading", "connectionManager", "dom", "events", "emby-select", "emby-checkbox"], function(require, browser, appSettings, appHost, focusManager, qualityoptions, globalize, loading, connectionManager, dom, events) {
    "use strict";

    function fillSkipLengths(select) {
        var options = [5, 10, 15, 20, 25, 30];
        select.innerHTML = options.map(function(option) {
            return {
                name: globalize.translate("sharedcomponents#ValueSeconds", option),
                value: 1e3 * option
            }
        }).map(function(o) {
            return '<option value="' + o.value + '">' + o.name + "</option>"
        }).join("")
    }

    function populateLanguages(select, languages) {
        var html = "";
        html += "<option value=''>" + globalize.translate("sharedcomponents#AnyLanguage") + "</option>";
        for (var i = 0, length = languages.length; i < length; i++) {
            var culture = languages[i];
            html += "<option value='" + culture.ThreeLetterISOLanguageName + "'>" + culture.DisplayName + "</option>"
        }
        select.innerHTML = html
    }

    function setMaxBitrateIntoField(select, isInNetwork, mediatype) {
        var options = "Audio" === mediatype ? qualityoptions.getAudioQualityOptions({
            currentMaxBitrate: appSettings.maxStreamingBitrate(isInNetwork, mediatype),
            isAutomaticBitrateEnabled: appSettings.enableAutomaticBitrateDetection(isInNetwork, mediatype),
            enableAuto: !0
        }) : qualityoptions.getVideoQualityOptions({
            currentMaxBitrate: appSettings.maxStreamingBitrate(isInNetwork, mediatype),
            isAutomaticBitrateEnabled: appSettings.enableAutomaticBitrateDetection(isInNetwork, mediatype),
            enableAuto: !0
        });
        select.innerHTML = options.map(function(i) {
            return '<option value="' + (i.bitrate || "") + '">' + i.name + "</option>"
        }).join(""), appSettings.enableAutomaticBitrateDetection(isInNetwork, mediatype) ? select.value = "" : select.value = appSettings.maxStreamingBitrate(isInNetwork, mediatype)
    }

    function fillChromecastQuality(select) {
        var options = qualityoptions.getVideoQualityOptions({
            currentMaxBitrate: appSettings.maxChromecastBitrate(),
            isAutomaticBitrateEnabled: !appSettings.maxChromecastBitrate(),
            enableAuto: !0
        });
        select.innerHTML = options.map(function(i) {
            return '<option value="' + (i.bitrate || "") + '">' + i.name + "</option>"
        }).join(""), select.value = appSettings.maxChromecastBitrate() || ""
    }

    function setMaxBitrateFromField(select, isInNetwork, mediatype, value) {
        select.value ? (appSettings.maxStreamingBitrate(isInNetwork, mediatype, select.value), appSettings.enableAutomaticBitrateDetection(isInNetwork, mediatype, !1)) : appSettings.enableAutomaticBitrateDetection(isInNetwork, mediatype, !0)
    }

    function showHideQualityFields(context, user, apiClient) {
        if (user.Policy.EnableVideoPlaybackTranscoding ? context.querySelector(".videoQualitySection").classList.remove("hide") : context.querySelector(".videoQualitySection").classList.add("hide"), appHost.supports("multiserver")) return context.querySelector(".fldVideoInNetworkQuality").classList.remove("hide"), context.querySelector(".fldVideoInternetQuality").classList.remove("hide"), void(user.Policy.EnableAudioPlaybackTranscoding ? context.querySelector(".musicQualitySection").classList.remove("hide") : context.querySelector(".musicQualitySection").classList.add("hide"));
        apiClient.getEndpointInfo().then(function(endpointInfo) {
            endpointInfo.IsInNetwork ? (context.querySelector(".fldVideoInNetworkQuality").classList.remove("hide"), context.querySelector(".fldVideoInternetQuality").classList.add("hide"), context.querySelector(".musicQualitySection").classList.add("hide")) : (context.querySelector(".fldVideoInNetworkQuality").classList.add("hide"), context.querySelector(".fldVideoInternetQuality").classList.remove("hide"), user.Policy.EnableAudioPlaybackTranscoding ? context.querySelector(".musicQualitySection").classList.remove("hide") : context.querySelector(".musicQualitySection").classList.add("hide"))
        })
    }

    function showOrHideEpisodesField(context, user, apiClient) {
        if (browser.tizen || browser.web0s) return void context.querySelector(".fldEpisodeAutoPlay").classList.add("hide");
        context.querySelector(".fldEpisodeAutoPlay").classList.remove("hide")
    }

    function loadForm(context, user, userSettings, apiClient) {
        var loggedInUserId = apiClient.getCurrentUserId(),
            userId = user.Id;
        showHideQualityFields(context, user, apiClient), apiClient.getCultures().then(function(allCultures) {
            populateLanguages(context.querySelector("#selectAudioLanguage"), allCultures), context.querySelector("#selectAudioLanguage", context).value = user.Configuration.AudioLanguagePreference || "", context.querySelector(".chkEpisodeAutoPlay").checked = user.Configuration.EnableNextEpisodeAutoPlay || !1
        }), apiClient.getNamedConfiguration("cinemamode").then(function(cinemaConfig) {
            cinemaConfig.EnableIntrosForMovies || cinemaConfig.EnableIntrosForEpisodes ? context.querySelector(".cinemaModeOptions").classList.remove("hide") : context.querySelector(".cinemaModeOptions").classList.add("hide")
        }), appHost.supports("externalplayerintent") && userId === loggedInUserId ? context.querySelector(".fldExternalPlayer").classList.remove("hide") : context.querySelector(".fldExternalPlayer").classList.add("hide"), userId === loggedInUserId && (user.Policy.EnableVideoPlaybackTranscoding || user.Policy.EnableAudioPlaybackTranscoding) ? (context.querySelector(".qualitySections").classList.remove("hide"), appHost.supports("chromecast") && user.Policy.EnableVideoPlaybackTranscoding ? context.querySelector(".fldChromecastQuality").classList.remove("hide") : context.querySelector(".fldChromecastQuality").classList.add("hide")) : (context.querySelector(".qualitySections").classList.add("hide"), context.querySelector(".fldChromecastQuality").classList.add("hide")), browser.tizen || browser.web0s ? context.querySelector(".fldEnableNextVideoOverlay").classList.add("hide") : context.querySelector(".fldEnableNextVideoOverlay").classList.remove("hide"), context.querySelector(".chkPlayDefaultAudioTrack").checked = user.Configuration.PlayDefaultAudioTrack || !1, context.querySelector(".chkEnableCinemaMode").checked = userSettings.enableCinemaMode(), context.querySelector(".chkEnableNextVideoOverlay").checked = userSettings.enableNextVideoInfoOverlay(), context.querySelector(".chkExternalVideoPlayer").checked = appSettings.enableSystemExternalPlayers(), setMaxBitrateIntoField(context.querySelector(".selectVideoInNetworkQuality"), !0, "Video"), setMaxBitrateIntoField(context.querySelector(".selectVideoInternetQuality"), !1, "Video"), setMaxBitrateIntoField(context.querySelector(".selectMusicInternetQuality"), !1, "Audio"), fillChromecastQuality(context.querySelector(".selectChromecastVideoQuality"));
        var selectSkipForwardLength = context.querySelector(".selectSkipForwardLength");
        fillSkipLengths(selectSkipForwardLength), selectSkipForwardLength.value = userSettings.skipForwardLength();
        var selectSkipBackLength = context.querySelector(".selectSkipBackLength");
        fillSkipLengths(selectSkipBackLength), selectSkipBackLength.value = userSettings.skipBackLength(), showOrHideEpisodesField(context, user, apiClient), loading.hide()
    }

    function saveUser(context, user, userSettingsInstance, apiClient) {
        return appSettings.enableSystemExternalPlayers(context.querySelector(".chkExternalVideoPlayer").checked), appSettings.maxChromecastBitrate(context.querySelector(".selectChromecastVideoQuality").value), setMaxBitrateFromField(context.querySelector(".selectVideoInNetworkQuality"), !0, "Video"), setMaxBitrateFromField(context.querySelector(".selectVideoInternetQuality"), !1, "Video"), setMaxBitrateFromField(context.querySelector(".selectMusicInternetQuality"), !1, "Audio"), user.Configuration.AudioLanguagePreference = context.querySelector("#selectAudioLanguage").value, user.Configuration.PlayDefaultAudioTrack = context.querySelector(".chkPlayDefaultAudioTrack").checked, user.Configuration.EnableNextEpisodeAutoPlay = context.querySelector(".chkEpisodeAutoPlay").checked, userSettingsInstance.enableCinemaMode(context.querySelector(".chkEnableCinemaMode").checked), userSettingsInstance.enableNextVideoInfoOverlay(context.querySelector(".chkEnableNextVideoOverlay").checked), userSettingsInstance.skipForwardLength(context.querySelector(".selectSkipForwardLength").value), userSettingsInstance.skipBackLength(context.querySelector(".selectSkipBackLength").value), apiClient.updateUserConfiguration(user.Id, user.Configuration)
    }

    function save(instance, context, userId, userSettings, apiClient, enableSaveConfirmation) {
        loading.show(), apiClient.getUser(userId).then(function(user) {
            saveUser(context, user, userSettings, apiClient).then(function() {
                loading.hide(), enableSaveConfirmation && require(["toast"], function(toast) {
                    toast(globalize.translate("sharedcomponents#SettingsSaved"))
                }), events.trigger(instance, "saved")
            }, function() {
                loading.hide()
            })
        })
    }

    function onSubmit(e) {
        var self = this,
            apiClient = connectionManager.getApiClient(self.options.serverId),
            userId = self.options.userId,
            userSettings = self.options.userSettings;
        return userSettings.setUserInfo(userId, apiClient).then(function() {
            var enableSaveConfirmation = self.options.enableSaveConfirmation;
            save(self, self.options.element, userId, userSettings, apiClient, enableSaveConfirmation)
        }), e && e.preventDefault(), !1
    }

    function embed(options, self) {
        require(["text!./playbacksettings.template.html"], function(template) {
            options.element.innerHTML = globalize.translateDocument(template, "sharedcomponents"), options.element.querySelector("form").addEventListener("submit", onSubmit.bind(self)), options.enableSaveButton && options.element.querySelector(".btnSave").classList.remove("hide"), self.loadData(), options.autoFocus && focusManager.autoFocus(options.element)
        })
    }

    function PlaybackSettings(options) {
        this.options = options, embed(options, this)
    }
    return PlaybackSettings.prototype.loadData = function() {
        var self = this,
            context = self.options.element;
        loading.show();
        var userId = self.options.userId,
            apiClient = connectionManager.getApiClient(self.options.serverId),
            userSettings = self.options.userSettings;
        apiClient.getUser(userId).then(function(user) {
            userSettings.setUserInfo(userId, apiClient).then(function() {
                self.dataLoaded = !0, loadForm(context, user, userSettings, apiClient)
            })
        })
    }, PlaybackSettings.prototype.submit = function() {
        onSubmit.call(this)
    }, PlaybackSettings.prototype.destroy = function() {
        this.options = null
    }, PlaybackSettings
});