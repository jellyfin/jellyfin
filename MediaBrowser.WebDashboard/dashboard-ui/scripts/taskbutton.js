define(["events", "userSettings", "serverNotifications", "connectionManager", "emby-button"], function(events, userSettings, serverNotifications, connectionManager) {
    "use strict";
    return function(options) {
        function pollTasks() {
            connectionManager.getApiClient(serverId).getScheduledTasks({
                IsEnabled: !0
            }).then(updateTasks)
        }

        function updateTasks(tasks) {
            var task = tasks.filter(function(t) {
                return t.Key == options.taskKey
            })[0];
            if (options.panel && (task ? options.panel.classList.remove("hide") : options.panel.classList.add("hide")), task) {
                "Idle" == task.State ? button.removeAttribute("disabled") : button.setAttribute("disabled", "disabled"), button.setAttribute("data-taskid", task.Id);
                var progress = (task.CurrentProgressPercentage || 0).toFixed(1);
                if (options.progressElem && (options.progressElem.value = progress, "Running" == task.State ? options.progressElem.classList.remove("hide") : options.progressElem.classList.add("hide")), options.lastResultElem) {
                    var lastResult = task.LastExecutionResult ? task.LastExecutionResult.Status : "";
                    "Failed" == lastResult ? options.lastResultElem.html('<span style="color:#FF0000;">(' + Globalize.translate("LabelFailed") + ")</span>") : "Cancelled" == lastResult ? options.lastResultElem.html('<span style="color:#0026FF;">(' + Globalize.translate("LabelCancelled") + ")</span>") : "Aborted" == lastResult ? options.lastResultElem.html('<span style="color:#FF0000;">' + Globalize.translate("LabelAbortedByServerShutdown") + "</span>") : options.lastResultElem.html(lastResult)
                }
            }
        }

        function onScheduledTaskMessageConfirmed(id) {
            connectionManager.getApiClient(serverId).startScheduledTask(id).then(pollTasks)
        }

        function onButtonClick() {
            onScheduledTaskMessageConfirmed(this.getAttribute("data-taskid"))
        }

        function onScheduledTasksUpdate(e, apiClient, info) {
            apiClient.serverId() === serverId && updateTasks(info)
        }

        function onPollIntervalFired() {
            connectionManager.getApiClient(serverId).isMessageChannelOpen() || pollTasks()
        }
        var pollInterval, button = options.button,
            serverId = ApiClient.serverId();
        options.panel && options.panel.classList.add("hide"), "off" == options.mode ? (button.removeEventListener("click", onButtonClick), events.off(serverNotifications, "ScheduledTasksInfo", onScheduledTasksUpdate), function() {
            connectionManager.getApiClient(serverId).sendMessage("ScheduledTasksInfoStop"), pollInterval && clearInterval(pollInterval)
        }()) : (button.addEventListener("click", onButtonClick), pollTasks(), function() {
            var apiClient = connectionManager.getApiClient(serverId);
            pollInterval && clearInterval(pollInterval), apiClient.sendMessage("ScheduledTasksInfoStart", "1000,1000"), pollInterval = setInterval(onPollIntervalFired, 1e4)
        }(), events.on(serverNotifications, "ScheduledTasksInfo", onScheduledTasksUpdate))
    }
});