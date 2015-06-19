(function () {

    function onClosed() {
        $(this).remove();
    }
    function show(options) {

        require(['paperbuttonstyle'], function() {
            // items
            // positionTo
            // showCancel
            // title
            var id = 'dlg' + new Date().getTime();
            var html = '';

            html += '<paper-dialog id="' + id + '" entry-animation="scale-up-animation" exit-animation="fade-out-animation">';

            if (options.title) {
                html += '<h2>';
                html += options.title;
                html += '</h2>';
            }

            html += '<paper-dialog-scrollable>';
            for (var i = 0, length = options.items.length; i < length; i++) {

                var option = options.items[i];

                html += '<paper-button class="block blue ripple btnOption" data-id="' + option.id + '" style="margin:0;">' + option.name + '</paper-button>';
            }

            html += '</paper-dialog-scrollable>';

            if (options.showCancel) {
                html += '<div class="buttons">';
                html += '<paper-button dialog-dismiss>' + Globalize.translate('ButtonCancel') + '</paper-button>';
                html += '</div>';
            }

            html += '</paper-dialog>';

            $(html).appendTo(document.body);

            setTimeout(function () {
                var dlg = document.getElementById(id);
                dlg.open();

                // Has to be assigned a z-index after the call to .open() 
                $(dlg).css('z-index', '999999').on('iron-overlay-closed', onClosed);

                $('.btnOption', dlg).on('click', function () {

                    if (options.callback) {
                        options.callback(this.getAttribute('data-id'));
                    }
                    dlg.close();
                });
            }, 100);
        });
    }

    window.ActionSheetElement = {
        show: show
    };
})();