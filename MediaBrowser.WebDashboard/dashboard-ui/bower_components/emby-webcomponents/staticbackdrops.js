define([], function () {
    'use strict';

    function getStaticBackdrops() {
        var list = [];

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg1-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg2-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg3-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg4-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg5-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg6-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg7-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg8-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg9-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg10-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg11-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg12-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg13-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg14-1920.jpg',
                width: 1920
            }
        ]);

        list.push([
            {
                url: 'https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/images/wallpaper/bg15-1920.jpg',
                width: 1920
            }
        ]);

        return list;
    }

    function getRandomInt(min, max) {
        return Math.floor(Math.random() * (max - min + 1)) + min;
    }

    function getRandomImageUrl() {
        var images = getStaticBackdrops();
        var index = getRandomInt(0, images.length - 1);
        return images[index][0].url;
    }

    return {
        getStaticBackdrops: getStaticBackdrops,
        getRandomImageUrl: getRandomImageUrl
    };
});