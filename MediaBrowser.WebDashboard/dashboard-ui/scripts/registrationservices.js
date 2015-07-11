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

    addRecurringFields: function (page, period) {

        var form = page.querySelector('.supporterForm');

        // Add recurring fields to form
        $("<input type='hidden' name='a3' class='pprecurring' />")
            .attr('value', $('#donateAmt', page).val())
            .appendTo(form);

        $("<input type='hidden' name='p3' value='1' class='pprecurring' />")
            .appendTo(form);

        $("<input type='hidden' name='t3' value='" + period + "' class='pprecurring' />")
            .appendTo(form);

        $("<input type='hidden' name='src' value='1' class='pprecurring' />")
            .appendTo(form);

        $("<input type='hidden' name='sra' value='1' class='pprecurring' />")
            .appendTo(form);

        //change command for subscriptions
        $('#ppCmd', page).val('_xclick-subscriptions');

        Events.trigger(form, 'create');

    },

    initSupporterForm: function (page) {

        $('.supporterForm', page).attr('action', 'https://www.paypal.com/cgi-bin/webscr');
        $('.recurringSubscriptionCancellationHelp', page).html(Globalize.translate('LabelRecurringDonationCanBeCancelledHelp'));
    },

    validateFeature: function () {
        var deferred = DeferredBuilder.Deferred();
        deferred.resolve();
        return deferred.promise();
    }
};