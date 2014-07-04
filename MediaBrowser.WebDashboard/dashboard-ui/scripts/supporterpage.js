var SupporterPage = {
    onPageShow: function() {
        var page = this;

        $('#paypalReturnUrl', page).val(ApiClient.getUrl("supporterkey.html"));
        $('#cbxRecurring', '#supporterPage').change(function() {
            if (this.checked) {
                SupporterPage.addRecurringFields();
            } else {
                SupporterPage.removeRecurringFields();
            }
        });
    },
    
    addRecurringFields: function() {
        // Add recurring fields to form
        $("<input type='hidden' name='a3' class='pprecurring' />")
            .attr('value', $('#donateAmt', '#supporterPage').val())
            .appendTo("#payPalForm", '#supporterPage');
        $("<input type='hidden' name='p3' value='1' class='pprecurring' />")
            .appendTo("#payPalForm", '#supporterPage');
        $("<input type='hidden' name='t3' value='M' class='pprecurring' />")
            .appendTo("#payPalForm", '#supporterPage');
        $("<input type='hidden' name='src' value='1' class='pprecurring' />")
            .appendTo("#payPalForm", '#supporterPage');
        $("<input type='hidden' name='sra' value='1' class='pprecurring' />")
            .appendTo("#payPalForm", '#supporterPage');

        //change command for subscriptions
        $('#ppCmd', '#supporterPage').val('_xclick-subscriptions');
        
        $('#payPalForm', '#supporterPage').trigger('create');
            
    },
    
    removeRecurringFields: function() {
        $('.pprecurring', '#supporterPage').remove();
        
        //change command back
        $('#ppCmd', '#supporterPage').val('_xclick');

    },
    
};


$(document).on('pageshow', "#supporterPage", SupporterPage.onPageShow);

