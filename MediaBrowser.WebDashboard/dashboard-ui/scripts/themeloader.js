define(["userSettings", "skinManager", "connectionManager", "events"], function(userSettings, skinManager, connectionManager, events) {
    "use strict";
    var currentViewType;
    pageClassOn("viewbeforeshow", "page", function() {
        var classList = this.classList,
            viewType = classList.contains("type-interior") || classList.contains("wizardPage") ? "a" : "b";
        if (viewType !== currentViewType) {
            currentViewType = viewType;
            var theme, context;
            "a" === viewType ? (theme = userSettings.dashboardTheme(), context = "serverdashboard") : theme = userSettings.theme(), skinManager.setTheme(theme, context)
        }
    }), events.on(connectionManager, "localusersignedin", function(e, user) {
        currentViewType = null
    })
});