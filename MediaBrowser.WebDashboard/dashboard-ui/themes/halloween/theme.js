(function () {

    Dashboard.importCss('themes/halloween/style.css');

    var lastSound = 0;

    function onPageShow() {
        var page = this;

        if (!$.browser.mobile && !page.classList.contains('itemDetailPage')) {
            Backdrops.setBackdropUrl(page, 'themes/halloween/bg.jpg');
        }

        if (lastSound == 0) {
            playSound('http://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/monsterparade.mp3', .2);
        } else if ((new Date().getTime() - lastSound) > 30000) {
            playSound('http://github.com/MediaBrowser/Emby.Resources/raw/master/themes/halloween/howl.wav');
        }
    }

    pageClassOn('pageshow', "libraryPage", onPageShow);

    if ($($.mobile.activePage)[0].classList.contains('libraryPage')) {
        onPageShow.call($($.mobile.activePage)[0]);
    }

    function playSound(path, volume) {

        require(['howler'], function (howler) {

            var sound = new Howl({
                urls: [path],
                volume: volume || .3
            });

            sound.play();
            lastSound = new Date().getTime();
        });
    }

})();