define(['cardBuilder', 'apphost', 'emby-itemscontainer'], function (cardBuilder, appHost) {

    return function (view, params) {

        // The base query options
        var query = {
            UserId: Dashboard.getCurrentUserId(),
            StartIndex: 0,
            Fields: "ChannelInfo"
        };

        if (params.type == 'Recordings') {
            query.IsInProgress = false;

            if (params.groupid) {
                query.GroupId = params.groupid;
            }

        } else if (params.type == 'RecordingSeries') {
            query.SortOrder = 'SortName';
            query.SortOrder = 'Ascending';
        } else {
            query.HasAired = false;
            query.SortBy = 'StartDate,SortName';
            query.SortOrder = 'Ascending';
        }

        function getSavedQueryKey() {
            return LibraryBrowser.getSavedQueryKey();
        }

        function reloadItems(page) {

            Dashboard.showLoadingMsg();

            var promise = params.type == 'Recordings' ?
                ApiClient.getLiveTvRecordings(query) :
                params.type == 'RecordingSeries' ?
                ApiClient.getLiveTvRecordingSeries(query) :
                params.IsAiring == 'true' ?
                ApiClient.getLiveTvRecommendedPrograms(query) :
                ApiClient.getLiveTvPrograms(query);

            promise.then(function (result) {

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

                var html = '';
                var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    showLimit: false
                });

                page.querySelector('.listTopPaging').innerHTML = pagingHtml;

                var supportsImageAnalysis = appHost.supports('imageanalysis') && (params.type == 'Recordings' || params.type == 'RecordingSeries');

                html = cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: query.IsMovie || params.type == 'RecordingSeries' ? 'portrait' : "backdrop",
                    preferThumb: !query.IsMovie && params.type != 'RecordingSeries',
                    inheritThumb: params.type == 'Recordings',
                    context: 'livetv',
                    centerText: !supportsImageAnalysis,
                    lazy: true,
                    overlayText: false,
                    showParentTitleOrTitle: true,
                    showTitle: false,
                    showParentTitle: query.IsSeries !== false && !query.IsMovie,
                    showAirTime: params.type != 'Recordings' && params.type != 'RecordingSeries',
                    showAirDateTime: params.type != 'Recordings' && params.type != 'RecordingSeries',
                    showChannelName: params.type != 'Recordings' && params.type != 'RecordingSeries',
                    overlayMoreButton: !supportsImageAnalysis,
                    showYear: query.IsMovie && params.type == 'Recordings',
                    coverImage: true,
                    cardLayout: supportsImageAnalysis,
                    vibrant: supportsImageAnalysis
                });

                var elem = page.querySelector('.itemsContainer');
                elem.innerHTML = html + pagingHtml;
                ImageLoader.lazyChildren(elem);

                var i, length;
                var elems;

                function onNextPageClick() {
                    query.StartIndex += query.Limit;
                    reloadItems(page);
                }

                function onPreviousPageClick() {
                    query.StartIndex -= query.Limit;
                    reloadItems(page);
                }

                elems = page.querySelectorAll('.btnNextPage');
                for (i = 0, length = elems.length; i < length; i++) {
                    elems[i].addEventListener('click', onNextPageClick);
                }

                elems = page.querySelectorAll('.btnPreviousPage');
                for (i = 0, length = elems.length; i < length; i++) {
                    elems[i].addEventListener('click', onPreviousPageClick);
                }

                LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

                Dashboard.hideLoadingMsg();
            });
        }

        view.addEventListener('viewbeforeshow', function () {

            query.ParentId = LibraryMenu.getTopParentId();

            var page = this;
            var limit = LibraryBrowser.getDefaultPageSize();

            // If the default page size has changed, the start index will have to be reset
            if (limit != query.Limit) {
                query.Limit = limit;
                query.StartIndex = 0;
            }

            if (params.IsMovie == 'true') {
                query.IsMovie = true;
            }
            else if (params.IsMovie == 'false') {
                query.IsMovie = false;
            }
            if (params.IsSports == 'true') {
                query.IsSports = true;
            }
            else if (params.IsSports == 'false') {
                query.IsSports = false;
            }
            if (params.IsKids == 'true') {
                query.IsKids = true;
            }
            else if (params.IsKids == 'false') {
                query.IsKids = false;
            }
            if (params.IsAiring == 'true') {
                query.IsAiring = true;
            }
            else if (params.IsAiring == 'false') {
                query.IsAiring = false;
            }

            if (params.type == 'Recordings') {

                if (params.IsMovie == 'true') {
                    LibraryMenu.setTitle(Globalize.translate('TabMovies'));
                } else if (params.IsSports == 'true') {
                    LibraryMenu.setTitle(Globalize.translate('Sports'));
                } else if (params.IsKids == 'true') {
                    LibraryMenu.setTitle(Globalize.translate('HeaderForKids'));
                } else {
                    LibraryMenu.setTitle(Globalize.translate('TabRecordings'));
                }

            } else if (params.type == 'RecordingSeries') {

                LibraryMenu.setTitle(Globalize.translate('TabSeries'));
            } else {

                if (params.IsMovie == 'true') {
                    LibraryMenu.setTitle(Globalize.translate('HeaderUpcomingMovies'));
                } else if (params.IsSports == 'true') {
                    LibraryMenu.setTitle(Globalize.translate('HeaderUpcomingSports'));
                } else if (params.IsKids == 'true') {
                    LibraryMenu.setTitle(Globalize.translate('HeaderUpcomingForKids'));
                } else if (params.IsAiring == 'true') {
                    LibraryMenu.setTitle(Globalize.translate('HeaderWhatsOnTV'));
                } else {
                    LibraryMenu.setTitle(Globalize.translate('HeaderUpcomingPrograms'));
                }
            }

            var viewkey = getSavedQueryKey();

            LibraryBrowser.loadSavedQueryValues(viewkey, query);

            reloadItems(page);
        });
    };
});