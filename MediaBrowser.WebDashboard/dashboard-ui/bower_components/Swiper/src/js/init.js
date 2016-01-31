/*=========================
  Init/Destroy
  ===========================*/
s.init = function () {
    if (s.params.loop) s.createLoop();
    s.updateContainerSize();
    s.updateSlidesSize();
    s.updatePagination();
    if (s.params.scrollbar && s.scrollbar) {
        s.scrollbar.set();
        if (s.params.scrollbarDraggable) {
            s.scrollbar.enableDraggable();
        }
    }
    if (s.params.effect !== 'slide' && s.effects[s.params.effect]) {
        if (!s.params.loop) s.updateProgress();
        s.effects[s.params.effect].setTranslate();
    }
    if (s.params.loop) {
        s.slideTo(s.params.initialSlide + s.loopedSlides, 0, s.params.runCallbacksOnInit);
    }
    else {
        s.slideTo(s.params.initialSlide, 0, s.params.runCallbacksOnInit);
        if (s.params.initialSlide === 0) {
            if (s.parallax && s.params.parallax) s.parallax.setTranslate();
            if (s.lazy && s.params.lazyLoading) {
                s.lazy.load();
                s.lazy.initialImageLoaded = true;
            }
        }
    }
    s.attachEvents();
    if (s.params.observer && s.support.observer) {
        s.initObservers();
    }
    if (s.params.preloadImages && !s.params.lazyLoading) {
        s.preloadImages();
    }
    if (s.params.autoplay) {
        s.startAutoplay();
    }
    if (s.params.keyboardControl) {
        if (s.enableKeyboardControl) s.enableKeyboardControl();
    }
    if (s.params.mousewheelControl) {
        if (s.enableMousewheelControl) s.enableMousewheelControl();
    }
    if (s.params.hashnav) {
        if (s.hashnav) s.hashnav.init();
    }
    if (s.params.a11y && s.a11y) s.a11y.init();
    s.emit('onInit', s);
};

// Cleanup dynamic styles
s.cleanupStyles = function () {
    // Container
    s.container.removeClass(s.classNames.join(' ')).removeAttr('style');

    // Wrapper
    s.wrapper.removeAttr('style');

    // Slides
    if (s.slides && s.slides.length) {
        s.slides
            .removeClass([
              s.params.slideVisibleClass,
              s.params.slideActiveClass,
              s.params.slideNextClass,
              s.params.slidePrevClass
            ].join(' '))
            .removeAttr('style')
            .removeAttr('data-swiper-column')
            .removeAttr('data-swiper-row');
    }

    // Pagination/Bullets
    if (s.paginationContainer && s.paginationContainer.length) {
        s.paginationContainer.removeClass(s.params.paginationHiddenClass);
    }
    if (s.bullets && s.bullets.length) {
        s.bullets.removeClass(s.params.bulletActiveClass);
    }

    // Buttons
    if (s.params.prevButton) $(s.params.prevButton).removeClass(s.params.buttonDisabledClass);
    if (s.params.nextButton) $(s.params.nextButton).removeClass(s.params.buttonDisabledClass);

    // Scrollbar
    if (s.params.scrollbar && s.scrollbar) {
        if (s.scrollbar.track && s.scrollbar.track.length) s.scrollbar.track.removeAttr('style');
        if (s.scrollbar.drag && s.scrollbar.drag.length) s.scrollbar.drag.removeAttr('style');
    }
};

// Destroy
s.destroy = function (deleteInstance, cleanupStyles) {
    // Detach evebts
    s.detachEvents();
    // Stop autoplay
    s.stopAutoplay();
    // Disable draggable
    if (s.params.scrollbar && s.scrollbar) {
        if (s.params.scrollbarDraggable) {
            s.scrollbar.disableDraggable();
        }
    }
    // Destroy loop
    if (s.params.loop) {
        s.destroyLoop();
    }
    // Cleanup styles
    if (cleanupStyles) {
        s.cleanupStyles();
    }
    // Disconnect observer
    s.disconnectObservers();
    // Disable keyboard/mousewheel
    if (s.params.keyboardControl) {
        if (s.disableKeyboardControl) s.disableKeyboardControl();
    }
    if (s.params.mousewheelControl) {
        if (s.disableMousewheelControl) s.disableMousewheelControl();
    }
    // Disable a11y
    if (s.params.a11y && s.a11y) s.a11y.destroy();
    // Destroy callback
    s.emit('onDestroy');
    // Delete instance
    if (deleteInstance !== false) s = null;
};

s.init();
