(function ($, window, document) {

    function loadUser(page, user) {

        Dashboard.setPageTitle(user.Name);

        if (user.ConnectLinkType == 'Guest') {

            $('.connectMessage', page).show();
        }
        else {
            $('.connectMessage', page).hide();
        }

        Dashboard.hideLoadingMsg();
    }

    function loadData(page) {

        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");

        ApiClient.getUser(userId).done(function (user) {

            loadUser(page, user);

        });
    }

    $(document).on('pagebeforeshow', "#userPasswordPage", function () {

        var page = this;

        loadData(page);

    });

})(jQuery, window, document);