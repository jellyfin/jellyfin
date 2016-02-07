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

                ApiClient.deleteOriginalFileFromOrganizationResult(id).then(function () {

                    Dashboard.hideLoadingMsg();

                    reloadItems(page);

                }, onApiFailure);
            }

        });
    }

    function organizeEpsiodeWithCorrections(page, item) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(null, {
            recursive: true,
            includeItemTypes: 'Series',
            sortBy: 'SortName'

        }).then(function (result) {
            Dashboard.hideLoadingMsg();
            showEpisodeCorrectionPopup(page, item, result.Items);
        }, onApiFailure);

    }

    function showEpisodeCorrectionPopup(page, item, allSeries) {

        var popup = $('.episodeCorrectionPopup', page).popup("open");

        $('.inputFile', popup).html(item.OriginalFileName);

        $('#txtSeason', popup).val(item.ExtractedSeasonNumber);
        $('#txtEpisode', popup).val(item.ExtractedEpisodeNumber);
        $('#txtEndingEpisode', popup).val(item.ExtractedEndingEpisodeNumber);

        $('#chkRememberCorrection', popup).val(false);
        $('.extractedName', popup).html(item.ExtractedName);

        $('#hfResultId', popup).val(item.Id);

        var seriesHtml = allSeries.map(function (s) {

            return '<option value="' + s.Id + '">' + s.Name + '</option>';

        }).join('');

        seriesHtml = '<option value=""></option>' + seriesHtml;

        $('#selectSeries', popup).html(seriesHtml);
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

                ApiClient.performOrganization(id).then(function () {

                    Dashboard.hideLoadingMsg();

                    reloadItems(page);

                }, onApiFailure);
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
            EndingEpisodeNumber: $('#txtEndingEpisode', form).val(),
            RememberCorrection: $('#chkRememberCorrection', form).checked()
        };

        ApiClient.performEpisodeOrganization(resultId, options).then(function () {

            Dashboard.hideLoadingMsg();

            $('.episodeCorrectionPopup', page).popup("close");

            reloadItems(page);

        }, onApiFailure);
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getFileOrganizationResults(query).then(function (result) {

            currentResult = result;
            renderResults(page, result);

            Dashboard.hideLoadingMsg();

        }, onApiFailure);

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

        if (enhance) {

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

            html += '<td class="organizerButtonCell">';


            if (item.Status != 'Success') {
                html += '<paper-icon-button data-resultid="' + item.Id + '" icon="folder" class="btnProcessResult organizerButton" title="' + Globalize.translate('ButtonOrganizeFile') + '"></paper-icon-button>';
                html += '<paper-icon-button data-resultid="' + item.Id + '" icon="delete" class="btnDeleteResult organizerButton" title="' + Globalize.translate('ButtonDeleteFile') + '"></paper-icon-button>';
            }

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

        var pagingHtml = LibraryBrowser.getQueryPagingHtml({
            startIndex: query.StartIndex,
            limit: query.Limit,
            totalRecordCount: result.TotalRecordCount,
            showLimit: false,
            updatePageSizeSetting: false
        });

        $(page)[0].querySelector('.listTopPaging').innerHTML = pagingHtml;

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

    function onWebSocketMessage(e, msg) {

        var page = $.mobile.activePage;

        if (msg.MessageType == "ScheduledTaskEnded") {

            var result = msg.Data;

            if (result.Key == 'AutoOrganize') {

                reloadItems(page);
            }
        }
    }

    function onApiFailure(e) {

        Dashboard.hideLoadingMsg();

        Dashboard.alert({
            title: Globalize.translate('AutoOrganizeError'),
            message: Globalize.translate('ErrorOrganizingFileWithErrorCode', e.getResponseHeader("X-Application-Error-Code"))
        });
    }

    function onEpisodeCorrectionFormSubmit() {
        submitEpisodeForm(this);
        return false;
    }

    $(document).on('pageinit', "#libraryFileOrganizerLogPage", function () {

        var page = this;

        $('.btnClearLog', page).on('click', function () {

            ApiClient.clearOrganizationLog().then(function () {
                reloadItems(page);
            }, onApiFailure);

        });

        $('.episodeCorrectionForm').off('submit', onEpisodeCorrectionFormSubmit).on('submit', onEpisodeCorrectionFormSubmit);

    }).on('pageshow', "#libraryFileOrganizerLogPage", function () {

        var page = this;

        reloadItems(page);

        // on here
        $('.btnOrganize', page).taskButton({
            mode: 'on',
            progressElem: page.querySelector('.organizeProgress'),
            panel: $('.organizeTaskPanel', page),
            taskKey: 'AutoOrganize'
        });

        $(ApiClient).on("websocketmessage.autoorganizelog", onWebSocketMessage);

    }).on('pagebeforehide', "#libraryFileOrganizerLogPage", function () {

        var page = this;

        currentResult = null;

        // off here
        $('.btnOrganize', page).taskButton({
            mode: 'off'
        });

        $(ApiClient).off("websocketmessage.autoorganizelog", onWebSocketMessage);
    });

})(jQuery, document, window);
