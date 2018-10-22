define(["dom", "browser", "events", "emby-tabs", "emby-button", "emby-linkbutton"], function(dom, browser, events) {
    "use strict";

    function enableTabsInFooter() {
        return !1
    }

    function ensureElements(enableInFooter) {
        enableInFooter && (footerTabsContainer || (footerTabsContainer = document.createElement("div"), footerTabsContainer.classList.add("footerTabs"), footerTabsContainer.classList.add("sectionTabs"), footerTabsContainer.classList.add("hide"))), headerTabsContainer || (headerTabsContainer = queryScope.querySelector(".headerTabs"))
    }

    function onViewTabsReady() {
        this.selectedIndex(this.readySelectedIndex), this.readySelectedIndex = null
    }

    function allowSwipe(target) {
        for (var parent = target; null != parent;) {
            if (! function(elem) {
                    if (dom.parentWithTag(elem, "input")) return !1;
                    var classList = elem.classList;
                    return !classList || !classList.contains("scrollX") && !classList.contains("animatedScrollX")
                }(parent)) return !1;
            parent = parent.parentNode
        }
        return !0
    }

    function configureSwipeTabs(view, tabsElem, getTabContainersFn) {
        if (browser.touch) {
            var onSwipeLeft = (getTabContainersFn().length, function(e, target) {
                    allowSwipe(target) && view.contains(target) && tabsElem.selectNext()
                }),
                onSwipeRight = function(e, target) {
                    allowSwipe(target) && view.contains(target) && tabsElem.selectPrevious()
                };
            require(["touchHelper"], function(TouchHelper) {
                var touchHelper = new TouchHelper(view.parentNode.parentNode);
                events.on(touchHelper, "swipeleft", onSwipeLeft), events.on(touchHelper, "swiperight", onSwipeRight), view.addEventListener("viewdestroy", function() {
                    touchHelper.destroy()
                })
            })
        }
    }

    function setTabs(view, selectedIndex, getTabsFn, getTabContainersFn, onBeforeTabChange, onTabChange, setSelectedIndex) {
        var enableInFooter = enableTabsInFooter();
        if (!view) return tabOwnerView && (headerTabsContainer || (headerTabsContainer = queryScope.querySelector(".headerTabs")), ensureElements(enableInFooter), document.body.classList.remove("withSectionTabs"), headerTabsContainer.innerHTML = "", headerTabsContainer.classList.add("hide"), footerTabsContainer && (footerTabsContainer.innerHTML = "", footerTabsContainer.classList.add("hide")), tabOwnerView = null), {
            tabsContainer: headerTabsContainer,
            replaced: !1
        };
        ensureElements(enableInFooter);
        var tabsContainerElem = enableInFooter ? footerTabsContainer : headerTabsContainer;
        if (tabOwnerView || tabsContainerElem.classList.remove("hide"), tabOwnerView !== view) {
            var index = 0,
                indexAttribute = null == selectedIndex ? "" : ' data-index="' + selectedIndex + '"',
                tabsHtml = '<div is="emby-tabs"' + indexAttribute + ' class="tabs-viewmenubar"><div class="emby-tabs-slider" style="white-space:nowrap;">' + getTabsFn().map(function(t) {
                    var tabClass = "emby-tab-button";
                    !1 === t.enabled && (tabClass += " hide");
                    var tabHtml;
                    return t.cssClass && (tabClass += " " + t.cssClass), tabHtml = t.href ? '<a href="' + t.href + '" is="emby-linkbutton" class="' + tabClass + '" data-index="' + index + '"><div class="emby-button-foreground">' + t.name + "</div></a>" : '<button type="button" is="emby-button" class="' + tabClass + '" data-index="' + index + '"><div class="emby-button-foreground">' + t.name + "</div></button>", index++, tabHtml
                }).join("") + "</div></div>";
            return tabsContainerElem.innerHTML = tabsHtml, document.body.classList.add("withSectionTabs"), tabOwnerView = view, tabsElem = tabsContainerElem.querySelector('[is="emby-tabs"]'), configureSwipeTabs(view, tabsElem, getTabContainersFn), tabsElem.addEventListener("beforetabchange", function(e) {
                var tabContainers = getTabContainersFn();
                if (null != e.detail.previousIndex) {
                    var previousPanel = tabContainers[e.detail.previousIndex];
                    previousPanel && previousPanel.classList.remove("is-active")
                }
                var newPanel = tabContainers[e.detail.selectedTabIndex];
                newPanel && newPanel.classList.add("is-active")
            }), onBeforeTabChange && tabsElem.addEventListener("beforetabchange", onBeforeTabChange), onTabChange && tabsElem.addEventListener("tabchange", onTabChange), !1 !== setSelectedIndex && (tabsElem.selectedIndex ? tabsElem.selectedIndex(selectedIndex) : (tabsElem.readySelectedIndex = selectedIndex, tabsElem.addEventListener("ready", onViewTabsReady))), {
                tabsContainer: tabsContainerElem,
                tabs: tabsContainerElem.querySelector('[is="emby-tabs"]'),
                replaced: !0
            }
        }
        return tabsElem || (tabsElem = tabsContainerElem.querySelector('[is="emby-tabs"]')), tabsElem.selectedIndex(selectedIndex), tabOwnerView = view, {
            tabsContainer: tabsContainerElem,
            tabs: tabsElem,
            replaced: !1
        }
    }

    function selectedTabIndex(index) {
        var tabsContainerElem = headerTabsContainer;
        tabsElem || (tabsElem = tabsContainerElem.querySelector('[is="emby-tabs"]')), null != index ? tabsElem.selectedIndex(index) : tabsElem.triggerTabChange()
    }

    function getTabsElement() {
        return document.querySelector(".tabs-viewmenubar")
    }
    var tabOwnerView, footerTabsContainer, headerTabsContainer, tabsElem, queryScope = document.querySelector(".skinHeader");
    return {
        setTabs: setTabs,
        getTabsElement: getTabsElement,
        selectedTabIndex: selectedTabIndex
    }
});