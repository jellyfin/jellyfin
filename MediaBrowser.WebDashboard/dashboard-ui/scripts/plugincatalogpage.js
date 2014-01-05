(function ($, document) {

    // The base query options
    var query = {
        TargetSystems: 'Server'
    };

    function getApps() {

        var apps = [];

        apps.push({
            type: "UserInstalled",
            name: "MBKinect",
            category: "Voice Control",
            isApp: true,
            tileColor: "#050810",
            thumbImage: "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/images/mbkinect/thumb.png",
            externalUrl: "http://mediabrowser3.com/community/index.php?/topic/850-media-browser-kinect-sensor-plug-in-support/",
            isPremium: false
        });

        return apps;
    }

    function getAppsPromise() {

        var deferred = $.Deferred();

        deferred.resolveWith(null, [[getApps()]]);

        return deferred.promise();
    }

    function reloadList(page) {

        Dashboard.showLoadingMsg();

        var promise1 = query.TargetSystems == "Apps" ? getAppsPromise() : ApiClient.getAvailablePlugins(query);

        var promise2 = ApiClient.getInstalledPlugins();

        $.when(promise1, promise2).done(function (response1, response2) {
            populateList(page, response1[0], response2[0]);
        });
    }

    function populateList(page, availablePlugins, installedPlugins) {

        Dashboard.showLoadingMsg();

        availablePlugins = availablePlugins.filter(function (p) {
            return p.type == "UserInstalled";
        }).sort(function (a, b) {

            var aName = (a.category || "General") + " " + a.name;
            var bame = (b.category || "General") + " " + b.name;

            return aName > bame ? 1 : -1;
        });

        var pluginhtml = '';

        var currentCategory;

        for (var i = 0, length = availablePlugins.length; i < length; i++) {
            var html = '';
            var plugin = availablePlugins[i];

            var category = plugin.category || "General";

            if (category != currentCategory) {
                html += '<h2 class="listHeader">' + category + '</h2>';
                currentCategory = category;
            }

            var href = plugin.externalUrl ? plugin.externalUrl : "addplugin.html?name=" + encodeURIComponent(plugin.name) + "&guid=" + plugin.guid;
            var target = plugin.externalUrl ? ' target="_blank"' : '';

            html += "<a class='storeItem backdropPosterItem posterItem transparentPosterItem borderlessPosterItem' href='" + href + "' " + target + ">";

            if (plugin.thumbImage) {
                html += '<div class="posterItemImage" style="background-image:url(\'' + plugin.thumbImage + '\');background-size:cover;">';
            } else {
                html += '<div class="posterItemImage defaultPosterItemImage" style="background-image:url(\'css/images/items/list/collection.png\');">';
            }

            if (plugin.isPremium) {
                if (plugin.price > 0) {
                    html += "<div class='premiumBanner'><img src='css/images/supporter/premiumflag.png' /></div>";
                } else {
                    html += "<div class='premiumBanner'><img src='css/images/supporter/supporterflag.png' /></div>";
                }
            }
            html += "</div>";

            html += "<div class='posterItemText' style='color:#000;font-weight:400;font-size:16px;'>";

            var installedPlugin = plugin.isApp ? null : installedPlugins.filter(function (ip) {
                return ip.Name == plugin.name;
            })[0];

            if (installedPlugin) {
                html += plugin.name;
            } else {
                html += plugin.name;
            }
            
            html += "</div>";

            html += "<div class='posterItemText packageReviewText' style='color:#000;font-weight:400;font-size:15px;'>";
            html += plugin.price > 0 ? "$" + plugin.price.toFixed(2) : "Free";
            html += Dashboard.getStoreRatingHtml(plugin.avgRating, plugin.id, plugin.name);

            html += "<span class='storeReviewCount'>";
            html += " " + plugin.totalRatings + " Reviews";
            html += "</span>";

            html += "</div>";
            html += "</a>";

            pluginhtml += html;

        }

        if (!availablePlugins.length) {
            $("#noPlugins", page).hide();
        }

        $('#pluginTiles', page).html(pluginhtml);

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageinit', "#pluginCatalogPage", function () {

        var page = this;

        reloadList(page);

        $('.chkPremiumFilter', page).on('change', function () {

            if (this.checked) {
                query.IsPremium = false;
            } else {
                query.IsPremium = null;
            }
            reloadList(page);
        });

        $('.radioPackageTypes', page).on('change', function () {

            var val = $('.radioPackageTypes:checked', page).val();

            query.TargetSystems = val;
            reloadList(page);
        });

    }).on('pageshow', "#pluginCatalogPage", function () {

        var page = this;

        $(".radioPackageTypes", page).each(function() {

            this.checked = this.value == query.TargetSystems;

        }).checkboxradio('refresh');

        // Reset form values using the last used query
        $('.chkPremiumFilter', page).each(function () {

            var filters = query.IsPremium || false;

            this.checked = filters;

        }).checkboxradio('refresh');
    });

})(jQuery, document);