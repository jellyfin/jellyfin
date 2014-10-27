var PluginsPage = {

    onPageShow: function () {
        PluginsPage.reloadList(this);
    },

    reloadList: function (page) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getInstalledPlugins();

        var promise2 = ApiClient.getJSON(ApiClient.getUrl("dashboard/configurationpages") + "?pageType=PluginConfiguration");

        $.when(promise1, promise2).done(function (response1, response2) {

            PluginsPage.populateList(page, response1[0], response2[0]);

        });
    },

    showNoConfigurationMessage: function() {
        Dashboard.alert({
            message: Globalize.translate('NoPluginConfigurationMessage')
        });
    },

    showConnectMessage: function () {
        Dashboard.alert({
            message: Globalize.translate('MessagePluginConfigurationRequiresLocalAccess')
        });
    },

    populateList: function (page, plugins, pluginConfigurationPages) {

        plugins = plugins.sort(function (plugin1, plugin2) {

            return (plugin1.Name) > (plugin2.Name) ? 1 : -1;

        });

        var html = "";

        for (var i = 0, length = plugins.length; i < length; i++) {

            var plugin = plugins[i];

            var configPage = $.grep(pluginConfigurationPages, function (pluginConfigurationPage) {
                return pluginConfigurationPage.PluginId == plugin.Id;
            })[0];

            html += "<li>";

            if (Dashboard.isConnectMode()) {
                html += "<a onclick='PluginsPage.showConnectMessage();' href='#'>";
            }
            else if (configPage) {
                html += "<a href='" + Dashboard.getConfigurationPageUrl(configPage.Name) + "'>";
            }
            else {
                html += "<a onclick='PluginsPage.showNoConfigurationMessage();' href='#'>";
            }

            html += "<h3>" + plugin.Name + "</h3>";

            html += "<p>" + plugin.Version + "</p>";

            html += "</a>";

            html += "<a data-id='" + plugin.Id + "' data-pluginname='" + plugin.Name + "' onclick='PluginsPage.deletePlugin(this);' href='#'>" + Globalize.translate('Delete') + "</a>";

            html += "</li>";
        }

        if (!plugins.length) {

            html += '<li style="padding:5px;">';
            html += '<p>' + Globalize.translate('MessageNoPluginsInstalled') + '</p>';
            html += '<p><a href="plugincatalog.html">';
            html += Globalize.translate('BrowsePluginCatalogMessage');
            html += '</a></p>';
            html += '</li>';

            $('#ulInstalledPlugins', page).html(html);
        } else {
            $('#ulInstalledPlugins', page).html(html).listview('refresh');
        }

        Dashboard.hideLoadingMsg();
    },

    deletePlugin: function (link) {

        var page = $(link).parents('.page');
        var name = link.getAttribute('data-pluginname');
        var uniqueid = link.getAttribute('data-id');

        var msg = Globalize.translate('UninstallPluginConfirmation').replace("{0}", name);

        Dashboard.confirm(msg, Globalize.translate('UninstallPluginHeader'), function (result) {

            if (result) {
                Dashboard.showLoadingMsg();

                ApiClient.uninstallPlugin(uniqueid).done(function () {

                    PluginsPage.reloadList(page);
                });
            }
        });

    }
};

$(document).on('pageshow', "#pluginsPage", PluginsPage.onPageShow);