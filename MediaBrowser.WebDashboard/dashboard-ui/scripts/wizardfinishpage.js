var WizardFinishPage = {

    onFinish: function () {

        ApiClient.getServerConfiguration().done(function (config) {

            config.IsStartupWizardCompleted = true;
            
            // Try migrate everyone over to this. Eventually remove the config setting.
            config.EnablePeoplePrefixSubFolders = true;

            ApiClient.updateServerConfiguration(config).done(function () {

                ApiClient.getUsers().done(function (users) {

                    for (var i = 0, length = users.length; i < length; i++) {

                        if (users[i].Configuration.IsAdministrator) {
                            Dashboard.setCurrentUser(users[i].Id);
                            break;
                        }

                    }
                    
                    Dashboard.navigate('dashboard.html');
                });
            });
        });

    }

};