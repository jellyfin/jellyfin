define(['connectionManager', 'serverNotifications', 'events', 'datetime', 'dom', 'imageLoader', 'loading', 'globalize', 'apphost', 'layoutManager', 'scrollHelper', 'dialogHelper', 'listViewStyle', 'paper-icon-button-light', 'emby-button', 'formDialogStyle', 'emby-linkbutton'], function (connectionManager, serverNotifications, events, datetime, dom, imageLoader, loading, globalize, appHost, layoutManager, scrollHelper, dialogHelper) {
    'use strict';

    function syncNow() {
        require(['localsync'], function (localSync) {
            localSync.sync();
        });
    }

    function renderJob(context, job, dialogOptions) {

        require(['syncDialog'], function (syncDialog) {
            syncDialog.renderForm({
                elem: context.querySelector('.syncJobFormContent'),
                dialogOptions: dialogOptions,
                dialogOptionsFn: getTargetDialogOptionsFn(dialogOptions),
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
        var status = jobItem.Status;

        var nextAction;

        if (status === 'Failed') {
            nextAction = 'retry';
        }
        else if (status === 'Cancelled') {
            nextAction = 'retry';
        }
        else if (status === 'Queued' || status === 'Transferring' || status === 'Converting' || status === 'ReadyToTransfer') {
            nextAction = 'cancel';
        }
        else if (status === 'Synced' && !jobItem.IsMarkedForRemoval) {
            nextAction = 'remove';
        }

        var listItemClass = 'listItem listItem-border';
        if (layoutManager.tv && nextAction) {
            listItemClass += ' btnJobItemMenu';
        }

        if (layoutManager.tv) {
            listItemClass += ' listItem-button';
        }

        var tagName = layoutManager.tv ? 'button' : 'div';
        html += '<' + tagName + ' type="button" class="' + listItemClass + '" data-itemid="' + jobItem.Id + '" data-status="' + jobItem.Status + '" data-action="' + nextAction + '">';

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
            html += '<div class="listItemImage" style="background-image:url(\'' + imgUrl + '\');background-repeat:no-repeat;background-position:center center;background-size: cover;"></div>';
        }
        else {
            html += '<i class="md-icon listItemIcon">sync</i>';
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
        html += '<div style="background:#e0e0e0;height:2px;"><div style="background:#52B54B;width:' + (jobItem.Progress || 0) + '%;height:100%;"></div></div>';
        html += '</div>';

        html += '</div>';

        var moreIcon = '&#xE5D3;';

        if (!layoutManager.tv) {

            if (nextAction === 'retry') {
                html += '<button type="button" is="paper-icon-button-light" class="btnJobItemMenu" data-action="' + nextAction + '"><i class="md-icon">&#xE001;</i></button>';
            }
            else if (nextAction === 'cancel') {
                html += '<button type="button" is="paper-icon-button-light" class="btnJobItemMenu" data-action="' + nextAction + '"><i class="md-icon">&#xE15D;</i></button>';
            }
            else if (nextAction === 'remove') {
                html += '<button type="button" is="paper-icon-button-light" class="btnJobItemMenu" data-action="' + nextAction + '"><i class="md-icon">&#xE15D;</i></button>';
            }
        }

        html += '</' + tagName + '>';
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

        var action = elem.getAttribute('data-action');
        var context = parentWithClass(elem, 'formDialog');
        var listItem = parentWithClass(elem, 'listItem');
        var jobItemId = listItem.getAttribute('data-itemid');

        var menuItems = [];

        if (action === 'retry') {
            retryJobItem(context, jobId, jobItemId, apiClient);
        }
        else if (action === 'cancel' || action === 'remove') {
            cancelJobItem(context, jobId, jobItemId, apiClient);
        }
    }

    function cancelJobItem(context, jobId, jobItemId, apiClient) {

        showRemoveConfirm(function () {
            loading.show();

            apiClient.ajax({

                type: "DELETE",
                url: apiClient.getUrl('Sync/JobItems/' + jobItemId)

            }).then(function () {

                // TODO this should check editor options.mode === 'download'
                if (appHost.supports('sync')) {
                    syncNow();
                }

                loadJob(context, jobId, apiClient);
            });
        });
    }

    function retryJobItem(context, jobId, jobItemId, apiClient) {

        showRetryConfirm(function () {
            apiClient.ajax({

                type: "POST",
                url: apiClient.getUrl('Sync/JobItems/' + jobItemId + '/Enable')

            }).then(function () {

                // TODO this should check editor options.mode === 'download'
                if (appHost.supports('sync')) {
                    syncNow();
                }

                loadJob(context, jobId, apiClient);
            });
        });
    }

    function showRetryConfirm(callback) {

        // TODO Implement this as a retry dialog
        require(['confirm'], function (confirm) {

            confirm({

                text: globalize.translate('sharedcomponents#ConfirmRemoveDownload'),
                confirmText: globalize.translate('sharedcomponents#RemoveDownload'),
                cancelText: globalize.translate('sharedcomponents#KeepDownload'),
                primary: 'cancel'

            }).then(callback);
        });
    }

    function showRemoveConfirm(callback) {

        require(['confirm'], function (confirm) {

            confirm({

                text: globalize.translate('sharedcomponents#ConfirmRemoveDownload'),
                confirmText: globalize.translate('sharedcomponents#RemoveDownload'),
                cancelText: globalize.translate('sharedcomponents#KeepDownload'),
                primary: 'cancel'

            }).then(callback);
        });
    }

    function showConfirm(text, callback) {

        require(['confirm'], function (confirm) {

            confirm(text).then(callback);
        });
    }

    function fillJobValues(context, job, editOptions) {

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

                    // TODO this should check editor options.mode === 'download'
                    if (appHost.supports('sync')) {
                        syncNow();
                    }

                    loading.hide();
                    dialogHelper.close(context);
                });
            });
        });

    }

    function startListening(apiClient, jobId) {

        var startParams = "0,1500";

        startParams += "," + jobId;

        apiClient.sendMessage("SyncJobStart", startParams);
    }

    function stopListening(apiClient) {

        apiClient.sendMessage("SyncJobStop", "");
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

        if (appHost.supports('externallinks')) {
            html += '<a href="https://github.com/MediaBrowser/Wiki/wiki/Sync" target="_blank" is="emby-linkbutton" class="button-link lnkHelp" style="margin-top:0;display:inline-block;vertical-align:middle;margin-left:auto;"><i class="md-icon">info</i><span>' + globalize.translate('sharedcomponents#Help') + '</span></a>';
        }

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