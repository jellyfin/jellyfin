var ItemListPage = {

    onPageShow: function () {

        ItemListPage.reload();
    },

    reload: function () {

        var parentId = getParameterByName('parentId');

        var query = {
            Fields: "PrimaryImageAspectRatio",
            SortBy: "SortName"
        };

        if (parentId) {
            query.parentId = parentId;

            ApiClient.getItem(Dashboard.getCurrentUserId(), parentId).done(ItemListPage.renderTitle);
        } else {
            $('#itemName', $.mobile.activePage).html("Media Library");
        }

        ItemListPage.refreshItems(query);
    },

    refreshItems: function (query) {


        var page = $.mobile.activePage;

        page.itemQuery = query;

        $('#btnSort', page).html(query.SortBy).button("refresh");

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(ItemListPage.renderItems);
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
    },

    sortBy: function (sortBy) {

        var query = $.mobile.activePage.itemQuery;
        query.SortBy = sortBy;
        ItemListPage.refreshItems(query);
    },

    sortOrder: function (order) {

        var query = $.mobile.activePage.itemQuery;
        query.SortOrder = order;
        ItemListPage.refreshItems(query);
    }
};

$(document).on('pageshow', "#itemListPage", ItemListPage.onPageShow);
