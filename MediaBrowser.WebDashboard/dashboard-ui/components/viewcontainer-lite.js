define(["browser", "dom", "layoutManager", "css!bower_components/emby-webcomponents/viewmanager/viewcontainer-lite"], function(browser, dom, layoutManager) {
    "use strict";

    function setControllerClass(view, options) {
        if (options.controllerFactory) return Promise.resolve();
        var controllerUrl = view.getAttribute("data-controller");
        return controllerUrl ? (0 === controllerUrl.indexOf("__plugin/") && (controllerUrl = controllerUrl.substring("__plugin/".length)), controllerUrl = Dashboard.getConfigurationResourceUrl(controllerUrl), getRequirePromise([controllerUrl]).then(function(ControllerFactory) {
            options.controllerFactory = ControllerFactory
        })) : Promise.resolve()
    }

    function getRequirePromise(deps) {
        return new Promise(function(resolve, reject) {
            require(deps, resolve)
        })
    }

    function loadView(options) {
        if (!options.cancel) {
            var selected = selectedPageIndex,
                previousAnimatable = -1 === selected ? null : allPages[selected],
                pageIndex = selected + 1;
            pageIndex >= pageContainerCount && (pageIndex = 0);
            var isPluginpage = -1 !== options.url.toLowerCase().indexOf("/configurationpage"),
                newViewInfo = normalizeNewView(options, isPluginpage),
                newView = newViewInfo.elem,
                dependencies = "string" == typeof newView ? null : newView.getAttribute("data-require");
            return dependencies = dependencies ? dependencies.split(",") : [], isPluginpage && dependencies.push("legacy/dashboard"), newViewInfo.hasjQuerySelect && dependencies.push("legacy/selectmenu"), newViewInfo.hasjQueryChecked && dependencies.push("fnchecked"), newViewInfo.hasjQuery && dependencies.push("jQuery"), (isPluginpage || newView.classList && newView.classList.contains("type-interior")) && dependencies.push("dashboardcss"), new Promise(function(resolve, reject) {
                dependencies.join(",");
                require(dependencies, function() {
                    var currentPage = allPages[pageIndex];
                    currentPage && triggerDestroy(currentPage);
                    var view = newView;
                    "string" == typeof view && (view = document.createElement("div"), view.innerHTML = newView), view.classList.add("mainAnimatedPage"), currentPage ? newViewInfo.hasScript && window.$ ? (view = $(view).appendTo(mainAnimatedPages)[0], mainAnimatedPages.removeChild(currentPage)) : mainAnimatedPages.replaceChild(view, currentPage) : newViewInfo.hasScript && window.$ ? view = $(view).appendTo(mainAnimatedPages)[0] : mainAnimatedPages.appendChild(view), options.type && view.setAttribute("data-type", options.type);
                    var properties = [];
                    options.fullscreen && properties.push("fullscreen"), properties.length && view.setAttribute("data-properties", properties.join(","));
                    allPages[pageIndex] = view, setControllerClass(view, options).then(function() {
                        onBeforeChange && onBeforeChange(view, !1, options), beforeAnimate(allPages, pageIndex, selected), selectedPageIndex = pageIndex, currentUrls[pageIndex] = options.url, !options.cancel && previousAnimatable && afterAnimate(allPages, pageIndex), window.$ && ($.mobile = $.mobile || {}, $.mobile.activePage = view), resolve(view)
                    })
                })
            })
        }
    }

    function replaceAll(str, find, replace) {
        return str.split(find).join(replace)
    }

    function parseHtml(html, hasScript) {
        hasScript && (html = replaceAll(html, "\x3c!--<script", "<script"), html = replaceAll(html, "<\/script>--\x3e", "<\/script>"));
        var wrapper = document.createElement("div");
        return wrapper.innerHTML = html, wrapper.querySelector('div[data-role="page"]')
    }

    function normalizeNewView(options, isPluginpage) {
        var viewHtml = options.view;
        if (-1 === viewHtml.indexOf('data-role="page"')) return viewHtml;
        var hasScript = -1 !== viewHtml.indexOf("<script"),
            elem = parseHtml(viewHtml, hasScript);
        hasScript && (hasScript = null != elem.querySelector("script"));
        var hasjQuery = !1,
            hasjQuerySelect = !1,
            hasjQueryChecked = !1;
        return isPluginpage && (hasjQuery = -1 != viewHtml.indexOf("jQuery") || -1 != viewHtml.indexOf("$(") || -1 != viewHtml.indexOf("$."), hasjQueryChecked = -1 != viewHtml.indexOf(".checked("), hasjQuerySelect = -1 != viewHtml.indexOf(".selectmenu(")), {
            elem: elem,
            hasScript: hasScript,
            hasjQuerySelect: hasjQuerySelect,
            hasjQueryChecked: hasjQueryChecked,
            hasjQuery: hasjQuery
        }
    }

    function beforeAnimate(allPages, newPageIndex, oldPageIndex) {
        for (var i = 0, length = allPages.length; i < length; i++) newPageIndex === i || oldPageIndex === i || allPages[i].classList.add("hide")
    }

    function afterAnimate(allPages, newPageIndex) {
        for (var i = 0, length = allPages.length; i < length; i++) newPageIndex === i || allPages[i].classList.add("hide")
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
                var selected = selectedPageIndex,
                    previousAnimatable = -1 === selected ? null : allPages[selected];
                return setControllerClass(view, options).then(function() {
                    return onBeforeChange && onBeforeChange(view, !0, options), beforeAnimate(allPages, index, selected), animatable.classList.remove("hide"), selectedPageIndex = index, !options.cancel && previousAnimatable && afterAnimate(allPages, index), window.$ && ($.mobile = $.mobile || {}, $.mobile.activePage = view), view
                })
            }
        }
        return Promise.reject()
    }

    function triggerDestroy(view) {
        view.dispatchEvent(new CustomEvent("viewdestroy", {}))
    }

    function reset() {
        allPages = [], currentUrls = [], mainAnimatedPages.innerHTML = "", selectedPageIndex = -1
    }
    var onBeforeChange, mainAnimatedPages = document.querySelector(".mainAnimatedPages"),
        allPages = [],
        currentUrls = [],
        pageContainerCount = 3,
        selectedPageIndex = -1;
    return reset(), mainAnimatedPages.classList.remove("hide"), {
        loadView: loadView,
        tryRestoreView: tryRestoreView,
        reset: reset,
        setOnBeforeChange: setOnBeforeChange
    }
});