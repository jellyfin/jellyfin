define(["browser", "dom", "layoutManager", "css!./viewcontainer-lite"], function(browser, dom, layoutManager) {
    "use strict";

    function enableAnimation() {
        return !browser.tv && browser.supportsCssAnimation()
    }

    function findLastView(parent, className) {
        for (var nodes = parent.childNodes, i = nodes.length - 1; i >= 0; i--) {
            var node = nodes[i],
                classList = node.classList;
            if (classList && classList.contains(className)) return node
        }
    }

    function findViewBefore(elem, className) {
        for (var node = elem.previousSibling; node;) {
            var classList = node.classList;
            if (classList && classList.contains(className)) return node;
            node = node.previousSibling
        }
    }

    function loadView(options) {
        if (!options.cancel) {
            cancelActiveAnimations();
            var selected = selectedPageIndex,
                previousAnimatable = -1 === selected ? null : allPages[selected],
                pageIndex = selected + 1;
            pageIndex >= pageContainerCount && (pageIndex = 0);
            var viewHtml = options.view,
                properties = [];
            options.fullscreen && properties.push("fullscreen");
            var view, currentPage = allPages[pageIndex];
            return currentPage ? (triggerDestroy(currentPage), currentPage.insertAdjacentHTML("beforebegin", viewHtml), view = findViewBefore(currentPage, "view"), mainAnimatedPages.removeChild(currentPage)) : (mainAnimatedPages.insertAdjacentHTML("beforeend", viewHtml), view = findLastView(mainAnimatedPages, "view")), view.classList.add("mainAnimatedPage"), properties.length && view.setAttribute("data-properties", properties.join(",")), options.type && view.setAttribute("data-type", options.type), allPages[pageIndex] = view, onBeforeChange && onBeforeChange(view, !1, options), beforeAnimate(allPages, pageIndex, selected), animate(view, previousAnimatable, options.transition, options.isBack).then(function() {
                return selectedPageIndex = pageIndex, currentUrls[pageIndex] = options.url, !options.cancel && previousAnimatable && afterAnimate(allPages, pageIndex), view
            })
        }
    }

    function beforeAnimate(allPages, newPageIndex, oldPageIndex) {
        for (var i = 0, length = allPages.length; i < length; i++) newPageIndex === i || oldPageIndex === i || allPages[i].classList.add("hide")
    }

    function afterAnimate(allPages, newPageIndex) {
        for (var i = 0, length = allPages.length; i < length; i++) newPageIndex === i || allPages[i].classList.add("hide")
    }

    function animate(newAnimatedPage, oldAnimatedPage, transition, isBack) {
        if (enableAnimation() && oldAnimatedPage) {
            if ("slide" === transition) return slide(newAnimatedPage, oldAnimatedPage, transition, isBack);
            if ("fade" === transition) return fade(newAnimatedPage, oldAnimatedPage, transition, isBack);
            clearAnimation(newAnimatedPage), oldAnimatedPage && clearAnimation(oldAnimatedPage)
        }
        return Promise.resolve()
    }

    function clearAnimation(elem) {
        setAnimation(elem, "none")
    }

    function slide(newAnimatedPage, oldAnimatedPage, transition, isBack) {
        return new Promise(function(resolve, reject) {
            var duration = layoutManager.tv ? 450 : 160,
                animations = [];
            oldAnimatedPage && (isBack ? setAnimation(oldAnimatedPage, "view-slideright-r " + duration + "ms ease-out normal both") : setAnimation(oldAnimatedPage, "view-slideleft-r " + duration + "ms ease-out normal both"), animations.push(oldAnimatedPage)), isBack ? setAnimation(newAnimatedPage, "view-slideright " + duration + "ms ease-out normal both") : setAnimation(newAnimatedPage, "view-slideleft " + duration + "ms ease-out normal both"), animations.push(newAnimatedPage), currentAnimations = animations;
            var onAnimationComplete = function() {
                dom.removeEventListener(newAnimatedPage, dom.whichAnimationEvent(), onAnimationComplete, {
                    once: !0
                }), resolve()
            };
            dom.addEventListener(newAnimatedPage, dom.whichAnimationEvent(), onAnimationComplete, {
                once: !0
            })
        })
    }

    function fade(newAnimatedPage, oldAnimatedPage, transition, isBack) {
        return new Promise(function(resolve, reject) {
            var duration = layoutManager.tv ? 450 : 270,
                animations = [];
            newAnimatedPage.style.opacity = 0, setAnimation(newAnimatedPage, "view-fadein " + duration + "ms ease-in normal both"), animations.push(newAnimatedPage), oldAnimatedPage && (setAnimation(oldAnimatedPage, "view-fadeout " + duration + "ms ease-out normal both"), animations.push(oldAnimatedPage)), currentAnimations = animations;
            var onAnimationComplete = function() {
                dom.removeEventListener(newAnimatedPage, dom.whichAnimationEvent(), onAnimationComplete, {
                    once: !0
                }), resolve()
            };
            dom.addEventListener(newAnimatedPage, dom.whichAnimationEvent(), onAnimationComplete, {
                once: !0
            })
        })
    }

    function setAnimation(elem, value) {
        requestAnimationFrame(function() {
            elem.style.animation = value
        })
    }

    function cancelActiveAnimations() {
        for (var animations = currentAnimations, i = 0, length = animations.length; i < length; i++) animations[i].style.animation = "none"
    }

    function setOnBeforeChange(fn) {
        onBeforeChange = fn
    }

    function tryRestoreView(options) {
        var url = options.url,
            index = currentUrls.indexOf(url);
        if (-1 !== index) {
            var animatable = allPages[index],
                view = animatable;
            if (view) {
                if (options.cancel) return;
                cancelActiveAnimations();
                var selected = selectedPageIndex,
                    previousAnimatable = -1 === selected ? null : allPages[selected];
                return onBeforeChange && onBeforeChange(view, !0, options), beforeAnimate(allPages, index, selected), animatable.classList.remove("hide"), animate(animatable, previousAnimatable, options.transition, options.isBack).then(function() {
                    return selectedPageIndex = index, !options.cancel && previousAnimatable && afterAnimate(allPages, index), view
                })
            }
        }
        return Promise.reject()
    }

    function triggerDestroy(view) {
        view.dispatchEvent(new CustomEvent("viewdestroy", {
            cancelable: !1
        }))
    }

    function reset() {
        allPages = [], currentUrls = [], mainAnimatedPages.innerHTML = "", selectedPageIndex = -1
    }
    var onBeforeChange, mainAnimatedPages = document.querySelector(".mainAnimatedPages"),
        allPages = [],
        currentUrls = [],
        pageContainerCount = 3,
        selectedPageIndex = -1,
        currentAnimations = [];
    return {
        loadView: loadView,
        tryRestoreView: tryRestoreView,
        reset: reset,
        setOnBeforeChange: setOnBeforeChange
    }
});