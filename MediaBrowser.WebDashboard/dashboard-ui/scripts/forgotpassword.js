(function (window) {

    function processForgotPasswordResult(page, result) {

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

    function onSubmit(page) {

        ApiClient.ajax({

            type: 'POST',
            url: ApiClient.getUrl('Users/ForgotPassword'),
            dataType: 'json',
            data: {
                EnteredUsername: $('#txtName', page).val()
            }

        }).done(function (result) {

            processForgotPasswordResult(page, result);
        });
    }

    window.ForgotPasswordPage = {

        onSubmit: function () {

            var page = $(this).parents('.page');

            onSubmit(page);
            return false;
        }

    };

})(window);