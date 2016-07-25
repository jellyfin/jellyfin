define(['browser', 'css!./viewcontainer-lite'], function (browser) {

    var mainAnimatedPages = document.querySelector('.mainAnimatedPages');
    var allPages = [];
    var currentUrls = [];
    var pageContainerCount = 3;
    var selectedPageIndex = -1;

    function enableAnimation() {

        if (browser.tv) {
            return false;
        }

        if (browser.operaTv) {
            return false;
        }

        return true;
    }

    function loadView(options) {

        if (options.cancel) {
            return;
        }

        cancelActiveAnimations();

        var selected = selectedPageIndex;
        var previousAnimatable = selected == -1 ? null : allPages[selected];
        var pageIndex = selected + 1;

        if (pageIndex >= pageContainerCount) {
            pageIndex = 0;
        }

        var view = document.createElement('div');

        if (options.type) {
            view.setAttribute('data-type', options.type);
        }
        view.innerHTML = options.view;

        var currentPage = allPages[pageIndex];
        var animatable = view;

        view.classList.add('mainAnimatedPage');

        if (currentPage) {
            triggerDestroy(currentPage);
            mainAnimatedPages.replaceChild(view, currentPage);
        } else {
            mainAnimatedPages.appendChild(view);
        }

        allPages[pageIndex] = view;

        if (onBeforeChange) {
            onBeforeChange(view, false, options);
        }

        beforeAnimate(allPages, pageIndex, selected);

        // animate here
        return animate(animatable, previousAnimatable, options.transition, options.isBack).then(function () {

            selectedPageIndex = pageIndex;
            currentUrls[pageIndex] = options.url;
            if (!options.cancel && previousAnimatable) {
                afterAnimate(allPages, pageIndex);
            }

            return view;
        });
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

        return Promise.resolve();
    }

    function slide(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        return new Promise(function (resolve, reject) {
            var timings = {
                duration: 450,
                iterations: 1,
                easing: 'ease-out',
                fill: 'both'
            }

            var animations = [];

            if (oldAnimatedPage) {
                var destination = isBack ? '100%' : '-100%';

                animations.push(oldAnimatedPage.animate([

                  { transform: 'none', offset: 0 },
                  { transform: 'translate3d(' + destination + ', 0, 0)', offset: 1 }

                ], timings));
            }

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
            var timings = {
                duration: 300,
                iterations: 1,
                easing: 'ease-out',
                fill: 'both'
            }

            var animations = [];

            if (oldAnimatedPage) {
                animations.push(oldAnimatedPage.animate([

                  { opacity: 1, offset: 0 },
                  { opacity: 0, offset: 1 }

                ], timings));
            }

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

    function tryRestoreView(options) {

        var url = options.url;
        var index = currentUrls.indexOf(url);

        if (index != -1) {

            var animatable = allPages[index];
            var view = animatable;

            if (view) {

                if (options.cancel) {
                    return;
                }

                cancelActiveAnimations();

                var selected = selectedPageIndex;
                var previousAnimatable = selected == -1 ? null : allPages[selected];

                if (onBeforeChange) {
                    onBeforeChange(view, true, options);
                }

                beforeAnimate(allPages, index, selected);

                animatable.classList.remove('hide');

                return animate(animatable, previousAnimatable, options.transition, options.isBack).then(function () {

                    selectedPageIndex = index;
                    if (!options.cancel && previousAnimatable) {
                        afterAnimate(allPages, index);
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

        allPages = [];
        currentUrls = [];
        mainAnimatedPages.innerHTML = '';
        selectedPageIndex = -1;
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