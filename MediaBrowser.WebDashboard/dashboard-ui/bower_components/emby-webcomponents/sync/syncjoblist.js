define(['serverNotifications', 'events', 'loading', 'connectionManager', 'imageLoader', 'dom', 'globalize', 'registrationServices', 'layoutManager', 'listViewStyle'], function (serverNotifications, events, loading, connectionManager, imageLoader, dom, globalize, registrationServices, layoutManager) {
    'use strict';

    function onSyncJobsUpdated(e, apiClient, data) {

        var listInstance = this;
        renderList(listInstance, data, apiClient);
    }

    function refreshList(listInstance, jobs) {
        for (var i = 0, length = jobs.length; i < length; i++) {

            var job = jobs[i];
            refreshJob(listInstance, job);
        }
    }

    function cancelJob(listInstance, id) {

        require(['confirm'], function (confirm) {

            var msg = listInstance.options.isLocalSync ?
globalize.translate('sharedcomponents#ConfirmRemoveDownload') :
globalize.translate('sharedcomponents#CancelSyncJobConfirmation');

            confirm({

                text: msg,
                primary: 'cancel'

            }).then(function () {

                loading.show();
                var apiClient = getApiClient(listInstance);

                apiClient.ajax({

                    url: apiClient.getUrl('Sync/Jobs/' + id),
                    type: 'DELETE'

                }).then(function () {

                    fetchData(listInstance);
                });
            });
        });
    }

    function refreshJob(listInstance, job) {

        var listItem = listInstance.options.element.querySelector('.listItem[data-id=\'' + job.Id + '\']');

        if (!listItem) {
            return;
        }

        listItem.querySelector('.jobStatus').innerHTML = getProgressText(job);
    }

    function getProgressText(job) {

        var status = job.Status;

        if (status === 'Completed') {
            status = 'Synced';
        }

        var html = globalize.translate('sharedcomponents#SyncJobItemStatus' + status);

        if (job.Status === 'Transferring' || job.Status === 'Converting' || job.Status === 'Completed') {
            html += ' ';
            html += (job.Progress || 0) + '%';
        }

        return html;
    }

    function getSyncJobHtml(listInstance, job, apiClient) {

        var html = '';

        var tagName = layoutManager.tv ? 'button' : 'div';
        var typeAttribute = tagName === 'button' ? ' type="button"' : '';

        var listItemClass = 'listItem';

        if (layoutManager.tv) {
            listItemClass += ' listItem-button listItem-focusscale';
        }

        html += '<' + tagName + typeAttribute + ' class="' + listItemClass + '" data-id="' + job.Id + '" data-status="' + job.Status + '">';

        var imgUrl;

        if (job.PrimaryImageItemId) {

            imgUrl = apiClient.getImageUrl(job.PrimaryImageItemId, {
                type: "Primary",
                width: 80,
                tag: job.PrimaryImageTag,
                minScale: 1.5
            });
        }

        if (imgUrl) {
            html += '<div class="listItemImage lazy" data-src="' + imgUrl + '" item-icon>';
            html += '</div>';
        }
        else {
            html += '<i class="md-icon listItemIcon">file_download</i>';
        }

        var textLines = [];

        if (job.ParentName) {
            textLines.push(job.ParentName);
        }

        textLines.push(job.Name);

        if (job.ItemCount === 1) {
            textLines.push(globalize.translate('sharedcomponents#ValueOneItem'));
        } else {
            textLines.push(globalize.translate('sharedcomponents#ItemCount', job.ItemCount));
        }

        html += '<div class="listItemBody three-line">';

        for (var i = 0, length = textLines.length; i < length; i++) {

            if (i === 0) {
                html += '<h3 class="listItemBodyText">';
                html += textLines[i];
                html += '</h3>';
            } else {
                html += '<div class="listItemBodyText secondary">';
                html += textLines[i];
                html += '</div>';
            }
        }

        html += '<div class="secondary listItemBodyText jobStatus" style="color:green;">';
        html += getProgressText(job);
        html += '</div>';

        html += '</div>';

        if (!layoutManager.tv) {
            html += '<button type="button" is="paper-icon-button-light" class="btnJobMenu listItemButton"><i class="md-icon">more_vert</i></button>';
        }

        html += '</' + tagName + '>';

        return html;
    }

    function renderList(listInstance, jobs, apiClient) {

        if ((new Date().getTime() - listInstance.lastDataLoad) < 60000) {
            refreshList(listInstance, jobs);
            return;
        }

        listInstance.lastDataLoad = new Date().getTime();

        var html = '';
        var lastTargetName = '';

        var isLocalSync = listInstance.options.isLocalSync;
        var showTargetName = !isLocalSync;

        var hasOpenSection = false;

        for (var i = 0, length = jobs.length; i < length; i++) {

            var job = jobs[i];
            if (showTargetName) {
                var targetName = job.TargetName || 'Unknown';

                if (targetName !== lastTargetName) {

                    if (lastTargetName) {
                        html += '</div>';
                        html += '<br/>';
                        hasOpenSection = false;
                    }

                    lastTargetName = targetName;

                    html += '<div class="detailSectionHeader">';

                    html += '<h1>' + targetName + '</h1>';

                    html += '</div>';
                    html += '<div class="itemsContainer vertical-list paperList">';
                    hasOpenSection = true;
                }
            }

            html += getSyncJobHtml(listInstance, job, apiClient);
        }

        if (hasOpenSection) {
            html += '</div>';
        }

        var elem = listInstance.options.element.querySelector('.syncJobListContent');

        if (!html) {
            if (isLocalSync) {
                html = '<div style="padding:1em .25em;">' + globalize.translate('sharedcomponents#MessageNoDownloadsFound') + '</div>';
            } else {
                html = '<div style="padding:1em .25em;">' + globalize.translate('sharedcomponents#MessageNoSyncJobsFound') + '</div>';
            }
        }

        elem.innerHTML = html;

        imageLoader.lazyChildren(elem);
    }

    function fetchData(listInstance) {

        listInstance.lastDataLoad = 0;
        loading.show();

        var options = {};
        var apiClient = getApiClient(listInstance);

        if (listInstance.options.userId) {
            options.UserId = listInstance.options.userId;
        }

        if (listInstance.options.isLocalSync) {
            options.TargetId = apiClient.deviceId();
        } else {
            options.ExcludeTargetIds = apiClient.deviceId();
        }

        return apiClient.getJSON(apiClient.getUrl('Sync/Jobs', options)).then(function (response) {

            renderList(listInstance, response.Items, apiClient);
            loading.hide();
        });
    }

    function startListening(listInstance) {

        var startParams = "0,1500";

        var apiClient = getApiClient(listInstance);

        if (listInstance.options.userId) {
            startParams += "," + listInstance.options.userId;
        }
        if (listInstance.options.isLocalSync) {
            startParams += "," + apiClient.deviceId();
        }

        if (apiClient.isWebSocketOpen()) {
            apiClient.sendWebSocketMessage("SyncJobsStart", startParams);
        }
    }

    function stopListening(listInstance) {

        var apiClient = getApiClient(listInstance);
        if (apiClient.isWebSocketOpen()) {
            apiClient.sendWebSocketMessage("SyncJobsStop", "");
        }
    }

    function getApiClient(listInstance) {
        return connectionManager.getApiClient(listInstance.options.serverId);
    }

    function showJobMenu(listInstance, elem) {

        var item = dom.parentWithClass(elem, 'listItem');
        var jobId = item.getAttribute('data-id');
        var status = item.getAttribute('data-status');

        var menuItems = [];

        if (status === 'Cancelled') {
            menuItems.push({
                name: globalize.translate('sharedcomponents#Delete'),
                id: 'delete'
            });
        } else {
            var txt = listInstance.options.isLocalSync ?
globalize.translate('sharedcomponents#RemoveDownload') :
globalize.translate('sharedcomponents#ButtonCancelSyncJob');

            menuItems.push({
                name: txt,
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
                            cancelJob(listInstance, jobId);
                            break;
                        case 'cancel':
                            cancelJob(listInstance, jobId);
                            break;
                        default:
                            break;
                    }
                }
            });

        });
    }

    function onElementClick(e) {

        var listInstance = this;

        var btnJobMenu = dom.parentWithClass(e.target, 'btnJobMenu');
        if (btnJobMenu) {
            showJobMenu(this, btnJobMenu);
            return;
        }

        var listItem = dom.parentWithClass(e.target, 'listItem');
        if (listItem) {
            var jobId = listItem.getAttribute('data-id');
            // edit job
            require(['syncJobEditor'], function (syncJobEditor) {
                syncJobEditor.show({
                    serverId: listInstance.options.serverId,
                    jobId: jobId
                }).then(function () {
                    fetchData(listInstance);
                });
            });
        }
    }

    function syncJobList(options) {
        this.options = options;

        var onSyncJobsUpdatedHandler = onSyncJobsUpdated.bind(this);
        this.onSyncJobsUpdatedHandler = onSyncJobsUpdatedHandler;
        events.on(serverNotifications, 'SyncJobs', onSyncJobsUpdatedHandler);

        var onClickHandler = onElementClick.bind(this);
        options.element.addEventListener('click', onClickHandler);
        this.onClickHandler = onClickHandler;

        options.element.innerHTML = '<div class="syncJobListContent"></div>';

        fetchData(this);
        startListening(this);

        initSupporterInfo(options.element, getApiClient(this));
    }

    function showSupporterInfo(context) {

        var html = '<button is="emby-button" class="raised button-accent block btnSyncSupporter" style="margin:1em 0;">';

        html += '<div>';
        html += globalize.translate('sharedcomponents#HeaderSyncRequiresSub');
        html += '</div>';
        html += '<div style="margin-top:.5em;">';
        html += globalize.translate('sharedcomponents#LearnMore');
        html += '</div>';

        html += '</button';

        context.insertAdjacentHTML('afterbegin', html);

        context.querySelector('.btnSyncSupporter').addEventListener('click', function () {

            registrationServices.validateFeature('sync');
        });

    }

    function initSupporterInfo(context, apiClient) {

        apiClient.getPluginSecurityInfo().then(function (regInfo) {

            if (!regInfo.IsMBSupporter) {
                showSupporterInfo(context, apiClient);
            }

        }, function () {
            showSupporterInfo(context, apiClient);
        });
    }

    syncJobList.prototype.destroy = function () {

        stopListening(this);

        var onSyncJobsUpdatedHandler = this.onSyncJobsUpdatedHandler;
        this.onSyncJobsUpdatedHandler = null;
        events.off(serverNotifications, 'SyncJobs', onSyncJobsUpdatedHandler);

        var onClickHandler = this.onClickHandler;
        this.onClickHandler = null;
        this.options.element.removeEventListener('click', onClickHandler);

        this.options = null;
    };

    return syncJobList;
});