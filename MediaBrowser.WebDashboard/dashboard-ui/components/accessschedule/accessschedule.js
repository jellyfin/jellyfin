define(["dialogHelper", "datetime", "emby-select", "paper-icon-button-light", "formDialogStyle"], function(dialogHelper, datetime) {
    "use strict";

    function getDisplayTime(hours) {
        var minutes = 0,
            pct = hours % 1;
        return pct && (minutes = parseInt(60 * pct)), datetime.getDisplayTime(new Date(2e3, 1, 1, hours, minutes, 0, 0))
    }

    function populateHours(context) {
        for (var html = "", i = 0; i < 24; i++) html += '<option value="' + i + '">' + getDisplayTime(i) + "</option>";
        html += '<option value="24">' + getDisplayTime(0) + "</option>", context.querySelector("#selectStart").innerHTML = html, context.querySelector("#selectEnd").innerHTML = html
    }

    function loadSchedule(context, schedule) {
        context.querySelector("#selectDay").value = schedule.DayOfWeek || "Sunday", context.querySelector("#selectStart").value = schedule.StartHour || 0, context.querySelector("#selectEnd").value = schedule.EndHour || 0
    }

    function submitSchedule(context, options) {
        var updatedSchedule = {
            DayOfWeek: context.querySelector("#selectDay").value,
            StartHour: context.querySelector("#selectStart").value,
            EndHour: context.querySelector("#selectEnd").value
        };
        if (parseFloat(updatedSchedule.StartHour) >= parseFloat(updatedSchedule.EndHour)) return void alert(Globalize.translate("ErrorMessageStartHourGreaterThanEnd"));
        context.submitted = !0, options.schedule = Object.assign(options.schedule, updatedSchedule), dialogHelper.close(context)
    }
    return {
        show: function(options) {
            return new Promise(function(resolve, reject) {
                var xhr = new XMLHttpRequest;
                xhr.open("GET", "components/accessschedule/accessschedule.template.html", !0), xhr.onload = function(e) {
                    var template = this.response,
                        dlg = dialogHelper.createDialog({
                            removeOnClose: !0,
                            size: "small"
                        });
                    dlg.classList.add("formDialog");
                    var html = "";
                    html += Globalize.translateDocument(template), dlg.innerHTML = html, populateHours(dlg), loadSchedule(dlg, options.schedule), dialogHelper.open(dlg), dlg.addEventListener("close", function() {
                        dlg.submitted ? resolve(options.schedule) : reject()
                    }), dlg.querySelector(".btnCancel").addEventListener("click", function(e) {
                        dialogHelper.close(dlg)
                    }), dlg.querySelector("form").addEventListener("submit", function(e) {
                        return submitSchedule(dlg, options), e.preventDefault(), !1
                    })
                }, xhr.send()
            })
        }
    }
});