(function ($, document) {

    function reloadList(page) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getAvailablePlugins({
            TargetSystems: 'Server'
        });

        var promise2 = ApiClient.getInstalledPlugins();

        $.when(promise1, promise2).done(function (response1, response2) {
            renderInstalled(page, response1[0], response2[0]);
            renderCatalog(page, response1[0], response2[0]);
        });
    }

    function renderInstalled(page, availablePlugins, installedPlugins) {

        installedPlugins = installedPlugins.filter(function (i) {

            var catalogEntry = availablePlugins.filter(function (a) {
                return a.guid == i.Id;
            })[0];

            return catalogEntry && catalogEntry.category == 'Sync';

        });

        PluginsPage.renderPlugins(page, installedPlugins);
    }

    function renderCatalog(page, availablePlugins, installedPlugins) {

        PluginCatalog.renderCatalog({

            catalogElement: $('.catalog', page),
            availablePlugins: availablePlugins,
            installedPlugins: installedPlugins,
            categories: ['Sync'],
            showCategory: false,
            context: 'sync'
        });

    }

    $(document).on('pageshow', "#syncServicesPage", function () {

        var page = this;

        reloadList(page);
    });

})(jQuery, document);