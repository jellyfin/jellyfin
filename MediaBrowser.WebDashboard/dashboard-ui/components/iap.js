define(['apphost', 'globalize', 'shell'], function (appHost, globalize, shell) {

    function getProductInfo(feature) {
        return null;
    }

    function showExternalPremiereInfo() {
        shell.openUrl('https://emby.media/premiere');
    }

    function beginPurchase(feature, email) {
        showExternalPremiereInfo();
        return Promise.reject();
    }

    function restorePurchase(id) {
        return Promise.reject();
    }

    function getSubscriptionOptions() {

        var options = [];

        options.push({
            id: 'embypremiere',
            title: globalize.translate('sharedcomponents#HeaderBecomeProjectSupporter'),
            requiresEmail: false
        });

        return Promise.resolve(options);
    }

    function isUnlockedByDefault(feature, options) {

        var autoUnlockedFeatures = appHost.unlockedFeatures ? appHost.unlockedFeatures() : [];
        if (autoUnlockedFeatures.indexOf(feature) != -1) {

            return Promise.resolve();
        }

        return Promise.reject();
    }

    function getAdminFeatureName(feature) {

        return feature;
    }

    function getRestoreButtonText() {
        return globalize.translate('sharedcomponents#ButtonAlreadyPaid');
    }

    function getPeriodicMessageIntervalMs(feature) {

        if (feature == 'playback') {
            return 259200000;
        }

        return 0;
    }

    return {
        getProductInfo: getProductInfo,
        beginPurchase: beginPurchase,
        restorePurchase: restorePurchase,
        getSubscriptionOptions: getSubscriptionOptions,
        isUnlockedByDefault: isUnlockedByDefault,
        getAdminFeatureName: getAdminFeatureName,
        getRestoreButtonText: getRestoreButtonText,
        getPeriodicMessageIntervalMs: getPeriodicMessageIntervalMs
    };

});