define([], function () {

    function navigateToComponents() {
        var apiClient = ApiClient;

        apiClient.getJSON(apiClient.getUrl('Startup/Info')).then(function (info) {

            if (info.HasMediaEncoder) {
                navigateToService();

            } else {
                Dashboard.navigate('wizardcomponents.html');
            }
        });
    }

    function navigateToService() {
        var apiClient = ApiClient;

        apiClient.getJSON(apiClient.getUrl('Startup/Info')).then(function (info) {

            if (info.SupportsRunningAsService) {
                Dashboard.navigate('wizardservice.html');

            } else {
                Dashboard.navigate('wizardagreement.html');
            }
        });
    }

    return {
        navigateToComponents: navigateToComponents,
        navigateToService: navigateToService
    };
});