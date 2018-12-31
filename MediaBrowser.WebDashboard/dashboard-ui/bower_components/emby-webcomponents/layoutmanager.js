define(['browser', 'appSettings', 'events'], function (browser, appSettings, events) {
    'use strict';

    function setLayout(instance, layout, selectedLayout) {

        if (layout === selectedLayout) {
            instance[layout] = true;
            document.documentElement.classList.add('layout-' + layout);
        } else {
            instance[layout] = false;
            document.documentElement.classList.remove('layout-' + layout);
        }
    }

    function LayoutManager() {

    }

    LayoutManager.prototype.setLayout = function (layout, save) {

        if (!layout || layout === 'auto') {
            this.autoLayout();

            if (save !== false) {
                appSettings.set('layout', '');
            }
        } else {
            setLayout(this, 'mobile', layout);
            setLayout(this, 'tv', layout);
            setLayout(this, 'desktop', layout);

            if (save !== false) {
                appSettings.set('layout', layout);
            }
        }

        events.trigger(this, 'modechange');
    };

    LayoutManager.prototype.getSavedLayout = function (layout) {

        return appSettings.get('layout');
    };

    LayoutManager.prototype.autoLayout = function () {

        // Take a guess at initial layout. The consuming app can override
        if (browser.mobile) {
            this.setLayout('mobile', false);
        } else if (browser.tv || browser.xboxOne) {
            this.setLayout('tv', false);
        } else {
            this.setLayout(this.defaultLayout || 'tv', false);
        }
    };

    LayoutManager.prototype.init = function () {
        var saved = this.getSavedLayout();
        if (saved) {
            this.setLayout(saved, false);
        } else {
            this.autoLayout();
        }
    };

    return new LayoutManager();
});