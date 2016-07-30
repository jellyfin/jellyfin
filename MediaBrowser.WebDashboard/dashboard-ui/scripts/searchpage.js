define(['libraryBrowser', 'focusManager', 'embyRouter', 'cardBuilder', 'emby-input', 'paper-icon-button-light', 'material-icons', 'emby-itemscontainer'], function (libraryBrowser, focusManager, embyRouter, cardBuilder) {

    function loadSuggestions(page) {

        var options = {

            SortBy: "IsFavoriteOrLiked,Random",
            IncludeItemTypes: "Movie,Series,MusicArtist",
            Limit: 20,
            Recursive: true,
            ImageTypeLimit: 0,
            EnableImages: false
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            var html = result.Items.map(function (i) {

                var href = LibraryBrowser.getHref(i);

                var itemHtml = '<div><a style="display:inline-block;padding:.55em 1em;" href="' + href + '">';
                itemHtml += i.Name;
                itemHtml += '</a></div>';
                return itemHtml;

            }).join('');

            page.querySelector('.searchSuggestions').innerHTML = html;
        });
    }

    return function (view, params) {

        var textSuggestions = view.querySelector('.textSuggestions');
        var searchResults = view.querySelector('.searchResults');
        var searchHintTimeout;

        function clearSearchHintTimeout() {

            if (searchHintTimeout) {

                clearTimeout(searchHintTimeout);
                searchHintTimeout = null;
            }
        }

        function showTextSuggestions() {
            if (AppInfo.enableAppLayouts) {
                textSuggestions.classList.remove('hide');
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

        function renderSearchResultsInOverlay(hints) {

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

            var html = cardBuilder.getCardsHtml({
                items: hints,
                shape: "auto",
                lazy: true,
                overlayText: false,
                showTitle: true,
                centerImage: true,
                centerText: true,
                textLines: getAdditionalTextLines,
                overlayPlayButton: true,
                serverId: ApiClient.serverInfo().Id
            });

            if (!hints.length) {
                html = '<p style="text-align:center;margin-top:2em;">' + Globalize.translate('NoResultsFound') + '</p>';
            }

            var itemsContainer = searchResults;
            itemsContainer.innerHTML = html;
            searchResults.classList.remove('hide');
            textSuggestions.classList.add('hide');
            ImageLoader.lazyChildren(itemsContainer);
        }

        function requestSearchHintsForOverlay(searchTerm) {

            var currentTimeout = searchHintTimeout;
            Dashboard.showLoadingMsg();

            ApiClient.getSearchHints({

                userId: Dashboard.getCurrentUserId(),
                searchTerm: (searchTerm || '').trim(),
                limit: 30

            }).then(function (result) {

                if (currentTimeout == searchHintTimeout) {
                    renderSearchResultsInOverlay(result.SearchHints);
                }

                Dashboard.hideLoadingMsg();
            }, function () {
                Dashboard.hideLoadingMsg();
            });
        }

        function onSearchChange(val) {

            if (!val) {
                clearSearchHintTimeout();
                searchResults.classList.add('hide');
                searchResults.innerHTML = '';
                showTextSuggestions();
                return;
            }

            clearSearchHintTimeout();

            searchHintTimeout = setTimeout(function () {
                requestSearchHintsForOverlay(val);
            }, 300);
        }

        if (AppInfo.enableAppLayouts) {
            showTextSuggestions();
            loadSuggestions(view);
        }

        view.querySelector('.txtSearch').addEventListener('input', function () {
            onSearchChange(this.value);
        });

        view.querySelector('.btnBack').addEventListener('click', function () {
            embyRouter.back();
        });

        view.addEventListener('viewbeforeshow', function (e) {
            document.body.classList.add('hiddenViewMenuBar');
            document.body.classList.add('hiddenNowPlayingBar');
            LibraryMenu.setMenuButtonVisible(false);
        });

        view.addEventListener('viewbeforehide', function (e) {

            document.body.classList.remove('hiddenViewMenuBar');
            document.body.classList.remove('hiddenNowPlayingBar');
            LibraryMenu.setMenuButtonVisible(true);
        });

    };
});