var PluginCatalogPage = {

    onPageShow: function () {
        PluginCatalogPage.reloadList();
    },

    reloadList: function () {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getAvailablePlugins();

        var promise2 = ApiClient.getInstalledPlugins();

        $.when(promise1, promise2).done(function (response1, response2) {

            PluginCatalogPage.populateList(response1[0], response2[0]);
        });
    },

    populateList: function (availablePlugins, installedPlugins) {

        var page = $($.mobile.activePage);

        availablePlugins = availablePlugins.filter(function (p) {

            return p.type == "UserInstalled";

        }).sort(function (a, b) {

            return a.name > b.name ? 1 : -1;

        });

        var html = "";

        for (var i = 0, length = availablePlugins.length; i < length; i++) {

            var plugin = availablePlugins[i];

            html += "<div class='posterViewItem'><a href='addPlugin.html?name=" + encodeURIComponent(plugin.name) + "'>";

            if (plugin.thumbImage) {
                html += "<img src='" + plugin.thumbImage + "' />";
            } else {
                html += "<img style='background:#444444;' src='css/images/items/list/collection.png' />";
            }

            if (plugin.isPremium) {
                if (plugin.price > 0) {
                    html += "<div class='premiumBanner'><img src='css/images/supporter/premiumflag.png' /></div>";
                } else {
                    html += "<div class='premiumBanner'><img src='css/images/supporter/supporterflag.png' /></div>";
                }
            }

            var color = plugin.tileColor || Dashboard.getRandomMetroColor();

            html += "<div class='posterViewItemText' style='background:" + color + "'>";

            var installedPlugin = installedPlugins.filter(function (ip) {
                return ip.Name == plugin.name;
            })[0];

            html += "<div>";
            if (installedPlugin) {

                html += plugin.name + " (Installed)";
            } else {
                html += plugin.name;
            }
            html += "</div>";

            html += "</div>";

            html += "</a></div>";

        }

        $('#pluginTiles', page).html(html);

        Dashboard.hideLoadingMsg();
    }
};

$(document).on('pageshow', "#pluginCatalogPage", PluginCatalogPage.onPageShow);