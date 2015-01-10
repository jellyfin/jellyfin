(function ($, window) {

    function deletePlugin(page, uniqueid, name) {

        var msg = Globalize.translate('UninstallPluginConfirmation').replace("{0}", name);

        Dashboard.confirm(msg, Globalize.translate('UninstallPluginHeader'), function (result) {

            if (result) {
                Dashboard.showLoadingMsg();

                ApiClient.uninstallPlugin(uniqueid).done(function () {

                    reloadList(page);
                });
            }
        });
    }

    function showNoConfigurationMessage() {
        Dashboard.alert({
            message: Globalize.translate('NoPluginConfigurationMessage')
        });
    }

    function showConnectMessage() {
        Dashboard.alert({
            message: Globalize.translate('MessagePluginConfigurationRequiresLocalAccess')
        });
    }

    function getPluginCardHtml(plugin, pluginConfigurationPages) {

        var configPage = $.grep(pluginConfigurationPages, function (pluginConfigurationPage) {
            return pluginConfigurationPage.PluginId == plugin.Id;
        })[0];

        var html = '';

        var href = configPage && !Dashboard.isConnectMode() ?
            Dashboard.getConfigurationPageUrl(configPage.Name) :
            null;

        html += "<div data-id='" + plugin.Id + "' data-name='" + plugin.Name + "' class='card backdropCard alternateHover bottomPaddedCard'>";

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        if (href) {
            html += '<a class="cardContent" href="' + href + '">';
        } else {
            html += '<div class="cardContent">';
        }

        if (plugin.ImageUrl) {
            html += '<div class="cardImage" style="background-image:url(\'' + plugin.ImageUrl + '\');">';
        } else {
            html += '<div class="cardImage" style="background-image:url(\'css/images/items/list/collection.png\');">';
        }

        html += "</div>";

        // cardContent
        if (href) {
            html += "</a>";
        } else {
            html += "</div>";
        }

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        html += '<div class="cardText" style="text-align:right; float:right;">';
        html += '<button class="btnCardMenu" type="button" data-inline="true" data-iconpos="notext" data-icon="ellipsis-v" style="margin: 2px 0 0;"></button>';
        html += "</div>";

        html += "<div class='cardText'>";
        html += plugin.Name;
        html += "</div>";

        html += "<div class='cardText'>";
        html += plugin.Version;
        html += "</div>";

        // cardFooter
        html += "</div>";

        // cardBox
        html += "</div>";

        // card
        html += "</div>";

        return html;
    }

    function populateList(page, plugins, pluginConfigurationPages) {

        plugins = plugins.sort(function (plugin1, plugin2) {

            return (plugin1.Name) > (plugin2.Name) ? 1 : -1;

        });

        var html = plugins.map(function (p) {
            return getPluginCardHtml(p, pluginConfigurationPages);

        }).join('');

        if (!plugins.length) {

            html += '<div style="padding:5px;">';
            html += '<p>' + Globalize.translate('MessageNoPluginsInstalled') + '</p>';
            html += '<p><a href="plugincatalog.html">';
            html += Globalize.translate('BrowsePluginCatalogMessage');
            html += '</a></p>';
            html += '</div>';

            $('.installedPlugins', page).html(html).trigger('create');
        } else {

            var elem = $('.installedPlugins', page).html(html).trigger('create');

            $('.btnNoConfig', elem).on('click', function () {
                showNoConfigurationMessage();
            });

            $('.btnConnectPlugin', elem).on('click', function () {
                showConnectMessage();
            });

            $('.btnCardMenu', elem).on('click', function () {
                showPluginMenu(page, this);
            });
        }

        Dashboard.hideLoadingMsg();
    }

    function showPluginMenu(page, elem) {

        var card = $(elem).parents('.card');
        var id = card.attr('data-id');
        var name = card.attr('data-name');
        var configHref = $('.cardContent', card).attr('href');

        $('.cardMenu', page).popup("close").remove();

        var html = '<div data-role="popup" class="cardMenu tapHoldMenu" data-theme="a">';

        html += '<ul data-role="listview" style="min-width: 180px;">';
        html += '<li data-role="list-divider">' + Globalize.translate('HeaderMenu') + '</li>';

        if (configHref) {
            html += '<li><a href="' + configHref + '">' + Globalize.translate('ButtonSettings') + '</a></li>';
        }

        html += '<li><a href="#" class="btnDeletePlugin">' + Globalize.translate('ButtonUninstall') + '</a></li>';

        html += '</ul>';

        html += '</div>';

        $(page).append(html);

        var flyout = $('.cardMenu', page).popup({ positionTo: elem || "window" }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();

        });

        $('.btnDeletePlugin', flyout).on('click', function () {

            $('.cardMenu', page).popup('close');

            // jqm won't show a popup while another is in the act of closing
            setTimeout(function () {
                deletePlugin(page, id, name);
            }, 300);
        });
    }

    function reloadList(page) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getInstalledPlugins();

        var promise2 = ApiClient.getJSON(ApiClient.getUrl("dashboard/configurationpages") + "?pageType=PluginConfiguration");

        $.when(promise1, promise2).done(function (response1, response2) {

            populateList(page, response1[0], response2[0]);

        });
    }

    $(document).on('pageshow', "#pluginsPage", function () {

        reloadList(this);
    });

})(jQuery, window);