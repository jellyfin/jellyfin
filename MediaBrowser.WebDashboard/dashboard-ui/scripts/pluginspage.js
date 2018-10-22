define(["loading", "libraryMenu", "dom", "globalize", "cardStyle", "emby-linkbutton"], function(loading, libraryMenu, dom, globalize) {
    "use strict";

    function deletePlugin(page, uniqueid, name) {
        var msg = globalize.translate("UninstallPluginConfirmation").replace("{0}", name);
        require(["confirm"], function(confirm) {
            confirm({
                title: globalize.translate("UninstallPluginHeader"),
                text: msg,
                primary: "cancel",
                confirmText: globalize.translate("UninstallPluginHeader")
            }).then(function() {
                loading.show(), ApiClient.uninstallPlugin(uniqueid).then(function() {
                    reloadList(page)
                })
            })
        })
    }

    function showNoConfigurationMessage() {
        Dashboard.alert({
            message: globalize.translate("NoPluginConfigurationMessage")
        })
    }

    function showConnectMessage() {
        Dashboard.alert({
            message: globalize.translate("MessagePluginConfigurationRequiresLocalAccess")
        })
    }

    function getPluginCardHtml(plugin, pluginConfigurationPages) {
        var configPage = pluginConfigurationPages.filter(function(pluginConfigurationPage) {
                return pluginConfigurationPage.PluginId == plugin.Id
            })[0],
            html = "",
            disallowPlugins = !Dashboard.allowPluginPages(plugin.Id),
            configPageUrl = configPage ? Dashboard.getConfigurationPageUrl(configPage.Name) : null,
            href = configPage && !disallowPlugins ? configPageUrl : null;
        return html += "<div data-id='" + plugin.Id + "' data-name='" + plugin.Name + "' class='card backdropCard'>", html += '<div class="cardBox visualCardBox">', html += '<div class="cardScalable">', html += '<div class="cardPadder cardPadder-backdrop"></div>', html += href ? '<a class="cardContent cardImageContainer" is="emby-linkbutton" href="' + href + '">' : configPageUrl ? disallowPlugins ? '<div class="cardContent connectModePluginCard cardImageContainer">' : '<div class="cardContent cardImageContainer">' : '<div class="cardContent noConfigPluginCard noHoverEffect cardImageContainer">', plugin.ImageUrl ? (html += '<div class="cardImage coveredImage" style="background-image:url(\'' + plugin.ImageUrl + "');\">", html += "</div>") : html += '<i class="cardImageIcon md-icon">&#xE2C7;</i>', html += href ? "</a>" : "</div>", html += "</div>", html += '<div class="cardFooter">', html += '<div style="text-align:right; float:right;padding-top:5px;">', html += '<button type="button" is="paper-icon-button-light" class="btnCardMenu autoSize"><i class="md-icon">more_horiz</i></button>', html += "</div>", html += "<div class='cardText'>", html += configPage ? configPage.DisplayName || plugin.Name : plugin.Name, html += "</div>", html += "<div class='cardText cardText-secondary'>", html += plugin.Version, html += "</div>", html += "</div>", html += "</div>", html += "</div>"
    }

    function renderPlugins(page, plugins, showNoPluginsMessage) {
        ApiClient.getJSON(ApiClient.getUrl("web/configurationpages") + "?pageType=PluginConfiguration").then(function(configPages) {
            populateList(page, plugins, configPages, showNoPluginsMessage)
        })
    }

    function populateList(page, plugins, pluginConfigurationPages, showNoPluginsMessage) {
        plugins = plugins.sort(function(plugin1, plugin2) {
            return plugin1.Name > plugin2.Name ? 1 : -1
        });
        var html = plugins.map(function(p) {
                return getPluginCardHtml(p, pluginConfigurationPages)
            }).join(""),
            installedPluginsElement = page.querySelector(".installedPlugins");
        installedPluginsElement.removeEventListener("click", onInstalledPluginsClick), installedPluginsElement.addEventListener("click", onInstalledPluginsClick), plugins.length ? (installedPluginsElement.classList.add("itemsContainer"), installedPluginsElement.classList.add("vertical-wrap"), installedPluginsElement.innerHTML = html) : (showNoPluginsMessage && (html += '<div style="padding:5px;">', html += "<p>" + globalize.translate("MessageNoPluginsInstalled") + "</p>", html += '<p><a is="emby-linkbutton" class="button-link" href="plugincatalog.html">', html += globalize.translate("BrowsePluginCatalogMessage"), html += "</a></p>", html += "</div>"), installedPluginsElement.innerHTML = html), loading.hide()
    }

    function showPluginMenu(page, elem) {
        var card = dom.parentWithClass(elem, "card"),
            id = card.getAttribute("data-id"),
            name = card.getAttribute("data-name"),
            configHref = card.querySelector(".cardContent").getAttribute("href"),
            menuItems = [];
        configHref && menuItems.push({
            name: globalize.translate("ButtonSettings"),
            id: "open",
            ironIcon: "mode-edit"
        }), menuItems.push({
            name: globalize.translate("ButtonUninstall"),
            id: "delete",
            ironIcon: "delete"
        }), require(["actionsheet"], function(actionsheet) {
            actionsheet.show({
                items: menuItems,
                positionTo: elem,
                callback: function(resultId) {
                    switch (resultId) {
                        case "open":
                            Dashboard.navigate(configHref);
                            break;
                        case "delete":
                            deletePlugin(page, id, name)
                    }
                }
            })
        })
    }

    function reloadList(page) {
        loading.show(), ApiClient.getInstalledPlugins().then(function(plugins) {
            renderPlugins(page, plugins, !0)
        })
    }

    function getTabs() {
        return [{
            href: "plugins.html",
            name: globalize.translate("TabMyPlugins")
        }, {
            href: "plugincatalog.html",
            name: globalize.translate("TabCatalog")
        }]
    }

    function onInstalledPluginsClick(e) {
        if (dom.parentWithClass(e.target, "noConfigPluginCard")) showNoConfigurationMessage();
        else if (dom.parentWithClass(e.target, "connectModePluginCard")) showConnectMessage();
        else {
            var btnCardMenu = dom.parentWithClass(e.target, "btnCardMenu");
            btnCardMenu && showPluginMenu(dom.parentWithClass(btnCardMenu, "page"), btnCardMenu)
        }
    }
    pageIdOn("pageshow", "pluginsPage", function() {
        libraryMenu.setTabs("plugins", 0, getTabs), reloadList(this)
    }), window.PluginsPage = {
        renderPlugins: renderPlugins
    }
});