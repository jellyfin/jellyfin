define(['dom', 'scroller', 'browser', 'layoutManager', 'focusManager', 'registerElement', 'css!./emby-tabs', 'scrollStyles'], function (dom, scroller, browser, layoutManager, focusManager) {
    'use strict';

    var EmbyTabs = Object.create(HTMLDivElement.prototype);
    var buttonClass = 'emby-tab-button';
    var activeButtonClass = buttonClass + '-active';

    function setActiveTabButton(tabs, newButton, oldButton, animate) {

        newButton.classList.add(activeButtonClass);
    }

    function getFocusCallback(tabs, e) {
        return function () {
            onClick.call(tabs, e);
        };
    }

    function onFocus(e) {

        if (layoutManager.tv) {

            if (this.focusTimeout) {
                clearTimeout(this.focusTimeout);
            }
            this.focusTimeout = setTimeout(getFocusCallback(this, e), 700);
        }
    }

    function getTabPanel(tabs, index) {

        return null;
    }

    function removeActivePanelClass(tabs, index) {
        var tabPanel = getTabPanel(tabs, index);
        if (tabPanel) {
            tabPanel.classList.remove('is-active');
        }
    }

    function addActivePanelClass(tabs, index) {
        var tabPanel = getTabPanel(tabs, index);
        if (tabPanel) {
            tabPanel.classList.add('is-active');
        }
    }

    function fadeInRight(elem) {

        var pct = browser.mobile ? '4%' : '0.5%';

        var keyframes = [
            { opacity: '0', transform: 'translate3d(' + pct + ', 0, 0)', offset: 0 },
            { opacity: '1', transform: 'none', offset: 1 }];

        elem.animate(keyframes, {
            duration: 160,
            iterations: 1,
            easing: 'ease-out'
        });
    }

    function triggerBeforeTabChange(tabs, index, previousIndex) {

        tabs.dispatchEvent(new CustomEvent("beforetabchange", {
            detail: {
                selectedTabIndex: index,
                previousIndex: previousIndex
            }
        }));
        if (previousIndex != null && previousIndex !== index) {
            removeActivePanelClass(tabs, previousIndex);
        }

        var newPanel = getTabPanel(tabs, index);

        if (newPanel) {
            // animate new panel ?
            if (newPanel.animate) {
                fadeInRight(newPanel);
            }

            newPanel.classList.add('is-active');
        }
    }

    function onClick(e) {

        if (this.focusTimeout) {
            clearTimeout(this.focusTimeout);
        }

        var tabs = this;

        var current = tabs.querySelector('.' + activeButtonClass);
        var tabButton = dom.parentWithClass(e.target, buttonClass);

        if (tabButton && tabButton !== current) {

            if (current) {
                current.classList.remove(activeButtonClass);
            }

            var previousIndex = current ? parseInt(current.getAttribute('data-index')) : null;

            setActiveTabButton(tabs, tabButton, current, true);

            var index = parseInt(tabButton.getAttribute('data-index'));

            triggerBeforeTabChange(tabs, index, previousIndex);

            // If toCenter is called syncronously within the click event, it sometimes ends up canceling it
            setTimeout(function () {

                tabs.selectedTabIndex = index;

                tabs.dispatchEvent(new CustomEvent("tabchange", {
                    detail: {
                        selectedTabIndex: index,
                        previousIndex: previousIndex
                    }
                }));
            }, 120);

            if (tabs.scroller) {
                tabs.scroller.toCenter(tabButton, false);
            }

        }
    }

    function initScroller(tabs) {

        if (tabs.scroller) {
            return;
        }

        var contentScrollSlider = tabs.querySelector('.emby-tabs-slider');
        if (contentScrollSlider) {
            tabs.scroller = new scroller(tabs, {
                horizontal: 1,
                itemNav: 0,
                mouseDragging: 1,
                touchDragging: 1,
                slidee: contentScrollSlider,
                smart: true,
                releaseSwing: true,
                scrollBy: 200,
                speed: 120,
                elasticBounds: 1,
                dragHandle: 1,
                dynamicHandle: 1,
                clickBar: 1,
                hiddenScroll: true,

                // In safari the transform is causing the headers to occasionally disappear or flicker
                requireAnimation: !browser.safari,
                allowNativeSmoothScroll: true
            });
            tabs.scroller.init();
        } else {
            tabs.classList.add('scrollX');
            tabs.classList.add('hiddenScrollX');
            tabs.classList.add('smoothScrollX');
       }
    }

    EmbyTabs.createdCallback = function () {

        if (this.classList.contains('emby-tabs')) {
            return;
        }
        this.classList.add('emby-tabs');
        this.classList.add('focusable');

        dom.addEventListener(this, 'click', onClick, {
            passive: true
        });
        dom.addEventListener(this, 'focus', onFocus, {
            passive: true,
            capture: true
        });
    };

    EmbyTabs.focus = function () {

        var selected = this.querySelector('.' + activeButtonClass);

        if (selected) {
            focusManager.focus(selected);
        } else {
            focusManager.autoFocus(this);
        }
    };

    EmbyTabs.refresh = function () {

        if (this.scroller) {
            this.scroller.reload();
        }
    };

    EmbyTabs.attachedCallback = function () {

        initScroller(this);

        var current = this.querySelector('.' + activeButtonClass);
        var currentIndex = current ? parseInt(current.getAttribute('data-index')) : parseInt(this.getAttribute('data-index') || '0');

        if (currentIndex !== -1) {

            this.selectedTabIndex = currentIndex;

            var tabButtons = this.querySelectorAll('.' + buttonClass);

            var newTabButton = tabButtons[currentIndex];

            if (newTabButton) {
                setActiveTabButton(this, newTabButton, current, false);
            }
        }

        if (!this.readyFired) {
            this.readyFired = true;
            this.dispatchEvent(new CustomEvent("ready", {}));
        }
    };

    EmbyTabs.detachedCallback = function () {

        if (this.scroller) {
            this.scroller.destroy();
            this.scroller = null;
        }

        dom.removeEventListener(this, 'click', onClick, {
            passive: true
        });
        dom.removeEventListener(this, 'focus', onFocus, {
            passive: true,
            capture: true
        });
    };

    function getSelectedTabButton(elem) {

        return elem.querySelector('.' + activeButtonClass);
    }

    EmbyTabs.selectedIndex = function (selected, triggerEvent) {

        var tabs = this;

        if (selected == null) {

            return tabs.selectedTabIndex || 0;
        }

        var current = tabs.selectedIndex();

        tabs.selectedTabIndex = selected;

        var tabButtons = tabs.querySelectorAll('.' + buttonClass);

        if (current === selected || triggerEvent === false) {

            triggerBeforeTabChange(tabs, selected, current);

            tabs.dispatchEvent(new CustomEvent("tabchange", {
                detail: {
                    selectedTabIndex: selected
                }
            }));

            var currentTabButton = tabButtons[current];
            setActiveTabButton(tabs, tabButtons[selected], currentTabButton, false);

            if (current !== selected && currentTabButton) {
                currentTabButton.classList.remove(activeButtonClass);
            }

        } else {

            onClick.call(tabs, {
                target: tabButtons[selected]
            });
            //tabButtons[selected].click();
        }
    };

    function getSibling(elem, method) {

        var sibling = elem[method];

        while (sibling) {
            if (sibling.classList.contains(buttonClass)) {

                if (!sibling.classList.contains('hide')) {
                    return sibling;
                }
            }

            sibling = sibling[method];
        }

        return null;
    }

    EmbyTabs.selectNext = function () {

        var current = getSelectedTabButton(this);

        var sibling = getSibling(current, 'nextSibling');

        if (sibling) {
            onClick.call(this, {
                target: sibling
            });
        }
    };

    EmbyTabs.selectPrevious = function () {

        var current = getSelectedTabButton(this);

        var sibling = getSibling(current, 'previousSibling');

        if (sibling) {
            onClick.call(this, {
                target: sibling
            });
        }
    };

    EmbyTabs.triggerBeforeTabChange = function (selected) {

        var tabs = this;

        triggerBeforeTabChange(tabs, tabs.selectedIndex());
    };

    EmbyTabs.triggerTabChange = function (selected) {

        var tabs = this;

        tabs.dispatchEvent(new CustomEvent("tabchange", {
            detail: {
                selectedTabIndex: tabs.selectedIndex()
            }
        }));
    };

    EmbyTabs.setTabEnabled = function (index, enabled) {

        var tabs = this;
        var btn = this.querySelector('.emby-tab-button[data-index="' + index + '"]');

        if (enabled) {
            btn.classList.remove('hide');
        } else {
            btn.classList.remove('add');
        }
    };

    document.registerElement('emby-tabs', {
        prototype: EmbyTabs,
        extends: 'div'
    });
});