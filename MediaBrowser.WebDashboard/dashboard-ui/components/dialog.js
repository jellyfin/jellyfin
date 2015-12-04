define(['fade-in-animation', 'fade-out-animation', 'paper-dialog'], function () {

    return function (options) {

        var title = options.title;
        var message = options.message;
        var buttons = options.buttons;
        var callback = options.callback;

        var id = 'paperdlg' + new Date().getTime();

        var html = '<paper-dialog id="' + id + '" role="alertdialog" entry-animation="fade-in-animation" exit-animation="fade-out-animation" with-backdrop>';
        html += '<h2>' + title + '</h2>';
        html += '<div>' + message + '</div>';
        html += '<div class="buttons">';

        var index = 0;
        html += buttons.map(function (b) {

            var dataIndex = ' data-index="' + index + '"';
            index++;
            return '<paper-button class="dialogButton"' + dataIndex + ' dialog-dismiss>' + b + '</paper-button>';

        }).join('');

        html += '</div>';
        html += '</paper-dialog>';

        $(document.body).append(html);

        // This timeout is obviously messy but it's unclear how to determine when the webcomponent is ready for use
        // element onload never fires
        setTimeout(function () {

            var dlg = document.getElementById(id);

            $('.dialogButton', dlg).on('click', function () {

                if (callback) {
                    callback(parseInt(this.getAttribute('data-index')));
                }

            });

            // Has to be assigned a z-index after the call to .open() 
            dlg.addEventListener('iron-overlay-closed', function (e) {

                dlg.parentNode.removeChild(dlg);
            });

            dlg.open();

        }, 300);
    };
});