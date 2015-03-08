(function ($, document) {

    function reloadList(page) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getAvailablePlugins({
            TargetSystems: 'Server'
        });

        var promise2 = ApiClient.getInstalledPlugins();

        $.when(promise1, promise2).done(function (response1, response2) {
            populateList(page, response1[0], response2[0]);
        });
    }

    function populateList(page, availablePlugins, installedPlugins) {

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