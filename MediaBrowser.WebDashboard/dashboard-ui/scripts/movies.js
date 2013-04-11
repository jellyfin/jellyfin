(function ($, document) {

    var view = "Tile";

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Movie",
        Recursive: true,
        Fields: "PrimaryImageAspectRatio,UserData,DisplayMediaType,ItemCounts,DateCreated",
        Limit: LibraryBrowser.getDetaultPageSize(),
        StartIndex: 0
    };

    function getTableHtml(result) {

        var items = result.Items;

        var html = '';

        html += '<table class="libraryItemsGrid">';
        html += '<thead>';

        html += '<tr>';
        html += '<th>&nbsp;</th>';
        html += '<th class="thName">Name</th>';
        html += '<th class="desktopColumn">Type</th>';
        html += '<th class="tabletColumn">Year</th>';
        html += '<th class="tabletColumn">Rating</th>';
        html += '<th class="tabletColumn">Runtime</th>';
        html += '<th class="tabletColumn">Community Rating</th>';
        html += '<th class="tabletColumn"></th>';
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

        html += '<td>';

        var url = "itemdetails.html?id=" + item.Id;

        html += '<a href="' + url + '">';

        if (item.BackdropImageTags && item.BackdropImageTags.length) {

            html += '<img class="libraryGridImage" src="' + ApiClient.getImageUrl(item.Id, {
                type: "Backdrop",
                width: 220,
                tag: item.BackdropImageTags[0],
                index: 0
            }) + '" />';

        }
        else if (item.ImageTags && item.ImageTags.Thumb) {
            html += '<img class="libraryGridImage" src="' + ApiClient.getImageUrl(item.Id, {
                type: "Thumb",
                width: 220,
                tag: item.ImageTags.Thumb
            }) + '" />';
        }
        else if (item.ImageTags && item.ImageTags.Primary) {
            html += '<img class="libraryGridImage" src="' + ApiClient.getImageUrl(item.Id, {
                type: "Primary",
                width: 220,
                tag: item.ImageTags.Primary
            }) + '" />';
        }
        else {
            html += '<img class="libraryGridImage" style="background:' + LibraryBrowser.getMetroColor(item.Id) + ';" src="css/images/items/list/video.png" />';
        }

        html += '</a></td>';

        html += '<td class="tdName"><a href="' + url + '">' + item.Name + '</a></td>';

        html += '<td class="desktopColumn">' + (item.VideoType == "VideoFile" ? item.DisplayMediaType : item.VideoType) + '</td>';

        html += '<td class="tabletColumn">' + (item.ProductionYear || "") + '</td>';

        html += '<td class="tabletColumn">' + (item.OfficialRating || "") + '</td>';

        var minutes = (item.RunTimeTicks || 0) / 600000000;

        minutes = minutes || 1;

        html += '<td class="tabletColumn">' + parseInt(minutes) + 'min</td>';
        html += '<td class="tabletColumn">' + (item.CommunityRating || "") + '</td>';

        html += '<td class="tabletColumn">';

        html += LibraryBrowser.getUserDataIconsHtml(item);

        html += '</td>';

        html += '</tr>';
        return html;
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            var html = '';

            var showPaging = result.TotalRecordCount > query.Limit;

            if (showPaging) {
                html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);
            }

            if (view == "Tile") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true,
                    preferBackdrop: true
                });
            }
            else if (view == "Poster") {
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true
                });
            }
            else if (view == "Grid") {
                html += getTableHtml(result);
            }

            if (showPaging) {
                html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);
            }

            var elem = $('#items', page);

            // cleanup existing event handlers
            $('select', elem).off('change');

            elem.html(html).trigger('create');

            $('select', elem).on('change', function () {
                query.StartIndex = (parseInt(this.value) - 1) * query.Limit;
                reloadItems(page);
            });

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinit', "#moviesPage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            query.StartIndex = 0;
            query.SortBy = this.getAttribute('data-sortby');
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
            query.StartIndex = 0;
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

            query.StartIndex = 0;
            query.Filters = filters;

            reloadItems(page);
        });


        $('.chkVideoTypeFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.VideoTypes || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.VideoTypes = filters;

            reloadItems(page);
        });

        $('#selectView', this).on('change', function () {

            view = this.value;

            reloadItems(page);
        });

        $('#chk3D', this).on('change', function () {

            query.StartIndex = 0;
            query.VideoFormats = this.checked ? this.getAttribute('data-filter') : null;

            reloadItems(page);
        });

    }).on('pagebeforeshow', "#moviesPage", function () {

        reloadItems(this);

    }).on('pageshow', "#moviesPage", function () {


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

        $('.chkVideoTypeFilter', this).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#selectView', this).val(view).selectmenu('refresh');

        $('#chk3D', this).checked(query.VideoFormats == "Digital3D,Sbs3D").checkboxradio('refresh');
    });

})(jQuery, document);