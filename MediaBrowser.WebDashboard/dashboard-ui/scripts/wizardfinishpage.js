var WizardFinishPage = {

    onFinish: function () {

        ApiClient.ajax({

            url: ApiClient.getUrl('Startup/Complete'),
            type: 'POST'

        }).done(function () {

            Dashboard.navigate('dashboard.html');
        });
    }
};