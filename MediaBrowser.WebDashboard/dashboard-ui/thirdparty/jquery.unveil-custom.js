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
            return 8000;
        }

        var screens = $.browser.mobile ? 2 : 1;

        // This helps eliminate the draw-in effect as you scroll
        return Math.max(screen.availHeight * screens, 1000);
    }

    $.fn.unveil = function () {

        var $w = $(window),
            th = getThreshold(),
            attrib = "data-src",
            images = this,
            loaded;

        unveilId++;
        var eventNamespace = 'unveil' + unveilId;

        this.one("unveil", function () {
            var elem = this;
            var source = elem.getAttribute(attrib);
            if (source) {
                ImageStore.setImageInto(elem, source);
                elem.setAttribute("data-src", '');
            }
        });

        function unveil() {
            var inview = images.filter(function () {
                var $e = $(this);

                if ($e.is(":hidden")) return;
                var wt = $w.scrollTop(),
                    wb = wt + $w.height(),
                    et = $e.offset().top,
                    eb = et + $e.height();

                return eb >= wt - th && et <= wb + th;
            });

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

    $.fn.lazyChildren = function () {

        var lazyItems = $(".lazy", this);

        if (lazyItems.length) {
            lazyItems.unveil();
        }

        return this;
    };

    $.fn.lazyImage = function (url) {

        return this.attr('data-src', url).unveil();
    };

})(window.jQuery || window.Zepto);

(function () {

    function setImageIntoElement(elem, url) {

        if (elem.tagName === "DIV") {

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