(function ($) {

    /**
     * Copyright 2012, Digital Fusion
     * Licensed under the MIT license.
     * http://teamdf.com/jquery-plugins/license/
     *
     * @author Sam Sehnert
     * @desc A small plugin that checks whether elements are within
     *       the user visible viewport of a web browser.
     *       only accounts for vertical position, not horizontal.
     */
    var $w = $(window);
    $.fn.visibleInViewport = function (partial, hidden, direction, threshold) {

        if (this.length < 1)
            return;

        var $t = this.length > 1 ? this.eq(0) : this,
            t = $t.get(0),
            vpWidth = $w.width(),
            vpHeight = $w.height(),
            direction = (direction) ? direction : 'both',
            clientSize = hidden === true ? t.offsetWidth * t.offsetHeight : true;

        if (typeof t.getBoundingClientRect === 'function') {

            // Use this native browser method, if available.
            var rec = t.getBoundingClientRect(),
                tViz = rec.top >= 0 && rec.top < vpHeight + threshold,
                bViz = rec.bottom > 0 && rec.bottom <= vpHeight + threshold,
                lViz = rec.left >= 0 && rec.left < vpWidth + threshold,
                rViz = rec.right > 0 && rec.right <= vpWidth + threshold,
                vVisible = partial ? tViz || bViz : tViz && bViz,
                hVisible = partial ? lViz || rViz : lViz && rViz;

            if (direction === 'both')
                return clientSize && vVisible && hVisible;
            else if (direction === 'vertical')
                return clientSize && vVisible;
            else if (direction === 'horizontal')
                return clientSize && hVisible;
        } else {

            var viewTop = $w.scrollTop(),
                viewBottom = viewTop + vpHeight,
                viewLeft = $w.scrollLeft(),
                viewRight = viewLeft + vpWidth,
                offset = $t.offset(),
                _top = offset.top,
                _bottom = _top + $t.height(),
                _left = offset.left,
                _right = _left + $t.width(),
                compareTop = partial === true ? _bottom : _top,
                compareBottom = partial === true ? _top : _bottom,
                compareLeft = partial === true ? _right : _left,
                compareRight = partial === true ? _left : _right;

            if (direction === 'both')
                return !!clientSize && ((compareBottom <= viewBottom) && (compareTop >= viewTop)) && ((compareRight <= viewRight) && (compareLeft >= viewLeft));
            else if (direction === 'vertical')
                return !!clientSize && ((compareBottom <= viewBottom) && (compareTop >= viewTop));
            else if (direction === 'horizontal')
                return !!clientSize && ((compareRight <= viewRight) && (compareLeft >= viewLeft));
        }
    };

})(jQuery);

/**
 * jQuery Unveil
 * A very lightweight jQuery plugin to lazy load images
 * http://luis-almeida.github.com/unveil
 *
 * Licensed under the MIT license.
 * Copyright 2013 Luís Almeida
 * https://github.com/luis-almeida
 */

(function ($) {

    var unveilId = 0;

    function getThreshold() {

        // If less than 100, the search window ends up not getting images
        // If less than 200, this happens on the home page
        // Need to fix those before this can be set to 0

        if (window.AppInfo && AppInfo.isNativeApp && $.browser.safari) {
            return 7000;
        }

        var screens = $.browser.mobile ? 2.5 : 1;

        // This helps eliminate the draw-in effect as you scroll
        return Math.max(screen.availHeight * screens, 1000);
    }

    var threshold = getThreshold();

    function isVisible() {
        return $(this).visibleInViewport(true, false, 'both', threshold);
    }

    function fillImage() {
        var elem = this;
        var source = elem.getAttribute('data-src');
        if (source) {
            ImageStore.setImageInto(elem, source);
            elem.setAttribute("data-src", '');
        }
    }

    $.fn.unveil = function () {

        var $w = $(window),
            images = this,
            loaded;

        unveilId++;
        var eventNamespace = 'unveil' + unveilId;

        this.one("unveil", fillImage);

        function unveil() {
            var inview = images.filter(isVisible);

            loaded = inview.trigger("unveil");
            images = images.not(loaded);

            if (!images.length) {
                $w.off('scroll.' + eventNamespace);
                $w.off('resize.' + eventNamespace);
            }
        }

        $w.on('scroll.' + eventNamespace, unveil);
        $w.on('resize.' + eventNamespace, unveil);

        unveil();

        return this;

    };

    $.fn.fillImages = function () {

        return this.each(fillImage);
    };

    $.fn.lazyChildren = function () {

        var lazyItems = $(".lazy", this);

        if (lazyItems.length) {
            lazyItems.unveil();
        }

        return this;
    };

    $.fn.lazyImage = function (url) {

        return this.attr('data-src', url).fillImages();
    };

})(window.jQuery || window.Zepto);

(function () {

    function setImageIntoElement(elem, url) {

        if (elem.tagName !== "IMG") {

            elem.style.backgroundImage = "url('" + url + "')";

        } else {
            elem.setAttribute("src", url);
        }
    }

    function simpleImageStore() {

        var self = this;

        self.setImageInto = setImageIntoElement;
    }

    console.log('creating simpleImageStore');
    window.ImageStore = new simpleImageStore();

})();