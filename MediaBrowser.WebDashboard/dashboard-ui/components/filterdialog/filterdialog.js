define(['dialogHelper', 'events', 'browser', 'jQuery', 'paper-checkbox', 'emby-collapsible', 'css!components/filterdialog/style', 'paper-radio-button', 'paper-radio-group'], function (dialogHelper, events, browser, $) {

    function renderOptions(context, selector, cssClass, items, isCheckedFn) {

        var elem = context.querySelector(selector);

        if (items.length) {

            elem.classList.remove('hide');

        } else {
            elem.classList.add('hide');
        }

        var html = '';

        //  style="margin: -.2em -.8em;"
        html += '<div class="paperCheckboxList">';

        html += items.map(function (filter) {

            var itemHtml = '';

            var checkedHtml = isCheckedFn(filter) ? ' checked' : '';
            itemHtml += '<paper-checkbox' + checkedHtml + ' data-filter="' + filter + '" class="' + cssClass + '">' + filter + '</paper-checkbox>';

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

        if (options.mode == 'livetvchannels') {

            context.querySelector('.chkFavorite').checked = query.IsFavorite == true;
            context.querySelector('.chkLikes').checked = query.IsLiked == true;
            context.querySelector('.chkDislikes').checked = query.IsDisliked == true;

        } else {
            $('.chkStandardFilter', context).each(function () {

                var filters = "," + (query.Filters || "");
                var filterName = this.getAttribute('data-filter');

                this.checked = filters.indexOf(',' + filterName) != -1;
            });
        }

        $('.chkVideoTypeFilter', context).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;
        });

        context.querySelector('.chk3DFilter').checked = query.Is3D == true;
        context.querySelector('.chkHDFilter').checked = query.IsHD == true;
        context.querySelector('.chkSDFilter').checked = query.IsHD == true;

        context.querySelector('#chkSubtitle').checked = query.HasSubtitles == true;
        context.querySelector('#chkTrailer').checked = query.HasTrailer == true;
        context.querySelector('#chkThemeSong').checked = query.HasThemeSong == true;
        context.querySelector('#chkThemeVideo').checked = query.HasThemeVideo == true;
        context.querySelector('#chkSpecialFeature').checked = query.HasSpecialFeature == true;

        $('#chkSpecialEpisode', context).checked(query.ParentIndexNumber == 0);
        $('#chkMissingEpisode', context).checked(query.IsMissing == true);
        $('#chkFutureEpisode', context).checked(query.IsUnaired == true);

        context.querySelector('.playersRadioGroup').selected = query.MinPlayers == null ? 'all' : query.MinPlayers;

        $('.chkStatus', context).each(function () {

            var filters = "," + (query.SeriesStatus || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;
        });

        $('.chkAirDays', context).each(function () {

            var filters = "," + (query.AirDays || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;
        });
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

    function bindEvents(instance, context, options) {

        var query = options.query;

        if (options.mode == 'livetvchannels') {

            $('.chkFavorite', context).on('change', function () {
                query.StartIndex = 0;
                query.IsFavorite = this.checked ? true : null;
                triggerChange(instance);
            });


            $('.chkLikes', context).on('change', function () {

                query.StartIndex = 0;
                query.IsLiked = this.checked ? true : null;
                triggerChange(instance);
            });

            $('.chkDislikes', context).on('change', function () {

                query.StartIndex = 0;
                query.IsDisliked = this.checked ? true : null;
                triggerChange(instance);
            });

        } else {
            $('.chkStandardFilter', context).on('change', function () {

                var filterName = this.getAttribute('data-filter');
                var filters = query.Filters || "";

                filters = (',' + filters).replace(',' + filterName, '').substring(1);

                if (this.checked) {
                    filters = filters ? (filters + ',' + filterName) : filterName;
                }

                query.StartIndex = 0;
                query.Filters = filters;
                triggerChange(instance);
            });
        }

        $('.chkVideoTypeFilter', context).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.VideoTypes || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.VideoTypes = filters;

            triggerChange(instance);
        });

        $('.chk3DFilter', context).on('change', function () {

            query.StartIndex = 0;
            query.Is3D = this.checked ? true : null;

            triggerChange(instance);
        });

        $('.chkHDFilter', context).on('change', function () {

            query.StartIndex = 0;
            query.IsHD = this.checked ? true : null;
            triggerChange(instance);
        });

        $('.chkSDFilter', context).on('change', function () {

            query.StartIndex = 0;
            query.IsHD = this.checked ? false : null;

            triggerChange(instance);
        });

        $('.chkStatus', context).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.SeriesStatus || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.SeriesStatus = filters;
            query.StartIndex = 0;
            triggerChange(instance);
        });

        $('.chkAirDays', context).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.AirDays || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.AirDays = filters;
            query.StartIndex = 0;
            triggerChange(instance);
        });

        $('#chkTrailer', context).on('change', function () {

            query.StartIndex = 0;
            query.HasTrailer = this.checked ? true : null;

            triggerChange(instance);
        });

        $('#chkThemeSong', context).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeSong = this.checked ? true : null;

            triggerChange(instance);
        });

        $('#chkSpecialFeature', context).on('change', function () {

            query.StartIndex = 0;
            query.HasSpecialFeature = this.checked ? true : null;

            triggerChange(instance);
        });

        $('#chkThemeVideo', context).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeVideo = this.checked ? true : null;

            triggerChange(instance);
        });

        $('#chkMissingEpisode', context).on('change', function () {

            query.StartIndex = 0;
            query.IsMissing = this.checked ? true : false;

            triggerChange(instance);
        });

        $('#chkSpecialEpisode', context).on('change', function () {

            query.StartIndex = 0;
            query.ParentIndexNumber = this.checked ? 0 : null;

            triggerChange(instance);
        });

        $('#chkFutureEpisode', context).on('change', function () {

            query.StartIndex = 0;

            if (this.checked) {
                query.IsUnaired = true;
                query.IsVirtualUnaired = null;
            } else {
                query.IsUnaired = null;
                query.IsVirtualUnaired = false;
            }

            triggerChange(instance);
        });

        $('#chkSubtitle', context).on('change', function () {

            query.StartIndex = 0;
            query.HasSubtitles = this.checked ? true : null;

            triggerChange(instance);
        });

        context.querySelector('.playersRadioGroup').addEventListener('iron-select', function (e) {

            query.StartIndex = 0;
            var val = e.target.selected;
            var newValue = val == "all" ? null : val;
            var changed = query.MinPlayers != newValue;
            query.MinPlayers = newValue;
            if (changed) {
                triggerChange(instance);
            }
        });

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

                triggerChange(instance);
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

                triggerChange(instance);
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

                triggerChange(instance);
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

                triggerChange(instance);
                return;
            }
        });
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
            context.querySelector('.players').classList.remove('hide');
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

                    var onTimeout = function () {
                        updateFilterControls(dlg, options);
                        bindEvents(self, dlg, options);

                        if (enableDynamicFilters(options.mode)) {
                            dlg.classList.add('dynamicFilterDialog');
                            loadDynamicFilters(dlg, Dashboard.getCurrentUserId(), options.query);
                        }
                    };

                    // In browsers without native web components (FF/IE), there are some quirks with the checkboxes appearing incorrectly with no visible checkmark
                    // Applying a delay after setting innerHTML seems to resolve this
                    if (browser.animate) {
                        onTimeout();
                    } else {
                        setTimeout(onTimeout, 100);
                    }
                }

                xhr.send();
            });
        };

    };
});