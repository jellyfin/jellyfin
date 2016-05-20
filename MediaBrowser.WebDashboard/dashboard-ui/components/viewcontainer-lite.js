define(['browser'], function (browser) {

    var allPages = document.querySelectorAll('.mainAnimatedPage');
    var currentUrls = [];
    var pageContainerCount = allPages.length;
    var selectedPageIndex = -1;

    function enableAnimation() {

        if (browser.tv) {
            return false;
        }
        if (browser.safari) {
            // Right now they don't look good. Haven't figured out why yet.
            return false;
        }

        return true;
    }

    function loadView(options) {

        if (options.cancel) {
            return;
        }

        cancelActiveAnimations();

        var selected = getSelectedIndex(allPages);
        var previousAnimatable = selected == -1 ? null : allPages[selected];
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

        return new Promise(function (resolve, reject) {

            require(dependencies, function () {

                var animatable = allPages[pageIndex];

                var currentPage = animatable.querySelector('.page-view');

                if (currentPage) {
                    triggerDestroy(currentPage);
                }

                var view;

                if (typeof (newView) == 'string') {
                    animatable.innerHTML = newView;
                    view = animatable.querySelector('.page-view');
                } else {
                    if (newViewInfo.hasScript) {
                        // TODO: figure this out without jQuery
                        animatable.innerHTML = '';
                        $(newView).appendTo(animatable);
                    } else {
                        if (currentPage) {
                            animatable.replaceChild(newView, currentPage);
                        } else {
                            animatable.appendChild(newView);
                        }
                    }
                    enhanceNewView(dependencies, newView);
                    view = newView;
                }

                if (onBeforeChange) {
                    onBeforeChange(view, false, options);
                }

                beforeAnimate(allPages, pageIndex, selected);
                // animate here
                animate(animatable, previousAnimatable, options.transition, options.isBack).then(function () {

                    selectedPageIndex = pageIndex;
                    currentUrls[pageIndex] = options.url;
                    if (!options.cancel && previousAnimatable) {
                        afterAnimate(allPages, pageIndex);
                    }

                    // Temporary hack
                    // If a view renders UI in viewbeforeshow the lazy image loader will think the images aren't visible and won't load images
                    // The views need to be updated to start loading data in beforeshow, but not render until show
                    document.dispatchEvent(new CustomEvent('scroll', {}));

                    if (window.$) {
                        $.mobile = $.mobile || {};
                        $.mobile.activePage = view;
                    }

                    resolve(view);
                });
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
            var html = '<div class="page-view" data-type="' + (options.type || '') + '">';
            html += options.view;
            html += '</div>';
            return html;
        }

        var hasScript = options.view.indexOf('<script') != -1;

        var elem = parseHtml(options.view, hasScript);
        elem.classList.add('page-view');
        elem.setAttribute('data-type', options.type || '');

        return {
            elem: elem,
            hasScript: hasScript
        };
    }

    function beforeAnimate(allPages, newPageIndex, oldPageIndex) {
        for (var i = 0, length = allPages.length; i < length; i++) {
            if (newPageIndex == i || oldPageIndex == i) {
                //allPages[i].classList.remove('hide');
            } else {
                allPages[i].classList.add('hide');
            }
        }
    }

    function afterAnimate(allPages, newPageIndex) {
        for (var i = 0, length = allPages.length; i < length; i++) {
            if (newPageIndex == i) {
                //allPages[i].classList.remove('hide');
            } else {
                allPages[i].classList.add('hide');
            }
        }
    }

    function animate(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        if (enableAnimation() && oldAnimatedPage && newAnimatedPage.animate) {
            if (transition == 'slide') {
                return slide(newAnimatedPage, oldAnimatedPage, transition, isBack);
            } else if (transition == 'fade') {
                return fade(newAnimatedPage, oldAnimatedPage, transition, isBack);
            }
        }

        return nullAnimation(newAnimatedPage, oldAnimatedPage, transition, isBack);
    }

    function nullAnimation(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        newAnimatedPage.classList.remove('hide');
        return Promise.resolve();
    }

    function slide(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        return new Promise(function (resolve, reject) {

            // Do not use fill: both or the ability to swipe horizontally may be affected on Chrome 50
            var timings = {
                duration: 450,
                iterations: 1,
                easing: 'ease-out'
            }

            var animations = [];

            if (oldAnimatedPage) {
                var destination = isBack ? '100%' : '-100%';

                animations.push(oldAnimatedPage.animate([

                  { transform: 'none', offset: 0 },
                  { transform: 'translate3d(' + destination + ', 0, 0)', offset: 1 }

                ], timings));
            }

            newAnimatedPage.classList.remove('hide');

            var start = isBack ? '-100%' : '100%';

            animations.push(newAnimatedPage.animate([

              { transform: 'translate3d(' + start + ', 0, 0)', offset: 0 },
              { transform: 'none', offset: 1 }

            ], timings));

            currentAnimations = animations;

            animations[animations.length - 1].onfinish = resolve;
        });
    }

    function fade(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        return new Promise(function (resolve, reject) {

            // Do not use fill: both or the ability to swipe horizontally may be affected on Chrome 50
            var timings = {
                duration: 140,
                iterations: 1,
                easing: 'ease-out'
            }

            var animations = [];

            if (oldAnimatedPage) {
                animations.push(oldAnimatedPage.animate([

                  { opacity: 1, offset: 0 },
                  { opacity: 0, offset: 1 }

                ], timings));
            }

            newAnimatedPage.classList.remove('hide');

            animations.push(newAnimatedPage.animate([

                  { opacity: 0, offset: 0 },
                  { opacity: 1, offset: 1 }

            ], timings));

            currentAnimations = animations;

            animations[animations.length - 1].onfinish = resolve;
        });
    }

    var currentAnimations = [];
    function cancelActiveAnimations() {

        var animations = currentAnimations;
        for (var i = 0, length = animations.length; i < length; i++) {
            cancelAnimation(animations[i]);
        }
    }

    function cancelAnimation(animation) {

        try {
            animation.cancel();
        } catch (err) {
            console.log('Error canceling animation: ' + err);
        }
    }

    var onBeforeChange;
    function setOnBeforeChange(fn) {
        onBeforeChange = fn;
    }

    function getSelectedIndex(allPages) {

        return selectedPageIndex;
    }

    function tryRestoreView(options) {

        var url = options.url;
        var index = currentUrls.indexOf(url);

        if (index != -1) {
            var page = allPages[index];
            var view = page.querySelector(".page-view");

            if (view) {

                if (options.cancel) {
                    return;
                }

                cancelActiveAnimations();

                var animatable = allPages[index];
                var selected = getSelectedIndex(allPages);
                var previousAnimatable = selected == -1 ? null : allPages[selected];

                if (onBeforeChange) {
                    onBeforeChange(view, true, options);
                }

                beforeAnimate(allPages, index, selected);

                return animate(animatable, previousAnimatable, options.transition, options.isBack).then(function () {

                    selectedPageIndex = index;
                    if (!options.cancel && previousAnimatable) {
                        afterAnimate(allPages, index);
                    }

                    // Temporary hack
                    // If a view renders UI in viewbeforeshow the lazy image loader will think the images aren't visible and won't load images
                    // The views need to be updated to start loading data in beforeshow, but not render until show
                    document.dispatchEvent(new CustomEvent('scroll', {}));

                    if (window.$) {
                        $.mobile = $.mobile || {};
                        $.mobile.activePage = view;
                    }

                    return view;
                });
            }
        }

        return Promise.reject();
    }

    function triggerDestroy(view) {
        view.dispatchEvent(new CustomEvent("viewdestroy", {}));
    }

    function reset() {

        currentUrls = [];
    }

    if (enableAnimation() && !document.documentElement.animate) {
        require(['webAnimations']);
    }

    return {
        loadView: loadView,
        tryRestoreView: tryRestoreView,
        reset: reset,
        setOnBeforeChange: setOnBeforeChange
    };
});