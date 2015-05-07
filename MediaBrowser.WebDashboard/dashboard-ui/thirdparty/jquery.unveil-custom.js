/**
 * jQuery Unveil
 * A very lightweight jQuery plugin to lazy load images
 * http://luis-almeida.github.com/unveil
 *
 * Licensed under the MIT license.
 * Copyright 2013 Luís Almeida
 * https://github.com/luis-almeida
 */

; (function ($) {

    $.fn.unveil = function (threshold, callback) {

        var $w = $(window),
            th = threshold || 0,
            attrib = "data-src",
            images = this,
            loaded;

        this.one("unveil", function () {
            var elem = this;
            var elemType = elem.tagName;
            var source = elem.getAttribute(attrib);
            if (source) {
                if (elemType === "DIV") {

                    elem.style.backgroundImage = "url('" + source + "')";

                } else {
                    elem.setAttribute("src", source);
                }
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
        }

        $w.on('scroll.unveil', unveil);
        $w.on('resize.unveil', unveil);

        unveil();

        return this;

    };

    $.fn.lazyChildren = function () {

        $(".lazy", this).unveil(0);
        return this;
    };

})(window.jQuery || window.Zepto);