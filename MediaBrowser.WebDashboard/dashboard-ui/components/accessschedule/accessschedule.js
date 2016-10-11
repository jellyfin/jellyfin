define(['dialogHelper', 'datetime', 'emby-select', 'paper-icon-button-light', 'formDialogStyle'], function (dialogHelper, datetime) {

    function getDisplayTime(hours) {

        var minutes = 0;

        var pct = hours % 1;

        if (pct) {
            minutes = parseInt(pct * 60);
        }

        return datetime.getDisplayTime(new Date(2000, 1, 1, hours, minutes, 0, 0));
    }

    function populateHours(context) {

        var html = '';

        for (var i = 0; i < 24; i++) {

            html += '<option value="' + i + '">' + getDisplayTime(i) + '</option>';
        }

        html += '<option value="24">' + getDisplayTime(0) + '</option>';

        context.querySelector('#selectStart').innerHTML = html;
        context.querySelector('#selectEnd').innerHTML = html;
    }

    function loadSchedule(context, schedule) {

        context.querySelector('#selectDay').value = schedule.DayOfWeek || 'Sunday';
        context.querySelector('#selectStart').value = schedule.StartHour || 0;
        context.querySelector('#selectEnd').value = schedule.EndHour || 0;
    }

    function submitSchedule(context, options) {

        var updatedSchedule = {
            DayOfWeek: context.querySelector('#selectDay').value,
            StartHour: context.querySelector('#selectStart').value,
            EndHour: context.querySelector('#selectEnd').value
        };

        if (parseFloat(updatedSchedule.StartHour) >= parseFloat(updatedSchedule.EndHour)) {

            alert(Globalize.translate('ErrorMessageStartHourGreaterThanEnd'));

            return;
        }

        context.submitted = true;
        options.schedule = Object.assign(options.schedule, updatedSchedule);
        dialogHelper.close(context);
    }

    return {
        show: function (options) {
            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/accessschedule/accessschedule.template.html', true);

                xhr.onload = function (e) {

                    var template = this.response;
                    var dlg = dialogHelper.createDialog({
                        removeOnClose: true,
                        size: 'small'
                    });

                    dlg.classList.add('formDialog');

                    var html = '';

                    html += Globalize.translateDocument(template);

                    dlg.innerHTML = html;

                    populateHours(dlg);
                    loadSchedule(dlg, options.schedule);

                    dialogHelper.open(dlg);

                    dlg.addEventListener('close', function () {

                        if (dlg.submitted) {
                            resolve(options.schedule);
                        } else {
                            reject();
                        }
                    });

                    dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                        dialogHelper.close(dlg);
                    });

                    dlg.querySelector('form').addEventListener('submit', function (e) {

                        submitSchedule(dlg, options);

                        e.preventDefault();
                        return false;
                    });
                }

                xhr.send();
            });
        }
    };
});