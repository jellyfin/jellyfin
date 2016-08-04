define(['dialogHelper', 'paper-checkbox', 'paper-input', 'emby-button', 'paper-icon-button-light'], function (dialogHelper) {

    var extractedName;
    var extractedYear;
    var currentNewItem;
    var existingSeriesHtml;
    var seriesLocationsCount = 0;

    function onApiFailure(e) {

        Dashboard.hideLoadingMsg();

        require(['alert'], function (alert) {
            alert({
                title: Globalize.translate('AutoOrganizeError'),
                text: Globalize.translate('ErrorOrganizingFileWithErrorCode', e.headers.get("X-Application-Error-Code"))
            });
        });
    }

    function initEpisodeForm(context, item) {

        if (!item.ExtractedName || item.ExtractedName.length < 3) {
            context.querySelector('.fldRemember').classList.add('hide');
        }
        else {
            context.querySelector('.fldRemember').classList.remove('hide');
        }

        context.querySelector('.inputFile').innerHTML = item.OriginalFileName;

        context.querySelector('#txtSeason').value = item.ExtractedSeasonNumber;
        context.querySelector('#txtEpisode').value = item.ExtractedEpisodeNumber;
        context.querySelector('#txtEndingEpisode').value = item.ExtractedEndingEpisodeNumber;
        //context.querySelector('.extractedName').innerHTML = item.ExtractedName;

        extractedName = item.ExtractedName;
        extractedYear = item.ExtractedYear;

        context.querySelector('#chkRememberCorrection').checked = false;

        context.querySelector('#hfResultId').value = item.Id;

        ApiClient.getItems(null, {
            recursive: true,
            includeItemTypes: 'Series',
            sortBy: 'SortName'

        }).then(function (result) {

            existingSeriesHtml = result.Items.map(function (s) {

                return '<option value="' + s.Id + '">' + s.Name + '</option>';

            }).join('');

            existingSeriesHtml = '<option value=""></option>' + existingSeriesHtml;

            context.querySelector('#selectSeries').innerHTML = existingSeriesHtml;

            ApiClient.getVirtualFolders().then(function (result) {

                //var movieLocations = [];
                var seriesLocations = [];

                for (var n = 0; n < result.length; n++) {

                    var virtualFolder = result[n];

                    for (var i = 0, length = virtualFolder.Locations.length; i < length; i++) {
                        var location = {
                            value: virtualFolder.Locations[i],
                            display: virtualFolder.Name + ': ' + virtualFolder.Locations[i]
                        };

                        //if (virtualFolder.CollectionType == 'movies') {
                        //    movieLocations.push(location);
                        //}
                        if (virtualFolder.CollectionType == 'tvshows') {
                            seriesLocations.push(location);
                        }
                    }
                }

                seriesLocationsCount = seriesLocations.length;

                var seriesFolderHtml = seriesLocations.map(function (s) {
                    return '<option value="' + s.value + '">' + s.display + '</option>';
                }).join('');

                if (seriesLocations.length > 1) {
                    // If the user has multiple folders, add an empty item to enforce a manual selection
                    seriesFolderHtml = '<option value=""></option>' + seriesFolderHtml;
                }

                context.querySelector('#selectSeriesFolder').innerHTML = seriesFolderHtml;

            }, onApiFailure);

        }, onApiFailure);
    }

    function submitEpisodeForm(dlg) {

        Dashboard.showLoadingMsg();

        var resultId = dlg.querySelector('#hfResultId').value;
        var seriesId = dlg.querySelector('#selectSeries').value;

        var targetFolder;
        var newProviderIds;
        var newSeriesName;
        var newSeriesYear;

        if (seriesId == "##NEW##" && currentNewItem != null) {
            seriesId = null;
            newProviderIds = JSON.stringify(currentNewItem.ProviderIds);
            newSeriesName = currentNewItem.Name;
            newSeriesYear = currentNewItem.ProductionYear;
            targetFolder = dlg.querySelector('#selectSeriesFolder').value;
        }

        var options = {

            SeriesId: seriesId,
            SeasonNumber: dlg.querySelector('#txtSeason').value,
            EpisodeNumber: dlg.querySelector('#txtEpisode').value,
            EndingEpisodeNumber: dlg.querySelector('#txtEndingEpisode').value,
            RememberCorrection: dlg.querySelector('#chkRememberCorrection').checked,
            NewSeriesProviderIds: newProviderIds,
            NewSeriesName: newSeriesName,
            NewSeriesYear: newSeriesYear,
            TargetFolder: targetFolder
        };

        ApiClient.performEpisodeOrganization(resultId, options).then(function () {

            Dashboard.hideLoadingMsg();

            dlg.submitted = true;
            dialogHelper.close(dlg);

        }, onApiFailure);
    }

    function showNewSeriesDialog(dlg) {

        if (seriesLocationsCount == 0) {

            require(['alert'], function (alert) {
                alert({
                    title: Globalize.translate('AutoOrganizeError'),
                    text: Globalize.translate('NoTvFoldersConfigured')
                });
            });
            return;
        }

        require(['itemIdentifier'], function (itemIdentifier) {

            itemIdentifier.showFindNew(extractedName, extractedYear, 'Series', ApiClient.serverId()).then(function (newItem) {

                if (newItem != null) {
                    currentNewItem = newItem;
                    var seriesHtml = existingSeriesHtml;
                    seriesHtml = seriesHtml + '<option selected value="##NEW##">' + currentNewItem.Name + '</option>';
                    dlg.querySelector('#selectSeries').innerHTML = seriesHtml;
                    selectedSeriesChanged(dlg);
                }
            });
        });
    }

    function selectedSeriesChanged(dlg) {
        var seriesId = dlg.querySelector('#selectSeries').value;

        if (seriesId == "##NEW##") {
            dlg.querySelector('.fldSelectSeriesFolder').classList.remove('hide');
            dlg.querySelector('#selectSeriesFolder').setAttribute('required', 'required');
        }
        else {
            dlg.querySelector('.fldSelectSeriesFolder').classList.add('hide');
            dlg.querySelector('#selectSeriesFolder').removeAttribute('required');
        }
    }

    return {
        show: function (item) {
            return new Promise(function (resolve, reject) {

                extractedName = null;
                extractedYear = null;
                currentNewItem = null;
                existingSeriesHtml = null;

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/fileorganizer/fileorganizer.template.html', true);

                xhr.onload = function (e) {

                    var template = this.response;
                    var dlg = dialogHelper.createDialog({
                        removeOnClose: true,
                        size: 'small'
                    });

                    dlg.classList.add('ui-body-a');
                    dlg.classList.add('background-theme-a');

                    dlg.classList.add('formDialog');

                    var html = '';

                    html += Globalize.translateDocument(template);

                    dlg.innerHTML = html;
                    document.body.appendChild(dlg);

                    dlg.querySelector('.dialogHeaderTitle').innerHTML = Globalize.translate('FileOrganizeManually');

                    dialogHelper.open(dlg);

                    dlg.addEventListener('close', function () {

                        if (dlg.submitted) {
                            resolve();
                        } else {
                            reject();
                        }
                    });

                    dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                        dialogHelper.close(dlg);
                    });

                    dlg.querySelector('form').addEventListener('submit', function (e) {

                        submitEpisodeForm(dlg);

                        e.preventDefault();
                        return false;
                    });

                    dlg.querySelector('#btnNewSeries').addEventListener('click', function (e) {

                        showNewSeriesDialog(dlg);
                    });

                    dlg.querySelector('#selectSeries').addEventListener('change', function (e) {

                        selectedSeriesChanged(dlg);
                    });

                    initEpisodeForm(dlg, item);
                }

                xhr.send();
            });
        }
    };
});