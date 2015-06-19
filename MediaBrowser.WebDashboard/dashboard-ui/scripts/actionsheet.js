(function () {

    function onClosed() {
        $(this).remove();
    }
    function show(options) {

        require(['paperbuttonstyle'], function () {
            // items
            // positionTo
            // showCancel
            // title
            var id = 'dlg' + new Date().getTime();
            var html = '';

            var style = "";

            if (options.positionTo) {

                var pos = $(options.positionTo).offset();

                pos.top += $(options.positionTo).innerHeight() / 2;
                pos.left += $(options.positionTo).innerWidth() / 2;

                // Account for margins
                pos.top -= 24;
                pos.left -= 24;

                // Account for popup size - we can't predict this yet so just estimate
                pos.top -= 100;
                pos.left -= 80;

                // Account for scroll position
                pos.top -= $(window).scrollTop();
                pos.left -= $(window).scrollLeft();

                // Avoid showing too close to the bottom
                pos.top = Math.min(pos.top, $(window).height() - 300);
                pos.left = Math.min(pos.left, $(window).width() - 300);

                // Do some boundary checking
                pos.top = Math.max(pos.top, 0);
                pos.left = Math.max(pos.left, 0);

                style += 'position:fixed;top:' + pos.top + 'px;left:' + pos.left + 'px';
            }

            html += '<paper-dialog id="' + id + '" entry-animation="fade-in-animation" exit-animation="fade-out-animation" with-backdrop style="' + style + '">';

            if (options.title) {
                html += '<h2>';
                html += options.title;
                html += '</h2>';
            }

            html += '<paper-dialog-scrollable>';
            for (var i = 0, length = options.items.length; i < length; i++) {

                var option = options.items[i];

                html += '<paper-button class="block menuButton ripple btnOption" data-id="' + option.id + '" style="margin:0;">';

                if (option.ironIcon) {
                    html += '<iron-icon icon="' + option.ironIcon + '"></iron-icon>';
                }
                html += '<span>' + option.name + '</span>';
                html += '</paper-button>';
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

                    var selectedId = this.getAttribute('data-id');

                    // Add a delay here to allow the click animation to finish, for nice effect
                    setTimeout(function () {

                        dlg.close();

                        if (options.callback) {
                            options.callback(selectedId);
                        }

                    }, 100);
                });
            }, 100);
        });
    }

    window.ActionSheetElement = {
        show: show
    };
})();