define([], function () {

    function navigateToComponents() {
        var apiClient = ApiClient;

        apiClient.getJSON(apiClient.getUrl('Startup/Info')).then(function (info) {

            if (info.HasMediaEncoder) {
                Dashboard.navigate('wizardagreement.html');

            } else {
                Dashboard.navigate('wizardcomponents.html');
            }
        });
    }

    return {
        navigateToComponents: navigateToComponents
    };
});