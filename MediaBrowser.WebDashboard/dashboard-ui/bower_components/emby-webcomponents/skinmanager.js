define(["apphost", "userSettings", "browser", "events", "pluginManager", "backdrop", "globalize", "require", "appSettings"], function(appHost, userSettings, browser, events, pluginManager, backdrop, globalize, require, appSettings) {
    "use strict";

    function getCurrentSkin() {
        return currentSkin
    }

    function getRequirePromise(deps) {
        return new Promise(function(resolve, reject) {
            require(deps, resolve)
        })
    }

    function loadSkin(id) {
        var newSkin = pluginManager.plugins().filter(function(p) {
            return p.id === id
        })[0];
        newSkin || (newSkin = pluginManager.plugins().filter(function(p) {
            return "defaultskin" === p.id
        })[0]);
        var unloadPromise;
        if (currentSkin) {
            if (currentSkin.id === newSkin.id) return Promise.resolve(currentSkin);
            unloadPromise = unloadSkin(currentSkin)
        } else unloadPromise = Promise.resolve();
        return unloadPromise.then(function() {
            var deps = newSkin.getDependencies();
            return console.log("Loading skin dependencies"), getRequirePromise(deps).then(function() {
                console.log("Skin dependencies loaded");
                var strings = newSkin.getTranslations ? newSkin.getTranslations() : [];
                return globalize.loadStrings({
                    name: newSkin.id,
                    strings: strings
                }).then(function() {
                    return globalize.defaultModule(newSkin.id), loadSkinHeader(newSkin)
                })
            })
        })
    }

    function unloadSkin(skin) {
        return unloadTheme(), backdrop.clear(), console.log("Unloading skin: " + skin.name), skin.unload().then(function() {
            document.dispatchEvent(new CustomEvent("skinunload", {
                detail: {
                    name: skin.name
                }
            }))
        })
    }

    function loadSkinHeader(skin) {
        return getSkinHeader(skin).then(function(headerHtml) {
            return document.querySelector(".skinHeader").innerHTML = headerHtml, currentSkin = skin, skin.load(), skin
        })
    }

    function getSkinHeader(skin) {
        return new Promise(function(resolve, reject) {
            if (!skin.getHeaderTemplate) return void resolve("");
            var xhr = new XMLHttpRequest,
                url = skin.getHeaderTemplate();
            url += -1 === url.indexOf("?") ? "?" : "&", url += "v=" + cacheParam, xhr.open("GET", url, !0), xhr.onload = function(e) {
                resolve(this.status < 400 ? this.response : "")
            }, xhr.send()
        })
    }

    function loadUserSkin(options) {
        loadSkin(userSettings.get("skin", !1) || "defaultskin").then(function(skin) {
            options = options || {}, options.start ? Emby.Page.invokeShortcut(options.start) : Emby.Page.goHome()
        })
    }

    function unloadTheme() {
        var elem = themeStyleElement;
        elem && (elem.parentNode.removeChild(elem), themeStyleElement = null, currentThemeId = null)
    }

    function getThemes() {
        return currentSkin.getThemes ? currentSkin.getThemes() : []
    }

    function onRegistrationSuccess() {
        appSettings.set("appthemesregistered", "true")
    }

    function onRegistrationFailure() {
        appSettings.set("appthemesregistered", "false")
    }

    function isRegistered() {
        return getRequirePromise(["registrationServices"]).then(function(registrationServices) {
            registrationServices.validateFeature("themes", {
                showDialog: !1
            }).then(onRegistrationSuccess, onRegistrationFailure)
        }), "false" !== appSettings.get("appthemesregistered")
    }

    function getThemeStylesheetInfo(id, requiresRegistration, isDefaultProperty) {
        for (var defaultTheme, selectedTheme, themes = skinManager.getThemes(), i = 0, length = themes.length; i < length; i++) {
            var theme = themes[i];
            theme[isDefaultProperty] && (defaultTheme = theme), id === theme.id && (selectedTheme = theme)
        }
        selectedTheme = selectedTheme || defaultTheme, selectedTheme.id !== defaultTheme.id && requiresRegistration && !isRegistered() && (selectedTheme = defaultTheme);
        return {
            stylesheetPath: require.toUrl("bower_components/emby-webcomponents/themes/" + selectedTheme.id + "/theme.css"),
            themeId: selectedTheme.id
        }
    }

    function modifyThemeForSeasonal(id) {
        if (!userSettings.enableSeasonalThemes()) return id;
        var date = new Date,
            month = date.getMonth(),
            day = date.getDate();
        return 9 === month && day >= 30 ? "halloween" : id
    }

    function loadThemeResources(id) {
        if (lastSound = 0, currentSound && (currentSound.stop(), currentSound = null), backdrop.clear(), "halloween" === id) return void(themeResources = {
            themeSong: "https://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/monsterparadefade.mp3",
            effect: "https://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/howl.wav",
            backdrop: "https://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/bg.jpg"
        });
        themeResources = {}
    }

    function onThemeLoaded() {
        document.documentElement.classList.remove("preload");
        try {
            var color = getComputedStyle(document.querySelector(".skinHeader")).getPropertyValue("background-color");
            color && appHost.setThemeColor(color)
        } catch (err) {
            console.log("Error setting theme color: " + err)
        }
    }

    function onViewBeforeShow(e) {
        e.detail && "video-osd" === e.detail.type || (themeResources.backdrop && backdrop.setBackdrop(themeResources.backdrop), !browser.mobile && userSettings.enableThemeSongs() && (0 === lastSound ? themeResources.themeSong && playSound(themeResources.themeSong) : (new Date).getTime() - lastSound > 3e4 && themeResources.effect && playSound(themeResources.effect)))
    }

    function playSound(path, volume) {
        lastSound = (new Date).getTime(), require(["howler"], function(howler) {
            try {
                var sound = new Howl({
                    src: [path],
                    volume: volume || .1
                });
                sound.play(), currentSound = sound
            } catch (err) {
                console.log("Error playing sound: " + err)
            }
        })
    }
    var currentSkin, cacheParam = (new Date).getTime();
    events.on(userSettings, "change", function(e, name) {
        "skin" !== name && "language" !== name || loadUserSkin()
    });
    var themeStyleElement, currentThemeId, currentSound, skinManager = {
            getCurrentSkin: getCurrentSkin,
            loadSkin: loadSkin,
            loadUserSkin: loadUserSkin,
            getThemes: getThemes
        },
        themeResources = {},
        lastSound = 0;
    return skinManager.setTheme = function(id, context) {
        return new Promise(function(resolve, reject) {
            var requiresRegistration = !0;
            if ("serverdashboard" !== context) {
                var newId = modifyThemeForSeasonal(id);
                newId !== id && (requiresRegistration = !1), id = newId
            }
            if (currentThemeId && currentThemeId === id) return void resolve();
            var isDefaultProperty = "serverdashboard" === context ? "isDefaultServerDashboard" : "isDefault",
                info = getThemeStylesheetInfo(id, requiresRegistration, isDefaultProperty);
            if (currentThemeId && currentThemeId === info.themeId) return void resolve();
            var linkUrl = info.stylesheetPath;
            unloadTheme();
            var link = document.createElement("link");
            link.setAttribute("rel", "stylesheet"), link.setAttribute("type", "text/css"), link.onload = function() {
                onThemeLoaded(), resolve()
            }, link.setAttribute("href", linkUrl), document.head.appendChild(link), themeStyleElement = link, currentThemeId = info.themeId, loadThemeResources(info.themeId), onViewBeforeShow({})
        })
    }, document.addEventListener("viewshow", onViewBeforeShow), skinManager
});