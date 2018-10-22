define(["require", "browser", "layoutManager", "appSettings", "pluginManager", "apphost", "focusManager", "datetime", "globalize", "loading", "connectionManager", "skinManager", "dom", "events", "emby-select", "emby-checkbox", "emby-linkbutton"], function(require, browser, layoutManager, appSettings, pluginManager, appHost, focusManager, datetime, globalize, loading, connectionManager, skinManager, dom, events) {
    "use strict";

    function fillThemes(select, isDashboard) {
        select.innerHTML = skinManager.getThemes().map(function(t) {
            var value = t.id;
            return t.isDefault && !isDashboard ? value = "" : t.isDefaultServerDashboard && isDashboard && (value = ""), '<option value="' + value + '">' + t.name + "</option>"
        }).join("")
    }

    function loadScreensavers(context, userSettings) {
        var selectScreensaver = context.querySelector(".selectScreensaver"),
            options = pluginManager.ofType("screensaver").map(function(plugin) {
                return {
                    name: plugin.name,
                    value: plugin.id
                }
            });
        options.unshift({
            name: globalize.translate("sharedcomponents#None"),
            value: "none"
        }), selectScreensaver.innerHTML = options.map(function(o) {
            return '<option value="' + o.value + '">' + o.name + "</option>"
        }).join(""), selectScreensaver.value = userSettings.screensaver(), selectScreensaver.value || (selectScreensaver.value = "none")
    }

    function loadSoundEffects(context, userSettings) {
        var selectSoundEffects = context.querySelector(".selectSoundEffects"),
            options = pluginManager.ofType("soundeffects").map(function(plugin) {
                return {
                    name: plugin.name,
                    value: plugin.id
                }
            });
        options.unshift({
            name: globalize.translate("sharedcomponents#None"),
            value: "none"
        }), selectSoundEffects.innerHTML = options.map(function(o) {
            return '<option value="' + o.value + '">' + o.name + "</option>"
        }).join(""), selectSoundEffects.value = userSettings.soundEffects(), selectSoundEffects.value || (selectSoundEffects.value = "none")
    }

    function loadSkins(context, userSettings) {
        var selectSkin = context.querySelector(".selectSkin"),
            options = pluginManager.ofType("skin").map(function(plugin) {
                return {
                    name: plugin.name,
                    value: plugin.id
                }
            });
        selectSkin.innerHTML = options.map(function(o) {
            return '<option value="' + o.value + '">' + o.name + "</option>"
        }).join(""), selectSkin.value = userSettings.skin(), !selectSkin.value && options.length && (selectSkin.value = options[0].value), options.length > 1 && appHost.supports("skins") ? context.querySelector(".selectSkinContainer").classList.remove("hide") : context.querySelector(".selectSkinContainer").classList.add("hide")
    }

    function showOrHideMissingEpisodesField(context, user, apiClient) {
        if (browser.tizen || browser.web0s) return void context.querySelector(".fldDisplayMissingEpisodes").classList.add("hide");
        context.querySelector(".fldDisplayMissingEpisodes").classList.remove("hide")
    }

    function loadForm(context, user, userSettings, apiClient) {
        apiClient.getCurrentUserId(), user.Id;
        user.Policy.IsAdministrator ? context.querySelector(".selectDashboardThemeContainer").classList.remove("hide") : context.querySelector(".selectDashboardThemeContainer").classList.add("hide"), appHost.supports("displaylanguage") ? context.querySelector(".languageSection").classList.remove("hide") : context.querySelector(".languageSection").classList.add("hide"), appHost.supports("displaymode") ? context.querySelector(".fldDisplayMode").classList.remove("hide") : context.querySelector(".fldDisplayMode").classList.add("hide"), appHost.supports("externallinks") ? context.querySelector(".learnHowToContributeContainer").classList.remove("hide") : context.querySelector(".learnHowToContributeContainer").classList.add("hide"), appHost.supports("runatstartup") ? context.querySelector(".fldAutorun").classList.remove("hide") : context.querySelector(".fldAutorun").classList.add("hide"), appHost.supports("soundeffects") ? context.querySelector(".fldSoundEffects").classList.remove("hide") : context.querySelector(".fldSoundEffects").classList.add("hide"), appHost.supports("screensaver") ? context.querySelector(".selectScreensaverContainer").classList.remove("hide") : context.querySelector(".selectScreensaverContainer").classList.add("hide"), datetime.supportsLocalization() ? context.querySelector(".fldDateTimeLocale").classList.remove("hide") : context.querySelector(".fldDateTimeLocale").classList.add("hide"), browser.tizen || browser.web0s ? (context.querySelector(".fldSeasonalThemes").classList.add("hide"), context.querySelector(".fldBackdrops").classList.add("hide"), context.querySelector(".fldThemeSong").classList.add("hide"), context.querySelector(".fldThemeVideo").classList.add("hide")) : (context.querySelector(".fldSeasonalThemes").classList.remove("hide"), context.querySelector(".fldBackdrops").classList.remove("hide"), context.querySelector(".fldThemeSong").classList.remove("hide"), context.querySelector(".fldThemeVideo").classList.remove("hide")), context.querySelector(".chkRunAtStartup").checked = appSettings.runAtStartup();
        var selectTheme = context.querySelector("#selectTheme"),
            selectDashboardTheme = context.querySelector("#selectDashboardTheme");
        fillThemes(selectTheme), fillThemes(selectDashboardTheme, !0), loadScreensavers(context, userSettings), loadSoundEffects(context, userSettings), loadSkins(context, userSettings), context.querySelector(".chkDisplayMissingEpisodes").checked = user.Configuration.DisplayMissingEpisodes || !1, context.querySelector("#chkThemeSong").checked = userSettings.enableThemeSongs(), context.querySelector("#chkThemeVideo").checked = userSettings.enableThemeVideos(), context.querySelector("#chkBackdrops").checked = userSettings.enableBackdrops(), context.querySelector("#chkSeasonalThemes").checked = userSettings.enableSeasonalThemes(), context.querySelector("#selectLanguage").value = userSettings.language() || "", context.querySelector(".selectDateTimeLocale").value = userSettings.dateTimeLocale() || "", selectDashboardTheme.value = userSettings.dashboardTheme() || "", selectTheme.value = userSettings.theme() || "", context.querySelector(".selectLayout").value = layoutManager.getSavedLayout() || "", showOrHideMissingEpisodesField(context, user, apiClient), loading.hide()
    }

    function saveUser(context, user, userSettingsInstance, apiClient) {
        return appSettings.runAtStartup(context.querySelector(".chkRunAtStartup").checked), user.Configuration.DisplayMissingEpisodes = context.querySelector(".chkDisplayMissingEpisodes").checked, appHost.supports("displaylanguage") && userSettingsInstance.language(context.querySelector("#selectLanguage").value), userSettingsInstance.dateTimeLocale(context.querySelector(".selectDateTimeLocale").value), userSettingsInstance.enableThemeSongs(context.querySelector("#chkThemeSong").checked), userSettingsInstance.enableThemeVideos(context.querySelector("#chkThemeVideo").checked), userSettingsInstance.dashboardTheme(context.querySelector("#selectDashboardTheme").value), userSettingsInstance.theme(context.querySelector("#selectTheme").value), userSettingsInstance.soundEffects(context.querySelector(".selectSoundEffects").value), userSettingsInstance.screensaver(context.querySelector(".selectScreensaver").value), userSettingsInstance.skin(context.querySelector(".selectSkin").value), userSettingsInstance.enableBackdrops(context.querySelector("#chkBackdrops").checked), userSettingsInstance.enableSeasonalThemes(context.querySelector("#chkSeasonalThemes").checked), user.Id === apiClient.getCurrentUserId() && skinManager.setTheme(userSettingsInstance.theme()), layoutManager.setLayout(context.querySelector(".selectLayout").value), apiClient.updateUserConfiguration(user.Id, user.Configuration)
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
        require(["text!./displaysettings.template.html"], function(template) {
            options.element.innerHTML = globalize.translateDocument(template, "sharedcomponents"), options.element.querySelector("form").addEventListener("submit", onSubmit.bind(self)), options.enableSaveButton && options.element.querySelector(".btnSave").classList.remove("hide"), self.loadData(options.autoFocus)
        })
    }

    function DisplaySettings(options) {
        this.options = options, embed(options, this)
    }
    return DisplaySettings.prototype.loadData = function(autoFocus) {
        var self = this,
            context = self.options.element;
        loading.show();
        var userId = self.options.userId,
            apiClient = connectionManager.getApiClient(self.options.serverId),
            userSettings = self.options.userSettings;
        return apiClient.getUser(userId).then(function(user) {
            return userSettings.setUserInfo(userId, apiClient).then(function() {
                self.dataLoaded = !0, loadForm(context, user, userSettings, apiClient), autoFocus && focusManager.autoFocus(context)
            })
        })
    }, DisplaySettings.prototype.submit = function() {
        onSubmit.call(this)
    }, DisplaySettings.prototype.destroy = function() {
        this.options = null
    }, DisplaySettings
});