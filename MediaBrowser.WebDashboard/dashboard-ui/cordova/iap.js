(function () {

    var updatedProducts = [];

    function getStoreFeatureId(feature) {

        if (feature == 'embypremieremonthly') {
            return 'emby.subscription.monthly';
        }

        return 'appunlock';
    }

    function updateProductInfo(product) {

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
        var id = getStoreFeatureId(feature);
        store.order(id);
    }

    function restorePurchase(id) {
        store.refresh();
    }

    function validateProduct(product, callback) {

        // product attributes:
        // https://github.com/j3k0/cordova-plugin-purchase/blob/master/doc/api.md#validation-error-codes

        alert(JSON.stringify(product.transaction));

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

    function initProduct(id, alias, type) {

        store.register({
            id: id,
            alias: alias,
            type: type
        });

        // When purchase of the full version is approved,
        // show some logs and finish the transaction.
        store.when(id).approved(function (product) {

            if (product.type == store.PAID_SUBSCRIPTION) {
                product.verify();
            } else {
                product.finish();
            }
        });

        store.when(id).verified(function (p) {
            p.finish();
        });

        // The play button can only be accessed when the user
        // owns the full version.
        store.when(id).updated(function (product) {

            if (product.loaded && product.valid && product.state == store.APPROVED) {
                Logger.log('finishing previously created transaction');
                product.finish();
            }
            updateProductInfo(product);
        });
    }

    function initializeStore() {

        // Let's set a pretty high verbosity level, so that we see a lot of stuff
        // in the console (reassuring us that something is happening).
        store.verbosity = store.INFO;

        store.validator = validateProduct;

        initProduct(getStoreFeatureId(""), "premium features", store.NON_CONSUMABLE);
        initProduct(getStoreFeatureId("embypremieremonthly"), "emby premiere monthly", store.PAID_SUBSCRIPTION);

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
        getStoreFeatureId: getStoreFeatureId,
        getSubscriptionOptions: getSubscriptionOptions
    };

    initializeStore();

})();