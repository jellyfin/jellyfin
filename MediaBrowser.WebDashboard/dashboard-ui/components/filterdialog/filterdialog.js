define(['dialogHelper', 'events', 'browser', 'emby-checkbox', 'emby-collapse', 'css!components/filterdialog/style'], function (dialogHelper, events, browser) {

    function renderOptions(context, selector, cssClass, items, isCheckedFn) {

        var elem = context.querySelector(selector);

        if (items.length) {

            elem.classList.remove('hide');

        } else {
            elem.classList.add('hide');
        }

        var html = '';

        //  style="margin: -.2em -.8em;"
        html += '<div class="checkboxList">';

        html += items.map(function (filter) {

            var itemHtml = '';

            var checkedHtml = isCheckedFn(filter) ? ' checked' : '';
            itemHtml += '<label>';
            itemHtml += '<input is="emby-checkbox" type="checkbox"' + checkedHtml + ' data-filter="' + filter + '" class="' + cssClass + '"/>';
            itemHtml += '<span>' + filter + '</span>';
            itemHtml += '</label>';

            return itemHtml;

        }).join('');

        html += '</div>';

        elem.querySelector('.filterOptions').innerHTML = html;
    }

    function renderFilters(context, result, query) {

        // If there's a huge number of these they will be really show to render
        if (result.Tags) {
            result.Tags.length = Math.min(result.Tags.length, 50);
        }

        renderOptions(context, '.genreFilters', 'chkGenreFilter', result.Genres, function (i) {
            var delimeter = '|';
            return (delimeter + (query.Genres || '') + delimeter).indexOf(delimeter + i + delimeter) != -1;
        });

        renderOptions(context, '.officialRatingFilters', 'chkOfficialRatingFilter', result.OfficialRatings, function (i) {
            var delimeter = '|';
            return (delimeter + (query.OfficialRatings || '') + delimeter).indexOf(delimeter + i + delimeter) != -1;
        });

        renderOptions(context, '.tagFilters', 'chkTagFilter', result.Tags, function (i) {
            var delimeter = '|';
            return (delimeter + (query.Tags || '') + delimeter).indexOf(delimeter + i + delimeter) != -1;
        });

        renderOptions(context, '.yearFilters', 'chkYearFilter', result.Years, function (i) {

            var delimeter = ',';
            return (delimeter + (query.Years || '') + delimeter).indexOf(delimeter + i + delimeter) != -1;
        });
    }

    function loadDynamicFilters(context, userId, itemQuery) {

        return ApiClient.getJSON(ApiClient.getUrl('Items/Filters', {

            UserId: userId,
            ParentId: itemQuery.ParentId,
            IncludeItemTypes: itemQuery.IncludeItemTypes


        })).then(function (result) {

            renderFilters(context, result, itemQuery);
        });

    }

    function updateFilterControls(context, options) {

        var query = options.query;

        var elems, i, length;

        if (options.mode == 'livetvchannels') {

            context.querySelector('.chkFavorite').checked = query.IsFavorite == true;
            context.querySelector('.chkLikes').checked = query.IsLiked == true;
            context.querySelector('.chkDislikes').checked = query.IsDisliked == true;

        } else {
            elems = context.querySelectorAll('.chkStandardFilter');
            for (i = 0, length = elems.length; i < length; i++) {

                var chkStandardFilter = elems[i];
                var filters = "," + (query.Filters || "");
                var filterName = chkStandardFilter.getAttribute('data-filter');

                chkStandardFilter.checked = filters.indexOf(',' + filterName) != -1;
            }
        }

        elems = context.querySelectorAll('.chkVideoTypeFilter');
        for (i = 0, length = elems.length; i < length; i++) {

            var chkVideoTypeFilter = elems[i];
            var filters = "," + (query.VideoTypes || "");
            var filterName = chkVideoTypeFilter.getAttribute('data-filter');

            chkVideoTypeFilter.checked = filters.indexOf(',' + filterName) != -1;
        }

        context.querySelector('.chk3DFilter').checked = query.Is3D == true;
        context.querySelector('.chkHDFilter').checked = query.IsHD == true;
        context.querySelector('.chkSDFilter').checked = query.IsHD == true;

        context.querySelector('#chkSubtitle').checked = query.HasSubtitles == true;
        context.querySelector('#chkTrailer').checked = query.HasTrailer == true;
        context.querySelector('#chkThemeSong').checked = query.HasThemeSong == true;
        context.querySelector('#chkThemeVideo').checked = query.HasThemeVideo == true;
        context.querySelector('#chkSpecialFeature').checked = query.HasSpecialFeature == true;

        context.querySelector('#chkSpecialEpisode').checked = query.ParentIndexNumber == 0;
        context.querySelector('#chkMissingEpisode').checked = query.IsMissing == true;
        context.querySelector('#chkFutureEpisode').checked = query.IsUnaired == true;

        //context.querySelector('.playersRadioGroup').selected = query.MinPlayers == null ? 'all' : query.MinPlayers;

        for (i = 0, length = elems.length; i < length; i++) {

            var chkStatus = elems[i];
            var filters = "," + (query.SeriesStatus || "");
            var filterName = chkStatus.getAttribute('data-filter');

            chkStatus.checked = filters.indexOf(',' + filterName) != -1;
        }

        elems = context.querySelectorAll('.chkAirDays');
        for (i = 0, length = elems.length; i < length; i++) {

            var chkAirDays = elems[i];
            var filters = "," + (query.AirDays || "");
            var filterName = chkAirDays.getAttribute('data-filter');

            chkAirDays.checked = filters.indexOf(',' + filterName) != -1;
        }
    }

    function triggerChange(instance) {

        events.trigger(instance, 'filterchange');
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function setVisibility(context, options) {

        if (options.mode == 'livetvchannels' || options.mode == 'albums' || options.mode == 'artists' || options.mode == 'albumartists' || options.mode == 'songs') {
            hideByClass(context, 'videoStandard');
        }

        if (enableDynamicFilters(options.mode)) {
            context.querySelector('.genreFilters').classList.remove('hide');
            context.querySelector('.officialRatingFilters').classList.remove('hide');
            context.querySelector('.tagFilters').classList.remove('hide');
            context.querySelector('.yearFilters').classList.remove('hide');
        }

        if (options.mode == 'movies' || options.mode == 'episodes') {
            context.querySelector('.videoTypeFilters').classList.remove('hide');
        }

        if (options.mode == 'games') {
            //context.querySelector('.players').classList.remove('hide');
        }

        if (options.mode == 'movies' || options.mode == 'series' || options.mode == 'games' || options.mode == 'episodes') {
            context.querySelector('.features').classList.remove('hide');
        }

        if (options.mode == 'series') {
            context.querySelector('.airdays').classList.remove('hide');
            context.querySelector('.seriesStatus').classList.remove('hide');
        }

        if (options.mode == 'episodes') {
            showByClass(context, 'episodeFilter');
        }
    }

    function showByClass(context, className) {

        var elems = context.querySelectorAll('.' + className);

        for (var i = 0, length = elems.length; i < length; i++) {
            elems[i].classList.remove('hide');
        }
    }

    function hideByClass(context, className) {

        var elems = context.querySelectorAll('.' + className);

        for (var i = 0, length = elems.length; i < length; i++) {
            elems[i].classList.add('hide');
        }
    }

    function enableDynamicFilters(mode) {
        return mode == 'movies' || mode == 'games' || mode == 'series' || mode == 'albums' || mode == 'albumartists' || mode == 'artists' || mode == 'songs' || mode == 'episodes';
    }

    return function (options) {

        var self = this;

        function onFavoriteChange() {
            var query = options.query;
            query.StartIndex = 0;
            query.IsFavorite = this.checked ? true : null;
            triggerChange(self);
        }

        function onStandardFilterChange() {
            
            var query = options.query;
            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Filters = filters;
            triggerChange(self);
        }

        function onVideoTypeFilterChange() {

            var query = options.query;
            var filterName = this.getAttribute('data-filter');
            var filters = query.VideoTypes || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.VideoTypes = filters;

            triggerChange(self);
        }

        function onStatusChange() {
            var query = options.query;
            var filterName = this.getAttribute('data-filter');
            var filters = query.SeriesStatus || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.SeriesStatus = filters;
            query.StartIndex = 0;
            triggerChange(self);
        }

        function onAirDayChange() {
            
            var query = options.query;
            var filterName = this.getAttribute('data-filter');
            var filters = query.AirDays || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.AirDays = filters;
            query.StartIndex = 0;
            triggerChange(self);
        }

        function bindEvents(context) {

            var query = options.query;
            var elems, i, length;

            if (options.mode == 'livetvchannels') {

                elems = context.querySelectorAll('.chkFavorite');
                for (i = 0, length = elems.length; i < length; i++) {

                    elems[i].addEventListener('change', onFavoriteChange);
                }


                context.querySelector('.chkLikes').addEventListener('change', function () {

                    query.StartIndex = 0;
                    query.IsLiked = this.checked ? true : null;
                    triggerChange(self);
                });

                context.querySelector('.chkDislikes').addEventListener('change', function () {

                    query.StartIndex = 0;
                    query.IsDisliked = this.checked ? true : null;
                    triggerChange(self);
                });

            } else {
                elems = context.querySelectorAll('.chkStandardFilter');
                for (i = 0, length = elems.length; i < length; i++) {
                    elems[i].addEventListener('change', onStandardFilterChange);
                }
            }

            elems = context.querySelectorAll('.chkVideoTypeFilter');
            for (i = 0, length = elems.length; i < length; i++) {
                elems[i].addEventListener('change', onVideoTypeFilterChange);
            }

            context.querySelector('.chk3DFilter').addEventListener('change', function () {

                query.StartIndex = 0;
                query.Is3D = this.checked ? true : null;

                triggerChange(self);
            });

            context.querySelector('.chkHDFilter').addEventListener('change', function () {

                query.StartIndex = 0;
                query.IsHD = this.checked ? true : null;
                triggerChange(self);
            });

            context.querySelector('.chkSDFilter').addEventListener('change', function () {

                query.StartIndex = 0;
                query.IsHD = this.checked ? false : null;

                triggerChange(self);
            });

            elems = context.querySelectorAll('.chkStatus');
            for (i = 0, length = elems.length; i < length; i++) {
                elems[i].addEventListener('change', onStatusChange);
            }

            elems = context.querySelectorAll('.chkAirDays');
            for (i = 0, length = elems.length; i < length; i++) {
                elems[i].addEventListener('change', onAirDayChange);
            }

            context.querySelector('#chkTrailer').addEventListener('change', function () {
                query.StartIndex = 0;
                query.HasTrailer = this.checked ? true : null;

                triggerChange(self);
            });

            context.querySelector('#chkThemeSong').addEventListener('change', function () {
                query.StartIndex = 0;
                query.HasThemeSong = this.checked ? true : null;

                triggerChange(self);
            });

            context.querySelector('#chkSpecialFeature').addEventListener('change', function () {
                query.StartIndex = 0;
                query.HasSpecialFeature = this.checked ? true : null;

                triggerChange(self);
            });

            context.querySelector('#chkThemeVideo').addEventListener('change', function () {

                query.StartIndex = 0;
                query.HasThemeVideo = this.checked ? true : null;

                triggerChange(self);
            });

            context.querySelector('#chkMissingEpisode').addEventListener('change', function () {

                query.StartIndex = 0;
                query.IsMissing = this.checked ? true : false;

                triggerChange(self);
            });

            context.querySelector('#chkSpecialEpisode').addEventListener('change', function () {

                query.StartIndex = 0;
                query.ParentIndexNumber = this.checked ? 0 : null;

                triggerChange(self);
            });

            context.querySelector('#chkFutureEpisode').addEventListener('change', function () {

                query.StartIndex = 0;

                if (this.checked) {
                    query.IsUnaired = true;
                    query.IsVirtualUnaired = null;
                } else {
                    query.IsUnaired = null;
                    query.IsVirtualUnaired = false;
                }

                triggerChange(self);
            });

            context.querySelector('#chkSubtitle').addEventListener('change', function () {

                query.StartIndex = 0;
                query.HasSubtitles = this.checked ? true : null;

                triggerChange(self);
            });

            //context.querySelector('.playersRadioGroup').addEventListener('iron-select', function (e) {

            //    query.StartIndex = 0;
            //    var val = e.target.selected;
            //    var newValue = val == "all" ? null : val;
            //    var changed = query.MinPlayers != newValue;
            //    query.MinPlayers = newValue;
            //    if (changed) {
            //        triggerChange(self);
            //    }
            //});

            context.addEventListener('change', function (e) {

                var chkGenreFilter = parentWithClass(e.target, 'chkGenreFilter');
                if (chkGenreFilter) {
                    var filterName = chkGenreFilter.getAttribute('data-filter');
                    var filters = query.Genres || "";
                    var delimiter = '|';

                    filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

                    if (chkGenreFilter.checked) {
                        filters = filters ? (filters + delimiter + filterName) : filterName;
                    }

                    query.StartIndex = 0;
                    query.Genres = filters;

                    triggerChange(self);
                    return;
                }

                var chkTagFilter = parentWithClass(e.target, 'chkTagFilter');
                if (chkTagFilter) {
                    var filterName = chkTagFilter.getAttribute('data-filter');
                    var filters = query.Tags || "";
                    var delimiter = '|';

                    filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

                    if (chkTagFilter.checked) {
                        filters = filters ? (filters + delimiter + filterName) : filterName;
                    }

                    query.StartIndex = 0;
                    query.Tags = filters;

                    triggerChange(self);
                    return;
                }

                var chkYearFilter = parentWithClass(e.target, 'chkYearFilter');
                if (chkYearFilter) {
                    var filterName = chkYearFilter.getAttribute('data-filter');
                    var filters = query.Years || "";
                    var delimiter = ',';

                    filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

                    if (chkYearFilter.checked) {
                        filters = filters ? (filters + delimiter + filterName) : filterName;
                    }

                    query.StartIndex = 0;
                    query.Years = filters;

                    triggerChange(self);
                    return;
                }

                var chkOfficialRatingFilter = parentWithClass(e.target, 'chkOfficialRatingFilter');
                if (chkOfficialRatingFilter) {
                    var filterName = chkOfficialRatingFilter.getAttribute('data-filter');
                    var filters = query.OfficialRatings || "";
                    var delimiter = '|';

                    filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

                    if (chkOfficialRatingFilter.checked) {
                        filters = filters ? (filters + delimiter + filterName) : filterName;
                    }

                    query.StartIndex = 0;
                    query.OfficialRatings = filters;

                    triggerChange(self);
                    return;
                }
            });
        }

        self.show = function () {
            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/filterdialog/filterdialog.template.html', true);

                xhr.onload = function (e) {

                    var template = this.response;
                    var dlg = dialogHelper.createDialog({
                        removeOnClose: true,
                        modal: false,
                        entryAnimationDuration: 160,
                        exitAnimationDuration: 200,
                        autoFocus: false
                    });

                    dlg.classList.add('ui-body-a');
                    dlg.classList.add('background-theme-a');

                    dlg.classList.add('formDialog');
                    dlg.classList.add('filterDialog');

                    dlg.innerHTML = Globalize.translateDocument(template);

                    setVisibility(dlg, options);
                    document.body.appendChild(dlg);

                    dialogHelper.open(dlg);

                    dlg.addEventListener('close', resolve);

                    updateFilterControls(dlg, options);
                    bindEvents(dlg);

                    if (enableDynamicFilters(options.mode)) {
                        dlg.classList.add('dynamicFilterDialog');
                        loadDynamicFilters(dlg, Dashboard.getCurrentUserId(), options.query);
                    }
                }

                xhr.send();
            });
        };

    };
});