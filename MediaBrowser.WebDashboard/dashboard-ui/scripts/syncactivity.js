define(['jQuery', 'paper-icon-button-light', 'cardStyle'], function ($) {

    function cancelJob(page, id) {

        var msg = Globalize.translate('CancelSyncJobConfirmation');

        require(['confirm'], function (confirm) {

            confirm(msg, Globalize.translate('HeaderCancelSyncJob')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({

                    url: ApiClient.getUrl('Sync/Jobs/' + id),
                    type: 'DELETE'

                }).then(function () {

                    reloadData(page);
                });
            });
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
        html += '<div class="syncStatusBanner" data-status="' + job.Status + '" style="background-color:' + background + ';position:absolute;top:0;right:0;padding:.5em .5em; text-align:left;color: #fff; font-weight: 500; text-transform:uppercase; border-bottom-left-radius: 3px;">';
        html += text;
        html += '</div>';

        return html;
    }

    function getSyncJobHtml(page, job, cardBoxCssClass, syncJobPage) {

        var html = '';

        html += "<div class='card squareCard scalableCard' data-id='" + job.Id + "' data-status='" + job.Status + "'>";

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

        var progress = job.Progress || 0;

        var footerClass = 'cardFooter fullCardFooter lightCardFooter';

        if (progress == 0 || progress >= 100) {
            footerClass += ' hide';
        }

        html += '<div class="' + footerClass + '">';
        html += "<div class='cardText cardProgress'>";
        html += '<progress class="itemProgressBar" min="0" max="100" value="' + progress + '"></progress>';
        html += "</div>";
        html += "</div>";

        html += "</div>";

        html += getSyncStatusBanner(job);

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter outerCardFooter">';

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

        html += '<div class="cardText" style="text-align:right; float:right;padding:0;">';
        html += '<button type="button" is="paper-icon-button-light" class="btnJobMenu autoSize"><i class="md-icon">' + AppInfo.moreIcon.replace('-', '_') + '</i></button>';
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

        var hasOpenSection = false;

        for (var i = 0, length = jobs.length; i < length; i++) {

            var job = jobs[i];
            if (showTargetName) {
                var targetName = job.TargetName || 'Unknown';

                if (targetName != lastTargetName) {

                    if (lastTargetName) {
                        html += '</div>';
                        html += '<br/>';
                        html += '<br/>';
                        html += '<br/>';
                        hasOpenSection = false;
                    }

                    lastTargetName = targetName;

                    html += '<div class="detailSectionHeader">';

                    html += '<div>' + targetName + '</div>';

                    html += '</div>';
                    html += '<div class="itemsContainer vertical-wrap">';
                    hasOpenSection = true;
                }
            }

            html += getSyncJobHtml(page, job, cardBoxCssClass, syncJobPage);
        }

        if (hasOpenSection) {
            html += '</div>';
        }

        var elem = $('.syncActivity', page).html(html).lazyChildren();

        $('.btnJobMenu', elem).on('click', function () {
            showJobMenu(page, this);
        });

        if (!jobs.length) {

            elem.html('<div style="padding:1em .25em;">' + Globalize.translate('MessageNoSyncJobsFound') + '</div>');
        }
    }

    $.fn.lazyChildren = function () {

        for (var i = 0, length = this.length; i < length; i++) {
            ImageLoader.lazyChildren(this[i]);
        }
        return this;
    };

    function refreshData(page, jobs) {

        for (var i = 0, length = jobs.length; i < length; i++) {

            var job = jobs[i];
            refreshJob(page, job);
        }
    }

    function refreshJob(page, job) {

        var card = page.querySelector('.card[data-id=\'' + job.Id + '\']');

        if (!card) {
            return;
        }

        var banner = card.querySelector('.syncStatusBanner');

        if (banner.getAttribute('data-status') == job.Status) {
            var elem = document.createElement('div');
            elem.innerHTML = getSyncStatusBanner(job);
            elem = elem.querySelector('.syncStatusBanner');
            elem.parentNode.removeChild(elem);

            banner.parentNode.replaceChild(elem, banner);
        }

        var progress = job.Progress || 0;
        var cardFooter = card.querySelector('.cardFooter');

        if (progress == 0 || progress >= 100) {
            cardFooter.classList.add('hide');
        }
        else {
            cardFooter.classList.remove('hide');
            cardFooter.querySelector('.itemProgressBar').value = progress;
        }
    }

    function showJobMenu(page, elem) {

        var card = $(elem).parents('.card');
        var jobId = card.attr('data-id');
        var status = card.attr('data-status');

        var menuItems = [];

        if (status == 'Cancelled') {
            menuItems.push({
                name: Globalize.translate('ButtonDelete'),
                id: 'delete'
            });
        } else {
            menuItems.push({
                name: Globalize.translate('ButtonCancelSyncJob'),
                id: 'cancel'
            });
        }

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
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

        lastDataLoad = 0;
        Dashboard.showLoadingMsg();

        var options = {};

        Dashboard.getCurrentUser().then(function (user) {

            if ($(page).hasClass('mySyncPage')) {
                options.UserId = Dashboard.getCurrentUserId();

                if (hasLocalSync()) {
                    options.TargetId = ApiClient.deviceId();
                }
            }

            ApiClient.getJSON(ApiClient.getUrl('Sync/Jobs', options)).then(function (response) {

                loadData(page, response.Items);
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

    function getTabs() {
        return [
        {
            href: 'syncactivity.html',
            name: Globalize.translate('TabSyncJobs')
        },
         {
             href: 'devicesupload.html',
             name: Globalize.translate('TabCameraUpload')
         },
        {
            href: 'appservices.html?context=sync',
            name: Globalize.translate('TabServices')
        },
         {
             href: 'syncsettings.html',
             name: Globalize.translate('TabSettings')
         }];
    }

    $(document).on('pageinit', ".syncActivityPage", function () {

        var page = this;

        $('.btnSyncSupporter', page).on('click', function () {

            requirejs(["registrationservices"], function () {
                RegistrationServices.validateFeature('sync');
            });
        });
        $('.supporterPromotion .mainText', page).html(Globalize.translate('HeaderSyncRequiresSupporterMembership'));

    }).on('pageshow', ".syncActivityPage", function () {

        if (this.id == 'syncActivityPage') {
            LibraryMenu.setTabs('syncadmin', 0, getTabs);
        }
        var page = this;

        Dashboard.getPluginSecurityInfo().then(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                $('.supporterPromotionContainer', page).hide();
            } else {
                $('.supporterPromotionContainer', page).show();
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
        Events.on(ApiClient, "websocketmessage", onWebSocketMessage);

    }).on('pagebeforehide', ".syncActivityPage", function () {

        var page = this;

        // off here
        $('.btnSync', page).taskButton({
            mode: 'off'
        });

        stopListening();
        Events.off(ApiClient, "websocketmessage", onWebSocketMessage);
    });

});