(function ($, document, window) {

    var currentType;

    function loadTabs(page, tabs) {

        var html = '';

        for (var i = 0, length = tabs.length; i < length; i++) {

            var tab = tabs[i];

            var isChecked = i == 0 ? ' selected="selected"' : '';

            html += '<option value="' + tab.type + '"' + isChecked + '>' + Globalize.translate(tab.name) + '</option>';
        }

        $('#selectItemType', page).html(html).trigger('change');

        Dashboard.hideLoadingMsg();
    }

    function loadType(page, type) {

        Dashboard.showLoadingMsg();

        currentType = type;

        var promise1 = ApiClient.getServerConfiguration();
        var promise2 = ApiClient.getJSON(ApiClient.getUrl("System/Configuration/MetadataPlugins"));

        Promise.all([promise1, promise2]).then(function (responses) {

            var config = responses[0];
            var metadataPlugins = responses[1];

            config = config.MetadataOptions.filter(function (c) {
                return c.ItemType == type;
            })[0];

            if (config) {

                renderType(page, type, config, metadataPlugins);

                Dashboard.hideLoadingMsg();

            } else {

                ApiClient.getJSON(ApiClient.getUrl("System/Configuration/MetadataOptions/Default")).then(function (defaultConfig) {


                    config = defaultConfig;

                    renderType(page, type, config, metadataPlugins);

                    Dashboard.hideLoadingMsg();
                });

            }

        });

    }

    function setVisibilityOfBackdrops(elem, visible) {

        if (visible) {
            elem.show();

            $('input', elem).attr('required', 'required');

        } else {
            elem.hide();

            $('input', elem).attr('required', '').removeAttr('required');
        }
    }

    function renderType(page, type, config, metadataPlugins) {

        var metadataInfo = metadataPlugins.filter(function (f) {

            return type == f.ItemType;
        })[0];

        setVisibilityOfBackdrops($('.backdropFields', page), metadataInfo.SupportedImageTypes.indexOf('Backdrop') != -1);
        setVisibilityOfBackdrops($('.screenshotFields', page), metadataInfo.SupportedImageTypes.indexOf('Screenshot') != -1);

        $('.imageType', page).each(function () {

            var imageType = this.getAttribute('data-imagetype');

            if (metadataInfo.SupportedImageTypes.indexOf(imageType) == -1) {
                $(this).hide();
            } else {
                $(this).show();
            }

            if (getImageConfig(config, imageType).Limit) {

                $('input', this).checked(true).checkboxradio('refresh');

            } else {
                $('input', this).checked(false).checkboxradio('refresh');
            }
        });

        var backdropConfig = getImageConfig(config, 'Backdrop');

        $('#txtMaxBackdrops', page).val(backdropConfig.Limit);
        $('#txtMinBackdropDownloadWidth', page).val(backdropConfig.MinWidth);

        var screenshotConfig = getImageConfig(config, 'Screenshot');

        $('#txtMaxScreenshots', page).val(screenshotConfig.Limit);
        $('#txtMinScreenshotDownloadWidth', page).val(screenshotConfig.MinWidth);

        renderMetadataLocals(page, type, config, metadataInfo);
        renderMetadataFetchers(page, type, config, metadataInfo);
        renderMetadataSavers(page, type, config, metadataInfo);
        renderImageFetchers(page, type, config, metadataInfo);
    }

    function getImageConfig(config, type) {

        return config.ImageOptions.filter(function (i) {

            return i.Type == type;

        })[0] || {

            Type: type,
            MinWidth: type == 'Backdrop' ? 1280 : 0,
            Limit: type == 'Backdrop' ? 3 : 1
        };
    }

    function renderImageFetchers(page, type, config, metadataInfo) {

        var plugins = metadataInfo.Plugins.filter(function (p) {
            return p.Type == 'ImageFetcher';
        });

        var html = '';

        if (!plugins.length) {
            $('.imageFetchers', page).html(html).hide().trigger('create');
            return;
        }

        var i, length, plugin, id;

        html += '<div class="ui-controlgroup-label" style="margin-bottom:0;padding-left:2px;">' + Globalize.translate('LabelImageFetchers') + '</div>';

        html += '<div style="display:inline-block;width: 75%;vertical-align:top;">';
        html += '<div data-role="controlgroup" class="imageFetcherGroup">';

        for (i = 0, length = plugins.length; i < length; i++) {

            plugin = plugins[i];

            id = 'chkImageFetcher' + i;

            var isChecked = config.DisabledImageFetchers.indexOf(plugin.Name) == -1 ? ' checked="checked"' : '';

            html += '<input class="chkImageFetcher" type="checkbox" name="' + id + '" id="' + id + '" data-pluginname="' + plugin.Name + '" data-mini="true"' + isChecked + '>';
            html += '<label for="' + id + '">' + plugin.Name + '</label>';
        }

        html += '</div>';
        html += '</div>';

        if (plugins.length > 1) {
            html += '<div style="display:inline-block;vertical-align:top;margin-left:5px;">';

            for (i = 0, length = plugins.length; i < length; i++) {

                html += '<div>';

                if (i > 0) {
                    html += '<paper-icon-button class="btnUp" data-pluginindex="' + i + '" icon="keyboard-arrow-up" title="' + Globalize.translate('ButtonUp') + '" style="padding:3px 8px;"></paper-icon-button>';
                } else {
                    html += '<paper-icon-button disabled class="btnUp" data-pluginindex="' + i + '" icon="keyboard-arrow-up" title="' + Globalize.translate('ButtonUp') + '" style="padding:3px 8px;"></paper-icon-button>';
                }

                if (i < (plugins.length - 1)) {
                    html += '<paper-icon-button class="btnDown" data-pluginindex="' + i + '" icon="keyboard-arrow-down" title="' + Globalize.translate('ButtonDown') + '" style="padding:3px 8px;"></paper-icon-button>';
                } else {
                    html += '<paper-icon-button disabled class="btnDown" data-pluginindex="' + i + '" icon="keyboard-arrow-down" title="' + Globalize.translate('ButtonDown') + '" style="padding:3px 8px;"></paper-icon-button>';
                }
                html += '</div>';
            }
        }

        html += '</div>';
        html += '<div class="fieldDescription" style="width:75%;">' + Globalize.translate('LabelImageFetchersHelp') + '</div>';

        var elem = $('.imageFetchers', page).html(html).show().trigger('create');

        $('.btnDown', elem).on('click', function () {
            var index = parseInt(this.getAttribute('data-pluginindex'));

            var elemToMove = $('.imageFetcherGroup .ui-checkbox', page)[index];

            var insertAfter = $(elemToMove).next('.ui-checkbox')[0];

            elemToMove.parentNode.removeChild(elemToMove);
            $(elemToMove).insertAfter(insertAfter);

            $('.imageFetcherGroup', page).controlgroup('destroy').controlgroup();
        });

        $('.btnUp', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-pluginindex'));

            var elemToMove = $('.imageFetcherGroup .ui-checkbox', page)[index];

            var insertBefore = $(elemToMove).prev('.ui-checkbox')[0];

            elemToMove.parentNode.removeChild(elemToMove);
            $(elemToMove).insertBefore(insertBefore);

            $('.imageFetcherGroup', page).controlgroup('destroy').controlgroup();
        });
    }

    function renderMetadataSavers(page, type, config, metadataInfo) {

        var plugins = metadataInfo.Plugins.filter(function (p) {
            return p.Type == 'MetadataSaver';
        });

        var html = '';

        if (!plugins.length) {
            $('.metadataSavers', page).html(html).hide().trigger('create');
            return;
        }

        html += '<fieldset data-role="controlgroup">';
        html += '<legend>' + Globalize.translate('LabelMetadataSavers') + '</legend>';

        for (var i = 0, length = plugins.length; i < length; i++) {

            var plugin = plugins[i];

            var id = 'chkMetadataSaver' + i;

            var isChecked = config.DisabledMetadataSavers.indexOf(plugin.Name) == -1 ? ' checked="checked"' : '';

            html += '<input class="chkMetadataSaver" type="checkbox" name="' + id + '" id="' + id + '" data-mini="true"' + isChecked + ' data-pluginname="' + plugin.Name + '">';
            html += '<label for="' + id + '">' + plugin.Name + '</label>';
        }

        html += '</fieldset>';
        html += '<div class="fieldDescription">' + Globalize.translate('LabelMetadataSaversHelp') + '</div>';

        $('.metadataSavers', page).html(html).show().trigger('create');
    }

    function renderMetadataFetchers(page, type, config, metadataInfo) {

        var plugins = metadataInfo.Plugins.filter(function (p) {
            return p.Type == 'MetadataFetcher';
        });

        var html = '';

        if (!plugins.length) {
            $('.metadataFetchers', page).html(html).hide().trigger('create');
            return;
        }

        var i, length, plugin, id;

        html += '<div class="ui-controlgroup-label" style="margin-bottom:0;padding-left:2px;">' + Globalize.translate('LabelMetadataDownloaders') + '</div>';

        html += '<div style="display:inline-block;width: 75%;vertical-align:top;">';
        html += '<div data-role="controlgroup" class="metadataFetcherGroup">';

        for (i = 0, length = plugins.length; i < length; i++) {

            plugin = plugins[i];

            id = 'chkMetadataFetcher' + i;

            var isChecked = config.DisabledMetadataFetchers.indexOf(plugin.Name) == -1 ? ' checked="checked"' : '';

            html += '<input class="chkMetadataFetcher" type="checkbox" name="' + id + '" id="' + id + '" data-pluginname="' + plugin.Name + '" data-mini="true"' + isChecked + '>';
            html += '<label for="' + id + '">' + plugin.Name + '</label>';
        }

        html += '</div>';
        html += '</div>';

        if (plugins.length > 1) {
            html += '<div style="display:inline-block;vertical-align:top;margin-left:5px;">';

            for (i = 0, length = plugins.length; i < length; i++) {

                html += '<div>';

                if (i > 0) {
                    html += '<paper-icon-button class="btnUp" data-pluginindex="' + i + '" icon="keyboard-arrow-up" title="' + Globalize.translate('ButtonUp') + '" style="padding:3px 8px;"></paper-icon-button>';
                } else {
                    html += '<paper-icon-button disabled class="btnUp" data-pluginindex="' + i + '" icon="keyboard-arrow-up" title="' + Globalize.translate('ButtonUp') + '" style="padding:3px 8px;"></paper-icon-button>';
                }

                if (i < (plugins.length - 1)) {
                    html += '<paper-icon-button class="btnDown" data-pluginindex="' + i + '" icon="keyboard-arrow-down" title="' + Globalize.translate('ButtonDown') + '" style="padding:3px 8px;"></paper-icon-button>';
                } else {
                    html += '<paper-icon-button disabled class="btnDown" data-pluginindex="' + i + '" icon="keyboard-arrow-down" title="' + Globalize.translate('ButtonDown') + '" style="padding:3px 8px;"></paper-icon-button>';
                }
                html += '</div>';
            }
        }

        html += '</div>';
        html += '<div class="fieldDescription" style="width:75%;">' + Globalize.translate('LabelMetadataDownloadersHelp') + '</div>';

        var elem = $('.metadataFetchers', page).html(html).show().trigger('create');

        $('.btnDown', elem).on('click', function () {
            var index = parseInt(this.getAttribute('data-pluginindex'));

            var elemToMove = $('.metadataFetcherGroup .ui-checkbox', page)[index];

            var insertAfter = $(elemToMove).next('.ui-checkbox')[0];

            elemToMove.parentNode.removeChild(elemToMove);
            $(elemToMove).insertAfter(insertAfter);

            $('.metadataFetcherGroup', page).controlgroup('destroy').controlgroup();
        });

        $('.btnUp', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-pluginindex'));

            var elemToMove = $('.metadataFetcherGroup .ui-checkbox', page)[index];

            var insertBefore = $(elemToMove).prev('.ui-checkbox')[0];

            elemToMove.parentNode.removeChild(elemToMove);
            $(elemToMove).insertBefore(insertBefore);

            $('.metadataFetcherGroup', page).controlgroup('destroy').controlgroup();
        });
    }

    function renderMetadataLocals(page, type, config, metadataInfo) {

        var plugins = metadataInfo.Plugins.filter(function (p) {
            return p.Type == 'LocalMetadataProvider';
        });

        var html = '';

        if (plugins.length < 2) {
            $('.metadataReaders', page).html(html).hide().trigger('create');
            return;
        }

        html += '<div class="ui-controlgroup-label" style="margin-bottom:0;padding-left:2px;">' + Globalize.translate('LabelMetadataReaders') + '</div>';
        html += '<ul data-role="listview" data-inset="true" data-mini="true" style="margin-top:.5em;margin-bottom:.5em;">';

        for (var i = 0, length = plugins.length; i < length; i++) {

            var plugin = plugins[i];

            html += '<li data-mini="true" class="localReaderOption" data-pluginname="' + plugin.Name + '">';

            if (i > 0) {
                html += '<a href="#" style="font-size:13px;font-weight:normal;">' + plugin.Name + '</a>';

                html += '<a class="btnLocalReaderUp btnLocalReaderMove" data-pluginindex="' + i + '" href="#" style="font-size:13px;font-weight:normal;" data-icon="arrow-u">' + Globalize.translate('ButtonUp') + '</a>';
            }
            else if (plugins.length > 1) {

                html += '<a href="#" style="font-size:13px;font-weight:normal;">' + plugin.Name + '</a>';

                html += '<a class="btnLocalReaderDown btnLocalReaderMove" data-pluginindex="' + i + '" href="#" style="font-size:13px;font-weight:normal;" data-icon="arrow-d">' + Globalize.translate('ButtonDown') + '</a>';
            }
            else {

                html += plugin.Name;

            }
            html += '</li>';
        }

        html += '</ul>';
        html += '<div class="fieldDescription">' + Globalize.translate('LabelMetadataReadersHelp') + '</div>';

        $('.metadataReaders', page).html(html).show().trigger('create');
    }

    function loadPage(page) {

        loadTabs(page, [

            { name: 'OptionMovies', type: 'Movie' },
            //{ name: 'Trailers', type: 'Trailer' },
            { name: 'OptionCollections', type: 'BoxSet' },
            { name: 'OptionSeries', type: 'Series' },
            { name: 'OptionSeasons', type: 'Season' },
            { name: 'OptionEpisodes', type: 'Episode' },
            { name: 'OptionGames', type: 'Game' },
            { name: 'OptionGameSystems', type: 'GameSystem' },
            //{ name: 'Game Genres', type: 'GameGenre' },
            { name: 'OptionMusicArtists', type: 'MusicArtist' },
            { name: 'OptionMusicAlbums', type: 'MusicAlbum' },
            { name: 'OptionMusicVideos', type: 'MusicVideo' },
            //{ name: 'Music Genres', type: 'MusicGenre' },
            { name: 'OptionSongs', type: 'Audio' },
            { name: 'OptionHomeVideos', type: 'Video' },
            { name: 'OptionBooks', type: 'Book' },
            { name: 'OptionPeople', type: 'Person' }
            //{ name: 'Genres', type: 'Genre' },
            //{ name: 'Studios', type: 'Studio' }
        ]);
    }

    function saveSettingsIntoConfig(form, config) {

        config.DisabledMetadataSavers = $('.chkMetadataSaver:not(:checked)', form).get().map(function (c) {

            return c.getAttribute('data-pluginname');

        });

        config.LocalMetadataReaderOrder = $('.localReaderOption', form).get().map(function (c) {

            return c.getAttribute('data-pluginname');

        });

        config.DisabledMetadataFetchers = $('.chkMetadataFetcher:not(:checked)', form).get().map(function (c) {

            return c.getAttribute('data-pluginname');

        });

        config.MetadataFetcherOrder = $('.chkMetadataFetcher', form).get().map(function (c) {

            return c.getAttribute('data-pluginname');

        });

        config.DisabledImageFetchers = $('.chkImageFetcher:not(:checked)', form).get().map(function (c) {

            return c.getAttribute('data-pluginname');

        });

        config.ImageFetcherOrder = $('.chkImageFetcher', form).get().map(function (c) {

            return c.getAttribute('data-pluginname');

        });

        config.ImageOptions = $('.imageType:visible input', form).get().map(function (c) {


            return {
                Type: $(c).parents('.imageType').attr('data-imagetype'),
                Limit: c.checked ? 1 : 0,
                MinWidth: 0
            };

        });

        config.ImageOptions.push({
            Type: 'Backdrop',
            Limit: $('#txtMaxBackdrops', form).val(),
            MinWidth: $('#txtMinBackdropDownloadWidth', form).val()
        });

        config.ImageOptions.push({
            Type: 'Screenshot',
            Limit: $('#txtMaxScreenshots', form).val(),
            MinWidth: $('#txtMinScreenshotDownloadWidth', form).val()
        });
    }

    function onSubmit() {

        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().then(function (config) {

            var type = currentType;

            var metadataOptions = config.MetadataOptions.filter(function (c) {
                return c.ItemType == type;
            })[0];

            if (metadataOptions) {

                saveSettingsIntoConfig(form, metadataOptions);
                ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult);

            } else {

                ApiClient.getJSON(ApiClient.getUrl("System/Configuration/MetadataOptions/Default")).then(function (defaultOptions) {

                    defaultOptions.ItemType = type;
                    config.MetadataOptions.push(defaultOptions);
                    saveSettingsIntoConfig(form, defaultOptions);
                    ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult);

                });
            }
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#metadataImagesConfigurationPage", function () {

        var page = this;

        $('.metadataReaders', page).on('click', '.btnLocalReaderMove', function () {

            var li = $(this).parents('.localReaderOption');
            var ul = li.parents('ul');

            if ($(this).hasClass('btnLocalReaderDown')) {

                var next = li.next();

                li.remove().insertAfter(next);

            } else {

                var prev = li.prev();

                li.remove().insertBefore(prev);
            }

            $('.localReaderOption', ul).each(function () {

                if ($(this).prev('.localReaderOption').length) {
                    $('.btnLocalReaderMove', this).addClass('btnLocalReaderUp').removeClass('btnLocalReaderDown').attr('data-icon', 'arrow-u').removeClass('ui-icon-arrow-d').addClass('ui-icon-arrow-u');
                } else {
                    $('.btnLocalReaderMove', this).addClass('btnLocalReaderDown').removeClass('btnLocalReaderUp').attr('data-icon', 'arrow-d').removeClass('ui-icon-arrow-u').addClass('ui-icon-arrow-d');
                }

            });

            ul.listview('destroy').listview({});
        });

        $('#selectItemType', page).on('change', function () {

            loadType(page, this.value);
        });

        $('.metadataImagesConfigurationForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#metadataImagesConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        loadPage(page);
    });

})(jQuery, document, window);