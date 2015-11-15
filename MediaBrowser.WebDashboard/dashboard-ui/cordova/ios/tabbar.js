(function () {

    var initComplete = false;
    var ignoreNextSelection = false;

    function onTabSelected(name) {

        if (ignoreNextSelection) {
            ignoreNextSelection = false;
            return;
        }

        switch (name) {
            case 'Favorites':
                Dashboard.navigate('favorites.html');
                break;
            case 'Library':
                Dashboard.navigate('index.html');
                break;
            case 'Search':
                Dashboard.navigate('search.html');
                break;
            case 'NowPlaying':
                Dashboard.navigate('nowplaying.html');
                break;
            case 'Sync':
                Dashboard.navigate('mysync.html');
                break;
            case 'LiveTv':
                Dashboard.navigate('livetv.html');
                break;
            case 'Settings':
                Dashboard.navigate('mypreferencesmenu.html?userId=' + Dashboard.getCurrentUserId());
                break;
            default:
                break;
        }
    }

    function init() {

        // Use system defined items for this demo
        // If an image is passed, label is not used

        /**
 * Create a new tab bar item for use on a previously created tab bar.  Use ::showTabBarItems to show the new item on the tab bar.
 *
 * If the supplied image name is one of the labels listed below, then this method will construct a tab button
 * using the standard system buttons.  Note that if you use one of the system images, that the \c title you supply will be ignored.
 * - <b>Tab Buttons</b>
 *   - tabButton:More
 *   - tabButton:Favorites
 *   - tabButton:Featured
 *   - tabButton:TopRated
 *   - tabButton:Recents
 *   - tabButton:Contacts
 *   - tabButton:History
 *   - tabButton:Bookmarks
 *   - tabButton:Search
 *   - tabButton:Downloads
 *   - tabButton:MostRecent
 *   - tabButton:MostViewed
 * @brief create a tab bar item
 * @param arguments Parameters used to create the tab bar
 *  -# \c name internal name to refer to this tab by
 *  -# \c title title text to show on the tab, or null if no text should be shown
 *  -# \c image image filename or internal identifier to show, or null if now image should be shown
 *  -# \c tag unique number to be used as an internal reference to this button
 * @param options Options for customizing the individual tab item
 *  - \c badge value to display in the optional circular badge on the item; if nil or unspecified, the badge will be hidden
 */

        var items = [
          { name: 'Library', label: Globalize.translate('ButtonLibrary'), image: 'tabbar/tab-library.png', options: {} },
          { name: 'LiveTv', label: Globalize.translate('HeaderLiveTV'), image: 'tabbar/tab-livetv.png', options: {} },
          { name: 'Favorites', label: Globalize.translate('ButtonFavorites'), image: 'tabButton:Favorites', options: {} },
          { name: 'Search', label: Globalize.translate('ButtonSearch'), image: 'tabButton:Search', options: {} },
          { name: 'NowPlaying', label: Globalize.translate('ButtonNowPlaying'), image: 'tabbar/tab-nowplaying.png', options: {} },
          { name: 'Sync', label: Globalize.translate('ButtonSync'), image: 'tabbar/tab-sync.png', options: {} },
          { name: 'Settings', label: Globalize.translate('ButtonSettings'), image: 'tabbar/tab-settings.png', options: {} }
        ];

        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            TabBar.createItem(item.name, item.label, item.image, onTabSelected, item.options);
        };

        TabBar.showItems();
        initComplete = true;

        ignoreNextSelection = true;
        TabBar.selectItem('Library');
    }

    function showTabs() {

        if (!initComplete) {
            return;
        }

        TabBar.show();
    }

    function showUserTabs(user) {

        if (!window.ApiClient) {
            onUserViewResponse(user, []);
            return;
        }

        ApiClient.getUserViews({}, user.Id).done(function (result) {

            onUserViewResponse(user, result.Items);

        }).fail(function (result) {

            onUserViewResponse(user, []);
        });

    }

    function onUserViewResponse(user, views) {

        var tabs = ['Library'];

        if (views.filter(function (v) {

            return v.CollectionType == 'livetv';

        }).length) {
            tabs.push('LiveTv');
        }

        tabs.push('Favorites');
        tabs.push('Search');
        tabs.push('NowPlaying');

        if (user.Policy.EnableSync && Dashboard.capabilities().SupportsSync) {

            tabs.push('Sync');
        }

        tabs.push('Settings');

        TabBar.showNamedItems(tabs);

        // We need to make sure the above completes first
        setTimeout(showTabs, 500);
    }

    function showCurrentUserTabs() {

        if (!Dashboard.getCurrentUserId()) {
            return;
        }

        Dashboard.getCurrentUser().done(showUserTabs);
    }

    var isFirstHide = true;
    function hideTabs() {

        if (!initComplete) {
            return;
        }

        var hide = function () { TabBar.hide(); };
        if (isFirstHide) {
            isFirstHide = false;
            setTimeout(hide, 1000);
        } else {
            hide();
        }
    }

    Dashboard.ready(function () {

        init();

        Events.on(ConnectionManager, 'localusersignedin', function (e, user) {
            showUserTabs(user);
        });

        Events.on(ConnectionManager, 'localusersignedout', hideTabs);
        Events.on(MediaController, 'beforeplaybackstart', onPlaybackStart);
        Events.on(MediaController, 'playbackstop', onPlaybackStop);

        showCurrentUserTabs();
    });

    function onPlaybackStart(e, state, player) {

        if (state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video' && player.isLocalPlayer) {
            hideTabs();
        }
    }

    function onPlaybackStop(e, state, player) {

        if (state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video' && player.isLocalPlayer) {
            showTabs();
        }
    }

})();