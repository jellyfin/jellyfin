define(['globalize', 'emby-checkbox', 'emby-button'], function (globalize) {

    function getTabs() {
        return [
        {
            href: 'library.html',
            name: globalize.translate('HeaderLibraries')
        },
         {
             href: 'librarydisplay.html',
             name: globalize.translate('TabDisplay')
         },
         {
             href: 'librarypathmapping.html',
             name: globalize.translate('TabPathSubstitution')
         },
         {
             href: 'librarysettings.html',
             name: globalize.translate('TabAdvanced')
         }];
    }

    return function (view, params) {

        var self = this;

        view.querySelector('form').addEventListener('submit', function (e) {

            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().then(function (config) {

                config.EnableFolderView = form.querySelector('.chkFolderView').checked;
                config.EnableGroupingIntoCollections = form.querySelector('.chkGroupMoviesIntoCollections').checked;
                config.DisplaySpecialsWithinSeasons = form.querySelector('.chkDisplaySpecialsWithinSeasons').checked;
                config.DisplayCollectionsView = form.querySelector('.chkDisplayCollectionView').checked;
                config.EnableChannelView = !form.querySelector('.chkDisplayChannelsInline').checked;
                config.EnableExternalContentInSuggestions = form.querySelector('.chkExternalContentInSuggestions').checked;

                ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult);
            });

            e.preventDefault();
            return false;
        });

        function loadData() {
            ApiClient.getServerConfiguration().then(function (config) {
                view.querySelector('.chkFolderView').checked = config.EnableFolderView;
                view.querySelector('.chkGroupMoviesIntoCollections').checked = config.EnableGroupingIntoCollections;
                view.querySelector('.chkDisplaySpecialsWithinSeasons').checked = config.DisplaySpecialsWithinSeasons;
                view.querySelector('.chkDisplayCollectionView').checked = config.DisplayCollectionsView;
                view.querySelector('.chkDisplayChannelsInline').checked = !(config.EnableChannelView || false);
                view.querySelector('.chkExternalContentInSuggestions').checked = config.EnableExternalContentInSuggestions;
            });
        }

        view.addEventListener('viewshow', function () {
            LibraryMenu.setTabs('librarysetup', 1, getTabs);
            loadData();
        });
    };
});