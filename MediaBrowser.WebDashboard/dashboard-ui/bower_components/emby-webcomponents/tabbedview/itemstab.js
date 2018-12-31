define(['playbackManager', 'userSettings', 'alphaPicker', 'alphaNumericShortcuts', 'connectionManager', 'focusManager', 'loading', 'globalize'], function (playbackManager, userSettings, AlphaPicker, AlphaNumericShortcuts, connectionManager, focusManager, loading, globalize) {
    'use strict';

    function trySelectValue(instance, scroller, view, value) {

        var card;

        // If it's the symbol just pick the first card
        if (value === '#') {

            card = view.querySelector('.card');

            if (card) {
                scroller.toStart(card, false);
                return;
            }
        }

        card = view.querySelector('.card[data-prefix^=\'' + value + '\']');

        if (card) {
            scroller.toStart(card, false);
            return;
        }

        // go to the previous letter
        var values = instance.alphaPicker.values();
        var index = values.indexOf(value);

        if (index < values.length - 2) {
            trySelectValue(instance, scroller, view, values[index + 1]);
        } else {
            var all = view.querySelectorAll('.card');
            card = all.length ? all[all.length - 1] : null;

            if (card) {
                scroller.toStart(card, false);
            }
        }
    }

    function onAlphaValueChanged() {

        var value = this.alphaPicker.value();
        var scroller = this.scroller;

        trySelectValue(this, scroller, this.itemsContainer, value);
    }

    function initAlphaPicker(instance, view) {

        instance.itemsContainer = view.querySelector('.itemsContainer');

        instance.alphaPicker = new AlphaPicker({
            element: instance.alphaPickerElement,
            itemsContainer: instance.itemsContainer,
            itemClass: 'card'
        });

        instance.alphaPicker.on('alphavaluechanged', onAlphaValueChanged.bind(instance));
    }

    function showFilterMenu() {

        var instance = this;

        require(['filterMenu'], function (FilterMenu) {

            new FilterMenu().show({

                settingsKey: instance.getSettingsKey(),
                settings: instance.getFilters(),
                visibleSettings: instance.getVisibleFilters(),
                onChange: instance.itemsContainer.refreshItems.bind(instance.itemsContainer),
                parentId: instance.params.parentId,
                itemTypes: instance.getItemTypes ? instance.getItemTypes() : [],
                serverId: instance.apiClient.serverId(),
                filterMenuOptions: instance.getFilterMenuOptions()

            }).then(function () {

                instance.itemsContainer.refreshItems();
            });
        });
    }

    function updateAlphaPickerState(instance) {

        if (!instance.alphaPicker) {
            return;
        }

        var alphaPicker = instance.alphaPickerElement;
        if (!alphaPicker) {
            return;
        }
        var values = instance.getSortValues();

        if (values.sortBy === 'SortName' && values.sortOrder === 'Ascending') {
            alphaPicker.classList.remove('hide');
        } else {
            alphaPicker.classList.add('hide');
        }
    }

    function showSortMenu() {

        var instance = this;

        require(['sortMenu'], function (SortMenu) {

            new SortMenu().show({

                settingsKey: instance.getSettingsKey(),
                settings: instance.getSortValues(),
                onChange: instance.itemsContainer.refreshItems.bind(instance.itemsContainer),
                serverId: instance.params.serverId,
                sortOptions: instance.getSortMenuOptions()

            }).then(function () {

                updateSortText(instance);
                updateAlphaPickerState(instance);

                instance.itemsContainer.refreshItems();
            });
        });
    }

    function showViewSettingsMenu() {

        var instance = this;

        require(['viewSettings'], function (ViewSettings) {

            new ViewSettings().show({

                settingsKey: instance.getSettingsKey(),
                settings: instance.getViewSettings(),
                visibleSettings: instance.getVisibleViewSettings()

            }).then(function () {

                updateItemsContainerForViewType(instance);
                instance.itemsContainer.refreshItems();
            });
        });
    }

    function updateItemsContainerForViewType(instance) {

        var settings = instance.getViewSettings();

        if (settings.imageType === 'list') {

            instance.itemsContainer.classList.remove('vertical-wrap');
            instance.itemsContainer.classList.add('vertical-list');

        } else {
            instance.itemsContainer.classList.add('vertical-wrap');
            instance.itemsContainer.classList.remove('vertical-list');
        }
    }

    function updateSortText(instance) {

        var btnSortText = instance.btnSortText;
        if (!btnSortText) {
            return;
        }

        var options = instance.getSortMenuOptions();
        var values = instance.getSortValues();

        var sortBy = values.sortBy;

        for (var i = 0, length = options.length; i < length; i++) {

            if (sortBy === options[i].value) {

                btnSortText.innerHTML = globalize.translate('sharedcomponents#SortByValue', options[i].name);
                break;
            }
        }

        var btnSortIcon = instance.btnSortIcon;
        if (!btnSortIcon) {
            return;
        }

        btnSortIcon.innerHTML = values.sortOrder === 'Descending' ? '&#xE5DB;' : '&#xE5D8;';
    }

    function bindAll(elems, eventName, fn) {
        for (var i = 0, length = elems.length; i < length; i++) {

            elems[i].addEventListener(eventName, fn);
        }
    }

    function play() {

        this.fetchData().then(function (result) {
            playbackManager.play({
                items: result.Items || result
            });
        });
    }

    function shuffle() {

        this.fetchData().then(function (result) {
            playbackManager.play({
                items: result.Items || result
            });
        });
    }

    function hideOrShowAll(elems, hide) {

        for (var i = 0, length = elems.length; i < length; i++) {

            if (hide) {
                elems[i].classList.add('hide');
            } else {
                elems[i].classList.remove('hide');
            }
        }
    }

    function ItemsTab(view, params) {
        this.view = view;
        this.params = params;

        if (params.serverId) {
            this.apiClient = connectionManager.getApiClient(params.serverId);
        }

        this.itemsContainer = view.querySelector('.itemsContainer');
        this.scroller = view.querySelector('.scrollFrameY');

        this.itemsContainer.fetchData = this.fetchData.bind(this);
        this.itemsContainer.getItemsHtml = this.getItemsHtml.bind(this);

        if (params.parentId) {
            this.itemsContainer.setAttribute('data-parentid', params.parentId);
        }

        var i, length;

        var btnViewSettings = view.querySelectorAll('.btnViewSettings');
        for (i = 0, length = btnViewSettings.length; i < length; i++) {

            btnViewSettings[i].addEventListener('click', showViewSettingsMenu.bind(this));
        }

        var filterButtons = view.querySelectorAll('.btnFilter');
        this.filterButtons = filterButtons;
        var hasVisibleFilters = this.getVisibleFilters().length;
        for (i = 0, length = filterButtons.length; i < length; i++) {

            var btnFilter = filterButtons[i];
            btnFilter.addEventListener('click', showFilterMenu.bind(this));

            if (hasVisibleFilters) {
                btnFilter.classList.remove('hide');
            } else {
                btnFilter.classList.add('hide');
            }
        }

        var sortButtons = view.querySelectorAll('.btnSort');
        this.sortButtons = sortButtons;
        for (i = 0, length = sortButtons.length; i < length; i++) {

            var sortButton = sortButtons[i];
            sortButton.addEventListener('click', showSortMenu.bind(this));

            if (params.type !== 'nextup') {
                sortButton.classList.remove('hide');
            }
        }
        this.btnSortText = view.querySelector('.btnSortText');
        this.btnSortIcon = view.querySelector('.btnSortIcon');

        this.alphaPickerElement = view.querySelector('.alphaPicker');

        hideOrShowAll(view.querySelectorAll('.btnShuffle'), true);

        bindAll(view.querySelectorAll('.btnPlay'), 'click', play.bind(this));
        bindAll(view.querySelectorAll('.btnShuffle'), 'click', shuffle.bind(this));
    }

    function getSettingValue(key, defaultValue) {
    }

    ItemsTab.prototype.getViewSettings = function () {

        var basekey = this.getSettingsKey();

        return {
            showTitle: userSettings.get(basekey + '-showTitle') !== 'false',
            showYear: userSettings.get(basekey + '-showYear') !== 'false',
            imageType: userSettings.get(basekey + '-imageType') || this.getDefaultImageType()
        };
    };

    ItemsTab.prototype.getDefaultImageType = function () {

        return 'primary';
    };

    ItemsTab.prototype.getSettingsKey = function () {

        return this.params.parentId + '-1';
    };

    ItemsTab.prototype.onResume = function (options) {

        if (options && options.refresh) {
            updateSortText(this);
            updateItemsContainerForViewType(this);
            loading.show();
        }

        var view = this.view;

        var scroller = this.scroller;
        if (scroller && scroller.resume) {
            scroller.resume();
        }

        if (this.enableAlphaPicker && !this.alphaPicker) {
            initAlphaPicker(this, view);
            updateAlphaPickerState(this);
        }

        if (this.enableAlphaNumericShortcuts !== false) {
            this.alphaNumericShortcuts = new AlphaNumericShortcuts({
                itemsContainer: this.itemsContainer
            });
        }

        var instance = this;
        var autoFocus = options.autoFocus;

        this.itemsContainer.resume(options).then(function (result) {

            loading.hide();

            if (autoFocus) {
                focusManager.autoFocus(instance.itemsContainer);
            }
        });
    };

    ItemsTab.prototype.getVisibleViewSettings = function () {

        return [
            'showTitle',
            'showYear',
            'imageType'
        ];
    };

    ItemsTab.prototype.getFilters = function () {

        var basekey = this.getSettingsKey();

        return {
            IsPlayed: userSettings.getFilter(basekey + '-filter-IsPlayed') === 'true',
            IsUnplayed: userSettings.getFilter(basekey + '-filter-IsUnplayed') === 'true',
            IsFavorite: userSettings.getFilter(basekey + '-filter-IsFavorite') === 'true',
            IsResumable: userSettings.getFilter(basekey + '-filter-IsResumable') === 'true',
            Is4K: userSettings.getFilter(basekey + '-filter-Is4K') === 'true',
            IsHD: userSettings.getFilter(basekey + '-filter-IsHD') === 'true',
            IsSD: userSettings.getFilter(basekey + '-filter-IsSD') === 'true',
            Is3D: userSettings.getFilter(basekey + '-filter-Is3D') === 'true',
            VideoTypes: userSettings.getFilter(basekey + '-filter-VideoTypes'),
            SeriesStatus: userSettings.getFilter(basekey + '-filter-SeriesStatus'),
            HasSubtitles: userSettings.getFilter(basekey + '-filter-HasSubtitles'),
            HasTrailer: userSettings.getFilter(basekey + '-filter-HasTrailer'),
            HasSpecialFeature: userSettings.getFilter(basekey + '-filter-HasSpecialFeature'),
            HasThemeSong: userSettings.getFilter(basekey + '-filter-HasThemeSong'),
            HasThemeVideo: userSettings.getFilter(basekey + '-filter-HasThemeVideo'),
            GenreIds: userSettings.getFilter(basekey + '-filter-GenreIds')
        };
    };

    ItemsTab.prototype.getSortValues = function () {

        var basekey = this.getSettingsKey();

        return {
            sortBy: userSettings.getFilter(basekey + '-sortby') || this.getSortMenuOptions()[0].value,
            sortOrder: userSettings.getFilter(basekey + '-sortorder') === 'Descending' ? 'Descending' : 'Ascending'
        };
    };

    ItemsTab.prototype.getVisibleFilters = function () {

        return [
            'IsUnplayed',
            'IsPlayed',
            'IsFavorite',
            'IsResumable',
            'VideoType',
            'HasSubtitles',
            'HasTrailer',
            'HasSpecialFeature',
            'HasThemeSong',
            'HasThemeVideo'
        ];
    };

    ItemsTab.prototype.getDefaultSortBy = function () {

        return 'SortName';
    };

    ItemsTab.prototype.getSortMenuOptions = function () {

        var sortBy = [];

        var option = this.getNameSortOption();
        if (option) {
            sortBy.push(option);
        }

        option = this.getCommunityRatingSortOption();
        if (option) {
            sortBy.push(option);
        }

        option = this.getCriticRatingSortOption();

        if (option) {
            sortBy.push(option);
        }

        sortBy.push({
            name: globalize.translate('sharedcomponents#DateAdded'),
            value: 'DateCreated,SortName'
        });

        option = this.getDatePlayedSortOption();
        if (option) {
            sortBy.push(option);
        }

        sortBy.push({
            name: globalize.translate('sharedcomponents#ParentalRating'),
            value: 'OfficialRating,SortName'
        });

        option = this.getPlayCountSortOption();
        if (option) {
            sortBy.push(option);
        }

        sortBy.push({
            name: globalize.translate('sharedcomponents#ReleaseDate'),
            value: 'PremiereDate,ProductionYear,SortName'
        });

        sortBy.push({
            name: globalize.translate('sharedcomponents#Runtime'),
            value: 'Runtime,SortName'
        });

        return sortBy;
    };

    ItemsTab.prototype.getNameSortOption = function () {

        return {
            name: globalize.translate('sharedcomponents#Name'),
            value: 'SortName'
        };
    };

    ItemsTab.prototype.getPlayCountSortOption = function () {

        return {
            name: globalize.translate('sharedcomponents#PlayCount'),
            value: 'PlayCount,SortName'
        };
    };

    ItemsTab.prototype.getDatePlayedSortOption = function () {

        return {
            name: globalize.translate('sharedcomponents#DatePlayed'),
            value: 'DatePlayed,SortName'
        };
    };

    ItemsTab.prototype.getCriticRatingSortOption = function () {

        return {
            name: globalize.translate('sharedcomponents#CriticRating'),
            value: 'CriticRating,SortName'
        };
    };

    ItemsTab.prototype.getCommunityRatingSortOption = function () {

        return {
            name: globalize.translate('sharedcomponents#CommunityRating'),
            value: 'CommunityRating,SortName'
        };
    };

    ItemsTab.prototype.getFilterMenuOptions = function () {

        var params = this.params;

        return {

        };
    };

    ItemsTab.prototype.getItemTypes = function () {

        return [];
    };

    ItemsTab.prototype.setFilterStatus = function (hasFilters) {

        this.hasFilters = hasFilters;

        var filterButtons = this.filterButtons;
        if (!filterButtons.length) {
            return;
        }

        for (var i = 0, length = filterButtons.length; i < length; i++) {

            var btnFilter = filterButtons[i];

            var bubble = btnFilter.querySelector('.filterButtonBubble');
            if (!bubble) {

                if (!hasFilters) {
                    continue;
                }

                btnFilter.insertAdjacentHTML('afterbegin', '<div class="filterButtonBubble">!</div>');
                btnFilter.classList.add('btnFilterWithBubble');
                bubble = btnFilter.querySelector('.filterButtonBubble');
            }

            if (hasFilters) {
                bubble.classList.remove('hide');
            } else {
                bubble.classList.add('hide');
            }
        }
    };

    ItemsTab.prototype.onPause = function () {

        var scroller = this.scroller;
        if (scroller && scroller.pause) {
            scroller.pause();
        }

        var alphaNumericShortcuts = this.alphaNumericShortcuts;
        if (alphaNumericShortcuts) {
            alphaNumericShortcuts.destroy();
            this.alphaNumericShortcuts = null;
        }
    };

    ItemsTab.prototype.destroy = function () {

        this.view = null;
        this.itemsContainer = null;
        this.params = null;
        this.apiClient = null;
        this.scroller = null;
        this.filterButtons = null;

        if (this.alphaPicker) {
            this.alphaPicker.destroy();
            this.alphaPicker = null;
        }
        this.sortButtons = null;
        this.btnSortText = null;
        this.btnSortIcon = null;
        this.alphaPickerElement = null;
    };

    return ItemsTab;
});