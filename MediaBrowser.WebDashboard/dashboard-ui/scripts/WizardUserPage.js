var WizardUserPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getUsers().done(function (users) {

            var user = users[0] || { Name: "User" };

            $('#txtUsername', page).val(user.Name);

            Dashboard.hideLoadingMsg();
        });

    },
    
    onSubmit: function() {        

        Dashboard.showLoadingMsg();

        var page = $.mobile.activePage;

        ApiClient.getUsers().done(function (users) {

            var user;
            
            if (users.length) {
                
                user = users[0];

                user.Name = $('#txtUsername', page).val();

                ApiClient.updateUser(user).done(WizardUserPage.saveComplete);
                
            } else {

                user = { Name: $('#txtUsername', page).val() };
                
                ApiClient.createUser(user).done(WizardUserPage.saveComplete);
            }

        });

        return false;
    },
    
    saveComplete: function () {
        
        Dashboard.hideLoadingMsg();

        Dashboard.navigate('wizardLibrary.html');
    }

};

$(document).on('pageshow', "#wizardUserPage", WizardUserPage.onPageShow);
