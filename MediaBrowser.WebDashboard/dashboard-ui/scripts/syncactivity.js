(function () {

    function cancelJob(page, id) {

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
    }

    function getSyncStatusBanner(job) {

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

        var html = '';
        html += '<div class="syncStatus" secondary data-status="' + job.Status + '" style="color:' + background + ';">';
        html += text;
        html += '</div>';

        return html;
    }

    function getSyncJobHtml(page, job, cardBoxCssClass, syncJobPage) {

        var html = '';

        html += '<paper-icon-item class="syncJobItem" data-id="' + job.Id + '" data-status="' + job.Status + '">';

        if (job.PrimaryImageItemId) {
            var imgUrl = ApiClient.getScaledImageUrl(job.PrimaryImageItemId, {
                type: "Primary",
                width: 40,
                tag: job.PrimaryImageTag,
                minScale: 3
            });
            html += '<paper-fab class="listAvatar blue" style="background-image:url(\'' + imgUrl + '\');background-repeat:no-repeat;background-position:center center;background-size: cover;" item-icon></paper-fab>';
        } else {
            html += '<paper-fab class="listAvatar blue" icon="sync" item-icon></paper-fab>';
        }

        html += '<paper-item-body three-line style="min-height:120px;">';
        syncJobPage += '?id=' + job.Id;
        html += '<a class="clearLink" href="' + syncJobPage + '">';

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

        for (var i = 0, length = textLines.length; i < length; i++) {

            if (i == 0) {
                html += "<div>";
            } else {
                html += "<div secondary>";
            }
            html += textLines[i];
            html += "</div>";
        }

        html += getSyncStatusBanner(job);

        html += '<div secondary class="syncProgresContainer" style="padding-top:5px;">';
        html += '<paper-progress class="mini" style="width:100%;" value="' + (job.Progress || 0) + '"></paper-progress>';
        html += '</div>';

        html += '</a>';
        html += '</paper-item-body>';

        html += '<paper-icon-button icon="' + AppInfo.moreIcon + '" class="btnJobMenu"></paper-icon-button>';

        html += '</paper-icon-item>';

        //html += "<div class='card squareCard'>";

        //html += '<div class="' + cardBoxCssClass + '">';
        //html += '<div class="cardScalable">';

        //html += '<div class="cardPadder"></div>';

        //html += '<a class="cardContent" href="' + syncJobPage + '">';

        //var imgUrl;
        //var style = '';

        //html += '<div class="cardImage coveredCardImage lazy" data-src="' + imgUrl + '" style="' + style + '">';

        //var progress = job.Progress || 0;

        //var footerClass = 'cardFooter fullCardFooter lightCardFooter';

        //if (progress == 0 || progress >= 100) {
        //    footerClass += ' hide';
        //}

        //html += '<div class="' + footerClass + '">';
        //html += "<div class='cardText cardProgress'>";
        //html += '<progress class="itemProgressBar" min="0" max="100" value="' + progress + '"></progress>';
        //html += "</div>";
        //html += "</div>";

        //html += "</div>";

        //// cardContent
        //html += "</a>";

        //// cardScalable
        //html += "</div>";

        //html += '<div class="cardFooter outerCardFooter">';

        //html += '<div class="cardText" style="text-align:right; float:right;padding:0;">';
        //html += '<paper-icon-button icon="' + AppInfo.moreIcon + '" class="btnJobMenu"></paper-icon-button>';
        //html += "</div>";

        //// cardFooter
        //html += "</div>";

        //// cardBox
        //html += "</div>";

        //// card
        //html += "</div>";

        return html;
    }

    var lastDataLoad = 0;

    function loadData(page, jobs) {

        if ((new Date().getTime() - lastDataLoad) < 60000) {
            refreshData(page, jobs);
            return;
        }

        lastDataLoad = new Date().getTime();

        var html = '';
        var lastTargetName = '';

        var cardBoxCssClass = 'cardBox visualCardBox';

        var syncJobPage = 'syncjob.html';
        var showTargetName = true;

        if ($(page).hasClass('mySyncPage')) {
            syncJobPage = 'mysyncjob.html';

            showTargetName = !hasLocalSync();
        }

        for (var i = 0, length = jobs.length; i < length; i++) {

            var job = jobs[i];
            if (showTargetName) {
                var targetName = job.TargetName || 'Unknown';

                if (targetName != lastTargetName) {

                    if (lastTargetName) {
                        html += '</div>';
                        html += '</div>';
                    }

                    lastTargetName = targetName;

                    html += '<div class="syncActivityForTarget">';
                    html += '<h1>' + targetName + '</h1>';
                    html += '<div class="paperList">';
                }
            }

            html += getSyncJobHtml(page, job, cardBoxCssClass, syncJobPage);
        }

        if (jobs.length) {
            html += '</div>';
            html += '</div>';
        }

        var elem = $('.syncActivity', page).html(html).lazyChildren();
        Events.trigger(elem[0], 'create');

        $('.btnJobMenu', elem).on('click', function () {
            showJobMenu(page, this);
        });

        if (!jobs.length) {

            elem.html('<div style="padding:1em .25em;">' + Globalize.translate('MessageNoSyncJobsFound') + '</div>');
        }
    }

    function refreshData(page, jobs) {

        for (var i = 0, length = jobs.length; i < length; i++) {

            var job = jobs[i];
            refreshJob(page, job);
        }
    }

    function refreshJob(page, job) {

        var card = page.querySelector('.syncJobItem[data-id=\'' + job.Id + '\']');

        if (!card) {
            return;
        }

        var banner = card.querySelector('.syncStatus');

        if (banner.getAttribute('data-status') == job.Status) {
            var elem = document.createElement('div');
            elem.innerHTML = getSyncStatusBanner(job);
            elem = elem.querySelector('.syncStatus');
            elem.parentNode.removeChild(elem);

            banner.parentNode.replaceChild(elem, banner);
        }

        var progress = job.Progress || 0;
        var syncProgresContainer = card.querySelector('.syncProgresContainer');

        syncProgresContainer.querySelector('paper-progress').value = progress;
    }

    function showJobMenu(page, elem) {

        var card = $(elem).parents('.syncJobItem');
        var jobId = card.attr('data-id');
        var status = card.attr('data-status');

        var menuItems = [];

        if (status == 'Cancelled') {
            menuItems.push({
                name: Globalize.translate('ButtonDelete'),
                id: 'delete',
                ironIcon: 'delete'
            });
        } else {
            menuItems.push({
                name: Globalize.translate('ButtonCancelSyncJob'),
                id: 'cancel',
                ironIcon: 'delete'
            });
        }

        require(['actionsheet'], function () {

            ActionSheetElement.show({
                items: menuItems,
                positionTo: elem,
                callback: function (id) {

                    switch (id) {

                        case 'delete':
                            cancelJob(page, jobId);
                            break;
                        case 'cancel':
                            cancelJob(page, jobId);
                            break;
                        default:
                            break;
                    }
                }
            });

        });
    }

    function hasLocalSync() {
        return Dashboard.capabilities().SupportsSync;
    }

    function reloadData(page) {

        Dashboard.showLoadingMsg();

        var options = {};

        Dashboard.getCurrentUser().done(function (user) {

            if ($(page).hasClass('mySyncPage')) {
                options.UserId = Dashboard.getCurrentUserId();

                if (hasLocalSync()) {
                    options.TargetId = ApiClient.deviceId();
                }
            }

            ApiClient.getJSON(ApiClient.getUrl('Sync/Jobs', options)).done(function (response) {

                loadData(page, response.Items);

                setTimeout(function () {
                    loadData(page, response.Items);
                }, 2000);
                Dashboard.hideLoadingMsg();

            });
        });
    }

    function onWebSocketMessage(e, msg) {

        var page = $($.mobile.activePage)[0];

        if (msg.MessageType == "SyncJobs") {

            var data = msg.Data;

            if (hasLocalSync()) {
                var targetId = ApiClient.deviceId();
                data = data.filter(function (j) {
                    return j.TargetId == targetId;
                });
            }
            loadData(page, data);
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
        lastDataLoad = 0;

        Dashboard.getPluginSecurityInfo().done(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                $('.supporterPromotionContainer', page).hide();
            } else {
                $('.supporterPromotionContainer', page).show();

                if (AppInfo.enableSupporterMembership) {
                    $('.supporterPromotion a', page).attr('href', 'http://emby.media/donate');
                    $('.supporterPromotion .btnLearnMore', page).show();
                    $('.supporterPromotion .mainText', page).html(Globalize.translate('HeaderSyncRequiresSupporterMembership'));
                } else {
                    $('.supporterPromotion a', page).attr('href', '#');
                    $('.supporterPromotion .btnLearnMore', page).hide();
                    $('.supporterPromotion .mainText', page).html(Globalize.translate('HeaderSyncRequiresSupporterMembershipAppVersion'));
                }
            }
        });

        reloadData(page);

        // on here
        $('.btnSync', page).taskButton({
            mode: 'on',
            progressElem: page.querySelector('.syncProgress'),
            taskKey: 'SyncPrepare'
        });

        startListening(page);
        $(ApiClient).on("websocketmessage", onWebSocketMessage);

    }).on('pagebeforehide', ".syncActivityPage", function () {

        var page = this;

        // off here
        $('.btnSync', page).taskButton({
            mode: 'off'
        });

        stopListening();
        $(ApiClient).off("websocketmessage", onWebSocketMessage);
    });

})();