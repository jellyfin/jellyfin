(function (window, $) {

    function loadPage(page, config, languageOptions) {

        $('#selectLocalizationLanguage', page).html(languageOptions.map(function (l) {

            return '<option value="' + l.Value + '">' + l.Name + '</option>';

        })).val(config.UICulture).selectmenu('refresh');

        Dashboard.hideLoadingMsg();
    }

    function save(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.UICulture = $('#selectLocalizationLanguage', page).val();

            ApiClient.updateServerConfiguration(config).done(function (result) {

                Dashboard.navigate('wizarduser.html');

            });
        });

    }

    $(document).on('pageshow', "#wizardStartPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();

        var promise2 = $.getJSON(ApiClient.getUrl("Localization/Options"));

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

    });

    window.WizardStartPage = {

        onSubmit: function () {

            save($(this).parents('.page'));

            return false;
        }
    };

})(window, jQuery);