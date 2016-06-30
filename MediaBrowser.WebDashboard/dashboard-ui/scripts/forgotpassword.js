define([], function () {

    function processForgotPasswordResult(result) {

        if (result.Action == 'ContactAdmin') {

            Dashboard.alert({

                message: Globalize.translate('MessageContactAdminToResetPassword'),
                title: Globalize.translate('HeaderForgotPassword')
            });
            return;
        }

        if (result.Action == 'InNetworkRequired') {

            Dashboard.alert({

                message: Globalize.translate('MessageForgotPasswordInNetworkRequired'),
                title: Globalize.translate('HeaderForgotPassword')
            });
            return;
        }

        if (result.Action == 'PinCode') {

            var msg = Globalize.translate('MessageForgotPasswordFileCreated');

            msg += "<br/>";
            msg += "<br/>";
            msg += result.PinFile;
            msg += "<br/>";

            Dashboard.alert({

                message: msg,
                title: Globalize.translate('HeaderForgotPassword')
            });
            return;
        }
    }

    return function (view, params) {

        function onSubmit(e) {

            ApiClient.ajax({

                type: 'POST',
                url: ApiClient.getUrl('Users/ForgotPassword'),
                dataType: 'json',
                data: {
                    EnteredUsername: view.querySelector('#txtName').value
                }

            }).then(processForgotPasswordResult);

            e.preventDefault();
            return false;
        }

        view.querySelector('form').addEventListener('submit', onSubmit);
    };

});