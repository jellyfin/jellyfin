(function () {

    function addRecurringFields(page) {

        // Add recurring fields to form
        $("<input type='hidden' name='a3' class='pprecurring' />")
            .attr('value', $('#donateAmt', page).val())
            .appendTo("#payPalForm", page);

        $("<input type='hidden' name='p3' value='1' class='pprecurring' />")
            .appendTo("#payPalForm", page);

        $("<input type='hidden' name='t3' value='M' class='pprecurring' />")
            .appendTo("#payPalForm", page);

        $("<input type='hidden' name='src' value='1' class='pprecurring' />")
            .appendTo("#payPalForm", page);

        $("<input type='hidden' name='sra' value='1' class='pprecurring' />")
            .appendTo("#payPalForm", page);

        //change command for subscriptions
        $('#ppCmd', page).val('_xclick-subscriptions');

        $('#payPalForm', page).trigger('create');
    }

    function removeRecurringFields(page) {

        $('.pprecurring', page).remove();

        //change command back
        $('#ppCmd', page).val('_xclick');
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

    $(document).on('pageinit', "#supporterPage", function () {

        var page = this;

        $('.radioDonationType', page).on('change', function () {

            var donationType = getDonationType(page);

            updateSavedDonationAmount(page);

            if (donationType == 'once') {
                $('.fldOneTimeDonationAmount', page).show();
                removeRecurringFields(page);
                // TODO: Update item_number ?
            }
            else if (donationType == 'yearly') {

                $('.fldOneTimeDonationAmount', page).hide();
                addRecurringFields(page);
                // TODO: Update item_number ?
            }
            else if (donationType == 'monthly') {

                $('.fldOneTimeDonationAmount', page).hide();
                addRecurringFields(page);
                // TODO: Update item_number ?
            }
            else {
                // Lifetime
                $('.fldOneTimeDonationAmount', page).hide();
                removeRecurringFields(page);
                // TODO: Update item_number ?
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

        // TODO: Pull down supporter status
        // If already lifetime, had that option, but allow them to add monthly - many supporters probably will
        // If already monthly, hide monthly option
        // Or possibly not hide and select that option, but that will imply that changing the option will update their PP (can we do that?)
    });

})();