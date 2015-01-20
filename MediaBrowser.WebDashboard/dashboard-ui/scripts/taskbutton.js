
$.fn.taskButton = function (options) {

    function pollTasks(button) {

        ApiClient.getScheduledTasks({

            IsEnabled: true

        }).done(function (tasks) {

            updateTasks(button, tasks);
        });

    }

    function updateTasks(button, tasks) {

        var task = tasks.filter(function (t) {

            return t.Key == options.taskKey;

        })[0];

        if (options.panel) {
            if (task) {
                $(options.panel).show();
            } else {
                $(options.panel).hide();
            }
        }

        if (!task) {
            return;
        }

        button.buttonEnabled(task.State == 'Idle').attr('data-taskid', task.Id);

        var progress = (task.CurrentProgressPercentage || 0).toFixed(1);

        if (options.progressElem) {
            var progressElem = options.progressElem.val(progress);

            if (task.State == 'Running') {
                progressElem.show();
            } else {
                progressElem.hide();
            }
        }

        if (options.lastResultElem) {
            var lastResult = task.LastExecutionResult ? task.LastExecutionResult.Status : '';

            if (lastResult == "Failed") {
                options.lastResultElem.html('<span style="color:#FF0000;">' + Globalize.translate('LabelFailed') + '</span>');
            }
            else if (lastResult == "Cancelled") {
                options.lastResultElem.html('<span style="color:#0026FF;">' + Globalize.translate('LabelCancelled') + '</span>');
            }
            else if (lastResult == "Aborted") {
                options.lastResultElem.html('<span style="color:#FF0000;">' + Globalize.translate('LabelAbortedByServerShutdown') + '</span>');
            } else {
                options.lastResultElem.html(lastResult);
            }
        }
    }

    var self = this;

    if (options.panel) {
        $(options.panel).hide();
    }

    if (options.mode == 'off') {

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("ScheduledTasksInfoStop");
        }

        $(ApiClient).off(".taskbutton");

    } else {

        this.on('click', function () {

            var button = this;
            var id = button.getAttribute('data-taskid');

            ApiClient.startScheduledTask(id).done(function () {

                pollTasks(self);
            });

        });

        pollTasks(self);

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "1000,1000");
        }

        $(ApiClient).on("websocketmessage.taskbutton", function (e, msg) {

            if (msg.MessageType == "ScheduledTasksInfo") {

                var tasks = msg.Data;

                updateTasks(self, tasks);
            }

        }).on('websocketopen.taskbutton', function () {

            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "1000,1000");
            }
        });
    }

    return this;
};