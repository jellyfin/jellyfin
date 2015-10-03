(function () {

    var updatedProducts = [];

    function getStoreFeatureId(feature) {

        if (feature == 'embypremieremonthly') {
            return "emby.supporter.weekly";
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

    function isPurchaseAvailable(feature) {

        return NativeIapManager.isStoreAvailable();
    }

    function beginPurchase(feature, email) {
        var id = getStoreFeatureId(feature);
        return MainActivity.beginPurchase(id, email);
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
        updateProduct: updateProductInfo,
        beginPurchase: beginPurchase,
        onPurchaseComplete: onPurchaseComplete,
        getStoreFeatureId: getStoreFeatureId,
        getSubscriptionOptions: getSubscriptionOptions
    };

    refreshPurchases();

})();