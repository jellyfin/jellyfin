(function ($, document) {

    var guideController;

    function init(page, type, providerId) {

        var url = 'tvproviders/' + type + '.js';

        require([url], function (factory) {

            var instance = new factory(page, providerId, {
                showCancelButton: false,
                showSubmitButton: false
            });

            instance.init();
            guideController = instance;
        });
    }

    function loadTemplate(page, type, providerId) {

        guideController = null;

        ApiClient.ajax({

            type: 'GET',
            url: 'tvproviders/' + type + '.template.html'

        }).done(function (html) {

            var elem = page.querySelector('.providerTemplate');
            elem.innerHTML = Globalize.translateDocument(html);
            $(elem).trigger('create');

            init(page, type, providerId);
        });
    }

    function skip() {
        var apiClient = ApiClient;

        apiClient.getJSON(apiClient.getUrl('Startup/Info')).done(function (info) {

            if (info.SupportsRunningAsService) {
                Dashboard.navigate('wizardservice.html');

            } else {
                Dashboard.navigate('wizardagreement.html');
            }
        });
    }

    function next() {
        guideController.submit();
    }

    $(document).on('pageinitdepends', "#wizardGuidePage", function () {

        var page = this;

        $('#selectType', page).on('change', function () {

            loadTemplate(page, this.value);
        });

        $('.btnSkip', page).on('click', skip);
        $('.btnNext', page).on('click', next);

    }).on('pageshowready', "#wizardGuidePage", function () {

        var page = this;

        $('#selectType', page).trigger('change');
    });

})(jQuery, document, window);
