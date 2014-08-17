(function ($, document) {

    function getElement() {

        var elem = $('.backdropContainer');

        if (!elem.length) {

            elem = $('<div class="backdropContainer"></div>').prependTo(document.body);
        }

        return elem;
    }

    function getRandom(min, max) {
        return Math.floor(Math.random() * (max - min) + min);
    }

    function getBackdropItemIds(userId, types, parentId) {

        var key = 'backdrops2_' + userId + (types || '') + (parentId || '');

        var deferred = $.Deferred();

        var data = sessionStore.getItem(key);

        if (data) {

            console.log('Found backdrop id list in cache. Key: ' + key)
            data = JSON.parse(data);
            deferred.resolveWith(null, [data]);
        } else {

            var options = {

                SortBy: "IsFavoriteOrLiked,Random",
                Limit: 50,
                Recursive: true,
                IncludeItemTypes: types,
                ImageTypes: "Backdrop",
                //Ids: "8114409aa00a2722456c08e298f90bed",
                ParentId: parentId
            };

            ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

                var images = result.Items.map(function (i) {
                    return {
                        id: i.Id,
                        tag: i.BackdropImageTags[0]
                    };
                });

                sessionStore.setItem(key, JSON.stringify(images));
                deferred.resolveWith(null, [images]);
            });
        }

        return deferred.promise();
    }

    function showBackdrop(type) {

        getBackdropItemIds(Dashboard.getCurrentUserId(), type, LibraryMenu.getTopParentId()).done(function (images) {

            if (images.length) {

                var index = getRandom(0, images.length - 1);
                var item = images[index];

                var screenWidth = $(window).width();

                var imgUrl = ApiClient.getScaledImageUrl(item.id, {
                    type: "Backdrop",
                    tag: item.tag,
                    maxWidth: screenWidth,
                    quality: 80
                });

                getElement().css('backgroundImage', 'url(\'' + imgUrl + '\')');

            } else {

                clearBackdrop();
            }
        });
    }

    function clearBackdrop() {

        $('.backdropContainer').css('backgroundImage', '');
    }

    function enabled() {

        // Gets real messy and jumps around the page when scrolling
        // Can be reviewed later.
        if ($.browser.msie) {
            return false;
        }

        var userId = Dashboard.getCurrentUserId();

        var val = store.getItem('enableBackdrops-' + userId);

        // For bandwidth
        return val == '1' || (val != '0' && !$.browser.mobile);
    }

    function setBackdrops(page, items) {

        var images = items.filter(function (i) {

            return i.BackdropImageTags.length > 0;

        }).map(function (i) {
            return {
                id: i.Id,
                tag: i.BackdropImageTags[0]
            };
        });

        if (images.length) {
            $(page).addClass('backdropPage');

            var index = getRandom(0, images.length - 1);
            var item = images[index];

            var screenWidth = $(window).width();

            var imgUrl = ApiClient.getScaledImageUrl(item.id, {
                type: "Backdrop",
                tag: item.tag,
                maxWidth: screenWidth,
                quality: 80
            });

            getElement().css('backgroundImage', 'url(\'' + imgUrl + '\')');

        } else {
            $(page).removeClass('backdropPage');
        }
    }

    $(document).on('pagebeforeshow', ".page", function () {

        var page = this;

        if ($(page).hasClass('backdropPage')) {

            if (enabled()) {
                var type = page.getAttribute('data-backdroptype');

                showBackdrop(type);

            } else {
                $(page).removeClass('backdropPage');
                clearBackdrop();
            }
        } else {
            clearBackdrop();
        }

    });

    window.Backdrops = {

        setBackdrops: setBackdrops
    };

})(jQuery, document);