var ScheduledTasksPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        ScheduledTasksPage.reloadList(true);

        $(document).on("websocketmessage", ScheduledTasksPage.onWebSocketMessage).on("websocketopen", ScheduledTasksPage.onWebSocketConnectionChange).on("websocketerror", ScheduledTasksPage.onWebSocketConnectionChange).on("websocketclose", ScheduledTasksPage.onWebSocketConnectionChange);
    },

    onPageHide: function () {
        $(document).off("websocketmessage", ScheduledTasksPage.onWebSocketMessage).off("websocketopen", ScheduledTasksPage.onWebSocketConnectionChange).off("websocketerror", ScheduledTasksPage.onWebSocketConnectionChange).off("websocketclose", ScheduledTasksPage.onWebSocketConnectionChange);
        ScheduledTasksPage.stopInterval();
    },

    startInterval: function () {

        if (Dashboard.isWebSocketOpen()) {
            Dashboard.sendWebSocketMessage("ScheduledTasksInfoStart", "1500,1500");
        }
    },

    stopInterval: function () {

        if (Dashboard.isWebSocketOpen()) {
            Dashboard.sendWebSocketMessage("ScheduledTasksInfoStop");
        }
    },

    onWebSocketMessage: function (e, msg) {

        if (msg.MessageType == "ScheduledTasksInfo") {
            ScheduledTasksPage.populateList(msg.Data);
        }
    },
    
    onWebSocketConnectionChange: function() {
        ScheduledTasksPage.reloadList(true);
    },

    reloadList: function (updateInterval) {

        if (updateInterval) {
            ScheduledTasksPage.stopInterval();
        }

        ApiClient.getScheduledTasks().done(function (tasks) {
            ScheduledTasksPage.populateList(tasks);
            Dashboard.hideLoadingMsg();

            if (updateInterval) {
                ScheduledTasksPage.startInterval();
            }

        });
    },

    populateList: function (tasks) {

        tasks = tasks.sort(function (a, b) {

            a = a.Category + " " + a.Name;
            b = b.Category + " " + b.Name;

            if (a == b) {
                return 0;
            }

            if (a < b) {
                return -1;
            }

            return 1;
        });

        var page = $($.mobile.activePage);

        var html = "";

        var currentCategory;

        for (var i = 0, length = tasks.length; i < length; i++) {

            var task = tasks[i];

            if (task.Category != currentCategory) {
                currentCategory = task.Category;

                html += "<li data-role='list-divider'>" + currentCategory + "</li>";
            }

            html += "<li>";

            html += "<a href='scheduledTask.html?id=" + task.Id + "'>";

            html += "<h3>" + task.Name + "</h3>";

            if (task.State == "Idle") {

                if (task.LastExecutionResult) {

                    var text = "Last ran " + humane_date(task.LastExecutionResult.EndTimeUtc) + ', taking ' + humane_elapsed(task.LastExecutionResult.StartTimeUtc, task.LastExecutionResult.EndTimeUtc);

                    if (task.LastExecutionResult.Status == "Failed") {
                        text += " <span style='color:#FF0000;'>(failed)</span>";
                    }
                    else if (task.LastExecutionResult.Status == "Cancelled") {
                        text += " <span style='color:#0026FF;'>(cancelled)</span>";
                    }
                    else if (task.LastExecutionResult.Status == "Aborted") {
                        text += " <span style='color:#FF0000;'>(Aborted by server shutdown)</span>";
                    }

                    html += "<p>" + text + "</p>";
                }

                html += "<a href='#' data-icon='play' onclick='ScheduledTasksPage.startTask(\"" + task.Id + "\");'>Start</a>";
            }
            else if (task.State == "Running") {

                var progress = Math.round(task.CurrentProgressPercentage || 0);
                
                html += '<p><progress max="100" value="' + progress + '" title="' + progress + '%">';
                html += '' + progress + '%';
                html += '</progress>';

                html += "<span style='color:#009F00;margin-left:5px;'>" + progress + "%</span>";
                html += '</p>';

                html += "<a href='#' data-icon='stop' onclick='ScheduledTasksPage.stopTask(\"" + task.Id + "\");'>Stop</a>";

            } else {

                html += "<p style='color:#FF0000;'>Stopping</p>";
                html += "<a href='#' data-icon='play' style='visibility:hidden;'>Start</a>";
            }

            html += "</a>";

            html += "</li>";
        }

        $('#ulScheduledTasks', page).html(html).listview('refresh');
    },

    startTask: function (id) {

        Dashboard.showLoadingMsg();

        ApiClient.startScheduledTask(id).done(function (result) {

            ScheduledTasksPage.reloadList();
        });

    },

    stopTask: function (id) {

        Dashboard.showLoadingMsg();

        ApiClient.stopScheduledTask(id).done(function (result) {

            ScheduledTasksPage.reloadList();
        });
    }
};

$(document).on('pageshow', "#scheduledTasksPage", ScheduledTasksPage.onPageShow).on('pagehide', "#scheduledTasksPage", ScheduledTasksPage.onPageHide);