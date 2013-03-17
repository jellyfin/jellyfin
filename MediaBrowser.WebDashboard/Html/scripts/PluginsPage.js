var PluginsPage = {

    onPageShow: function () {
        PluginsPage.reloadList();
    },

    reloadList: function () {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getInstalledPlugins();

        var promise2 = $.getJSON("configurationpages?pageType=PluginConfiguration");

        $.when(promise1, promise2).done(function(response1, response2) {

            PluginsPage.populateList(response1[0], response2[0]);

        });
    },

    populateList: function (plugins, pluginConfigurationPages) {

        var page = $($.mobile.activePage);

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

            var href = configPage ? Dashboard.getConfigurationPageUrl(configPage.Name) : "#";

            html += "<a href='" + href + "'>";

            html += "<h3>" + plugin.Name + "</h3>";

            html += "<p><strong>" + plugin.Version + "</strong></p>";

            html += "</a>";

            html += "<a data-id='" + plugin.Id + "' data-pluginname='" + plugin.Name + "' onclick='PluginsPage.deletePlugin(this);' href='#'>Delete</a>";

            html += "</li>";
        }

        $('#ulInstalledPlugins', page).html(html).listview('refresh');

        Dashboard.hideLoadingMsg();
    },

    deletePlugin: function (link) {

        var name = link.getAttribute('data-pluginname');
        var uniqueid = link.getAttribute('data-id');

        var msg = "Are you sure you wish to uninstall " + name + "?";

        Dashboard.confirm(msg, "Uninstall Plugin", function (result) {

            if (result) {
                Dashboard.showLoadingMsg();

                ApiClient.uninstallPlugin(uniqueid).done(function () {

                    PluginsPage.reloadList();
                });
            }
        });

    }
};

$(document).on('pageshow', "#pluginsPage", PluginsPage.onPageShow);