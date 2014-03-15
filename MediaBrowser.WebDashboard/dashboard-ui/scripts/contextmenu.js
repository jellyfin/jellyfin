(function () {

    function onDocumentMouseDown(e) {

        var $e = $(e.target);
        
        var isContextMenuOption = $e.is('.contextMenuOption');

        if (!isContextMenuOption || $e.is('.contextMenuCommandOption')) {
            if ($e.is('.itemContextMenu') || $e.parents('.itemContextMenu').length) {
                return;
            }
        }

        if (isContextMenuOption) {

            setTimeout(closeContextMenus, 150);

        } else {
            closeContextMenus();
        }
    }
    
    function closeContextMenus() {
        $('.itemContextMenu').hide().remove();
        $('.hasContextMenu').removeClass('hasContextMenu');
    }

    function getMenuOptionHtml(item) {

        var html = '';

        if (item.type == 'divider') {

            html += '<p class="contextMenuDivider"></p>';
        }

        if (item.type == 'header') {

            html += '<p class="contextMenuHeader">' + item.text + '</p>';
        }

        if (item.type == 'link') {

            html += '<a class="contextMenuOption" href="' + item.url + '">' + item.text + '</a>';
        }

        if (item.type == 'command') {

            html += '<a class="contextMenuOption contextMenuCommandOption" data-command="' + item.name + '" href="#">' + item.text + '</a>';
        }

        return html;
    }

    function getMenu(items) {

        var html = '';

        html += '<div class="itemContextMenu">';
        html += '<div class="contextMenuInner">' + items.map(getMenuOptionHtml).join('') + '</div>';
        html += '</div>';

        return $(html).appendTo(document.body);
    }

    $.fn.createContextMenu = function (options) {

        return this.on('contextmenu', options.selector, function (e) {

            var elem = this;
            var items = options.getOptions(elem);

            if (!items.length) {
                return;
            }

            var menu = getMenu(items);

            var autoH = menu.height() + 12;

            if ((e.pageY + autoH) > $('html').height()) {

                menu.addClass('dropdown-context-up').css({
                    top: e.pageY - 20 - autoH,
                    left: e.pageX - 13

                }).fadeIn();

            } else {

                menu.css({
                    top: e.pageY + 10,
                    left: e.pageX - 13

                }).fadeIn();
            }

            $(this).addClass('hasContextMenu');
            $(document).off('mousedown.closecontextmenu').on('mousedown.closecontextmenu', onDocumentMouseDown);

            menu.on('click', '.contextMenuCommandOption', function() {

                closeContextMenus();

                options.command(this.getAttribute('data-command'), elem);

                return false;
            });

            return false;
        });
    };

})();

