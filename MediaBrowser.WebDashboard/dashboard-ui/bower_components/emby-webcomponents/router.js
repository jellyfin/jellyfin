define(["loading", "globalize", "events", "viewManager", "layoutManager", "skinManager", "pluginManager", "backdrop", "browser", "pageJs", "appSettings", "apphost", "connectionManager"], function(loading, globalize, events, viewManager, layoutManager, skinManager, pluginManager, backdrop, browser, page, appSettings, appHost, connectionManager) {
    "use strict";

    function beginConnectionWizard() {
        backdrop.clear(), loading.show(), connectionManager.connect({
            enableAutoLogin: appSettings.enableAutoLogin()
        }).then(function(result) {
            handleConnectionResult(result, loading)
        })
    }

    function handleConnectionResult(result, loading) {
        switch (result.State) {
            case "SignedIn":
                loading.hide(), skinManager.loadUserSkin();
                break;
            case "ServerSignIn":
                result.ApiClient.getPublicUsers().then(function(users) {
                    users.length ? appRouter.showLocalLogin(result.Servers[0].Id) : appRouter.showLocalLogin(result.Servers[0].Id, !0)
                });
                break;
            case "ServerSelection":
                appRouter.showSelectServer();
                break;
            case "ConnectSignIn":
                appRouter.showWelcome();
                break;
            case "ServerUpdateNeeded":
                require(["alert"], function(alert) {
                    alert({
                        text: globalize.translate("sharedcomponents#ServerUpdateNeeded", "https://github.com/jellyfin/jellyfin"),
                        html: globalize.translate("sharedcomponents#ServerUpdateNeeded", '<a href="https://github.com/jellyfin/jellyfin">https://github.com/jellyfin/jellyfin</a>')
                    }).then(function() {
                        appRouter.showSelectServer()
                    })
                })
        }
    }

    function loadContentUrl(ctx, next, route, request) {
        var url;
        url = route.contentPath && "function" == typeof route.contentPath ? route.contentPath(ctx.querystring) : route.contentPath || route.path, -1 === url.indexOf("://") && (0 !== url.indexOf("/") && (url = "/" + url), url = baseUrl() + url), ctx.querystring && route.enableContentQueryString && (url += "?" + ctx.querystring), require(["text!" + url], function(html) {
            loadContent(ctx, route, html, request)
        })
    }

    function handleRoute(ctx, next, route) {
        authenticate(ctx, route, function() {
            initRoute(ctx, next, route)
        })
    }

    function initRoute(ctx, next, route) {
        var onInitComplete = function(controllerFactory) {
            sendRouteToViewManager(ctx, next, route, controllerFactory)
        };
        require(route.dependencies || [], function() {
            route.controller ? require([route.controller], onInitComplete) : onInitComplete()
        })
    }

    function cancelCurrentLoadRequest() {
        var currentRequest = currentViewLoadRequest;
        currentRequest && (currentRequest.cancel = !0)
    }

    function sendRouteToViewManager(ctx, next, route, controllerFactory) {
        if (isDummyBackToHome && "home" === route.type) return void(isDummyBackToHome = !1);
        cancelCurrentLoadRequest();
        var isBackNav = ctx.isBack,
            currentRequest = {
                url: baseUrl() + ctx.path,
                transition: route.transition,
                isBack: isBackNav,
                state: ctx.state,
                type: route.type,
                fullscreen: route.fullscreen,
                controllerFactory: controllerFactory,
                options: {
                    supportsThemeMedia: route.supportsThemeMedia || !1,
                    enableMediaControl: !1 !== route.enableMediaControl
                },
                autoFocus: route.autoFocus
            };
        currentViewLoadRequest = currentRequest;
        var onNewViewNeeded = function() {
            "string" == typeof route.path ? loadContentUrl(ctx, next, route, currentRequest) : next()
        };
        if (!isBackNav) return void onNewViewNeeded();
        viewManager.tryRestoreView(currentRequest, function() {
            currentRouteInfo = {
                route: route,
                path: ctx.path
            }
        }).catch(function(result) {
            result && result.cancelled || onNewViewNeeded()
        })
    }

    function onForcedLogoutMessageTimeout() {
        var msg = forcedLogoutMsg;
        forcedLogoutMsg = null, msg && require(["alert"], function(alert) {
            alert(msg)
        })
    }

    function showForcedLogoutMessage(msg) {
        forcedLogoutMsg = msg, msgTimeout && clearTimeout(msgTimeout), msgTimeout = setTimeout(onForcedLogoutMessageTimeout, 100)
    }

    function onRequestFail(e, data) {
        var apiClient = this;
        if (401 === data.status && "ParentalControl" === data.errorCode) {
            !currentRouteInfo || (currentRouteInfo.route.anonymous || currentRouteInfo.route.startup) || (showForcedLogoutMessage(globalize.translate("sharedcomponents#AccessRestrictedTryAgainLater")), connectionManager.isLoggedIntoConnect() ? appRouter.showConnectLogin() : appRouter.showLocalLogin(apiClient.serverId()))
        }
    }

    function onBeforeExit(e) {
        browser.web0s && page.restorePreviousState()
    }

    function normalizeImageOptions(options) {
        var setQuality, scaleFactor = browser.tv ? .8 : 1;
        if (options.maxWidth && (options.maxWidth = Math.round(options.maxWidth * scaleFactor), setQuality = !0), options.width && (options.width = Math.round(options.width * scaleFactor), setQuality = !0), options.maxHeight && (options.maxHeight = Math.round(options.maxHeight * scaleFactor), setQuality = !0), options.height && (options.height = Math.round(options.height * scaleFactor), setQuality = !0), setQuality) {
            var quality = 100,
                type = options.type || "Primary";
            quality = browser.tv || browser.slow ? browser.chrome ? "Primary" === type ? 40 : 50 : "Backdrop" === type ? 60 : 50 : "Backdrop" === type ? 70 : 90, options.quality = quality
        }
    }

    function getMaxBandwidth() {
        if (navigator.connection) {
            var max = navigator.connection.downlinkMax;
            if (max && max > 0 && max < Number.POSITIVE_INFINITY) return max /= 8, max *= 1e6, max *= .7, max = parseInt(max)
        }
        return null
    }

    function getMaxBandwidthIOS() {
        return 8e5
    }

    function onApiClientCreated(e, newApiClient) {
        newApiClient.normalizeImageOptions = normalizeImageOptions, browser.iOS ? newApiClient.getMaxBandwidth = getMaxBandwidthIOS : newApiClient.getMaxBandwidth = getMaxBandwidth, events.off(newApiClient, "requestfail", onRequestFail), events.on(newApiClient, "requestfail", onRequestFail)
    }

    function initApiClient(apiClient) {
        onApiClientCreated({}, apiClient)
    }

    function initApiClients() {
        connectionManager.getApiClients().forEach(initApiClient), events.on(connectionManager, "apiclientcreated", onApiClientCreated)
    }

    function onAppResume() {
        var apiClient = connectionManager.currentApiClient();
        apiClient && apiClient.ensureWebSocket()
    }

    function start(options) {
        loading.show(), initApiClients(), events.on(appHost, "beforeexit", onBeforeExit), events.on(appHost, "resume", onAppResume), connectionManager.connect({
            enableAutoLogin: appSettings.enableAutoLogin()
        }).then(function(result) {
            firstConnectionResult = result, loading.hide(), options = options || {}, page({
                click: !1 !== options.click,
                hashbang: !1 !== options.hashbang,
                enableHistory: enableHistory()
            })
        })
    }

    function enableHistory() {
        return !browser.xboxOne && !browser.orsay
    }

    function enableNativeHistory() {
        return page.enableNativeHistory()
    }

    function authenticate(ctx, route, callback) {
        var firstResult = firstConnectionResult;
        if (firstResult && (firstConnectionResult = null, "SignedIn" !== firstResult.State && !route.anonymous)) return void handleConnectionResult(firstResult, loading);
        var apiClient = connectionManager.currentApiClient(),
            pathname = ctx.pathname.toLowerCase();
        console.log("appRouter - processing path request " + pathname);
        var isCurrentRouteStartup = !currentRouteInfo || currentRouteInfo.route.startup,
            shouldExitApp = ctx.isBack && route.isDefaultRoute && isCurrentRouteStartup;
        if (!(shouldExitApp || apiClient && apiClient.isLoggedIn() || route.anonymous)) return console.log("appRouter - route does not allow anonymous access, redirecting to login"), void beginConnectionWizard();
        if (shouldExitApp) {
            if (appHost.supports("exit")) return void appHost.exit()
        } else {
            if (apiClient && apiClient.isLoggedIn()) {
                if (console.log("appRouter - user is authenticated"), ctx.isBack && (route.isDefaultRoute || route.startup) && !isCurrentRouteStartup) return void handleBackToDefault();
                if (route.isDefaultRoute) return console.log("appRouter - loading skin home page"), void loadUserSkinWithOptions(ctx);
                if (route.roles) return void validateRoles(apiClient, route.roles).then(function() {
                    callback()
                }, beginConnectionWizard)
            }
            console.log("appRouter - proceeding to " + pathname), callback()
        }
    }

    function loadUserSkinWithOptions(ctx) {
        require(["queryString"], function(queryString) {
            var params = queryString.parse(ctx.querystring);
            skinManager.loadUserSkin({
                start: params.start
            })
        })
    }

    function validateRoles(apiClient, roles) {
        return Promise.all(roles.split(",").map(function(role) {
            return validateRole(apiClient, role)
        }))
    }

    function validateRole(apiClient, role) {
        return "admin" === role ? apiClient.getCurrentUser().then(function(user) {
            return user.Policy.IsAdministrator ? Promise.resolve() : Promise.reject()
        }) : Promise.resolve()
    }

    function handleBackToDefault() {
        if (!appHost.supports("exitmenu") && appHost.supports("exit")) return void appHost.exit();
        isDummyBackToHome = !0, skinManager.loadUserSkin(), isHandlingBackToDefault || skinManager.getCurrentSkin().showBackMenu().then(function() {
            isHandlingBackToDefault = !1
        })
    }

    function loadContent(ctx, route, html, request) {
        html = globalize.translateDocument(html, route.dictionary), request.view = html, viewManager.loadView(request), currentRouteInfo = {
            route: route,
            path: ctx.path
        }, ctx.handled = !0
    }

    function getRequestFile() {
        var path = self.location.pathname || "",
            index = path.lastIndexOf("/");
        return path = -1 !== index ? path.substring(index) : "/" + path, path && "/" !== path || (path = "/index.html"), path
    }

    function endsWith(str, srch) {
        return str.lastIndexOf(srch) === srch.length - 1
    }

    function baseUrl() {
        return baseRoute
    }

    function getHandler(route) {
        return function(ctx, next) {
            handleRoute(ctx, next, route)
        }
    }

    function getWindowLocationSearch(win) {
        var currentPath = currentRouteInfo ? currentRouteInfo.path || "" : "",
            index = currentPath.indexOf("?"),
            search = "";
        return -1 !== index && (search = currentPath.substring(index)), search || ""
    }

    function param(name, url) {
        name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
        var regexS = "[\\?&]" + name + "=([^&#]*)",
            regex = new RegExp(regexS, "i"),
            results = regex.exec(url || getWindowLocationSearch());
        return null == results ? "" : decodeURIComponent(results[1].replace(/\+/g, " "))
    }

    function back() {
        page.back()
    }

    function canGoBack() {
        var curr = current();
        return !!curr && ("home" !== curr.type && page.canGoBack())
    }

    function show(path, options) {
        0 !== path.indexOf("/") && -1 === path.indexOf("://") && (path = "/" + path);
        var baseRoute = baseUrl();
        return path = path.replace(baseRoute, ""), currentRouteInfo && currentRouteInfo.path === path && "home" !== currentRouteInfo.route.type ? (loading.hide(), Promise.resolve()) : new Promise(function(resolve, reject) {
            resolveOnNextShow = resolve, page.show(path, options)
        })
    }

    function current() {
        return currentRouteInfo ? currentRouteInfo.route : null
    }

    function goHome() {
        var skin = skinManager.getCurrentSkin();
        if (skin.getHomeRoute) {
            var homePath = skin.getHomeRoute();
            return show(pluginManager.mapRoute(skin, homePath))
        }
        var homeRoute = skin.getRoutes().filter(function(r) {
            return "home" === r.type
        })[0];
        return show(pluginManager.mapRoute(skin, homeRoute))
    }

    function getRouteUrl(item, options) {
        return "downloads" === item ? "offline/offline.html" : "managedownloads" === item ? "offline/managedownloads.html" : "settings" === item ? "settings/settings.html" : skinManager.getCurrentSkin().getRouteUrl(item, options)
    }

    function showItem(item, serverId, options) {
        if ("string" == typeof item) {
            var apiClient = serverId ? connectionManager.getApiClient(serverId) : connectionManager.currentApiClient();
            apiClient.getItem(apiClient.getCurrentUserId(), item).then(function(item) {
                appRouter.showItem(item, options)
            })
        } else {
            2 === arguments.length && (options = arguments[1]);
            var url = appRouter.getRouteUrl(item, options);
            appRouter.show(url, {
                item: item
            })
        }
    }

    function setTitle(title) {
        skinManager.getCurrentSkin().setTitle(title)
    }

    function showVideoOsd() {
        var skin = skinManager.getCurrentSkin(),
            homeRoute = skin.getRoutes().filter(function(r) {
                return "video-osd" === r.type
            })[0];
        return show(pluginManager.mapRoute(skin, homeRoute))
    }

    function addRoute(path, newRoute) {
        page(path, getHandler(newRoute)), allRoutes.push(newRoute)
    }

    function getRoutes() {
        return allRoutes
    }

    function setTransparency(level) {
        backdropContainer || (backdropContainer = document.querySelector(".backdropContainer")), backgroundContainer || (backgroundContainer = document.querySelector(".backgroundContainer")), "full" === level || 2 === level ? (backdrop.clear(!0), document.documentElement.classList.add("transparentDocument"), backgroundContainer.classList.add("backgroundContainer-transparent"), backdropContainer.classList.add("hide")) : "backdrop" === level || 1 === level ? (backdrop.externalBackdrop(!0), document.documentElement.classList.add("transparentDocument"), backgroundContainer.classList.add("backgroundContainer-transparent"), backdropContainer.classList.add("hide")) : (backdrop.externalBackdrop(!1), document.documentElement.classList.remove("transparentDocument"), backgroundContainer.classList.remove("backgroundContainer-transparent"), backdropContainer.classList.remove("hide"))
    }

    function pushState(state, title, url) {
        state.navigate = !1, page.pushState(state, title, url)
    }

    function invokeShortcut(id) {
        0 === id.indexOf("library-") ? (id = id.replace("library-", ""), id = id.split("_"), appRouter.showItem(id[0], id[1])) : 0 === id.indexOf("item-") ? (id = id.replace("item-", ""), id = id.split("_"), appRouter.showItem(id[0], id[1])) : (id = id.split("_"), appRouter.show(appRouter.getRouteUrl(id[0], {
            serverId: id[1]
        })))
    }
    var currentViewLoadRequest, msgTimeout, forcedLogoutMsg, firstConnectionResult, isHandlingBackToDefault, isDummyBackToHome, appRouter = {
            showLocalLogin: function(serverId, manualLogin) {
                show("/startup/" + (manualLogin ? "manuallogin" : "login") + ".html?serverid=" + serverId)
            },
            showSelectServer: function() {
                show("/startup/selectserver.html")
            },
            showWelcome: function() {
                show("/startup/welcome.html")
            },
            showConnectLogin: function() {
                show("/startup/connectlogin.html")
            },
            showSettings: function() {
                show("/settings/settings.html")
            },
            showSearch: function() {
                skinManager.getCurrentSkin().search()
            },
            showGenre: function(options) {
                skinManager.getCurrentSkin().showGenre(options)
            },
            showGuide: function() {
                skinManager.getCurrentSkin().showGuide({
                    serverId: connectionManager.currentApiClient().serverId()
                })
            },
            showLiveTV: function() {
                skinManager.getCurrentSkin().showLiveTV({
                    serverId: connectionManager.currentApiClient().serverId()
                })
            },
            showRecordedTV: function() {
                skinManager.getCurrentSkin().showRecordedTV()
            },
            showFavorites: function() {
                skinManager.getCurrentSkin().showFavorites()
            },
            showNowPlaying: function() {
                skinManager.getCurrentSkin().showNowPlaying()
            }
        },
        baseRoute = self.location.href.split("?")[0].replace(getRequestFile(), "");
    baseRoute = baseRoute.split("#")[0], endsWith(baseRoute, "/") && !endsWith(baseRoute, "://") && (baseRoute = baseRoute.substring(0, baseRoute.length - 1));
    var resolveOnNextShow;
    document.addEventListener("viewshow", function() {
        var resolve = resolveOnNextShow;
        resolve && (resolveOnNextShow = null, resolve())
    });
    var currentRouteInfo, backdropContainer, backgroundContainer, allRoutes = [];
    return function() {
        var baseRoute = self.location.pathname.replace(getRequestFile(), "");
        baseRoute.lastIndexOf("/") === baseRoute.length - 1 && (baseRoute = baseRoute.substring(0, baseRoute.length - 1)), console.log("Setting page base to " + baseRoute), page.base(baseRoute)
    }(), appRouter.addRoute = addRoute, appRouter.param = param, appRouter.back = back, appRouter.show = show, appRouter.start = start, appRouter.baseUrl = baseUrl, appRouter.canGoBack = canGoBack, appRouter.current = current, appRouter.beginConnectionWizard = beginConnectionWizard, appRouter.goHome = goHome, appRouter.showItem = showItem, appRouter.setTitle = setTitle, appRouter.setTransparency = setTransparency, appRouter.getRoutes = getRoutes, appRouter.getRouteUrl = getRouteUrl, appRouter.pushState = pushState, appRouter.enableNativeHistory = enableNativeHistory, appRouter.showVideoOsd = showVideoOsd, appRouter.handleAnchorClick = page.handleAnchorClick, appRouter.TransparencyLevel = {
        None: 0,
        Backdrop: 1,
        Full: 2
    }, appRouter.invokeShortcut = invokeShortcut, appRouter
});
