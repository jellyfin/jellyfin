define(['dialogHelper', 'layoutManager', 'globalize', 'browser', 'dom', 'emby-button', 'css!./actionsheet', 'material-icons', 'scrollStyles'], function (dialogHelper, layoutManager, globalize, browser, dom) {

    function getOffsets(elems) {

        var doc = document;
        var results = [];

        if (!doc) {
            return results;
        }

        var box;
        var elem;

        for (var i = 0, length = elems.length; i < length; i++) {

            elem = elems[i];
            // Support: BlackBerry 5, iOS 3 (original iPhone)
            // If we don't have gBCR, just use 0,0 rather than error
            if (elem.getBoundingClientRect) {
                box = elem.getBoundingClientRect();
            } else {
                box = { top: 0, left: 0 };
            }

            results[i] = {
                top: box.top,
                left: box.left
            };
        }

        return results;
    }

    function getPosition(options, dlg) {

        var windowHeight = window.innerHeight;
        var windowWidth = window.innerWidth;

        if (windowHeight < 540) {
            return null;
        }

        var pos = getOffsets([options.positionTo])[0];

        pos.top += options.positionTo.offsetHeight / 2;
        pos.left += options.positionTo.offsetWidth / 2;

        var height = dlg.offsetHeight || 300;
        var width = dlg.offsetWidth || 160;

        // Account for popup size 
        pos.top -= height / 2;
        pos.left -= width / 2;

        // Avoid showing too close to the bottom
        var overflowX = pos.left + width - windowWidth;
        var overflowY = pos.top + height - windowHeight;

        if (overflowX > 0) {
            pos.left -= (overflowX + 20);
        }
        if (overflowY > 0) {
            pos.top -= (overflowY + 20);
        }

        // Do some boundary checking
        pos.top = Math.max(pos.top, 10);
        pos.left = Math.max(pos.left, 10);

        return pos;
    }

    function centerFocus(elem, horiz, on) {
        require(['scrollHelper'], function (scrollHelper) {
            var fn = on ? 'on' : 'off';
            scrollHelper.centerFocus[fn](elem, horiz);
        });
    }

    function show(options) {

        // items
        // positionTo
        // showCancel
        // title
        var dialogOptions = {
            removeOnClose: true,
            enableHistory: options.enableHistory,
            scrollY: false
        };

        var backButton = false;
        var isFullscreen;

        if (layoutManager.tv) {
            dialogOptions.size = 'fullscreen';
            isFullscreen = true;
            backButton = true;
            dialogOptions.autoFocus = true;
        } else {

            dialogOptions.modal = false;
            dialogOptions.entryAnimationDuration = 140;
            dialogOptions.exitAnimationDuration = 180;
            dialogOptions.autoFocus = false;
        }

        var dlg = dialogHelper.createDialog(dialogOptions);

        if (isFullscreen) {
            dlg.classList.add('actionsheet-fullscreen');
        }

        if (!layoutManager.tv) {
            dlg.classList.add('actionsheet-extraSpacing');
        }

        dlg.classList.add('actionSheet');

        var html = '';

        var scrollType = layoutManager.desktop ? 'smoothScrollY' : 'hiddenScrollY';
        var style = (browser.noFlex || browser.firefox) ? 'max-height:400px;' : '';

        // Admittedly a hack but right now the scrollbar is being factored into the width which is causing truncation
        if (options.items.length > 20) {
            var minWidth = window.innerWidth >= 300 ? 240 : 200;
            style += "min-width:" + minWidth + "px;";
        }

        var i, length, option;
        var renderIcon = false;
        for (i = 0, length = options.items.length; i < length; i++) {

            option = options.items[i];
            option.icon = option.selected ? 'check' : null;

            if (option.icon) {
                renderIcon = true;
            }
        }

        // If any items have an icon, give them all an icon just to make sure they're all lined up evenly
        var center = options.title && (!renderIcon /*|| itemsWithIcons.length != options.items.length*/);

        if (center) {
            html += '<div class="actionSheetContent actionSheetContent-centered">';
        } else {
            html += '<div class="actionSheetContent">';
        }

        if (options.title) {

            if (layoutManager.tv) {
                html += '<h1 class="actionSheetTitle">';
                html += options.title;
                html += '</h1>';
            } else {
                html += '<h2 class="actionSheetTitle">';
                html += options.title;
                html += '</h2>';
            }
        }
        if (options.text) {
            html += '<p class="actionSheetText">';
            html += options.text;
            html += '</p>';
        }

        html += '<div class="actionSheetScroller ' + scrollType + '" style="' + style + '">';

        var itemTagName = 'button';

        for (i = 0, length = options.items.length; i < length; i++) {

            option = options.items[i];

            var autoFocus = option.selected ? ' autoFocus' : '';
            html += '<' + itemTagName + autoFocus + ' is="emby-button" type="button" class="actionSheetMenuItem" data-id="' + (option.id || option.value) + '">';

            if (option.icon) {
                html += '<i class="actionSheetItemIcon md-icon">' + option.icon + '</i>';
            }
            else if (renderIcon && !center) {
                html += '<i class="actionSheetItemIcon md-icon" style="visibility:hidden;">check</i>';
            }
            html += '<div class="actionSheetItemText">' + (option.name || option.textContent || option.innerText) + '</div>';
            html += '</' + itemTagName + '>';
        }

        if (options.showCancel) {
            html += '<div class="buttons">';
            html += '<button is="emby-button" type="button" class="btnCancel">' + globalize.translate('sharedcomponents#ButtonCancel') + '</button>';
            html += '</div>';
        }
        html += '</div>';

        dlg.innerHTML = html;

        if (layoutManager.tv) {
            centerFocus(dlg.querySelector('.actionSheetScroller'), false, true);
        }

        if (options.showCancel) {
            dlg.querySelector('.btnCancel').addEventListener('click', function () {
                dialogHelper.close(dlg);
            });
        }

        document.body.appendChild(dlg);

        // Seeing an issue in some non-chrome browsers where this is requiring a double click
        //var eventName = browser.firefox ? 'mousedown' : 'click';
        var selectedId;

        dlg.addEventListener('click', function (e) {

            var actionSheetMenuItem = dom.parentWithClass(e.target, 'actionSheetMenuItem');

            if (actionSheetMenuItem) {
                selectedId = actionSheetMenuItem.getAttribute('data-id');
                dialogHelper.close(dlg);
            }

        });

        var timeout;
        if (options.timeout) {
            timeout = setTimeout(function () {
                dialogHelper.close(dlg);
            }, options.timeout);
        }

        return new Promise(function (resolve, reject) {

            dlg.addEventListener('close', function () {

                if (layoutManager.tv) {
                    centerFocus(dlg.querySelector('.actionSheetScroller'), false, false);
                }

                if (timeout) {
                    clearTimeout(timeout);
                    timeout = null;
                }

                if (selectedId != null) {
                    if (options.callback) {
                        options.callback(selectedId);
                    }

                    resolve(selectedId);
                } else {
                    reject();
                }
            });

            dialogHelper.open(dlg);

            var pos = options.positionTo ? getPosition(options, dlg) : null;

            if (pos) {
                dlg.style.position = 'fixed';
                dlg.style.margin = 0;
                dlg.style.left = pos.left + 'px';
                dlg.style.top = pos.top + 'px';
            }
        });
    }

    return {
        show: show
    };
});