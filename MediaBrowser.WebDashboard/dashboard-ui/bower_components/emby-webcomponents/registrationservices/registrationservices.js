define(['appSettings', 'loading', 'apphost', 'iapManager', 'events', 'shell', 'globalize', 'dialogHelper', 'connectionManager', 'layoutManager', 'emby-button'], function (appSettings, loading, appHost, iapManager, events, shell, globalize, dialogHelper, connectionManager, layoutManager) {

    var validatedFeatures = [];

    function alertText(options) {
        return new Promise(function (resolve, reject) {

            require(['alert'], function (alert) {
                alert(options).then(resolve, reject);
            });
        });
    }

    function showInAppPurchaseInfo(subscriptionOptions, unlockableProductInfo, dialogOptions) {

        return new Promise(function (resolve, reject) {

            require(['listViewStyle', 'formDialogStyle'], function () {
                showInAppPurchaseElement(subscriptionOptions, unlockableProductInfo, dialogOptions, resolve, reject);

                currentDisplayingResolve = resolve;
            });
        });
    }

    function validateFeature(feature) {

        console.log('validateFeature: ' + feature);

        if (validatedFeatures.indexOf(feature) != -1) {
            return Promise.resolve();
        }

        return iapManager.isUnlockedByDefault(feature).catch(function () {

            var unlockableFeatureCacheKey = 'featurepurchased-' + feature;
            if (appSettings.get(unlockableFeatureCacheKey) == '1') {
                return Promise.resolve();
            }

            var unlockableProduct = iapManager.getProductInfo(feature);
            if (unlockableProduct) {

                var unlockableCacheKey = 'productpurchased-' + unlockableProduct.id;
                if (unlockableProduct.owned) {

                    // Cache this to eliminate the store as a possible point of failure in the future
                    appSettings.set(unlockableFeatureCacheKey, '1');
                    appSettings.set(unlockableCacheKey, '1');
                    return Promise.resolve();
                }

                if (appSettings.get(unlockableCacheKey) == '1') {
                    return Promise.resolve();
                }
            }

            var unlockableProductInfo = unlockableProduct ? {
                enableAppUnlock: true,
                id: unlockableProduct.id,
                price: unlockableProduct.price,
                feature: feature

            } : null;

            return iapManager.getSubscriptionOptions().then(function (subscriptionOptions) {

                if (subscriptionOptions.filter(function (p) {
                    return p.owned;
                }).length > 0) {
                    return Promise.resolve();
                }

                // Get supporter status
                return connectionManager.getRegistrationInfo(iapManager.getAdminFeatureName(feature), connectionManager.currentApiClient()).catch(function () {

                    var dialogOptions = {
                        title: globalize.translate('sharedcomponents#HeaderUnlockFeature'),
                        feature: feature
                    };

                    return showInAppPurchaseInfo(subscriptionOptions, unlockableProductInfo, dialogOptions);
                });
            });
        });
    }

    function cancelInAppPurchase() {

        var elem = document.querySelector('.inAppPurchaseOverlay');
        if (elem) {
            dialogHelper.close(elem);
        }
    }

    var currentDisplayingProductInfos = [];
    var currentDisplayingResolve = null;

    function clearCurrentDisplayingInfo() {
        currentDisplayingProductInfos = [];
        currentDisplayingResolve = null;
    }

    function showExternalPremiereInfo() {
        shell.openUrl('https://emby.media/premiere');
    }

    function centerFocus(elem, horiz, on) {
        require(['scrollHelper'], function (scrollHelper) {
            var fn = on ? 'on' : 'off';
            scrollHelper.centerFocus[fn](elem, horiz);
        });
    }

    function showInAppPurchaseElement(subscriptionOptions, unlockableProductInfo, dialogOptions, resolve, reject) {

        cancelInAppPurchase();

        // clone
        currentDisplayingProductInfos = subscriptionOptions.slice(0);

        if (unlockableProductInfo) {
            currentDisplayingProductInfos.push(unlockableProductInfo);
        }

        var dlg = dialogHelper.createDialog({
            size: 'fullscreen-border',
            removeOnClose: true,
            scrollY: false
        });

        dlg.classList.add('formDialog');

        var html = '';
        html += '<div class="formDialogHeader">';
        html += '<button is="paper-icon-button-light" class="btnCloseDialog autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
        html += '<h3 class="formDialogHeaderTitle">';
        html += '</h3>';
        html += '</div>';

        html += '<div class="formDialogContent smoothScrollY">';
        html += '<div class="dialogContentInner dialog-content-centered">';
        html += '<form style="margin:auto;">';

        html += '<p style="margin-top:1.5em;">';

        if (unlockableProductInfo) {
            html += globalize.translate('sharedcomponents#MessageUnlockAppWithPurchaseOrSupporter');
        }
        else {
            html += globalize.translate('sharedcomponents#MessageUnlockAppWithSupporter');
        }
        html += '</p>';

        html += '<p style="margin:1.5em 0 2em;">';
        html += globalize.translate('sharedcomponents#MessageToValidateSupporter');
        html += '</p>';

        var hasProduct = false;

        for (var i = 0, length = subscriptionOptions.length; i < length; i++) {

            hasProduct = true;
            html += '<p>';
            html += '<button is="emby-button" type="button" class="raised button-submit block btnPurchase" data-email="' + (subscriptionOptions[i].requiresEmail !== false) + '" data-featureid="' + subscriptionOptions[i].id + '"><span>';
            html += subscriptionOptions[i].title;
            html += '</span></button>';
            html += '</p>';
        }

        if (unlockableProductInfo) {

            hasProduct = true;
            var unlockText = globalize.translate('sharedcomponents#ButtonUnlockWithPurchase');
            if (unlockableProductInfo.price) {
                unlockText = globalize.translate('sharedcomponents#ButtonUnlockPrice', unlockableProductInfo.price);
            }
            html += '<p>';
            html += '<button is="emby-button" type="button" class="raised secondary block btnPurchase" data-featureid="' + unlockableProductInfo.id + '"><span>' + unlockText + '</span></button>';
            html += '</p>';
        }

        html += '<p>';
        html += '<button is="emby-button" type="button" class="raised button-cancel block btnRestorePurchase"><span>' + iapManager.getRestoreButtonText() + '</span></button>';
        html += '</p>';

        if (subscriptionOptions.length) {
            html += '<h1 style="margin-top:1.5em;">' + globalize.translate('sharedcomponents#HeaderBenefitsEmbyPremiere') + '</h1>';

            html += '<div class="paperList" style="margin-bottom:1em;">';
            html += getSubscriptionBenefits().map(getSubscriptionBenefitHtml).join('');
            html += '</div>';
        }

        if (dialogOptions.feature == 'playback') {
            html += '<p>';
            html += '<button is="emby-button" type="button" class="raised button-cancel block btnCloseDialog"><span>' + globalize.translate('sharedcomponents#ButtonPlayOneMinute') + '</span></button>';
            html += '</p>';
        }

        html += '</form>';
        html += '</div>';
        html += '</div>';

        dlg.innerHTML = html;
        document.body.appendChild(dlg);

        var i, length;
        var btnPurchases = dlg.querySelectorAll('.btnPurchase');
        for (i = 0, length = btnPurchases.length; i < length; i++) {
            btnPurchases[i].addEventListener('click', onPurchaseButtonClick);
        }

        var btnPurchases = dlg.querySelectorAll('.buttonPremiereInfo');
        for (i = 0, length = btnPurchases.length; i < length; i++) {
            btnPurchases[i].addEventListener('click', showExternalPremiereInfo);
        }

        dlg.querySelector('.btnRestorePurchase').addEventListener('click', function () {
            restorePurchase(unlockableProductInfo);
        });

        loading.hide();

        var rejected = false;

        function onCloseButtonClick() {

            var onConfirmed = function () {
                rejected = true;
                dialogHelper.close(dlg);
            };

            if (dialogOptions.feature == 'playback') {
                alertText({
                    text: globalize.translate('sharedcomponents#ThankYouForTryingEnjoyOneMinute'),
                    title: globalize.translate('sharedcomponents#HeaderTryPlayback')
                }).then(onConfirmed);
            } else {
                onConfirmed();
            }
        }

        var btnCloseDialogs = dlg.querySelectorAll('.btnCloseDialog');
        for (var i = 0, length = btnCloseDialogs.length; i < length; i++) {
            btnCloseDialogs[i].addEventListener('click', onCloseButtonClick);
        }

        dlg.classList.add('inAppPurchaseOverlay');

        if (layoutManager.tv) {
            centerFocus(dlg.querySelector('.formDialogContent'), false, true);
        }

        dialogHelper.open(dlg).then(function () {
            if (layoutManager.tv) {
                centerFocus(dlg.querySelector('.formDialogContent'), false, false);
            }

            clearCurrentDisplayingInfo();
            if (rejected) {
                reject();
            }
        });
    }

    function getSubscriptionBenefits() {

        var list = [];

        list.push({
            name: globalize.translate('sharedcomponents#HeaderFreeApps'),
            icon: 'check',
            text: globalize.translate('sharedcomponents#FreeAppsFeatureDescription')
        });

        if (appHost.supports('sync')) {
            list.push({
                name: globalize.translate('sharedcomponents#HeaderOfflineDownloads'),
                icon: 'file_download',
                text: globalize.translate('sharedcomponents#HeaderOfflineDownloadsDescription')
            });
        }

        list.push({
            name: globalize.translate('sharedcomponents#CoverArt'),
            icon: 'photo',
            text: globalize.translate('sharedcomponents#CoverArtFeatureDescription')
        });

        list.push({
            name: globalize.translate('sharedcomponents#HeaderCinemaMode'),
            icon: 'movie',
            text: globalize.translate('sharedcomponents#CinemaModeFeatureDescription')
        });

        list.push({
            name: globalize.translate('sharedcomponents#HeaderCloudSync'),
            icon: 'sync',
            text: globalize.translate('sharedcomponents#CloudSyncFeatureDescription')
        });

        return list;
    }

    function getSubscriptionBenefitHtml(item) {

        var enableLink = appHost.supports('externalpremium');

        var html = '';

        var cssClass = "listItem";

        cssClass += ' listItem-button';

        if (layoutManager.tv) {
            cssClass += ' listItem-focusscale';
        }

        if (enableLink) {
            html += '<button type="button" class="' + cssClass + ' buttonPremiereInfo">';
        } else {
            html += '<div class="' + cssClass + '">';
        }

        html += '<i class="listItemIcon md-icon">' + item.icon + '</i>';

        html += '<div class="listItemBody">';

        html += '<h3 class="listItemBodyText">';
        html += item.name;
        html += '</h3>';

        html += '<div class="listItemBodyText secondary">';
        html += item.text;
        html += '</div>';

        if (enableLink) {
            html += '</div>';
        } else {
            html += '</button>';
        }

        return html;
    }

    function onPurchaseButtonClick() {

        var featureId = this.getAttribute('data-featureid');

        if (this.getAttribute('data-email') == 'true') {
            getUserEmail().then(function (email) {
                iapManager.beginPurchase(featureId, email);
            });
        } else {
            iapManager.beginPurchase(featureId);
        }
    }

    function restorePurchase(unlockableProductInfo) {

        var dlg = dialogHelper.createDialog({
            size: 'fullscreen-border',
            removeOnClose: true,
            scrollY: false
        });

        dlg.classList.add('formDialog');

        var html = '';
        html += '<div class="formDialogHeader">';
        html += '<button is="paper-icon-button-light" class="btnCloseDialog autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
        html += '<h3 class="formDialogHeaderTitle">';
        html += iapManager.getRestoreButtonText();
        html += '</h3>';
        html += '</div>';

        html += '<div class="formDialogContent smoothScrollY">';
        html += '<div class="dialogContentInner dialog-content-centered">';

        html += '<p style="margin:2em 0;">';
        html += globalize.translate('sharedcomponents#HowDidYouPay');
        html += '</p>';

        html += '<p>';
        html += '<button is="emby-button" type="button" class="raised button-cancel block btnRestoreSub"><span>' + globalize.translate('sharedcomponents#IHaveEmbyPremiere') + '</span></button>';
        html += '</p>';

        if (unlockableProductInfo) {
            html += '<p>';
            html += '<button is="emby-button" type="button" class="raised button-cancel block btnRestoreUnlock"><span>' + globalize.translate('sharedcomponents#IPurchasedThisApp') + '</span></button>';
            html += '</p>';
        }

        html += '</div>';
        html += '</div>';

        dlg.innerHTML = html;
        document.body.appendChild(dlg);

        loading.hide();

        if (layoutManager.tv) {
            centerFocus(dlg.querySelector('.formDialogContent'), false, true);
        }

        dlg.querySelector('.btnCloseDialog').addEventListener('click', function () {

            dialogHelper.close(dlg);
        });

        dlg.querySelector('.btnRestoreSub').addEventListener('click', function () {

            dialogHelper.close(dlg);
            alertText({
                text: globalize.translate('sharedcomponents#MessageToValidateSupporter'),
                title: 'Emby Premiere'
            });

        });

        var btnRestoreUnlock = dlg.querySelector('.btnRestoreUnlock');
        if (btnRestoreUnlock) {
            btnRestoreUnlock.addEventListener('click', function () {

                dialogHelper.close(dlg);
                iapManager.restorePurchase();
            });
        }

        dialogHelper.open(dlg).then(function () {

            if (layoutManager.tv) {
                centerFocus(dlg.querySelector('.formDialogContent'), false, false);
            }
        });
    }

    function getUserEmail() {

        if (connectionManager.isLoggedIntoConnect()) {

            var connectUser = connectionManager.connectUser();

            if (connectUser && connectUser.Email) {
                return Promise.resolve(connectUser.Email);
            }
        }

        return new Promise(function (resolve, reject) {

            require(['prompt'], function (prompt) {

                prompt({

                    label: globalize.translate('sharedcomponents#LabelEmailAddress')

                }).then(resolve, reject);
            });
        });
    }

    function onProductUpdated(e, product) {

        if (product.owned) {

            var resolve = currentDisplayingResolve;

            if (resolve && currentDisplayingProductInfos.filter(function (p) {

                return product.id == p.id;

            }).length) {

                cancelInAppPurchase();
                resolve();
            }
        }
    }

    function showPremiereInfo() {

        return iapManager.getSubscriptionOptions().then(function (subscriptionOptions) {

            var dialogOptions = {
                title: 'Emby Premiere',
                feature: 'sync'
            };

            return showInAppPurchaseInfo(subscriptionOptions, null, dialogOptions);
        });
    }

    events.on(iapManager, 'productupdated', onProductUpdated);

    return {

        validateFeature: validateFeature,
        showPremiereInfo: showPremiereInfo
    };
});