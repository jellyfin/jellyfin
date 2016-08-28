define(['appStorage', 'emby-button'], function (appStorage) {

    return function (options) {

        var button = options.button;

        function pollTasks() {

            ApiClient.getScheduledTasks({

                IsEnabled: true

            }).then(updateTasks);
        }

        function updateTasks(tasks) {

            var task = tasks.filter(function (t) {

                return t.Key == options.taskKey;

            })[0];

            if (options.panel) {
                if (task) {
                    options.panel.classList.remove('hide');
                } else {
                    options.panel.classList.add('hide');
                }
            }

            if (!task) {
                return;
            }

            if (task.State == 'Idle') {
                button.removeAttribute('disabled');
            } else {
                button.setAttribute('disabled', 'disabled');
            }

            button.setAttribute('data-taskid', task.Id);

            var progress = (task.CurrentProgressPercentage || 0).toFixed(1);

            if (options.progressElem) {
                options.progressElem.value = progress;

                if (task.State == 'Running') {
                    options.progressElem.classList.remove('hide');
                } else {
                    options.progressElem.classList.add('hide');
                }
            }

            if (options.lastResultElem) {
                var lastResult = task.LastExecutionResult ? task.LastExecutionResult.Status : '';

                if (lastResult == "Failed") {
                    options.lastResultElem.html('<span style="color:#FF0000;">(' + Globalize.translate('LabelFailed') + ')</span>');
                }
                else if (lastResult == "Cancelled") {
                    options.lastResultElem.html('<span style="color:#0026FF;">(' + Globalize.translate('LabelCancelled') + ')</span>');
                }
                else if (lastResult == "Aborted") {
                    options.lastResultElem.html('<span style="color:#FF0000;">' + Globalize.translate('LabelAbortedByServerShutdown') + '</span>');
                } else {
                    options.lastResultElem.html(lastResult);
                }
            }
        }

        function onScheduledTaskMessageConfirmed(id) {
            ApiClient.startScheduledTask(id).then(pollTasks);
        }

        function onButtonClick() {

            var button = this;
            var id = button.getAttribute('data-taskid');

            var key = 'scheduledTaskButton' + options.taskKey;
            var expectedValue = new Date().getMonth() + '6';

            if (appStorage.getItem(key) == expectedValue) {
                onScheduledTaskMessageConfirmed(id);
            } else {

                var msg = Globalize.translate('ConfirmMessageScheduledTaskButton');
                msg += '<br/>';
                msg += '<div style="margin-top:1em;">';
                msg += '<a class="clearLink" href="scheduledtasks.html"><button is="emby-button" type="button" style="color:#3f51b5!important;margin:0;">' + Globalize.translate('ButtonScheduledTasks') + '</button></a>';
                msg += '</div>';

                require(['confirm'], function (confirm) {

                    confirm({

                        title: Globalize.translate('HeaderConfirmation'),
                        html: msg,
                        text: Globalize.translate('ConfirmMessageScheduledTaskButton') + "\n\n" + Globalize.translate('ButtonScheduledTasks')

                    }).then(function () {
                        appStorage.setItem(key, expectedValue);
                        onScheduledTaskMessageConfirmed(id);
                    });

                });
            }
        }

        function onSocketOpen() {
            startInterval();
        }

        function onSocketMessage(e, msg) {
            if (msg.MessageType == "ScheduledTasksInfo") {

                var tasks = msg.Data;

                updateTasks(tasks);
            }
        }

        var pollInterval;

        function onPollIntervalFired() {

            if (!ApiClient.isWebSocketOpen()) {
                pollTasks();
            }
        }

        function startInterval() {
            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "1000,1000");
            }
            if (pollInterval) {
                clearInterval(pollInterval);
            }
            pollInterval = setInterval(onPollIntervalFired, 5000);
        }

        function stopInterval() {
            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("ScheduledTasksInfoStop");
            }
            if (pollInterval) {
                clearInterval(pollInterval);
            }
        }

        if (options.panel) {
            options.panel.classList.add('hide');
        }

        if (options.mode == 'off') {

            button.removeEventListener('click', onButtonClick);
            Events.off(ApiClient, 'websocketmessage', onSocketMessage);
            Events.off(ApiClient, 'websocketopen', onSocketOpen);
            stopInterval();

        } else  {

            button.addEventListener('click', onButtonClick);

            pollTasks();

            startInterval();

            Events.on(ApiClient, 'websocketmessage', onSocketMessage);
            Events.on(ApiClient, 'websocketopen', onSocketOpen);
        }
    };
});