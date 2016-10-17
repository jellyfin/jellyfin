define(['components/categorysyncbuttons', 'cardBuilder', 'apphost', 'scripts/livetvcomponents', 'emby-button', 'listViewStyle', 'emby-itemscontainer'], function (categorysyncbuttons, cardBuilder, appHost) {

    function getRecordingGroupHtml(group) {

        var html = '';

        html += '<div class="listItem">';

        html += '<button type="button" is="emby-button" class="fab mini autoSize blue" item-icon><i class="md-icon">live_tv</i></button>';

        html += '<div class="listItemBody two-line">';
        html += '<a href="livetvitems.html?type=Recordings&groupid=' + group.Id + '" class="clearLink">';

        html += '<div>';
        html += group.Name;
        html += '</div>';

        html += '<div class="secondary">';
        if (group.RecordingCount == 1) {
            html += Globalize.translate('ValueItemCount', group.RecordingCount);
        } else {
            html += Globalize.translate('ValueItemCountPlural', group.RecordingCount);
        }
        html += '</div>';

        html += '</a>';
        html += '</div>';
        html += '</div>';

        return html;
    }

    function renderRecordingGroups(context, groups) {

        if (groups.length) {
            context.querySelector('#recordingGroups').classList.remove('hide');
        } else {
            context.querySelector('#recordingGroups').classList.add('hide');
        }

        var html = '';

        html += '<div class="paperList">';

        for (var i = 0, length = groups.length; i < length; i++) {

            html += getRecordingGroupHtml(groups[i]);
        }

        html += '</div>';

        context.querySelector('#recordingGroupItems').innerHTML = html;

        Dashboard.hideLoadingMsg();
    }

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts;
    }

    function renderRecordings(elem, recordings, cardOptions) {

        if (recordings.length) {
            elem.classList.remove('hide');
        } else {
            elem.classList.add('hide');
        }

        var recordingItems = elem.querySelector('.recordingItems');

        if (enableScrollX()) {
            recordingItems.classList.add('hiddenScrollX');
            recordingItems.classList.remove('vertical-wrap');
        } else {
            recordingItems.classList.remove('hiddenScrollX');
            recordingItems.classList.add('vertical-wrap');
        }

        var supportsImageAnalysis = appHost.supports('imageanalysis');
        var cardLayout = appHost.preferVisualCards || supportsImageAnalysis;

        recordingItems.innerHTML = cardBuilder.getCardsHtml(Object.assign({
            items: recordings,
            shape: (enableScrollX() ? 'autooverflow' : 'auto'),
            showTitle: true,
            showParentTitle: true,
            coverImage: true,
            lazy: true,
            cardLayout: cardLayout,
            centerText: !cardLayout,
            vibrant: supportsImageAnalysis,
            allowBottomPadding: !enableScrollX(),
            preferThumb: 'auto',
            overlayText: false

        }, cardOptions || {}));

        ImageLoader.lazyChildren(recordingItems);
    }

    function getBackdropShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
    }

    function renderActiveRecordings(context, promise) {

        promise.then(function (result) {

            // The IsActive param is new, so handle older servers that don't support it
            if (result.Items.length && result.Items[0].Status != 'InProgress') {
                result.Items = [];
            }

            renderRecordings(context.querySelector('#activeRecordings'), result.Items, {
                shape: getBackdropShape(),
                showParentTitle: false,
                showTitle: true,
                showAirTime: true,
                showAirEndTime: true,
                showChannelName: true,
                cardLayout: true,
                vibrant: true,
                preferThumb: true,
                coverImage: true
            });
        });
    }

    function renderLatestRecordings(context, promise) {

        promise.then(function (result) {

            renderRecordings(context.querySelector('#latestRecordings'), result.Items, {
                shape: (enableScrollX() ? 'overflowBackdrop' : 'backdrop')
            });

            Dashboard.hideLoadingMsg();
        });
    }

    function renderMovieRecordings(context, promise) {

        promise.then(function (result) {

            renderRecordings(context.querySelector('#movieRecordings'), result.Items, {
                showYear: true,
                showParentTitle: false
            });
        });
    }

    function renderEpisodeRecordings(context, promise) {

        promise.then(function (result) {

            renderRecordings(context.querySelector('#episodeRecordings'), result.Items, {
                showItemCounts: true,
                showParentTitle: false
            });
        });
    }

    function renderSportsRecordings(context, promise) {

        promise.then(function (result) {

            renderRecordings(context.querySelector('#sportsRecordings'), result.Items, {
                showYear: true,
                showParentTitle: false
            });
        });
    }

    function renderKidsRecordings(context, promise) {

        promise.then(function (result) {

            renderRecordings(context.querySelector('#kidsRecordings'), result.Items, {
                showYear: true,
                showParentTitle: false
            });
        });
    }

    function onMoreClick(e) {

        var type = this.getAttribute('data-type');

        switch (type) {
            case 'latest':
                Dashboard.navigate('livetvitems.html?type=Recordings');
                break;
            case 'movies':
                Dashboard.navigate('livetvitems.html?type=Recordings&IsMovie=true');
                break;
            case 'episodes':
                Dashboard.navigate('livetvitems.html?type=RecordingSeries');
                break;
            case 'programs':
                Dashboard.navigate('livetvitems.html?type=Recordings&IsSeries=false&IsMovie=false');
                break;
            case 'kids':
                Dashboard.navigate('livetvitems.html?type=Recordings&IsKids=true');
                break;
            case 'sports':
                Dashboard.navigate('livetvitems.html?type=Recordings&IsSports=true');
                break;
            default:
                break;
        }
    }

    return function (view, params, tabContent) {

        var self = this;
        var activeRecordingsPromise;
        var sportsPromise;
        var kidsPromise;
        var moviesPromise;
        var seriesPromise;
        var latestPromise;
        var lastFullRender = 0;

        categorysyncbuttons.init(tabContent);

        var moreButtons = tabContent.querySelectorAll('.more');
        for (var i = 0, length = moreButtons.length; i < length; i++) {
            moreButtons[i].addEventListener('click', onMoreClick);
        }
        tabContent.querySelector('#activeRecordings .recordingItems').addEventListener('timercancelled', function () {
            self.preRender();
            self.renderTab();
        });

        function enableFullRender() {
            return (new Date().getTime() - lastFullRender) > 300000;
        }

        self.preRender = function () {

            activeRecordingsPromise = ApiClient.getLiveTvRecordings({

                UserId: Dashboard.getCurrentUserId(),
                IsInProgress: true,
                Fields: 'CanDelete,PrimaryImageAspectRatio,BasicSyncInfo',
                EnableTotalRecordCount: false,
                EnableImageTypes: "Primary,Thumb,Backdrop"
            });

            if (!enableFullRender()) {
                return;
            }

            latestPromise = ApiClient.getLiveTvRecordings({

                UserId: Dashboard.getCurrentUserId(),
                Limit: enableScrollX() ? 12 : 8,
                IsInProgress: false,
                Fields: 'CanDelete,PrimaryImageAspectRatio,BasicSyncInfo',
                EnableTotalRecordCount: false,
                EnableImageTypes: "Primary,Thumb,Backdrop"
            });

            moviesPromise = ApiClient.getLiveTvRecordings({

                UserId: Dashboard.getCurrentUserId(),
                Limit: enableScrollX() ? 12 : 8,
                IsInProgress: false,
                Fields: 'CanDelete,PrimaryImageAspectRatio,BasicSyncInfo',
                EnableTotalRecordCount: false,
                IsMovie: true
            });

            seriesPromise = ApiClient.getLiveTvRecordingSeries({

                UserId: Dashboard.getCurrentUserId(),
                Limit: enableScrollX() ? 12 : 8,
                IsInProgress: false,
                Fields: 'CanDelete,PrimaryImageAspectRatio,BasicSyncInfo',
                EnableTotalRecordCount: false,
                IsSeries: true
            });

            kidsPromise = ApiClient.getLiveTvRecordings({

                UserId: Dashboard.getCurrentUserId(),
                Limit: enableScrollX() ? 12 : 8,
                IsInProgress: false,
                Fields: 'CanDelete,PrimaryImageAspectRatio,BasicSyncInfo',
                EnableTotalRecordCount: false,
                IsKids: true
            });

            sportsPromise = ApiClient.getLiveTvRecordings({
                UserId: Dashboard.getCurrentUserId(),
                Limit: enableScrollX() ? 12 : 8,
                IsInProgress: false,
                Fields: 'CanDelete,PrimaryImageAspectRatio,BasicSyncInfo',
                EnableTotalRecordCount: false,
                IsSports: true
            });
        };

        self.renderTab = function () {

            renderActiveRecordings(tabContent, activeRecordingsPromise);

            if (enableFullRender()) {
                Dashboard.showLoadingMsg();

                renderLatestRecordings(tabContent, latestPromise);
                renderMovieRecordings(tabContent, moviesPromise);
                renderEpisodeRecordings(tabContent, seriesPromise);
                renderSportsRecordings(tabContent, sportsPromise);
                renderKidsRecordings(tabContent, kidsPromise);

                ApiClient.getLiveTvRecordingGroups({

                    userId: Dashboard.getCurrentUserId()

                }).then(function (result) {

                    renderRecordingGroups(tabContent, result.Items);
                });

                lastFullRender = new Date().getTime();
            }
        };
    };

});