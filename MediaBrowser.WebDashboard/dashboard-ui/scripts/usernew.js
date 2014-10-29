(function ($, window, document) {

    function loadUser(page, user) {

        $('#txtUserName', page).val(user.Name);
    }

    function saveUser(user, page) {

        user.Name = $('#txtUserName', page).val();

        ApiClient.createUser(user).done(function (newUser) {
            Dashboard.navigate("useredit.html?userId=" + newUser.Id);
        });
    }

    function newUserPage() {

        var self = this;

        self.onSubmit = function () {

            var page = $(this).parents('.page');

            Dashboard.showLoadingMsg();

            saveUser(getUser(), page);

            // Disable default form submission
            return false;
        };
    }

    function getUser() {

        return {};
    }

    function loadData(page) {

        loadUser(page, getUser());
    }

    window.NewUserPage = new newUserPage();

    $(document).on('pageshow', "#newUserPage", function () {

        var page = this;

        loadData(page);

    });

})(jQuery, window, document);