(function ($, document, apiClient) {

    function getUserViews(userId) {

        var deferred = $.Deferred();

        ApiClient.getUserViews(userId).done(function (result) {

            var items = result.Items;

            deferred.resolveWith(null, [items]);
        });

        return deferred.promise();
    }

    function getDefaultSection(index) {

        switch (index) {

            case 0:
                return 'smalllibrarytiles';
            case 1:
                return 'resume';
            case 2:
                return 'latestmedia';
            case 3:
                return '';
            default:
                return '';
        }

    }

    function loadRecentlyAdded(elem, userId) {

        var screenWidth = $(window).width();

        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            Limit: screenWidth >= 2400 ? 30 : (screenWidth >= 1920 ? 20 : (screenWidth >= 1440 ? 10 : (screenWidth >= 800 ? 9 : 8))),
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

    function loadLatestChannelMedia(elem, userId) {

        var screenWidth = $(window).width();

        var options = {

            Limit: screenWidth >= 2400 ? 10 : (screenWidth >= 1920 ? 10 : (screenWidth >= 1440 ? 8 : (screenWidth >= 800 ? 7 : 6))),
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsUnplayed",
            UserId: userId
        };

        $.getJSON(ApiClient.getUrl("Channels/Items/Latest", options)).done(function (result) {

            var html = '';

            if (result.Items.length) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderLatestChannelMedia') + '</h1>';
                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: true,
                    shape: 'auto',
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

    function loadLibraryTiles(elem, userId, shape, index) {

        getUserViews(userId).done(function (items) {

            var html = '';

            if (items.length) {

                var cssClass = index ? 'listHeader' : 'listHeader firstListHeader';

                html += '<h1 class="' + cssClass + '">' + Globalize.translate('HeaderMyLibrary') + '</h1>';

                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: shape,
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

    function loadLibraryFolders(elem, userId, shape, index) {

        ApiClient.getItems(userId, {

            SortBy: "SortName"

        }).done(function (result) {

            var html = '';
            var items = result.Items;

            for (var i = 0, length = items.length; i < length; i++) {
                items[i].url = 'itemlist.html?parentid=' + items[i].Id;
            }

            if (items.length) {

                html += '<h1 class="listHeader">' + Globalize.translate('HeaderLibraryFolders') + '</h1>';

                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: shape,
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
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderResume') + '</h1>';
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
            loadLibraryTiles(elem, userId, 'backdrop', index);
        }
        else if (section == 'smalllibrarytiles' || section == 'librarybuttons') {
            loadLibraryTiles(elem, userId, 'miniBackdrop', index);
        }
        else if (section == 'resume') {
            loadResume(elem, userId);
        }

        else if (section == 'folders') {
            loadLibraryFolders(elem, userId, 'backdrop', index);

        } else if (section == 'latestchannelmedia') {
            loadLatestChannelMedia(elem, userId);

        } else {

            elem.empty();
        }
    }

    function loadSections(page, userId, displayPreferences) {

        var i, length;
        var sectionCount = 4;

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

    var homePageDismissValue = '2';

    function dismissWelcome(page, userId) {

        ApiClient.getDisplayPreferences('home', userId, 'webclient').done(function (result) {

            result.CustomPrefs.homePageWelcomeDismissed = homePageDismissValue;
            ApiClient.updateDisplayPreferences('home', result, userId, 'webclient').done(function () {

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

            if (result.CustomPrefs.homePageWelcomeDismissed == homePageDismissValue) {
                $('.welcomeMessage', page).hide();
            } else {
                $('.welcomeMessage', page).show();
            }

            loadSections(page, userId, result);
        });

    });

})(jQuery, document, ApiClient);