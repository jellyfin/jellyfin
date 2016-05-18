define(['libraryBrowser', 'events', 'scrollStyles'], function (libraryBrowser, events) {

    var searchHintTimeout;

    function clearSearchHintTimeout() {

        if (searchHintTimeout) {

            clearTimeout(searchHintTimeout);
            searchHintTimeout = null;
        }
    }

    function getAdditionalTextLines(hint) {

        if (hint.Type == "Audio") {

            return [[hint.AlbumArtist, hint.Album].join(" - ")];

        }
        else if (hint.Type == "MusicAlbum") {

            return [hint.AlbumArtist];

        }
        else if (hint.Type == "MusicArtist") {

            return [Globalize.translate('LabelArtist')];

        }
        else if (hint.Type == "Movie") {

            return [Globalize.translate('LabelMovie')];

        }
        else if (hint.Type == "MusicVideo") {

            return [Globalize.translate('LabelMusicVideo')];
        }
        else if (hint.Type == "Episode") {

            return [Globalize.translate('LabelEpisode')];

        }
        else if (hint.Type == "Series") {

            return [Globalize.translate('Series')];
        }
        else if (hint.Type == "BoxSet") {

            return [Globalize.translate('LabelCollection')];
        }
        else if (hint.ChannelName) {

            return [hint.ChannelName];
        }

        return [hint.Type];
    }

    function search() {

        var self = this;

        self.showSearchPanel = function () {

            showSearchMenu();
        };
    }
    window.Search = new search();

    function renderSearchResultsInOverlay(elem, hints) {

        // Massage the objects to look like regular items
        hints = hints.map(function (i) {

            i.Id = i.ItemId;
            i.ImageTags = {};
            i.UserData = {};

            if (i.PrimaryImageTag) {
                i.ImageTags.Primary = i.PrimaryImageTag;
            }
            return i;
        });

        var html = libraryBrowser.getPosterViewHtml({
            items: hints,
            shape: "auto",
            lazy: true,
            overlayText: false,
            showTitle: true,
            centerImage: true,
            centerText: true,
            textLines: getAdditionalTextLines,
            overlayPlayButton: true
        });

        var itemsContainer = elem.querySelector('.itemsContainer');
        itemsContainer.innerHTML = html;
        ImageLoader.lazyChildren(itemsContainer);
    }

    function requestSearchHintsForOverlay(elem, searchTerm) {

        var currentTimeout = searchHintTimeout;
        Dashboard.showLoadingMsg();

        ApiClient.getSearchHints({

            userId: Dashboard.getCurrentUserId(),
            searchTerm: (searchTerm || '').trim(),
            limit: 30

        }).then(function (result) {

            if (currentTimeout == searchHintTimeout) {
                renderSearchResultsInOverlay(elem, result.SearchHints);
            }

            Dashboard.hideLoadingMsg();
        }, function () {
            Dashboard.hideLoadingMsg();
        });
    }

    function updateSearchOverlay(elem, searchTerm) {

        if (!searchTerm) {

            var itemsContainer = elem.querySelector('.itemsContainer');
            if (itemsContainer) {
                itemsContainer.innerHTML = '';
            }
            clearSearchHintTimeout();
            return;
        }

        clearSearchHintTimeout();

        searchHintTimeout = setTimeout(function () {

            requestSearchHintsForOverlay(elem, searchTerm);

        }, 300);
    }

    function getSearchOverlay(createIfNeeded) {

        var elem = document.querySelector('.searchResultsOverlay');

        if (createIfNeeded && !elem) {

            var div = document.createElement('div');
            div.className = 'searchResultsOverlay ui-body-b smoothScrollY background-theme-b';

            div.innerHTML = '<div class="searchResultsContainer"><div class="itemsContainer"></div></div></div>';

            document.body.appendChild(div);
            libraryBrowser.createCardMenus(div);

            elem = div;
        }

        return elem;
    }

    var isVisible;

    function onHeaderSearchChange(val) {

        var elem;

        if (val) {

            elem = getSearchOverlay(true);

            if (!isVisible) {
                fadeIn(elem, 1);
            }
            isVisible = true;

            document.body.classList.add('bodyWithPopupOpen');

            updateSearchOverlay(elem, val);

        } else {
            elem = getSearchOverlay(false);

            if (elem) {
                updateSearchOverlay(elem, '');

                if (isVisible) {
                    fadeOut(elem, 1);
                    isVisible = false;
                }
                document.body.classList.remove('bodyWithPopupOpen');
            }
        }
    }

    function fadeIn(elem, iterations) {

        var keyframes = [
          { opacity: '0', offset: 0 },
          { opacity: '1', offset: 1 }];
        var timing = { duration: 200, iterations: iterations, fill: 'both' };

        if (elem.animate) {
            elem.animate(keyframes, timing);
        }
    }

    function fadeOut(elem, iterations) {
        var keyframes = [
          { opacity: '1', offset: 0 },
          { opacity: '0', offset: 1 }];
        var timing = { duration: 600, iterations: iterations, fill: 'both' };

        var onfinish = function () {
            elem.parentNode.removeChild(elem);
        };

        if (elem.animate) {
            elem.animate(keyframes, timing).onfinish = onfinish;
        } else {
            onfinish();
        }
    }

    function bindSearchEvents() {

        require(['searchmenu'], function (searchmenu) {
            events.on(window.SearchMenu, 'closed', closeSearchResults);
            events.on(window.SearchMenu, 'change', function (e, value) {

                onHeaderSearchChange(value);
            });
        });
    }

    function closeSearchResults() {

        onHeaderSearchChange('');
        hideSearchMenu();
    }

    function showSearchMenu() {
        require(['searchmenu'], function (searchmenu) {
            window.SearchMenu.show();
        });
    }

    function hideSearchMenu() {
        require(['searchmenu'], function (searchmenu) {
            window.SearchMenu.hide();
        });
    }

    document.addEventListener('viewbeforehide', closeSearchResults);

    document.addEventListener('headercreated', function () {

        bindSearchEvents();
    });

    // dismiss search UI if user clicks a play button on a search result
    events.on(MediaController, 'beforeplaybackstart', closeSearchResults);

});