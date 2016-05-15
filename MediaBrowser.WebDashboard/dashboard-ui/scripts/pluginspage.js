define(['jQuery'], function ($) {

    function deletePlugin(page, uniqueid, name) {

        var msg = Globalize.translate('UninstallPluginConfirmation').replace("{0}", name);

        require(['confirm'], function (confirm) {
            confirm(msg, Globalize.translate('UninstallPluginHeader')).then(function () {
                Dashboard.showLoadingMsg();

                ApiClient.uninstallPlugin(uniqueid).then(function () {

                    reloadList(page);
                });
            });
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

        var isConnectMode = Dashboard.isConnectMode();
        var configPageUrl = configPage ? Dashboard.getConfigurationPageUrl(configPage.Name) : null;

        var href = configPage && !isConnectMode ?
            configPageUrl :
            null;

        html += "<div data-id='" + plugin.Id + "' data-name='" + plugin.Name + "' class='card backdropCard bottomPaddedCard'>";

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        if (href) {
            html += '<a class="cardContent" href="' + href + '">';
        }
        else if (!configPageUrl) {
            html += '<div class="cardContent noConfigPluginCard noHoverEffect">';
        }
        else if (isConnectMode) {
            html += '<div class="cardContent connectModePluginCard">';
        }
        else {
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

        html += '<div class="cardText" style="text-align:right; float:right;padding-top:5px;">';
        html += '<button type="button" is="paper-icon-button-light" class="btnCardMenu"><iron-icon icon="' + AppInfo.moreIcon + '"></iron-icon></button>';
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

    function renderPlugins(page, plugins, showNoPluginsMessage) {

        ApiClient.getJSON(ApiClient.getUrl("web/configurationpages") + "?pageType=PluginConfiguration").then(function (configPages) {

            populateList(page, plugins, configPages, showNoPluginsMessage);

        });
    }

    function populateList(page, plugins, pluginConfigurationPages, showNoPluginsMessage) {

        plugins = plugins.sort(function (plugin1, plugin2) {

            return (plugin1.Name) > (plugin2.Name) ? 1 : -1;

        });

        var html = plugins.map(function (p) {
            return getPluginCardHtml(p, pluginConfigurationPages);

        }).join('');

        if (!plugins.length) {

            if (showNoPluginsMessage) {
                html += '<div style="padding:5px;">';

                if (AppInfo.enableAppStorePolicy) {
                    html += '<p>' + Globalize.translate('MessageNoPluginsDueToAppStore') + '</p>';
                } else {
                    html += '<p>' + Globalize.translate('MessageNoPluginsInstalled') + '</p>';

                    html += '<p><a href="plugincatalog.html">';
                    html += Globalize.translate('BrowsePluginCatalogMessage');
                    html += '</a></p>';
                }
                html += '</div>';
            }

            $('.installedPlugins', page).html(html);
        } else {

            var elem = $('.installedPlugins', page).html(html);

            $('.noConfigPluginCard', elem).on('click', function () {
                showNoConfigurationMessage();
            });

            $('.connectModePluginCard', elem).on('click', function () {
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

        var menuItems = [];

        if (configHref) {
            menuItems.push({
                name: Globalize.translate('ButtonSettings'),
                id: 'open',
                ironIcon: 'mode-edit'
            });
        }

        menuItems.push({
            name: Globalize.translate('ButtonUninstall'),
            id: 'delete',
            ironIcon: 'delete'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: elem,
                callback: function (resultId) {

                    switch (resultId) {

                        case 'open':
                            Dashboard.navigate(configHref);
                            break;
                        case 'delete':
                            deletePlugin(page, id, name);
                            break;
                        default:
                            break;
                    }
                }
            });

        });
    }

    function reloadList(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getInstalledPlugins().then(function (plugins) {

            renderPlugins(page, plugins, true);
        });
    }

    function getTabs() {
        return [
        {
            href: 'plugins.html',
            name: Globalize.translate('TabMyPlugins')
        },
         {
             href: 'plugincatalog.html',
             name: Globalize.translate('TabCatalog')
         }];
    }

    $(document).on('pageshow', "#pluginsPage", function () {

        LibraryMenu.setTabs('plugins', 0, getTabs);
        reloadList(this);
    });

    window.PluginsPage = {
        renderPlugins: renderPlugins
    };

});