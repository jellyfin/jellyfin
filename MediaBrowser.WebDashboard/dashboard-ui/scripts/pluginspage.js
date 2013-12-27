var PluginsPage = {

    onPageShow: function () {
        PluginsPage.reloadList(this);
    },

    reloadList: function (page) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getInstalledPlugins();

        var promise2 = $.getJSON("configurationpages?pageType=PluginConfiguration");

        $.when(promise1, promise2).done(function(response1, response2) {

            PluginsPage.populateList(page, response1[0], response2[0]);

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

            if (configPage) {
                html += "<a href='" + Dashboard.getConfigurationPageUrl(configPage.Name) + "'>";
            } else {
                html += "<a onclick='Dashboard.alert(\"Nothing to configure.\");' href='#'>";
            }

            html += "<h3>" + plugin.Name + "</h3>";

            html += "<p>" + plugin.Version + "</p>";

            html += "</a>";

            html += "<a data-id='" + plugin.Id + "' data-pluginname='" + plugin.Name + "' onclick='PluginsPage.deletePlugin(this);' href='#'>Delete</a>";

            html += "</li>";
        }

	    if (plugins.length == 0 || !plugins.length) {
		    html += '<li style="padding:5px;">You have no plugins installed. Browse our <a href="plugincatalog.html">plugin catalog</a> to view available plugins.</li>';
	        $('#ulInstalledPlugins', page).html(html);
	    }else {
	        $('#ulInstalledPlugins', page).html(html).listview('refresh');
        }


        Dashboard.hideLoadingMsg();
    },

    deletePlugin: function (link) {

        var page = $(link).parents('.page');
        var name = link.getAttribute('data-pluginname');
        var uniqueid = link.getAttribute('data-id');

        var msg = "Are you sure you wish to uninstall " + name + "?";

        Dashboard.confirm(msg, "Uninstall Plugin", function (result) {

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