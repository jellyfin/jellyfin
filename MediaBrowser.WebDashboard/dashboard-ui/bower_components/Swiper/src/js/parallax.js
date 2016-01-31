/*=========================
  Parallax
  ===========================*/
function setParallaxTransform(el, progress) {
    el = $(el);
    var p, pX, pY;
    var rtlFactor = s.rtl ? -1 : 1;

    p = el.attr('data-swiper-parallax') || '0';
    pX = el.attr('data-swiper-parallax-x');
    pY = el.attr('data-swiper-parallax-y');
    if (pX || pY) {
        pX = pX || '0';
        pY = pY || '0';
    }
    else {
        if (s.isHorizontal()) {
            pX = p;
            pY = '0';
        }
        else {
            pY = p;
            pX = '0';
        }
    }

    if ((pX).indexOf('%') >= 0) {
        pX = parseInt(pX, 10) * progress * rtlFactor + '%';
    }
    else {
        pX = pX * progress * rtlFactor + 'px' ;
    }
    if ((pY).indexOf('%') >= 0) {
        pY = parseInt(pY, 10) * progress + '%';
    }
    else {
        pY = pY * progress + 'px' ;
    }

    el.transform('translate3d(' + pX + ', ' + pY + ',0px)');
}
s.parallax = {
    setTranslate: function () {
        s.container.children('[data-swiper-parallax], [data-swiper-parallax-x], [data-swiper-parallax-y]').each(function(){
            setParallaxTransform(this, s.progress);

        });
        s.slides.each(function () {
            var slide = $(this);
            slide.find('[data-swiper-parallax], [data-swiper-parallax-x], [data-swiper-parallax-y]').each(function () {
                var progress = Math.min(Math.max(slide[0].progress, -1), 1);
                setParallaxTransform(this, progress);
            });
        });
    },
    setTransition: function (duration) {
        if (typeof duration === 'undefined') duration = s.params.speed;
        s.container.find('[data-swiper-parallax], [data-swiper-parallax-x], [data-swiper-parallax-y]').each(function(){
            var el = $(this);
            var parallaxDuration = parseInt(el.attr('data-swiper-parallax-duration'), 10) || duration;
            if (duration === 0) parallaxDuration = 0;
            el.transition(parallaxDuration);
        });
    }
};
