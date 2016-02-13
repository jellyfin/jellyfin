define(['paperdialoghelper', 'paper-tabs', 'paper-item', 'paper-input', 'paper-fab', 'paper-item-body'], function (paperDialogHelper) {

    var currentItemId;
    var currentFile;
    var currentDeferred;
    var hasChanges = false;
    var reloadItems;

    function submitEpisodeForm(form) {

        Dashboard.showLoadingMsg();

        var resultId = $('#hfResultId', form).val();

        var targetFolder = $('#spanTargetFolder', form).text();

        var elemString = $('#hfNewSeriesProviderIds', form).val();
        var newSeriesName = $('#hfNewSeriesName', form).val();
        var newSeriesYear = $('#hfNewSeriesYear', form).val();

        var options = {

            SeriesId: $('#selectSeries', form).val(),
            SeasonNumber: $('#txtSeason', form).val(),
            EpisodeNumber: $('#txtEpisode', form).val(),
            EndingEpisodeNumber: $('#txtEndingEpisode', form).val(),
            RememberCorrection: $('#chkRememberCorrection', form).checked(),
            NewSeriesProviderIds: elemString,
            NewSeriesName: newSeriesName,
            NewSeriesYear: newSeriesYear,
            TargetFolder: targetFolder
        };

        ApiClient.performEpisodeOrganization(resultId, options).then(function () {

            Dashboard.hideLoadingMsg();

            document.querySelector('.organizerDialog').close();

            reloadItems();

        }, onApiFailure);
    }

    function submitMovieForm(form) {

        Dashboard.showLoadingMsg();

        var resultId = $('#hfResultIdMovie', form).val();

        var targetFolder = $('#selectMovieFolder', form).val();

        var options = {
            MovieName: $('#txtMovieName', form).val(),
            MovieYear: $('#txtMovieYear', form).val(),
            TargetFolder: targetFolder
        };

        ApiClient.performMovieOrganization(resultId, options).then(function () {

            Dashboard.hideLoadingMsg();

            document.querySelector('.organizerDialog').close();

            reloadItems();

        }, onApiFailure);
    }

    function searchForIdentificationResults(popup, itemtype) {

        var lookupInfo = {
            Name: $('#txtMovieName', popup).val(),
            Year: $('#txtMovieYear', popup).val(),
        };

        var url = ApiClient.getUrl("Items/RemoteSearch/Movie");

        if (itemtype == 'tvshows') {
            lookupInfo.Name = $('#txtNewSeriesName', popup).val();
            lookupInfo.Year = $('#txtNewSeriesYear', popup).val();
            url = ApiClient.getUrl("Items/RemoteSearch/Series");
        }

        if (!lookupInfo.Name) {
            Dashboard.alert(Globalize.translate('MessagePleaseEnterNameOrId'));
            return;
        }

        lookupInfo = {
            SearchInfo: lookupInfo,
            IncludeDisabledProviders: true
        };

        Dashboard.showLoadingMsg();

        ApiClient.ajax({
            type: "POST",
            url: url,
            data: JSON.stringify(lookupInfo),
            dataType: "json",
            contentType: "application/json"

        }).then(function (results) {

            Dashboard.hideLoadingMsg();
            showIdentificationSearchResults(popup, results, itemtype);

            $('#btnBack', popup).off('click').on('click', function () {
                $('.identificationResultForm', popup).hide();
                $('.organizeMovieForm', popup).show();
                $('.createSeriesForm', popup).show();

                if (itemtype == 'tvshows') {
                    $('#btnBack', popup).off('click').on('click', function () {
                        $('.createSeriesForm', popup).hide();
                        $('.episodeCorrectionForm', popup).show();

                        $('#btnBack', popup).off('click').on('click', function () {
                            popup.close();
                        });
                    });
                }
                else {
                    $('#btnBack', popup).off('click').on('click', function () {
                        popup.close();
                    });
                }
            });

        }, onApiFailure);
    }

    function showIdentificationSearchResults(popup, results, itemtype) {

        $('.organizeMovieForm', popup).hide();
        $('.createSeriesForm', popup).hide();
        $('.identificationResultForm', popup).show();

        var html = '';

        for (var i = 0, length = results.length; i < length; i++) {

            var result = results[i];

            html += getIdentifyResultHtml(result, i);
        }

        var elem = $('.identificationSearchResultList', popup).html(html).trigger('create');

        $('.searchImage', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-index'));

            var currentResult = results[index];

            $('.identificationResultForm', popup).hide();

            var targetName = currentResult.Name;
            if (currentResult.ProductionYear) {
                targetName = targetName + ' (' + currentResult.ProductionYear + ')';
            }

            if (itemtype == 'tvshows') {
                $('#txtNewSeriesName', popup).val(currentResult.Name);
                $('#txtNewSeriesYear', popup).val(currentResult.ProductionYear);
                $('#txtSelectedNewSeries', popup).val(targetName);
                $('#txtSelectedNewSeries2', popup).val(targetName);
                $('#hfNewSeriesName', popup).val(currentResult.Name);
                $('#hfNewSeriesYear', popup).val(currentResult.ProductionYear);
                var elems = currentResult.ProviderIds;
                $('#hfNewSeriesProviderIds', popup).val(JSON.stringify(elems));
                $('.createSeriesForm', popup).show();
                $('#btnBack', popup).off('click').on('click', function () {
                    $('.createSeriesForm', popup).hide();
                    $('.episodeCorrectionForm', popup).show();
                    $('#btnBack', popup).off('click').on('click', function () {
                        popup.close();
                    });
                });
            }
            else {
                $('#txtMovieName', popup).val(currentResult.Name);
                $('#txtMovieYear', popup).val(currentResult.ProductionYear);
                $('#txtSelectedMovie', popup).val(targetName);
                $('.organizeMovieForm', popup).show();
                $('#btnBack', popup).off('click').on('click', function () {
                    popup.close();
                });
            }
        });
    }

    function getSearchImageDisplayUrl(url, provider) {
        return ApiClient.getUrl("Items/RemoteSearch/Image", { imageUrl: url, ProviderName: provider });
    }

    function getIdentifyResultHtml(result, index) {

        var html = '';
        var cssClass = "searchImageContainer remoteImageContainer";

        cssClass += " searchPosterImageContainer";

        html += '<div class="' + cssClass + '">';

        if (result.ImageUrl) {
            var displayUrl = getSearchImageDisplayUrl(result.ImageUrl, result.SearchProviderName);

            html += '<a href="#" class="searchImage" data-index="' + index + '" style="background-image:url(\'' + displayUrl + '\');">';
        } else {

            html += '<a href="#" class="searchImage iconCardImage" data-index="' + index + '"><iron-icon icon="search"></iron-icon>';
        }
        html += '</a>';

        html += '<div class="remoteImageDetails" style="background-color: transparent">';
        html += result.Name;
        html += '</div>';

        html += '<div class="remoteImageDetails" style="background-color: transparent">';
        html += result.ProductionYear || '&nbsp;';
        html += '</div>';

        html += '</div>';
        return html;
    }

    function onEpisodeCorrectionFormSubmit() {
        submitEpisodeForm(this);
        return false;
    }

    function onOrganizeMovieFormFormSubmit() {
        submitMovieForm(this);
        return false;
    }

    function showTab(popup, index) {
        $('.organizeMovieForm', popup).show();
        $('.episodeCorrectionForm', popup).show();
        $('.identificationResultForm', popup).hide();
        $('.createSeriesForm', popup).hide();
        $('.popupTabPage', popup).addClass('hide')[index].classList.remove('hide');

        $('#btnBack', popup).off('click').on('click', function () {
            popup.close();
        });
    }

    function initEditor(popup, item, allSeries, movieLocations, seriesLocations) {

        $('#divNewSeries', popup).hide();

        showTab(popup, 0);

        $('.inputFile', popup).html(item.OriginalFileName);

        $('#txtSeason', popup).val(item.ExtractedSeasonNumber);
        $('#txtEpisode', popup).val(item.ExtractedEpisodeNumber);
        $('#txtEndingEpisode', popup).val(item.ExtractedEndingEpisodeNumber);

        $('#chkRememberCorrection', popup).val(false);
        $('.extractedName', popup).html(item.ExtractedName);

        if (!item.ExtractedName || item.ExtractedName.length < 4) {
            $('#divRememberCorrection', popup).hide();
        }

        $('#txtNewSeriesName', popup).val(item.ExtractedName);
        $('#txtNewSeriesYear', popup).val(item.ExtractedYear);

        $('#hfResultId', popup).val(item.Id);
        $('#hfResultIdMovie', popup).val(item.Id);
        $('#hfNewSeriesProviderIds', popup).val(null);
        $('#hfNewSeriesName', popup).val(null);
        $('#hfNewSeriesYear', popup).val(null);

        $('#txtSelectedNewSeries', popup).val(null);
        $('#txtSelectedNewSeries2', popup).val(null);

        $('#txtMovieName', popup).val(item.ExtractedMovieName);
        $('#txtMovieYear', popup).val(item.ExtractedMovieYear);
        $('#txtSelectedMovie', popup).val(null);

        var seriesHtml = allSeries.map(function (s) {

            return '<option value="' + s.Id + '">' + s.Name + '</option>';

        }).join('');

        seriesHtml = '<option value=""></option>' + seriesHtml;

        $('#selectSeries', popup).html(seriesHtml);


        var movieFolderHtml = movieLocations.map(function (s) {
            return '<option value="' + s.value + '">' + s.display + '</option>';
        }).join('');

        if (movieLocations.length > 1) {
            movieFolderHtml = '<option value=""></option>' + movieFolderHtml;
        }

        var seriesFolderHtml = seriesLocations.map(function (s) {
            return '<option value="' + s.value + '">' + s.display + '</option>';
        }).join('');

        if (seriesLocations.length > 1) {
            seriesFolderHtml = '<option value=""></option>' + seriesFolderHtml;
        }

        $('#selectMovieFolder', popup).html(movieFolderHtml);
        $('#selectSeriesFolder', popup).html(seriesFolderHtml);

        $('.episodeCorrectionForm').off('submit', onEpisodeCorrectionFormSubmit).on('submit', onEpisodeCorrectionFormSubmit);
        $('.organizeMovieForm').off('submit', onOrganizeMovieFormFormSubmit).on('submit', onOrganizeMovieFormFormSubmit);

        $('#btnIdentifyMovie', popup).on('click', function () {
            searchForIdentificationResults(popup, 'movies');
        });

        $('#btnIdentifySeries', popup).on('click', function () {
            searchForIdentificationResults(popup, 'tvshows');
        });

        $('.txt-readonly', popup).keydown(function (e) {
            e.preventDefault();
        });

        $('#btnNewSeries', popup).on('click', function () {
            $('.episodeCorrectionForm', popup).hide();
            $('.createSeriesForm', popup).show();

            $('#btnBack', popup).off('click').on('click', function () {

                $('.createSeriesForm', popup).hide();
                $('.episodeCorrectionForm', popup).show();

                $('#btnBack', popup).off('click').on('click', function () {
                    popup.close();
                });
            });
        });

        $('.createSeriesForm').off('submit').on('submit', function () {
            var folder = $('#selectSeriesFolder', popup).val();
            $('#spanTargetFolder', popup).text(folder);

            $('#divSelectSeries', popup).hide();
            $('#divNewSeries', popup).show();
            $('.episodeCorrectionForm', popup).show();
            $('.createSeriesForm', popup).hide();

            $('#btnBack', popup).off('click').on('click', function () {
                popup.close();
            });

            return false;
        });
    }

    function showEditor(page, item, allSeries, movieLocations, seriesLocations, reloadDelegate) {

        reloadItems = reloadDelegate;

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/fileorganizer/fileorganizer.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;

            var dlg = createDialog();
            dlg.setAttribute('id', 'with-backdrop');

            var html = '';
            //html += '<div class="ui-bar-a" style="text-align: left; padding: 10px 15px; margin: 0">';
            html += '<h2 class="dialogHeader">';
            html += '<paper-fab icon="arrow-back" mini class="btnCloseDialog" id="btnBack"></paper-fab>';
            html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + Globalize.translate('FileOrganizeManually') + '</div>';
            html += '</h2>';
            //html += '</div>';

            html += '<div style="padding:0; margin: 10px 0px 0px 0px"><paper-tabs hidescrollbuttons selected="0">';
            html += '<paper-tab id="popupTab1" class="episodeTabButton">TV Episode</paper-tab>';
            html += '<paper-tab id="popupTab2" class="movieTabButton">Movie</paper-tab>';
            html += '</paper-tabs></div>';

            html += '<div class="editorContent" style="margin:auto;">';
            html += Globalize.translateDocument(template);
            html += '</div>';

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            initEditor(dlg, item, allSeries, movieLocations, seriesLocations);

            // Has to be assigned a z-index after the call to .open() 
            $(dlg).on('iron-overlay-closed', onDialogClosed);

            var tabs = dlg.querySelector('paper-tabs');

            $(tabs).on('iron-select', function () {

                var self = this;

                var selected = this.selected;
                showTab(dlg, selected);

                //setTimeout(function () {
                //    Events.trigger(self, 'tabchange');
                //}, 400);

            });
            
            //.on('tabchange', function () {
            //    var selected = this.selected;

            //    showTab(dlg, selected);
            //});

            dlg.classList.add('organizerDialog');

            paperDialogHelper.open(dlg);
            //PaperDialogHelper.openWithHash(dlg, 'fileorganizer');
            //dlg.open();

            $('#btnBack', dlg).on('click', function () {
                paperDialogHelper.close(dlg);
            });
        };

        xhr.send();
    }

    function createDialog() {
        //var dlg = document.createElement('paper-dialog');

        var dlg = paperDialogHelper.createDialog({
            removeOnClose: true
        });

        dlg.classList.add('ui-body-a');
        dlg.classList.add('background-theme-a');

        return dlg;
    }

    function onApiFailure(e) {

        Dashboard.hideLoadingMsg();

        document.querySelector('.organizerDialog').close();

        if (e.status == 0) {
            Dashboard.alert({
                title: 'Auto-Organize',
                message: 'The operation is going to take a little longer. The view will be updated on completion.'
            });
        }
        else {
            Dashboard.alert({
                title: Globalize.translate('AutoOrganizeError'),
                message: Globalize.translate('ErrorOrganizingFileWithErrorCode', e.getResponseHeader("X-Application-Error-Code"))
            });
        }
    }

    function onDialogClosed() {

        $(this).remove();
        Dashboard.hideLoadingMsg();
        currentDeferred.resolveWith(null, [hasChanges]);
    }

    window.FileOrganizer = {
        show: function (page, item, allSeries, movieLocations, seriesLocations, reloadDelegate) {

            var deferred = DeferredBuilder.Deferred();

            currentDeferred = deferred;
            hasChanges = false;

            showEditor(page, item, allSeries, movieLocations, seriesLocations, reloadDelegate);

            return deferred.promise();
        }
    };
});
