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

    function validateFeature(info, deferred) {

        var products = updatedProducts.filter(function (r) {
            return r.alias == info.alias;
        });

        var product = products.length ? products[0] : null;

        if (product && product.owned) {
            deferred.resolve();
            return;
        }

        // Get supporter status
        ConnectionManager.getRegistrationInfo('appunlock', ApiClient).done(function (registrationInfo) {

            if (registrationInfo.IsRegistered) {
                deferred.resolve();
                return;
            }

            showInAppPurchaseInfo(info, product, registrationInfo, deferred);

        }).fail(function () {
            deferred.reject();
        });
    }

    function showInAppPurchaseInfo(info, product, serverRegistrationInfo, deferred) {

        var requiresLocalValidation = serverRegistrationInfo.IsLocalValidationRequired;
        var canPurchase = product != null && product.canPurchase;

        // Can only purchase if product != null
        deferred.resolve();
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