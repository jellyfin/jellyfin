define(['appSettings', 'backdrop', 'browser', 'globalize', 'require', 'paper-icon-button-light'], function (appSettings, backdrop, browser, globalize, require) {
    'use strict';

    var lastSound = 0;
    var iconCreated;
    var destroyed;
    var currentSound;
    var cancelKey = 'cancelHalloween2015';
    var cancelValue = '6';

    function onPageShow() {
        var page = this;

        if (!destroyed) {

            if (appSettings.get(cancelKey) == cancelValue) {

                destroyed = true;
                return;
            }

            if (!browser.mobile) {

                require(['css!./style.css']);

                if (!page.classList.contains('itemDetailPage')) {
                    backdrop.setBackdrop('themes/halloween/bg.jpg');
                }

                if (lastSound == 0) {
                    playSound('http://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/monsterparadefade.mp3', .1);
                } else if ((new Date().getTime() - lastSound) > 30000) {
                    playSound('http://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/howl.wav');
                }

                addIcon();
            }
        }
    }

    function addIcon() {

        if (iconCreated) {
            return;
        }

        iconCreated = true;

        var viewMenuSecondary = document.querySelector('.viewMenuSecondary');

        if (viewMenuSecondary) {

            var html = '<button is="paper-icon-button-light" class="halloweenInfoButton"><i class="md-icon">info</i></button>';

            viewMenuSecondary.insertAdjacentHTML('afterbegin', html);

            viewMenuSecondary.querySelector('.halloweenInfoButton').addEventListener('click', onIconClick);
        }
    }

    function onIconClick() {

        require(['dialog'], function (dialog) {
            dialog({

                title: "Happy Halloween",
                text: "Happy Halloween from the Emby Team. We hope your Halloween is spooktacular! Would you like to allow the Halloween theme to continue?",

                buttons: [
                    {
                        id: 'yes',
                        name: globalize.translate('ButtonYes'),
                        type: 'submit'
                    },
                    {
                        id: 'no',
                        name: globalize.translate('ButtonNo'),
                        type: 'cancel'
                    }
                ]

            }).then(function (result) {
                if (result == 'no') {
                    destroyTheme();
                }
            });
        });
    }

    function destroyTheme() {

        destroyed = true;

        var halloweenInfoButton = document.querySelector('.halloweenInfoButton');
        if (halloweenInfoButton) {
            halloweenInfoButton.parentNode.removeChild(halloweenInfoButton);
        }

        if (currentSound) {
            currentSound.stop();
        }

        backdrop.clear();
        appSettings.set(cancelKey, cancelValue);
        window.location.reload(true);
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

});