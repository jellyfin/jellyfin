define(['loading'], function (loading) {

    function onFinish() {

        loading.show();

        ApiClient.ajax({

            url: ApiClient.getUrl('Startup/Complete'),
            type: 'POST'

        }).then(function () {

            Dashboard.navigate('dashboard.html');
            loading.hide();
        });
    }

    return function (view, params) {

        var self = this;

        view.querySelector('.btnWizardNext').addEventListener('click', onFinish);
    };

});