define(['jQuery'], function ($) {

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

        ApiClient.getUser(userId).then(function (user) {

            loadUser(page, user);

        });
    }

    $(document).on('pageinit', "#userPasswordPage", function () {

        $('.adminUpdatePasswordForm').off('submit', UpdatePasswordPage.onSubmit).on('submit', UpdatePasswordPage.onSubmit);
        $('.adminLocalAccessForm').off('submit', UpdatePasswordPage.onLocalAccessSubmit).on('submit', UpdatePasswordPage.onLocalAccessSubmit);

    }).on('pagebeforeshow', "#userPasswordPage", function () {

        var page = this;

        loadData(page);

    });

});