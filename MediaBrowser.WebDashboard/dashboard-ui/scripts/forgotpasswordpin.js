define(['jQuery'], function ($) {

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

                    window.location.href = 'login.html';
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

    function onSubmit() {

        var page = $(this).parents('.page');

        ApiClient.ajax({

            type: 'POST',
            url: ApiClient.getUrl('Users/ForgotPassword/Pin'),
            dataType: 'json',
            data: {
                Pin: $('#txtPin', page).val()
            }

        }).then(function (result) {

            processForgotPasswordResult(page, result);
        });
        return false;
    }

    $(document).on('pageinit', '.forgotPasswordPinPage', function () {

        var page = this;

        $('form', page).off('submit', onSubmit).on('submit', onSubmit);
    });

});