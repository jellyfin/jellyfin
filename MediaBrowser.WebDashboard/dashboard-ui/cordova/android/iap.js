(function () {

    var unlockId = "com.mb.android.unlock";
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

    function getProduct(id) {
        var products = updatedProducts.filter(function (r) {
            return r.id == id;
        });

        return products.length ? products[0] : null;
    }

    function isPurchaseAvailable(id) {

        return NativeIapManager.isStoreAvailable();
    }

    function beginPurchase(id) {
        return MainActivity.beginPurchase(id);
    }

    function onPurchaseComplete(result) {

        if (result) {
            refreshPurchases();
        }
    }

    function refreshPurchases() {
        NativeIapManager.isPurchased(unlockId, "window.IapManager.updateProduct");
    }

    window.IapManager = {
        isPurchaseAvailable: isPurchaseAvailable,
        getProductInfo: getProduct,
        updateProduct: updateProductInfo,
        beginPurchase: beginPurchase,
        onPurchaseComplete: onPurchaseComplete
    };

    refreshPurchases();

})();