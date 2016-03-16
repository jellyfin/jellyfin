define([], function () {

    function fadeIn(elem, iterations) {

        var keyframes = [
          { opacity: '0', offset: 0 },
          { opacity: '1', offset: 1 }];
        var timing = { duration: 200, iterations: iterations };
        return elem.animate(keyframes, timing);
    }

    function searchMenu() {

        var self = this;

        self.show = function () {

            require(['css!css/search.css'], function () {

                document.querySelector('.headerSearchInput').value = '';

                document.querySelector('.btnCloseSearch').classList.add('hide');
                var elem = document.querySelector('.viewMenuSearch');

                elem.classList.remove('hide');

                var onFinish = function() {
                    document.querySelector('.headerSearchInput').focus();
                    document.querySelector('.btnCloseSearch').classList.remove('hide');
                };

                if (elem.animate) {
                    fadeIn(elem, 1).onfinish = onFinish;
                } else {
                    onFinish();
                }

            });
        };

        self.hide = function () {

            var viewMenuSearch = document.querySelector('.viewMenuSearch');

            if (!viewMenuSearch) {
                return;
            }

            if (!viewMenuSearch.classList.contains('hide')) {
                document.querySelector('.btnCloseSearch').classList.add('hide');
                viewMenuSearch.classList.add('hide');
            }
        };

        document.querySelector('.viewMenuSearchForm').addEventListener('submit', function (e) {
            e.preventDefault();
            return false;
        });

        document.querySelector('.btnCloseSearch').addEventListener('click', function () {
            self.hide();
            Events.trigger(self, 'closed');
        });

        document.querySelector('.headerSearchInput').addEventListener('keyup', function (e) {

            // Down key
            if (e.keyCode == 40) {

                e.preventDefault();
                return false;

            } else {

                Events.trigger(self, 'change', [this.value]);
            }
        });

        document.querySelector('.headerSearchInput').addEventListener('search', function (e) {
            if (!this.value) {
                Events.trigger(self, 'change', ['']);
            }
        });
    }

    window.SearchMenu = new searchMenu();
    return Window.SearchMenu;
});