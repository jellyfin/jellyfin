define(['paperdialoghelper', 'paper-checkbox', 'paper-input', 'paper-button'], function (paperDialogHelper) {

    var extractedName;
    var extractedYear;
    var currentNewItem;
    var existingSeriesHtml;

    function onApiFailure(e) {

        Dashboard.hideLoadingMsg();

        Dashboard.alert({
            title: Globalize.translate('AutoOrganizeError'),
            message: Globalize.translate('ErrorOrganizingFileWithErrorCode', e.getResponseHeader("X-Application-Error-Code"))
        });
    }

    function initEpisodeForm(context, item) {

        if (!item.ExtractedName || item.ExtractedName.length < 4) {
            context.querySelector('.fldRemember').classList.add('hide');
        }
        else {
            context.querySelector('.fldRemember').classList.remove('hide');
        }

        context.querySelector('.inputFile').innerHTML = item.OriginalFileName;

        context.querySelector('#txtSeason').value = item.ExtractedSeasonNumber;
        context.querySelector('#txtEpisode').value = item.ExtractedEpisodeNumber;
        context.querySelector('#txtEndingEpisode').value = item.ExtractedEndingEpisodeNumber;
        context.querySelector('.extractedName').value = item.ExtractedName;

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

        }, onApiFailure);
    }

    function submitEpisodeForm(dlg) {

        Dashboard.showLoadingMsg();

        var resultId = dlg.querySelector('#hfResultId').value;

        var options = {

            SeriesId: dlg.querySelector('#selectSeries').value,
            SeasonNumber: dlg.querySelector('#txtSeason').value,
            EpisodeNumber: dlg.querySelector('#txtEpisode').value,
            EndingEpisodeNumber: dlg.querySelector('#txtEndingEpisode').value,
            RememberCorrection: dlg.querySelector('#chkRememberCorrection').checked
        };

        ApiClient.performEpisodeOrganization(resultId, options).then(function () {

            Dashboard.hideLoadingMsg();

            dlg.submitted = true;
            paperDialogHelper.close(dlg);

        }, onApiFailure);
    }

    function showNewSeriesDialog(dlg) {

        require(['components/itemidentifier/itemidentifier'], function (itemidentifier) {

            itemidentifier.showFindNew(extractedName, extractedYear, 'Series').then(function (newItem) {

                if (newItem != null) {
                    currentNewItem = newItem;
                    var seriesHtml = existingSeriesHtml;
                    seriesHtml = seriesHtml + '<option selected value="##NEW##">' + currentNewItem.Name + '</option>';
                    dlg.querySelector('#selectSeries').innerHTML = seriesHtml;
                }
            });
        });
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
                    var dlg = paperDialogHelper.createDialog({
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

                    paperDialogHelper.open(dlg);

                    dlg.addEventListener('iron-overlay-closed', function () {

                        if (dlg.submitted) {
                            resolve();
                        } else {
                            reject();
                        }
                    });

                    dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                        paperDialogHelper.close(dlg);
                    });

                    dlg.querySelector('form').addEventListener('submit', function (e) {

                        submitEpisodeForm(dlg);

                        e.preventDefault();
                        return false;
                    });

                    dlg.querySelector('#btnNewSeries').addEventListener('click', function (e) {

                        showNewSeriesDialog(dlg);
                    });

                    initEpisodeForm(dlg, item);
                }

                xhr.send();
            });
        }
    };
});