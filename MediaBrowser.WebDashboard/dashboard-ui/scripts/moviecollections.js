(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'Poster');

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "BoxSet",
        Recursive: true,
        Fields: "PrimaryImageAspectRatio,SortName,SyncInfo",
        StartIndex: 0,
        ImageTypeLimit: 1,
        EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
    };

    function getSavedQueryKey() {

        return 'collections' + (query.ParentId || '');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getItems(Dashboard.getCurrentUserId(), query);
        var promise2 = Dashboard.getCurrentUser();

        $.when(promise1, promise2).done(function (response1, response2) {

            var result = response1[0];
            var user = response2[0];

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            var addiontalButtonsHtml = user.Configuration.IsAdministrator ?
                ('<button class="btnNewCollection" data-mini="true" data-icon="plus" data-inline="true" data-iconpos="notext">' + Globalize.translate('ButtonNew') + '</button>') :
                '';

            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                additionalButtonsHtml: addiontalButtonsHtml
            });

            $('.listTopPaging', page).html(pagingHtml).trigger('create');

            updateFilterControls(page);

            if (result.TotalRecordCount) {

                if (view == "List") {

                    html = LibraryBrowser.getListViewHtml({
                        items: result.Items,
                        context: 'movies',
                        sortBy: query.SortBy
                    });
                }
                else if (view == "Poster") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "portrait",
                        context: 'movies',
                        showTitle: false,
                        centerText: true,
                        lazy: true
                    });
                }

                html += pagingHtml;
                $('.noItemsMessage', page).hide();

            } else {

                $('.noItemsMessage', page).show();
            }

            $('.itemsContainer', page).html(html).trigger('create').createCardMenus();

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

        // Reset form values using the last used query
        $('.radioSortBy', page).each(function () {

            this.checked = (query.SortBy || '').toLowerCase() == this.getAttribute('data-sortby').toLowerCase();

        }).checkboxradio('refresh');

        $('.radioSortOrder', page).each(function () {

            this.checked = (query.SortOrder || '').toLowerCase() == this.getAttribute('data-sortorder').toLowerCase();

        }).checkboxradio('refresh');

        $('.chkStandardFilter', page).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#selectView', page).val(view).selectmenu('refresh');

        $('#chkTrailer', page).checked(query.HasTrailer == true).checkboxradio('refresh');
        $('#chkThemeSong', page).checked(query.HasThemeSong == true).checkboxradio('refresh');
        $('#chkThemeVideo', page).checked(query.HasThemeVideo == true).checkboxradio('refresh');

        $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);

        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    $(document).on('pageinit', "#boxsetsPage", function () {

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

            query.StartIndex = 0;
            query.Filters = filters;

            reloadItems(page);
        });

        $('#chkTrailer', this).on('change', function () {

            query.StartIndex = 0;
            query.HasTrailer = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeSong', this).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeSong = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeVideo', this).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeVideo = this.checked ? true : null;

            reloadItems(page);
        });

        $('.alphabetPicker', this).on('alphaselect', function (e, character) {

            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

        $('#selectView', this).on('change', function () {

            view = this.value;

            reloadItems(page);

            LibraryBrowser.saveViewSetting(getSavedQueryKey(), view);
        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

    }).on('pagebeforeshow', "#boxsetsPage", function () {

        var page = this;

        var context = getParameterByName('context');

        if (context == 'movies') {
            $('.collectionTabs', page).hide();
            $('.movieTabs', page).show();
        } else {
            $('.collectionTabs', page).show();
            $('.movieTabs', page).hide();
        }

        //query.ParentId = LibraryMenu.getTopParentId();

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        var viewkey = getSavedQueryKey();

        LibraryBrowser.loadSavedQueryValues(viewkey, query);

        LibraryBrowser.getSavedViewSetting(viewkey).done(function (val) {

            if (val) {
                $('#selectView', page).val(val).selectmenu('refresh').trigger('change');
            } else {
                reloadItems(page);
            }
        });

    }).on('pageshow', "#boxsetsPage", function () {

        updateFilterControls(this);

    });

})(jQuery, document);

(function ($, document) {

    function showNewCollectionPanel(page, items) {

        $('.fldSelectedItemIds', page).val(items.join(','));

        var panel = $('.newCollectionPanel', page).panel('toggle');

        populateCollections(panel);
    }

    function populateCollections(panel) {

        var select = $('#selectCollectionToAddTo', panel);

        if (!select.length) {

            $('#txtNewCollectionName', panel).val('').focus();
            return;
        }

        $('.newCollectionInfo', panel).hide();

        var options = {

            Recursive: true,
            IncludeItemTypes: "BoxSet"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            var html = '';

            html += '<option value="">' + Globalize.translate('OptionNewCollection') + '</option>';

            html += result.Items.map(function (i) {

                return '<option value="' + i.Id + '">' + i.Name + '</option>';
            });

            select.html(html).val('').selectmenu('refresh').trigger('change');

        });
    }

    $(document).on('pageinit', ".collectionEditorPage", function () {

        var page = this;

        $('.itemsContainer', page).on('itemsrendered', function () {

            $('.btnNewCollection', page).off('click.newcollectionpanel').on('click.newcollectionpanel', function () {

                showNewCollectionPanel(page, []);
            });

        });

        $('#selectCollectionToAddTo', page).on('change', function () {

            if (this.value) {
                $('.newCollectionInfo', page).hide();
                $('#txtNewCollectionName', page).removeAttr('required');
            } else {
                $('.newCollectionInfo', page).show();
                $('#txtNewCollectionName', page).attr('required', 'required');
            }
        });
    });

    function redirectToCollection(id) {

        var context = getParameterByName('context');

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            Dashboard.navigate(LibraryBrowser.getHref(item, context));

        });
    }

    function createCollection(page) {

        var url = ApiClient.getUrl("Collections", {

            Name: $('#txtNewCollectionName', page).val(),
            IsLocked: !$('#chkEnableInternetMetadata', page).checked(),
            Ids: $('.fldSelectedItemIds', page).val() || ''

            //ParentId: getParameterByName('parentId') || LibraryMenu.getTopParentId()

        });

        ApiClient.ajax({
            type: "POST",
            url: url,
            dataType: "json"

        }).done(function (result) {

            Dashboard.hideLoadingMsg();

            var id = result.Id;

            $('.newCollectionPanel', page).panel('toggle');
            redirectToCollection(id);

        });
    }

    function addToCollection(page, id) {

        var url = ApiClient.getUrl("Collections/" + id + "/Items", {

            Ids: $('.fldSelectedItemIds', page).val() || ''
        });

        ApiClient.ajax({
            type: "POST",
            url: url

        }).done(function () {

            Dashboard.hideLoadingMsg();

            $('.newCollectionPanel', page).panel('toggle');
            redirectToCollection(id);

        });
    }

    window.BoxSetEditor = {

        showPanel: function (page, items) {
            showNewCollectionPanel(page, items);
        },

        onNewCollectionSubmit: function () {

            Dashboard.showLoadingMsg();

            var page = $(this).parents('.page');

            var collectionId = $('#selectCollectionToAddTo', page).val();

            if (collectionId) {
                addToCollection(page, collectionId);
            } else {
                createCollection(page);
            }

            return false;
        }
    };

})(jQuery, document);