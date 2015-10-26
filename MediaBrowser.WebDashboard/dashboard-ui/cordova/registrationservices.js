(function () {

    function validateServerManagement(deferred) {
        deferred.resolve();
    }

    function getRegistrationInfo(feature) {

        return ConnectionManager.getRegistrationInfo(feature, ApiClient);
    }

    var validatedFeatures = [];

    function validateFeature(feature, deferred) {

        if (validatedFeatures.indexOf(feature) != -1) {
            deferred.resolve();
            return;
        }

        var info = IapManager.getProductInfo(feature) || {};

        if (info.owned) {
            notifyServer(info.id);
            validatedFeatures.push(feature);
            deferred.resolve();
            return;
        }

        var unlockableProductInfo = IapManager.isPurchaseAvailable(feature) ? {
            enableAppUnlock: IapManager.isPurchaseAvailable(feature),
            id: info.id,
            price: info.price,
            feature: feature

        } : null;

        var prefix = $.browser.android ? 'android' : 'ios';

        // Get supporter status
        getRegistrationInfo(prefix + 'appunlock').done(function (registrationInfo) {

            if (registrationInfo.IsRegistered) {
                validatedFeatures.push(feature);
                deferred.resolve();
                return;
            }

            IapManager.getSubscriptionOptions().done(function (subscriptionOptions) {

                var dialogOptions = {
                    title: Globalize.translate('HeaderUnlockApp')
                };

                showInAppPurchaseInfo(subscriptionOptions, unlockableProductInfo, registrationInfo, dialogOptions, deferred);
            });

        }).fail(function () {
            deferred.reject();
        });
    }

    function notifyServer(id) {

        if (!$.browser.android) {
            return;
        }

        HttpClient.send({
            type: "POST",
            url: "https://mb3admin.com/admin/service/appstore/addDeviceFeature",
            data: {
                deviceId: ConnectionManager.deviceId(),
                feature: 'com.mb.android.unlock'
            },
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
            headers: {
                "X-EMBY-TOKEN": "EMBY_DEVICE"
            }

        }).done(function (result) {

            Logger.log('addDeviceFeature succeeded');

        }).fail(function () {
            Logger.log('addDeviceFeature failed');
        });
    }

    function cancelInAppPurchase() {

        var elem = document.querySelector('.inAppPurchaseOverlay');
        if (elem) {
            PaperDialogHelper.close(elem);
        }
    }

    var isCancelled = true;
    var currentDisplayingProductInfos = [];
    var currentDisplayingDeferred = null;

    function clearCurrentDisplayingInfo() {
        currentDisplayingProductInfos = [];
        currentDisplayingDeferred = null;
    }

    function showInAppPurchaseElement(subscriptionOptions, unlockableProductInfo, dialogOptions, deferred) {

        cancelInAppPurchase();

        // clone
        currentDisplayingProductInfos = subscriptionOptions.slice(0);

        if (unlockableProductInfo) {
            currentDisplayingProductInfos.push(unlockableProductInfo);
        }

        var dlg = PaperDialogHelper.createDialog();

        var html = '';
        html += '<h2 class="dialogHeader">';
        html += '<paper-fab icon="arrow-back" mini class="btnCloseDialog"></paper-fab>';
        html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + dialogOptions.title + '</div>';
        html += '</h2>';

        html += '<div class="editorContent">';

        html += '<form style="max-width: 800px;margin:auto;">';
        html += '<p style="margin:2em 0;">';

        if (unlockableProductInfo) {
            html += Globalize.translate('MessageUnlockAppWithPurchaseOrSupporter');
        }
        else {
            html += Globalize.translate('MessageUnlockAppWithSupporter');
        }
        html += '</p>';

        html += '<p style="margin:2em 0;">';
        html += Globalize.translate('MessageToValidateSupporter');
        html += '</p>';

        if (unlockableProductInfo) {

            var unlockText = Globalize.translate('ButtonUnlockWithPurchase');
            if (unlockableProductInfo.price) {
                unlockText = Globalize.translate('ButtonUnlockPrice', unlockableProductInfo.price);
            }
            html += '<p>';
            html += '<paper-button raised class="secondary block btnPurchase" data-feature="' + unlockableProductInfo.feature + '"><iron-icon icon="check"></iron-icon><span>' + unlockText + '</span></paper-button>';
            html += '</p>';
        }

        for (var i = 0, length = subscriptionOptions.length; i < length; i++) {

            html += '<p>';
            html += '<paper-button raised class="submit block btnPurchase" data-email="true" data-feature="' + subscriptionOptions[i].feature + '"><iron-icon icon="check"></iron-icon><span>';
            html += subscriptionOptions[i].buttonText;
            html += '</span></paper-button>';
            html += '</p>';
        }

        if (IapManager.restorePurchase) {
            html += '<p>';
            html += '<paper-button raised class="secondary block btnRestorePurchase" style="background-color: #673AB7;"><iron-icon icon="check"></iron-icon><span>' + Globalize.translate('ButtonRestorePreviousPurchase') + '</span></paper-button>';
            html += '</p>';
        }

        html += '</form>';
        html += '</div>';

        dlg.innerHTML = html;
        document.body.appendChild(dlg);

        initInAppPurchaseElementEvents(dlg, deferred);

        PaperDialogHelper.openWithHash(dlg, 'iap');

        $('.btnCloseDialog', dlg).on('click', function () {

            PaperDialogHelper.close(dlg);
        });

        $(dlg).on('iron-overlay-closed', function () {

            if (window.TabBar) {
                TabBar.show();
            }
        });

        dlg.classList.add('inAppPurchaseOverlay');
    }

    function initInAppPurchaseElementEvents(elem, deferred) {

        isCancelled = true;

        $('.btnPurchase', elem).on('click', function () {

            isCancelled = false;

            if (this.getAttribute('data-email') == 'true') {
                promptForEmail(this.getAttribute('data-feature'));
            } else {
                IapManager.beginPurchase(this.getAttribute('data-feature'));
            }
        });

        $('.btnRestorePurchase', elem).on('click', function () {

            isCancelled = false;
            IapManager.restorePurchase();
        });

        $(elem).on('iron-overlay-closed', function () {

            clearCurrentDisplayingInfo();

            if (isCancelled) {
                deferred.reject();
            }

            $(this).remove();
        });
    }

    function showInAppPurchaseInfo(subscriptionOptions, unlockableProductInfo, serverRegistrationInfo, dialogOptions, deferred) {

        require(['components/paperdialoghelper'], function () {

            if (window.TabBar) {
                TabBar.hide();
            }

            showInAppPurchaseElement(subscriptionOptions, unlockableProductInfo, dialogOptions, deferred);

            currentDisplayingDeferred = deferred;
        });
    }

    function promptForEmail(feature) {

        require(['prompt'], function (prompt) {

            prompt({
                text: Globalize.translate('TextPleaseEnterYourEmailAddressForSubscription'),
                title: Globalize.translate('HeaderEmailAddress'),
                callback: function (email) {

                    if (email) {
                        IapManager.beginPurchase(feature, email);
                    }
                }
            });
        });
    }

    function onProductUpdated(e, product) {

        var deferred = currentDisplayingDeferred;

        if (deferred && product.owned) {

            if (currentDisplayingProductInfos.filter(function (p) {

                return product.id == p.id;

            }).length) {

                isCancelled = false;

                cancelInAppPurchase();
                deferred.resolve();
            }
        }
    }

    function validateSync(deferred) {

        Dashboard.getPluginSecurityInfo().done(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                deferred.resolve();
                return;
            }

            // Get supporter status
            getRegistrationInfo('Sync').done(function (registrationInfo) {

                if (registrationInfo.IsRegistered) {
                    validatedFeatures.push(feature);
                    deferred.resolve();
                    return;
                }

                IapManager.getSubscriptionOptions().done(function (subscriptionOptions) {

                    var dialogOptions = {
                        title: Globalize.translate('HeaderUnlockSync')
                    };

                    showInAppPurchaseInfo(subscriptionOptions, null, registrationInfo, dialogOptions, deferred);
                });

            }).fail(function () {
                deferred.reject();
            });
        });
    }

    window.RegistrationServices = {

        renderPluginInfo: function (page, pkg, pluginSecurityInfo) {


        },

        validateFeature: function (name) {
            var deferred = DeferredBuilder.Deferred();

            if (name == 'playback') {
                validateFeature(name, deferred);
            } else if (name == 'livetv') {
                validateFeature(name, deferred);
            } else if (name == 'sync') {
                validateSync(deferred);
            } else {
                deferred.resolve();
            }

            return deferred.promise();
        }
    };

    function onIapManagerLoaded() {
        Events.on(IapManager, 'productupdated', onProductUpdated);
    }

    if ($.browser.android) {
        requirejs(['cordova/android/iap'], onIapManagerLoaded);
    } else {
        requirejs(['cordova/iap'], onIapManagerLoaded);
    }

})();