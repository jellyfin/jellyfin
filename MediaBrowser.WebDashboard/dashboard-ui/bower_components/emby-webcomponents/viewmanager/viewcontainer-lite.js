define(['browser', 'dom', 'css!./viewcontainer-lite'], function (browser, dom) {
    'use strict';

    var mainAnimatedPages = document.querySelector('.mainAnimatedPages');
    var allPages = [];
    var currentUrls = [];
    var pageContainerCount = 3;
    var selectedPageIndex = -1;

    function enableAnimation() {

        if (browser.animate) {
            return true;
        }

        if (browser.tv) {
            return false;
        }

        if (browser.operaTv) {
            return false;
        }

        return browser.edge && !browser.mobile;
    }

    function loadView(options) {

        if (options.cancel) {
            return;
        }

        cancelActiveAnimations();

        var selected = selectedPageIndex;
        var previousAnimatable = selected === -1 ? null : allPages[selected];
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
            if (newPageIndex === i || oldPageIndex === i) {
                //allPages[i].classList.remove('hide');
            } else {
                allPages[i].classList.add('hide');
            }
        }
    }

    function afterAnimate(allPages, newPageIndex) {
        for (var i = 0, length = allPages.length; i < length; i++) {
            if (newPageIndex === i) {
                //allPages[i].classList.remove('hide');
            } else {
                allPages[i].classList.add('hide');
            }
        }
    }

    function animate(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        if (enableAnimation() && oldAnimatedPage && newAnimatedPage.animate) {
            if (transition === 'slide') {
                return slide(newAnimatedPage, oldAnimatedPage, transition, isBack);
            } else if (transition === 'fade') {
                return fade(newAnimatedPage, oldAnimatedPage, transition, isBack);
            }
        }

        return Promise.resolve();
    }

    function slide(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        return new Promise(function (resolve, reject) {

            var duration = 450;

            var animations = [];

            if (oldAnimatedPage) {
                if (isBack) {
                    oldAnimatedPage.style.animation = 'view-slideright-r ' + duration + 'ms ease-out normal both';
                    setAnimation(oldAnimatedPage, 'view-slideright-r ' + duration + 'ms ease-out normal both');
                } else {
                    setAnimation(oldAnimatedPage, 'view-slideleft-r ' + duration + 'ms ease-out normal both');
                }
                animations.push(oldAnimatedPage);
            }

            if (isBack) {
                setAnimation(newAnimatedPage, 'view-slideright ' + duration + 'ms ease-out normal both');
            } else {
                setAnimation(newAnimatedPage, 'view-slideleft ' + duration + 'ms ease-out normal both');
            }
            animations.push(newAnimatedPage);

            currentAnimations = animations;

            var onAnimationComplete = function () {
                dom.removeEventListener(newAnimatedPage, 'animationend', onAnimationComplete, {
                    once: true
                });
                resolve();
            };

            dom.addEventListener(newAnimatedPage, 'animationend', onAnimationComplete, {
                once: true
            });
        });
    }

    function fade(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        return new Promise(function (resolve, reject) {

            var duration = 400;
            var animations = [];

            if (oldAnimatedPage) {
                setAnimation(oldAnimatedPage, 'view-fadeout ' + duration + 'ms ease-out normal both');
                animations.push(oldAnimatedPage);
            }

            setAnimation(newAnimatedPage, 'view-fadein ' + duration + 'ms ease-in normal both');
            animations.push(newAnimatedPage);

            currentAnimations = animations;

            var onAnimationComplete = function () {
                dom.removeEventListener(newAnimatedPage, 'animationend', onAnimationComplete, {
                    once: true
                });
                resolve();
            };

            dom.addEventListener(newAnimatedPage, 'animationend', onAnimationComplete, {
                once: true
            });
        });
    }

    function setAnimation(elem, value) {

        requestAnimationFrame(function () {
            elem.style.animation = value;
        });
    }

    var currentAnimations = [];
    function cancelActiveAnimations() {

        var animations = currentAnimations;
        for (var i = 0, length = animations.length; i < length; i++) {
            animations[i].animation = 'none';
        }
    }

    var onBeforeChange;
    function setOnBeforeChange(fn) {
        onBeforeChange = fn;
    }

    function tryRestoreView(options) {

        var url = options.url;
        var index = currentUrls.indexOf(url);

        if (index !== -1) {

            var animatable = allPages[index];
            var view = animatable;

            if (view) {

                if (options.cancel) {
                    return;
                }

                cancelActiveAnimations();

                var selected = selectedPageIndex;
                var previousAnimatable = selected === -1 ? null : allPages[selected];

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

        view.dispatchEvent(new CustomEvent('viewdestroy', {
            cancelable: false
        }));
    }

    function reset() {

        allPages = [];
        currentUrls = [];
        mainAnimatedPages.innerHTML = '';
        selectedPageIndex = -1;
    }

    if (enableAnimation()) {
        require(['webAnimations']);
    }

    return {
        loadView: loadView,
        tryRestoreView: tryRestoreView,
        reset: reset,
        setOnBeforeChange: setOnBeforeChange
    };
});