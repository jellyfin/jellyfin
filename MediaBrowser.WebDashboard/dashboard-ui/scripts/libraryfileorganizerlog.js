(function ($, document, window) {

    var query = {

        StartIndex: 0,
        Limit: 50
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

        var message = 'The following file will be deleted:<p>' + item.OriginalPath + '</p><p>Are you sure you wish to proceed?</p>';

        Dashboard.confirm(message, "Delete File", function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();
                
                ApiClient.deleteOriginalFileFromOrganizationResult(id).done(function () {

                    Dashboard.hideLoadingMsg();

                    reloadItems(page);

                });
            }

        });
    }

    function organizeFile(page, id) {

        var item = currentResult.Items.filter(function (i) {
            return i.Id == id;

        })[0];

        var message = 'The following file will be moved from:<p>' + item.OriginalPath + '</p><p>to:</p><p>' + item.TargetPath + '</p><p>Are you sure you wish to proceed?</p>';

        Dashboard.confirm(message, "Organize File", function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                ApiClient.performOrganization(id).done(function () {

                    Dashboard.hideLoadingMsg();

                    reloadItems(page);

                });

            }

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
            status = 'Skipped';
        }
        else if (status == 'Failure') {
            color = '#cc0000';
            status = 'Failed';
        }
        if (status == 'Success') {
            color = 'green';
            status = 'Success';
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

            html += '<td>';

            var date = parseISO8601Date(item.Date, { toLocal: true });
            html += date.toLocaleDateString();

            html += '</td>';

            html += '<td>';
            html += item.OriginalFileName || '';
            html += '</td>';

            html += '<td>';
            html += item.TargetPath || '';
            html += '</td>';

            html += '<td>';
            html += getStatusText(item, true);
            html += '</td>';

            html += '<td class="organizerButtonCell">';


            if (item.Status == 'SkippedExisting') {
                html += '<button data-resultid="' + item.Id + '" type="button" data-inline="true" data-icon="action" data-mini="true" data-iconpos="notext" class="btnProcessResult organizerButton" title="Organize File">Process</button>';
            } else {
                html += '<button style="visibility:hidden;" type="button" data-inline="true" data-icon="info" data-mini="true" data-iconpos="notext" class="organizerButton"></button>';
            }

            if (item.Status != 'Success') {
                html += '<button data-resultid="' + item.Id + '" type="button" data-inline="true" data-icon="delete" data-mini="true" data-iconpos="notext" class="btnDeleteResult organizerButton" title="Delete">Delete File</button>';
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

        var pagingHtml = LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, false, [], false);
        $('.listTopPaging', page).html(pagingHtml).trigger('create');

        if (result.TotalRecordCount > query.Limit && result.TotalRecordCount > 50) {
            $('.listBottomPaging', page).html(pagingHtml).trigger('create');
        } else {
            $('.listBottomPaging', page).empty();
        }

        $('.selectPage', page).on('change', function () {
            query.StartIndex = (parseInt(this.value) - 1) * query.Limit;
            reloadItems(page);
        });

        $('.btnNextPage', page).on('click', function () {
            query.StartIndex += query.Limit;
            reloadItems(page);
        });

        $('.btnPreviousPage', page).on('click', function () {
            query.StartIndex -= query.Limit;
            reloadItems(page);
        });
    }

    $(document).on('pageshow', "#libraryFileOrganizerLogPage", function () {

        var page = this;

        reloadItems(page);

    }).on('pagehide', "#libraryFileOrganizerLogPage", function () {

        currentResult = null;
    });

})(jQuery, document, window);
