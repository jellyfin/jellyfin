(function () {

    var updatedProducts = [];

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

        var id;
        if (feature == 'embypremieremonthly') {
            id = NativeIapManager.getPremiereMonthlySku();
        } else {
            id = NativeIapManager.getUnlockProductSku();
        }

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

    function isPurchaseAvailable() {

        return storeReady;
    }

    function beginPurchase(feature, email) {

        if (feature == 'embypremieremonthly') {
            return MainActivity.purchasePremiereMonthly(email);
        }
        return MainActivity.purchaseUnlock();
    }

    function onPurchaseComplete(result) {

        if (result) {
            refreshPurchases();
        }
    }

    function refreshPurchases() {
        NativeIapManager.getPurchaseInfos("window.IapManager.updateProduct");
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

            var prod = getProduct(o.feature);
            o.buttonText = Globalize.translate(o.buttonText, prod.price);
            o.owned = prod.owned;
            return o;
        });

        deferred.resolveWith(null, [options]);
        return deferred.promise();
    }

    function isUnlockedOverride(feature) {

        var deferred = DeferredBuilder.Deferred();

        if (feature == 'playback' || feature == 'livetv') {
            deferred.resolveWith(null, [false]);
            //isPlaybackUnlockedViaOldApp(deferred);
        } else {
            deferred.resolveWith(null, [false]);
        }

        return deferred.promise();
    }

    function isPlaybackUnlockedViaOldApp(deferred) {

        testDeviceId(ConnectionManager.deviceId()).done(function (isUnlocked) {

            if (isUnlocked) {
                deferred.resolveWith(null, [true]);
                return;
            }

            testDeviceId(device.uuid).done(function (isUnlocked) {

                if (isUnlocked) {
                    deferred.resolveWith(null, [true]);
                    return;
                }

                deferred.resolveWith(null, [false]);
            });
        });
    }

    function testDeviceId(deviceId) {


        var cacheKey = 'oldapp-' + deviceId;
        var cacheValue = appStorage.getItem(cacheKey);
        if (cacheValue) {

            var deferred = DeferredBuilder.Deferred();
            deferred.resolveWith(null, [cacheValue == 'true']);
            return deferred.promise();

        } else {
            return HttpClient.send({

                type: 'GET',
                url: 'https://mb3admin.com/admin/service/statistics/appAccess?application=AndroidV1&deviceId=' + deviceId

            }).done(function () {

                appStorage.setItem(cacheKey, 'true');

            }).fail(function (e) {

                if (e.status == 404) {
                    appStorage.setItem(cacheKey, 'false');
                }
            });
        }
    }

    window.IapManager = {
        isPurchaseAvailable: isPurchaseAvailable,
        getProductInfo: getProduct,
        updateProduct: updateProductInfo,
        beginPurchase: beginPurchase,
        onPurchaseComplete: onPurchaseComplete,
        getSubscriptionOptions: getSubscriptionOptions,
        onStoreReady: onStoreReady,
        isUnlockedOverride: isUnlockedOverride
    };

    NativeIapManager.initStore();

})();