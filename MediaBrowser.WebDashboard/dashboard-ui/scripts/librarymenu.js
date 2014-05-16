(function (window, document, $) {

    function renderHeader(page, user) {

        var html = '<div class="viewMenuBar ui-bar-b">';

        html += '<button type="button" data-role="none" title="Menu" onclick="LibraryMenu.showLibraryMenu();" class="headerButton libraryMenuButton headerButtonLeft"><img src="css/images/menu.png" /></button>';

        html += '<a class="desktopHomeLink headerButton headerButtonLeft" href="index.html"><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></a>';

        html += '<div class="viewMenuSecondary">';

        html += '<a href="nowplaying.html" class="headerButton headerButtonRight headerRemoteButton"><img src="css/images/remote.png" /></a>';

        html += '<button id="btnCast" class="btnCast btnDefaultCast headerButton headerButtonRight" type="button" data-role="none"><div class="btnCastImage"></div></button>';

        html += '<button onclick="Search.showSearchPanel($.mobile.activePage);" type="button" data-role="none" class="headerButton headerButtonRight headerSearchButton"><img src="css/images/headersearch.png" /></button>';

        if (user.Configuration.IsAdministrator) {
            html += '<a href="dashboard.html" class="headerButton headerButtonRight headerSettingsButton"><img src="css/images/items/folders/settings.png" /></a>';
        }

        html += '<a class="headerButton headerButtonRight" href="#" onclick="Dashboard.showUserFlyout(this);">';

        if (user.PrimaryImageTag) {

            var url = ApiClient.getUserImageUrl(user.Id, {
                height: 18,
                tag: user.PrimaryImageTag,
                type: "Primary"
            });

            html += '<img src="' + url + '" />';
        } else {
            html += '<img src="css/images/currentuserdefaultwhite.png" />';
        }

        html += '</a>';

        html += '</div>';

        html += '</div>';

        var $page = $(page);

        $page.prepend(html);

        $page.trigger('headercreated');
    }

    function getItemHref(item) {

        return LibraryBrowser.getHref(item);
    }

    function getViewsHtml() {

        var html = '';

        html += '<div class="libraryMenuOptions">';
        html += '</div>';

        html += '<div class="libraryMenuDivider secondaryDivider" style="display:none;"></div>';

        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder channelsViewMenu channelsMenuOption" style="display:none;" data-itemid="channels" href="channels.html">Channels</a>';

        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder tvshowsViewMenu liveTvMenuOption" style="display:none;" data-itemid="livetv" href="livetvsuggested.html">Live TV</a>';

        html += '<div class="adminMenuOptions">';
        html += '<div class="libraryMenuDivider"></div>';
        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder dashboardViewMenu" data-itemid="dashboard" href="dashboard.html">Dashboard</a>';
        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder editorViewMenu" data-itemid="editor" href="edititemmetadata.html">Metadata Manager</a>';
        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder reportsViewMenu" data-itemid="reports" href="reports.html">Reports</a>';
        html += '</div>';

        return html;
    }

    function showLibraryMenu() {

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getItems(userId, {

            SortBy: "SortName"

        }).done(function (result) {

            var items = result.Items;

            var html = items.map(function (i) {

                var viewMenuCssClass = (i.CollectionType || 'general') + 'ViewMenu';

                return '<a data-itemid="' + i.Id + '" class="lnkMediaFolder viewMenuLink viewMenuTextLink ' + viewMenuCssClass + '" href="' + getItemHref(i) + '">' + i.Name + '</a>';

            }).join('');

            $('.libraryMenuOptions').html(html);
        });

        var page = $.mobile.activePage;

        var panel = getLibraryMenu();

        updateLibraryNavLinks(page);

        $(panel).panel('toggle');

        ApiClient.getLiveTvInfo().done(function (liveTvInfo) {

            var showLiveTv = liveTvInfo.EnabledUsers.indexOf(userId) != -1;

            if (showLiveTv) {
                $('.liveTvMenuOption').show();
                $('.secondaryDivider').show();
            }
        });

        $.getJSON(ApiClient.getUrl("Channels", {
            userId: userId,

            // We just want the total record count
            limit: 0

        })).done(function (response) {

            if (response.TotalRecordCount) {
                $('.channelsMenuOption').show();
                $('.secondaryDivider').show();
            }

        });

        Dashboard.getCurrentUser().done(function (user) {

            if (user.Configuration.IsAdministrator) {
                $('.adminMenuOptions').show();
            } else {
                $('.adminMenuOptions').hide();
            }
        });
    }

    function getLibraryMenu(user, channelCount, items, liveTvInfo) {

        var panel = $('#libraryPanel');

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="libraryPanel" class="libraryPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<p class="libraryPanelHeader"><a href="index.html" class="imageLink"><img src="css/images/mblogoicon.png" /><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></a></p>';

            html += '<div style="margin: 0 -1em;">';
            html += getViewsHtml(user, channelCount, items, liveTvInfo);
            html += '</div>';

            html += '</div>';

            $(document.body).append(html);

            panel = $('#libraryPanel').panel({}).trigger('create');
        }

        return panel;
    }

    function getTopParentId() {

        return getParameterByName('topParentId') || sessionStorage.getItem('topParentId') || null;
    }

    window.LibraryMenu = {
        showLibraryMenu: showLibraryMenu,

        getTopParentId: getTopParentId
    };

    function updateCastIcon() {

        var info = MediaController.getPlayerInfo();

        if (info.isLocalPlayer) {

            $('.btnCast').addClass('btnDefaultCast').removeClass('btnActiveCast');

        } else {

            $('.btnCast').removeClass('btnDefaultCast').addClass('btnActiveCast');
        }
    }

    function updateLibraryNavLinks(page) {

        page = $(page);

        var isLiveTvPage = page.hasClass('liveTvPage');
        var isChannelsPage = page.hasClass('channelsPage');
        var isEditorPage = page.hasClass('metadataEditorPage');
        var isReportsPage = page.hasClass('reportsPage');

        var id = isLiveTvPage || isChannelsPage || isEditorPage || isReportsPage || page.hasClass('allLibraryPage') ?
            '' :
            getTopParentId() || '';

        sessionStorage.setItem('topParentId', id);

        $('.lnkMediaFolder').each(function () {

            var itemId = this.getAttribute('data-itemid');

            if (isChannelsPage && itemId == 'channels') {
                $(this).addClass('selectedMediaFolder');
            }
            else if (isLiveTvPage && itemId == 'livetv') {
                $(this).addClass('selectedMediaFolder');
            }
            else if (isEditorPage && itemId == 'editor') {
                $(this).addClass('selectedMediaFolder');
            }
            else if (isReportsPage && itemId == 'reports') {
                $(this).addClass('selectedMediaFolder');
            }
            else if (id && itemId == id) {
                $(this).addClass('selectedMediaFolder');
            }
            else {
                $(this).removeClass('selectedMediaFolder');
            }

        });

        $('.scopedLibraryViewNav a', page).each(function () {

            var src = this.href;

            if (src.indexOf('#') != -1) {
                return;
            }

            src = replaceQueryString(src, 'topParentId', id);

            this.href = src;
        });
    }

    $(document).on('pageinit', ".libraryPage", function () {

        var page = this;

        $('.libraryViewNav', page).wrapInner('<div class="libraryViewNavInner"></div>');

        $('.libraryViewNav a', page).each(function () {

            this.innerHTML = '<span class="libraryViewNavLinkContent">' + this.innerHTML + '</span>';

        });

    }).on('pagebeforeshow', ".libraryPage", function () {

        var page = this;

        updateLibraryNavLinks(page);

        if (!$('.viewMenuBar', page).length) {

            Dashboard.getCurrentUser().done(function (user) {

                renderHeader(page, user);

                updateCastIcon();

                updateLibraryNavLinks(page);
            });
        }

    }).on('pageshow', ".libraryPage", function () {

        var page = this;

        var elem = $('.libraryViewNavInner .ui-btn-active:visible', page);

        if (elem.length) {
            elem[0].scrollIntoView();

            // Scroll back up so in case vertical scroll was messed with
            $(document).scrollTop(0);
        }


    });

    $(function () {

        $(MediaController).on('playerchange', function () {
            updateCastIcon();
        });

    });

})(window, document, jQuery);