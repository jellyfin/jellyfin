(function ($, document, window) {

    var query = {

        StartIndex: 0,
        Limit: 20
    };

    var currentResult;

    function showStatusMessage(id) {

        var item = currentResult.Items.filter(function (i) {
            return i.Id == id;

        })[0];

        Dashboard.alert({

            title: getStatusText(item, false),
            message: item.StatusMessage

        });
    }

    function deleteOriginalFile(page, id) {

        var item = currentResult.Items.filter(function (i) {
            return i.Id == id;

        })[0];

        var message = Globalize.translate('MessageFileWillBeDeleted') + '<p style="word-wrap:break-word;">' + item.OriginalPath + '</p><p>' + Globalize.translate('MessageSureYouWishToProceed') + '</p>';

        Dashboard.confirm(message, Globalize.translate('HeaderDeleteFile'), function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                ApiClient.deleteOriginalFileFromOrganizationResult(id).done(function () {

                    Dashboard.hideLoadingMsg();

                    reloadItems(page);

                });
            }

        });
    }

    function organizeEpsiodeWithCorrections(page, item) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems({
            recursive: true,
            includeItemTypes: 'Series',
            sortBy: 'SortName'

        }).done(function (result) {

            Dashboard.hideLoadingMsg();

            showEpisodeCorrectionPopup(page, item, result.Items);
        });

    }

    function showEpisodeCorrectionPopup(page, item, allSeries) {

        var popup = $('.episodeCorrectionPopup', page).popup("open");

        $('.inputFile', popup).html(item.OriginalFileName);

        $('#txtSeason', popup).val(item.ExtractedSeasonNumber);
        $('#txtEpisode', popup).val(item.ExtractedEpisodeNumber);
        $('#txtEndingEpisode', popup).val(item.ExtractedEndingEpisodeNumber);

        $('#hfResultId', popup).val(item.Id);

        var seriesHtml = allSeries.map(function (s) {

            return '<option value="' + s.Id + '">' + s.Name + '</option>';

        }).join('');

        seriesHtml = '<option value=""></option>' + seriesHtml;

        $('#selectSeries', popup).html(seriesHtml).selectmenu('refresh');
    }

    function organizeFile(page, id) {

        var item = currentResult.Items.filter(function (i) {
            return i.Id == id;

        })[0];

        if (!item.TargetPath) {

            if (item.Type == "Episode") {
                organizeEpsiodeWithCorrections(page, item);
            }

            return;
        }

        var message = Globalize.translate('MessageFollowingFileWillBeMovedFrom') + '<p style="word-wrap:break-word;">' + item.OriginalPath + '</p><p>' + Globalize.translate('MessageDestinationTo') + '</p><p style="word-wrap:break-word;">' + item.TargetPath + '</p>';

        if (item.DuplicatePaths.length) {
            message += '<p><b>' + Globalize.translate('MessageDuplicatesWillBeDeleted') + '</b></p>';

            message += '<p style="word-wrap:break-word;">' + item.DuplicatePaths.join('<br/>') + '</p>';
        }

        message += '<p>' + Globalize.translate('MessageSureYouWishToProceed') + '</p>';

        Dashboard.confirm(message, Globalize.translate('HeaderOrganizeFile'), function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                ApiClient.performOrganization(id).done(function () {

                    Dashboard.hideLoadingMsg();

                    reloadItems(page);

                });

            }

        });
    }

    function submitEpisodeForm(form) {

        Dashboard.showLoadingMsg();

        var page = $(form).parents('.page');

        var resultId = $('#hfResultId', form).val();

        var options = {

            SeriesId: $('#selectSeries', form).val(),
            SeasonNumber: $('#txtSeason', form).val(),
            EpisodeNumber: $('#txtEpisode', form).val(),
            EndingEpisodeNumber: $('#txtEndingEpisode', form).val()
        };

        ApiClient.performEpisodeOrganization(resultId, options).done(function () {

            Dashboard.hideLoadingMsg();

            $('.episodeCorrectionPopup', page).popup("close");

            reloadItems(page);

        });
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getFileOrganizationResults(query).done(function (result) {

            currentResult = result;
            renderResults(page, result);

            Dashboard.hideLoadingMsg();
        });

    }

    function getStatusText(item, enhance) {

        var status = item.Status;

        var color = null;

        if (status == 'SkippedExisting') {
            status = Globalize.translate('StatusSkipped');
        }
        else if (status == 'Failure') {
            color = '#cc0000';
            status = Globalize.translate('StatusFailed');
        }
        if (status == 'Success') {
            color = 'green';
            status = Globalize.translate('StatusSuccess');
        }

        if (enhance && enhance) {

            if (item.StatusMessage) {

                return '<a style="color:' + color + ';" data-resultid="' + item.Id + '" href="#" class="btnShowStatusMessage">' + status + '</a>';
            } else {
                return '<span data-resultid="' + item.Id + '" style="color:' + color + ';">' + status + '</span>';
            }
        }


        return status;
    }

    function renderResults(page, result) {

        var rows = result.Items.map(function (item) {

            var html = '';

            html += '<tr>';

            html += '<td class="organizerButtonCell">';


            if (item.Status != 'Success') {
                html += '<button data-resultid="' + item.Id + '" type="button" data-inline="true" data-icon="delete" data-mini="true" data-iconpos="notext" class="btnDeleteResult organizerButton" title="' + Globalize.translate('ButtonDeleteFile') + '">' + Globalize.translate('ButtonDeleteFile') + '</button>';
                html += '<button data-resultid="' + item.Id + '" type="button" data-inline="true" data-icon="action" data-mini="true" data-iconpos="notext" class="btnProcessResult organizerButton" title="' + Globalize.translate('ButtonOrganizeFile') + '">' + Globalize.translate('ButtonOrganizeFile') + '</button>';
            }

            html += '</td>';

            html += '<td>';

            var date = parseISO8601Date(item.Date, { toLocal: true });
            html += date.toLocaleDateString();

            html += '</td>';

            html += '<td>';
            var status = item.Status;

            if (status == 'SkippedExisting') {
                html += '<div style="color:blue;">';
                html += item.OriginalFileName;
                html += '</div>';
            }
            else if (status == 'Failure') {
                html += '<a data-resultid="' + item.Id + '" style="color:red;" href="#" class="btnShowStatusMessage">';
                html += item.OriginalFileName;
                html += '</a>';
            } else {
                html += '<div style="color:green;">';
                html += item.OriginalFileName;
                html += '</div>';
            }
            html += '</td>';

            html += '<td>';
            html += item.TargetPath || '';
            html += '</td>';

            html += '</tr>';

            return html;
        }).join('');

        var elem = $('.resultBody', page).html(rows).parents('.tblOrganizationResults').table("refresh").trigger('create');

        $('.btnShowStatusMessage', elem).on('click', function () {

            var id = this.getAttribute('data-resultid');

            showStatusMessage(id);
        });

        $('.btnProcessResult', elem).on('click', function () {

            var id = this.getAttribute('data-resultid');

            organizeFile(page, id);
        });

        $('.btnDeleteResult', elem).on('click', function () {

            var id = this.getAttribute('data-resultid');

            deleteOriginalFile(page, id);
        });

        var pagingHtml = LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, false, [], false);
        $('.listTopPaging', page).html(pagingHtml).trigger('create');

        if (result.TotalRecordCount > query.Limit && result.TotalRecordCount > 50) {
            $('.listBottomPaging', page).html(pagingHtml).trigger('create');
        } else {
            $('.listBottomPaging', page).empty();
        }

        $('.btnNextPage', page).on('click', function () {
            query.StartIndex += query.Limit;
            reloadItems(page);
        });

        $('.btnPreviousPage', page).on('click', function () {
            query.StartIndex -= query.Limit;
            reloadItems(page);
        });

        if (result.TotalRecordCount) {
            $('.btnClearLog', page).show();
        } else {
            $('.btnClearLog', page).hide();
        }
    }

    $(document).on('pageinit', "#libraryFileOrganizerLogPage", function () {

        var page = this;

        $('.btnClearLog', page).on('click', function () {

            ApiClient.clearOrganizationLog().done(function () {
                reloadItems(page);
            });

        });

    }).on('pageshow', "#libraryFileOrganizerLogPage", function () {

        var page = this;

        reloadItems(page);

        // on here
        $('.btnOrganize', page).taskButton({
            mode: 'on',
            progressElem: $('.organizeProgress', page),
            taskKey: 'AutoOrganize'
        });

    }).on('pagehide', "#libraryFileOrganizerLogPage", function () {

        currentResult = null;

        // off here
        $('.btnOrganize', page).taskButton({
            mode: 'off'
        });
    });

    window.OrganizerLogPage = {

        onEpisodeCorrectionFormSubmit: function () {

            submitEpisodeForm(this);
            return false;
        }
    };

})(jQuery, document, window);
