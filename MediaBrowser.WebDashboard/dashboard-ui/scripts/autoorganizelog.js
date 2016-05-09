define(['jQuery', 'datetime', 'paper-icon-button-light'], function ($, datetime) {

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

        var message = Globalize.translate('MessageFileWillBeDeleted') + '<br/><br/>' + item.OriginalPath + '<br/><br/>' + Globalize.translate('MessageSureYouWishToProceed');

        require(['confirm'], function (confirm) {

            confirm(message, Globalize.translate('HeaderDeleteFile')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.deleteOriginalFileFromOrganizationResult(id).then(function () {

                    Dashboard.hideLoadingMsg();

                    reloadItems(page);

                }, onApiFailure);
            });
        });
    }

    function organizeFileWithCorrections(page, item) {

        showCorrectionPopup(page, item);
    }

    function showCorrectionPopup(page, item) {

        require(['components/fileorganizer/fileorganizer'], function (fileorganizer) {

            fileorganizer.show(item).then(function () {
                reloadItems(page);
            });
        });
    }

    function organizeFile(page, id) {

        var item = currentResult.Items.filter(function (i) {

            return i.Id == id;
        })[0];

        if (!item.TargetPath) {

            if (item.Type == "Episode") {
                organizeFileWithCorrections(page, item);
            }

            return;
        }

        var message = Globalize.translate('MessageFollowingFileWillBeMovedFrom') + '<br/><br/>' + item.OriginalPath + '<br/><br/>' + Globalize.translate('MessageDestinationTo') + '<br/><br/>' + item.TargetPath;

        if (item.DuplicatePaths.length) {
            message += '<br/><br/>' + Globalize.translate('MessageDuplicatesWillBeDeleted');

            message += '<br/><br/>' + item.DuplicatePaths.join('<br/>');
        }

        message += '<br/><br/>' + Globalize.translate('MessageSureYouWishToProceed');

        require(['confirm'], function (confirm) {

            confirm(message, Globalize.translate('HeaderOrganizeFile')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.performOrganization(id).then(function () {

                    Dashboard.hideLoadingMsg();

                    reloadItems(page);

                }, onApiFailure);
            });
        });
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

            var date = datetime.parseISO8601Date(item.Date, true);
            html += date.toLocaleDateString();

            html += '</td>';

            html += '<td>';
            var status = item.Status;

            if (status == 'SkippedExisting') {
                html += '<a data-resultid="' + item.Id + '" style="color:blue;" href="#" class="btnShowStatusMessage">';
                html += item.OriginalFileName;
                html += '</a>';
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

                html += '<button type="button" is="paper-icon-button-light" data-resultid="' + item.Id + '" class="btnProcessResult organizerButton" title="' + Globalize.translate('ButtonOrganizeFile') + '"><iron-icon icon="folder"></iron-icon></button>';
                html += '<button type="button" is="paper-icon-button-light" data-resultid="' + item.Id + '" class="btnDeleteResult organizerButton" title="' + Globalize.translate('ButtonDeleteFile') + '"><iron-icon icon="delete"></iron-icon></button>';
            }

            html += '</td>';

            html += '</tr>';

            return html;
        }).join('');

        var elem = $('.resultBody', page).html(rows).parents('.tblOrganizationResults').table('refresh').trigger('create');

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

        if ((msg.MessageType == 'ScheduledTaskEnded' && msg.Data.Key == 'AutoOrganize') || msg.MessageType == 'AutoOrganizeUpdate') {

            reloadItems(page);
        }
    }

    function onApiFailure(e) {

        Dashboard.hideLoadingMsg();

        if (e.status == 0) {

            Dashboard.alert({
                title: 'Auto-Organize',
                message: 'The operation is going to take a little longer. The view will be updated on completion.'
            });
        }
        else {

            Dashboard.alert({
                title: Globalize.translate('AutoOrganizeError'),
                message: Globalize.translate('ErrorOrganizingFileWithErrorCode', e.headers.get('X-Application-Error-Code'))
            });
        }
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

    $(document).on('pageinit', "#libraryFileOrganizerLogPage", function () {

        var page = this;

        $('.btnClearLog', page).on('click', function () {

            ApiClient.clearOrganizationLog().then(function () {
                reloadItems(page);
            }, onApiFailure);
        });

    }).on('pageshow', '#libraryFileOrganizerLogPage', function () {

        LibraryMenu.setTabs('autoorganize', 0, getTabs);

        var page = this;

        reloadItems(page);

        // on here
        $('.btnOrganize', page).taskButton({
            mode: 'on',
            progressElem: page.querySelector('.organizeProgress'),
            panel: $('.organizeTaskPanel', page),
            taskKey: 'AutoOrganize'
        });

        Events.on(ApiClient, 'websocketmessage', onWebSocketMessage);

    }).on('pagebeforehide', '#libraryFileOrganizerLogPage', function () {

        var page = this;

        currentResult = null;

        // off here
        $('.btnOrganize', page).taskButton({
            mode: 'off'
        });

        Events.off(ApiClient, 'websocketmessage', onWebSocketMessage);
    });

});