var ItemListPage = {

    onPageShow: function () {

        ItemListPage.reload();
    },

    reload: function () {

        var page = $.mobile.activePage;

        var parentId = getParameterByName('parentId');

        var query = {
            Fields: "PrimaryImageAspectRatio",
            Recursive: getParameterByName('Recursive') == 'true'
        };

        var filters = [];

        if (getParameterByName('IsResumable') == 'true') {
            filters.push("IsResumable");
            $('#chkResumable', page).checked(true).checkboxradio("refresh");
        }

        if (getParameterByName('IsFavorite') == 'true') {
            filters.push("IsFavorite");
            $('#chkIsFavorite', page).checked(true).checkboxradio("refresh");
        }

        if (getParameterByName('IsRecentlyAdded') == 'true') {
            filters.push("IsRecentlyAdded");
            $('#chkRecentlyAdded', page).checked(true).checkboxradio("refresh");
        }

        var sortBy = getParameterByName('SortBy') || 'SortName';
        query.SortBy = sortBy;
        $('.radioSortBy', page).checked(false).checkboxradio("refresh");
        $('#radio' + sortBy, page).checked(true).checkboxradio("refresh");

        var order = getParameterByName('SortOrder') || 'Ascending';

        query.SortOrder = order;
        $('.radioSortOrder', page).checked(false).checkboxradio("refresh");
        $('#radio' + order, page).checked(true).checkboxradio("refresh");

        query.Filters = filters.join(',');
        //query.limit = 100;

        if (parentId) {
            query.parentId = parentId;

            ApiClient.getItem(Dashboard.getCurrentUserId(), parentId).done(ItemListPage.renderTitle);
        }
        else {
            $('#itemName', page).html(getParameterByName('Title') || "Media Library");
        }

        ItemListPage.refreshItems(query);
    },

    refreshItems: function (query) {

        Dashboard.showLoadingMsg();

        var page = $.mobile.activePage;

        page.itemQuery = query;

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(ItemListPage.renderItems);
    },

    renderItems: function (result) {

        var items = result.Items;

        var query = $.mobile.activePage.itemQuery;

        var renderOptions = {

            items: items,
            useAverageAspectRatio: query.Recursive !== true,
            showTitle: query.Recursive
        };

        var html = Dashboard.getPosterViewHtml(renderOptions);

        $('#listItems', $.mobile.activePage).html(html);

        Dashboard.hideLoadingMsg();
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
    },

    filter: function(name, add)
    {
        var query = $.mobile.activePage.itemQuery;
        var filters = query.Filters || "";

        filters = (',' + filters).replace(',' + name, '').substring(1);

        if (add) {
            filters = filters ? (filters + ',' + name) : name;
        }

        query.Filters = filters;

        ItemListPage.refreshItems(query);
    },

    showSortPanel: function () {

        var page = $.mobile.activePage;

        $('#viewpanel', page).hide();
        $('#filterpanel', page).hide();
        $('#indexpanel', page).hide();
        $('#sortpanel', page).show();

        $('#btnViewPanel', page).buttonMarkup({ theme: "c" });
        $('#btnSortPanel', page).buttonMarkup({ theme: "a" });
        $('#btnIndexPanel', page).buttonMarkup({ theme: "c" });
        $('#btnFilterPanel', page).buttonMarkup({ theme: "c" });
    },

    showViewPanel: function () {

        var page = $.mobile.activePage;

        $('#viewpanel', page).show();
        $('#filterpanel', page).hide();
        $('#indexpanel', page).hide();
        $('#sortpanel', page).hide();

        $('#btnViewPanel', page).buttonMarkup({ theme: "a" });
        $('#btnSortPanel', page).buttonMarkup({ theme: "c" });
        $('#btnIndexPanel', page).buttonMarkup({ theme: "c" });
        $('#btnFilterPanel', page).buttonMarkup({ theme: "c" });
    },

    showIndexPanel: function () {

        var page = $.mobile.activePage;

        $('#viewpanel', page).hide();
        $('#filterpanel', page).hide();
        $('#indexpanel', page).show();
        $('#sortpanel', page).hide();

        $('#btnViewPanel', page).buttonMarkup({ theme: "c" });
        $('#btnSortPanel', page).buttonMarkup({ theme: "c" });
        $('#btnIndexPanel', page).buttonMarkup({ theme: "a" });
        $('#btnFilterPanel', page).buttonMarkup({ theme: "c" });
    },

    showFilterPanel: function () {

        var page = $.mobile.activePage;

        $('#viewpanel', page).hide();
        $('#filterpanel', page).show();
        $('#indexpanel', page).hide();
        $('#sortpanel', page).hide();

        $('#btnViewPanel', page).buttonMarkup({ theme: "c" });
        $('#btnSortPanel', page).buttonMarkup({ theme: "c" });
        $('#btnIndexPanel', page).buttonMarkup({ theme: "c" });
        $('#btnFilterPanel', page).buttonMarkup({ theme: "a" });
    }
};

$(document).on('pageshow', "#itemListPage", ItemListPage.onPageShow);
