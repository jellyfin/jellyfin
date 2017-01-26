define(['connectionManager', 'serverNotifications', 'events', 'datetime', 'dom', 'imageLoader', 'loading', 'globalize', 'apphost', 'layoutManager', 'scrollHelper', 'dialogHelper', 'shell', 'listViewStyle', 'paper-icon-button-light', 'emby-button', 'formDialogStyle'], function (connectionManager, serverNotifications, events, datetime, dom, imageLoader, loading, globalize, appHost, layoutManager, scrollHelper, dialogHelper, shell) {
    'use strict';

    function renderJob(context, job, dialogOptions) {

        require(['syncDialog'], function (syncDialog) {
            syncDialog.renderForm({
                elem: context.querySelector('.syncJobFormContent'),
                dialogOptions: dialogOptions,
                dialogOptionsFn: getTargetDialogOptionsFn(dialogOptions),
                showName: true,
                readOnlySyncTarget: true
            }).then(function () {
                fillJobValues(context, job, dialogOptions);
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

        var hasActions = ['Queued', 'Cancelled', 'Failed', 'ReadyToTransfer', 'Transferring', 'Converting', 'Synced'].indexOf(jobItem.Status) !== -1;

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

        html += '<h3 class="listItemBodyText">';
        html += jobItem.ItemName;
        html += '</h3>';

        if (jobItem.Status === 'Failed') {
            html += '<div class="secondary listItemBodyText" style="color:red;">';
        } else {
            html += '<div class="secondary listItemBodyText">';
        }
        html += globalize.translate('sharedcomponents#SyncJobItemStatus' + jobItem.Status);
        if (jobItem.Status === 'Synced' && jobItem.IsMarkedForRemoval) {
            html += '<br/>';
            html += globalize.translate('sharedcomponents#RemovingFromDevice');
        }
        html += '</div>';

        html += '<div class="secondary listItemBodyText" style="padding-top:5px;">';
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

    function renderJobItems(context, items, apiClient) {

        var html = '';

        html += '<h1>' + globalize.translate('sharedcomponents#Items') + '</h1>';

        html += '<div class="paperList">';

        var index = 0;
        html += items.map(function (i) {

            return getJobItemHtml(i, apiClient, index++);

        }).join('');

        html += '</div>';

        var elem = context.querySelector('.jobItems');
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

        var context = parentWithClass(elem, 'page');
        var listItem = parentWithClass(elem, 'listItem');
        var jobItemId = listItem.getAttribute('data-itemid');
        var status = listItem.getAttribute('data-status');
        var remove = listItem.getAttribute('data-remove').toLowerCase() === 'true';

        var menuItems = [];

        if (status === 'Failed' || status === 'Cancelled') {
            menuItems.push({
                name: globalize.translate('sharedcomponents#Retry'),
                id: 'retry'
            });
        }
        else if (status === 'Queued' || status === 'Transferring' || status === 'Converting' || status === 'ReadyToTransfer') {
            menuItems.push({
                name: globalize.translate('sharedcomponents#CancelDownload'),
                id: 'cancel'
            });
        }
        else if (status === 'Synced' && remove) {
            menuItems.push({
                name: globalize.translate('sharedcomponents#KeepOnDevice'),
                id: 'unmarkforremoval'
            });
        }
        else if (status === 'Synced') {
            menuItems.push({
                name: globalize.translate('sharedcomponents#RemoveFromDevice'),
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
                            cancelJobItem(context, jobId, jobItemId, apiClient);
                            break;
                        case 'retry':
                            retryJobItem(context, jobId, jobItemId, apiClient);
                            break;
                        case 'markforremoval':
                            markForRemoval(context, jobId, jobItemId, apiClient);
                            break;
                        case 'unmarkforremoval':
                            unMarkForRemoval(context, jobId, jobItemId, apiClient);
                            break;
                        default:
                            break;
                    }
                }
            });

        });
    }

    function cancelJobItem(context, jobId, jobItemId, apiClient) {

        // Need a timeout because jquery mobile will not show a popup while another is in the act of closing

        loading.show();

        apiClient.ajax({

            type: "DELETE",
            url: apiClient.getUrl('Sync/JobItems/' + jobItemId)

        }).then(function () {

            loadJob(context, jobId, apiClient);
        });

    }

    function markForRemoval(context, jobId, jobItemId, apiClient) {

        apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl('Sync/JobItems/' + jobItemId + '/MarkForRemoval')

        }).then(function () {

            loadJob(context, jobId, apiClient);
        });
    }

    function unMarkForRemoval(context, jobId, jobItemId, apiClient) {

        apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl('Sync/JobItems/' + jobItemId + '/UnmarkForRemoval')

        }).then(function () {

            loadJob(context, jobId, apiClient);
        });
    }

    function retryJobItem(context, jobId, jobItemId, apiClient) {

        apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl('Sync/JobItems/' + jobItemId + '/Enable')

        }).then(function () {

            loadJob(context, jobId, apiClient);
        });
    }

    function fillJobValues(context, job, editOptions) {

        var txtSyncJobName = context.querySelector('#txtSyncJobName');
        if (txtSyncJobName) {
            txtSyncJobName.value = job.Name;
        }

        var selectProfile = context.querySelector('#selectProfile');
        if (selectProfile) {
            selectProfile.value = job.Profile || '';
        }

        var selectQuality = context.querySelector('#selectQuality');
        if (selectQuality) {
            selectQuality.value = job.Quality || '';
        }

        var chkUnwatchedOnly = context.querySelector('#chkUnwatchedOnly');
        if (chkUnwatchedOnly) {
            chkUnwatchedOnly.checked = job.UnwatchedOnly;
        }

        var chkSyncNewContent = context.querySelector('#chkSyncNewContent');
        if (chkSyncNewContent) {
            chkSyncNewContent.checked = job.SyncNewContent;
        }

        var txtItemLimit = context.querySelector('#txtItemLimit');
        if (txtItemLimit) {
            txtItemLimit.value = job.ItemLimit;
        }

        var txtBitrate = context.querySelector('#txtBitrate');
        if (job.Bitrate) {
            txtBitrate.value = job.Bitrate / 1000000;
        } else {
            txtBitrate.value = '';
        }

        var target = editOptions.Targets.filter(function (t) {
            return t.Id === job.TargetId;
        })[0];
        var targetName = target ? target.Name : '';

        var selectSyncTarget = context.querySelector('#selectSyncTarget');
        if (selectSyncTarget) {
            selectSyncTarget.value = targetName;
        }
    }

    var _jobOptions;
    function loadJob(context, id, apiClient) {

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
                renderJob(context, job, options);
                loading.hide();
            });
        });

        apiClient.getJSON(apiClient.getUrl('Sync/JobItems', {

            JobId: id,
            AddMetadata: true

        })).then(function (result) {

            renderJobItems(context, result.Items, apiClient);
            loading.hide();
        });
    }

    function loadJobInfo(context, job, jobItems, apiClient) {

        //renderJob(page, job, _jobOptions);
        renderJobItems(context, jobItems, apiClient);
        loading.hide();
    }

    function saveJob(context, id, apiClient) {

        loading.show();

        apiClient.getJSON(apiClient.getUrl('Sync/Jobs/' + id)).then(function (job) {

            require(['syncDialog'], function (syncDialog) {
                syncDialog.setJobValues(job, context);

                apiClient.ajax({

                    url: apiClient.getUrl('Sync/Jobs/' + id),
                    type: 'POST',
                    data: JSON.stringify(job),
                    contentType: "application/json"

                }).then(function () {

                    loading.hide();
                    dialogHelper.close(context);
                });
            });
        });

    }

    function onHelpLinkClick(e) {

        shell.openUrl(this.href);

        e.preventDefault();
        return false;
    }

    function startListening(apiClient, jobId) {

        var startParams = "0,1500";

        startParams += "," + jobId;

        if (apiClient.isWebSocketOpen()) {
            apiClient.sendWebSocketMessage("SyncJobStart", startParams);
        }
    }

    function stopListening(apiClient) {

        if (apiClient.isWebSocketOpen()) {
            apiClient.sendWebSocketMessage("SyncJobStop", "");
        }

    }

    function bindEvents(context, jobId, apiClient) {
        context.querySelector('.jobItems').addEventListener('click', function (e) {
            var btnJobItemMenu = dom.parentWithClass(e.target, 'btnJobItemMenu');
            if (btnJobItemMenu) {
                showJobItemMenu(btnJobItemMenu, jobId, apiClient);
            }
        });
    }

    function showEditor(options) {
        
        var apiClient = connectionManager.getApiClient(options.serverId);
        var id = options.jobId;

        var dlgElementOptions = {
            removeOnClose: true,
            scrollY: false,
            autoFocus: false
        };

        if (layoutManager.tv) {
            dlgElementOptions.size = 'fullscreen';
        } else {
            dlgElementOptions.size = 'medium';
        }

        var dlg = dialogHelper.createDialog(dlgElementOptions);

        dlg.classList.add('formDialog');

        var html = '';
        html += '<div class="formDialogHeader">';
        html += '<button is="paper-icon-button-light" class="btnCancel autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
        html += '<h3 class="formDialogHeaderTitle">';
        html += globalize.translate('sharedcomponents#Sync');
        html += '</h3>';

        html += '<a href="https://github.com/MediaBrowser/Wiki/wiki/Sync" target="_blank" class="clearLink lnkHelp" style="margin-top:0;display:inline-block;vertical-align:middle;margin-left:auto;"><button is="emby-button" type="button" class="button-accent-flat button-flat"><i class="md-icon">info</i><span>' + globalize.translate('sharedcomponents#Help') + '</span></button></a>';

        html += '</div>';

        html += '<div class="formDialogContent smoothScrollY" style="padding-top:2em;">';
        html += '<div class="dialogContentInner dialog-content-centered">';

        html += '<form class="syncJobForm" style="margin: auto;">';

        html += '<div class="syncJobFormContent"></div>';

        html += '<div class="jobItems"></div>';

        html += '<div class="formDialogFooter">';
        html += '<button is="emby-button" type="submit" class="raised button-submit block formDialogFooterItem"><span>' + globalize.translate('sharedcomponents#Save') + '</span></button>';
        html += '</div>';

        html += '</form>';

        html += '</div>';
        html += '</div>';

        dlg.innerHTML = html;

        dlg.querySelector('.lnkHelp').addEventListener('click', onHelpLinkClick);

        var submitted = false;

        dlg.querySelector('form').addEventListener('submit', function (e) {

            saveJob(dlg, id, apiClient);
            e.preventDefault();
            return false;
        });

        dlg.querySelector('.btnCancel').addEventListener('click', function () {
            dialogHelper.close(dlg);
        });

        if (layoutManager.tv) {
            scrollHelper.centerFocus.on(dlg.querySelector('.formDialogContent'), false);
        }

        function onSyncJobMessage(e, apiClient, msg) {
            loadJobInfo(dlg, msg.Job, msg.JobItems, apiClient);
        }

        loadJob(dlg, id, apiClient);
        bindEvents(dlg, id, apiClient);

        var promise = dialogHelper.open(dlg);

        startListening(apiClient, id);
        events.on(serverNotifications, "SyncJob", onSyncJobMessage);

        return promise.then(function () {

            stopListening(apiClient);
            events.off(serverNotifications, "SyncJob", onSyncJobMessage);

            if (layoutManager.tv) {
                scrollHelper.centerFocus.off(dlg.querySelector('.formDialogContent'), false);
            }

            if (submitted) {
                return Promise.resolve();
            }
            return Promise.reject();
        });
    }

    return {
        show: showEditor
    };

});