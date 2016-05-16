define(['browser'], function (browser) {

    var allPages = document.querySelectorAll('.mainAnimatedPage');
    var currentUrls = [];
    var pageContainerCount = allPages.length;
    var animationDuration = 500;
    var allowAnimation = true;
    var selectedPageIndex = -1;

    function enableAnimation() {

        if (!allowAnimation) {
            return false;
        }
        if (browser.tv) {
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

        var view = document.createElement('div');
        view.classList.add('page-view');
        if (options.type) {
            view.setAttribute('data-type', options.type);
        }
        view.innerHTML = options.view;

        var animatable = allPages[pageIndex];

        var currentPage = animatable.querySelector('.page-view');

        if (currentPage) {
            triggerDestroy(currentPage);
            animatable.replaceChild(view, currentPage);
        } else {
            animatable.appendChild(view);
        }

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

        if (enableAnimation() && newAnimatedPage.animate) {
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

        newAnimatedPage.classList.remove('hide');

        var start = isBack ? '-100%' : '100%';

        animations.push(newAnimatedPage.animate([

          { transform: 'translate3d(' + start + ', 0, 0)', offset: 0 },
          { transform: 'none', offset: 1 }

        ], timings));

        currentAnimations = animations;

        return new Promise(function (resolve, reject) {
            animations[animations.length - 1].onfinish = resolve;
        });
    }

    function fade(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        var timings = {
            duration: animationDuration,
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

        newAnimatedPage.classList.remove('hide');

        animations.push(newAnimatedPage.animate([

              { opacity: 0, offset: 0 },
              { opacity: 1, offset: 1 }

        ], timings));

        currentAnimations = animations;

        return new Promise(function (resolve, reject) {
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

    function sendResolve(resolve, view) {

        // Don't report completion until the animation has finished, otherwise rendering may not perform well
        setTimeout(function () {

            resolve(view);

        }, animationDuration);
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

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function init(isAnimationAllowed) {

        if (allowAnimation && enableAnimation() && !browser.animate) {
            require(['webAnimations']);
        }
    }

    return {
        loadView: loadView,
        tryRestoreView: tryRestoreView,
        reset: reset,
        setOnBeforeChange: setOnBeforeChange,
        init: init
    };
});