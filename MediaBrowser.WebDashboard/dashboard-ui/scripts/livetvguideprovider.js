define(['events'], function (events) {

    function onListingsSubmitted() {

        Dashboard.navigate('livetvstatus.html');
    }

    function init(page, type, providerId) {

        var url = 'components/tvproviders/' + type + '.js';

        require([url], function (factory) {

            var instance = new factory(page, providerId, {
            });

            events.on(instance, 'submitted', onListingsSubmitted);

            instance.init();
        });
    }

    function loadTemplate(page, type, providerId) {

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/tvproviders/' + type + '.template.html', true);

        xhr.onload = function (e) {

            var html = this.response;
            var elem = page.querySelector('.providerTemplate');
            elem.innerHTML = Globalize.translateDocument(html);

            init(page, type, providerId);
        }

        xhr.send();
    }

    pageIdOn('pageshow', "liveTvGuideProviderPage", function () {

        Dashboard.showLoadingMsg();

        var providerId = getParameterByName('id');
        var type = getParameterByName('type');
        var page = this;
        loadTemplate(page, type, providerId);
    });

});
