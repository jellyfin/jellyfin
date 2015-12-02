(function () {

    function searchMenu() {

        var self = this;

        self.show = function () {

            require(['css!css/search.css'], function() {
                $('.headerSearchInput').val('');

                $('.btnCloseSearch').hide();
                var elem = $('.viewMenuSearch').removeClass('hide')[0];

                fadeIn(elem, 1).onfinish = function () {
                    $('.headerSearchInput').focus();
                    $('.btnCloseSearch').show();
                };
            });
        };

        function fadeIn(elem, iterations) {

            var keyframes = [
              { opacity: '0', offset: 0 },
              { opacity: '1', offset: 1 }];
            var timing = { duration: 200, iterations: iterations };
            return elem.animate(keyframes, timing);
        }

        self.hide = function () {

            var viewMenuSearch = document.querySelector('.viewMenuSearch');

            if (!viewMenuSearch) {
                return;
            }

            if (!viewMenuSearch.classList.contains('hide')) {
                $('.btnCloseSearch').hide();
                viewMenuSearch.classList.add('hide');
            }
        };

        $('.viewMenuSearchForm').on('submit', function () {

            return false;
        });

        $('.btnCloseSearch').on('click', function () {
            self.hide();
            Events.trigger(self, 'closed');
        });

        $('.headerSearchInput').on("keyup", function (e) {

            // Down key
            if (e.keyCode == 40) {

                //var first = $('.card', panel)[0];

                //if (first) {
                //    first.focus();
                //}

                return false;

            } else {

                Events.trigger(self, 'change', [this.value]);
            }

        }).on("search", function (e) {

            if (!this.value) {

                Events.trigger(self, 'change', ['']);
            }

        });

    }

    window.SearchMenu = new searchMenu();

})();