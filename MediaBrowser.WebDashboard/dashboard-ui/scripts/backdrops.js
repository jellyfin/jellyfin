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

    function getBackdropItemIds(apiClient, userId, types, parentId) {

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
                Limit: 20,
                Recursive: true,
                IncludeItemTypes: types,
                ImageTypes: "Backdrop",
                //Ids: "8114409aa00a2722456c08e298f90bed",
                ParentId: parentId
            };

            apiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

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

    function setBackdropImage(elem, url) {

        elem.lazyImage(url);
    }

    function showBackdrop(type, parentId) {

        var apiClient = window.ApiClient;

        if (!apiClient) {
            return;
        }

        getBackdropItemIds(apiClient, Dashboard.getCurrentUserId(), type, parentId).done(function (images) {

            if (images.length) {

                var index = getRandom(0, images.length - 1);
                var item = images[index];

                var screenWidth = $(window).width();

                var imgUrl = apiClient.getScaledImageUrl(item.id, {
                    type: "Backdrop",
                    tag: item.tag,
                    maxWidth: screenWidth,
                    quality: 80
                });

                setBackdropImage(getElement(), imgUrl);

            } else {

                clearBackdrop();
            }
        });
    }

    function setDefault(page) {
        
        var backdropContainer = $('.backdropContainer');

        if (backdropContainer.length) {
            backdropContainer.css('backgroundImage', 'url(css/images/splash.jpg)');
        } else {
            $(document.body).prepend('<div class="backdropContainer" style="background-image:url(css/images/splash.jpg);top:0;"></div>');
        }

        $(page).addClass('backdropPage staticBackdropPage');
    }

    function clearBackdrop() {

        $('.backdropContainer').css('backgroundImage', '');
    }

    function isEnabledByDefault() {

        if (AppInfo.hasLowImageBandwidth) {

            return false;
        }

        // It flickers too much in IE
        if ($.browser.msie) {

            return false;
        }

        if (!$.browser.mobile) {
            return true;
        }

        var screenWidth = $(window).width();

        return screenWidth >= 600;
    }

    function enabled() {

        var userId = Dashboard.getCurrentUserId();

        var val = appStorage.getItem('enableBackdrops-' + userId);

        // For bandwidth
        return val == '1' || (val != '0' && isEnabledByDefault());
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

            setBackdropImage(getElement(), imgUrl);

        } else {
            $(page).removeClass('backdropPage');
        }
    }
    
    function setBackdropUrl(page, url) {

        if (url) {
            $(page).addClass('backdropPage');

            setBackdropImage(getElement(), url);

        } else {
            $(page).removeClass('backdropPage');
            clearBackdrop();
        }
    }

    $(document).on('pagebeforeshowready', ".page", function () {

        var page = this;

        var $page = $(page);

        if (!$page.hasClass('staticBackdropPage')) {

            if ($page.hasClass('backdropPage')) {

                if (enabled()) {
                    var type = page.getAttribute('data-backdroptype');

                    var parentId = $page.hasClass('globalBackdropPage') ? '' : LibraryMenu.getTopParentId();

                    showBackdrop(type, parentId);

                } else {
                    $page.removeClass('backdropPage');
                    clearBackdrop();
                }
            } else {
                clearBackdrop();
            }
        }

    });

    window.Backdrops = {

        setBackdrops: setBackdrops,
        setBackdropUrl: setBackdropUrl,
        setDefault: setDefault
    };

})(jQuery, document);