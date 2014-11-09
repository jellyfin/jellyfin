(function (window) {

    function processForgotPasswordResult(page, result) {

        if (result.Success) {

            var msg = Globalize.translate('MessagePasswordResetForUsers');

            msg += '<br/>';
            msg += '<br/>';
            msg += result.UsersReset.join('<br/>');

            Dashboard.alert({

                message: msg,
                title: Globalize.translate('HeaderPasswordReset'),

                callback: function () {

                    window.location = 'login.html';
                }
            });
            return;
        }

        Dashboard.alert({

            message: Globalize.translate('MessageInvalidForgotPasswordPin'),
            title: Globalize.translate('HeaderPasswordReset')
        });
        return;
    }

    function onSubmit(page) {

        ApiClient.ajax({

            type: 'POST',
            url: ApiClient.getUrl('Users/ForgotPassword/Pin'),
            dataType: 'json',
            data: {
                Pin: $('#txtPin', page).val()
            }

        }).done(function (result) {

            processForgotPasswordResult(page, result);
        });
    }

    window.ForgotPasswordPinPage = {

        onSubmit: function () {

            var page = $(this).parents('.page');

            onSubmit(page);
            return false;
        }

    };

})(window);