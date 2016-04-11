define(['browser'], function (browser) {

    function setLayout(self, layout, selectedLayout) {

        if (layout == selectedLayout) {
            self[layout] = true;
            document.documentElement.classList.add('layout-' + layout);
        } else {
            self[layout] = false;
            document.documentElement.classList.remove('layout-' + layout);
        }
    }

    function layoutManager() {

        var self = this;

        self.layout = function (layout) {

            setLayout(self, 'mobile', layout);
            setLayout(self, 'tv', layout);
            setLayout(self, 'desktop', layout);
        };

        self.autoLayout = function () {

            // Take a guess at initial layout. The consuming app can override
            if (browser.mobile) {
                self.layout('mobile');
            } else if (browser.tv) {
                self.layout('tv');
            } else {
                self.layout('desktop');
            }
        };

        self.autoLayout();
    };

    return new layoutManager();
});