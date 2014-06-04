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

            imgUrl = ApiClient.getScaledImageUrl(item.ParentLogoItemId, {
                height: logoHeight,
                type: 'logo',
                tag: item.ParentLogoImageTag
            });

            html += '<img src="' + imgUrl + '" style="max-height:' + logoHeight + 'px;max-width:' + maxLogoWidth + 'px;" />';

        }
        else if (item.ImageTags.Logo) {

            imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                height: logoHeight,
                type: 'logo',
                tag: item.ImageTags.Logo
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

        if (MediaController.canPlay(item)) {

            var resumePosition = (item.UserData || {}).PlaybackPositionTicks || 0;
            var onPlayClick = 'LibraryBrowser.showPlayMenu(this, \'' + item.Id + '\', \'' + item.Type + '\', ' + item.IsFolder + ', \'' + item.MediaType + '\', ' + resumePosition + ');return false;';

            html += '<button type="button" data-mini="true" data-inline="true" data-icon="play" data-iconpos="notext" title="' + Globalize.translate('ButtonPlay') + '" onclick="' + onPlayClick + '" style="' + buttonMargin + '">' + Globalize.translate('ButtonPlay') + '</button>';
            buttonCount++;

            if (item.MediaType == "Audio" || item.Type == "MusicAlbum") {
                html += '<button type="button" data-mini="true" data-inline="true" data-icon="plus" data-iconpos="notext" title="' + Globalize.translate('ButtonQueue') + '" onclick="MediaController.queue(\'' + item.Id + '\');return false;" style="' + buttonMargin + '">' + Globalize.translate('ButtonQueue') + '</button>';
                buttonCount++;
            }
        }

        if (item.LocalTrailerCount && item.PlayAccess == 'Full') {
            html += '<button type="button" data-mini="true" data-inline="true" data-icon="video" data-iconpos="notext" class="btnPlayTrailer" data-itemid="' + item.Id + '" title="' + Globalize.translate('ButtonPlayTrailer') + '" style="' + buttonMargin + '">' + Globalize.translate('ButtonPlayTrailer') + '</button>';
            buttonCount++;
        }

        if (currentUser.Configuration.IsAdministrator && item.Type != "Recording" && item.Type != "Program") {
            html += '<button type="button" data-mini="true" data-inline="true" data-icon="edit" data-iconpos="notext" title="' + Globalize.translate('ButtonEdit') + '" onclick="Dashboard.navigate(\'edititemmetadata.html?id=' + item.Id + '\');return false;" style="' + buttonMargin + '">' + Globalize.translate('ButtonEdit') + '</button>';
            buttonCount++;
        }

        html += '</div>';

        html += '</div>';

        return html;
    }

    function onTrailerButtonClick() {

        var id = this.getAttribute('data-itemid');

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), id).done(function (trailers) {
            MediaController.play({ items: trailers });
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

        Dashboard.confirm(Globalize.translate('MessageConfirmSplitMedia'), Globalize.translate('HeaderSplitMedia'), function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                $.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl("Videos/" + id + "/AlternateSources")

                }).done(function () {

                    Dashboard.hideLoadingMsg();

                    $('.itemsContainer', page).trigger('needsrefresh');
                });
            }
        });

    }

    $.fn.createPosterItemMenus = function (options) {

        options = options || {};

        var preventHover = false;

        function onShowTimerExpired(elem) {

            if ($(elem).hasClass('hasContextMenu')) {
                return;
            }

            if ($('.itemSelectionPanel:visible', elem).length) {
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
            });

            innerElem.show().each(function () {

                this.style.height = 0;

            }).animate({ "height": "100%" }, "fast");
        }

        function onHoverIn() {

            if (preventHover === true) {
                preventHover = false;
                return;
            }

            if (showOverlayTimeout) {
                clearTimeout(showOverlayTimeout);
                showOverlayTimeout = null;
            }

            var elem = this;

            showOverlayTimeout = setTimeout(function () {

                onShowTimerExpired(elem);

            }, 1000);
        }

        function preventTouchHover() {
            preventHover = true;
        }

        var elems = '.backdropPosterItem,.smallBackdropPosterItem,.portraitPosterItem,.squarePosterItem';

        return this.off('.posterItemHoverMenu')
            .on('mouseenter.posterItemHoverMenu', elems, onHoverIn)
            .on('mouseleave.posterItemHoverMenu', elems, onHoverOut)
            .on("touchstart.posterItemHoverMenu", elems, preventTouchHover);
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
                message: Globalize.translate('MessagePleaseSelectTwoItems'),
                title: Globalize.translate('HeaderError')
            });

            return;
        }

        var names = $('.chkItemSelect:checked', page).parents('.posterItem').get().reverse().map(function (e) {

            return $('.posterItemText', e).html();

        }).join('<br/>');

        var msg = Globalize.translate('MessageTheFollowingItemsWillBeGrouped') + "<br/><br/>" + names;

        msg += "<br/><br/>" + Globalize.translate('MessageConfirmItemGrouping');

        Dashboard.confirm(msg, "Group Versions", function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                $.ajax({
                    
                    type: "POST",
                    url: ApiClient.getUrl("Videos/MergeVersions", { Ids: selection.join(',') })

                }).done(function () {

                    Dashboard.hideLoadingMsg();

                    hideSelections(page);

                    $('.itemsContainer', page).trigger('needsrefresh');
                });
            }
        });
    }
    
    function addToCollection(page) {
        
        var selection = getSelectedItems(page);

        if (selection.length < 1) {

            Dashboard.alert({
                message: Globalize.translate('MessagePleaseSelectOneItem'),
                title: Globalize.translate('HeaderError')
            });

            return;
        }

        BoxSetEditor.showPanel(page, selection);
    }

    $(document).on('pageinit', ".libraryPage", function () {

        var page = this;

        $('.btnToggleSelections', page).on('click', function () {
            toggleSelections(page);
        });

        $('.btnMergeVersions', page).on('click', function () {
            combineVersions(page);
        });

        $('.btnAddToCollection', page).on('click', function () {
            addToCollection(page);
        });

    }).on('pagebeforeshow', ".libraryPage", function () {

        var page = this;

        hideSelections(page);

    });

})(jQuery, document, window);