(function () {

    var updatedProducts = [];

    function updateProductInfo(p) {

        updatedProducts = updatedProducts.filter(function (r) {
            return r.alias != p.alias;
        });

        updatedProducts.push(p);
    }

    function isAndroid() {

        var platform = (device.platform || '').toLowerCase();

        return platform.indexOf('android') != -1;
    }

    function validatePlayback(deferred) {

        // Don't require validation on android
        if (isAndroid()) {
            deferred.resolve();
            return;
        }

        validateFeature({

            id: 'appunlock',
            alias: "premium features"

        }, deferred);
    }

    function validateLiveTV(deferred) {

        // Don't require validation if not android
        if (!isAndroid()) {
            deferred.resolve();
            return;
        }

        validateFeature({

            id: 'premiumunlock',
            alias: "premium features"

        }, deferred);
    }

    function validateSmb(deferred) {

        // Don't require validation if not android
        if (!isAndroid()) {
            deferred.resolve();
            return;
        }

        validateFeature({

            id: 'premiumunlock',
            alias: "premium features"

        }, deferred);
    }

    function getRegistrationInfo(feature, enableSupporterUnlock) {

        if (!enableSupporterUnlock) {
            var deferred = $.Deferred();
            deferred.resolveWith(null, [{}]);
            return deferred.promise();
        }
        return ConnectionManager.getRegistrationInfo(feature, ApiClient);
    }

    function validateFeature(info, deferred) {

        var products = updatedProducts.filter(function (r) {
            return r.alias == info.alias;
        });

        var product = products.length ? products[0] : null;

        if (product && product.owned) {
            deferred.resolve();
            return;
        }

        var productInfo = {
            enableSupporterUnlock: isAndroid(),
            enableAppUnlock: product != null && product.canPurchase
        };

        // Get supporter status
        getRegistrationInfo('appunlock', productInfo.enableSupporterUnlock).done(function (registrationInfo) {

            if (registrationInfo.IsRegistered) {
                deferred.resolve();
                return;
            }

            showInAppPurchaseInfo(productInfo, registrationInfo, deferred);

        }).fail(function () {
            deferred.reject();
        });
    }

    function getInAppPurchaseElement(info) {

        cancelInAppPurchase();

        var html = '';
        html += '<div class="inAppPurchaseOverlay" style="background-image:url(css/images/splash.jpg);top:0;left:0;right:0;bottom:0;position:fixed;background-position:center center;background-size:100% 100%;background-repeat:no-repeat;z-index:999999;">';
        html += '<div class="inAppPurchaseOverlayInner" style="background:rgba(10,10,10,.8);width:100%;height:100%;color:#eee;">';


        html += '<form class="inAppPurchaseForm" style="margin: 0 auto;padding: 30px 1em 0;">';

        html += '<h1 style="color:#fff;">' + Globalize.translate('HeaderUnlockApp') + '</h1>';

        html += '<p style="margin:2em 0;">';

        if (info.enableSupporterUnlock && info.enableAppUnlock) {
            html += Globalize.translate('MessageUnlockAppWithPurchaseOrSupporter');
        }
        else if (info.enableSupporterUnlock) {
            html += Globalize.translate('MessageUnlockAppWithSupporter');
        } else if (info.enableAppUnlock) {
            html += Globalize.translate('MessageUnlockAppWithPurchase');
        } else {
            html += '<span style="color:red;">';
            html += Globalize.translate('MessagePaymentServicesUnavailable');
            html += '</span>';
        }
        html += '</p>';

        if (info.enableSupporterUnlock) {
            html += '<p style="margin:2em 0;">';
            html += Globalize.translate('MessageToValidateSupporter');
            html += '</p>';
        }

        if (info.enableAppUnlock) {
            html += '<p style="margin:2em 0;">';
            html += Globalize.translate('MessageToValidateSupporter');
            html += '</p>';
        }

        if (info.enableAppUnlock) {
            html += '<button class="btn btnActionAccent btnAppUnlock" data-role="none" type="button"><span>' + Globalize.translate('ButtonUnlockWithPurchase') + '</span><i class="fa fa-check"></i></button>';
        }

        if (info.enableSupporterUnlock) {
            html += '<button class="btn btnSignInSupporter" data-role="none" type="button"><span>' + Globalize.translate('ButtonUnlockWithSupporter') + '</span><i class="fa fa-check"></i></button>';
        }

        html += '<button class="btn btnCancel" data-role="none" type="button"><span>' + Globalize.translate('ButtonCancel') + '</span><i class="fa fa-close"></i></button>';

        html += '</form>';

        html += '</div>';
        html += '</div>';

        $(document.body).append(html);

        return $('.inAppPurchaseOverlay');
    }

    function cancelInAppPurchase() {

        $('.inAppPurchaseOverlay').remove();
    }

    function showInAppPurchaseInfo(info, serverRegistrationInfo, deferred) {

        var elem = getInAppPurchaseElement(info);

        $('.inAppPurchaseForm', elem).on('submit', function () {

            return false;
        });

        $('.btnCancel', elem).on('click', function () {
            cancelInAppPurchase();

            // For testing purposes
            if (!info.enableSupporterUnlock && !info.enableAppUnlock) {
                deferred.resolve();
            } else {
                deferred.reject();
            }
        });
        $('.btnSignInSupporter', elem).on('click', function () {

            Dashboard.alert({
                message: 'MessagePleaseSignInLocalNetwork',
                callback: function () {
                    cancelInAppPurchase();
                    Dashboard.logout();
                }
            });
        });

        $('.btnAppUnlock', elem).on('click', function () {

            alert('coming soon');
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
            } else {
                deferred.resolve();
            }

            return deferred.promise();
        }
    };

    function validateProduct(product, callback) {

        // product attributes:
        // https://github.com/j3k0/cordova-plugin-purchase/blob/master/doc/api.md#validation-error-codes

        callback(true, {

        });

        //callback(true, { ... transaction details ... }); // success!

        //// OR
        //callback(false, {
        //    error: {
        //        code: store.PURCHASE_EXPIRED,
        //        message: "XYZ"
        //    }
        //});

        //// OR
        //callback(false, "Impossible to proceed with validation");  
    }

    function initializeStore() {

        // Let's set a pretty high verbosity level, so that we see a lot of stuff
        // in the console (reassuring us that something is happening).
        store.verbosity = store.INFO;

        store.validator = validateProduct;

        if (isAndroid) {
            store.register({
                id: "premiumunlock",
                alias: "premium features",
                type: store.NON_CONSUMABLE
            });
        } else {

            // iOS
            store.register({
                id: "appunlock",
                alias: "premium features",
                type: store.NON_CONSUMABLE
            });
        }

        // When purchase of the full version is approved,
        // show some logs and finish the transaction.
        store.when("premium feautres").approved(function (order) {
            log('You just unlocked the FULL VERSION!');
            order.finish();
        });

        // The play button can only be accessed when the user
        // owns the full version.
        store.when("premium feautres").updated(function (product) {

            updateProductInfo(product);
        });

        // When every goes as expected, it's time to celebrate!
        // The "ready" event should be welcomed with music and fireworks,
        // go ask your boss about it! (just in case)
        store.ready(function () {

            console.log("Store ready");

            // After we've done our setup, we tell the store to do
            // it's first refresh. Nothing will happen if we do not call store.refresh()
            store.refresh();
        });
    }

    // We must wait for the "deviceready" event to fire
    // before we can use the store object.
    initializeStore();

})();