(function () {

    var unlockAlias = "premium features";
    var updatedProducts = [];

    function updateProductInfo(p) {

        updatedProducts = updatedProducts.filter(function (r) {
            return r.id != p.id;
        });

        updatedProducts.push(p);
    }

    function normalizeId(id) {

        // This is what i named it in itunes
        id = id.replace('premiumunlock', 'appunlock');

        return id;
    }

    function getProduct(id) {

        id = normalizeId(id);

        var products = updatedProducts.filter(function (r) {
            return r.id == id;
        });

        return products.length ? products[0] : null;
    }

    function hasPurchased(id) {
        var product = getProduct(id);

        return product != null && product.owned;
    }

    function isPurchaseAvailable(id) {
        var product = getProduct(id);

        return product != null && product.canPurchase;
    }

    function beginPurchase(id) {

    }

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

        // iOS
        store.register({
            id: "appunlock",
            alias: unlockAlias,
            type: store.NON_CONSUMABLE
        });

        // When purchase of the full version is approved,
        // show some logs and finish the transaction.
        store.when(unlockAlias).approved(function (order) {
            log('You just unlocked the FULL VERSION!');
            order.finish();
        });

        // The play button can only be accessed when the user
        // owns the full version.
        store.when(unlockAlias).updated(function (product) {

            updateProductInfo(product);
        });

        // When every goes as expected, it's time to celebrate!
        // The "ready" event should be welcomed with music and fireworks,
        // go ask your boss about it! (just in case)
        store.ready(function () {

            console.log("Store ready");
        });

        // After we've done our setup, we tell the store to do
        // it's first refresh. Nothing will happen if we do not call store.refresh()
        store.refresh();
    }

    window.IapManager = {
        isPurchaseAvailable: isPurchaseAvailable,
        hasPurchased: hasPurchased,
        beginPurchase: beginPurchase
    };

    initializeStore();

})();