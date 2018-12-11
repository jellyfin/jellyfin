define(["globalize", "shell", "browser", "apphost"], function(globalize, shell, browser, appHost) {
    "use strict";

    function getProductInfo(feature) {
        return null
    }

    function getPremiumInfoUrl() {
        return "https://github.com/jellyfin/jellyfin"
    }

    function beginPurchase(feature, email) {
        appHost.supports("externalpremium") ? shell.openUrl(getPremiumInfoUrl()) : require(["alert"], function(alert) {
            alert("Please visit " + getPremiumInfoUrl())
        })
    }

    function restorePurchase(id) {
        return Promise.reject()
    }

    function getSubscriptionOptions() {
        var options = [];
        return options.push({
            id: "embypremiere",
            title: globalize.translate("sharedcomponents#HeaderBecomeProjectSupporter"),
            requiresEmail: !1
        }), Promise.resolve(options)
    }

    function isUnlockedByDefault(feature, options) {
        return "playback" === feature || "livetv" === feature ? Promise.resolve() : Promise.reject()
    }

    function getAdminFeatureName(feature) {
        return feature
    }

    function getRestoreButtonText() {
        return globalize.translate("sharedcomponents#HeaderAlreadyPaid")
    }

    function getPeriodicMessageIntervalMs(feature) {
        return 0
    }
    return {
        getProductInfo: getProductInfo,
        beginPurchase: beginPurchase,
        restorePurchase: restorePurchase,
        getSubscriptionOptions: getSubscriptionOptions,
        isUnlockedByDefault: isUnlockedByDefault,
        getAdminFeatureName: getAdminFeatureName,
        getRestoreButtonText: getRestoreButtonText,
        getPeriodicMessageIntervalMs: getPeriodicMessageIntervalMs,
        getPremiumInfoUrl: getPremiumInfoUrl
    }
});
