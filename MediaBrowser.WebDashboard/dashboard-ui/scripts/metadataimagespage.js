(function ($, document, window) {

    function loadTabs(page, tabs) {

        var html = '';

        html += '<div data-role="controlgroup" data-type="horizontal" data-mini="true">';

        for (var i = 0, length = tabs.length; i < length; i++) {

            var tab = tabs[i];

            html += '<input type="radio" name="radioTypeTab" class="radioTypeTab" id="' + tab.type + '" value="' + tab.type + '">';
            html += '<label for="' + tab.type + '">' + tab.name + '</label>';
        }

        html += '</div>';

        var elem = $('.tabs', page).html(html).trigger('create');

        Dashboard.hideLoadingMsg();

        $('.radioTypeTab', elem).on('change', function () {

            if (this.checked) {

                loadType(page, this.id);
            }

        })[0].click();
    }

    function loadType(page, type) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getServerConfiguration();
        var promise2 = $.getJSON(ApiClient.getUrl("System/Configuration/MetadataPlugins"));

        $.when(promise1, promise2).done(function (response1, response2) {

            renderType(page, type, response1[0], response2[0]);

            Dashboard.hideLoadingMsg();
        });

    }

    function renderType(page, type, config, metadataPlugins) {

        var metadataInfo = metadataPlugins.filter(function (f) {

            return type == f.ItemType;
        })[0];

        if (metadataInfo.SupportedImageTypes.indexOf('Backdrop') == -1) {
            $('.backdropFields', page).hide();
        } else {
            $('.backdropFields', page).show();
        }

        if (metadataInfo.SupportedImageTypes.indexOf('Screenshot') == -1) {
            $('.screenshotFields', page).hide();
        } else {
            $('.screenshotFields', page).show();
        }

        $('.imageType', page).each(function () {

            var imageType = this.getAttribute('data-imagetype');

            if (metadataInfo.SupportedImageTypes.indexOf(imageType) == -1) {
                $(this).hide();
            } else {
                $(this).show();
            }
        });

        renderMetadataLocals(page, type, config, metadataInfo);
        renderMetadataFetchers(page, type, config, metadataInfo);
        renderMetadataSavers(page, type, config, metadataInfo);
        renderImageFetchers(page, type, config, metadataInfo);
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

        html += '<fieldset data-role="controlgroup">';
        html += '<legend>Image Fetchers:</legend>';

        for (var i = 0, length = plugins.length; i < length; i++) {

            var plugin = plugins[i];

            var id = 'chkImageFetcher' + i;

            html += '<input type="checkbox" name="' + id + '" id="' + id + '" data-mini="true">';
            html += '<label for="' + id + '">' + plugin.Name + '</label>';
        }

        html += '</fieldset>';

        $('.imageFetchers', page).html(html).show().trigger('create');
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
        html += '<legend>Metadata Savers:</legend>';

        for (var i = 0, length = plugins.length; i < length; i++) {

            var plugin = plugins[i];

            var id = 'chkMetadataSaver' + i;

            html += '<input type="checkbox" name="' + id + '" id="' + id + '" data-mini="true">';
            html += '<label for="' + id + '">' + plugin.Name + '</label>';
        }

        html += '</fieldset>';

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

        html += '<fieldset data-role="controlgroup">';
        html += '<legend>Metadata Fetchers:</legend>';

        for (var i = 0, length = plugins.length; i < length; i++) {

            var plugin = plugins[i];

            var id = 'chkMetadataFetcher' + i;

            html += '<input type="checkbox" name="' + id + '" id="' + id + '" data-mini="true">';
            html += '<label for="' + id + '">' + plugin.Name + '</label>';
        }

        html += '</fieldset>';

        $('.metadataFetchers', page).html(html).show().trigger('create');
    }

    function renderMetadataLocals(page, type, config, metadataInfo) {

        var plugins = metadataInfo.Plugins.filter(function (p) {
            return p.Type == 'LocalMetadataProvider';
        });

        var html = '';

        if (!plugins.length) {
            $('.metadataReaders', page).html(html).hide().trigger('create');
            return;
        }

        html += '<fieldset data-role="controlgroup">';
        html += '<legend>Local Metadata Readers:</legend>';

        for (var i = 0, length = plugins.length; i < length; i++) {

            var plugin = plugins[i];

            var id = 'chkMetadataLocal' + i;

            html += '<input type="checkbox" name="' + id + '" id="' + id + '" data-mini="true">';
            html += '<label for="' + id + '">' + plugin.Name + '</label>';
        }

        html += '</fieldset>';

        $('.metadataReaders', page).html(html).show().trigger('create');
    }

    function loadPage(page) {

        var type = getParameterByName('type');

        $('.categoryTab', page).removeClass('ui-btn-active');

        if (type == 'games') {

            loadTabs(page, [

                { name: 'Game', type: 'Game' },
                { name: 'Game System', type: 'GameSystem' }
            ]);

            $('.gamesTab', page).addClass('ui-btn-active');
        }
        else if (type == 'movies') {

            loadTabs(page, [

                { name: 'Movie', type: 'Movie' },
                { name: 'Trailer', type: 'Trailer' },
                { name: 'Collection', type: 'BoxSet' }
            ]);

            $('.moviesTab', page).addClass('ui-btn-active');
        }
        else if (type == 'tv') {

            loadTabs(page, [

                { name: 'Series', type: 'Series' },
                { name: 'Season', type: 'Season' },
                { name: 'Episode', type: 'Episode' }
            ]);

            $('.tvTab', page).addClass('ui-btn-active');
        }
        else if (type == 'music') {

            loadTabs(page, [

                { name: 'Artist', type: 'MusicArtist' },
                { name: 'Album', type: 'MusicAlbum' },
                { name: 'Song', type: 'Audio' },
                { name: 'Music Video', type: 'MusicVideo' }
            ]);

            $('.musicTab', page).addClass('ui-btn-active');
        }
        else if (type == 'others') {

            loadTabs(page, [

                { name: 'Person', type: 'Person' },
                { name: 'Genre', type: 'Genre' },
                { name: 'Game Genre', type: 'GameGenre' },
                { name: 'Music Genre', type: 'MusicGenre' },
                { name: 'Studio', type: 'Studio' },
                { name: 'Book', type: 'Book' },
                { name: 'Adult Video', type: 'AdultVideo' }
            ]);

            $('.othersTab', page).addClass('ui-btn-active');
        }
    }

    function onSubmit() {

        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageshow', "#metadataImagesConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        loadPage(page);
    });

    window.MetadataImagesPage = {

        onSubmit: onSubmit
    };

})(jQuery, document, window);