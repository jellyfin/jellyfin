(function () {

    $(document).on('pageshow', "#supporterPage", function () {

        var page = this;
        
        $('#paypalReturnUrl', page).val(ApiClient.getUrl("supporterkey.html"));

    });

})();