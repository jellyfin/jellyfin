(function ($, document) {

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Movie",
        Recursive: true,
        Fields: "PrimaryImageAspectRatio"
    };

    function getTableHtml(items) {

        var html = '<table data-role="table" data-mode="reflow" class="ui-responsive table-stroke libraryItemsGrid">';

        html += '<thead>';

        html += '<tr>';
        html += '<th>Name</th>';
        html += '<th>Year</th>';
        html += '<th>Official Rating</th>';
        html += '<th>Runtime</th>';
        html += '</tr>';

        html += '</thead>';

        html += '<tbody>';

        for (var i = 0, length = items.length; i < length; i++) {

            html += getRowHtml(items[i]);
        }

        html += '</tbody>';

        html += '</table>';

        return html;
    }

    function getRowHtml(item) {

        var html = '<tr>';

        html += '<td>' + (item.Name || "") + '</td>';

        html += '<td>' + (item.ProductionYear || "") + '</td>';

        html += '<td>' + (item.OfficialRating || "") + '</td>';
        html += '<td>' + (item.OfficialRating || "") + '</td>';

        html += '</tr>';
        return html;
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            $('#items', page).html(Dashboard.getPosterViewHtml({

                items: result.Items,
                useAverageAspectRatio: true

            })).html(getTableHtml(result.Items)).trigger('create');

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinit', "#moviesPage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            query.SortBy = this.getAttribute('data-sortby');
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
            query.SortOrder = this.getAttribute('data-sortorder');
            reloadItems(page);
        });

        $('.chkStandardFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.Filters = filters;

            reloadItems(page);
        });

    }).on('pageshow', "#moviesPage", function () {

        reloadItems(this);

        // Reset form values using the last used query
        $('.radioSortBy', this).each(function () {

            this.checked = query.SortBy == this.getAttribute('data-sortby');

        }).checkboxradio('refresh');

        $('.radioSortOrder', this).each(function () {

            this.checked = query.SortOrder == this.getAttribute('data-sortorder');

        }).checkboxradio('refresh');

        $('.chkStandardFilter', this).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');
    });

})(jQuery, document);