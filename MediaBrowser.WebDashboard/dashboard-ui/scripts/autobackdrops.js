define(['backdrop', 'appStorage'], function (backdrop, appStorage) {

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

    var cache = {};

    function getBackdropItemIds(apiClient, userId, types, parentId) {

        var key = 'backdrops2_' + userId + (types || '') + (parentId || '');

        var data = cache[key];

        if (data) {

            console.log('Found backdrop id list in cache. Key: ' + key)
            data = JSON.parse(data);
            return Promise.resolve(data);
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

            return apiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

                var images = result.Items.map(function (i) {
                    return {
                        Id: i.Id,
                        tag: i.BackdropImageTags[0],
                        ServerId: i.ServerId
                    };
                });

                cache[key] = JSON.stringify(images);
                return images;
            });
        }
    }

    function showBackdrop(type, parentId) {

        var apiClient = window.ApiClient;

        if (!apiClient) {
            return;
        }

        getBackdropItemIds(apiClient, Dashboard.getCurrentUserId(), type, parentId).then(function (images) {

            if (images.length) {

                backdrop.setBackdrops(images.map(function (i) {
                    i.BackdropImageTags = [i.tag];
                    return i;
                }));

            } else {

                backdrop.clear();
            }
        });
    }

    pageClassOn('pagebeforeshow', "page", function () {

        var page = this;

        // These pages self-manage their backdrops
        if (page.classList.contains('selfBackdropPage')) {
            return;
        }

        if (page.classList.contains('backdropPage')) {

            if (enabled()) {
                var type = page.getAttribute('data-backdroptype');

                var parentId = page.classList.contains('globalBackdropPage') ? '' : LibraryMenu.getTopParentId();
                showBackdrop(type, parentId);

            } else {
                page.classList.remove('backdropPage');
                backdrop.clear();
            }
        } else {
            backdrop.clear();
        }

    });
});