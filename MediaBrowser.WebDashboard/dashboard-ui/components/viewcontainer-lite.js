define(['jQuery'], function ($) {

    var pageContainerCount;
    var animationDuration = 500;

    function loadView(options) {

        return new Promise(function (resolve, reject) {

            var animatedPages = document.querySelector('.mainAnimatedPages');

            if (options.cancel) {
                return;
            }

            var selected = getSelectedIndex(animatedPages);
            var pageIndex = selected + 1;

            if (pageIndex >= pageContainerCount) {
                pageIndex = 0;
            }

            var newViewInfo = normalizeNewView(options);
            var newView = newViewInfo.elem;

            var dependencies = typeof (newView) == 'string' ? null : newView.getAttribute('data-require');
            dependencies = dependencies ? dependencies.split(',') : [];

            var isPluginpage = options.url.toLowerCase().indexOf('/configurationpage?') != -1;

            if (isPluginpage) {
                dependencies.push('jqmpopup');
                dependencies.push('jqmcollapsible');
                dependencies.push('jqmcheckbox');
                dependencies.push('legacy/dashboard');
                dependencies.push('legacy/selectmenu');
                dependencies.push('jqmcontrolgroup');
            }

            if (isPluginpage || (newView.classList && newView.classList.contains('type-interior'))) {
                dependencies.push('jqmlistview');
                dependencies.push('scripts/notifications');
            }

            require(dependencies, function () {
                var allPages = animatedPages.querySelectorAll('.mainAnimatedPage');
                var animatable = allPages[pageIndex];

                var currentPage = animatable.querySelector('.page-view');

                if (currentPage) {
                    triggerDestroy(currentPage);
                }

                for (var i = 0, length = allPages.length; i < length; i++) {
                    if (pageIndex == i) {
                        allPages[i].classList.remove('hide');
                    } else {
                        allPages[i].classList.add('hide');
                    }
                }

                if (typeof (newView) == 'string') {
                    animatable.innerHTML = newView;
                } else {
                    animatable.innerHTML = '';
                    if (newViewInfo.hasScript) {
                        // TODO: figure this out without jQuery
                        $(newView).appendTo(animatable);
                    } else {
                        animatable.appendChild(newView);
                    }
                    enhanceNewView(dependencies, newView);
                }

                var view = animatable.querySelector('.page-view');

                if (onBeforeChange) {
                    onBeforeChange(view, false, options);
                }

                $.mobile = $.mobile || {};
                $.mobile.activePage = view;

                resolve(view);
            });
        });
    }

    function enhanceNewView(dependencies, newView) {

        var hasJqm = false;

        for (var i = 0, length = dependencies.length; i < length; i++) {
            if (dependencies[i].indexOf('jqm') == 0) {
                hasJqm = true;
                break;
            }
        }

        if (hasJqm) {
            $(newView).trigger('create');
        }
    }

    function replaceAll(str, find, replace) {

        return str.split(find).join(replace);
    }

    function parseHtml(html, hasScript) {

        if (hasScript) {
            html = replaceAll(html, '<!--<script', '<script');
            html = replaceAll(html, '</script>-->', '</script>');
        }

        var wrapper = document.createElement('div');
        wrapper.innerHTML = html;
        return wrapper.querySelector('div[data-role="page"]');
    }

    function normalizeNewView(options) {

        if (options.view.indexOf('data-role="page"') == -1) {
            var html = '<div class="page-view" data-type="' + (options.type || '') + '" data-url="' + options.url + '">';
            html += options.view;
            html += '</div>';
            return html;
        }

        var hasScript = options.view.indexOf('<script') != -1;

        var elem = parseHtml(options.view, hasScript);
        elem.classList.add('page-view');
        elem.setAttribute('data-type', options.type || '');
        elem.setAttribute('data-url', options.url);
        return {
            elem: elem,
            hasScript: hasScript
        };
    }

    var onBeforeChange;
    function setOnBeforeChange(fn) {
        onBeforeChange = fn;
    }

    function getSelectedIndex(animatedPages) {
        var allPages = animatedPages.querySelectorAll('.mainAnimatedPage');
        for (var i = 0, length = allPages.length; i < length; i++) {
            if (!allPages[i].classList.contains('hide')) {
                return i;
            }
        }

        return -1;
    }

    function replaceAnimatedPages() {
        var elem = document.querySelector('neon-animated-pages.mainAnimatedPages');

        if (elem) {
            var div = document.createElement('div');
            div.classList.add('mainAnimatedPages');
            div.classList.add('skinBody');
            div.innerHTML = '<div class="mainAnimatedPage hide"></div><div class="mainAnimatedPage hide"></div><div class="mainAnimatedPage hide"></div>';
            elem.parentNode.replaceChild(div, elem);
        }

        pageContainerCount = document.querySelectorAll('.mainAnimatedPage').length;
    }

    function tryRestoreView(options) {
        return new Promise(function (resolve, reject) {

            var url = options.url;
            var view = document.querySelector(".page-view[data-url='" + url + "']");
            var page = parentWithClass(view, 'mainAnimatedPage');

            if (view) {

                var index = -1;
                var pages = document.querySelectorAll('.mainAnimatedPage');
                for (var i = 0, length = pages.length; i < length; i++) {
                    if (pages[i] == page) {
                        index = i;
                        break;
                    }
                }
                if (index != -1) {

                    var animatedPages = document.querySelector('.mainAnimatedPages');
                    if (options.cancel) {
                        return;
                    }

                    var allPages = animatedPages.querySelectorAll('.mainAnimatedPage');
                    var animatable = allPages[index];
                    var view = animatable.querySelector('.page-view');

                    if (onBeforeChange) {
                        onBeforeChange(view, true, options);
                    }

                    for (var i = 0, length = allPages.length; i < length; i++) {
                        if (index == i) {
                            allPages[i].classList.remove('hide');
                        } else {
                            allPages[i].classList.add('hide');
                        }
                    }

                    $.mobile = $.mobile || {};
                    $.mobile.activePage = view;

                    resolve(view);
                    return;
                }
            }

            reject();
        });
    }

    function triggerDestroy(view) {
        view.dispatchEvent(new CustomEvent("viewdestroy", {}));
    }

    function reset() {

        var views = document.querySelectorAll(".mainAnimatedPage.hide .page-view");

        for (var i = 0, length = views.length; i < length; i++) {

            var view = views[i];
            triggerDestroy(view);
            view.parentNode.removeChild(view);
        }
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    replaceAnimatedPages();

    return {
        loadView: loadView,
        tryRestoreView: tryRestoreView,
        reset: reset,
        setOnBeforeChange: setOnBeforeChange
    };
});