(function () {

    var initComplete = false;
    var ignoreNextSelection = false;

    function onTabSelected(name) {

        if (ignoreNextSelection) {
            ignoreNextSelection = false;
            return;
        }

        switch (name) {
            case 'Featured':
                Dashboard.navigate('index.html');
                break;
            case 'Library':
                Dashboard.navigate('index.html');
                break;
            case 'Search':
                Dashboard.navigate('index.html');
                break;
            case 'NowPlaying':
                Dashboard.navigate('nowplaying.html');
                break;
            case 'Sync':
                Dashboard.navigate('mysync.html');
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
          { name: 'Featured', label: Globalize.translate('ButtonForYou'), image: 'tabButton:Featured', options: {} },
          { name: 'Library', label: Globalize.translate('ButtonLibrary'), image: 'tabButton:History', options: {} },
          { name: 'Search', label: Globalize.translate('ButtonSearch'), image: 'tabButton:Search', options: {} },
          { name: 'NowPlaying', label: Globalize.translate('ButtonNowPlaying'), image: 'tabButton:MostViewed', options: {} },
          { name: 'Sync', label: Globalize.translate('ButtonSync'), image: 'tabButton:Downloads', options: {} },
          { name: 'Settings', label: Globalize.translate('ButtonSettings'), image: 'tabButton:More', options: {} }
        ];

        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            var options = item.options;
            // set the function to invoke when the item is selected
            options.onSelect = onTabSelected;
            TabBar.createItem(item.name, item.label, item.image, item.options);
        };

        TabBar.showItems();
        initComplete = true;
        showTabs();

        ignoreNextSelection = true;
        TabBar.selectItem('Featured');
    }

    function showTabs() {

        if (!initComplete) {
            return;
        }

        TabBar.show();
    }

    function hideTabs() {

        if (!initComplete) {
            return;
        }

        TabBar.hide();
    }

    Dashboard.ready(function () {

        init();

        Events.on(ConnectionManager, 'localusersignedin', showTabs);
        Events.on(ConnectionManager, 'localusersignedout', hideTabs);
    });

    pageClassOn('pageshowready', "page", function () {

        var page = this;

        if (page.classList.contains('libraryPage')) {
            showTabs();
        }
        else {
            hideTabs();
        }
    });

})();