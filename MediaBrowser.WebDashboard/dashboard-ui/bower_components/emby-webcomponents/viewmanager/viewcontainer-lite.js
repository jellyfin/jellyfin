define(['browser', 'dom', 'layoutManager', 'css!./viewcontainer-lite'], function (browser, dom, layoutManager) {
    'use strict';

    var mainAnimatedPages = document.querySelector('.mainAnimatedPages');
    var allPages = [];
    var currentUrls = [];
    var pageContainerCount = 3;
    var selectedPageIndex = -1;

    function enableAnimation() {

        // too slow
        if (browser.tv) {
            return false;
        }

        return browser.supportsCssAnimation();
    }

    function findLastView(parent, className) {

        var nodes = parent.childNodes;
        for (var i = nodes.length - 1; i >= 0; i--) {
            var node = nodes[i];
            var classList = node.classList;
            if (classList && classList.contains(className)) {
                return node;
            }
        }
    }

    function findViewBefore(elem, className) {

        var node = elem.previousSibling;
        while (node) {
            var classList = node.classList;
            if (classList && classList.contains(className)) {
                return node;
            }

            node = node.previousSibling;
        }
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

        var viewHtml = options.view;

        var properties = [];
        if (options.fullscreen) {
            properties.push('fullscreen');
        }

        var currentPage = allPages[pageIndex];

        var view;

        if (currentPage) {
            triggerDestroy(currentPage);
            currentPage.insertAdjacentHTML('beforebegin', viewHtml);
            view = findViewBefore(currentPage, 'view');

            mainAnimatedPages.removeChild(currentPage);

        } else {
            mainAnimatedPages.insertAdjacentHTML('beforeend', viewHtml);

            view = findLastView(mainAnimatedPages, 'view');
        }

        view.classList.add('mainAnimatedPage');

        if (properties.length) {
            view.setAttribute('data-properties', properties.join(','));
        }

        if (options.type) {
            view.setAttribute('data-type', options.type);
        }

        allPages[pageIndex] = view;

        if (onBeforeChange) {
            onBeforeChange(view, false, options);
        }

        beforeAnimate(allPages, pageIndex, selected);

        // animate here
        return animate(view, previousAnimatable, options.transition, options.isBack).then(function () {

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

        if (enableAnimation() && oldAnimatedPage) {
            if (transition === 'slide') {
                return slide(newAnimatedPage, oldAnimatedPage, transition, isBack);
            } else if (transition === 'fade') {
                return fade(newAnimatedPage, oldAnimatedPage, transition, isBack);
            } else {
                clearAnimation(newAnimatedPage);
                if (oldAnimatedPage) {
                    clearAnimation(oldAnimatedPage);
                }
            }
        }

        return Promise.resolve();
    }

    function clearAnimation(elem) {
        setAnimation(elem, 'none');
    }

    function slide(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        return new Promise(function (resolve, reject) {

            var duration = layoutManager.tv ? 450 : 160;

            var animations = [];

            if (oldAnimatedPage) {
                if (isBack) {
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
                dom.removeEventListener(newAnimatedPage, dom.whichAnimationEvent(), onAnimationComplete, {
                    once: true
                });
                resolve();
            };

            dom.addEventListener(newAnimatedPage, dom.whichAnimationEvent(), onAnimationComplete, {
                once: true
            });
        });
    }

    function fade(newAnimatedPage, oldAnimatedPage, transition, isBack) {

        return new Promise(function (resolve, reject) {

            var duration = layoutManager.tv ? 450 : 270;
            var animations = [];

            newAnimatedPage.style.opacity = 0;
            setAnimation(newAnimatedPage, 'view-fadein ' + duration + 'ms ease-in normal both');
            animations.push(newAnimatedPage);

            if (oldAnimatedPage) {
                setAnimation(oldAnimatedPage, 'view-fadeout ' + duration + 'ms ease-out normal both');
                animations.push(oldAnimatedPage);
            }

            currentAnimations = animations;

            var onAnimationComplete = function () {
                dom.removeEventListener(newAnimatedPage, dom.whichAnimationEvent(), onAnimationComplete, {
                    once: true
                });
                resolve();
            };

            dom.addEventListener(newAnimatedPage, dom.whichAnimationEvent(), onAnimationComplete, {
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
            animations[i].style.animation = 'none';
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

    return {
        loadView: loadView,
        tryRestoreView: tryRestoreView,
        reset: reset,
        setOnBeforeChange: setOnBeforeChange
    };
});