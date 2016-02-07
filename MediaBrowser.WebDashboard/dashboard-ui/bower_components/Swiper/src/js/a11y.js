// Accessibility tools
s.a11y = {
    makeFocusable: function ($el) {
        $el.attr('tabIndex', '0');
        return $el;
    },
    addRole: function ($el, role) {
        $el.attr('role', role);
        return $el;
    },

    addLabel: function ($el, label) {
        $el.attr('aria-label', label);
        return $el;
    },

    disable: function ($el) {
        $el.attr('aria-disabled', true);
        return $el;
    },

    enable: function ($el) {
        $el.attr('aria-disabled', false);
        return $el;
    },

    onEnterKey: function (event) {
        if (event.keyCode !== 13) return;
        if ($(event.target).is(s.params.nextButton)) {
            s.onClickNext(event);
            if (s.isEnd) {
                s.a11y.notify(s.params.lastSlideMessage);
            }
            else {
                s.a11y.notify(s.params.nextSlideMessage);
            }
        }
        else if ($(event.target).is(s.params.prevButton)) {
            s.onClickPrev(event);
            if (s.isBeginning) {
                s.a11y.notify(s.params.firstSlideMessage);
            }
            else {
                s.a11y.notify(s.params.prevSlideMessage);
            }
        }
        if ($(event.target).is('.' + s.params.bulletClass)) {
            $(event.target)[0].click();
        }
    },

    liveRegion: $('<span class="swiper-notification" aria-live="assertive" aria-atomic="true"></span>'),

    notify: function (message) {
        var notification = s.a11y.liveRegion;
        if (notification.length === 0) return;
        notification.html('');
        notification.html(message);
    },
    init: function () {
        // Setup accessibility
        if (s.params.nextButton && s.nextButton && s.nextButton.length > 0) {
            s.a11y.makeFocusable(s.nextButton);
            s.a11y.addRole(s.nextButton, 'button');
            s.a11y.addLabel(s.nextButton, s.params.nextSlideMessage);
        }
        if (s.params.prevButton && s.prevButton && s.prevButton.length > 0) {
            s.a11y.makeFocusable(s.prevButton);
            s.a11y.addRole(s.prevButton, 'button');
            s.a11y.addLabel(s.prevButton, s.params.prevSlideMessage);
        }

        $(s.container).append(s.a11y.liveRegion);
    },
    initPagination: function () {
        if (s.params.pagination && s.params.paginationClickable && s.bullets && s.bullets.length) {
            s.bullets.each(function () {
                var bullet = $(this);
                s.a11y.makeFocusable(bullet);
                s.a11y.addRole(bullet, 'button');
                s.a11y.addLabel(bullet, s.params.paginationBulletMessage.replace(/{{index}}/, bullet.index() + 1));
            });
        }
    },
    destroy: function () {
        if (s.a11y.liveRegion && s.a11y.liveRegion.length > 0) s.a11y.liveRegion.remove();
    }
};
