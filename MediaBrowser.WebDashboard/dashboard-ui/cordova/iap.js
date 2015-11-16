(function () {

    var updatedProducts = [];
    var enteredEmail;

    function getStoreFeatureId(feature) {

        if (feature == 'embypremieremonthly') {
            return 'emby.subscription.monthly';
        }

        return 'appunlock';
    }

    function updateProductInfo(product) {

        //if (product.id == 'appunlock') {
        //    product.owned = false;
        //}

        updatedProducts = updatedProducts.filter(function (r) {
            return r.id != product.id;
        });

        updatedProducts.push(product);

        Events.trigger(IapManager, 'productupdated', [product]);
    }

    function getProduct(feature) {

        var id = getStoreFeatureId(feature);

        var products = updatedProducts.filter(function (r) {
            return r.id == id;
        });

        return products.length ? products[0] : null;
    }

    function isPurchaseAvailable(feature) {

        var product = getProduct(feature);

        return product != null && product.valid /*&& product.canPurchase*/;
    }

    function beginPurchase(feature, email) {

        if (email) {
            enteredEmail = email;
        }

        validationCache = {};

        var id = getStoreFeatureId(feature);
        store.order(id);
    }

    function restorePurchase(id) {
        validationCache = {};
        store.refresh();
    }

    var validationCache = {};

    function validateProduct(product, callback) {

        var productId = product.id;

        // We should never get in here with the unlock, but in case we do
        if ((productId || '').toLowerCase().indexOf('appunlock') != -1) {
            callback(true, product);
            return;
        }

        var cacheKey = productId + (product.transaction.id || '');

        var cachedResult = validationCache[cacheKey];
        if (cachedResult && (new Date().getTime() - cachedResult.date) < 60000) {
            if (cachedResult.result) {
                callback(true, product);
            } else {
                callback(false, {
                    code: cachedResult.errorCode,
                    error: {
                        message: cachedResult.errorMessage
                    }
                });
            }
            return;
        }

        // product attributes:
        // https://github.com/j3k0/cordova-plugin-purchase/blob/master/doc/api.md#validation-error-codes

        var receipt = product.transaction.appStoreReceipt;
        var price = product.price;

        var postData = {
            store: "Apple",
            application: "com.emby.mobile",
            product: productId,
            type: "Subscription",
            storeToken: receipt,
            amt: price
        };

        var promise;

        if (enteredEmail) {
            postData.email = enteredEmail;
            postData.storeId = enteredEmail;
            postData.feature = "MBSClubMonthly";

            promise = ApiClient.ajax({
                type: "POST",
                url: ApiClient.getUrl("Appstore/Register"),
                data: {
                    Parameters: JSON.stringify(postData)
                }
            });

        } else {

            promise = HttpClient.send({
                type: "POST",
                url: "http://mb3admin.com/admin/service/appstore/register",
                data: JSON.stringify(postData),
                contentType: "application/json",
                headers: {
                    "X-Emby-Token": "EMBY-APPLE-VALIDATE"
                }
            });
        }

        promise.done(function () {

            setCachedResult(cacheKey, true);

            callback(true, product);

        }).fail(function (e) {

            if (e.status == 402) {

                setCachedResult(cacheKey, false, store.PURCHASE_EXPIRED, 'Subscription Expired');

                callback(false, {
                    code: store.PURCHASE_EXPIRED,
                    error: {
                        message: "Subscription Expired"
                    }
                });

            } else {
                //alert('validate fail - other ' + e.status);

                validationCache = {};

                callback(false, {
                    code: store.CONNECTION_FAILED,
                    error: {
                        message: "Connection Failure"
                    }
                });
            }
        });
    }

    function setCachedResult(key, result, code, message) {

        validationCache[key] = {
            date: new Date().getTime(),
            result: result,
            errorCode: code,
            errorMessage: message
        };
    }

    function initProduct(id, requiresVerification, type) {

        store.register({
            id: id,
            alias: id,
            type: type
        });

        // When purchase of the full version is approved,
        // show some logs and finish the transaction.
        store.when(id).approved(function (product) {

            //product.finish();
            if (requiresVerification) {
                product.verify();
            } else {
                product.finish();
            }
        });

        if (requiresVerification) {
            store.when(id).verified(function (p) {
                //alert('verified');
                updateProductInfo(p);
                p.finish();
            });
        }

        // The play button can only be accessed when the user
        // owns the full version.
        store.when(id).updated(function (product) {

            if (product.loaded && product.valid && product.state == store.APPROVED) {
                Logger.log('finishing previously created transaction');
                if (requiresVerification) {
                    //product.verify();
                    if (product.owned) {
                        //alert('sub owned!');
                    }
                } else {
                    product.finish();
                }
            }
            updateProductInfo(product);
        });
    }

    function initializeStore() {

        // Let's set a pretty high verbosity level, so that we see a lot of stuff
        // in the console (reassuring us that something is happening).
        store.verbosity = store.INFO;

        store.validator = validateProduct;

        initProduct(getStoreFeatureId(""), false, store.NON_CONSUMABLE);
        initProduct(getStoreFeatureId("embypremieremonthly"), true, store.PAID_SUBSCRIPTION);

        // When every goes as expected, it's time to celebrate!
        // The "ready" event should be welcomed with music and fireworks,
        // go ask your boss about it! (just in case)
        store.ready(function () {

            Logger.log("Store ready");
        });

        // After we've done our setup, we tell the store to do
        // it's first refresh. Nothing will happen if we do not call store.refresh()
        store.refresh();
    }

    function getSubscriptionOptions() {
        var deferred = DeferredBuilder.Deferred();

        var options = [];

        options.push({
            feature: 'embypremieremonthly',
            buttonText: 'EmbyPremiereMonthlyWithPrice'
        });

        options = options.filter(function (o) {
            return getProduct(o.feature) != null;

        }).map(function (o) {

            o.id = getStoreFeatureId(o.feature);
            o.buttonText = Globalize.translate(o.buttonText, getProduct(o.feature).price);
            o.owned = getProduct(o.feature).owned;
            return o;
        });

        deferred.resolveWith(null, [options]);
        return deferred.promise();
    }

    function isUnlockedOverride(feature) {

        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, [false]);
        return deferred.promise();
    }

    window.IapManager = {
        isPurchaseAvailable: isPurchaseAvailable,
        getProductInfo: getProduct,
        beginPurchase: beginPurchase,
        restorePurchase: restorePurchase,
        getSubscriptionOptions: getSubscriptionOptions,
        isUnlockedOverride: isUnlockedOverride
    };

    initializeStore();

})();