(function (window, $) {

    function loadPage(page, config, languageOptions) {

        $('#selectLocalizationLanguage', page).html(languageOptions.map(function (l) {

            return '<option value="' + l.Value + '">' + l.Name + '</option>';

        })).val(config.UICulture).selectmenu('refresh');

        Dashboard.hideLoadingMsg();
    }

    function save(page) {

        Dashboard.showLoadingMsg();

        var apiClient = ApiClient;

        apiClient.getServerConfiguration().done(function (config) {

            config.UICulture = $('#selectLocalizationLanguage', page).val();

            apiClient.updateServerConfiguration(config).done(function (result) {

                Dashboard.navigate('wizarduser.html');

            });
        });

    }

    $(document).on('pageshow', "#wizardStartPage", function () {

        Dashboard.showLoadingMsg();
        var page = this;

        var apiClient = ApiClient;

        apiClient.getPublicUsers().done(function (u) {

            var user = u.filter(function (i) {
                return i.Configuration.IsAdministrator;
            })[0];

            apiClient.authenticateUserByName(user.Name, '').done(function (result) {

                user = result.User;

                Dashboard.setCurrentUser(user.Id, result.AccessToken);

                var promise1 = apiClient.getServerConfiguration();

                var promise2 = apiClient.getJSON(apiClient.getUrl("Localization/Options"));

                $.when(promise1, promise2).done(function (response1, response2) {

                    loadPage(page, response1[0], response2[0]);

                });

            });

        });
    });

    window.WizardStartPage = {

        onSubmit: function () {

            save($(this).parents('.page'));

            return false;
        }
    };

})(window, jQuery);