define(['appStorage', 'shell'], function (appStorage, shell) {

    var supporterPlaybackKey = 'lastSupporterPlaybackMessage4';

    function validatePlayback(resolve, reject) {

        Dashboard.getPluginSecurityInfo().then(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                resolve();
            } else {

                var lastMessage = parseInt(appStorage.getItem(supporterPlaybackKey) || '0');

                if (!lastMessage) {

                    // Don't show on the very first playback attempt
                    appStorage.setItem(supporterPlaybackKey, new Date().getTime());
                    resolve();
                }
                else if ((new Date().getTime() - lastMessage) > 259200000) {

                    showPlaybackOverlay(resolve, reject);
                } else {
                    resolve();
                }
            }
        });
    }

    function getSubscriptionBenefits() {

        var list = [];

        list.push({
            name: Globalize.translate('CoverArt'),
            icon: 'photo',
            text: Globalize.translate('CoverArtFeatureDescription')
        });

        list.push({
            name: Globalize.translate('HeaderFreeApps'),
            icon: 'check',
            text: Globalize.translate('FreeAppsFeatureDescription')
        });

        if (Dashboard.capabilities().SupportsSync) {
            list.push({
                name: Globalize.translate('HeaderMobileSync'),
                icon: 'sync',
                text: Globalize.translate('MobileSyncFeatureDescription')
            });
        }
        else {
            list.push({
                name: Globalize.translate('HeaderCinemaMode'),
                icon: 'movie',
                text: Globalize.translate('CinemaModeFeatureDescription')
            });
        }

        return list;
    }

    function getSubscriptionBenefitHtml(item) {

        var html = '';
        html += '<paper-icon-item>';

        html += '<paper-fab mini style="background-color:#52B54B;" icon="' + item.icon + '" item-icon></paper-fab>';

        html += '<paper-item-body three-line>';
        html += '<a class="clearLink" href="https://emby.media/premiere" target="_blank">';

        html += '<div>';
        html += item.name;
        html += '</div>';

        html += '<div secondary style="white-space:normal;">';
        html += item.text;
        html += '</div>';

        html += '</a>';
        html += '</paper-item-body>';

        html += '</paper-icon-item>';

        return html;
    }

    function showPlaybackOverlay(resolve, reject) {

        require(['dialogHelper', 'paper-fab', 'paper-item-body', 'paper-icon-item', 'emby-button'], function (dialogHelper) {

            var dlg = dialogHelper.createDialog({
                size: 'fullscreen-border',
                removeOnClose: true
            });

            dlg.classList.add('ui-body-b');
            dlg.classList.add('background-theme-b');
            dlg.classList.add('popupEditor');

            var html = '';
            html += '<h2 class="dialogHeader">';
            html += '<paper-fab icon="arrow-back" mini class="btnCancelSupporterInfo" tabindex="-1"></paper-fab>';
            html += '</h2>';

            html += '<div class="readOnlyContent" style="margin:0 auto 0;color:#fff;padding:1em;">';

            html += '<h1>' + Globalize.translate('HeaderTryEmbyPremiere') + '</h1>';

            html += '<p>' + Globalize.translate('MessageDidYouKnowCinemaMode') + '</p>';
            html += '<p>' + Globalize.translate('MessageDidYouKnowCinemaMode2') + '</p>';

            html += '<br/>';

            html += '<h1>' + Globalize.translate('HeaderBenefitsEmbyPremiere') + '</h1>';

            html += '<div class="paperList">';
            html += getSubscriptionBenefits().map(getSubscriptionBenefitHtml).join('');
            html += '</div>';

            html += '<br/>';

            html += '<a class="clearLink" href="http://emby.media/premiere" target="_blank"><button is="emby-button" type="button" class="raised submit block" autoFocus><i class="md-icon">check</i><span>' + Globalize.translate('ButtonBecomeSupporter') + '</span></button></a>';
            html += '<button is="emby-button" type="button" class="raised subdued block btnCancelSupporterInfo" style="background:#444;"><i class="md-icon">close</i><span>' + Globalize.translate('ButtonClosePlayVideo') + '</span></button>';

            html += '</div>';

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            // Has to be assigned a z-index after the call to .open() 
            dlg.addEventListener('close', function (e) {
                appStorage.setItem(supporterPlaybackKey, new Date().getTime());
                resolve();
            });

            dialogHelper.open(dlg);

            var onCancelClick = function () {
                dialogHelper.close(dlg);
            };
            var i, length;
            var elems = dlg.querySelectorAll('.btnCancelSupporterInfo');
            for (i = 0, length = elems.length; i < length; i++) {
                elems[i].addEventListener('click', onCancelClick);
            }
        });
    }

    function validateSync(resolve, reject) {

        Dashboard.getPluginSecurityInfo().then(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                resolve();
                return;
            }

            Dashboard.showLoadingMsg();

            ApiClient.getRegistrationInfo('Sync').then(function (registrationInfo) {

                Dashboard.hideLoadingMsg();

                if (registrationInfo.IsRegistered) {
                    resolve();
                    return;
                }

                Dashboard.alert({
                    message: Globalize.translate('HeaderSyncRequiresSupporterMembership') + '<br/><p><a href="http://emby.media/premiere" target="_blank">' + Globalize.translate('ButtonLearnMore') + '</a></p>',
                    title: Globalize.translate('HeaderSync'),
                    callback: reject
                });

            }, function () {

                reject();
                Dashboard.hideLoadingMsg();

                Dashboard.alert({
                    message: Globalize.translate('ErrorValidatingSupporterInfo')
                });
            });

        });
    }

    window.RegistrationServices = {
        renderPluginInfo: function (page, pkg, pluginSecurityInfo) {

            require(['jQuery'], function ($) {
                if (pkg.isPremium) {
                    $('.premiumPackage', page).show();

                    // Fill in registration info
                    var regStatus = "";
                    if (pkg.isRegistered) {

                        regStatus += "<p style='color:green;'>";

                        regStatus += Globalize.translate('MessageFeatureIncludedWithSupporter');

                    } else {

                        var expDateTime = new Date(pkg.expDate).getTime();
                        var nowTime = new Date().getTime();

                        if (expDateTime <= nowTime) {
                            regStatus += "<p style='color:red;'>";
                            regStatus += Globalize.translate('MessageTrialExpired');
                        } else if (expDateTime > new Date(1970, 1, 1).getTime()) {

                            regStatus += "<p style='color:blue;'>";
                            regStatus += Globalize.translate('MessageTrialWillExpireIn').replace('{0}', Math.round(expDateTime - nowTime) / (86400000));
                        }
                    }

                    regStatus += "</p>";
                    $('#regStatus', page).html(regStatus);

                    if (pluginSecurityInfo.IsMBSupporter) {
                        $('#regInfo', page).html(pkg.regInfo || "");

                        $('.premiumDescription', page).hide();
                        $('.supporterDescription', page).hide();

                        if (pkg.price > 0) {

                            $('.premiumHasPrice', page).show();
                            $('#featureId', page).val(pkg.featureId);
                            $('#featureName', page).val(pkg.name);
                            $('#amount', page).val(pkg.price);

                            $('#regPrice', page).html("<h3>" + Globalize.translate('ValuePriceUSD').replace('{0}', "$" + pkg.price.toFixed(2)) + "</h3>");
                            $('#ppButton', page).hide();

                            var url = "https://mb3admin.com/admin/service/user/getPayPalEmail?id=" + pkg.owner;

                            fetch(url).then(function (response) {

                                return response.json();

                            }).then(function (dev) {

                                if (dev.payPalEmail) {
                                    $('#payPalEmail', page).val(dev.payPalEmail);
                                    $('#ppButton', page).show();

                                }
                            });

                        } else {
                            // Supporter-only feature
                            $('.premiumHasPrice', page).hide();
                        }
                    } else {

                        if (pkg.price) {
                            $('.premiumDescription', page).show();
                            $('.supporterDescription', page).hide();
                            $('#regInfo', page).html("");

                        } else {
                            $('.premiumDescription', page).hide();
                            $('.supporterDescription', page).show();
                            $('#regInfo', page).html("");
                        }

                        $('#ppButton', page).hide();
                    }

                } else {
                    $('.premiumPackage', page).hide();
                }
            });
        },

        validateFeature: function (name) {

            return new Promise(function (resolve, reject) {
                if (name == 'playback') {
                    validatePlayback(resolve, reject);
                } else if (name == 'livetv') {
                    resolve();
                } else if (name == 'sync') {
                    validateSync(resolve, reject);
                } else {
                    resolve();
                }
            });
        },

        showPremiereInfo: function () {
            shell.openUrl('https://emby.media/premiere');
        }
    };

    return window.RegistrationServices;
});