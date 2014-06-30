(function ($, document, window) {

    function loadPage(page, config, providers) {

        if (providers.length) {
            $('.noChapterProviders', page).hide();
            $('.chapterDownloadSettings', page).show();
        } else {
            $('.noChapterProviders', page).show();
            $('.chapterDownloadSettings', page).hide();
        }

        $('#chkChaptersMovies', page).checked(config.EnableMovieChapterImageExtraction).checkboxradio("refresh");
        $('#chkChaptersEpisodes', page).checked(config.EnableEpisodeChapterImageExtraction).checkboxradio("refresh");
        $('#chkChaptersOtherVideos', page).checked(config.EnableOtherVideoChapterImageExtraction).checkboxradio("refresh");

        $('#chkDownloadChapterMovies', page).checked(config.DownloadMovieChapters).checkboxradio("refresh");
        $('#chkDownloadChapterEpisodes', page).checked(config.DownloadEpisodeChapters).checkboxradio("refresh");

        renderChapterFetchers(page, config, providers);

        Dashboard.hideLoadingMsg();
    }

    function renderChapterFetchers(page, config, plugins) {

        var html = '';

        if (!plugins.length) {
            $('.chapterFetchers', page).html(html).hide().trigger('create');
            return;
        }

        var i, length, plugin, id;

        html += '<div class="ui-controlgroup-label" style="margin-bottom:0;padding-left:2px;">';
        html += Globalize.translate('LabelChapterDownloaders');
        html += '</div>';

        html += '<div style="display:inline-block;width: 75%;vertical-align:top;">';
        html += '<div data-role="controlgroup" class="chapterFetcherGroup">';

        for (i = 0, length = plugins.length; i < length; i++) {

            plugin = plugins[i];

            id = 'chkChapterFetcher' + i;

            var isChecked = config.DisabledFetchers.indexOf(plugin.Name) == -1 ? ' checked="checked"' : '';

            html += '<input class="chkChapterFetcher" type="checkbox" name="' + id + '" id="' + id + '" data-pluginname="' + plugin.Name + '" data-mini="true"' + isChecked + '>';
            html += '<label for="' + id + '">' + plugin.Name + '</label>';
        }

        html += '</div>';
        html += '</div>';

        if (plugins.length > 1) {
            html += '<div style="display:inline-block;vertical-align:top;margin-left:5px;">';

            for (i = 0, length = plugins.length; i < length; i++) {

                html += '<div style="margin:6px 0;">';
                if (i == 0) {
                    html += '<button data-inline="true" disabled="disabled" class="btnUp" data-pluginindex="' + i + '" type="button" data-icon="arrow-u" data-mini="true" data-iconpos="notext" style="margin: 0 1px;">Up</button>';
                    html += '<button data-inline="true" class="btnDown" data-pluginindex="' + i + '" type="button" data-icon="arrow-d" data-mini="true" data-iconpos="notext" style="margin: 0 1px;">Down</button>';
                } else if (i == (plugins.length - 1)) {
                    html += '<button data-inline="true" class="btnUp" data-pluginindex="' + i + '" type="button" data-icon="arrow-u" data-mini="true" data-iconpos="notext" style="margin: 0 1px;">Up</button>';
                    html += '<button data-inline="true" disabled="disabled" class="btnDown" data-pluginindex="' + i + '" type="button" data-icon="arrow-d" data-mini="true" data-iconpos="notext" style="margin: 0 1px;">Down</button>';
                }
                else {
                    html += '<button data-inline="true" class="btnUp" data-pluginindex="' + i + '" type="button" data-icon="arrow-u" data-mini="true" data-iconpos="notext" style="margin: 0 1px;">Up</button>';
                    html += '<button data-inline="true" class="btnDown" data-pluginindex="' + i + '" type="button" data-icon="arrow-d" data-mini="true" data-iconpos="notext" style="margin: 0 1px;">Down</button>';
                }
                html += '</div>';
            }
        }

        html += '</div>';
        html += '<div class="fieldDescription">' + Globalize.translate('LabelChapterDownloadersHelp') + '</div>';

        var elem = $('.chapterFetchers', page).html(html).show().trigger('create');

        $('.btnDown', elem).on('click', function () {
            var index = parseInt(this.getAttribute('data-pluginindex'));

            var elemToMove = $('.chapterFetcherGroup .ui-checkbox', page)[index];

            var insertAfter = $(elemToMove).next('.ui-checkbox')[0];

            elemToMove.parentNode.removeChild(elemToMove);
            $(elemToMove).insertAfter(insertAfter);

            $('.chapterFetcherGroup', page).controlgroup('destroy').controlgroup();
        });

        $('.btnUp', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-pluginindex'));

            var elemToMove = $('.chapterFetcherGroup .ui-checkbox', page)[index];

            var insertBefore = $(elemToMove).prev('.ui-checkbox')[0];

            elemToMove.parentNode.removeChild(elemToMove);
            $(elemToMove).insertBefore(insertBefore);

            $('.chapterFetcherGroup', page).controlgroup('destroy').controlgroup();
        });
    }


    $(document).on('pageshow', "#chapterMetadataConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getNamedConfiguration("chapters");
        var promise2 = $.getJSON(ApiClient.getUrl("Providers/Chapters"));

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);
        });
    });

    function metadataChaptersPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getNamedConfiguration("chapters").done(function (config) {

                config.EnableMovieChapterImageExtraction = $('#chkChaptersMovies', form).checked();
                config.EnableEpisodeChapterImageExtraction = $('#chkChaptersEpisodes', form).checked();
                config.EnableOtherVideoChapterImageExtraction = $('#chkChaptersOtherVideos', form).checked();

                config.DownloadMovieChapters = $('#chkDownloadChapterMovies', form).checked();
                config.DownloadEpisodeChapters = $('#chkDownloadChapterEpisodes', form).checked();

                config.DisabledFetchers = $('.chkChapterFetcher:not(:checked)', form).get().map(function (c) {

                    return c.getAttribute('data-pluginname');

                });

                config.FetcherOrder = $('.chkChapterFetcher', form).get().map(function (c) {

                    return c.getAttribute('data-pluginname');

                });

                ApiClient.updateNamedConfiguration("chapters", config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.MetadataChaptersPage = new metadataChaptersPage();

})(jQuery, document, window);
