
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

    function onScheduledTaskMessageConfirmed(instance, id) {
        ApiClient.startScheduledTask(id).done(function () {

            pollTasks(instance);
        });
    }

    function onButtonClick() {

        var button = this;
        var id = button.getAttribute('data-taskid');

        var key = 'scheduledTaskButton' + options.taskKey;
        var expectedValue = '4';

        if (appStorage.getItem(key) == expectedValue) {
            onScheduledTaskMessageConfirmed(button, id);
        } else {

            var msg = Globalize.translate('ConfirmMessageScheduledTaskButton');
            msg += '<br/>';
            msg += '<br/>';
            msg += '<a href="scheduledtasks.html">' + Globalize.translate('ButtonScheduledTasks') + '</a>';

            Dashboard.confirm(msg, Globalize.translate('HeaderConfirmation'), function (result) {

                if (result) {

                    appStorage.setItem(key, expectedValue);
                    onScheduledTaskMessageConfirmed(button, id);
                }
            });

        }
    }

    function onSocketOpen() {
        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "1000,1000");
        }
    }

    function onSocketMessage(e, msg) {
        if (msg.MessageType == "ScheduledTasksInfo") {

            var tasks = msg.Data;

            updateTasks(self, tasks);
        }
    }

    var self = this;

    if (options.panel) {
        $(options.panel).hide();
    }

    if (options.mode == 'off') {

        this.off('click', onButtonClick);
        $(ApiClient).off("websocketmessage", onSocketMessage).off('websocketopen', onSocketOpen);

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("ScheduledTasksInfoStop");
        }

    } else if (this.length) {

        this.on('click', onButtonClick);

        pollTasks(self);

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "1000,1000");
        }

        $(ApiClient).on("websocketmessage", onSocketMessage).on('websocketopen', onSocketOpen);
    }

    return this;
};