define(["serverNotifications", "events", "loading", "connectionManager", "imageLoader", "dom", "globalize", "registrationServices", "layoutManager", "listViewStyle"], function(serverNotifications, events, loading, connectionManager, imageLoader, dom, globalize, registrationServices, layoutManager) {
    "use strict";

    function onSyncJobCreated(e, apiClient, data) {
        fetchData(this)
    }

    function onSyncJobUpdated(e, apiClient, data) {
        refreshJob(this, data)
    }

    function onSyncJobCancelled(e, apiClient, data) {
        fetchData(this)
    }

    function refreshList(listInstance, jobs) {
        for (var i = 0, length = jobs.length; i < length; i++) {
            refreshJob(listInstance, jobs[i])
        }
    }

    function syncNow() {
        require(["localsync"], function(localSync) {
            localSync.sync()
        })
    }

    function cancelJob(listInstance, id) {
        require(["confirm"], function(confirm) {
            confirm({
                text: globalize.translate("sharedcomponents#ConfirmRemoveDownload"),
                primary: "cancel"
            }).then(function() {
                loading.show();
                var apiClient = getApiClient(listInstance);
                apiClient.ajax({
                    url: apiClient.getUrl("Sync/Jobs/" + id),
                    type: "DELETE"
                }).then(function() {
                    "download" === listInstance.options.mode && syncNow(), fetchData(listInstance)
                })
            })
        })
    }

    function refreshJob(listInstance, job) {
        var listItem = listInstance.options.element.querySelector(".listItem[data-id='" + job.Id + "']");
        listItem && (listItem.querySelector(".jobStatus").innerHTML = getProgressText(job))
    }

    function getProgressText(job) {
        var status = job.Status;
        "Completed" === status && (status = "Synced");
        var html = globalize.translate("sharedcomponents#SyncJobItemStatus" + status);
        if ("Transferring" === job.Status || "Converting" === job.Status || "Completed" === job.Status) {
            html += " ";
            var progress = job.Progress || 0;
            progress > 0 && progress < 100 && (progress = progress.toFixed(1)), html += progress + "%"
        }
        return html
    }

    function getSyncJobHtml(listInstance, job, apiClient) {
        var html = "",
            tagName = layoutManager.tv ? "button" : "div",
            typeAttribute = "button" === tagName ? ' type="button"' : "",
            listItemClass = "listItem listItem-border";
        layoutManager.tv && (listItemClass += " listItem-button listItem-focusscale", listItemClass += " btnJobMenu");
        var canEdit = (job.ItemCount || 1) > 1 || "Queued" === job.Status;
        html += "<" + tagName + typeAttribute + ' class="' + listItemClass + '" data-canedit="' + canEdit + '" data-id="' + job.Id + '" data-status="' + job.Status + '">';
        var imgUrl;
        job.PrimaryImageItemId && (imgUrl = apiClient.getImageUrl(job.PrimaryImageItemId, {
            type: "Primary",
            width: 80,
            tag: job.PrimaryImageTag,
            minScale: 1.5
        })), imgUrl ? (html += '<div class="listItemImage lazy" data-src="' + imgUrl + '" item-icon>', html += "</div>") : html += '<i class="md-icon listItemIcon">file_download</i>';
        var textLines = [],
            name = job.Name;
        job.ParentName && (name += " - " + job.ParentName), textLines.push(name), 1 === job.ItemCount || textLines.push(globalize.translate("sharedcomponents#ItemCount", job.ItemCount)), html += '<div class="listItemBody three-line">';
        for (var i = 0, length = textLines.length; i < length; i++) 0 === i ? (html += '<h3 class="listItemBodyText">', html += textLines[i], html += "</h3>") : (html += '<div class="listItemBodyText secondary">', html += textLines[i], html += "</div>");
        return html += '<div class="secondary listItemBodyText jobStatus">', html += getProgressText(job), html += "</div>", html += "</div>", layoutManager.tv || (html += canEdit ? '<button type="button" is="paper-icon-button-light" class="btnJobMenu listItemButton"><i class="md-icon">more_horiz</i></button>' : '<button type="button" is="paper-icon-button-light" class="btnCancelJob listItemButton"><i class="md-icon">delete</i></button>'), html += "</" + tagName + ">"
    }

    function renderList(listInstance, jobs, apiClient) {
        if ((new Date).getTime() - listInstance.lastDataLoad < 6e4) return void refreshList(listInstance, jobs);
        listInstance.lastDataLoad = (new Date).getTime();
        for (var html = "", lastTargetName = "", mode = listInstance.options.mode, showTargetName = "download" !== mode, hasOpenSection = !1, i = 0, length = jobs.length; i < length; i++) {
            var job = jobs[i];
            if (showTargetName) {
                var targetName = job.TargetName || "Unknown";
                targetName !== lastTargetName && (lastTargetName && (html += "</div>", html += "<br/>", hasOpenSection = !1), lastTargetName = targetName, html += '<div class="verticalSection">', html += '<div class="sectionTitleContainer">', html += '<h2 class="sectionTitle">' + targetName + "</h2>", html += "</div>", html += '<div class="itemsContainer vertical-list paperList">', hasOpenSection = !0)
            }
            html += getSyncJobHtml(listInstance, job, apiClient)
        }
        hasOpenSection && (html += "</div>", html += "</div>");
        var elem = listInstance.options.element.querySelector(".syncJobListContent");
        html || (html = "download" === mode ? '<div style="padding:1em .25em;">' + globalize.translate("sharedcomponents#MessageNoDownloadsFound") + "</div>" : '<div style="padding:1em .25em;">' + globalize.translate("sharedcomponents#MessageNoSyncJobsFound") + "</div>"), elem.innerHTML = html, imageLoader.lazyChildren(elem)
    }

    function fetchData(listInstance) {
        listInstance.lastDataLoad = 0, loading.show();
        var options = {},
            apiClient = getApiClient(listInstance);
        return listInstance.options.userId && (options.UserId = listInstance.options.userId), "download" === listInstance.options.mode && (options.TargetId = apiClient.deviceId()), apiClient.getJSON(apiClient.getUrl("Sync/Jobs", options)).then(function(response) {
            renderList(listInstance, response.Items, apiClient), loading.hide()
        })
    }

    function getApiClient(listInstance) {
        return connectionManager.getApiClient(listInstance.options.serverId)
    }

    function showJobMenu(listInstance, elem) {
        var item = dom.parentWithClass(elem, "listItem"),
            jobId = item.getAttribute("data-id"),
            menuItems = (item.getAttribute("data-status"), []);
        "true" === item.getAttribute("data-canedit") && menuItems.push({
            name: globalize.translate("sharedcomponents#Edit"),
            id: "edit"
        });
        var txt = globalize.translate("sharedcomponents#RemoveDownload");
        menuItems.push({
            name: txt,
            id: "cancel"
        }), require(["actionsheet"], function(actionsheet) {
            actionsheet.show({
                items: menuItems,
                positionTo: elem,
                callback: function(id) {
                    switch (id) {
                        case "delete":
                        case "cancel":
                            cancelJob(listInstance, jobId);
                            break;
                        case "edit":
                            showJobEditor(listInstance, elem)
                    }
                }
            })
        })
    }

    function onElementClick(e) {
        var listInstance = this,
            btnJobMenu = dom.parentWithClass(e.target, "btnJobMenu");
        if (btnJobMenu) return void showJobMenu(listInstance, btnJobMenu);
        var btnCancelJob = dom.parentWithClass(e.target, "btnCancelJob");
        if (btnCancelJob) {
            var listItem = dom.parentWithClass(btnCancelJob, "listItem");
            if (listItem) {
                cancelJob(listInstance, listItem.getAttribute("data-id"))
            }
        } else showJobEditor(listInstance, e.target)
    }

    function showJobEditor(listInstance, elem) {
        var listItem = dom.parentWithClass(elem, "listItem");
        if (listItem && "true" === listItem.getAttribute("data-canedit")) {
            var jobId = listItem.getAttribute("data-id");
            require(["syncJobEditor"], function(syncJobEditor) {
                syncJobEditor.show({
                    serverId: listInstance.options.serverId,
                    jobId: jobId,
                    mode: listInstance.options.mode
                }).then(function() {
                    fetchData(listInstance)
                })
            })
        }
    }

    function syncJobList(options) {
        this.options = options;
        var onSyncJobCreatedHandler = onSyncJobCreated.bind(this);
        this.onSyncJobCreatedHandler = onSyncJobCreatedHandler, events.on(serverNotifications, "SyncJobCreated", onSyncJobCreatedHandler);
        var onSyncJobCancelledHandler = onSyncJobCancelled.bind(this);
        this.onSyncJobCancelledHandler = onSyncJobCancelledHandler, events.on(serverNotifications, "SyncJobCancelled", onSyncJobCancelledHandler);
        var onSyncJobUpdatedHandler = onSyncJobUpdated.bind(this);
        this.onSyncJobUpdatedHandler = onSyncJobUpdatedHandler, events.on(serverNotifications, "SyncJobUpdated", onSyncJobUpdatedHandler);
        var onClickHandler = onElementClick.bind(this);
        options.element.addEventListener("click", onClickHandler), this.onClickHandler = onClickHandler, options.element.innerHTML = '<div class="syncJobListContent"></div>', fetchData(this), initSupporterInfo(options.element, getApiClient(this))
    }

    function showSupporterInfo(context) {
        var html = '<button is="emby-button" class="raised button-submit block btnSyncSupporter" style="margin:1em 0;">';
        html += "<div>", html += globalize.translate("sharedcomponents#HeaderSyncRequiresSub"), html += "</div>", html += '<div style="margin-top:.5em;">', html += globalize.translate("sharedcomponents#LearnMore"), html += "</div>", html += "</button", context.insertAdjacentHTML("afterbegin", html), context.querySelector(".btnSyncSupporter").addEventListener("click", function() {
            registrationServices.validateFeature("sync")
        })
    }

    function initSupporterInfo(context, apiClient) {
        registrationServices.validateFeature("sync", {
            showDialog: !1
        }).catch(function() {
            showSupporterInfo(context, apiClient)
        })
    }
    return syncJobList.prototype.destroy = function() {
        var onSyncJobCreatedHandler = this.onSyncJobCreatedHandler;
        this.onSyncJobCreatedHandler = null, events.off(serverNotifications, "SyncJobCreated", onSyncJobCreatedHandler);
        var onSyncJobCancelledHandler = this.onSyncJobCancelledHandler;
        this.onSyncJobCancelledHandler = null, events.off(serverNotifications, "SyncJobCancelled", onSyncJobCancelledHandler);
        var onSyncJobUpdatedHandler = this.onSyncJobUpdatedHandler;
        this.onSyncJobUpdatedHandler = null, events.off(serverNotifications, "SyncJobUpdated", onSyncJobUpdatedHandler);
        var onClickHandler = this.onClickHandler;
        this.onClickHandler = null, this.options.element.removeEventListener("click", onClickHandler), this.options = null
    }, syncJobList
});