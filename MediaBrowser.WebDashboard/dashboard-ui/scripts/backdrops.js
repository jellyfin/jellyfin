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

        var key = 'backdrops_' + userId + (types || '') + (parentId || '');

        var deferred = $.Deferred();

        var data = localStorage.getItem(key);

        if (data) {

            console.log('Found backdrop id list in cache. Key: ' + key)
            data = JSON.parse(data);
            deferred.resolveWith(null, [data]);
        } else {

            var options = {

                SortBy: "Random",
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

                localStorage.setItem(key, JSON.stringify(images));
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

                var imgUrl = ApiClient.getImageUrl(item.id, {
                    type: "Backdrop",
                    tag: item.tag
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

    $(document).on('pagebeforeshow', ".backdropPage", function () {

        var page = this;

        // Gets real messy and jumps around the page when scrolling
        // Can be reviewed later.
        if ($.browser.msie) {
            $(page).removeClass('backdropPage');
        } else {
            var type = page.getAttribute('data-backdroptype');

            showBackdrop(type);
        }

    });

})(jQuery, document);