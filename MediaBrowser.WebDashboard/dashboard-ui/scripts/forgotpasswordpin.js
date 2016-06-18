define([], function () {

    function processForgotPasswordResult(result) {

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

    return function (view, params) {

        function onSubmit(e) {

            ApiClient.ajax({

                type: 'POST',
                url: ApiClient.getUrl('Users/ForgotPassword/Pin'),
                dataType: 'json',
                data: {
                    Pin: view.querySelector('#txtPin').value
                }

            }).then(processForgotPasswordResult);

            e.preventDefault();
            return false;
        }

        view.querySelector('form').addEventListener('submit', onSubmit);
    };
});