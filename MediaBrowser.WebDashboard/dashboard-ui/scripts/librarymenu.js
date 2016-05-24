define(['imageLoader', 'layoutManager', 'jQuery'], function (imageLoader, layoutManager, $) {

    var mainDrawerPanel = document.querySelector('.mainDrawerPanel');

    function renderHeader() {

        var html = '';

        html += '<div class="primaryIcons">';
        var backIcon = browserInfo.safari ? 'chevron-left' : 'arrow-back';

        html += '<button type="button" is="paper-icon-button-light" class="headerButton headerButtonLeft headerBackButton hide"><iron-icon icon="' + backIcon + '"></iron-icon></button>';

        html += '<button type="button" is="paper-icon-button-light" class="headerButton mainDrawerButton barsMenuButton headerButtonLeft"><iron-icon icon="menu"></iron-icon></button>';
        html += '<button type="button" is="paper-icon-button-light" class="headerButton headerAppsButton barsMenuButton headerButtonLeft"><iron-icon icon="menu"></iron-icon></button>';

        html += '<div class="libraryMenuButtonText headerButton">' + Globalize.translate('ButtonHome') + '</div>';

        html += '<div class="viewMenuSecondary">';

        html += '<span class="headerSelectedPlayer"></span>';
        html += '<button is="paper-icon-button-light" class="btnCast headerButton headerButtonRight hide"><iron-icon icon="cast"></iron-icon></button>';

        if (AppInfo.enableSearchInTopMenu) {
            html += '<button type="button" is="paper-icon-button-light" class=headerButton headerButtonRight headerSearchButton hide" onclick="Search.showSearchPanel();"><iron-icon icon="search"></iron-icon></button>';
            html += '<div class="viewMenuSearch hide">';
            html += '<form class="viewMenuSearchForm">';
            html += '<input type="text" data-role="none" data-type="search" class="headerSearchInput" autocomplete="off" spellcheck="off" />';
            html += '<button type="button" is="paper-icon-button-light" class="btnCloseSearch"><iron-icon icon="close"></iron-icon></button>';
            html += '</form>';
            html += '</div>';
        }

        html += '<button is="paper-icon-button-light" class="headerButton headerButtonRight headerVoiceButton hide"><iron-icon icon="mic"></iron-icon></button>';

        html += '<button is="paper-icon-button-light" class="headerButton headerButtonRight btnNotifications clearButton"><div class="btnNotificationsInner">0</div></button>';

        html += '<button is="paper-icon-button-light" class="headerButton headerButtonRight headerUserButton"><iron-icon icon="person"></iron-icon></button>';

        if (!browserInfo.mobile && !Dashboard.isConnectMode()) {
            html += '<button is="paper-icon-button-light" class="headerButton headerButtonRight dashboardEntryHeaderButton" onclick="return LibraryMenu.onSettingsClicked(event);"><iron-icon icon="settings"></iron-icon></button>';
        }

        html += '</div>';
        html += '</div>';

        html += '<div class="viewMenuBarTabs hiddenScrollX">';
        html += '</div>';

        var viewMenuBar = document.createElement('div');
        viewMenuBar.classList.add('viewMenuBar');
        viewMenuBar.innerHTML = html;

        document.querySelector('.skinHeader').appendChild(viewMenuBar);

        imageLoader.lazyChildren(document.querySelector('.viewMenuBar'));

        document.dispatchEvent(new CustomEvent("headercreated", {}));
        bindMenuEvents();
    }

    function onBackClick() {

        if (Dashboard.exitOnBack()) {
            Dashboard.exit();
        }
        else {
            history.back();
        }
    }

    function updateUserInHeader(user) {

        var header = document.querySelector('.viewMenuBar');
        if (!header) {
            return;
        }

        var headerUserButton = header.querySelector('.headerUserButton');
        var hasImage;

        if (user && user.name) {
            if (user.imageUrl) {

                var userButtonHeight = 26;

                var url = user.imageUrl;

                if (user.supportsImageParams) {
                    url += "&height=" + Math.round((userButtonHeight * Math.max(window.devicePixelRatio || 1, 2)));
                }

                if (headerUserButton) {
                    updateHeaderUserButton(headerUserButton, url, null);
                    hasImage = true;
                }
            }
        }

        if (headerUserButton && !hasImage) {

            updateHeaderUserButton(headerUserButton, null, 'person');
        }
        if (user) {
            updateLocalUser(user.localUser);
        }

        requiresUserRefresh = false;
    }

    function updateHeaderUserButton(headerUserButton, src, icon) {

        if (src) {
            headerUserButton.classList.add('headerUserButtonRound');
            headerUserButton.innerHTML = '<img src="' + src + '" />';
        } else if (icon) {
            headerUserButton.classList.remove('headerUserButtonRound');
            headerUserButton.innerHTML = '<iron-icon icon="' + icon + '"></iron-icon>';
        } else {
            headerUserButton.classList.remove('headerUserButtonRound');
        }
    }

    function updateLocalUser(user) {

        var header = document.querySelector('.viewMenuBar');

        var headerSearchButton = header.querySelector('.headerSearchButton');
        var btnCast = header.querySelector('.btnCast');
        var dashboardEntryHeaderButton = header.querySelector('.dashboardEntryHeaderButton');

        if (user) {
            btnCast.classList.remove('hide');

            if (headerSearchButton) {
                headerSearchButton.classList.remove('hide');
            }

            if (dashboardEntryHeaderButton) {
                if (user.Policy.IsAdministrator) {
                    dashboardEntryHeaderButton.classList.remove('hide');
                } else {
                    dashboardEntryHeaderButton.classList.add('hide');
                }
            }

            require(['voice/voice'], function (voice) {

                if (voice.isSupported()) {
                    header.querySelector('.headerVoiceButton').classList.remove('hide');
                } else {
                    header.querySelector('.headerVoiceButton').classList.add('hide');
                }

            });

        } else {
            btnCast.classList.add('hide');
            header.querySelector('.headerVoiceButton').classList.add('hide');
            if (headerSearchButton) {
                headerSearchButton.classList.add('hide');
            }

            if (dashboardEntryHeaderButton) {
                dashboardEntryHeaderButton.classList.add('hide');
            }
        }
    }

    function showVoice() {
        require(['voice/voice'], function (voice) {
            voice.startListening();
        });
    }

    function onHeaderUserButtonClick(e) {
        Dashboard.showUserFlyout(e.target);
    }

    function onHeaderAppsButtonClick() {

        require(['dialogHelper'], function (dialogHelper) {

            var dlg = dialogHelper.createDialog({
                removeOnClose: true,
                modal: false,
                autoFocus: false,
                entryAnimationDuration: 160,
                exitAnimationDuration: 160,
                enableHistory: false
            });

            dlg.classList.add('ui-body-a');
            dlg.classList.add('background-theme-a');
            dlg.classList.add('adminAppsMenu');

            var html = '';

            html += '<div class="adminAppsMenuRow">';

            html += '<a class="adminAppsButton" href="home.html">';
            html += '<button is="paper-icon-button-light"><iron-icon icon="home"></iron-icon></button>';
            html += '<div>' + Globalize.translate('ButtonHome') + '</div>';
            html += '</a>';

            html += '</div>';

            html += '<div class="adminAppsMenuRow">';

            html += '<a class="adminAppsButton" href="edititemmetadata.html">';
            html += '<button is="paper-icon-button-light"><iron-icon icon="mode-edit"></iron-icon></button>';
            html += '<div>' + Globalize.translate('ButtonMetadataManager') + '</div>';
            html += '</a>';
            html += '<a class="adminAppsButton" href="reports.html">';
            html += '<button is="paper-icon-button-light"><iron-icon icon="insert-chart"></iron-icon></button>';
            html += '<div>' + Globalize.translate('ButtonReports') + '</div>';
            html += '</a>';

            html += '</div>';

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            dlg.addEventListener('click', function (e) {
                var link = parentWithTag(e.target, 'A');
                if (link) {
                    dialogHelper.close(dlg);
                }
            });
            dialogHelper.open(dlg);

        });
    }

    function bindMenuEvents() {

        var mainDrawerButton = document.querySelector('.mainDrawerButton');

        if (mainDrawerButton) {
            mainDrawerButton.addEventListener('click', toggleMainDrawer);
        }

        var headerBackButton = document.querySelector('.headerBackButton');
        if (headerBackButton) {
            headerBackButton.addEventListener('click', onBackClick);
        }

        var headerVoiceButton = document.querySelector('.headerVoiceButton');
        if (headerVoiceButton) {
            headerVoiceButton.addEventListener('click', showVoice);
        }

        var headerUserButton = document.querySelector('.headerUserButton');
        if (headerUserButton) {
            headerUserButton.addEventListener('click', onHeaderUserButtonClick);
        }

        var headerAppsButton = document.querySelector('.headerAppsButton');
        if (headerAppsButton) {
            headerAppsButton.addEventListener('click', onHeaderAppsButtonClick);
        }

        var viewMenuBar = document.querySelector(".viewMenuBar");
        initHeadRoom(viewMenuBar);

        viewMenuBar.querySelector('.btnNotifications').addEventListener('click', function () {
            Dashboard.navigate('notificationlist.html');
        });
    }

    function getItemHref(item, context) {

        return LibraryBrowser.getHref(item, context);
    }

    var requiresUserRefresh = true;
    var lastOpenTime = new Date().getTime();

    function toggleMainDrawer() {

        if (mainDrawerPanel.selected == 'drawer') {
            closeMainDrawer(mainDrawerPanel);
        } else {
            openMainDrawer(mainDrawerPanel);
        }
    }

    function openMainDrawer(drawerPanel) {

        drawerPanel = drawerPanel || document.querySelector('.mainDrawerPanel');
        drawerPanel.openDrawer();
        lastOpenTime = new Date().getTime();
    }

    function onMainDrawerOpened() {

        if (browserInfo.mobile) {
            document.body.classList.add('bodyWithPopupOpen');
        }
    }
    function closeMainDrawer(drawerPanel) {

        drawerPanel = drawerPanel || document.querySelector('.mainDrawerPanel');
        drawerPanel.closeDrawer();
    }
    function onMainDrawerSelect(e) {

        var drawer = e.target;

        if (drawer.selected != 'drawer') {
            document.body.classList.remove('bodyWithPopupOpen');
        } else {
            onMainDrawerOpened();
        }
    }

    function refreshLibraryInfoInDrawer(user, drawer) {

        var html = '';

        html += '<div style="height:.5em;"></div>';

        var homeHref = window.ApiClient ? 'home.html' : 'selectserver.html?showuser=1';

        html += '<a class="lnkMediaFolder sidebarLink" href="' + homeHref + '" onclick="return LibraryMenu.onLinkClicked(event, this);">';
        html += '<div style="background-image:url(\'css/images/mblogoicon.png\');width:' + 28 + 'px;height:' + 28 + 'px;background-size:contain;background-repeat:no-repeat;background-position:center center;border-radius:1000px;vertical-align:middle;margin:0 1.6em 0 1.5em;display:inline-block;"></div>';
        html += Globalize.translate('ButtonHome');
        html += '</a>';

        html += '<a class="sidebarLink lnkMediaFolder" data-itemid="remote" href="nowplaying.html" onclick="return LibraryMenu.onLinkClicked(event, this);"><iron-icon icon="tablet-android" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonRemote') + '</span></a>';

        html += '<div class="sidebarDivider"></div>';

        html += '<div class="libraryMenuOptions">';
        html += '</div>';

        var localUser = user.localUser;
        if (localUser && localUser.Policy.IsAdministrator) {

            html += '<div class="adminMenuOptions">';
            html += '<div class="sidebarDivider"></div>';

            html += '<div class="sidebarHeader">';
            html += Globalize.translate('HeaderAdmin');
            html += '</div>';

            html += '<a class="sidebarLink lnkMediaFolder lnkManageServer" data-itemid="dashboard" href="#"><iron-icon icon="dashboard" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonManageServer') + '</span></a>';
            html += '<a class="sidebarLink lnkMediaFolder editorViewMenu" data-itemid="editor" onclick="return LibraryMenu.onLinkClicked(event, this);" href="edititemmetadata.html"><iron-icon icon="mode-edit" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonMetadataManager') + '</span></a>';

            if (!browserInfo.mobile) {
                html += '<a class="sidebarLink lnkMediaFolder" data-itemid="reports" onclick="return LibraryMenu.onLinkClicked(event, this);" href="reports.html"><iron-icon icon="insert-chart" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonReports') + '</span></a>';
            }
            html += '</div>';
        }

        html += '<div class="userMenuOptions">';

        html += '<div class="sidebarDivider"></div>';

        if (user.localUser && (AppInfo.isNativeApp && browserInfo.android)) {
            html += '<a class="sidebarLink lnkMediaFolder lnkMySettings" onclick="return LibraryMenu.onLinkClicked(event, this);" href="mypreferencesmenu.html?userId=' + user.localUser.Id + '"><iron-icon icon="settings" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSettings') + '</span></a>';
        }

        html += '<a class="sidebarLink lnkMediaFolder lnkMySync" data-itemid="mysync" onclick="return LibraryMenu.onLinkClicked(event, this);" href="mysync.html"><iron-icon icon="sync" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSync') + '</span></a>';

        if (Dashboard.isConnectMode()) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="selectserver" onclick="return LibraryMenu.onLinkClicked(event, this);" href="selectserver.html?showuser=1"><iron-icon icon="wifi" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSelectServer') + '</span></a>';
        }

        if (user.localUser) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="logout" onclick="return LibraryMenu.onLogoutClicked(this);" href="#"><iron-icon icon="lock" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSignOut') + '</span></a>';
        }

        html += '</div>';

        var drawer = mainDrawerPanel.querySelector('.mainDrawer');

        drawer.innerHTML = html;

        var lnkManageServer = drawer.querySelector('.lnkManageServer');
        if (lnkManageServer) {
            lnkManageServer.addEventListener('click', onManageServerClicked);
        }

        require(['imageLoader'], function (imageLoader) {
            imageLoader.fillImages(mainDrawerPanel.getElementsByClassName('lazy'));
        });
    }

    function refreshDashboardInfoInDrawer(page, user) {

        if (!mainDrawerPanel.querySelector('.adminDrawerLogo')) {
            createDashboardMenu(page);
        } else {
            updateDashboardMenuSelectedItem();
        }
    }

    function parentWithTag(elem, tagName) {

        while (elem.tagName != tagName) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function updateDashboardMenuSelectedItem() {

        var links = mainDrawerPanel.querySelectorAll('.sidebarLink');

        for (var i = 0, length = links.length; i < length; i++) {
            var link = links[i];

            var selected = false;

            var pageIds = link.getAttribute('data-pageids');
            if (pageIds) {
                selected = pageIds.split(',').indexOf($.mobile.activePage.id) != -1
            }

            if (selected) {
                link.classList.add('selectedSidebarLink');

                var title = '';

                var secondaryTitle = (link.innerText || link.textContent).trim();
                title += secondaryTitle;

                var documentTitle = secondaryTitle;

                Dashboard.setPageTitle(title, documentTitle);

            } else {
                link.classList.remove('selectedSidebarLink');
            }
        }
    }

    function createDashboardMenu() {
        var html = '';

        html += '<a class="adminDrawerLogo clearLink" href="home.html">'
        html += '<img src="css/images/logo.png" />';
        html += '</a>';

        html += Dashboard.getToolsMenuHtml();

        html = html.split('href=').join('onclick="return LibraryMenu.onLinkClicked(event, this);" href=');

        mainDrawerPanel.querySelector('.mainDrawer').innerHTML = html;

        updateDashboardMenuSelectedItem();
    }

    function onSidebarLinkClick() {
        var section = this.getElementsByClassName('sectionName')[0];
        var text = section ? section.innerHTML : this.innerHTML;

        LibraryMenu.setTitle(text);
    }

    function getUserViews(apiClient, userId) {

        return apiClient.getUserViews({}, userId).then(function (result) {

            var items = result.Items;

            var list = [];

            for (var i = 0, length = items.length; i < length; i++) {

                var view = items[i];

                list.push(view);

                if (view.CollectionType == 'livetv') {

                    view.ImageTags = {};
                    view.icon = 'live-tv';
                    view.onclick = "LibraryBrowser.showTab('livetv.html', 0);";

                    var guideView = Object.assign({}, view);
                    guideView.Name = Globalize.translate('ButtonGuide');
                    guideView.ImageTags = {};
                    guideView.icon = 'dvr';
                    guideView.url = 'livetv.html?tab=1';
                    guideView.onclick = "LibraryBrowser.showTab('livetv.html', 1);";
                    list.push(guideView);
                }
            }

            return list;
        });
    }

    function showBySelector(selector, show) {
        var elem = document.querySelector(selector);

        if (elem) {
            if (show) {
                elem.classList.remove('hide');
            } else {
                elem.classList.add('hide');
            }
        }
    }

    function updateLibraryMenu(user) {

        if (!user) {

            showBySelector('.lnkMySync', false);
            showBySelector('.userMenuOptions', false);
            return;
        }

        if (user.Policy.EnableSync) {
            showBySelector('.lnkMySync', true);
        } else {
            showBySelector('.lnkMySync', false);
        }

        var userId = Dashboard.getCurrentUserId();

        var apiClient = window.ApiClient;

        var libraryMenuOptions = document.querySelector('.libraryMenuOptions');

        if (!libraryMenuOptions) {
            return;
        }

        getUserViews(apiClient, userId).then(function (result) {

            var items = result;

            var html = '';
            html += '<div class="sidebarHeader">';
            html += Globalize.translate('HeaderMedia');
            html += '</div>';

            html += items.map(function (i) {

                var icon = 'folder';
                var color = 'inherit';
                var itemId = i.Id;

                if (i.CollectionType == "channels") {
                    itemId = "channels";
                }
                else if (i.CollectionType == "livetv") {
                    itemId = "livetv";
                }

                if (i.CollectionType == "photos") {
                    icon = 'photo-library';
                    color = "#009688";
                }
                else if (i.CollectionType == "music" || i.CollectionType == "musicvideos") {
                    icon = 'library-music';
                    color = '#FB8521';
                }
                else if (i.CollectionType == "books") {
                    icon = 'library-books';
                    color = "#1AA1E1";
                }
                else if (i.CollectionType == "playlists") {
                    icon = 'view-list';
                    color = "#795548";
                }
                else if (i.CollectionType == "games") {
                    icon = 'games';
                    color = "#F44336";
                }
                else if (i.CollectionType == "movies") {
                    icon = 'video-library';
                    color = '#CE5043';
                }
                else if (i.CollectionType == "channels" || i.Type == 'Channel') {
                    icon = 'videocam';
                    color = '#E91E63';
                }
                else if (i.CollectionType == "tvshows") {
                    icon = 'tv';
                    color = "#4CAF50";
                }
                else if (i.CollectionType == "livetv") {
                    icon = 'live-tv';
                    color = "#293AAE";
                }

                icon = i.icon || icon;

                var onclick = i.onclick ? ' function(){' + i.onclick + '}' : 'null';
                return '<a data-itemid="' + itemId + '" class="lnkMediaFolder sidebarLink" onclick="return LibraryMenu.onLinkClicked(event, this, ' + onclick + ');" href="' + getItemHref(i, i.CollectionType) + '"><iron-icon icon="' + icon + '" class="sidebarLinkIcon" style="color:' + color + '"></iron-icon><span class="sectionName">' + i.Name + '</span></a>';

            }).join('');

            libraryMenuOptions.innerHTML = html;
            var elem = libraryMenuOptions;

            var sidebarLinks = elem.querySelectorAll('.sidebarLink');
            for (var i = 0, length = sidebarLinks.length; i < length; i++) {
                sidebarLinks[i].removeEventListener('click', onSidebarLinkClick);
                sidebarLinks[i].addEventListener('click', onSidebarLinkClick);
            }
        });
    }

    function onManageServerClicked() {

        closeMainDrawer();

        Dashboard.navigate('dashboard.html');
    }

    function getTopParentId() {

        return getParameterByName('topParentId') || null;
    }

    window.LibraryMenu = {
        getTopParentId: getTopParentId,

        onLinkClicked: function (event, link, action) {

            if (event.which != 1) {
                return true;
            }

            // There doesn't seem to be a way to detect if the drawer is in the process of opening, so try to handle that here
            if ((new Date().getTime() - lastOpenTime) > 200) {

                setTimeout(function () {
                    closeMainDrawer();

                    // On mobile devices don't navigate until after the closing animation has completed or it may stutter
                    var delay = browserInfo.mobile ? 350 : 200;

                    setTimeout(function () {
                        if (action) {
                            action();
                        } else {
                            Dashboard.navigate(link.href);
                        }
                    }, delay);

                }, 50);
            }

            event.stopPropagation();
            event.preventDefault();
            return false;
        },

        onLogoutClicked: function () {
            // There doesn't seem to be a way to detect if the drawer is in the process of opening, so try to handle that here
            if ((new Date().getTime() - lastOpenTime) > 200) {

                closeMainDrawer();

                // On mobile devices don't navigate until after the closing animation has completed or it may stutter
                var delay = browserInfo.mobile ? 350 : 200;

                setTimeout(function () {
                    Dashboard.logout();
                }, delay);
            }

            return false;
        },

        onHardwareMenuButtonClick: function () {
            toggleMainDrawer();
        },

        onSettingsClicked: function (event) {

            if (event.which != 1) {
                return true;
            }

            // There doesn't seem to be a way to detect if the drawer is in the process of opening, so try to handle that here
            Dashboard.navigate('dashboard.html');
            return false;
        },

        setTabs: function (type, selectedIndex, builder) {

            var viewMenuBarTabs;

            if (!type) {
                if (LibraryMenu.tabType) {

                    mainDrawerPanel.classList.remove('withTallToolbar');
                    viewMenuBarTabs = document.querySelector('.viewMenuBarTabs');
                    viewMenuBarTabs.innerHTML = '';
                    viewMenuBarTabs.classList.add('hide');
                    LibraryMenu.tabType = null;
                }
                return;
            }

            viewMenuBarTabs = document.querySelector('.viewMenuBarTabs');

            if (!LibraryMenu.tabType) {
                viewMenuBarTabs.classList.remove('hide');
            }

            if (LibraryMenu.tabType != type) {

                require(['paper-tabs'], function () {

                    var noInk = browserInfo.animate ? '' : ' noink';

                    viewMenuBarTabs.innerHTML = '<paper-tabs selected="' + selectedIndex + '" hidescrollbuttons ' + noInk + '>' + builder().map(function (t) {

                        return '<paper-tab link><a class="clearLink paperTabLink" href="' + t.href + '"><div>' + t.name + '</div></a></paper-tab>';

                    }).join('') + '</paper-tabs>';
                    mainDrawerPanel.classList.add('withTallToolbar');
                    LibraryMenu.tabType = type;
                });
                return;
            }

            viewMenuBarTabs.querySelector('paper-tabs').selected = selectedIndex;
            LibraryMenu.tabType = type;
        },

        setTitle: function (title) {

            var html = title;

            if (window.$ && $.mobile) {
                var page = $.mobile.activePage;
                if (page) {
                    var helpUrl = page.getAttribute('data-helpurl');

                    if (helpUrl) {
                        html += '<a href="' + helpUrl + '" target="_blank" class="clearLink" style="margin-left:2em;" title="' + Globalize.translate('ButtonHelp') + '"><paper-button class="accent" style="margin:0;font-weight:normal;font-size:13px;padding:.25em;display:flex;align-items:center;"><iron-icon icon="info"></iron-icon><span>' + Globalize.translate('ButtonHelp') + '</span></paper-button></a>';
                    }
                }
            }

            document.querySelector('.libraryMenuButtonText').innerHTML = html;
        },

        setBackButtonVisible: function (visible) {

            var backButton = document.querySelector('.headerBackButton');

            if (backButton) {
                if (visible) {
                    backButton.classList.remove('hide');
                } else {
                    backButton.classList.add('hide');
                }
            }
        },

        setMenuButtonVisible: function (visible) {

            var mainDrawerButton = document.querySelector('.mainDrawerButton');

            if (mainDrawerButton) {
                if (!visible && browserInfo.mobile) {
                    mainDrawerButton.classList.remove('hide');
                } else {
                    mainDrawerButton.classList.remove('hide');
                }
            }
        },
        setTransparentMenu: function (transparent) {

            var viewMenuBar = document.querySelector('.viewMenuBar');

            if (viewMenuBar) {
                if (transparent) {
                    viewMenuBar.classList.add('semiTransparent');
                } else {
                    viewMenuBar.classList.remove('semiTransparent');
                }
            }
        }
    };

    function updateCastIcon() {

        var context = document;

        var btnCast = context.querySelector('.btnCast');

        if (!btnCast) {
            return;
        }

        var info = MediaController.getPlayerInfo();

        if (info.isLocalPlayer) {

            btnCast.querySelector('iron-icon').icon = 'cast';
            btnCast.classList.remove('btnActiveCast');

            context.querySelector('.headerSelectedPlayer').innerHTML = '';

        } else {

            btnCast.querySelector('iron-icon').icon = 'cast-connected';
            btnCast.classList.add('btnActiveCast');
            context.querySelector('.headerSelectedPlayer').innerHTML = info.deviceName || info.name;
        }
    }

    function updateLibraryNavLinks(page) {

        var isLiveTvPage = page.classList.contains('liveTvPage');
        var isChannelsPage = page.classList.contains('channelsPage');
        var isEditorPage = page.classList.contains('metadataEditorPage');
        var isReportsPage = page.classList.contains('reportsPage');
        var isMySyncPage = page.classList.contains('mySyncPage');

        var id = isLiveTvPage || isChannelsPage || isEditorPage || isReportsPage || isMySyncPage || page.classList.contains('allLibraryPage') ?
            '' :
            getTopParentId() || '';

        var i, length;
        var elems = document.getElementsByClassName('lnkMediaFolder');

        for (i = 0, length = elems.length; i < length; i++) {

            var lnkMediaFolder = elems[i];
            var itemId = lnkMediaFolder.getAttribute('data-itemid');

            if (isChannelsPage && itemId == 'channels') {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (isLiveTvPage && itemId == 'livetv') {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (isEditorPage && itemId == 'editor') {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (isReportsPage && itemId == 'reports') {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (isMySyncPage && itemId == 'mysync') {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (id && itemId == id) {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else {
                lnkMediaFolder.classList.remove('selectedMediaFolder');
            }
        }
    }

    function updateTabLinks(page) {

        var elems = page.querySelectorAll('.scopedLibraryViewNav a');

        var id = page.classList.contains('liveTvPage') || page.classList.contains('channelsPage') || page.classList.contains('metadataEditorPage') || page.classList.contains('reportsPage') || page.classList.contains('mySyncPage') || page.classList.contains('allLibraryPage') ?
            '' :
            getTopParentId() || '';

        if (!id) {
            return;
        }

        for (i = 0, length = elems.length; i < length; i++) {

            var lnk = elems[i];
            var src = lnk.href;

            if (src.indexOf('#') != -1) {
                continue;
            }

            src = replaceQueryString(src, 'topParentId', id);

            lnk.href = src;
        }
    }

    function onWebSocketMessage(e, data) {

        var msg = data;

        if (msg.MessageType === "UserConfigurationUpdated") {

            if (msg.Data.Id == Dashboard.getCurrentUserId()) {

                // refresh library menu
            }
        }
    }

    function updateViewMenuBar(page) {

        var viewMenuBar = document.querySelector('.viewMenuBar');

        if (viewMenuBar) {
            if (page.classList.contains('standalonePage')) {
                viewMenuBar.classList.add('hide');
            } else {
                viewMenuBar.classList.remove('hide');
            }

            if (page.classList.contains('type-interior') && !layoutManager.mobile) {
                viewMenuBar.classList.add('headroomDisabled');
            } else {
                viewMenuBar.classList.remove('headroomDisabled');
            }
        }

        if (requiresUserRefresh) {
            ConnectionManager.user(window.ApiClient).then(updateUserInHeader);
        }
    }

    pageClassOn('pageinit', 'page', function () {

        var page = this;

        var isLibraryPage = page.classList.contains('libraryPage');

        if (isLibraryPage) {

            var navs = page.querySelectorAll('.libraryViewNav');
            for (var i = 0, length = navs.length; i < length; i++) {
                initHeadRoom(navs[i]);
            }
        }
    });

    pageClassOn('pagebeforeshow', 'page', function (e) {

        var page = this;

        if (!page.classList.contains('withTabs')) {
            LibraryMenu.setTabs(null);
        }
    });

    pageClassOn('pageshow', 'page', function (e) {

        var page = this;

        var isDashboardPage = page.classList.contains('type-interior');

        if (isDashboardPage) {
            refreshDashboardInfoInDrawer(page);
            mainDrawerPanel.forceNarrow = false;
        } else {

            if (mainDrawerPanel.classList.contains('adminDrawerPanel')) {
                refreshLibraryDrawer();
            }

            mainDrawerPanel.forceNarrow = true;
        }

        setDrawerClass(page);

        updateViewMenuBar(page);
        updateTabLinks(page);

        if (!e.detail.isRestored) {
            // Scroll back up so in case vertical scroll was messed with
            window.scrollTo(0, 0);
        }

        updateTitle(page);
        updateBackButton(page);

        if (page.classList.contains('libraryPage')) {

            document.body.classList.add('libraryDocument');
            document.body.classList.remove('dashboardDocument');
            document.body.classList.remove('hideMainDrawer');
        }
        else if (isDashboardPage) {

            document.body.classList.remove('libraryDocument');
            document.body.classList.add('dashboardDocument');
            document.body.classList.remove('hideMainDrawer');

        } else {

            document.body.classList.remove('libraryDocument');
            document.body.classList.remove('dashboardDocument');
            document.body.classList.add('hideMainDrawer');
        }

        updateLibraryNavLinks(page);
    });

    function updateTitle(page) {
        var title = page.getAttribute('data-title') || page.getAttribute('data-contextname');

        if (!title) {
            var titleKey = getParameterByName('titlekey');

            if (titleKey) {
                title = Globalize.translate(titleKey);
            }
        }

        if (title) {
            LibraryMenu.setTitle(title);
        }
    }

    function updateBackButton(page) {

        var canGoBack = !page.classList.contains('homePage') && history.length > 0;

        var backButton = document.querySelector('.headerBackButton');

        var showBackButton = AppInfo.enableBackButton;

        if (!showBackButton) {
            showBackButton = page.getAttribute('data-backbutton') == 'true';
        }

        if (backButton) {
            if (canGoBack && showBackButton) {
                backButton.classList.remove('hide');
            } else {
                backButton.classList.add('hide');
            }
        }
    }

    function initHeadRoom(elem) {

        if (!AppInfo.enableHeadRoom) {
            return;
        }

        require(["headroom"], function () {

            // construct an instance of Headroom, passing the element
            var headroom = new Headroom(elem, {
                // or scroll tolerance per direction
                tolerance: {
                    down: 40,
                    up: 0
                }
            });
            // initialise
            headroom.init();
        });
    }

    function initializeApiClient(apiClient) {

        Events.off(apiClient, 'websocketmessage', onWebSocketMessage);

        Events.on(apiClient, 'websocketmessage', onWebSocketMessage);
    }

    if (window.ApiClient) {
        initializeApiClient(window.ApiClient);
    }

    function setDrawerClass(page) {

        var admin = false;

        if (!page) {
            if (window.$ && window.$.mobile) {
                page = $.mobile.activePage;
            }
        }

        if (page && page.classList.contains('type-interior')) {
            admin = true;
        }

        if (admin) {
            mainDrawerPanel.classList.add('adminDrawerPanel');
            mainDrawerPanel.classList.remove('darkDrawerPanel');
        } else {
            mainDrawerPanel.classList.add('darkDrawerPanel');
            mainDrawerPanel.classList.remove('adminDrawerPanel');
        }
    }

    function refreshLibraryDrawer(user) {

        var promise = user ? Promise.resolve(user) : ConnectionManager.user(window.ApiClient);

        promise.then(function (user) {
            refreshLibraryInfoInDrawer(user);

            document.dispatchEvent(new CustomEvent("libraryMenuCreated", {}));
            updateLibraryMenu(user.localUser);
        });
    }

    mainDrawerPanel.addEventListener('iron-select', onMainDrawerSelect);
    var headerCreated;
    var userRequiresUpdateAfterHeader;

    require(['paper-icon-button-light', 'emby-icons'], function () {
        renderHeader();
        headerCreated = true;

        var user = userRequiresUpdateAfterHeader;
        if (user) {
            updateUserInHeader(user);
        }
        userRequiresUpdateAfterHeader = null;
    });

    Events.on(ConnectionManager, 'apiclientcreated', function (e, apiClient) {
        initializeApiClient(apiClient);
    });

    Events.on(ConnectionManager, 'localusersignedin', function (e, user) {
        setDrawerClass();
        ConnectionManager.user(ConnectionManager.getApiClient(user.ServerId)).then(function (user) {
            refreshLibraryDrawer(user);

            if (headerCreated) {
                updateUserInHeader(user);
            } else {
                userRequiresUpdateAfterHeader = user;
            }
        });
    });

    Events.on(ConnectionManager, 'localusersignedout', updateUserInHeader);
    Events.on(MediaController, 'playerchange', updateCastIcon);

    setDrawerClass();

});