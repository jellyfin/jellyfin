(function () {

    var supporterPlaybackKey = 'lastSupporterPlaybackMessage2';

    function validatePlayback(deferred) {

        Dashboard.getPluginSecurityInfo().done(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                deferred.resolve();
            } else {

                var lastMessage = parseInt(appStorage.getItem(supporterPlaybackKey) || '0');

                if (!lastMessage) {

                    // Don't show on the very first playback attempt
                    appStorage.setItem(supporterPlaybackKey, new Date().getTime());
                    deferred.resolve();
                }
                else if ((new Date().getTime() - lastMessage) > 345600000) {

                    showPlaybackOverlay(deferred);
                } else {
                    deferred.resolve();
                }
            }
        });
    }

    function showPlaybackOverlay(deferred) {

        require(['paperbuttonstyle']);

        var html = '';
        html += '<div class="supporterInfoOverlay" style="top: 0;left: 0;right: 0;bottom: 0;position: fixed;background-color:#1c1c1c;background-image: url(css/images/splash.jpg);background-position: center center;background-size: 100% 100%;background-repeat: no-repeat;z-index:1097;">';
        html += '<div style="background:rgba(0,0,0,.82);top: 0;left: 0;right: 0;bottom: 0;position: fixed;z-index:1098;font-size:14px;">';
        html += '<div class="readOnlyContent" style="margin:20px auto 0;color:#fff;padding:1em;">';

        html += '<h1>' + Globalize.translate('HeaderTryCinemaMode') + '</h1>';

        html += '<p>' + Globalize.translate('MessageDidYouKnowCinemaMode') + '</p>';
        html += '<p>' + Globalize.translate('MessageDidYouKnowCinemaMode2') + '</p>';

        html += '<br/>';

        html += '<a class="clearLink" href="http://emby.media/premiere" target="_blank"><paper-button raised class="submit block"><iron-icon icon="check"></iron-icon><span>' + Globalize.translate('ButtonBecomeSupporter') + '</span></paper-button></a>';
        html += '<paper-button raised class="subdued block btnCancelSupporterInfo" style="background:#444;"><iron-icon icon="close"></iron-icon><span>' + Globalize.translate('ButtonClosePlayVideo') + '</span></paper-button>';

        html += '</div>';
        html += '</div>';
        html += '</div>';

        $(document.body).append(html);

        $('.btnCancelSupporterInfo').on('click', function () {

            $('.supporterInfoOverlay').remove();
            appStorage.setItem(supporterPlaybackKey, new Date().getTime());
            deferred.resolve();
        });
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
                    message: Globalize.translate('HeaderSyncRequiresSupporterMembership') + '<br/><p><a href="http://emby.media/premiere" target="_blank">' + Globalize.translate('ButtonLearnMore') + '</a></p>',
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
                    }
                    else if (expDateTime > new Date(1970, 1, 1).getTime()) {

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

                        var url = "http://mb3admin.com/admin/service/user/getPayPalEmail?id=" + pkg.owner;

                        $.getJSON(url).done(function (dev) {
                            if (dev.payPalEmail) {
                                $('#payPalEmail', page).val(dev.payPalEmail);

                            } else {
                                $('#ppButton', page).hide();
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
        },

        validateFeature: function (name) {

            var deferred = DeferredBuilder.Deferred();

            if (name == 'playback') {
                validatePlayback(deferred);
            } else if (name == 'livetv') {
                deferred.resolve();
            } else if (name == 'sync') {
                validateSync(deferred);
            } else {
                deferred.resolve();
            }

            return deferred.promise();
        }
    };

})();
