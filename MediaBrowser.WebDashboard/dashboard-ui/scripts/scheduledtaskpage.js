define(["jQuery", "loading", "datetime", "dom", "globalize", "emby-input", "emby-button", "emby-select"], function($, loading, datetime, dom, globalize) {
    "use strict";

    function fillTimeOfDay(select) {
        for (var options = [], i = 0; i < 864e5; i += 9e5) options.push({
            name: ScheduledTaskPage.getDisplayTime(1e4 * i),
            value: 1e4 * i
        });
        select.innerHTML = options.map(function(o) {
            return '<option value="' + o.value + '">' + o.name + "</option>"
        }).join("")
    }
    Array.prototype.remove = function(from, to) {
        var rest = this.slice((to || from) + 1 || this.length);
        return this.length = from < 0 ? this.length + from : from, this.push.apply(this, rest)
    };
    var ScheduledTaskPage = {
        refreshScheduledTask: function(view) {
            loading.show();
            var id = getParameterByName("id");
            ApiClient.getScheduledTask(id).then(function(task) {
                ScheduledTaskPage.loadScheduledTask(view, task)
            })
        },
        loadScheduledTask: function(view, task) {
            $(".taskName", view).html(task.Name), $("#pTaskDescription", view).html(task.Description), require(["listViewStyle"], function() {
                ScheduledTaskPage.loadTaskTriggers(view, task)
            }), loading.hide()
        },
        loadTaskTriggers: function(context, task) {
            var html = "";
            html += '<div class="paperList">';
            for (var i = 0, length = task.Triggers.length; i < length; i++) {
                var trigger = task.Triggers[i];
                if (html += '<div class="listItem listItem-border">', html += '<i class="md-icon listItemIcon">schedule</i>', trigger.MaxRuntimeTicks ? html += '<div class="listItemBody two-line">' : html += '<div class="listItemBody">', html += "<div class='listItemBodyText'>" + ScheduledTaskPage.getTriggerFriendlyName(trigger) + "</div>", trigger.MaxRuntimeTicks) {
                    html += '<div class="listItemBodyText secondary">';
                    var hours = trigger.MaxRuntimeTicks / 36e9;
                    html += 1 == hours ? globalize.translate("ValueTimeLimitSingleHour") : globalize.translate("ValueTimeLimitMultiHour", hours), html += "</div>"
                }
                html += "</div>", html += '<button class="btnDeleteTrigger" data-index="' + i + '" type="button" is="paper-icon-button-light" title="' + globalize.translate("ButtonDelete") + '"><i class="md-icon">delete</i></button>', html += "</div>"
            }
            html += "</div>", context.querySelector(".taskTriggers").innerHTML = html
        },
        getTriggerFriendlyName: function(trigger) {
            if ("DailyTrigger" == trigger.Type) return "Daily at " + ScheduledTaskPage.getDisplayTime(trigger.TimeOfDayTicks);
            if ("WeeklyTrigger" == trigger.Type) return trigger.DayOfWeek + "s at " + ScheduledTaskPage.getDisplayTime(trigger.TimeOfDayTicks);
            if ("SystemEventTrigger" == trigger.Type && "WakeFromSleep" == trigger.SystemEvent) return "On wake from sleep";
            if ("IntervalTrigger" == trigger.Type) {
                var hours = trigger.IntervalTicks / 36e9;
                return .25 == hours ? "Every 15 minutes" : .5 == hours ? "Every 30 minutes" : .75 == hours ? "Every 45 minutes" : 1 == hours ? "Every hour" : "Every " + hours + " hours"
            }
            return "StartupTrigger" == trigger.Type ? "On application startup" : trigger.Type
        },
        getDisplayTime: function(ticks) {
            var ms = ticks / 1e4,
                now = new Date;
            return now.setHours(0, 0, 0, 0), now.setTime(now.getTime() + ms), datetime.getDisplayTime(now)
        },
        showAddTriggerPopup: function(view) {
            $("#selectTriggerType", view).val("DailyTrigger"), view.querySelector("#selectTriggerType").dispatchEvent(new CustomEvent("change", {})), $("#popupAddTrigger", view).removeClass("hide")
        },
        confirmDeleteTrigger: function(view, index) {
            require(["confirm"], function(confirm) {
                confirm(globalize.translate("MessageDeleteTaskTrigger"), globalize.translate("HeaderDeleteTaskTrigger")).then(function() {
                    ScheduledTaskPage.deleteTrigger(view, index)
                })
            })
        },
        deleteTrigger: function(view, index) {
            loading.show();
            var id = getParameterByName("id");
            ApiClient.getScheduledTask(id).then(function(task) {
                task.Triggers.remove(index), ApiClient.updateScheduledTaskTriggers(task.Id, task.Triggers).then(function() {
                    ScheduledTaskPage.refreshScheduledTask(view)
                })
            })
        },
        refreshTriggerFields: function(page, triggerType) {
            "DailyTrigger" == triggerType ? ($("#fldTimeOfDay", page).show(), $("#fldDayOfWeek", page).hide(), $("#fldSelectSystemEvent", page).hide(), $("#fldSelectInterval", page).hide(), $("#selectTimeOfDay", page).attr("required", "required")) : "WeeklyTrigger" == triggerType ? ($("#fldTimeOfDay", page).show(), $("#fldDayOfWeek", page).show(), $("#fldSelectSystemEvent", page).hide(), $("#fldSelectInterval", page).hide(), $("#selectTimeOfDay", page).attr("required", "required")) : "SystemEventTrigger" == triggerType ? ($("#fldTimeOfDay", page).hide(), $("#fldDayOfWeek", page).hide(), $("#fldSelectSystemEvent", page).show(), $("#fldSelectInterval", page).hide(), $("#selectTimeOfDay", page).removeAttr("required")) : "IntervalTrigger" == triggerType ? ($("#fldTimeOfDay", page).hide(), $("#fldDayOfWeek", page).hide(), $("#fldSelectSystemEvent", page).hide(), $("#fldSelectInterval", page).show(), $("#selectTimeOfDay", page).removeAttr("required")) : "StartupTrigger" == triggerType && ($("#fldTimeOfDay", page).hide(), $("#fldDayOfWeek", page).hide(), $("#fldSelectSystemEvent", page).hide(), $("#fldSelectInterval", page).hide(), $("#selectTimeOfDay", page).removeAttr("required"))
        },
        getTriggerToAdd: function(page) {
            var trigger = {
                Type: $("#selectTriggerType", page).val()
            };
            "DailyTrigger" == trigger.Type ? trigger.TimeOfDayTicks = $("#selectTimeOfDay", page).val() : "WeeklyTrigger" == trigger.Type ? (trigger.DayOfWeek = $("#selectDayOfWeek", page).val(), trigger.TimeOfDayTicks = $("#selectTimeOfDay", page).val()) : "SystemEventTrigger" == trigger.Type ? trigger.SystemEvent = $("#selectSystemEvent", page).val() : "IntervalTrigger" == trigger.Type && (trigger.IntervalTicks = $("#selectInterval", page).val());
            var timeLimit = $("#txtTimeLimit", page).val() || "0";
            return timeLimit = 36e5 * parseFloat(timeLimit), trigger.MaxRuntimeMs = timeLimit || null, trigger
        }
    };
    return function(view, params) {
        function onSubmit(e) {
            loading.show();
            var id = getParameterByName("id");
            ApiClient.getScheduledTask(id).then(function(task) {
                task.Triggers.push(ScheduledTaskPage.getTriggerToAdd(view)), ApiClient.updateScheduledTaskTriggers(task.Id, task.Triggers).then(function() {
                    $("#popupAddTrigger").addClass("hide"), ScheduledTaskPage.refreshScheduledTask(view)
                })
            }), e.preventDefault()
        }
        view.querySelector(".addTriggerForm").addEventListener("submit", onSubmit), fillTimeOfDay(view.querySelector("#selectTimeOfDay")), $(view.querySelector("#popupAddTrigger").parentNode).trigger("create"), view.querySelector(".selectTriggerType").addEventListener("change", function() {
            ScheduledTaskPage.refreshTriggerFields(view, this.value)
        }), view.querySelector(".btnAddTrigger").addEventListener("click", function() {
            ScheduledTaskPage.showAddTriggerPopup(view)
        }), view.addEventListener("click", function(e) {
            var btnDeleteTrigger = dom.parentWithClass(e.target, "btnDeleteTrigger");
            btnDeleteTrigger && ScheduledTaskPage.confirmDeleteTrigger(view, parseInt(btnDeleteTrigger.getAttribute("data-index")))
        }), view.addEventListener("viewshow", function() {
            ScheduledTaskPage.refreshScheduledTask(view)
        })
    }
});