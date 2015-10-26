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

        if (product.id == 'appunlock') {
            product.owned = false;
        }

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

        var id = getStoreFeatureId(feature);
        store.order(id);
    }

    function restorePurchase(id) {
        store.refresh();
    }

    var transactionIds = {};

    function updateOriginalTransactionInfo(transactionId, originalTransactionId) {

        alert('updateOriginalTransactionInfo ' + transactionId + '-' + originalTransactionId);

        if (!transactionId) {
            return;
        }
        if (!originalTransactionId) {
            return;
        }
        if (transactionId == 'null') {
            return;
        }
        if (originalTransactionId == 'null') {
            return;
        }

        transactionIds[transactionId] = originalTransactionId;
    }

    function validateProduct(product, callback) {

        // product attributes:
        // https://github.com/j3k0/cordova-plugin-purchase/blob/master/doc/api.md#validation-error-codes

        if (!product.transaction) {
            Logger.log('Transaction info missing. Failing validateProduct');
            callback(false, product);
            return;
        }

        if (!product.transaction.id) {
            Logger.log('Transaction id missing. Failing validateProduct');
            callback(false, product);
            return;
        }

        var productId = product.id;
        var transactionId = product.transaction.id;
        transactionId = transactionIds[transactionId] || transactionId;
        var receipt = product.transaction.appStoreReceipt;
        var price = product.price;

        var postData = {
            store: "Apple",
            application: "com.emby.mobile",
            product: productId,
            type: "Subscription",
            feature: "MBSClubMonthly",
            storeToken: receipt,
            amt: price,
            storeId: transactionId
        };

        if (enteredEmail) {
            postData.email = enteredEmail;
        }

        ApiClient.ajax({

            type: "POST",
            url: ApiClient.getUrl("Appstore/Register"),
            data: {
                Parameters: JSON.stringify(postData)
            }
        }).done(function () {

            alert('validate ok');
            callback(true, product);

        }).fail(function (e) {

            alert('validate fail');
            callback(false, product);
        });
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
                alert('verified');
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
                    product.verify();
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

            o.buttonText = Globalize.translate(o.buttonText, getProduct(o.feature).price);
            return o;
        });

        deferred.resolveWith(null, [options]);
        return deferred.promise();
    }

    window.IapManager = {
        isPurchaseAvailable: isPurchaseAvailable,
        getProductInfo: getProduct,
        beginPurchase: beginPurchase,
        restorePurchase: restorePurchase,
        getSubscriptionOptions: getSubscriptionOptions,
        updateOriginalTransactionInfo: updateOriginalTransactionInfo
    };

    initializeStore();

})();