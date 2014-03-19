(function ($, document, window) {

    var showOverlayTimeout;

    function onHoverOut() {

        if (showOverlayTimeout) {
            clearTimeout(showOverlayTimeout);
            showOverlayTimeout = null;
        }

        $('.posterItemOverlayTarget:visible', this).each(function () {

            var elem = this;

            $(this).animate({ "height": "0" }, "fast", function () {

                $(elem).hide();

            });

        });

        $('.posterItemOverlayTarget:visible', this).stop().animate({ "height": "0" }, function () {

            $(this).hide();

        });
    }

    function getOverlayHtml(item, currentUser, posterItem) {

        var html = '';

        html += '<div class="posterItemOverlayInner">';

        var isSmallItem = $(posterItem).hasClass('smallBackdropPosterItem');
        var isPortrait = $(posterItem).hasClass('portraitPosterItem');
        var isSquare = $(posterItem).hasClass('squarePosterItem');

        var parentName = isSmallItem || isPortrait ? null : item.SeriesName;
        var name = LibraryBrowser.getPosterViewDisplayName(item, true);

        html += '<div style="font-weight:bold;margin-bottom:1em;">';
        var logoHeight = isSmallItem ? 20 : 26;
        var maxLogoWidth = isPortrait ? 100 : 200;
        var imgUrl;

        if (parentName && item.ParentLogoItemId) {

            imgUrl = ApiClient.getImageUrl(item.ParentLogoItemId, {
                height: logoHeight * 2,
                type: 'logo',
                tag: item.ParentLogoImageTag
            });

            html += '<img src="' + imgUrl + '" style="max-height:' + logoHeight + 'px;max-width:' + maxLogoWidth + 'px;" />';

        }
        else if (item.ImageTags.Logo) {

            imgUrl = LibraryBrowser.getImageUrl(item, 'Logo', 0, {
                height: logoHeight * 2,
            });

            html += '<img src="' + imgUrl + '" style="max-height:' + logoHeight + 'px;max-width:' + maxLogoWidth + 'px;" />';
        }
        else {
            html += parentName || name;
        }
        html += '</div>';

        if (parentName) {
            html += '<p>';
            html += name;
            html += '</p>';
        } else if (!isSmallItem) {
            html += '<p class="itemMiscInfo" style="white-space:nowrap;">';
            html += LibraryBrowser.getMiscInfoHtml(item);
            html += '</p>';
        }

        html += '<div style="margin:1.25em 0;">';
        html += '<span class="itemCommunityRating">';
        html += LibraryBrowser.getRatingHtml(item, false);
        html += '</span>';

        if (isPortrait) {
            html += '<span class="userDataIcons" style="display:block;margin:1.25em 0;">';
            html += LibraryBrowser.getUserDataIconsHtml(item);
            html += '</span>';
        } else {
            html += '<span class="userDataIcons">';
            html += LibraryBrowser.getUserDataIconsHtml(item);
            html += '</span>';
        }
        html += '</div>';

        html += '<div>';

        var buttonMargin = isPortrait || isSquare ? "margin:0 4px 0 0;" : "margin:0 10px 0 0;";

        var buttonCount = 0;

        if (MediaPlayer.canPlay(item, currentUser)) {

            var resumePosition = (item.UserData || {}).PlaybackPositionTicks || 0;
            var onPlayClick = 'LibraryBrowser.showPlayMenu(this, \'' + item.Id + '\', \'' + item.Type + '\', \'' + item.MediaType + '\', ' + resumePosition + ');return false;';

            html += '<button type="button" data-mini="true" data-inline="true" data-icon="play" data-iconpos="notext" title="Play" onclick="' + onPlayClick + '" style="' + buttonMargin + '">Play</button>';
            buttonCount++;

            if (item.MediaType == "Audio" || item.Type == "MusicAlbum") {
                html += '<button type="button" data-mini="true" data-inline="true" data-icon="plus" data-iconpos="notext" title="Queue" onclick="MediaPlayer.queue(\'' + item.Id + '\');return false;" style="' + buttonMargin + '">Queue</button>';
                buttonCount++;
            }
        }

        if (item.LocalTrailerCount && item.PlayAccess == 'Full') {
            html += '<button type="button" data-mini="true" data-inline="true" data-icon="video" data-iconpos="notext" class="btnPlayTrailer" data-itemid="' + item.Id + '" title="Play Trailer" style="' + buttonMargin + '">Play Trailer</button>';
            buttonCount++;
        }

        if (currentUser.Configuration.IsAdministrator && item.Type != "Recording" && item.Type != "Program") {
            html += '<button type="button" data-mini="true" data-inline="true" data-icon="edit" data-iconpos="notext" title="Edit" onclick="Dashboard.navigate(\'edititemmetadata.html?id=' + item.Id + '\');return false;" style="' + buttonMargin + '">Edit</button>';
            buttonCount++;
        }

        if (!isPortrait || buttonCount < 3) {
            html += '<button type="button" data-mini="true" data-inline="true" data-icon="wireless" data-iconpos="notext" title="Remote" class="btnRemoteControl" data-itemid="' + item.Id + '" style="' + buttonMargin + '">Remote</button>';
        }

        html += '</div>';

        html += '</div>';

        return html;
    }

    function onTrailerButtonClick() {

        var id = this.getAttribute('data-itemid');

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), id).done(function (trailers) {
            MediaPlayer.play(trailers);
        });

        return false;
    }

    function onRemoteControlButtonClick() {

        var id = this.getAttribute('data-itemid');

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            RemoteControl.showMenuForItem({
                item: item
            });

        });

        return false;
    }

    function onMenuCommand(command, elem) {

        var id = elem.getAttribute('data-itemid');
        var page = $(elem).parents('.page');

        if (command == 'SplitVersions') {
            splitVersions(id, page);
        }
    }

    function splitVersions(id, page) {

        Dashboard.showLoadingMsg();

        $.ajax({
            type: "DELETE",
            url: ApiClient.getUrl("Videos/" + id + "/AlternateVersions")

        }).done(function () {

            Dashboard.hideLoadingMsg();

            $('.itemsContainer', page).trigger('needsrefresh');
        });
    }

    function getContextMenuOptions(elem) {

        var items = [];

        var id = elem.getAttribute('data-itemid');

        items.push({ type: 'header', text: 'Edit' });

        items.push({ type: 'link', text: 'Details', url: 'edititemmetadata.html?id=' + id });

        items.push({ type: 'link', text: 'Images', url: 'edititemimages.html?id=' + id });

        if (elem.getAttribute('data-alternateversioncount') != '0') {

            items.push({ type: 'divider' });
            items.push({ type: 'header', text: 'Manage' });
            items.push({ type: 'command', text: 'Split Versions', name: 'SplitVersions' });
        }

        return items;
    }

    $.fn.createPosterItemMenus = function (options) {

        options = options || {};

        function onShowTimerExpired(elem) {

            if ($(elem).hasClass('hasContextMenu')) {
                return;
            }

            if ($('.itemSelectionPanel', elem).length) {
                return;
            }

            var innerElem = $('.posterItemOverlayTarget', elem);
            var id = elem.getAttribute('data-itemid');

            var promise1 = ApiClient.getItem(Dashboard.getCurrentUserId(), id);
            var promise2 = Dashboard.getCurrentUser();

            $.when(promise1, promise2).done(function (response1, response2) {

                var item = response1[0];
                var user = response2[0];

                innerElem.html(getOverlayHtml(item, user, elem)).trigger('create');

                $('.btnPlayTrailer', innerElem).on('click', onTrailerButtonClick);
                $('.btnRemoteControl', innerElem).on('click', onRemoteControlButtonClick);
            });

            innerElem.show().each(function () {

                this.style.height = 0;

            }).animate({ "height": "100%" }, "fast");
        }

        function onHoverIn() {

            if (showOverlayTimeout) {
                clearTimeout(showOverlayTimeout);
                showOverlayTimeout = null;
            }

            var elem = this;

            showOverlayTimeout = setTimeout(function () {

                onShowTimerExpired(elem);

            }, 1000);
        }

        // https://hacks.mozilla.org/2013/04/detecting-touch-its-the-why-not-the-how/

        if (('ontouchstart' in window) || (navigator.maxTouchPoints > 0) || (navigator.msMaxTouchPoints > 0)) {
            /* browser with either Touch Events of Pointer Events
               running on touch-capable device */
            return this;
        }

        var sequence = this;

        if (options.contextMenu !== false) {
            Dashboard.getCurrentUser().done(function (user) {

                if (user.Configuration.IsAdministrator) {

                    sequence.createContextMenu({
                        getOptions: getContextMenuOptions,
                        command: onMenuCommand,
                        selector: '.posterItem'
                    });
                }

            });
        }

        return this.on('mouseenter', '.backdropPosterItem,.smallBackdropPosterItem,.portraitPosterItem,.squarePosterItem', onHoverIn)
            .on('mouseleave', '.backdropPosterItem,.smallBackdropPosterItem,.portraitPosterItem,.squarePosterItem', onHoverOut);
    };

    function toggleSelections(page) {

        Dashboard.showLoadingMsg();

        var selectionCommands = $('.selectionCommands', page);

        if (selectionCommands.is(':visible')) {

            selectionCommands.hide();
            $('.itemSelectionPanel', page).hide();

        } else {

            selectionCommands.show();

            $('.itemSelectionPanel', page).show();

            $('.chkItemSelect:checked', page).checked(false).checkboxradio('refresh');
        }

        Dashboard.hideLoadingMsg();
    }

    function hideSelections(page) {

        $('.selectionCommands', page).hide();

        $('.itemSelectionPanel', page).hide();
    }

    function getSelectedItems(page) {

        var selection = $('.chkItemSelect:checked', page);

        return selection.parents('.posterItem')
            .map(function () {

                return this.getAttribute('data-itemid');

            }).get();
    }

    function combineVersions(page) {

        var selection = getSelectedItems(page);

        if (selection.length < 2) {

            Dashboard.alert({
                message: "Please select two or more items to combine.",
                title: "Error"
            });

            return;
        }

        var names = $('.chkItemSelect:checked', page).parents('.posterItem').get().reverse().map(function (e) {

            return $('.posterItemText', e).html();

        }).join('<br/>');

        var msg = "The following titles will be grouped into one item:<br/><br/>" + names;

        msg += "<br/><br/>Media Browser clients will choose the optimal version to play based on device and network conditions. Are you sure you wish to continue?";

        Dashboard.confirm(msg, "Group Versions", function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                $.ajax({
                    type: "POST",
                    url: ApiClient.getUrl("Videos/MergeVersions", { Ids: selection.join(',') })

                }).done(function () {

                    Dashboard.hideLoadingMsg();

                    hideSelections();

                    $('.itemsContainer', page).trigger('needsrefresh');
                });
            }
        });
    }

    $(document).on('pageinit', ".libraryPage", function () {

        var page = this;

        $('.btnToggleSelections', page).on('click', function () {
            toggleSelections(page);
        });

        $('.itemsContainer', page).on('listrender', function () {
            hideSelections(page);
        });

        $('.btnMergeVersions', page).on('click', function () {
            combineVersions(page);
        });

    });

})(jQuery, document, window);