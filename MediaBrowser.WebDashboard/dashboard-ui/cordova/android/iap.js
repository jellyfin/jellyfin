(function () {

    var updatedProducts = [];

    function getStoreFeatureId(feature) {

        if (feature == 'embypremieremonthly') {
            return "emby.supporter.monthly";
        }

        return "com.mb.android.unlock";
    }

    function updateProductInfo(id, owned, price) {

        updatedProducts = updatedProducts.filter(function (r) {
            return r.id != id;
        });

        var product = {
            id: id,
            owned: owned,
            price: price
        };

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

    var storeReady = false;
    function onStoreReady() {
        storeReady = true;
        refreshPurchases();
    }

    function isPurchaseAvailable(feature) {

        return storeReady;
    }

    function beginPurchase(feature, email) {

        if (feature == 'embypremieremonthly') {
            return MainActivity.purchasePremiereMonthly(email);
        }
        if (feature == 'embypremiereweekly') {
            return MainActivity.purchasePremiereWeekly(email);
        }
        return MainActivity.purchaseUnlock(email);
    }

    function onPurchaseComplete(result) {

        if (result) {
            refreshPurchases();
        }
    }

    function refreshPurchases() {
        NativeIapManager.isPurchased(getStoreFeatureId("") + "|" + getStoreFeatureId("embypremieremonthly"), "window.IapManager.updateProduct");
        //NativeIapManager.isPurchased(getStoreFeatureId("embypremieremonthly"), "window.IapManager.updateProduct");
    }

    function getSubscriptionOptions() {
        var deferred = DeferredBuilder.Deferred();

        var options = [];

        options.push({
            feature: 'embypremieremonthly',
            buttonText: 'EmbyPremiereMonthly'
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
        updateProduct: updateProductInfo,
        beginPurchase: beginPurchase,
        onPurchaseComplete: onPurchaseComplete,
        getSubscriptionOptions: getSubscriptionOptions,
        onStoreReady: onStoreReady
    };

    NativeIapManager.initStore();

})();