(function ($, document) {

    // The base query options
    var query = {
        TargetSystems: 'Server',
        IsAdult: false
    };

    function reloadList(page) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getAvailablePlugins(query);

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

                if (currentCategory) {
                    html += '<br/>';
                    html += '<br/>';
                    html += '<br/>';
                }

                html += '<div class="ui-bar-a" style="padding: 0 1em;"><h3>' + category + '</h3></div>';

                currentCategory = category;
            }

            var href = plugin.externalUrl ? plugin.externalUrl : "addplugin.html?name=" + encodeURIComponent(plugin.name) + "&guid=" + plugin.guid;
            var target = plugin.externalUrl ? ' target="_blank"' : '';

            html += "<div class='card backdropCard alternateHover'>";

            html += '<div class="cardBox visualCardBox">';
            html += '<div class="cardScalable">';

            html += '<div class="cardPadder"></div>';

            html += '<a class="cardContent" href="' + href + '"' + target + '>';
            if (plugin.thumbImage) {
                html += '<div class="cardImage" style="background-image:url(\'' + plugin.thumbImage + '\');">';
            } else {
                html += '<div class="cardImage" style="background-image:url(\'css/images/items/list/collection.png\');">';
            }

            if (plugin.isPremium) {
                if (plugin.price > 0) {
                    html += "<div class='premiumBanner'><img src='css/images/supporter/premiumflag.png' /></div>";
                } else {
                    html += "<div class='premiumBanner'><img src='css/images/supporter/supporterflag.png' /></div>";
                }
            }
            html += "</div>";

            // cardContent
            html += "</a>";

            // cardScalable
            html += "</div>";

            html += '<div class="cardFooter">';

            html += "<div class='cardText'>";
            html += plugin.name;
            html += "</div>";

            if (!plugin.isExternal) {
                html += "<div class='cardText packageReviewText'>";
                html += plugin.price > 0 ? "$" + plugin.price.toFixed(2) : Globalize.translate('LabelFree');
                html += RatingHelpers.getStoreRatingHtml(plugin.avgRating, plugin.id, plugin.name);

                html += "<span class='storeReviewCount'>";
                html += " " + Globalize.translate('LabelNumberReviews').replace("{0}", plugin.totalRatings);
                html += "</span>";

                html += "</div>";
            }

            var installedPlugin = plugin.isApp ? null : installedPlugins.filter(function (ip) {
                return ip.Name == plugin.name;
            })[0];

            html += "<div class='cardText'>";

            if (installedPlugin) {
                html += Globalize.translate('LabelVersionInstalled').replace("{0}", installedPlugin.Version);
            } else {
                html += '&nbsp;';
            }
            html += "</div>";

            // cardFooter
            html += "</div>";

            // cardBox
            html += "</div>";

            // card
            html += "</div>";

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

        $('#chkAdult', page).on('change', function () {

            query.IsAdult = this.checked ? null : false;
            reloadList(page);
        });

    }).on('pageshow', "#pluginCatalogPage", function () {

        var page = this;

        $(".radioPackageTypes", page).each(function () {

            this.checked = this.value == query.TargetSystems;

        }).checkboxradio('refresh');

        // Reset form values using the last used query
        $('.chkPremiumFilter', page).each(function () {

            var filters = query.IsPremium || false;

            this.checked = filters;

        }).checkboxradio('refresh');
    });

})(jQuery, document);