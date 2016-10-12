define(['dom'], function (dom) {
    'use strict';

    function loadImage(elem, url) {

        if (elem.tagName !== "IMG") {

            elem.style.backgroundImage = "url('" + url + "')";
            return Promise.resolve();

        }
        return loadImageIntoImg(elem, url);
    }

    function loadImageIntoImg(elem, url) {
        return new Promise(function (resolve, reject) {

            dom.addEventListener(elem, 'load', resolve, {
                once: true
            });
            elem.setAttribute("src", url);
        });
    }

    return {
        loadImage: loadImage
    };

});