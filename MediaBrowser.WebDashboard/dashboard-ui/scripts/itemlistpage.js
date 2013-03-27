var ItemListPage = {

    onPageShow: function () {

        ItemListPage.reload();
    },

    reload: function () {

        var userId = Dashboard.getCurrentUserId();

        var parentId = getParameterByName('parentId');

        var query = {
            SortBy: "SortName",
            
            Fields: "PrimaryImageAspectRatio"
        };

        if (parentId) {
            query.parentId = parentId;

            ApiClient.getItem(userId, parentId).done(ItemListPage.renderTitle);
        }

        ApiClient.getItems(userId, query).done(ItemListPage.renderItems);
    },

    renderItems: function (result) {

        var items = result.Items;

        var renderOptions = {

            items: items,
            useAverageAspectRatio: true
        };

        var html = Dashboard.getPosterViewHtml(renderOptions);

        $('#listItems', $.mobile.activePage).html(html);
    },

    renderTitle: function (item) {


        $('#itemName', $.mobile.activePage).html(item.Name);
    }
};

$(document).on('pageshow', "#itemListPage", ItemListPage.onPageShow);
