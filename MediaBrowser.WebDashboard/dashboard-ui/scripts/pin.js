define([], function () {

    function onSubmit(e) {

        var form = e.target;

        Dashboard.showLoadingMsg();

        ApiClient.ajax({

            type: "POST",
            url: ApiClient.getUrl('Auth/Pin/Validate'),
            data: JSON.stringify({
                Pin: form.querySelector('#txtPin').value
            }),
            contentType: "application/json",
            dataType: 'json'

        }).then(function (result) {

            Dashboard.hideLoadingMsg();
            Dashboard.alert({
                message: Globalize.translate('PinCodeConfirmedMessage', result.AppName),
                title: Globalize.translate('HeaderThankYou'),
                callback: function () {
                    Dashboard.navigate('index.html');
                }
            });

        }, function () {

            Dashboard.hideLoadingMsg();
            Dashboard.alert({
                message: Globalize.translate('PinCodeInvalidMessage'),
                title: Globalize.translate('PinCodeInvalid')
            });
        });

        // Disable default form submission
        e.preventDefault();
        return false;
    }

    pageIdOn('pageinit', 'pinEntryPage', function () {

        var page = this;

        page.querySelector('form').addEventListener('submit', onSubmit);

        page.querySelector('.btnCancel').addEventListener('click', function () {
            Dashboard.navigate('mypreferencesmenu.html?userId=' + ApiClient.getCurrentUserId());
        });
    });

    pageIdOn('pageshow', 'pinEntryPage', function () {

        var page = this;

        var txtPin = page.querySelector('#txtPin');
        txtPin.focus();
        txtPin.value = '';
    });
});