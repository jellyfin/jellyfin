define(['connectionManager', 'serverNotifications', 'events', 'datetime', 'dom', 'imageLoader', 'loading', 'globalize', 'apphost', 'listViewStyle', 'paper-icon-button-light', 'emby-button'], function (connectionManager, serverNotifications, events, datetime, dom, imageLoader, loading, globalize, appHost) {
    'use strict';

    function renderJob(page, job, dialogOptions) {

        var html = '';

        html += '<div>';
        html += globalize.translate('ValueDateCreated', datetime.parseISO8601Date(job.DateCreated, true).toLocaleString());
        html += '</div>';
        html += '<br/>';
        html += '<div class="formFields"></div>';

        html += '<br/>';
        html += '<br/>';
        html += '<button is="emby-button" type="submit" class="raised button-submit block"><span>' + globalize.translate('ButtonSave') + '</span></button>';

        page.querySelector('.syncJobForm').innerHTML = html;

        require(['syncDialog'], function (syncDialog) {
            syncDialog.renderForm({
                elem: page.querySelector('.formFields'),
                dialogOptions: dialogOptions,
                dialogOptionsFn: getTargetDialogOptionsFn(dialogOptions),
                showName: true,
                readOnlySyncTarget: true
            }).then(function () {
                fillJobValues(page, job, dialogOptions);
            });
        });
    }

    function getTargetDialogOptionsFn(dialogOptions) {

        return function (targetId) {

            return Promise.resolve(dialogOptions);
        };
    }

    function getJobItemHtml(jobItem, apiClient, index) {

        var html = '';

        html += '<div class="listItem" data-itemid="' + jobItem.Id + '" data-status="' + jobItem.Status + '" data-remove="' + jobItem.IsMarkedForRemoval + '">';

        var hasActions = ['Queued', 'Cancelled', 'Failed', 'ReadyToTransfer', 'Transferring', 'Converting', 'Synced'].indexOf(jobItem.Status) != -1;

        var imgUrl;

        if (jobItem.PrimaryImageItemId) {

            imgUrl = apiClient.getImageUrl(jobItem.PrimaryImageItemId, {
                type: "Primary",
                width: 80,
                tag: jobItem.PrimaryImageTag,
                minScale: 1.5
            });
        }

        if (imgUrl) {
            html += '<button type="button" is="emby-button" class="blue mini fab autoSize" icon="sync" style="background-image:url(\'' + imgUrl + '\');background-repeat:no-repeat;background-position:center center;background-size: cover;"><i style="visibility:hidden;" class="md-icon">sync</i></button>';
        }
        else {
            html += '<button type="button" is="emby-button" class="blue mini fab autoSize" icon="sync"><i class="md-icon">sync</i></button>';
        }

        html += '<div class="listItemBody three-line">';

        html += '<div>';
        html += jobItem.ItemName;
        html += '</div>';

        if (jobItem.Status == 'Failed') {
            html += '<div class="secondary" style="color:red;">';
        } else {
            html += '<div class="secondary">';
        }
        html += globalize.translate('SyncJobItemStatus' + jobItem.Status);
        if (jobItem.Status == 'Synced' && jobItem.IsMarkedForRemoval) {
            html += '<br/>';
            html += globalize.translate('SyncJobItemStatusSyncedMarkForRemoval');
        }
        html += '</div>';

        html += '<div class="secondary" style="padding-top:5px;">';
        html += '<div style="background:#e0e0e0;height:4px;"><div style="background:#52B54B;width:' + (jobItem.Progress || 0) + '%;height:100%;"></div></div>';
        html += '</div>';

        html += '</div>';

        var moreIcon = appHost.moreIcon === 'dots-horiz' ? '&#xE5D3;' : '&#xE5D4;';

        if (hasActions) {

            html += '<button type="button" is="paper-icon-button-light" class="btnJobItemMenu autoSize"><i class="md-icon">' + moreIcon + '</i></button>';
        } else {
            html += '<button type="button" is="paper-icon-button-light" class="btnJobItemMenu autoSize" disabled><i class="md-icon">' + moreIcon + '</i></button>';
        }

        html += '</div>';
        return html;
    }

    function renderJobItems(page, items, apiClient) {

        var html = '';

        html += '<h1>' + globalize.translate('HeaderItems') + '</h1>';

        html += '<div class="paperList">';

        var index = 0;
        html += items.map(function (i) {

            return getJobItemHtml(i, apiClient, index++);

        }).join('');

        html += '</div>';

        var elem = page.querySelector('.jobItems');
        elem.innerHTML = html;
        imageLoader.lazyChildren(elem);
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function showJobItemMenu(elem, jobId, apiClient) {

        var page = parentWithClass(elem, 'page');
        var listItem = parentWithClass(elem, 'listItem');
        var jobItemId = listItem.getAttribute('data-itemid');
        var status = listItem.getAttribute('data-status');
        var remove = listItem.getAttribute('data-remove').toLowerCase() == 'true';

        var menuItems = [];

        if (status == 'Failed') {
            menuItems.push({
                name: globalize.translate('ButtonQueueForRetry'),
                id: 'retry'
            });
        }
        else if (status == 'Cancelled') {
            menuItems.push({
                name: globalize.translate('ButtonReenable'),
                id: 'retry'
            });
        }
        else if (status == 'Queued' || status == 'Transferring' || status == 'Converting' || status == 'ReadyToTransfer') {
            menuItems.push({
                name: globalize.translate('ButtonCancelItem'),
                id: 'cancel'
            });
        }
        else if (status == 'Synced' && remove) {
            menuItems.push({
                name: globalize.translate('ButtonUnmarkForRemoval'),
                id: 'unmarkforremoval'
            });
        }
        else if (status == 'Synced') {
            menuItems.push({
                name: globalize.translate('ButtonMarkForRemoval'),
                id: 'markforremoval'
            });
        }

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: elem,
                callback: function (id) {

                    switch (id) {

                        case 'cancel':
                            cancelJobItem(page, jobId, jobItemId, apiClient);
                            break;
                        case 'retry':
                            retryJobItem(page, jobId, jobItemId, apiClient);
                            break;
                        case 'markforremoval':
                            markForRemoval(page, jobId, jobItemId, apiClient);
                            break;
                        case 'unmarkforremoval':
                            unMarkForRemoval(page, jobId, jobItemId, apiClient);
                            break;
                        default:
                            break;
                    }
                }
            });

        });
    }

    function cancelJobItem(page, jobId, jobItemId, apiClient) {

        // Need a timeout because jquery mobile will not show a popup while another is in the act of closing

        loading.show();

        apiClient.ajax({

            type: "DELETE",
            url: apiClient.getUrl('Sync/JobItems/' + jobItemId)

        }).then(function () {

            loadJob(page, jobId, apiClient);
        });

    }

    function markForRemoval(page, jobId, jobItemId, apiClient) {

        apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl('Sync/JobItems/' + jobItemId + '/MarkForRemoval')

        }).then(function () {

            loadJob(page, jobId, apiClient);
        });
    }

    function unMarkForRemoval(page, jobId, jobItemId, apiClient) {

        apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl('Sync/JobItems/' + jobItemId + '/UnmarkForRemoval')

        }).then(function () {

            loadJob(page, jobId, apiClient);
        });
    }

    function retryJobItem(page, jobId, jobItemId, apiClient) {

        apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl('Sync/JobItems/' + jobItemId + '/Enable')

        }).then(function () {

            loadJob(page, jobId, apiClient);
        });
    }

    function fillJobValues(page, job, editOptions) {

        var txtSyncJobName = page.querySelector('#txtSyncJobName');
        if (txtSyncJobName) {
            txtSyncJobName.value = job.Name;
        }

        var selectProfile = page.querySelector('#selectProfile');
        if (selectProfile) {
            selectProfile.value = job.Profile || '';
        }

        var selectQuality = page.querySelector('#selectQuality');
        if (selectQuality) {
            selectQuality.value = job.Quality || '';
        }

        var chkUnwatchedOnly = page.querySelector('#chkUnwatchedOnly');
        if (chkUnwatchedOnly) {
            chkUnwatchedOnly.checked = job.UnwatchedOnly;
        }

        var chkSyncNewContent = page.querySelector('#chkSyncNewContent');
        if (chkSyncNewContent) {
            chkSyncNewContent.checked = job.SyncNewContent;
        }

        var txtItemLimit = page.querySelector('#txtItemLimit');
        if (txtItemLimit) {
            txtItemLimit.value = job.ItemLimit;
        }

        var txtBitrate = page.querySelector('#txtBitrate');
        if (job.Bitrate) {
            txtBitrate.value = job.Bitrate / 1000000;
        } else {
            txtBitrate.value = '';
        }

        var target = editOptions.Targets.filter(function (t) {
            return t.Id == job.TargetId;
        })[0];
        var targetName = target ? target.Name : '';

        var selectSyncTarget = page.querySelector('#selectSyncTarget');
        if (selectSyncTarget) {
            selectSyncTarget.value = targetName;
        }
    }

    var _jobOptions;
    function loadJob(page, id, apiClient) {

        loading.show();

        apiClient.getJSON(apiClient.getUrl('Sync/Jobs/' + id)).then(function (job) {

            apiClient.getJSON(apiClient.getUrl('Sync/Options', {

                UserId: job.UserId,
                ItemIds: (job.RequestedItemIds && job.RequestedItemIds.length ? job.RequestedItemIds.join('') : null),

                ParentId: job.ParentId,
                Category: job.Category,
                TargetId: job.TargetId

            })).then(function (options) {

                _jobOptions = options;
                renderJob(page, job, options);
                loading.hide();
            });
        });

        apiClient.getJSON(apiClient.getUrl('Sync/JobItems', {

            JobId: id,
            AddMetadata: true

        })).then(function (result) {

            renderJobItems(page, result.Items, apiClient);
            loading.hide();
        });
    }

    function loadJobInfo(page, job, jobItems, apiClient) {

        //renderJob(page, job, _jobOptions);
        renderJobItems(page, jobItems, apiClient);
        loading.hide();
    }

    function saveJob(page, id, apiClient) {

        loading.show();

        apiClient.getJSON(apiClient.getUrl('Sync/Jobs/' + id)).then(function (job) {

            require(['syncDialog'], function (syncDialog) {
                syncDialog.setJobValues(job, page);

                apiClient.ajax({

                    url: apiClient.getUrl('Sync/Jobs/' + id),
                    type: 'POST',
                    data: JSON.stringify(job),
                    contentType: "application/json"

                }).then(function () {

                    loading.hide();
                    require(['toast'], function (toast) {
                        toast(globalize.translate('SettingsSaved'));
                    });
                });
            });
        });

    }

    return function (view, params) {

        function getApiClient() {
            return connectionManager.getApiClient(params.serverId);
        }

        view.querySelector('.syncJobForm').addEventListener('submit', function (e) {

            saveJob(view, params.id, getApiClient());
            e.preventDefault();
            return false;
        });

        function onSyncJobMessage(e, apiClient, msg) {
            loadJobInfo(view, msg.Job, msg.JobItems, apiClient);
        }

        function startListening(page) {

            var startParams = "0,1500";

            startParams += "," + params.id;

            var apiClient = getApiClient();

            if (apiClient.isWebSocketOpen()) {
                apiClient.sendWebSocketMessage("SyncJobStart", startParams);
            }
        }

        function stopListening() {

            var apiClient = getApiClient();

            if (apiClient.isWebSocketOpen()) {
                apiClient.sendWebSocketMessage("SyncJobStop", "");
            }

        }

        view.querySelector('.jobItems').addEventListener('click', function (e) {
            var btnJobItemMenu = dom.parentWithClass(e.target, 'btnJobItemMenu');
            if (btnJobItemMenu) {
                showJobItemMenu(btnJobItemMenu, params.id);
            }
        });

        view.addEventListener('viewshow', function () {
            var page = this;
            loadJob(page, params.id, getApiClient());

            startListening(page);
            events.on(serverNotifications, "SyncJob", onSyncJobMessage);
        });

        view.addEventListener('viewbeforehide', function () {

            stopListening();
            events.off(serverNotifications, "SyncJob", onSyncJobMessage);
        });
    };

});