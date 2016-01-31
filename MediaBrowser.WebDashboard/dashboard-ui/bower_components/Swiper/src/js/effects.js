/*=========================
  Effects
  ===========================*/
s.effects = {
    fade: {
        setTranslate: function () {
            for (var i = 0; i < s.slides.length; i++) {
                var slide = s.slides.eq(i);
                var offset = slide[0].swiperSlideOffset;
                var tx = -offset;
                if (!s.params.virtualTranslate) tx = tx - s.translate;
                var ty = 0;
                if (!s.isHorizontal()) {
                    ty = tx;
                    tx = 0;
                }
                var slideOpacity = s.params.fade.crossFade ?
                        Math.max(1 - Math.abs(slide[0].progress), 0) :
                        1 + Math.min(Math.max(slide[0].progress, -1), 0);
                slide
                    .css({
                        opacity: slideOpacity
                    })
                    .transform('translate3d(' + tx + 'px, ' + ty + 'px, 0px)');

            }

        },
        setTransition: function (duration) {
            s.slides.transition(duration);
            if (s.params.virtualTranslate && duration !== 0) {
                var eventTriggered = false;
                s.slides.transitionEnd(function () {
                    if (eventTriggered) return;
                    if (!s) return;
                    eventTriggered = true;
                    s.animating = false;
                    var triggerEvents = ['webkitTransitionEnd', 'transitionend', 'oTransitionEnd', 'MSTransitionEnd', 'msTransitionEnd'];
                    for (var i = 0; i < triggerEvents.length; i++) {
                        s.wrapper.trigger(triggerEvents[i]);
                    }
                });
            }
        }
    },
    flip: {
        setTranslate: function () {
            for (var i = 0; i < s.slides.length; i++) {
                var slide = s.slides.eq(i);
                var progress = slide[0].progress;
                if (s.params.flip.limitRotation) {
                    progress = Math.max(Math.min(slide[0].progress, 1), -1);
                }
                var offset = slide[0].swiperSlideOffset;
                var rotate = -180 * progress,
                    rotateY = rotate,
                    rotateX = 0,
                    tx = -offset,
                    ty = 0;
                if (!s.isHorizontal()) {
                    ty = tx;
                    tx = 0;
                    rotateX = -rotateY;
                    rotateY = 0;
                }
                else if (s.rtl) {
                    rotateY = -rotateY;
                }

                slide[0].style.zIndex = -Math.abs(Math.round(progress)) + s.slides.length;

                if (s.params.flip.slideShadows) {
                    //Set shadows
                    var shadowBefore = s.isHorizontal() ? slide.find('.swiper-slide-shadow-left') : slide.find('.swiper-slide-shadow-top');
                    var shadowAfter = s.isHorizontal() ? slide.find('.swiper-slide-shadow-right') : slide.find('.swiper-slide-shadow-bottom');
                    if (shadowBefore.length === 0) {
                        shadowBefore = $('<div class="swiper-slide-shadow-' + (s.isHorizontal() ? 'left' : 'top') + '"></div>');
                        slide.append(shadowBefore);
                    }
                    if (shadowAfter.length === 0) {
                        shadowAfter = $('<div class="swiper-slide-shadow-' + (s.isHorizontal() ? 'right' : 'bottom') + '"></div>');
                        slide.append(shadowAfter);
                    }
                    if (shadowBefore.length) shadowBefore[0].style.opacity = Math.max(-progress, 0);
                    if (shadowAfter.length) shadowAfter[0].style.opacity = Math.max(progress, 0);
                }

                slide
                    .transform('translate3d(' + tx + 'px, ' + ty + 'px, 0px) rotateX(' + rotateX + 'deg) rotateY(' + rotateY + 'deg)');
            }
        },
        setTransition: function (duration) {
            s.slides.transition(duration).find('.swiper-slide-shadow-top, .swiper-slide-shadow-right, .swiper-slide-shadow-bottom, .swiper-slide-shadow-left').transition(duration);
            if (s.params.virtualTranslate && duration !== 0) {
                var eventTriggered = false;
                s.slides.eq(s.activeIndex).transitionEnd(function () {
                    if (eventTriggered) return;
                    if (!s) return;
                    if (!$(this).hasClass(s.params.slideActiveClass)) return;
                    eventTriggered = true;
                    s.animating = false;
                    var triggerEvents = ['webkitTransitionEnd', 'transitionend', 'oTransitionEnd', 'MSTransitionEnd', 'msTransitionEnd'];
                    for (var i = 0; i < triggerEvents.length; i++) {
                        s.wrapper.trigger(triggerEvents[i]);
                    }
                });
            }
        }
    },
    cube: {
        setTranslate: function () {
            var wrapperRotate = 0, cubeShadow;
            if (s.params.cube.shadow) {
                if (s.isHorizontal()) {
                    cubeShadow = s.wrapper.find('.swiper-cube-shadow');
                    if (cubeShadow.length === 0) {
                        cubeShadow = $('<div class="swiper-cube-shadow"></div>');
                        s.wrapper.append(cubeShadow);
                    }
                    cubeShadow.css({height: s.width + 'px'});
                }
                else {
                    cubeShadow = s.container.find('.swiper-cube-shadow');
                    if (cubeShadow.length === 0) {
                        cubeShadow = $('<div class="swiper-cube-shadow"></div>');
                        s.container.append(cubeShadow);
                    }
                }
            }
            for (var i = 0; i < s.slides.length; i++) {
                var slide = s.slides.eq(i);
                var slideAngle = i * 90;
                var round = Math.floor(slideAngle / 360);
                if (s.rtl) {
                    slideAngle = -slideAngle;
                    round = Math.floor(-slideAngle / 360);
                }
                var progress = Math.max(Math.min(slide[0].progress, 1), -1);
                var tx = 0, ty = 0, tz = 0;
                if (i % 4 === 0) {
                    tx = - round * 4 * s.size;
                    tz = 0;
                }
                else if ((i - 1) % 4 === 0) {
                    tx = 0;
                    tz = - round * 4 * s.size;
                }
                else if ((i - 2) % 4 === 0) {
                    tx = s.size + round * 4 * s.size;
                    tz = s.size;
                }
                else if ((i - 3) % 4 === 0) {
                    tx = - s.size;
                    tz = 3 * s.size + s.size * 4 * round;
                }
                if (s.rtl) {
                    tx = -tx;
                }

                if (!s.isHorizontal()) {
                    ty = tx;
                    tx = 0;
                }

                var transform = 'rotateX(' + (s.isHorizontal() ? 0 : -slideAngle) + 'deg) rotateY(' + (s.isHorizontal() ? slideAngle : 0) + 'deg) translate3d(' + tx + 'px, ' + ty + 'px, ' + tz + 'px)';
                if (progress <= 1 && progress > -1) {
                    wrapperRotate = i * 90 + progress * 90;
                    if (s.rtl) wrapperRotate = -i * 90 - progress * 90;
                }
                slide.transform(transform);
                if (s.params.cube.slideShadows) {
                    //Set shadows
                    var shadowBefore = s.isHorizontal() ? slide.find('.swiper-slide-shadow-left') : slide.find('.swiper-slide-shadow-top');
                    var shadowAfter = s.isHorizontal() ? slide.find('.swiper-slide-shadow-right') : slide.find('.swiper-slide-shadow-bottom');
                    if (shadowBefore.length === 0) {
                        shadowBefore = $('<div class="swiper-slide-shadow-' + (s.isHorizontal() ? 'left' : 'top') + '"></div>');
                        slide.append(shadowBefore);
                    }
                    if (shadowAfter.length === 0) {
                        shadowAfter = $('<div class="swiper-slide-shadow-' + (s.isHorizontal() ? 'right' : 'bottom') + '"></div>');
                        slide.append(shadowAfter);
                    }
                    if (shadowBefore.length) shadowBefore[0].style.opacity = Math.max(-progress, 0);
                    if (shadowAfter.length) shadowAfter[0].style.opacity = Math.max(progress, 0);
                }
            }
            s.wrapper.css({
                '-webkit-transform-origin': '50% 50% -' + (s.size / 2) + 'px',
                '-moz-transform-origin': '50% 50% -' + (s.size / 2) + 'px',
                '-ms-transform-origin': '50% 50% -' + (s.size / 2) + 'px',
                'transform-origin': '50% 50% -' + (s.size / 2) + 'px'
            });

            if (s.params.cube.shadow) {
                if (s.isHorizontal()) {
                    cubeShadow.transform('translate3d(0px, ' + (s.width / 2 + s.params.cube.shadowOffset) + 'px, ' + (-s.width / 2) + 'px) rotateX(90deg) rotateZ(0deg) scale(' + (s.params.cube.shadowScale) + ')');
                }
                else {
                    var shadowAngle = Math.abs(wrapperRotate) - Math.floor(Math.abs(wrapperRotate) / 90) * 90;
                    var multiplier = 1.5 - (Math.sin(shadowAngle * 2 * Math.PI / 360) / 2 + Math.cos(shadowAngle * 2 * Math.PI / 360) / 2);
                    var scale1 = s.params.cube.shadowScale,
                        scale2 = s.params.cube.shadowScale / multiplier,
                        offset = s.params.cube.shadowOffset;
                    cubeShadow.transform('scale3d(' + scale1 + ', 1, ' + scale2 + ') translate3d(0px, ' + (s.height / 2 + offset) + 'px, ' + (-s.height / 2 / scale2) + 'px) rotateX(-90deg)');
                }
            }
            var zFactor = (s.isSafari || s.isUiWebView) ? (-s.size / 2) : 0;
            s.wrapper.transform('translate3d(0px,0,' + zFactor + 'px) rotateX(' + (s.isHorizontal() ? 0 : wrapperRotate) + 'deg) rotateY(' + (s.isHorizontal() ? -wrapperRotate : 0) + 'deg)');
        },
        setTransition: function (duration) {
            s.slides.transition(duration).find('.swiper-slide-shadow-top, .swiper-slide-shadow-right, .swiper-slide-shadow-bottom, .swiper-slide-shadow-left').transition(duration);
            if (s.params.cube.shadow && !s.isHorizontal()) {
                s.container.find('.swiper-cube-shadow').transition(duration);
            }
        }
    },
    coverflow: {
        setTranslate: function () {
            var transform = s.translate;
            var center = s.isHorizontal() ? -transform + s.width / 2 : -transform + s.height / 2;
            var rotate = s.isHorizontal() ? s.params.coverflow.rotate: -s.params.coverflow.rotate;
            var translate = s.params.coverflow.depth;
            //Each slide offset from center
            for (var i = 0, length = s.slides.length; i < length; i++) {
                var slide = s.slides.eq(i);
                var slideSize = s.slidesSizesGrid[i];
                var slideOffset = slide[0].swiperSlideOffset;
                var offsetMultiplier = (center - slideOffset - slideSize / 2) / slideSize * s.params.coverflow.modifier;

                var rotateY = s.isHorizontal() ? rotate * offsetMultiplier : 0;
                var rotateX = s.isHorizontal() ? 0 : rotate * offsetMultiplier;
                // var rotateZ = 0
                var translateZ = -translate * Math.abs(offsetMultiplier);

                var translateY = s.isHorizontal() ? 0 : s.params.coverflow.stretch * (offsetMultiplier);
                var translateX = s.isHorizontal() ? s.params.coverflow.stretch * (offsetMultiplier) : 0;

                //Fix for ultra small values
                if (Math.abs(translateX) < 0.001) translateX = 0;
                if (Math.abs(translateY) < 0.001) translateY = 0;
                if (Math.abs(translateZ) < 0.001) translateZ = 0;
                if (Math.abs(rotateY) < 0.001) rotateY = 0;
                if (Math.abs(rotateX) < 0.001) rotateX = 0;

                var slideTransform = 'translate3d(' + translateX + 'px,' + translateY + 'px,' + translateZ + 'px)  rotateX(' + rotateX + 'deg) rotateY(' + rotateY + 'deg)';

                slide.transform(slideTransform);
                slide[0].style.zIndex = -Math.abs(Math.round(offsetMultiplier)) + 1;
                if (s.params.coverflow.slideShadows) {
                    //Set shadows
                    var shadowBefore = s.isHorizontal() ? slide.find('.swiper-slide-shadow-left') : slide.find('.swiper-slide-shadow-top');
                    var shadowAfter = s.isHorizontal() ? slide.find('.swiper-slide-shadow-right') : slide.find('.swiper-slide-shadow-bottom');
                    if (shadowBefore.length === 0) {
                        shadowBefore = $('<div class="swiper-slide-shadow-' + (s.isHorizontal() ? 'left' : 'top') + '"></div>');
                        slide.append(shadowBefore);
                    }
                    if (shadowAfter.length === 0) {
                        shadowAfter = $('<div class="swiper-slide-shadow-' + (s.isHorizontal() ? 'right' : 'bottom') + '"></div>');
                        slide.append(shadowAfter);
                    }
                    if (shadowBefore.length) shadowBefore[0].style.opacity = offsetMultiplier > 0 ? offsetMultiplier : 0;
                    if (shadowAfter.length) shadowAfter[0].style.opacity = (-offsetMultiplier) > 0 ? -offsetMultiplier : 0;
                }
            }

            //Set correct perspective for IE10
            if (s.browser.ie) {
                var ws = s.wrapper[0].style;
                ws.perspectiveOrigin = center + 'px 50%';
            }
        },
        setTransition: function (duration) {
            s.slides.transition(duration).find('.swiper-slide-shadow-top, .swiper-slide-shadow-right, .swiper-slide-shadow-bottom, .swiper-slide-shadow-left').transition(duration);
        }
    }
};