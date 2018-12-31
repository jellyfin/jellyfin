define(['apphost', 'userSettings', 'browser', 'events', 'pluginManager', 'backdrop', 'globalize', 'require', 'appSettings'], function (appHost, userSettings, browser, events, pluginManager, backdrop, globalize, require, appSettings) {
    'use strict';

    var currentSkin;

    function getCurrentSkin() {
        return currentSkin;
    }

    function getRequirePromise(deps) {
        return new Promise(function (resolve, reject) {

            require(deps, resolve);
        });
    }

    function loadSkin(id) {

        var newSkin = pluginManager.plugins().filter(function (p) {
            return p.id === id;
        })[0];

        if (!newSkin) {
            newSkin = pluginManager.plugins().filter(function (p) {
                return p.id === 'defaultskin';
            })[0];
        }

        var unloadPromise;

        if (currentSkin) {

            if (currentSkin.id === newSkin.id) {
                // Nothing to do, it's already the active skin
                return Promise.resolve(currentSkin);
            }
            unloadPromise = unloadSkin(currentSkin);
        } else {
            unloadPromise = Promise.resolve();
        }

        return unloadPromise.then(function () {
            var deps = newSkin.getDependencies();

            console.log('Loading skin dependencies');

            return getRequirePromise(deps).then(function () {

                console.log('Skin dependencies loaded');

                var strings = newSkin.getTranslations ? newSkin.getTranslations() : [];

                return globalize.loadStrings({

                    name: newSkin.id,
                    strings: strings

                }).then(function () {

                    globalize.defaultModule(newSkin.id);
                    return loadSkinHeader(newSkin);
                });
            });
        });
    }

    function unloadSkin(skin) {

        unloadTheme();
        backdrop.clear();

        console.log('Unloading skin: ' + skin.name);

        // TODO: unload css

        return skin.unload().then(function () {
            document.dispatchEvent(new CustomEvent("skinunload", {
                detail: {
                    name: skin.name
                }
            }));
        });
    }

    function loadSkinHeader(skin) {

        return getSkinHeader(skin).then(function (headerHtml) {

            document.querySelector('.skinHeader').innerHTML = headerHtml;

            currentSkin = skin;
            skin.load();

            return skin;
        });
    }

    var cacheParam = new Date().getTime();

    function getSkinHeader(skin) {

        return new Promise(function (resolve, reject) {

            if (!skin.getHeaderTemplate) {
                resolve('');
                return;
            }

            var xhr = new XMLHttpRequest();

            var url = skin.getHeaderTemplate();
            url += url.indexOf('?') === -1 ? '?' : '&';
            url += 'v=' + cacheParam;

            xhr.open('GET', url, true);

            xhr.onload = function (e) {
                if (this.status < 400) {
                    resolve(this.response);
                } else {
                    resolve('');
                }
            };

            xhr.send();
        });
    }

    function loadUserSkin(options) {

        var skin = userSettings.get('skin', false) || 'defaultskin';

        loadSkin(skin).then(function (skin) {

            options = options || {};
            if (options.start) {
                Emby.Page.invokeShortcut(options.start);
            } else {
                Emby.Page.goHome();
            }
        });
    }

    events.on(userSettings, 'change', function (e, name) {
        if (name === 'skin' || name === 'language') {
            loadUserSkin();
        }
    });

    var themeStyleElement;
    var currentThemeId;
    function unloadTheme() {
        var elem = themeStyleElement;
        if (elem) {

            elem.parentNode.removeChild(elem);
            themeStyleElement = null;
            currentThemeId = null;
        }
    }

    function getThemes() {

        if (currentSkin.getThemes) {
            return currentSkin.getThemes();
        }

        return [];
    }

    var skinManager = {
        getCurrentSkin: getCurrentSkin,
        loadSkin: loadSkin,
        loadUserSkin: loadUserSkin,
        getThemes: getThemes
    };

    function onRegistrationSuccess() {
        appSettings.set('appthemesregistered', 'true');
    }

    function onRegistrationFailure() {
        appSettings.set('appthemesregistered', 'false');
    }

    function isRegistered() {

        getRequirePromise(['registrationServices']).then(function (registrationServices) {
            registrationServices.validateFeature('themes', {

                showDialog: false

            }).then(onRegistrationSuccess, onRegistrationFailure);
        });

        return appSettings.get('appthemesregistered') !== 'false';
    }

    function getThemeStylesheetInfo(id, requiresRegistration, isDefaultProperty) {

        var themes = skinManager.getThemes();
        var defaultTheme;
        var selectedTheme;

        for (var i = 0, length = themes.length; i < length; i++) {

            var theme = themes[i];
            if (theme[isDefaultProperty]) {
                defaultTheme = theme;
            }
            if (id === theme.id) {
                selectedTheme = theme;
            }
        }

        selectedTheme = selectedTheme || defaultTheme;

        if (selectedTheme.id !== defaultTheme.id && requiresRegistration && !isRegistered()) {
            selectedTheme = defaultTheme;
        }

        var embyWebComponentsBowerPath = 'bower_components/emby-webcomponents';

        return {
            stylesheetPath: require.toUrl(embyWebComponentsBowerPath + '/themes/' + selectedTheme.id + '/theme.css'),
            themeId: selectedTheme.id
        };
    }

    var themeResources = {};
    function modifyThemeForSeasonal(id) {

        if (!userSettings.enableSeasonalThemes()) {
            return id;
        }

        var date = new Date();
        var month = date.getMonth();
        var day = date.getDate();

        if (month === 9 && day >= 30) {
            return 'halloween';
        }

        return id;
    }

    var lastSound = 0;
    var currentSound;

    function loadThemeResources(id) {

        lastSound = 0;

        if (currentSound) {
            currentSound.stop();
            currentSound = null;
        }

        backdrop.clear();

        if (id === 'halloween') {
            themeResources = {
                themeSong: 'https://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/monsterparadefade.mp3',
                effect: 'https://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/howl.wav',
                backdrop: 'https://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/bg.jpg'
            };
            return;
        }

        themeResources = {
        };
    }

    function onThemeLoaded() {
        document.documentElement.classList.remove('preload');


        try {
            var color = getComputedStyle(document.querySelector('.skinHeader')).getPropertyValue("background-color");

            if (color) {
                appHost.setThemeColor(color);
            }
        }
        catch (err) {
            console.log('Error setting theme color: ' + err);
        }
    }

    skinManager.setTheme = function (id, context) {

        return new Promise(function (resolve, reject) {

            var requiresRegistration = true;

            if (context !== 'serverdashboard') {

                var newId = modifyThemeForSeasonal(id);
                if (newId !== id) {
                    requiresRegistration = false;
                }
                id = newId;
            }

            if (currentThemeId && currentThemeId === id) {
                resolve();
                return;
            }

            var isDefaultProperty = context === 'serverdashboard' ? 'isDefaultServerDashboard' : 'isDefault';
            var info = getThemeStylesheetInfo(id, requiresRegistration, isDefaultProperty);

            if (currentThemeId && currentThemeId === info.themeId) {
                resolve();
                return;
            }

            var linkUrl = info.stylesheetPath;

            unloadTheme();

            var link = document.createElement('link');

            link.setAttribute('rel', 'stylesheet');
            link.setAttribute('type', 'text/css');
            link.onload = function () {

                onThemeLoaded();
                resolve();
            };

            link.setAttribute('href', linkUrl);
            document.head.appendChild(link);
            themeStyleElement = link;
            currentThemeId = info.themeId;
            loadThemeResources(info.themeId);

            onViewBeforeShow({});
        });
    };

    function onViewBeforeShow(e) {

        if (e.detail && e.detail.type === 'video-osd') {
            return;
        }

        if (themeResources.backdrop) {

            backdrop.setBackdrop(themeResources.backdrop);
        }

        if (!browser.mobile && userSettings.enableThemeSongs()) {
            if (lastSound === 0) {

                if (themeResources.themeSong) {
                    playSound(themeResources.themeSong);
                }

            } else if ((new Date().getTime() - lastSound) > 30000) {
                if (themeResources.effect) {
                    playSound(themeResources.effect);
                }
            }
        }
    }

    document.addEventListener('viewshow', onViewBeforeShow);

    function playSound(path, volume) {

        lastSound = new Date().getTime();

        require(['howler'], function (howler) {

            try {
                var sound = new Howl({
                    src: [path],
                    volume: volume || 0.1
                });

                sound.play();
                currentSound = sound;
            }
            catch (err) {
                console.log('Error playing sound: ' + err);
            }
        });
    }

    return skinManager;
});