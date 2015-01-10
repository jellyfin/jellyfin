(function (window, $) {

    function onSubmit(page) {

        if ($('#chkAccept', page).checked()) {
            Dashboard.navigate('wizardfinish.html');
        } else {

            Dashboard.alert({
                message: Globalize.translate('MessagePleaseAcceptTermsOfServiceBeforeContinuing'),
                title: ''
            });
        }
    }

    window.WizardAgreementPage = {

        onSubmit: function () {

            var page = $(this).parents('.page');

            onSubmit(page);

            return false;
        }
    };

})(window, jQuery);