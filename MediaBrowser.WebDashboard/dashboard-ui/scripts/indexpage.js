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