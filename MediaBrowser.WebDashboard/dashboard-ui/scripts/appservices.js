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

    function getCategories() {

        var context = getParameterByName('context');

        var categories = [];

        if (context == 'sync') {
            categories.push('Sync');
        }
        else if (context == 'livetv') {
            categories.push('Live TV');
        }

        return categories;
    }

    function renderInstalled(page, availablePlugins, installedPlugins) {

        var category = getCategories()[0];

        installedPlugins = installedPlugins.filter(function (i) {

            var catalogEntry = availablePlugins.filter(function (a) {
                return a.guid == i.Id;
            })[0];

            return catalogEntry && catalogEntry.category == category;

        });

        PluginsPage.renderPlugins(page, installedPlugins);
    }

    function renderCatalog(page, availablePlugins, installedPlugins) {

        var categories = getCategories();

        PluginCatalog.renderCatalog({

            catalogElement: $('.catalog', page),
            availablePlugins: availablePlugins,
            installedPlugins: installedPlugins,
            categories: categories,
            showCategory: false,
            context: getParameterByName('context'),
            targetSystem: 'Server'
        });

    }

    $(document).on('pagebeforeshow pageinit pageshow', "#appServicesPage", function () {

        // This needs both events for the helpurl to get done at the right time

        var page = this;

        var context = getParameterByName('context');

        $('.sectionTabs', page).hide();
        $('.' + context + 'SectionTabs', page).show();

        if (context == 'sync') {
            Dashboard.setPageTitle(Globalize.translate('TitleSync'));
            page.setAttribute('data-helpurl', 'https://github.com/MediaBrowser/Wiki/wiki/Sync');
        }
        else if (context == 'livetv') {
            Dashboard.setPageTitle(Globalize.translate('TitleLiveTV'));
            page.setAttribute('data-helpurl', 'https://github.com/MediaBrowser/Wiki/wiki/Live%20TV');
        }

    }).on('pageshow', "#appServicesPage", function () {

        // This needs both events for the helpurl to get done at the right time

        var page = this;

        reloadList(page);

        var context = getParameterByName('context');

        Dashboard.getPluginSecurityInfo().done(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter || context != 'sync') {
                $('.syncPromotion', page).hide();
            } else {
                $('.syncPromotion', page).show();
            }
        });

    });

})(jQuery, document);