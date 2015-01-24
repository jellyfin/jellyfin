(function ($, document, window, clearTimeout, setTimeout) {

    var searchHintTimeout;

    function clearSearchHintTimeout() {

        if (searchHintTimeout) {

            clearTimeout(searchHintTimeout);
            searchHintTimeout = null;
        }
    }

    function updateSearchHints(page, searchTerm) {

        if (!searchTerm) {

            $('#searchHints', page).empty();
            clearSearchHintTimeout();
            return;
        }

        clearSearchHintTimeout();

        searchHintTimeout = setTimeout(function () {

            requestSearchHints(page, searchTerm);

        }, 100);
    }

    function requestSearchHints(page, searchTerm) {

        var currentTimeout = searchHintTimeout;

        ApiClient.getSearchHints({ userId: Dashboard.getCurrentUserId(), searchTerm: searchTerm, limit: 10 }).done(function (result) {

            if (currentTimeout != searchHintTimeout) {
                return;
            }

            renderSearchHintResult(page, result.SearchHints);
        });
    }

    function preg_quote(str, delimiter) {
        // http://kevin.vanzonneveld.net
        // +   original by: booeyOH
        // +   improved by: Ates Goral (http://magnetiq.com)
        // +   improved by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
        // +   bugfixed by: Onno Marsman
        // +   improved by: Brett Zamir (http://brett-zamir.me)
        // *     example 1: preg_quote("$40");
        // *     returns 1: '\$40'
        // *     example 2: preg_quote("*RRRING* Hello?");
        // *     returns 2: '\*RRRING\* Hello\?'
        // *     example 3: preg_quote("\\.+*?[^]$(){}=!<>|:");
        // *     returns 3: '\\\.\+\*\?\[\^\]\$\(\)\{\}\=\!\<\>\|\:'
        return (str + '').replace(new RegExp('[.\\\\+*?\\[\\^\\]$(){}=!<>|:\\' + (delimiter || '') + '-]', 'g'), '\\$&');
    }

    function getHintDisplayName(data, term) {

        var regexp = new RegExp("(" + preg_quote(term) + ")", 'gi');

        return data.replace(regexp, "<b>$1</b>");
    }

    function getSearchHintHtml(hint) {

        var html = '';

        html += '<a class="searchHint" href="' + LibraryBrowser.getHref(hint, null, '') + '">';

        var imgUrl;

        if (hint.PrimaryImageTag) {

            hint.ImageTags = { Primary: hint.PrimaryImageTag };

            imgUrl = ApiClient.getImageUrl(hint.ItemId, {
                type: "Primary",
                maxWidth: 150,
                maxHeight: 150,
                tag: hint.PrimaryImageTag
            });

        }
        else if (hint.MediaType == "Game") {

            imgUrl = "css/images/items/searchhintsv2/game.png";

        }
        else if (hint.Type == "Episode" || hint.Type == "Season" || hint.Type == "Series") {

            imgUrl = "css/images/items/searchhintsv2/tv.png";

        }
        else if (hint.Type == "Audio" || hint.Type == "MusicAlbum" || hint.Type == "MusicArtist") {

            imgUrl = "css/images/items/searchhintsv2/music.png";

        }
        else if (hint.Type == "Person") {

            imgUrl = "css/images/items/searchhintsv2/person.png";

        }
        else {

            imgUrl = "css/images/items/searchhintsv2/film.png";
        }

        html += '<img class="searchHintImage" src="' + imgUrl + '" />';

        html += '<div class="searchHintContent"><div class="searchHintContentInner">';

        html += '<div class="searchHintName">' + getHintDisplayName(hint.Name, hint.MatchedTerm) + '</div>';

        if (hint.Type == "Audio") {

            html += '<div class="searchHintSecondaryText">' + [hint.AlbumArtist, hint.Album].join(" - ") + '</div>';

        }
        else if (hint.Type == "MusicAlbum") {

            html += '<div class="searchHintSecondaryText">' + hint.AlbumArtist + '</div>';

        }
        else if (hint.Type == "MusicArtist") {

            html += '<div class="searchHintSecondaryText">' + Globalize.translate('LabelArtist') + '</div>';

        }
        else if (hint.Type == "Movie") {

            html += '<div class="searchHintSecondaryText">' + Globalize.translate('LabelMovie') + '</div>';

        }
        else if (hint.Type == "MusicVideo") {

            html += '<div class="searchHintSecondaryText">' + Globalize.translate('LabelMusicVideo') + '</div>';

        }
        else if (hint.Type == "Episode") {

            html += '<div class="searchHintSecondaryText">' + Globalize.translate('LabelEpisode') + '</div>';

        }
        else if (hint.Type == "Series") {

            html += '<div class="searchHintSecondaryText">' + Globalize.translate('LabelSeries') + '</div>';

        }
        else {
            html += '<div class="searchHintSecondaryText">' + (hint.Type) + '</div>';
        }

        var text;

        if (hint.EpisodeCount) {

            text = hint.EpisodeCount == 1 ? "1 Episode" : hint.EpisodeCount + " Episodes";

            html += '<div class="searchHintSecondaryText">' + text + '</div>';
        }

        else if (hint.SongCount) {

            text = hint.SongCount == 1 ? "1 Song" : hint.SongCount + " Songs";

            html += '<div class="searchHintSecondaryText">' + text + '</div>';
        }

        else if (hint.ProductionYear && hint.MediaType != "Audio" && hint.Type != "Episode") {
            html += '<div class="searchHintSecondaryText">' + hint.ProductionYear + '</div>';
        }

        else if (hint.RunTimeTicks) {
            html += '<div class="searchHintSecondaryText">' + Dashboard.getDisplayTime(hint.RunTimeTicks) + '</div>';
        }

        html += '</div></div>';


        html += '</a>';

        return html;
    }

    function renderSearchHintResult(page, hints) {

        var html = '';

        for (var i = 0, length = hints.length; i < length; i++) {
            html += getSearchHintHtml(hints[i]);
        }

        $('#searchHints', page).html(html);
    }

    function getSearchPanel(page) {

        var panel = $('#searchPanel', page);

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="searchPanel" class="searchPanel" data-position="right" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<h3>';
            html += Globalize.translate('HeaderSearch');
            html += '</h3>';

            html += '<input id="txtSearch" class="txtSearch" type="search" data-theme="a" />';

            html += '<div id="searchHints" class="searchHints"></div>';

            html += '</div>';

            $(page).append(html);

            panel = $('#searchPanel', page).panel({}).trigger('create');

            $('#txtSearch', panel).on("keyup", function (e) {

                // Down
                if (e.keyCode == 40) {

                    var first = $('.searchHint', panel)[0];

                    if (first) {
                        first.focus();
                    }

                    return false;
                }

            }).on("keyup", function (e) {

                if (e.keyCode != 40) {
                    var value = this.value;

                    updateSearchHints(panel, value);
                }

            });

            $('#searchHints', page).on("keydown", '.searchHint', function (e) {

                // Down
                if (e.keyCode == 40) {

                    var next = $(this).next()[0];

                    if (next) {
                        next.focus();
                    }
                    return false;
                }

                // Up
                if (e.keyCode == 38) {

                    var prev = $(this).prev()[0];

                    if (prev) {
                        prev.focus();
                    } else {
                        $('#txtSearch', page)[0].focus();
                    }
                    return false;
                }
            });
        }

        return panel;
    }

    function search() {

        var self = this;

        self.showSearchPanel = function (page) {

            var panel = getSearchPanel(page);

            $(panel).panel('toggle');

            $('#txtSearch').focus();
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
            shape: "square",
            lazy: true,
            overlayText: false,
            showTitle: true,
            centerText: true
        });
        $('.itemsContainer', elem).html(html).lazyChildren();
    }

    function requestSearchHintsForOverlay(elem, searchTerm) {

        var currentTimeout = searchHintTimeout;

        ApiClient.getSearchHints({ userId: Dashboard.getCurrentUserId(), searchTerm: searchTerm, limit: 30 }).done(function (result) {

            if (currentTimeout != searchHintTimeout) {
                return;
            }

            renderSearchResultsInOverlay(elem, result.SearchHints);
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

        }, 100);
    }

    function getSearchOverlay(createIfNeeded) {

        var elem = $('.searchResultsOverlay');

        if (createIfNeeded && !elem.length) {

            var html = '<div class="searchResultsOverlay ui-page-theme-b">';

            html += '<div class="searchResultsContainer"><div class="itemsContainer"></div></div></div>';

            elem = $(html).appendTo(document.body).hide().trigger('create');

            elem.createCardMenus();
        }

        return elem;
    }

    function onHeaderSearchChange(val) {

        if (val) {
            $('.btnCloseSearch').show();
            updateSearchOverlay(getSearchOverlay(true).fadeIn('fast'), val);

        } else {

            $('.btnCloseSearch').hide();
            updateSearchOverlay(getSearchOverlay(false).fadeOut('fast'), val);
        }
    }

    function bindSearchEvents() {

        $('.headerSearchInput').on("keyup", function (e) {

            // Down key
            if (e.keyCode == 40) {

                //var first = $('.card', panel)[0];

                //if (first) {
                //    first.focus();
                //}

                return false;

            } else {

                onHeaderSearchChange(this.value);
            }

        }).on("search", function (e) {

            if (!this.value) {

                onHeaderSearchChange('');
            }

        });

        $('.btnCloseSearch').on('click', closeSearchOverlay);
    }

    function closeSearchOverlay() {
        $('.headerSearchInput').val('');
        onHeaderSearchChange('');
    }

    $(document).on('pagehide', ".libraryPage", function () {

        $('#txtSearch', this).val('');
        $('#searchHints', this).empty();

    }).on('pagecontainerbeforehide', closeSearchOverlay);

    $(document).on('headercreated', function () {

        bindSearchEvents();
    });

})(jQuery, document, window, clearTimeout, setTimeout);