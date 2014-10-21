if (!window.MediaBrowser) {
    window.MediaBrowser = {};
}

MediaBrowser.ConnectionManager = function () {

    return function () {

        var self = this;

        self.onConnectAuthenticated = function (result) {

            store.setItem('ConnectAccessToken', result.AccessToken);
            store.setItem('ConnectUserId', result.User.Id);
        };

        self.isLoggedIntoConnect = function () {

            return self.connectToken() && self.connectUserId();
        };

        self.logoutFromConnect = function () {
            store.removeItem('ConnectAccessToken');
            store.removeItem('ConnectUserId');
        };

        self.changeServer = function (currentApiClient, server) {

        };

        self.connectUserId = function () {
            return store.getItem('ConnectUserId');
        };

        self.connectToken = function () {
            return store.getItem('ConnectAccessToken');
        };

        self.getServers = function () {

            var url = "https://connect.mediabrowser.tv/service/servers?userId=" + self.connectUserId();

            return $.ajax({
                type: "GET",
                url: url,
                dataType: "json",
                headers: {
                    "X-Connect-UserToken": self.connectToken()
                }
            });
        };
    };

}();

window.ConnectionManager = new MediaBrowser.ConnectionManager();