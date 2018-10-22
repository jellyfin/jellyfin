define(["userSettings", "loading", "connectionManager", "apphost", "layoutManager", "focusManager", "homeSections", "emby-itemscontainer"], function(userSettings, loading, connectionManager, appHost, layoutManager, focusManager, homeSections) {
    "use strict";

    function HomeTab(view, params) {
        this.view = view, this.params = params, this.apiClient = connectionManager.currentApiClient(), this.sectionsContainer = view.querySelector(".sections"), view.querySelector(".sections").addEventListener("settingschange", onHomeScreenSettingsChanged.bind(this))
    }

    function onHomeScreenSettingsChanged() {
        this.sectionsRendered = !1, this.paused || this.onResume({
            refresh: !0
        })
    }
    return HomeTab.prototype.onResume = function(options) {
        if (this.sectionsRendered) {
            var sectionsContainer = this.sectionsContainer;
            return sectionsContainer ? homeSections.resume(sectionsContainer, options) : Promise.resolve()
        }
        loading.show();
        var view = this.view,
            apiClient = this.apiClient;
        return this.destroyHomeSections(), this.sectionsRendered = !0, apiClient.getCurrentUser().then(function(user) {
            return homeSections.loadSections(view.querySelector(".sections"), apiClient, user, userSettings).then(function() {
                options.autoFocus && focusManager.autoFocus(view), loading.hide()
            })
        })
    }, HomeTab.prototype.onPause = function() {
        var sectionsContainer = this.sectionsContainer;
        sectionsContainer && homeSections.pause(sectionsContainer)
    }, HomeTab.prototype.destroy = function() {
        this.view = null, this.params = null, this.apiClient = null, this.destroyHomeSections(), this.sectionsContainer = null
    }, HomeTab.prototype.destroyHomeSections = function() {
        var sectionsContainer = this.sectionsContainer;
        sectionsContainer && homeSections.destroySections(sectionsContainer)
    }, HomeTab
});