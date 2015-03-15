(function () {

    function renderJob(page, job, dialogOptions) {

        var html = '';

        html += '<div>';
        html += Globalize.translate('ValueDateCreated', parseISO8601Date(job.DateCreated, { toLocal: true }).toLocaleString());
        html += '</div>';
        html += '<br/>';
        html += '<div class="formFields"></div>';

        html += '<br/>';
        html += '<br/>';
        html += '<button type="submit" data-icon="check">' + Globalize.translate('ButtonSave') + '</button>';

        $('.syncJobForm', page).html(html).trigger('create');
        SyncManager.renderForm({
            elem: $('.formFields', page),
            dialogOptions: dialogOptions,
            dialogOptionsFn: getTargetDialogOptionsFn(dialogOptions),
            showName: true,
            readOnlySyncTarget: true
        });
        fillJobValues(page, job, dialogOptions);
    }

    function getTargetDialogOptionsFn(dialogOptions) {

        return function (targetId) {

            var deferred = $.Deferred();

            deferred.resolveWith(null, [dialogOptions]);
            return deferred.promise();
        };
    }

    function getJobItemHtml(jobItem, index) {

        var html = '';

        var cssClass = 'ui-li-has-thumb listItem';

        html += '<li class="' + cssClass + '"' + ' data-itemid="' + jobItem.Id + '" data-status="' + jobItem.Status + '" data-remove="' + jobItem.IsMarkedForRemoval + '">';

        var hasActions = ['Queued', 'Cancelled', 'Failed', 'ReadyToTransfer', 'Transferring', 'Converting', 'Synced'].indexOf(jobItem.Status) != -1;

        html += '<a href="#">';

        var imgUrl;

        if (jobItem.PrimaryImageItemId) {

            imgUrl = ApiClient.getImageUrl(jobItem.PrimaryImageItemId, {
                type: "Primary",
                width: 80,
                tag: jobItem.PrimaryImageTag,
                minScale: 1.5
            });
        }

        if (imgUrl) {

            if (index < 10) {
                html += '<div class="listviewImage ui-li-thumb" style="background-image:url(\'' + imgUrl + '\');"></div>';
            } else {
                html += '<div class="listviewImage ui-li-thumb lazy" data-src="' + imgUrl + '"></div>';
            }
        }

        html += '<h3>';
        html += jobItem.ItemName;
        html += '</h3>';

        if (jobItem.Status == 'Failed') {
            html += '<p style="color:red;">';
        } else {
            html += '<p>';
        }
        html += Globalize.translate('SyncJobItemStatus' + jobItem.Status);
        if (jobItem.Status == 'Synced' && jobItem.IsMarkedForRemoval) {
            html += '<br/>';
            html += Globalize.translate('SyncJobItemStatusSyncedMarkForRemoval');
        }
        html += '</p>';

        html += '</a>';

        if (hasActions) {

            html += '<a href="#" data-icon="ellipsis-v" class="listviewMenuButton btnJobItemMenu">';
        } else {
            html += '<a href="#" data-icon="ellipsis-v" class="listviewMenuButton btnJobItemMenu" style="visibility:hidden;">';
        }
        html += '</a>';

        html += '</li>';
        return html;
    }

    function renderJobItems(page, items) {

        var html = '';

        html += '<ul data-role="listview" class="itemsListview">';

        html += '<li data-role="list-divider">';
        html += Globalize.translate('HeaderItems');
        html += '</li>';

        var index = 0;
        html += items.map(function (i) {

            return getJobItemHtml(i, index++);

        }).join('');

        html += '</ul>';

        var elem = $('.jobItems', page).html(html).trigger('create');

        $(".lazy", elem).unveil(200);

        $('.btnJobItemMenu', elem).on('click', function () {
            showJobItemMenu(this);
        });
    }

    function showJobItemMenu(elem) {

        var page = $(elem).parents('.page');
        var listItem = $(elem).parents('li');
        var id = listItem.attr('data-itemid');
        var status = listItem.attr('data-status');
        var remove = listItem.attr('data-remove').toLowerCase() == 'true';

        $('.jobMenu', page).popup("close").remove();

        var html = '<div data-role="popup" class="jobMenu tapHoldMenu" data-theme="a">';

        html += '<ul data-role="listview" style="min-width: 180px;">';
        html += '<li data-role="list-divider">' + Globalize.translate('HeaderMenu') + '</li>';

        if (status == 'Failed') {
            html += '<li data-icon="check"><a href="#" class="btnRetryJobItem" data-id="' + id + '">' + Globalize.translate('ButtonQueueForRetry') + '</a></li>';
        }
        else if (status == 'Cancelled') {
            html += '<li data-icon="check"><a href="#" class="btnRetryJobItem" data-id="' + id + '">' + Globalize.translate('ButtonReenable') + '</a></li>';
        }
        else if (status == 'Queued' || status == 'Transferring' || status == 'Converting' || status == 'ReadyToTransfer') {
            html += '<li data-icon="delete"><a href="#" class="btnCancelJobItem" data-id="' + id + '">' + Globalize.translate('ButtonCancelItem') + '</a></li>';
        }
        else if (status == 'Synced' && remove) {
            html += '<li data-icon="check"><a href="#" class="btnUnmarkForRemoval" data-id="' + id + '">' + Globalize.translate('ButtonUnmarkForRemoval') + '</a></li>';
        }
        else if (status == 'Synced') {
            html += '<li data-icon="check"><a href="#" class="btnMarkForRemoval" data-id="' + id + '">' + Globalize.translate('ButtonMarkForRemoval') + '</a></li>';
        }

        html += '</ul>';

        html += '</div>';

        page.append(html);

        var flyout = $('.jobMenu', page).popup({ positionTo: elem || "window" }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();

        });

        $('.btnCancelJobItem', flyout).on('click', function () {
            cancelJobItem(page, this.getAttribute('data-id'));
        });

        $('.btnRetryJobItem', flyout).on('click', function () {
            retryJobItem(page, this.getAttribute('data-id'));
        });

        $('.btnUnmarkForRemoval', flyout).on('click', function () {
            unMarkForRemoval(page, this.getAttribute('data-id'));
        });

        $('.btnMarkForRemoval', flyout).on('click', function () {
            markForRemoval(page, this.getAttribute('data-id'));
        });
    }

    function cancelJobItem(page, jobItemId) {

        $('.jobMenu', page).popup('close');

        // Need a timeout because jquery mobile will not show a popup while another is in the act of closing

        Dashboard.showLoadingMsg();

        ApiClient.ajax({

            type: "DELETE",
            url: ApiClient.getUrl('Sync/JobItems/' + jobItemId)

        }).done(function () {

            loadJob(page);
        });

    }

    function markForRemoval(page, jobItemId) {

        $('.jobMenu', page).popup('close');

        ApiClient.ajax({

            type: "POST",
            url: ApiClient.getUrl('Sync/JobItems/' + jobItemId + '/MarkForRemoval')

        }).done(function () {

            loadJob(page);
        });
    }

    function unMarkForRemoval(page, jobItemId) {

        $('.jobMenu', page).popup('close');

        ApiClient.ajax({

            type: "POST",
            url: ApiClient.getUrl('Sync/JobItems/' + jobItemId + '/UnmarkForRemoval')

        }).done(function () {

            loadJob(page);
        });
    }

    function retryJobItem(page, jobItemId) {

        $('.jobMenu', page).popup('close');

        ApiClient.ajax({

            type: "POST",
            url: ApiClient.getUrl('Sync/JobItems/' + jobItemId + '/Enable')

        }).done(function () {

            loadJob(page);
        });
    }

    function fillJobValues(page, job, editOptions) {

        $('#txtSyncJobName', page).val(job.Name);
        $('#selectProfile', page).val(job.Profile || '').trigger('change').selectmenu('refresh');
        $('#selectQuality', page).val(job.Quality || '').trigger('change').selectmenu('refresh');
        $('#chkUnwatchedOnly', page).checked(job.UnwatchedOnly).checkboxradio('refresh');
        $('#chkSyncNewContent', page).checked(job.SyncNewContent).checkboxradio('refresh');
        $('#txtItemLimit', page).val(job.ItemLimit);

        var target = editOptions.Targets.filter(function (t) {
            return t.Id == job.TargetId;
        })[0];
        var targetName = target ? target.Name : '';

        $('#selectSyncTarget', page).val(targetName);
    }

    var _jobOptions;
    function loadJob(page) {

        Dashboard.showLoadingMsg();
        var id = getParameterByName('id');

        ApiClient.getJSON(ApiClient.getUrl('Sync/Jobs/' + id)).done(function (job) {

            ApiClient.getJSON(ApiClient.getUrl('Sync/Options', {

                UserId: job.UserId,
                ItemIds: (job.RequestedItemIds && job.RequestedItemIds.length ? job.RequestedItemIds.join('') : null),

                ParentId: job.ParentId,
                Category: job.Category,
                TargetId: job.TargetId

            })).done(function (options) {

                _jobOptions = options;
                renderJob(page, job, options);
                Dashboard.hideLoadingMsg();
            });
        });

        ApiClient.getJSON(ApiClient.getUrl('Sync/JobItems', {

            JobId: id,
            AddMetadata: true

        })).done(function (result) {

            renderJobItems(page, result.Items);
            Dashboard.hideLoadingMsg();
        });
    }

    function loadJobInfo(page, job, jobItems) {

        renderJob(page, job, _jobOptions);
        renderJobItems(page, jobItems);
        Dashboard.hideLoadingMsg();
    }

    function saveJob(page) {

        Dashboard.showLoadingMsg();
        var id = getParameterByName('id');

        ApiClient.getJSON(ApiClient.getUrl('Sync/Jobs/' + id)).done(function (job) {

            var quality = $('#selectQuality', page).val();

            if (quality == 'custom') {
                quality = parseFloat($('#txtBitrate', page).val()) * 1000000;
            }

            job.Name = $('#txtSyncJobName', page).val();
            job.Quality = quality || null;
            job.Profile = $('#selectProfile', page).val() || null;
            job.ItemLimit = $('#txtItemLimit', page).val() || job.ItemLimit;
            job.SyncNewContent = $('#chkSyncNewContent', page).checked();
            job.UnwatchedOnly = $('#chkUnwatchedOnly', page).checked();

            ApiClient.ajax({

                url: ApiClient.getUrl('Sync/Jobs/' + id),
                type: 'POST',
                data: JSON.stringify(job),
                contentType: "application/json"

            }).done(function () {

                Dashboard.hideLoadingMsg();
                Dashboard.alert(Globalize.translate('SettingsSaved'));
            });
        });

    }

    function onWebSocketMessage(e, msg) {

        var page = $.mobile.activePage;

        if (msg.MessageType == "SyncJob") {
            loadJobInfo(page, msg.Data.Job, msg.Data.JobItems);
        }
    }

    function startListening(page) {

        var startParams = "0,1500";

        startParams += "," + getParameterByName('id');

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("SyncJobStart", startParams);
        }

    }

    function stopListening() {

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("SyncJobStop", "");
        }

    }

    $(document).on('pageshow', ".syncJobPage", function () {

        var page = this;
        loadJob(page);

        startListening(page);
        $(ApiClient).on("websocketmessage.syncJobPage", onWebSocketMessage);

    }).on('pagehide', ".syncJobPage", function () {

        var page = this;

        stopListening();
        $(ApiClient).off(".syncJobPage");
    });

    window.SyncJobPage = {

        onSubmit: function () {

            var form = this;

            var page = $(form).parents('.page');

            saveJob(page);

            return false;
        }
    };

})();