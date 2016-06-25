define([], function () {

    function getTabs() {
        return [
        {
            href: 'about.html',
            name: Globalize.translate('TabAbout')
        },
         {
             href: 'supporterkey.html',
             name: Globalize.translate('TabEmbyPremiere')
         }];
    }

    return function (view, params) {

        var self = this;

        view.addEventListener('viewbeforeshow', function (e) {
            LibraryMenu.setTabs('helpadmin', 0, getTabs);
            var elem = view.querySelector('#appVersionNumber');

            elem.innerHTML = elem.innerHTML.replace('{0}', ConnectionManager.appVersion());
        });
    }
});