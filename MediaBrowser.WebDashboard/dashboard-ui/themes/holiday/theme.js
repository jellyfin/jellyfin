(function () {

    var lastSound = 0;
    var iconCreated;
    var destroyed;
    var currentSound;

    function onPageShow() {

        var page = this;

        if (!destroyed) {

            require(['css!themes/holiday/style.css']);

            if (!browserInfo.mobile) {

                if (!page.classList.contains('itemDetailPage')) {
                    Backdrops.setBackdropUrl(page, 'themes/holiday/bg.jpg');
                }

                if (lastSound == 0) {
                    playSound('http://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/monsterparadefade.mp3', .1);
                } else if ((new Date().getTime() - lastSound) > 30000) {
                    playSound('http://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/howl.wav');
                }
            }

            addIcon();
        }
    }

    function onIconClick() {

        // todo: switch this to action sheet

        //require(['dialog'], function (dialog) {
        //    dialog({

        //        title: "Happy Halloween",
        //        message: "Happy Halloween from the Emby Team. We hope your Halloween is spooktacular! Would you like to allow the Halloween theme to continue?",
        //        callback: function (result) {

        //            if (result == 1) {
        //                destroyTheme();
        //            }
        //        },

        //        buttons: [Globalize.translate('ButtonYes'), Globalize.translate('ButtonNo')]
        //    });
        //});
    }

    function addIcon() {

        if (iconCreated) {
            return;
        }

        iconCreated = true;

        var elem = document.createElement('paper-icon-button');
        elem.icon = 'info';
        elem.classList.add('holidayInfoButton');
        elem.addEventListener('click', onIconClick);

        var viewMenuSecondary = document.querySelector('.viewMenuSecondary');

        if (viewMenuSecondary) {
            viewMenuSecondary.insertBefore(elem, viewMenuSecondary.childNodes[0]);
        }
    }

    pageClassOn('pageshow', "libraryPage", onPageShow);

    function playSound(path, volume) {

        require(['howler'], function (howler) {

            var sound = new Howl({
                urls: [path],
                volume: volume || .3
            });

            sound.play();
            currentSound = sound;
            lastSound = new Date().getTime();
        });
    }

})();