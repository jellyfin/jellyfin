(function ($, document, window, clearTimeout, setTimeout) {

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

            return [Globalize.translate('LabelSeries')];
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

        var html = LibraryBrowser.getPosterViewHtml({
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
            searchTerm: searchTerm,
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

            $('.itemsContainer', elem).empty();
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

            var html = '<div class="searchResultsOverlay ui-body-b smoothScrollY background-theme-b">';

            html += '<div class="searchResultsContainer"><div class="itemsContainer"></div></div></div>';

            elem = $(html).appendTo(document.body)[0];
            $(elem).createCardMenus();
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
        elem.animate(keyframes, timing);
    }

    function fadeOut(elem, iterations) {
        var keyframes = [
          { opacity: '1', offset: 0 },
          { opacity: '0', offset: 1 }];
        var timing = { duration: 600, iterations: iterations, fill: 'both' };
        elem.animate(keyframes, timing).onfinish = function () {
            elem.parentNode.removeChild(elem);
        };
    }

    function bindSearchEvents() {

        require(['searchmenu'], function () {
            Events.on(SearchMenu, 'closed', closeSearchResults);
            Events.on(SearchMenu, 'change', function (e, value) {

                onHeaderSearchChange(value);
            });
        });
    }

    function closeSearchResults() {

        onHeaderSearchChange('');
        hideSearchMenu();
    }

    function showSearchMenu() {
        require(['searchmenu'], function () {
            SearchMenu.show();
        });
    }

    function hideSearchMenu() {
        require(['searchmenu'], function () {
            SearchMenu.hide();
        });
    }

    document.addEventListener('pagebeforehide', closeSearchResults);

    document.addEventListener('headercreated', function () {

        bindSearchEvents();
    });

})(jQuery, document, window, clearTimeout, setTimeout);