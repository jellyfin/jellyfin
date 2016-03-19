define(['jQuery'], function ($) {

    function loadPage(page, config, languageOptions) {

        $('#selectLocalizationLanguage', page).html(languageOptions.map(function (l) {

            return '<option value="' + l.Value + '">' + l.Name + '</option>';

        })).val(config.UICulture);

        Dashboard.hideLoadingMsg();
    }

    function save(page) {

        Dashboard.showLoadingMsg();

        var apiClient = ApiClient;

        apiClient.getJSON(apiClient.getUrl('Startup/Configuration')).then(function (config) {

            config.UICulture = $('#selectLocalizationLanguage', page).val();

            apiClient.ajax({

                type: 'POST',
                data: config,
                url: apiClient.getUrl('Startup/Configuration')

            }).then(function () {

                Dashboard.navigate('wizarduser.html');

            });
        });

    }

    function onSubmit() {
        save($(this).parents('.page'));

        return false;
    }

    $(document).on('pageinit', "#wizardStartPage", function () {

        $('.wizardStartForm').off('submit', onSubmit).on('submit', onSubmit);

        window.ConnectionManager.clearData();


    }).on('pageshow', "#wizardStartPage", function () {

        Dashboard.showLoadingMsg();
        var page = this;

        var apiClient = ApiClient;

        var promise1 = apiClient.getJSON(apiClient.getUrl('Startup/Configuration'));

        var promise2 = apiClient.getJSON(apiClient.getUrl("Localization/Options"));

        Promise.all([promise1, promise2]).then(function (responses) {

            loadPage(page, responses[0], responses[1]);

        });
    });

});