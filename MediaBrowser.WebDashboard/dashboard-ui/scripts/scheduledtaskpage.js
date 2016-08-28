define(['jQuery'], function ($) {

    // Array Remove - By John Resig (MIT Licensed)
    Array.prototype.remove = function (from, to) {
        var rest = this.slice((to || from) + 1 || this.length);
        this.length = from < 0 ? this.length + from : from;
        return this.push.apply(this, rest);
    };

    window.ScheduledTaskPage = {

        refreshScheduledTask: function () {
            Dashboard.showLoadingMsg();

            var id = getParameterByName('id');


            ApiClient.getScheduledTask(id).then(ScheduledTaskPage.loadScheduledTask);
        },

        loadScheduledTask: function (task) {

            var page = $($.mobile.activePage)[0];

            $('.taskName', page).html(task.Name);

            $('#pTaskDescription', page).html(task.Description);

            require(['listViewStyle'], function () {
                ScheduledTaskPage.loadTaskTriggers(page, task);
            });

            Dashboard.hideLoadingMsg();
        },

        loadTaskTriggers: function (context, task) {

            var html = '';

            html += '<div class="paperList">';

            for (var i = 0, length = task.Triggers.length; i < length; i++) {

                var trigger = task.Triggers[i];

                html += '<div class="listItem">';

                html += '<i class="md-icon listItemIcon">schedule</i>';

                if (trigger.MaxRuntimeMs) {
                    html += '<div class="listItemBody two-line">';
                } else {
                    html += '<div class="listItemBody">';
                }

                html += "<div class='listItemBodyText'>" + ScheduledTaskPage.getTriggerFriendlyName(trigger) + "</div>";

                if (trigger.MaxRuntimeMs) {
                    html += '<div class="listItemBodyText secondary">';

                    var hours = trigger.MaxRuntimeMs / 3600000;

                    if (hours == 1) {
                        html += Globalize.translate('ValueTimeLimitSingleHour');
                    } else {
                        html += Globalize.translate('ValueTimeLimitMultiHour', hours);
                    }
                    html += '</div>';
                }

                html += '</div>';

                html += '<button type="button" is="paper-icon-button-light" title="' + Globalize.translate('ButtonDelete') + '" onclick="ScheduledTaskPage.confirmDeleteTrigger(' + i + ');"><i class="md-icon">delete</i></button>';

                html += '</div>';
            }

            html += '</div>';

            context.querySelector('.taskTriggers').innerHTML = html;
        },

        getTriggerFriendlyName: function (trigger) {

            if (trigger.Type == 'DailyTrigger') {
                return 'Daily at ' + ScheduledTaskPage.getDisplayTime(trigger.TimeOfDayTicks);
            }

            if (trigger.Type == 'WeeklyTrigger') {

                return trigger.DayOfWeek + 's at ' + ScheduledTaskPage.getDisplayTime(trigger.TimeOfDayTicks);
            }

            if (trigger.Type == 'SystemEventTrigger') {

                if (trigger.SystemEvent == 'WakeFromSleep') {
                    return 'On wake from sleep';
                }
            }

            if (trigger.Type == 'IntervalTrigger') {

                var hours = trigger.IntervalTicks / 36000000000;

                if (hours == .25) {
                    return "Every 15 minutes";
                }
                if (hours == .5) {
                    return "Every 30 minutes";
                }
                if (hours == .75) {
                    return "Every 45 minutes";
                }
                if (hours == 1) {
                    return "Every hour";
                }

                return 'Every ' + hours + ' hours';
            }

            if (trigger.Type == 'StartupTrigger') {
                return 'On application startup';
            }

            return trigger.Type;
        },

        getDisplayTime: function (ticks) {

            var hours = ticks / 36000000000;

            if (hours < 1) {
                hours = 0;
            }

            hours = Math.floor(hours);

            ticks -= (hours * 36000000000);

            var minutes = Math.floor((ticks / 600000000));

            var suffix = "am";

            if (hours > 11) {
                suffix = "pm";
            }

            hours = hours % 12;

            if (hours == 0) {
                hours = 12;
            }

            if (minutes < 10) {
                minutes = '0' + minutes;
            }

            return hours + ':' + minutes + ' ' + suffix;
        },

        showAddTriggerPopup: function () {

            var page = $.mobile.activePage;

            $('#selectTriggerType', page).val('DailyTrigger').trigger('change');

            $('#popupAddTrigger', page).on("popupafteropen", function () {
                $('#addTriggerForm input:first', this).focus();
            }).popup("open").on("popupafterclose", function () {

                $('#addTriggerForm', page).off("submit");
                $(this).off("popupafterclose");
            });
        },

        confirmDeleteTrigger: function (index) {

            require(['confirm'], function (confirm) {
                confirm(Globalize.translate('MessageDeleteTaskTrigger'), Globalize.translate('HeaderDeleteTaskTrigger')).then(function () {
                    ScheduledTaskPage.deleteTrigger(index);
                });
            });
        },

        deleteTrigger: function (index) {

            Dashboard.showLoadingMsg();

            var id = getParameterByName('id');


            ApiClient.getScheduledTask(id).then(function (task) {

                task.Triggers.remove(index);

                ApiClient.updateScheduledTaskTriggers(task.Id, task.Triggers).then(function () {

                    ScheduledTaskPage.refreshScheduledTask();

                });

            });
        },

        refreshTriggerFields: function (triggerType) {

            var page = $.mobile.activePage;

            if (triggerType == 'DailyTrigger') {

                $('#fldTimeOfDay', page).show();
                $('#fldDayOfWeek', page).hide();
                $('#fldSelectSystemEvent', page).hide();
                $('#fldSelectInterval', page).hide();
                $('#txtTimeOfDay', page).attr('required', 'required');
            }

            else if (triggerType == 'WeeklyTrigger') {
                $('#fldTimeOfDay', page).show();
                $('#fldDayOfWeek', page).show();
                $('#fldSelectSystemEvent', page).hide();
                $('#fldSelectInterval', page).hide();
                $('#txtTimeOfDay', page).attr('required', 'required');
            }

            else if (triggerType == 'SystemEventTrigger') {
                $('#fldTimeOfDay', page).hide();
                $('#fldDayOfWeek', page).hide();
                $('#fldSelectSystemEvent', page).show();
                $('#fldSelectInterval', page).hide();
                $('#txtTimeOfDay', page).removeAttr('required');
            }

            else if (triggerType == 'IntervalTrigger') {
                $('#fldTimeOfDay', page).hide();
                $('#fldDayOfWeek', page).hide();
                $('#fldSelectSystemEvent', page).hide();
                $('#fldSelectInterval', page).show();
                $('#txtTimeOfDay', page).removeAttr('required');
            }

            else if (triggerType == 'StartupTrigger') {
                $('#fldTimeOfDay', page).hide();
                $('#fldDayOfWeek', page).hide();
                $('#fldSelectSystemEvent', page).hide();
                $('#fldSelectInterval', page).hide();
                $('#txtTimeOfDay', page).removeAttr('required');
            }
        },

        getTriggerToAdd: function () {

            var page = $.mobile.activePage;

            var trigger = {
                Type: $('#selectTriggerType', page).val()
            };

            if (trigger.Type == 'DailyTrigger') {
                trigger.TimeOfDayTicks = ScheduledTaskPage.getTimeOfDayTicks($('#txtTimeOfDay', page).val());
            }

            else if (trigger.Type == 'WeeklyTrigger') {
                trigger.DayOfWeek = $('#selectDayOfWeek', page).val();
                trigger.TimeOfDayTicks = ScheduledTaskPage.getTimeOfDayTicks($('#txtTimeOfDay', page).val());
            }

            else if (trigger.Type == 'SystemEventTrigger') {
                trigger.SystemEvent = $('#selectSystemEvent', page).val();
            }

            else if (trigger.Type == 'IntervalTrigger') {
                trigger.IntervalTicks = $('#selectInterval', page).val();
            }

            var timeLimit = $('#txtTimeLimit', page).val() || '0';
            timeLimit = parseFloat(timeLimit) * 3600000;

            trigger.MaxRuntimeMs = timeLimit || null;

            return trigger;
        },

        getTimeOfDayTicks: function (val) {

            var vals = val.split(':');

            var hours = vals[0];
            var minutes = vals[1].split(' ')[0];

            // Add hours
            var ticks = hours * 60 * 60 * 1000 * 10000;

            ticks += minutes * 60 * 1000 * 10000;

            return ticks;
        }
    };

    (function () {

        function onSubmit() {

            Dashboard.showLoadingMsg();

            var id = getParameterByName('id');

            ApiClient.getScheduledTask(id).then(function (task) {

                task.Triggers.push(ScheduledTaskPage.getTriggerToAdd());

                ApiClient.updateScheduledTaskTriggers(task.Id, task.Triggers).then(function () {

                    $('#popupAddTrigger').popup('close');

                    ScheduledTaskPage.refreshScheduledTask();

                });

            });

            return false;
        }

        $(document).on('pageinit', "#scheduledTaskPage", function () {

            var page = this;

            $('.addTriggerForm').off('submit', onSubmit).on('submit', onSubmit);

            page.querySelector('.timeFieldExample').innerHTML = Globalize.translate('ValueExample', '1:00 PM');

        }).on('pageshow', "#scheduledTaskPage", function () {

            ScheduledTaskPage.refreshScheduledTask();
        });

    })();

});