(function () {

    function addRecurringFields(period, page) {

        // Add recurring fields to form
        $("<input type='hidden' name='a3' class='pprecurring' />")
            .attr('value', $('#donateAmt', page).val())
            .appendTo("#payPalSupporterForm", page);

        $("<input type='hidden' name='p3' value='1' class='pprecurring' />")
            .appendTo("#payPalSupporterForm", page);

        $("<input type='hidden' name='t3' value='"+period+"' class='pprecurring' />")
            .appendTo("#payPalSupporterForm", page);

        $("<input type='hidden' name='src' value='1' class='pprecurring' />")
            .appendTo("#payPalSupporterForm", page);

        $("<input type='hidden' name='sra' value='1' class='pprecurring' />")
            .appendTo("#payPalSupporterForm", page);

        //change command for subscriptions
        $('#ppCmd', page).val('_xclick-subscriptions');

        $('#payPalSupporterForm', page).trigger('create');
        console.log($('#payPalSupporterForm', page).html());
    }

    function removeRecurringFields(page) {

        $('.pprecurring', page).remove();

        //change command back
        $('#ppCmd', page).val('_xclick');
    }

    function setItemNumber(page, itemNumber) {
        $('#ppItemNo', page).val(itemNumber);
    }

    function getDonationType(page) {

        return $(".radioDonationType:checked", page).val();
    }

    var lifeTimeAmount = 30;
    var monthlyAmount = 3;
    var yearlyAmount = 20;
    function getDonationAmount(page) {

        var type = getDonationType(page);

        if (type == 'once') {
            return $("#selectOneTimeDonationAmount", page).val();
        }
        if (type == 'yearly') {
            return yearlyAmount;
        }
        if (type == 'monthly') {
            return monthlyAmount;
        }

        // lifetime
        return lifeTimeAmount;
    }

    function updateSavedDonationAmount(page) {
        $("#donateAmt", page).val(getDonationAmount(page));
    }

    function loadUserInfo(page) {

        ApiClient.getJSON(ApiClient.getUrl('System/SupporterInfo')).done(function (info) {


            $('.hfPlanType', page).val(info.PlanType || '');
            $('.hfIsActive', page).val(info.IsActiveSupporter.toString());

            $('.radioDonationType', page).checked(false).checkboxradio('refresh');

            if (info.PlanType == 'Lifetime' && info.IsActiveSupporter) {

                // If they have an active lifetime plan, select the one-time option
                $('#radioOneTimeDonation', page).checked(true).checkboxradio('refresh');

            } else {

                // For all other statuses, select lifetime, to either acquire or upgrade
                $('#radioLifetimeSupporter', page).checked(true).checkboxradio('refresh');
            }

            $('.radioDonationType:checked', page).trigger('change');

            if (info.IsActiveSupporter || info.IsExpiredSupporter) {
                $('.currentPlanInfo', page).show();
            } else {
                $('.currentPlanInfo', page).hide();
            }

            if (info.IsActiveSupporter && info.PlanType == 'Lifetime') {

                $('.planSummary', page)
                    .html('You have a lifetime supporter club membership. You can provide additional donations on a one-time or recurring basis using the options below. Thank you for supporting Media Browser.')
                    .css('color', 'green');

            }
            else if (info.IsActiveSupporter) {

                $('.planSummary', page)
                    .html('You have an active ' + info.PlanType + ' membership. You can upgrade your plan using the options below.')
                    .css('color', 'green');

            }
            else if (info.IsExpiredSupporter) {

                var expirationDate = info.ExpirationDate ? parseISO8601Date(info.ExpirationDate, { toLocal: true }) : new Date();
                expirationDate = expirationDate.toLocaleDateString();

                $('.planSummary', page)
                    .html('Your ' + info.PlanType + ' membership expired on ' + expirationDate + '.')
                    .css('color', 'red');
            }
        });
    }

    $(document).on('pageinit', "#supporterPage", function () {

        var page = this;

        $('.radioDonationType', page).on('change', function () {

            var donationType = getDonationType(page);

            updateSavedDonationAmount(page);

            if (donationType == 'once') {
                $('.fldOneTimeDonationAmount', page).show();
                removeRecurringFields(page);
                setItemNumber(page, 'MBDonation');
                $('#oneTimeDescription').show();
            }
            else if (donationType == 'yearly') {

                $('.fldOneTimeDonationAmount', page).hide();
                addRecurringFields('Y', page);
                setItemNumber(page, 'MBSClubYearly');
                $('#oneTimeDescription').hide();
            }
            else if (donationType == 'monthly') {

                $('.fldOneTimeDonationAmount', page).hide();
                addRecurringFields('M', page);
                setItemNumber(page, 'MBSClubMonthly');
                $('#oneTimeDescription').hide();
            }
            else {
                // Lifetime
                $('.fldOneTimeDonationAmount', page).hide();
                removeRecurringFields(page);
                setItemNumber(page, 'MBSupporter');
                $('#oneTimeDescription').hide();
            }
        });

        $('#selectOneTimeDonationAmount', page).on('change', function () {

            updateSavedDonationAmount(page);
        });

    }).on('pageshow', "#supporterPage", function () {

        var page = this;

        $('.lifetimeAmount', page).html('$' + lifeTimeAmount);
        $('.monthlyAmount', page).html('$' + monthlyAmount);
        $('.yearlyAmount', page).html('$' + yearlyAmount);

        $('#paypalReturnUrl', page).val(ApiClient.getUrl("supporterkey.html"));

        $('.radioDonationType', page).trigger('change');

        loadUserInfo(page);
    });

    window.SupporterPage = {

        onSubmit: function () {

            var form = this;
            var page = $(form).parents('.page');

            if ($('.hfIsActive', page).val() == 'true') {

                var currentPlanType = $('.hfPlanType', page).val();

                if (currentPlanType != 'Lifetime') {

                    // Use a regular alert to block the submission process until they hit ok
                    alert('After completing this transaction you will need to cancel your previous recurring donation from within your PayPal account. Thank you for supporting Media Browser.');
                }
            }

        }

    };

})();