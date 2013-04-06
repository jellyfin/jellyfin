(function ($, document) {

    var view = "Poster";
    
    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Movie",
        Recursive: true,
        Fields: "PrimaryImageAspectRatio,UserData"
    };

    function getTableHtml(items) {

        var html = '<div class="libraryItemsGridContainer"><table data-role="table" data-mode="reflow" class="ui-responsive table-stroke libraryItemsGrid">';

        html += '<thead>';

        html += '<tr>';
        html += '<th>&nbsp;</th>';
        html += '<th class="thName">Name</th>';
        html += '<th>Year</th>';
        html += '<th>Rating</th>';
        html += '<th>Runtime</th>';
        html += '<th>Community Rating</th>';
        html += '<th></th>';
        html += '</tr>';

        html += '</thead>';

        html += '<tbody>';

        for (var i = 0, length = items.length; i < length; i++) {

            html += getRowHtml(items[i]);
        }

        html += '</tbody>';

        html += '</table></div>';

        return html;
    }

    function getRowHtml(item) {

        var html = '<tr>';

        html += '<td>';

        var url = "itemdetails.html?id=" + item.Id;

        var imageTags = item.ImageTags;

        html += '<a href="' + url + '">';

        if (imageTags.Primary) {

            html += '<img class="libraryGridImage" src="' + ApiClient.getImageUrl(item.Id, {
                type: "Primary",
                height: 100,
                tag: item.ImageTags.Primary
            }) + '" />';

        }
        else {
            html += '<img class="libraryGridImage" style="background:' + LibraryBrowser.getMetroColor(item.Id) + ';" src="css/images/items/list/video.png" />';
        }

        html += '</a></td>';

        html += '<td class="tdName"><a href="' + url + '">' + item.Name + '</a></td>';

        html += '<td>' + (item.ProductionYear || "") + '</td>';

        html += '<td>' + (item.OfficialRating || "") + '</td>';

        var minutes = (item.RunTimeTicks || 0) / 600000000;

        minutes = minutes || 1;

        html += '<td>' + parseInt(minutes) + 'min</td>';
        html += '<td>' + (item.CommunityRating || "") + '</td>';

        html += '<td>';

        var userData = item.UserData || {};

        if (userData.Played) {
            html += '<img class="imgUserItemRating" src="css/images/userdata/played.png" alt="Played" title="Played" />';
        } else {
            html += '<img class="imgUserItemRating" src="css/images/userdata/unplayed.png" alt="Played" title="Played" />';
        }

        if (typeof userData.Likes == "undefined") {
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" />';
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" />';
        } else if (userData.Likes) {
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" />';
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_up_on.png" alt="Liked" title="Like" />';
        } else {
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_down_on.png" alt="Dislike" title="Dislike" />';
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" />';
        }

        if (userData.IsFavorite) {
            html += '<img class="imgUserItemRating" src="css/images/userdata/heart_on.png" alt="Favorite" title="Favorite" />';
        } else {
            html += '<img class="imgUserItemRating" src="css/images/userdata/heart_off.png" alt="Favorite" title="Favorite" />';
        }

        html += '</td>';

        html += '</tr>';
        return html;
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            if (view == "Poster") {
                $('#items', page).html(LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true
                }));
            }
            else if (view == "Grid") {
                $('#items', page).html(getTableHtml(result.Items)).trigger('create');
            }

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


        $('.chkVideoTypeFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.VideoTypes || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.VideoTypes = filters;

            reloadItems(page);
        });
        
        $('#selectView', this).on('change', function () {

            view = this.value;

            reloadItems(page);
        });

    }).on('pagebeforeshow', "#moviesPage", function() {
        
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
    });

})(jQuery, document);