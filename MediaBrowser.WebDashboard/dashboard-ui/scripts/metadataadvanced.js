(function (window, $) {

    function loadAdvancedConfig(page, config) {

        $('#chkSaveMetadataHidden', page).checked(config.SaveMetadataHidden).checkboxradio("refresh");

        $('#txtMetadataPath', page).val(config.MetadataPath || '');

        $('#chkPeopleActors', page).checked(config.PeopleMetadataOptions.DownloadActorMetadata).checkboxradio("refresh");
        $('#chkPeopleComposers', page).checked(config.PeopleMetadataOptions.DownloadComposerMetadata).checkboxradio("refresh");
        $('#chkPeopleDirectors', page).checked(config.PeopleMetadataOptions.DownloadDirectorMetadata).checkboxradio("refresh");
        $('#chkPeopleProducers', page).checked(config.PeopleMetadataOptions.DownloadProducerMetadata).checkboxradio("refresh");
        $('#chkPeopleWriters', page).checked(config.PeopleMetadataOptions.DownloadWriterMetadata).checkboxradio("refresh");
        $('#chkPeopleOthers', page).checked(config.PeopleMetadataOptions.DownloadOtherPeopleMetadata).checkboxradio("refresh");
        $('#chkPeopleGuestStars', page).checked(config.PeopleMetadataOptions.DownloadGuestStarMetadata).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    function loadMetadataConfig(page, config) {

        $('#selectDateAdded', page).val((config.UseFileCreationTimeForDateAdded ? '1' : '0'));
    }

    function loadTmdbConfig(page, config) {

        $('#chkEnableTmdbUpdates', page).checked(config.EnableAutomaticUpdates).checkboxradio("refresh");
    }

    function loadTvdbConfig(page, config) {

        $('#chkEnableTvdbUpdates', page).checked(config.EnableAutomaticUpdates).checkboxradio("refresh");
    }

    function loadFanartConfig(page, config) {

        $('#chkEnableFanartUpdates', page).checked(config.EnableAutomaticUpdates).checkboxradio("refresh");
        $('#txtFanartApiKey', page).val(config.UserApiKey || '');
    }

    function loadChapters(page, config, providers) {

        $('#chkChaptersMovies', page).checked(config.EnableMovieChapterImageExtraction).checkboxradio("refresh");
        $('#chkChaptersEpisodes', page).checked(config.EnableEpisodeChapterImageExtraction).checkboxradio("refresh");
        $('#chkChaptersOtherVideos', page).checked(config.EnableOtherVideoChapterImageExtraction).checkboxradio("refresh");

        $('#chkExtractChaptersDuringLibraryScan', page).checked(config.ExtractDuringLibraryScan).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        var form = this;

        Dashboard.showLoadingMsg();

        saveAdvancedConfig(form);
        saveChapters(form);
        saveMetadata(form);
        saveTmdb(form);
        saveTvdb(form);
        saveFanart(form);

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#advancedMetadataConfigurationPage", function () {

        var page = this;

        $('#btnSelectMetadataPath', page).on("click.selectDirectory", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();
                
                picker.show({

                    callback: function (path) {
                        if (path) {
                            $('#txtMetadataPath', page).val(path);
                        }
                        picker.close();
                    },

                    header: Globalize.translate('HeaderSelectMetadataPath'),

                    instruction: Globalize.translate('HeaderSelectMetadataPathHelp')
                });
            });

        });

        $('.advancedMetadataConfigurationForm').on('submit', onSubmit).on('submit', onSubmit);


    }).on('pageshow', "#advancedMetadataConfigurationPage", function () {

        var page = this;

        ApiClient.getServerConfiguration().then(function (configuration) {

            loadAdvancedConfig(page, configuration);

        });

        ApiClient.getNamedConfiguration("metadata").then(function (metadata) {

            loadMetadataConfig(page, metadata);

        });

        ApiClient.getNamedConfiguration("fanart").then(function (metadata) {

            loadFanartConfig(page, metadata);
        });

        ApiClient.getNamedConfiguration("themoviedb").then(function (metadata) {

            loadTmdbConfig(page, metadata);
        });

        ApiClient.getNamedConfiguration("tvdb").then(function (metadata) {

            loadTvdbConfig(page, metadata);
        });

        var promise1 = ApiClient.getNamedConfiguration("chapters");
        var promise2 = ApiClient.getJSON(ApiClient.getUrl("Providers/Chapters"));

        Promise.all([promise1, promise2]).then(function (responses) {

            loadChapters(page, responses[0], responses[1]);
        });
    });

    function saveFanart(form) {

        ApiClient.getNamedConfiguration("fanart").then(function (config) {

            config.EnableAutomaticUpdates = $('#chkEnableFanartUpdates', form).checked();
            config.UserApiKey = $('#txtFanartApiKey', form).val();

            ApiClient.updateNamedConfiguration("fanart", config);
        });
    }

    function saveTvdb(form) {

        ApiClient.getNamedConfiguration("tvdb").then(function (config) {

            config.EnableAutomaticUpdates = $('#chkEnableTvdbUpdates', form).checked();

            ApiClient.updateNamedConfiguration("tvdb", config);
        });
    }

    function saveTmdb(form) {

        ApiClient.getNamedConfiguration("themoviedb").then(function (config) {

            config.EnableAutomaticUpdates = $('#chkEnableTmdbUpdates', form).checked();

            ApiClient.updateNamedConfiguration("themoviedb", config);
        });
    }

    function saveAdvancedConfig(form) {

        ApiClient.getServerConfiguration().then(function (config) {

            config.SaveMetadataHidden = $('#chkSaveMetadataHidden', form).checked();

            config.EnableTvDbUpdates = $('#chkEnableTvdbUpdates', form).checked();
            config.EnableTmdbUpdates = $('#chkEnableTmdbUpdates', form).checked();
            config.EnableFanArtUpdates = $('#chkEnableFanartUpdates', form).checked();
            config.MetadataPath = $('#txtMetadataPath', form).val();
            config.FanartApiKey = $('#txtFanartApiKey', form).val();

            config.PeopleMetadataOptions.DownloadActorMetadata = $('#chkPeopleActors', form).checked();
            config.PeopleMetadataOptions.DownloadComposerMetadata = $('#chkPeopleComposers', form).checked();
            config.PeopleMetadataOptions.DownloadDirectorMetadata = $('#chkPeopleDirectors', form).checked();
            config.PeopleMetadataOptions.DownloadGuestStarMetadata = $('#chkPeopleGuestStars', form).checked();
            config.PeopleMetadataOptions.DownloadProducerMetadata = $('#chkPeopleProducers', form).checked();
            config.PeopleMetadataOptions.DownloadWriterMetadata = $('#chkPeopleWriters', form).checked();
            config.PeopleMetadataOptions.DownloadOtherPeopleMetadata = $('#chkPeopleOthers', form).checked();

            ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult);
        });
    }

    function saveMetadata(form) {

        ApiClient.getNamedConfiguration("metadata").then(function (config) {

            config.UseFileCreationTimeForDateAdded = $('#selectDateAdded', form).val() == '1';

            ApiClient.updateNamedConfiguration("metadata", config);
        });
    }

    function saveChapters(form) {

        ApiClient.getNamedConfiguration("chapters").then(function (config) {

            config.EnableMovieChapterImageExtraction = $('#chkChaptersMovies', form).checked();
            config.EnableEpisodeChapterImageExtraction = $('#chkChaptersEpisodes', form).checked();
            config.EnableOtherVideoChapterImageExtraction = $('#chkChaptersOtherVideos', form).checked();

            config.ExtractDuringLibraryScan = $('#chkExtractChaptersDuringLibraryScan', form).checked();

            ApiClient.updateNamedConfiguration("chapters", config);
        });
    }

})(window, jQuery);
