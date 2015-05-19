(function () {

    function addRecurringFields(period, page) {
        RegistrationServices.addRecurringFields(page, period);
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

    var lifeTimeAmount = 69.99;
    var dailyAmount = 1;
    var monthlyAmount = 4.99;
    var yearlyAmount = 35.99;
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
        if (type == 'daily') {
            return dailyAmount;
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
                    .html(Globalize.translate('MessageYouHaveALifetimeMembership'))
                    .css('color', 'green');

            }
            else if (info.IsActiveSupporter) {

                $('.planSummary', page)
                    .html(Globalize.translate('MessageYouHaveAnActiveRecurringMembership').replace('{0}', info.PlanType))
                    .css('color', 'green');

            }
            else if (info.IsExpiredSupporter) {

                var expirationDate = info.ExpirationDate ? parseISO8601Date(info.ExpirationDate, { toLocal: true }) : new Date();
                expirationDate = expirationDate.toLocaleDateString();

                $('.planSummary', page)
                    .html(Globalize.translate('MessageSupporterMembershipExpiredOn').replace('{0}', expirationDate))
                    .css('color', 'red');
            }
        });
    }

    function onSubmit() {
        var form = this;
        var page = $(form).parents('.page');

        if ($('.hfIsActive', page).val() == 'true') {

            var currentPlanType = $('.hfPlanType', page).val();

            if (currentPlanType != 'Lifetime') {

                // Use a regular alert to block the submission process until they hit ok
                alert(Globalize.translate('MessageChangeRecurringPlanConfirm'));
            }
        }
    }

    $(document).on('pageinitdepends', "#supporterPage", function () {

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
            else if (donationType == 'daily') {

                $('.fldOneTimeDonationAmount', page).hide();
                addRecurringFields('D', page);
                setItemNumber(page, 'MBSClubDaily');
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

        RegistrationServices.initSupporterForm(page);

        $('.supporterForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshowready', "#supporterPage", function () {

        var page = this;

        $('.lifetimeAmount', page).html('$' + lifeTimeAmount);
        $('.monthlyAmount', page).html('$' + monthlyAmount);
        $('.dailyAmount', page).html('$' + dailyAmount);
        $('.yearlyAmount', page).html('$' + yearlyAmount);

        $('#returnUrl', page).val(ApiClient.getUrl("supporterkey.html"));

        $('.radioDonationType', page).trigger('change');

        $('.benefits', page).html(Globalize.translate('HeaderSupporterBenefit', '<a href="http://emby.media/donate" target="_blank">', '</a>')).trigger('create');

        loadUserInfo(page);
    });

})();