(function ($, document) {

    var pageBackgroundCreated;

    function getElement() {

        //var elem = $('.backdropContainer');

        //if (!elem.length) {

        //    elem = $('<div class="backdropContainer"></div>').prependTo(document.body);
        //}

        var elem = document.documentElement;

        elem.classList.add('backdropContainer');
        elem.classList.add('noFade');

        if (!pageBackgroundCreated) {
            pageBackgroundCreated = true;
            var div = document.createElement('div');
            div.classList.add('pageBackground');
            document.body.insertBefore(div, document.body.firstChild);
        }

        return elem;
    }

    function clearBackdrop() {

        var elem = document.documentElement;
        elem.classList.remove('backdropContainer');
        elem.removeAttribute('data-url');
        elem.style.backgroundImage = '';
    }

    function getRandom(min, max) {
        return Math.floor(Math.random() * (max - min) + min);
    }

    function getBackdropItemIds(apiClient, userId, types, parentId) {

        var key = 'backdrops2_' + userId + (types || '') + (parentId || '');

        var deferred = $.Deferred();

        var data = sessionStore.getItem(key);

        if (data) {

            Logger.log('Found backdrop id list in cache. Key: ' + key)
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

            apiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

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

        if (url == elem.getAttribute('data-url')) {
            return;
        }

        elem.setAttribute('data-url', url);
        ImageLoader.lazyImage(elem, url);
    }

    function showBackdrop(type, parentId) {

        var apiClient = window.ApiClient;

        if (!apiClient) {
            return;
        }

        getBackdropItemIds(apiClient, Dashboard.getCurrentUserId(), type, parentId).then(function (images) {

            if (images.length) {

                var index = getRandom(0, images.length - 1);
                var item = images[index];

                var screenWidth = $(window).width();

                var imgUrl = apiClient.getScaledImageUrl(item.id, {
                    type: "Backdrop",
                    tag: item.tag,
                    maxWidth: screenWidth,
                    quality: 50
                });

                setBackdropImage(getElement(), imgUrl);

            } else {

                clearBackdrop();
            }
        });
    }

    function setDefault(page) {

        var elem = getElement();
        elem.style.backgroundImage = "url(css/images/splash.jpg)";
        elem.setAttribute('data-url', 'css/images/splash.jpg');
        page = $(page)[0];
        page.classList.add('backdropPage');
        page.classList.add('staticBackdropPage');
    }

    function isEnabledByDefault() {

        if (AppInfo.hasLowImageBandwidth) {

            return false;
        }

        return false;
    }

    function enabled() {

        var userId = Dashboard.getCurrentUserId();

        var val = appStorage.getItem('enableBackdrops-' + userId);

        // For bandwidth
        return val == '1' || (val != '0' && isEnabledByDefault());
    }

    function setBackdrops(page, items) {

        var images = items.map(function (i) {

            if (i.BackdropImageTags.length > 0) {
                return {
                    id: i.Id,
                    tag: i.BackdropImageTags[0]
                };
            }

            if (i.ParentBackdropItemId && i.ParentBackdropImageTags && i.ParentBackdropImageTags.length) {

                return {
                    id: i.ParentBackdropItemId,
                    tag: i.ParentBackdropImageTags[0]
                };
            }
            return null;

        }).filter(function (i) {
            return i != null;
        });

        if (images.length) {
            page.classList.add('backdropPage');

            var index = getRandom(0, images.length - 1);
            var item = images[index];

            var screenWidth = $(window).width();

            var imgUrl = ApiClient.getScaledImageUrl(item.id, {
                type: "Backdrop",
                tag: item.tag,
                maxWidth: screenWidth,
                quality: 50
            });

            setBackdropImage(getElement(), imgUrl);

        } else {
            page.classList.remove('backdropPage');
        }
    }

    function setBackdropUrl(page, url) {

        if (url) {
            page.classList.add('backdropPage');

            setBackdropImage(getElement(), url);

        } else {
            page.classList.remove('backdropPage');
            clearBackdrop();
        }
    }

    pageClassOn('pagebeforeshow', "page", function () {

        var page = this;

        if (page.classList.contains('backdropPage')) {

            if (enabled()) {
                var type = page.getAttribute('data-backdroptype');

                var parentId = page.classList.contains('globalBackdropPage') ? '' : LibraryMenu.getTopParentId();

                showBackdrop(type, parentId);

            } else {
                page.classList.remove('backdropPage');
                clearBackdrop();
            }
        } else {
            clearBackdrop();
        }

    });

    window.Backdrops = {

        setBackdrops: setBackdrops,
        setBackdropUrl: setBackdropUrl,
        setDefault: setDefault,
        clear: clearBackdrop
    };

})(jQuery, document);