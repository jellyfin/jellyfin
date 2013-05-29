(function (window, document, $) {

    function remoteControl() {

        var self = this;

        self.showMenu = function (page, item) {

            $('#confirmFlyout').popup("close").remove();

            var html = '<div data-role="popup" id="remoteControlFlyout">';

            html += '<div class="ui-corner-top ui-bar-a" style="text-align:center;">';
            html += '<h3>Remote Control</h3>';
            html += '</div>';

            html += '<div data-role="content" class="ui-corner-bottom ui-content">';

            html += '<div style="padding: 1em;margin: 0;">test';
            html += '</div>';

            html += '<p><button type="button" data-icon="ok" onclick="$(\'#confirmFlyout\')[0].confirm=true;$(\'#confirmFlyout\').popup(\'close\');" data-theme="b" data-mini="true" data-inline="true">Ok</button>';
            html += '<button type="button" data-icon="delete" onclick="$(\'#confirmFlyout\').popup(\'close\');" data-theme="a" data-mini="true" data-inline="true">Cancel</button></p>';

            html += '</div>';

            html += '</div>';

            $(document.body).append(html);

            $('#remoteControlFlyout').popup({ history: false }).trigger('create').popup("open").on("popupafterclose", function () {

                $(this).off("popupafterclose").remove();
            });
        };
    }

    window.RemoteControl = new remoteControl();

})(window, document, jQuery);