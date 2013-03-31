var IndexPage = {

    onPageShow: function () {
        IndexPage.loadLibrary(Dashboard.getCurrentUserId(), this);
    },

    loadLibrary: function (userId, page) {

        if (!userId) {
            return;
        }

        page = $(page);

        var options = {

            limit: 6,
            sortBy: "DateCreated",
            sortOrder: "Descending",
            filters: "IsRecentlyAdded,IsNotFolder",
            ImageTypes: "Primary,Backdrop,Thumb",
            recursive: true
        };

        ApiClient.getItems(userId, options).done(function (result) {

            $('#divWhatsNew', page).html(Dashboard.getPosterViewHtml({
                items: result.Items,
                preferBackdrop: true,
                showTitle: true
            }));

        });

        options = {

            limit: 6,
            sortBy: "DatePlayed",
            sortOrder: "Descending",
            filters: "IsResumable",
            recursive: true
        };

        ApiClient.getItems(userId, options).done(function (result) {

            $('#divResumableItems', page).html(Dashboard.getPosterViewHtml({
                items: result.Items,
                preferBackdrop: true,
                showTitle: true
            }));

            if (result.Items.length) {
                $('#divResumable', page).show();
            } else {
                $('#divResumable', page).hide();
            }

        });

        options = {

            sortBy: "SortName"
        };

        ApiClient.getItems(userId, options).done(function (result) {

            $('#divCollections', page).html(Dashboard.getPosterViewHtml({
                items: result.Items,
                showTitle: true
            }));

        });
    }
};

$(document).on('pageshow', "#indexPage", IndexPage.onPageShow);