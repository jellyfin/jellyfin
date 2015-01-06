(function (window, $) {

    function onSubmit(page) {

        if ($('#chkAccept', page).checked()) {
            Dashboard.navigate('wizardfinish.html');
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