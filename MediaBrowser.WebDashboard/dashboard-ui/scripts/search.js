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

        var context;

        if (hint.Type == "Episode" || hint.Type == "Season" || hint.Type == "Series") {
            context = "tv";
        }
        else if (hint.Type == "Game" || hint.Type == "GameSystem") {
            context = "games";
        }
        else if (hint.Type == "MusicArtist" || hint.Type == "MusicAlbum") {
            context = "music";
        }
        else if (hint.Type == "Movie" || hint.Type == "BoxSet" || hint.Type == "Trailer") {
            context = "movie";
        }

        html += '<a class="searchHint" href="' + LibraryBrowser.getHref(hint, context) + '">';

        var imgUrl;

        if (hint.PrimaryImageTag) {

            hint.ImageTags = { Primary: hint.PrimaryImageTag };
            imgUrl = LibraryBrowser.getImageUrl(hint, "Primary", 0, { maxwidth: 150, maxheight: 150 });

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

            html += '<div class="searchHintSecondaryText">Artist</div>';

        }
        else if (hint.Type == "Movie") {

            html += '<div class="searchHintSecondaryText">Movie</div>';

        }
        else if (hint.Type == "MusicVideo") {

            html += '<div class="searchHintSecondaryText">Music Video</div>';

        }
        else if (hint.Type == "Episode") {

            html += '<div class="searchHintSecondaryText">Episode</div>';

        }
        else if (hint.Type == "Series") {

            html += '<div class="searchHintSecondaryText">Series</div>';

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
            html += 'Search';
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
        }

        return panel;
    }

    function search() {

        var self = this;

        self.showSearchPanel = function (page) {

            var panel = getSearchPanel(page);

            $(panel).panel('toggle');
        };

        self.onSearchRendered = function (parentElem) {

            $('#searchHints', parentElem).on("keydown", '.searchHint', function (e) {

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
                        $('#txtSearch', parentElem)[0].focus();
                    }
                    return false;
                }
            });

        };
    }

    window.Search = new search();

    $(document).on('pagehide', ".libraryPage", function () {

        $('#txtSearch', this).val('');
    });


})(jQuery, document, window, clearTimeout, setTimeout);