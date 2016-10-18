define(['browser', 'appSettings', 'events'], function (browser, appSettings, events) {
    'use strict';

    function setLayout(self, layout, selectedLayout) {

        if (layout === selectedLayout) {
            self[layout] = true;
            document.documentElement.classList.add('layout-' + layout);
        } else {
            self[layout] = false;
            document.documentElement.classList.remove('layout-' + layout);
        }
    }

    function LayoutManager() {

        var self = this;

        self.setLayout = function (layout, save) {

            if (!layout || layout === 'auto') {
                self.autoLayout();

                if (save !== false) {
                    appSettings.set('layout', '');
                }
            } else {
                setLayout(self, 'mobile', layout);
                setLayout(self, 'tv', layout);
                setLayout(self, 'desktop', layout);

                if (save !== false) {
                    appSettings.set('layout', layout);
                }
            }

            events.trigger(self, 'modechange');
        };

        self.getSavedLayout = function (layout) {

            return appSettings.get('layout');
        };

        self.autoLayout = function () {

            // Take a guess at initial layout. The consuming app can override
            if (browser.mobile) {
                self.setLayout('mobile', false);
            } else if (browser.tv || browser.xboxOne) {
                self.setLayout('tv', false);
            } else {
                self.setLayout(self.undetectedAutoLayout || 'desktop', false);
            }
        };

        self.init = function () {
            var saved = self.getSavedLayout();
            if (saved) {
                self.setLayout(saved, false);
            } else {
                self.autoLayout();
            }
        };
    }

    return new LayoutManager();
});