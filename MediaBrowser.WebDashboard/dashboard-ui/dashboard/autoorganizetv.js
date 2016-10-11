define([], function () {

    function getEpisodeFileName(value, enableMultiEpisode) {

        var seriesName = "Series Name";
        var episodeTitle = "Episode Four";

        var result = value.replace('%sn', seriesName)
            .replace('%s.n', seriesName.replace(' ', '.'))
            .replace('%s_n', seriesName.replace(' ', '_'))
            .replace('%s', '1')
            .replace('%0s', '01')
            .replace('%00s', '001')
            .replace('%ext', 'mkv')
            .replace('%en', episodeTitle)
            .replace('%e.n', episodeTitle.replace(' ', '.'))
            .replace('%e_n', episodeTitle.replace(' ', '_'));

        if (enableMultiEpisode) {
            result = result
            .replace('%ed', '5')
            .replace('%0ed', '05')
            .replace('%00ed', '005');
        }

        return result
            .replace('%e', '4')
            .replace('%0e', '04')
            .replace('%00e', '004');
    }

    function loadPage(view, config) {

        var tvOptions = config.TvOptions;

        view.querySelector('#chkEnableTvSorting').checked = tvOptions.IsEnabled;
        view.querySelector('#chkOverwriteExistingEpisodes').checked = tvOptions.OverwriteExistingEpisodes;
        view.querySelector('#chkDeleteEmptyFolders').checked = tvOptions.DeleteEmptyFolders;

        view.querySelector('#txtMinFileSize').value = tvOptions.MinFileSizeMb;
        view.querySelector('#txtSeasonFolderPattern').value = tvOptions.SeasonFolderPattern;
        view.querySelector('#txtSeasonZeroName').value = tvOptions.SeasonZeroFolderName;
        view.querySelector('#txtWatchFolder').value = tvOptions.WatchLocations[0] || '';

        view.querySelector('#txtEpisodePattern').value = tvOptions.EpisodeNamePattern;
        view.querySelector('#txtMultiEpisodePattern').value = tvOptions.MultiEpisodeNamePattern;

        view.querySelector('#txtDeleteLeftOverFiles').value = tvOptions.LeftOverFileExtensionsToDelete.join(';');

        view.querySelector('#copyOrMoveFile').value = tvOptions.CopyOriginalFile.toString();
    }

    function onSubmit(view) {

        ApiClient.getNamedConfiguration('autoorganize').then(function (config) {

            var tvOptions = config.TvOptions;
            
            tvOptions.IsEnabled = view.querySelector('#chkEnableTvSorting').checked;
            tvOptions.OverwriteExistingEpisodes = view.querySelector('#chkOverwriteExistingEpisodes').checked;
            tvOptions.DeleteEmptyFolders = view.querySelector('#chkDeleteEmptyFolders').checked;

            tvOptions.MinFileSizeMb = view.querySelector('#txtMinFileSize').value;
            tvOptions.SeasonFolderPattern = view.querySelector('#txtSeasonFolderPattern').value;
            tvOptions.SeasonZeroFolderName = view.querySelector('#txtSeasonZeroName').value;

            tvOptions.EpisodeNamePattern = view.querySelector('#txtEpisodePattern').value;
            tvOptions.MultiEpisodeNamePattern = view.querySelector('#txtMultiEpisodePattern').value;

            tvOptions.LeftOverFileExtensionsToDelete = view.querySelector('#txtDeleteLeftOverFiles').value.split(';');

            var watchLocation = view.querySelector('#txtWatchFolder').value;
            tvOptions.WatchLocations = watchLocation ? [watchLocation] : [];

            tvOptions.CopyOriginalFile = view.querySelector('#copyOrMoveFile').value;

            ApiClient.updateNamedConfiguration('autoorganize', config).then(Dashboard.processServerConfigurationUpdateResult, Dashboard.processErrorResponse);
        });

        return false;
    }

    function getTabs() {
        return [
        {
            href: 'autoorganizelog.html',
            name: Globalize.translate('TabActivityLog')
        },
         {
             href: 'autoorganizetv.html',
             name: Globalize.translate('TabTV')
         },
         {
             href: 'autoorganizesmart.html',
             name: Globalize.translate('TabSmartMatches')
         }];
    }

    return function (view, params) {


        function updateSeasonPatternHelp() {

            var value = view.querySelector('#txtSeasonFolderPattern').value;
            value = value.replace('%s', '1').replace('%0s', '01').replace('%00s', '001');

            var replacementHtmlResult = Globalize.translate('OrganizePatternResult').replace('{0}', value);

            view.querySelector('.seasonFolderFieldDescription').innerHTML = replacementHtmlResult;
        }

        function updateEpisodePatternHelp() {

            var value = view.querySelector('#txtEpisodePattern').value;
            var fileName = getEpisodeFileName(value, false);

            var replacementHtmlResult = Globalize.translate('OrganizePatternResult').replace('{0}', fileName);

            view.querySelector('.episodePatternDescription').innerHTML = replacementHtmlResult;
        }

        function updateMultiEpisodePatternHelp() {

            var value = view.querySelector('#txtMultiEpisodePattern').value;
            var fileName = getEpisodeFileName(value, true);

            var replacementHtmlResult = Globalize.translate('OrganizePatternResult').replace('{0}', fileName);

            view.querySelector('.multiEpisodePatternDescription').innerHTML = replacementHtmlResult;
        }

        function selectWatchFolder(e) {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    callback: function (path) {

                        if (path) {

                            view.querySelector('#txtWatchFolder').value = path;
                        }
                        picker.close();
                    },
                    header: Globalize.translate('HeaderSelectWatchFolder'),
                    instruction: Globalize.translate('HeaderSelectWatchFolderHelp')
                });
            });
        }

        view.querySelector('#txtSeasonFolderPattern').addEventListener('change', updateSeasonPatternHelp);
        view.querySelector('#txtSeasonFolderPattern').addEventListener('keyup', updateSeasonPatternHelp);
        view.querySelector('#txtEpisodePattern').addEventListener('change', updateEpisodePatternHelp);
        view.querySelector('#txtEpisodePattern').addEventListener('keyup', updateEpisodePatternHelp);
        view.querySelector('#txtMultiEpisodePattern').addEventListener('change', updateMultiEpisodePatternHelp);
        view.querySelector('#txtMultiEpisodePattern').addEventListener('keyup', updateMultiEpisodePatternHelp);
        view.querySelector('#btnSelectWatchFolder').addEventListener('click', selectWatchFolder);

        view.querySelector('.libraryFileOrganizerForm').addEventListener('submit', function (e) {

            e.preventDefault();
            onSubmit(view);
            return false;
        });

        view.addEventListener('viewshow', function (e) {

            LibraryMenu.setTabs('autoorganize', 1, getTabs);

            ApiClient.getNamedConfiguration('autoorganize').then(function (config) {
                loadPage(view, config);
                updateSeasonPatternHelp();
                updateEpisodePatternHelp();
                updateMultiEpisodePatternHelp();
            });
        });
    };
});