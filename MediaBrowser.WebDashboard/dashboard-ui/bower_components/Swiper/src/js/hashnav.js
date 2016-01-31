/*=========================
  Hash Navigation
  ===========================*/
s.hashnav = {
    init: function () {
        if (!s.params.hashnav) return;
        s.hashnav.initialized = true;
        var hash = document.location.hash.replace('#', '');
        if (!hash) return;
        var speed = 0;
        for (var i = 0, length = s.slides.length; i < length; i++) {
            var slide = s.slides.eq(i);
            var slideHash = slide.attr('data-hash');
            if (slideHash === hash && !slide.hasClass(s.params.slideDuplicateClass)) {
                var index = slide.index();
                s.slideTo(index, speed, s.params.runCallbacksOnInit, true);
            }
        }
    },
    setHash: function () {
        if (!s.hashnav.initialized || !s.params.hashnav) return;
        document.location.hash = s.slides.eq(s.activeIndex).attr('data-hash') || '';
    }
};