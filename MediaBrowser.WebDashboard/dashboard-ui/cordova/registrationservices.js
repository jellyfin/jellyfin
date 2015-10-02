(function () {

    function validateServerManagement(deferred) {
        deferred.resolve();
    }

    function getRegistrationInfo(feature) {

        return ConnectionManager.getRegistrationInfo(feature, ApiClient);
    }

    var validatedFeatures = [];

    function validateFeature(feature, deferred) {

        var id = IapManager.getStoreFeatureId(feature);

        if (validatedFeatures.indexOf(feature) != -1) {
            deferred.resolve();
            return;
        }

        var info = IapManager.getProductInfo(feature) || {};

        if (info.owned) {
            notifyServer(id);
            validatedFeatures.push(feature);
            deferred.resolve();
            return;
        }

        var productInfo = {
            enableAppUnlock: IapManager.isPurchaseAvailable(feature),
            id: id,
            price: info.price,
            feature: feature
        };

        var prefix = $.browser.android ? 'android' : 'ios';

        // Get supporter status
        getRegistrationInfo(prefix + 'appunlock').done(function (registrationInfo) {

            if (registrationInfo.IsRegistered) {
                validatedFeatures.push(feature);
                deferred.resolve();
                return;
            }

            IapManager.getSubscriptionOptions().done(function (subscriptionOptions) {

                showInAppPurchaseInfo(productInfo, subscriptionOptions, registrationInfo, deferred);
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

    function getInAppPurchaseElement(info, subscriptionOptions) {

        var dlg = PaperDialogHelper.createDialog();

        var html = '';
        html += '<h2 class="dialogHeader">';
        html += '<paper-fab icon="arrow-back" class="mini btnCloseDialog"></paper-fab>';
        html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + Globalize.translate('HeaderUnlockApp') + '</div>';
        html += '</h2>';

        html += '<div class="editorContent">';

        html += '<form style="max-width: 800px;margin:auto;">';
        html += '<p style="margin:2em 0;">';

        if (info.enableAppUnlock) {
            html += Globalize.translate('MessageUnlockAppWithPurchaseOrSupporter');
        }
        else {
            html += Globalize.translate('MessageUnlockAppWithSupporter');
        }
        html += '</p>';

        html += '<p style="margin:2em 0;">';
        html += Globalize.translate('MessageToValidateSupporter');
        html += '</p>';

        if (info.enableAppUnlock) {

            var unlockText = Globalize.translate('ButtonUnlockWithPurchase');
            if (info.price) {
                unlockText = Globalize.translate('ButtonUnlockPrice', info.price);
            }
            html += '<p>';
            html += '<paper-button raised class="secondary block btnPurchase" data-feature="' + info.feature + '"><iron-icon icon="check"></iron-icon><span>' + unlockText + '</span></paper-button>';
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

        // init dlg content here

        PaperDialogHelper.openWithHash(dlg, 'iap');

        $('.btnCloseDialog', dlg).on('click', function () {

            PaperDialogHelper.close(dlg);
        });

        dlg.classList.add('inAppPurchaseOverlay');

        return dlg;
    }

    function cancelInAppPurchase() {

        var elem = document.querySelector('.inAppPurchaseOverlay');
        if (elem) {
            PaperDialogHelper.close(elem);
        }
    }

    var currentDisplayingProductInfos = [];
    var currentDisplayingDeferred = null;
    var isCancelled = true;

    function clearCurrentDisplayingInfo() {
        currentDisplayingProductInfos = [];
        currentDisplayingDeferred = null;
    }

    function showInAppPurchaseInfo(info, subscriptionOptions, serverRegistrationInfo, deferred) {

        require(['components/paperdialoghelper'], function () {

            cancelInAppPurchase();
            isCancelled = true;

            var elem = getInAppPurchaseElement(info, subscriptionOptions);

            // clone
            currentDisplayingProductInfos = subscriptionOptions.slice(0);
            currentDisplayingProductInfos.push(info);

            currentDisplayingDeferred = deferred;

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
                IapManager.restorePurchase(info.feature);
            });

            $(elem).on('iron-overlay-closed', function () {

                if (isCancelled) {
                    clearCurrentDisplayingInfo();
                    cancelInAppPurchase();

                    deferred.reject();
                }
            });
        });
    }

    function promptForEmail(feature) {

        require(['prompt'], function (prompt) {

            prompt({
                text: Globalize.translate('TextPleaseEnterYourEmailAddressForSubscription'),
                title: Globalize.translate('HeaderEmailAddress'),
                callback: function(email) {
                    
                    if (email) {
                        IapManager.beginPurchase(this.getAttribute('data-feature'), email);
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
                clearCurrentDisplayingInfo();
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

            Dashboard.showLoadingMsg();

            ApiClient.getRegistrationInfo('Sync').done(function (registrationInfo) {

                Dashboard.hideLoadingMsg();

                if (registrationInfo.IsRegistered) {
                    deferred.resolve();
                    return;
                }

                Dashboard.alert({
                    message: Globalize.translate('HeaderSyncRequiresSupporterMembershipAppVersion'),
                    title: Globalize.translate('HeaderSync')
                });

            }).fail(function () {

                Dashboard.hideLoadingMsg();

                Dashboard.alert({
                    message: Globalize.translate('ErrorValidatingSupporterInfo')
                });
            });

        });
    }

    window.RegistrationServices = {

        renderPluginInfo: function (page, pkg, pluginSecurityInfo) {


        },

        addRecurringFields: function (page, period) {

        },

        initSupporterForm: function (page) {

            $('.recurringSubscriptionCancellationHelp', page).html('');
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