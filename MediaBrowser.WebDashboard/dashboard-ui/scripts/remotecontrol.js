(function (window, document, $) {

    function showMenu(page, item, context, sessionsPromise, usersPromise) {

        var html = '<div data-role="popup" id="remoteControlFlyout">';

        html += '<div class="ui-corner-top ui-bar-a" style="text-align:center;">';
        html += '<h3>Remote Control</h3>';
        html += '</div>';

        html += '<div data-role="content" class="ui-corner-bottom ui-content">';

        html += '<form id="sendToForm"><div class="sessionsPopupContent">';

        html += '<div class="circle"></div><div class="circle1"></div>';

        html += '</div>';

        html += '<p style="text-align:center;margin:0;"><button type="submit" data-icon="ok" data-theme="b" data-mini="true" data-inline="true">Ok</button>';
        html += '<button type="button" data-icon="delete" onclick="$(\'#remoteControlFlyout\').popup(\'close\');" data-theme="a" data-mini="true" data-inline="true">Cancel</button></p>';

        html += '</form></div>';

        html += '</div>';

        $(document.body).append(html);

        var popup = $('#remoteControlFlyout').popup({ history: false }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();
        });

        $('#sendToForm', popup).on('submit', function () {

            var checkboxes = $('.chkClient', popup);

            if (!checkboxes.length) {
                $('#remoteControlFlyout').popup("close");
                return false;
            }

            checkboxes = $('.chkClient:checked', popup);

            if (!checkboxes.length) {
                Dashboard.alert('Please select a device to control.');
                return false;
            }

            var sessionIds = [];

            checkboxes.parents('.trSession').each(function () {

                sessionIds.push(this.getAttribute('data-sessionid'));

            });

            var command = $('#selectCommand', popup).val();

            var promise;

            if (command == "Browse") {
                promise = ApiClient.sendBrowseCommand(sessionIds[0], {

                    ItemId: item.Id,
                    ItemName: item.Name,
                    ItemType: item.Type,
                    Context: context

                });
            }
            else if (command == "Play") {
                promise = ApiClient.sendPlayCommand(sessionIds[0], {

                    ItemIds: [item.Id].join(','),
                    PlayCommand: 'PlayNow'

                });
            }

            promise.done(function () {

                $('#remoteControlFlyout').popup("close");
            });

            return false;
        });

        var elem = $('.sessionsPopupContent');

        $.when(sessionsPromise, usersPromise).done(function (response1, response2) {

            var deviceId = ApiClient.deviceId();

            var sessions = response1[0].filter(function (s) {
                return s.DeviceId != deviceId;
            });

            renderSessions(sessions, response2[0], elem);

        });
    }

    function renderSessions(sessions, users, elem) {

        if (!sessions.length) {
            elem.html('<p>There are currently no available media browser sessions to control.</p>');
            $('#remoteControlFlyout').popup("reposition", {});
            return;
        }

        var html = '';

        html += '<p style="margin-top:0;">';
        html += '<label for="selectCommand">Select Command</label>';
        html += '<select id="selectCommand" data-mini="true">';
        html += '<option value="Browse">Browse To</label>';
        html += '<option value="Play">Play</label>';
        html += '</select>';
        html += '</p>';

        html += '<p style="margin: 1.5em 0;">Send To Device</p>';

        html += '<div>';

        html += '<table data-role="table" data-mode="reflow" class="ui-responsive table-stroke">';

        html += '<thead><tr>';
        html += '<th></th>';
        html += '<th>Client</th>';
        html += '<th>Device</th>';
        html += '<th>User</th>';
        html += '</tr></thead>';

        html += '<tbody>';

        for (var i = 0, length = sessions.length; i < length; i++) {

            var session = sessions[i];

            html += '<tr class="trSession" data-sessionid="' + session.Id + '">';

            html += '<td class="checkboxCell"></td>';
            html += '<td>' + session.Client + '</td>';
            html += '<td>' + session.DeviceName + '</td>';

            html += '<td>';

            var user = null;

            if (session.UserId) {

                user = users.filter(function (u) {
                    return u.Id == session.UserId;
                })[0];

            }

            html += user ? user.Name : '&nbsp;';

            html += '</td>';

            html += '</tr>';
        }

        html += '</tbody>';

        html += '</table>';

        html += '<br/>';
        html += '</div>';

        elem.html(html).trigger('create');

        $('.checkboxCell', elem).html('<input type="radio" class="chkClient" name="chkClient" />');

        $('#remoteControlFlyout').popup("reposition", {});
    }

    function remoteControl() {

        var self = this;

        self.showMenu = function (page, item, context) {
            showMenu(page, item, context, ApiClient.getSessions({ SupportsRemoteControl: true }), ApiClient.getUsers());
        };
    }

    window.RemoteControl = new remoteControl();

})(window, document, jQuery);