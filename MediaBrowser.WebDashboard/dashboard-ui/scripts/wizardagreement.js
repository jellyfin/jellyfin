define(['jQuery'], function ($) {

    function onSubmit() {

        var page = $(this).parents('.page')[0];

        if (page.querySelector('.chkAccept').checked) {
            Dashboard.navigate('wizardfinish.html');
        } else {

            Dashboard.alert({
                message: Globalize.translate('MessagePleaseAcceptTermsOfServiceBeforeContinuing'),
                title: ''
            });
        }

        return false;
    }

    $(document).on('pageinit', '#wizardAgreementPage', function () {

        $('.wizardAgreementForm').off('submit', onSubmit).on('submit', onSubmit);
    });

});