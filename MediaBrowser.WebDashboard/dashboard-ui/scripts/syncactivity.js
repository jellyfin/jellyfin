(function () {

    function cancelJob(page, id) {

        $('.jobMenu', page).on("popupafterclose.deleteuser", function () {

            $(this).off('popupafterclose.deleteuser');

            var msg = Globalize.translate('CancelSyncJobConfirmation');

            Dashboard.confirm(msg, Globalize.translate('HeaderCancelSyncJob'), function (result) {

                if (result) {
                    Dashboard.showLoadingMsg();

                    ApiClient.ajax({

                        url: ApiClient.getUrl('Sync/Jobs/' + id),
                        type: 'DELETE'

                    }).done(function () {

                        reloadData(page);
                    });
                }
            });

        }).popup('close');
    }

    function getSyncJobHtml(page, job, cardBoxCssClass, syncJobPage) {

        var html = '';

        html += "<div class='card squareCard' data-id='" + job.Id + "' data-status='" + job.Status + "'>";

        html += '<div class="' + cardBoxCssClass + '">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        syncJobPage += '?id=' + job.Id;

        html += '<a class="cardContent" href="' + syncJobPage + '">';

        var imgUrl;
        var style = '';

        if (job.PrimaryImageItemId) {
            imgUrl = ApiClient.getScaledImageUrl(job.PrimaryImageItemId, {
                type: "Primary",
                width: 400,
                tag: job.PrimaryImageTag
            });
            style = "background-position:center center;";
        } else {
            style = "background-color:#38c;background-position:center center;";
            imgUrl = "css/images/items/detail/video.png";
        }

        html += '<div class="cardImage coveredCardImage lazy" data-src="' + imgUrl + '" style="' + style + '">';

        if (job.Progress && job.Progress < 100) {
            html += '<div class="cardFooter">';
            html += "<div class='cardText cardProgress'>";
            html += '<progress class="itemProgressBar" min="0" max="100" value="' + job.Progress + '"></progress>';
            html += "</div>";
            html += "</div>";
        }

        html += "</div>";

        var opacity = '.85';
        var background = 'rgba(204,51,51,' + opacity + ')';
        var text = Globalize.translate('SyncJobStatus' + job.Status);

        if (job.Status == 'Completed') {
            background = 'rgba(82, 181, 75, ' + opacity + ')';
        }
        else if (job.Status == 'CompletedWithError') {

        }
        else if (job.Status == 'Queued') {
            background = 'rgba(51, 136, 204, ' + opacity + ')';
        }
        else if (job.Status == 'ReadyToTransfer') {
            background = 'rgba(51, 136, 204, ' + opacity + ')';
        }
        else if (job.Status == 'Transferring') {
            background = 'rgba(72, 0, 255, ' + opacity + ')';
        }
        else if (job.Status == 'Converting') {
            background = 'rgba(255, 106, 0, ' + opacity + ')';
        }

        html += '<div class="syncStatusBanner" style="background-color:' + background + ';position:absolute;top:0;right:0;padding:.5em .5em; text-align:left;color: #fff; font-weight: 500; text-transform:uppercase; border-bottom-left-radius: 3px;">';
        html += text;
        html += '</div>';

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        var textLines = [];

        if (job.ParentName) {
            textLines.push(job.ParentName);
        }

        textLines.push(job.Name);

        if (job.ItemCount == 1) {
            textLines.push(Globalize.translate('ValueItemCount', job.ItemCount));
        } else {
            textLines.push(Globalize.translate('ValueItemCountPlural', job.ItemCount));
        }

        if (!job.ParentName) {
            textLines.push('&nbsp;');
        }

        html += '<div class="cardText" style="text-align:right; position:absolute; bottom:5px; right: 5px;">';
        html += '<button class="btnJobMenu" type="button" data-inline="true" data-iconpos="notext" data-icon="ellipsis-v" style="margin: 0 0 0;"></button>';
        html += "</div>";

        for (var i = 0, length = textLines.length; i < length; i++) {
            html += "<div class='cardText' style='margin-right:30px;'>";
            html += textLines[i];
            html += "</div>";
        }

        // cardFooter
        html += "</div>";

        // cardBox
        html += "</div>";

        // card
        html += "</div>";

        return html;
    }

    function loadData(page, jobs) {

        var html = '';
        var lastTargetName = '';

        var cardBoxCssClass = 'cardBox visualCardBox';

        var syncJobPage = 'syncjob.html';

        if ($(page).hasClass('mySyncPage')) {
            syncJobPage = 'mysyncjob.html';
        }

        for (var i = 0, length = jobs.length; i < length; i++) {

            var job = jobs[i];
            var targetName = job.TargetName;

            if (targetName != lastTargetName) {

                if (lastTargetName) {
                    html += '<br/>';
                    html += '<br/>';
                    html += '<br/>';
                }

                lastTargetName = targetName;

                html += '<div class="detailSectionHeader" style="padding: .85em 0 .85em 1em;">';

                html += '<div style="display:inline-block;vertical-align:middle;">' + targetName + '</div>';

                html += '</div>';
            }

            html += getSyncJobHtml(page, job, cardBoxCssClass, syncJobPage);
        }

        var elem = $('.syncActivity', page).html(html).trigger('create');

        $(".lazy", elem).unveil(200);

        $('.btnJobMenu', elem).on('click', function () {
            showJobMenu(this);
        });

        if (!jobs.length) {

            elem.html('<div style="padding:1em .25em;">' + Globalize.translate('MessageNoSyncJobsFound') + '</div>');
        }
    }

    function showJobMenu(elem) {

        var card = $(elem).parents('.card');
        var page = $(elem).parents('.page');
        var id = card.attr('data-id');
        var status = card.attr('data-status');

        $('.jobMenu', page).popup("close").remove();

        var html = '<div data-role="popup" class="jobMenu tapHoldMenu" data-theme="a">';

        html += '<ul data-role="listview" style="min-width: 180px;">';
        html += '<li data-role="list-divider">' + Globalize.translate('HeaderMenu') + '</li>';

        if (status == 'Cancelled') {
            html += '<li data-icon="delete"><a href="#" class="btnCancelJob" data-id="' + id + '">' + Globalize.translate('ButtonDelete') + '</a></li>';
        } else {
            html += '<li data-icon="delete"><a href="#" class="btnCancelJob" data-id="' + id + '">' + Globalize.translate('ButtonCancel') + '</a></li>';
        }

        html += '</ul>';

        html += '</div>';

        page.append(html);

        var flyout = $('.jobMenu', page).popup({ positionTo: elem || "window" }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();

        });

        $('.btnCancelJob', flyout).on('click', function () {
            cancelJob(page, this.getAttribute('data-id'));
        });
    }

    function reloadData(page) {

        Dashboard.showLoadingMsg();

        var options = {};

        Dashboard.getCurrentUser().done(function (user) {

            if ($(page).hasClass('mySyncPage')) {
                options.UserId = Dashboard.getCurrentUserId();
            }

            ApiClient.getJSON(ApiClient.getUrl('Sync/Jobs', options)).done(function (response) {

                loadData(page, response.Items);

                Dashboard.hideLoadingMsg();

            });
        });
    }

    function onWebSocketMessage(e, msg) {

        var page = $.mobile.activePage;

        if (msg.MessageType == "SyncJobs") {
            loadData(page, msg.Data);
        }
    }

    function startListening(page) {

        var startParams = "0,1500";

        if ($(page).hasClass('mySyncPage')) {
            startParams += "," + Dashboard.getCurrentUserId();
        }

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("SyncJobsStart", startParams);
        }

    }

    function stopListening() {

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("SyncJobsStop", "");
        }

    }

    $(document).on('pageshow', ".syncActivityPage", function () {

        var page = this;

        Dashboard.getPluginSecurityInfo().done(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                $('.syncPromotion', page).hide();
            } else {
                $('.syncPromotion', page).show();
            }
        });

        reloadData(page);

        // on here
        $('.btnSync', page).taskButton({
            mode: 'on',
            progressElem: $('.syncProgress', page),
            taskKey: 'SyncPrepare'
        });

        startListening(page);
        $(ApiClient).on("websocketmessage.syncactivity", onWebSocketMessage);

    }).on('pagehide', ".syncActivityPage", function () {

        var page = this;

        // off here
        $('.btnSync', page).taskButton({
            mode: 'off'
        });

        stopListening();
        $(ApiClient).off(".syncactivity");
    });

})();