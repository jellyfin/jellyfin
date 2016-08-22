define([], function () {

    return function (view, params) {

        var self = this;

        view.addEventListener('viewbeforeshow', function (e) {

            var elem = view.querySelector('#appVersionNumber');

            elem.innerHTML = elem.innerHTML.replace('{0}', ConnectionManager.appVersion());
        });
    }
});