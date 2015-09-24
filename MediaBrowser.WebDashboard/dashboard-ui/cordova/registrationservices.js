(function () {

    function isAndroid() {

        return $.browser.android;
    }

    function getPremiumUnlockFeatureId() {

        if (isAndroid()) {
            return "com.mb.android.unlock";
        }

        return 'appunlock';
    }

    function validatePlayback(deferred) {

        // Don't require validation on android
        if (isAndroid()) {
            deferred.resolve();
            return;
        }

        validateFeature(getPremiumUnlockFeatureId(), deferred);
    }

    function validateLiveTV(deferred) {

        if (!isAndroid()) {
            deferred.resolve();
            return;
        }

        validateFeature(getPremiumUnlockFeatureId(), deferred);
    }

    function validateServerManagement(deferred) {
        deferred.resolve();
    }

    function getRegistrationInfo(feature, enableSupporterUnlock) {

        if (!enableSupporterUnlock) {
            var deferred = $.Deferred();
            deferred.resolveWith(null, [{}]);
            return deferred.promise();
        }
        return ConnectionManager.getRegistrationInfo(feature, ApiClient);
    }

    var validatedFeatures = [];

    function validateFeature(id, deferred) {

        if (validatedFeatures.indexOf(id) != -1) {
            deferred.resolve();
            return;
        }

        var info = IapManager.getProductInfo(id) || {};

        if (info.owned) {
            notifyServer(id);
            validatedFeatures.push(id);
            deferred.resolve();
            return;
        }

        var productInfo = {
            enableSupporterUnlock: true,
            enableAppUnlock: IapManager.isPurchaseAvailable(id),
            id: id,
            price: info.price
        };

        var prefix = isAndroid() ? 'android' : 'ios';

        // Get supporter status
        getRegistrationInfo(prefix + 'appunlock', productInfo.enableSupporterUnlock).done(function (registrationInfo) {

            if (registrationInfo.IsRegistered) {
                validatedFeatures.push(id);
                deferred.resolve();
                return;
            }

            showInAppPurchaseInfo(productInfo, registrationInfo, deferred);

        }).fail(function () {
            deferred.reject();
        });
    }

    function notifyServer(id) {

        if (!$.browser.android) {
            return;
        }

        HttpClient.send({
            type: "POST",
            url: "https://mb3admin.com/admin/service/appstore/addDeviceFeature",
            data: {
                deviceId: ConnectionManager.deviceId(),
                feature: 'com.mb.android.unlock'
            },
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
            headers: {
                "X-EMBY-TOKEN": "EMBY_DEVICE"
            }

        }).done(function (result) {

            Logger.log('addDeviceFeature succeeded');

        }).fail(function () {
            Logger.log('addDeviceFeature failed');
        });
    }

    function getInAppPurchaseElement(info) {

        require(['paperbuttonstyle']);
        cancelInAppPurchase();

        var html = '';
        html += '<div class="inAppPurchaseOverlay" style="background-image:url(css/images/splash.jpg);top:0;left:0;right:0;bottom:0;position:fixed;background-position:center center;background-size:100% 100%;background-repeat:no-repeat;z-index:999999;">';
        html += '<div class="inAppPurchaseOverlayInner" style="background:rgba(10,10,10,.8);width:100%;height:100%;color:#eee;">';


        html += '<div class="inAppPurchaseForm" style="margin: 0 auto;padding: 30px 1em 0;">';

        html += '<h1 style="color:#fff;">' + Globalize.translate('HeaderUnlockApp') + '</h1>';

        html += '<p style="margin:2em 0;">';

        var showSupporterInfo = info.enableSupporterUnlock && !$.browser.safari;

        if (showSupporterInfo && info.enableAppUnlock) {
            html += Globalize.translate('MessageUnlockAppWithPurchaseOrSupporter');
        }
        else if (showSupporterInfo) {
            html += Globalize.translate('MessageUnlockAppWithSupporter');
        } else if (info.enableAppUnlock) {
            html += Globalize.translate('MessageUnlockAppWithPurchase');
        } else {
            html += '<span style="color:red;">';
            html += Globalize.translate('MessagePaymentServicesUnavailable');
            html += '</span>';
        }
        html += '</p>';

        if (showSupporterInfo) {
            html += '<p style="margin:2em 0;">';
            html += Globalize.translate('MessageToValidateSupporter');
            html += '</p>';
        }

        if (info.enableAppUnlock) {

            var unlockText = Globalize.translate('ButtonUnlockWithPurchase');
            if (info.price) {
                unlockText = Globalize.translate('ButtonUnlockPrice', info.price);
            }
            html += '<p>';
            html += '<paper-button raised class="secondary block btnAppUnlock"><iron-icon icon="check"></iron-icon><span>' + unlockText + '</span></paper-button>';
            html += '</p>';

            if (IapManager.restorePurchase) {
                html += '<p>';
                html += '<paper-button raised class="secondary block btnRestorePurchase" style="background-color: #673AB7;"><iron-icon icon="check"></iron-icon><span>' + Globalize.translate('ButtonRestorePreviousPurchase') + '</span></paper-button>';
                html += '</p>';
            }
        }

        if (showSupporterInfo) {
            html += '<p>';
            html += '<paper-button raised class="submit block btnSignInSupporter"><iron-icon icon="check"></iron-icon><span>' + Globalize.translate('ButtonUnlockWithSupporter') + '</span></paper-button>';
            html += '</p>';
        }

        html += '<p>';
        html += '<paper-button raised class="cancelDark block btnCancel"><iron-icon icon="close"></iron-icon><span>' + Globalize.translate('ButtonCancel') + '</span></paper-button>';
        html += '</p>';

        html += '</div>';

        html += '</div>';
        html += '</div>';

        $(document.body).append(html);

        return $('.inAppPurchaseOverlay');
    }

    function cancelInAppPurchase() {

        $('.inAppPurchaseOverlay').remove();
    }

    var currentDisplayingProductInfo = null;
    var currentDisplayingDeferred = null;

    function clearCurrentDisplayingInfo() {
        currentDisplayingProductInfo = null;
        currentDisplayingDeferred = null;
    }

    function showInAppPurchaseInfo(info, serverRegistrationInfo, deferred) {

        var elem = getInAppPurchaseElement(info);

        currentDisplayingProductInfo = info;
        currentDisplayingDeferred = deferred;

        $('.btnAppUnlock', elem).on('click', function () {

            IapManager.beginPurchase(info.id);
        });

        $('.btnRestorePurchase', elem).on('click', function () {

            IapManager.restorePurchase(info.id);
        });

        $('.btnCancel', elem).on('click', function () {

            clearCurrentDisplayingInfo();
            cancelInAppPurchase();

            deferred.reject();
        });
        $('.btnSignInSupporter', elem).on('click', function () {

            clearCurrentDisplayingInfo();

            Dashboard.alert({
                message: Globalize.translate('MessagePleaseSignInLocalNetwork'),
                callback: function () {
                    cancelInAppPurchase();
                    Dashboard.logout();
                }
            });
        });
    }

    function onProductUpdated(e, product) {

        var currentInfo = currentDisplayingProductInfo;
        var deferred = currentDisplayingDeferred;

        if (currentInfo && deferred) {
            if (product.owned && product.id == currentInfo.id) {

                clearCurrentDisplayingInfo();
                cancelInAppPurchase();
                deferred.resolve();
            }
        }
    }

    function validateSync(deferred) {

        Dashboard.getPluginSecurityInfo().done(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                deferred.resolve();
                return;
            }

            Dashboard.showLoadingMsg();

            ApiClient.getRegistrationInfo('Sync').done(function (registrationInfo) {

                Dashboard.hideLoadingMsg();

                if (registrationInfo.IsRegistered) {
                    deferred.resolve();
                    return;
                }

                Dashboard.alert({
                    message: Globalize.translate('HeaderSyncRequiresSupporterMembershipAppVersion'),
                    title: Globalize.translate('HeaderSync')
                });

            }).fail(function () {

                Dashboard.hideLoadingMsg();

                Dashboard.alert({
                    message: Globalize.translate('ErrorValidatingSupporterInfo')
                });
            });

        });
    }

    window.RegistrationServices = {

        renderPluginInfo: function (page, pkg, pluginSecurityInfo) {


        },

        addRecurringFields: function (page, period) {

        },

        initSupporterForm: function (page) {

            $('.recurringSubscriptionCancellationHelp', page).html('');
        },

        validateFeature: function (name) {
            var deferred = DeferredBuilder.Deferred();

            if (name == 'playback') {
                validatePlayback(deferred);
            } else if (name == 'livetv') {
                validateLiveTV(deferred);
            } else if (name == 'manageserver') {
                validateServerManagement(deferred);
            } else if (name == 'sync') {
                validateSync(deferred);
            } else {
                deferred.resolve();
            }

            return deferred.promise();
        }
    };

    function onIapManagerLoaded() {
        Events.on(IapManager, 'productupdated', onProductUpdated);
    }

    if (isAndroid()) {
        requirejs(['cordova/android/iap'], onIapManagerLoaded);
    } else {
        requirejs(['cordova/iap'], onIapManagerLoaded);
    }

})();