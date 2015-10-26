(function () {

    function searchMenu() {

        var self = this;

        self.show = function () {

            $('.headerSearchInput').val('');

            require(["jquery", "velocity"], function ($, Velocity) {

                $('.btnCloseSearch').hide();
                var elem = $('.viewMenuSearch')
                    .css({ left: '100%' })
                    .removeClass('hide')[0];

                Velocity.animate(elem, { "left": "0px" },
                {
                    complete: function () {
                        $('.headerSearchInput').focus();
                        $('.btnCloseSearch').show();
                    }
                });
            });
        };

        self.hide = function () {

            var viewMenuSearch = document.querySelector('.viewMenuSearch');

            if (!viewMenuSearch) {
                return;
            }

            if (!viewMenuSearch.classList.contains('hide')) {
                require(["jquery", "velocity"], function ($, Velocity) {

                    $('.btnCloseSearch').hide();
                    viewMenuSearch.style.left = '0';

                    Velocity.animate(viewMenuSearch, { "left": "100%" },
                    {
                        complete: function () {
                            $('.viewMenuSearch').visible(false);
                        }
                    });
                });
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