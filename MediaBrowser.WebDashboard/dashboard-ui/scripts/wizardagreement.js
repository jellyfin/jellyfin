(function (window, $) {

    function onSubmit() {

        var page = $(this).parents('.page');

        if ($('#chkAccept', page).checked()) {
            Dashboard.navigate('wizardfinish.html');
        } else {

            Dashboard.alert({
                message: Globalize.translate('MessagePleaseAcceptTermsOfServiceBeforeContinuing'),
                title: ''
            });
        }

        return false;
    }

    $(document).on('pageinitdepends', '#wizardAgreementPage', function(){

    	$('.wizardAgreementForm').off('submit', onSubmit).on('submit', onSubmit);
    });

})(window, jQuery);