var ScheduledTaskPage = {

    onPageShow: function () {

        ScheduledTaskPage.refreshScheduledTask();
    },

    refreshScheduledTask: function () {
        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');


        ApiClient.getScheduledTask(id).done(ScheduledTaskPage.loadScheduledTask);
    },

    loadScheduledTask: function (task) {

        Dashboard.setPageTitle(task.Name);

        $('#pTaskDescription', $.mobile.activePage).html(task.Description);

        ScheduledTaskPage.loadTaskTriggers(task);

        Dashboard.hideLoadingMsg();
    },

    loadTaskTriggers: function (task) {

        var html = '';

        html += '<li data-role="list-divider"><h3>' + Globalize.translate('HeaderTaskTriggers') + '</h3></li>';

        for (var i = 0, length = task.Triggers.length; i < length; i++) {

            var trigger = task.Triggers[i];

            html += '<li>';

            html += '<a href="#">';
            html += ScheduledTaskPage.getTriggerFriendlyName(trigger);
            html += '</a>';

            html += '<a href="#" onclick="ScheduledTaskPage.confirmDeleteTrigger(' + i + ');">';
            html += Globalize.translate('Delete');
            html += '</a>';

            html += '</li>';
        }

        $('#ulTaskTriggers', $.mobile.activePage).html(html).listview('refresh');
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

        $('#selectTriggerType', page).val('DailyTrigger').trigger('change').selectmenu('refresh');

        $('#popupAddTrigger', page).on("popupafteropen",function() {
            $('#addTriggerForm input:first', this).focus();
        }).popup("open").on("popupafterclose", function () {

            $('#addTriggerForm', page).off("submit");
            $(this).off("popupafterclose");
        });
    },
    
    onSubmit: function() {
        
        ScheduledTaskPage.addTrigger();

        return false;
    },

    addTrigger: function () {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        ApiClient.getScheduledTask(id).done(function (task) {

            task.Triggers.push(ScheduledTaskPage.getTriggerToAdd());

            ApiClient.updateScheduledTaskTriggers(task.Id, task.Triggers).done(function () {

                $('#popupAddTrigger').popup('close');

                ScheduledTaskPage.refreshScheduledTask();

            });

        });

    },

    confirmDeleteTrigger: function (index) {

        Dashboard.confirm(Globalize.translate('MessageDeleteTaskTrigger'), Globalize.translate('HeaderDeleteTaskTrigger'), function (result) {

            if (result) {
                ScheduledTaskPage.deleteTrigger(index);
            }

        });

    },

    deleteTrigger: function (index) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');


        ApiClient.getScheduledTask(id).done(function (task) {

            task.Triggers.remove(index);

            ApiClient.updateScheduledTaskTriggers(task.Id, task.Triggers).done(function () {

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

        return trigger;
    },

    getTimeOfDayTicks: function (val) {

        var vals = val.split(':');

        var hours = vals[0];
        var minutes = vals[1];

        // Add hours
        var ticks = hours * 60 * 60 * 1000 * 10000;

        ticks += minutes * 60 * 1000 * 10000;

        return ticks;
    }
};

$(document).on('pageshow', "#scheduledTaskPage", ScheduledTaskPage.onPageShow);