(function ($, document, apiClient) {

    function createMediaLinks(options) {

        var html = "";

        var items = options.items;

        // "My Library" backgrounds
        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var imgUrl;

            switch (item.CollectionType) {
                case "movies":
                    imgUrl = "css/images/items/folders/movies.png";
                    break;
                case "music":
                    imgUrl = "css/images/items/folders/music.png";
                    break;
                case "photos":
                    imgUrl = "css/images/items/folders/photos.png";
                    break;
                case "livetv":
                case "tvshows":
                    imgUrl = "css/images/items/folders/tv.png";
                    break;
                case "games":
                    imgUrl = "css/images/items/folders/games.png";
                    break;
                case "trailers":
                    imgUrl = "css/images/items/folders/movies.png";
                    break;
                case "adultvideos":
                case "homevideos":
                    imgUrl = "css/images/items/folders/homevideos.png";
                    break;
                case "musicvideos":
                    imgUrl = "css/images/items/folders/musicvideos.png";
                    break;
                case "books":
                    imgUrl = "css/images/items/folders/books.png";
                    break;
                case "channels":
                    imgUrl = "css/images/items/folders/channels.png";
                    break;
                case "boxsets":
                default:
                    imgUrl = "css/images/items/folders/folder.png";
                    break;
            }

            var cssClass = "posterItem";
            cssClass += ' ' + options.shape + 'PosterItem';

            if (item.CollectionType) {
                cssClass += ' ' + item.CollectionType + 'PosterItem';
            }

            var href = item.url || LibraryBrowser.getHref(item, options.context);

            html += '<a data-itemid="' + item.Id + '" class="' + cssClass + '" href="' + href + '">';

            var style = "";

            if (imgUrl) {
                style += 'background-image:url(\'' + imgUrl + '\');';
            }

            var imageCssClass = 'posterItemImage';

            html += '<div class="' + imageCssClass + '" style="' + style + '">';
            html += '</div>';

            html += "<div class='posterItemDefaultText posterItemText'>";
            html += item.Name;
            html += "</div>";

            html += "</a>";
        }

        return html;
    }

    function getDefaultSection(index) {

        switch (index) {

            case 0:
                return 'librarybuttons';
            case 1:
                return 'resume';
            case 2:
                return 'latestmedia';
            default:
                return '';
        }

    }

    function loadlibraryButtons(elem, userId, index) {

        var options = {

            SortBy: "SortName",
            Fields: "PrimaryImageAspectRatio"
        };

        var promise1 = ApiClient.getItems(userId, options);

        var promise2 = ApiClient.getLiveTvInfo();

        var promise3 = $.getJSON(ApiClient.getUrl("Channels", {
            userId: userId,

            // We just want the total record count
            limit: 0
        }));

        $.when(promise1, promise2, promise3).done(function (r1, r2, r3) {

            var result = r1[0];
            var liveTvInfo = r2[0];
            var channelResponse = r3[0];

            if (channelResponse.TotalRecordCount) {

                result.Items.push({
                    Name: 'Channels',
                    CollectionType: 'channels',
                    Id: 'channels',
                    url: 'channels.html'
                });
            }

            var showLiveTv = liveTvInfo.EnabledUsers.indexOf(userId) != -1;

            if (showLiveTv) {

                result.Items.push({
                    Name: 'Live TV',
                    CollectionType: 'livetv',
                    Id: 'livetv',
                    url: 'livetvsuggested.html'
                });
            }

            var html = '<br/>';

            if (index) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderMyLibrary') + '</h1>';
            }
            html += '<div>';
            html += createMediaLinks({
                items: result.Items,
                shape: 'myLibrary',
                showTitle: true,
                centerText: true

            });
            html += '</div>';

            $(elem).html(html);

            handleLibraryLinkNavigations(elem);
        });
    }

    function loadRecentlyAdded(elem, userId) {

        var screenWidth = $(window).width();

        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            Limit: screenWidth >= 2400 ? 30 : (screenWidth >= 1920 ? 20 : (screenWidth >= 1440 ? 12 : (screenWidth >= 800 ? 12 : 8))),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsUnplayed,IsNotFolder",
            CollapseBoxSetItems: false,
            ExcludeLocationTypes: "Virtual,Remote"
        };

        ApiClient.getItems(userId, options).done(function (result) {

            var html = '';

            if (result.Items.length) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderLatestMedia') + '</h1>';
                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: true,
                    shape: 'backdrop',
                    showTitle: true,
                    centerText: true,
                    context: 'home',
                    lazy: true
                });
                html += '</div>';
            }


            $(elem).html(html).trigger('create').createPosterItemMenus();
        });
    }

    function loadLibraryTiles(elem, userId) {

        var options = {

            SortBy: "SortName",
            Fields: "PrimaryImageAspectRatio"
        };

        ApiClient.getItems(userId, options).done(function (result) {

            var html = '';

            if (result.Items.length) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderMyLibrary') + '</h1>';
                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: 'backdrop',
                    showTitle: true,
                    centerText: true,
                    lazy: true
                });
                html += '</div>';
            }


            $(elem).html(html).trigger('create').createPosterItemMenus();

            handleLibraryLinkNavigations(elem);
        });
    }

    function loadResume(elem, userId) {

        var screenWidth = $(window).width();

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            MediaTypes: "Video",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 10 : (screenWidth >= 1440 ? 8 : 6),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            CollapseBoxSetItems: false,
            ExcludeLocationTypes: "Virtual"
        };

        ApiClient.getItems(userId, options).done(function (result) {

            var html = '';

            if (result.Items.length) {
                html += '<h1 class="listHeader">'+Globalize.translate('HeaderResume')+'</h1>';
                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferBackdrop: true,
                    shape: 'backdrop',
                    overlayText: screenWidth >= 600,
                    showTitle: true,
                    showParentTitle: true,
                    context: 'home',
                    lazy: true
                });
                html += '</div>';
            }

            $(elem).html(html).trigger('create').createPosterItemMenus();
        });
    }

    function loadSection(page, userId, displayPreferences, index) {

        var section = displayPreferences.CustomPrefs['home' + index] || getDefaultSection(index);

        var elem = $('.section' + index, page);

        if (section == 'latestmedia') {
            loadRecentlyAdded(elem, userId);
        }
        else if (section == 'librarytiles') {
            loadLibraryTiles(elem, userId);
        }
        else if (section == 'resume') {
            loadResume(elem, userId);
        }
        else if (section == 'librarybuttons') {
            loadlibraryButtons(elem, userId, index);

        } else {

            elem.empty();
        }
    }

    function loadSections(page, userId, displayPreferences) {

        var i, length;
        var sectionCount = 3;

        var elem = $('.sections', page);

        if (!elem.html().length) {
            var html = '';
            for (i = 0, length = sectionCount; i < length; i++) {

                html += '<div class="homePageSection section' + i + '"></div>';
            }

            elem.html(html);
        }

        for (i = 0, length = sectionCount; i < length; i++) {

            loadSection(page, userId, displayPreferences, i);
        }
    }

    function handleLibraryLinkNavigations(elem) {

        $('a', elem).on('click', function () {

            var text = $('.posterItemText', this).html();

            LibraryMenu.setText(text);
        });
    }

    function dismissWelcome(page, userId) {

        ApiClient.getDisplayPreferences('home', userId, 'webclient').done(function (result) {

            result.CustomPrefs.homePageWelcomeDismissed = '1';
            ApiClient.updateDisplayPreferences('home', result, userId, 'webclient').done(function() {
                
                $('.welcomeMessage', page).hide();
                
            });
        });
    }

    $(document).on('pageinit', "#indexPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        $('.btnDismissWelcome', page).on('click', function () {
            dismissWelcome(page, userId);
        });

    }).on('pagebeforeshow', "#indexPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getDisplayPreferences('home', userId, 'webclient').done(function (result) {

            if (result.CustomPrefs.homePageWelcomeDismissed) {
                $('.welcomeMessage', page).hide();
            } else {
                $('.welcomeMessage', page).show();
            }
            
            loadSections(page, userId, result);
        });

    });

})(jQuery, document, ApiClient);