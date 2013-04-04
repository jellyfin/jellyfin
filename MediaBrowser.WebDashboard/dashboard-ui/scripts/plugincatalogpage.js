(function ($, document) {

    // The base query options
    var query = {
        IsPremium: false,
        TargetSystems: ""
    };

    function reloadList(page) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getAvailablePlugins(query);
        var promise2 = ApiClient.getInstalledPlugins();

        $.when(promise1, promise2).done(function (response1, response2) {
            populateList(page, response1[0], response2[0]);
        });

        Dashboard.hideLoadingMsg();
    }

    function populateList(page, availablePlugins, installedPlugins) {

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

            var color = plugin.tileColor || LibraryBrowser.getMetroColor(plugin.name);

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

    $(document).on('pageinit', "#pluginCatalogPage", function () {

        var page = this;

        $('.chkStandardFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.TargetSystems || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.TargetSystems = filters;

            reloadList(page);
        });

        $('.chkPremiumFilter', this).on('change', function () {

            if (this.checked) {
                query.IsPremium = true;
            } else {
                query.IsPremium = false;
            }

            reloadList(page);
        });

    }).on('pageshow', "#pluginCatalogPage", function () {

        reloadList(this);

        // Reset form values using the last used query

        $('.chkStandardFilter', this).each(function () {

            var filters = "," + (query.TargetSystems || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.chkPremiumFilter', this).each(function () {

            var filters = query.IsPremium || false;

            this.checked = filters;

        }).checkboxradio('refresh');
    });

})(jQuery, document);