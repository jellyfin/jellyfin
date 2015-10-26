(function () {

    function show(options) {

        // items
        // positionTo
        // showCancel
        // title
        var id = 'dlg' + new Date().getTime();
        var html = '';

        var style = "";

        var windowHeight = $(window).height();

        // If the window height is under a certain amount, don't bother trying to position
        // based on an element.
        if (options.positionTo && windowHeight >= 540) {

            var pos = $(options.positionTo).offset();

            pos.top += $(options.positionTo).innerHeight() / 2;
            pos.left += $(options.positionTo).innerWidth() / 2;

            // Account for margins
            pos.top -= 24;
            pos.left -= 24;

            // Account for popup size - we can't predict this yet so just estimate
            pos.top -= (55 * options.items.length) / 2;
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

        html += '<paper-dialog id="' + id + '" with-backdrop style="' + style + '">';

        if (options.title) {
            html += '<h2>';
            html += options.title;
            html += '</h2>';
        }

        // There seems to be a bug with this in safari causing it to immediately roll up to 0 height
        var isScrollable = !$.browser.safari;

        if (isScrollable) {
            html += '<paper-dialog-scrollable>';
        }

        // If any items have an icon, give them all an icon just to make sure they're all lined up evenly
        var renderIcon = options.items.filter(function (o) {
            return o.ironIcon;
        }).length;

        html += '<paper-menu>';
        for (var i = 0, length = options.items.length; i < length; i++) {

            var option = options.items[i];

            html += '<paper-menu-item class="actionSheetMenuItem" data-id="' + option.id + '" style="display:block;">';

            if (option.ironIcon) {
                html += '<iron-icon icon="' + option.ironIcon + '"></iron-icon>';
            }
            else if (renderIcon) {
                html += '<iron-icon></iron-icon>';
            }
            html += '<span>' + option.name + '</span>';
            html += '</paper-menu-item>';
        }
        html += '</paper-menu>';

        if (isScrollable) {
            html += '</paper-dialog-scrollable>';
        }

        if (options.showCancel) {
            html += '<div class="buttons">';
            html += '<paper-button dialog-dismiss>' + Globalize.translate('ButtonCancel') + '</paper-button>';
            html += '</div>';
        }

        html += '</paper-dialog>';

        $(document.body).append(html);

        setTimeout(function () {
            var dlg = document.getElementById(id);

            dlg.open();

            // Has to be assigned a z-index after the call to .open() 
            $(dlg).on('iron-overlay-closed', function () {
                $(this).remove();
            });

            // Seeing an issue in some non-chrome browsers where this is requiring a double click
            var eventName = $.browser.chrome || $.browser.safari ? 'click' : 'mousedown';

            $('.actionSheetMenuItem', dlg).on(eventName, function () {

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
    }

    window.ActionSheetElement = {
        show: show
    };
})();