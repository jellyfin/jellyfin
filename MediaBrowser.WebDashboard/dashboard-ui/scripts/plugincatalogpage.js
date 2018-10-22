define(["loading", "libraryMenu", "globalize", "cardStyle", "emby-linkbutton", "emby-checkbox", "emby-select"], function(loading, libraryMenu, globalize) {
    "use strict";

    function reloadList(page) {
        loading.show(), query.IsAppStoreSafe = !0;
        var promise1 = ApiClient.getAvailablePlugins(query),
            promise2 = ApiClient.getInstalledPlugins();
        Promise.all([promise1, promise2]).then(function(responses) {
            populateList({
                catalogElement: page.querySelector("#pluginTiles"),
                noItemsElement: page.querySelector("#noPlugins"),
                availablePlugins: responses[0],
                installedPlugins: responses[1]
            })
        })
    }

    function populateList(options) {
        populateListInternal(options)
    }

    function getHeaderText(category) {
        category.replace(" ", "").replace(" ", "");
        return "Channel" === category ? category = "Channels" : "Theme" === category ? category = "Themes" : "LiveTV" === category ? category = "HeaderLiveTV" : "ScreenSaver" === category && (category = "HeaderScreenSavers"), globalize.translate(category)
    }

    function isUserInstalledPlugin(plugin) {
        return -1 === ["02528C96-F727-44D7-BE87-9EEF040758C3", "0277E613-3EC0-4360-A3DE-F8AF0AABB5E9", "4DCB591C-0FA2-4C5D-A7E5-DABE37164C8B"].indexOf(plugin.guid)
    }

    function populateListInternal(options) {
        var availablePlugins = options.availablePlugins,
            installedPlugins = options.installedPlugins,
            allPlugins = availablePlugins.filter(function(p) {
                return p.category = p.category || "General", p.categoryDisplayName = getHeaderText(p.category), (!options.categories || -1 != options.categories.indexOf(p.category)) && ((!options.targetSystem || p.targetSystem == options.targetSystem) && "UserInstalled" == p.type)
            });
        availablePlugins = allPlugins.sort(function(a, b) {
            var aName = a.category,
                bName = b.category;
            return aName > bName ? 1 : bName > aName ? -1 : (aName = a.name, bName = b.name, aName > bName ? 1 : bName > aName ? -1 : 0)
        });
        var i, length, plugin, currentCategory, html = "";
        if (!options.categories) {
            currentCategory = globalize.translate("HeaderTopPlugins"), html += '<div class="verticalSection">', html += '<h2 class="sectionTitle sectionTitle-cards">' + currentCategory + "</h2>";
            var topPlugins = allPlugins.slice(0).sort(function(a, b) {
                if (a.installs > b.installs) return -1;
                if (b.installs > a.installs) return 1;
                var aName = a.name,
                    bName = b.name;
                return aName > bName ? 1 : bName > aName ? -1 : 0
            }).filter(isUserInstalledPlugin);
            html += '<div class="itemsContainer vertical-wrap">';
            var limit = screen.availWidth >= 1920 ? 15 : 12;
            for (i = 0, length = Math.min(topPlugins.length, limit); i < length; i++) html += getPluginHtml(topPlugins[i], options, installedPlugins);
            html += "</div>", html += "</div>"
        }
        var hasOpenTag = !1;
        for (currentCategory = null, !1 === options.showCategory && (html += '<div class="itemsContainer vertical-wrap">', hasOpenTag = !0), i = 0, length = availablePlugins.length; i < length; i++) {
            plugin = availablePlugins[i];
            var category = plugin.categoryDisplayName;
            category != currentCategory && (!1 !== options.showCategory && (currentCategory && (hasOpenTag = !1, html += "</div>", html += "</div>"), html += '<div class="verticalSection">', html += '<h2 class="sectionTitle sectionTitle-cards">' + category + "</h2>", html += '<div class="itemsContainer vertical-wrap">', hasOpenTag = !0), currentCategory = category), html += getPluginHtml(plugin, options, installedPlugins)
        }
        hasOpenTag && (html += "</div>", html += "</div>"), !availablePlugins.length && options.noItemsElement && options.noItemsElement.classList.add("hide"), options.catalogElement.innerHTML = html, loading.hide()
    }

    function getPluginHtml(plugin, options, installedPlugins) {
        var html = "",
            href = plugin.externalUrl ? plugin.externalUrl : "addplugin.html?name=" + encodeURIComponent(plugin.name) + "&guid=" + plugin.guid;
        options.context && (href += "&context=" + options.context);
        var target = plugin.externalUrl ? ' target="_blank"' : "";
        html += "<div class='card backdropCard'>", html += '<div class="cardBox visualCardBox">', html += '<div class="cardScalable visualCardBox-cardScalable">', html += '<div class="cardPadder cardPadder-backdrop"></div>', html += '<a class="cardContent cardImageContainer" is="emby-linkbutton" href="' + href + '"' + target + ">", plugin.thumbImage ? (html += '<div class="cardImage coveredImage" style="background-image:url(\'' + plugin.thumbImage + "');\">", html += "</div>") : html += '<i class="cardImageIcon md-icon">&#xE2C7;</i>', plugin.isPremium && (plugin.price > 0 ? html += "<div class='premiumBanner'><img src='css/images/supporter/premiumflag.png' /></div>" : html += "<div class='premiumBanner'><img src='css/images/supporter/supporterflag.png' /></div>"), html += "</a>", html += "</div>", html += '<div class="cardFooter">', html += "<div class='cardText'>", html += plugin.name, html += "</div>";
        var installedPlugin = plugin.isApp ? null : installedPlugins.filter(function(ip) {
            return ip.Id == plugin.guid
        })[0];
        return html += "<div class='cardText cardText-secondary'>", html += installedPlugin ? globalize.translate("LabelVersionInstalled").replace("{0}", installedPlugin.Version) : "&nbsp;", html += "</div>", html += "</div>", html += "</div>", html += "</div>"
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
    var query = {
        TargetSystems: "Server",
        IsAdult: !1
    };
    return window.PluginCatalog = {
            renderCatalog: populateList
        },
        function(view, params) {
            view.querySelector("#selectSystem").addEventListener("change", function() {
                query.TargetSystems = this.value, reloadList(view)
            }), view.addEventListener("viewshow", function() {
                libraryMenu.setTabs("plugins", 1, getTabs), reloadList(this)
            })
        }
});