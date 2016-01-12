define(['visibleinviewport'], function (visibleInViewport) {

    var wheelEvent = (document.implementation.hasFeature('Event.wheel', '3.0') ? 'wheel' : 'mousewheel');
    var thresholdX = screen.availWidth;
    var thresholdY = screen.availHeight;

    function isVisible(elem) {
        return visibleInViewport(elem, true, thresholdX, thresholdY);
    }

    function fillImage(elem) {
        var source = elem.getAttribute('data-src');
        if (source) {
            ImageStore.setImageInto(elem, source);
            elem.setAttribute("data-src", '');
        }
    }

    function unveilElements(elems) {

        if (!elems.length) {
            return;
        }

        var images = elems;

        function unveil() {

            var remaining = [];

            for (var i = 0, length = images.length; i < length; i++) {
                var img = images[i];
                if (isVisible(img)) {
                    fillImage(img);
                } else {
                    remaining.push(img);
                }
            }

            images = remaining;

            if (!images.length) {
                document.removeEventListener('scroll', unveil, true);
                document.removeEventListener(wheelEvent, unveil, true);
                window.removeEventListener('resize', unveil, true);
            }
        }

        document.addEventListener('scroll', unveil, true);
        document.addEventListener(wheelEvent, unveil, true);
        window.addEventListener('resize', unveil, true);

        unveil();
    }

    function fillImages(elems) {

        for (var i = 0, length = elems.length; i < length; i++) {
            var elem = elems[0];
            var source = elem.getAttribute('data-src');
            if (source) {
                ImageStore.setImageInto(elem, source);
                elem.setAttribute("data-src", '');
            }
        }
    }

    function lazyChildren(elem) {

        unveilElements(elem.getElementsByClassName('lazy'), elem);
    }

    function lazyImage(elem, url) {

        elem.setAttribute('data-src', url);
        fillImages([elem]);
    }

    function setImageIntoElement(elem, url) {

        if (elem.tagName !== "IMG") {

            elem.style.backgroundImage = "url('" + url + "')";

        } else {
            elem.setAttribute("src", url);
        }

        if (browserInfo.animate && !browserInfo.mobile) {
            if (!elem.classList.contains('noFade')) {
                fadeIn(elem, 1);
            }
        }
    }

    function fadeIn(elem, iterations) {

        var keyframes = [
          { opacity: '0', offset: 0 },
          { opacity: '1', offset: 1 }];
        var timing = { duration: 200, iterations: iterations };
        return elem.animate(keyframes, timing);
    }

    function simpleImageStore() {

        var self = this;

        self.setImageInto = setImageIntoElement;
    }

    window.ImageStore = new simpleImageStore();

    window.ImageLoader = {
        fillImages: fillImages,
        lazyImage: lazyImage,
        lazyChildren: lazyChildren
    };

});