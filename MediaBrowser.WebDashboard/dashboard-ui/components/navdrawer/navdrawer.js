define(['browser', 'css!./navdrawer', 'scrollStyles'], function (browser) {

    return function (options) {

        var self,
            defaults,
            mask,
            maskHammer,
            menuHammer,
            newPos = 0,
            currentPos = 0,
            startPoint = 0,
            countStart = 0,
            velocity = 0.0;

        options.target.classList.add('transition');
        var draggingX;
        var draggingY;

        var scrollContainer = options.target.querySelector('.scrollContainer');
        scrollContainer.classList.add('smoothScrollY');

        var TouchMenuLA = function () {
            self = this;

            defaults = {
                width: 280,
                handleSize: 30,
                disableMask: false,
                maxMaskOpacity: 0.5
            };

            this.isVisible = false;

            this.initialize();
        };

        TouchMenuLA.prototype.initElements = function (Hammer) {
            options.target.classList.add('touch-menu-la');
            options.target.style.width = options.width + 'px';
            options.target.style.left = -options.width + 'px';

            if (!options.disableMask) {
                mask = document.createElement('div');
                mask.className = 'tmla-mask';
                document.body.appendChild(mask);

                if (Hammer) {
                    maskHammer = new Hammer(mask, null);
                }
            }
        };

        function onPanStart(ev) {
            options.target.classList.remove('transition');
            options.target.classList.add('open');
            velocity = Math.abs(ev.velocity);
        }

        function onPanMove(ev) {
            velocity = Math.abs(ev.velocity);
            // Depending on the deltas, choose X or Y

            var isOpen = self.visible;

            // If it's already open, then treat any right-swipe as vertical pan
            if (isOpen && !draggingX && ev.deltaX > 0) {
                draggingY = true;
            }

            if (!draggingX && !draggingY && (!isOpen || Math.abs(ev.deltaX) >= 10)) {
                draggingX = true;
                scrollContainer.addEventListener('scroll', disableEvent);
                self.showMask();

            } else if (!draggingY) {
                draggingY = true;
            }

            if (draggingX) {
                newPos = currentPos + ev.deltaX;
                self.changeMenuPos();
            }
        }

        function onPanEnd(ev) {
            options.target.classList.add('transition');
            scrollContainer.removeEventListener('scroll', disableEvent);
            draggingX = false;
            draggingY = false;
            currentPos = ev.deltaX;
            self.checkMenuState(ev.deltaX, ev.deltaY);
        }

        function initEdgeSwipe(Hammer) {
            if (options.disableEdgeSwipe) {
                return;
            }

            require(['hammer-main'], initEdgeSwipeInternal);
        }

        function initEdgeSwipeInternal(edgeHammer) {
            var isPeeking = false;

            edgeHammer.on('panstart panmove', function (ev) {

                if (isPeeking) {
                    onPanMove(ev);
                } else {
                    var srcEvent = ev.srcEvent;
                    var clientX = srcEvent.clientX;
                    if (!clientX) {
                        var touches = srcEvent.touches;
                        if (touches && touches.length) {
                            clientX = touches[0].clientX;
                        }
                    }
                    if (clientX <= options.handleSize) {
                        isPeeking = true;
                        onPanStart(ev);
                    }
                }
            });
            edgeHammer.on('panend pancancel', function (ev) {
                if (isPeeking) {
                    isPeeking = false;
                    onPanEnd(ev);
                }
            });

            self.edgeHammer = edgeHammer;
        }

        function disableEvent(e) {

            e.preventDefault();
            e.stopPropagation();
        }

        TouchMenuLA.prototype.touchStartMenu = function () {

            menuHammer.on('panstart', function (ev) {
                onPanStart(ev);
            });
            menuHammer.on('panmove', function (ev) {
                onPanMove(ev);
            });
        };

        TouchMenuLA.prototype.animateToPosition = function (pos) {

            requestAnimationFrame(function () {
                if (pos) {
                    options.target.style.transform = 'translate3d(' + pos + 'px, 0, 0)';
                } else {
                    options.target.style.transform = 'none';
                }
            });
        };

        TouchMenuLA.prototype.changeMenuPos = function () {
            if (newPos <= options.width) {
                this.animateToPosition(newPos);
            }
        };

        TouchMenuLA.prototype.touchEndMenu = function () {
            menuHammer.on('panend pancancel', onPanEnd);
        };

        TouchMenuLA.prototype.clickMaskClose = function () {
            mask.addEventListener('click', function () {
                self.close();
            });
        };

        TouchMenuLA.prototype.checkMenuState = function (deltaX, deltaY) {
            if (velocity >= 1.0) {
                if (deltaX >= -80 || Math.abs(deltaY) >= 70) {
                    self.open();
                } else {
                    self.close();
                }
            } else {
                if (newPos >= 100) {
                    self.open();
                } else {
                    self.close();
                }
            }
        };

        TouchMenuLA.prototype.open = function () {
            this.animateToPosition(options.width);

            currentPos = options.width;
            this.isVisible = true;
            options.target.classList.add('open');

            self.showMask();
            self.invoke(options.onChange);
        };

        TouchMenuLA.prototype.close = function () {
            this.animateToPosition(0);
            currentPos = 0;
            self.isVisible = false;
            options.target.classList.remove('open');

            self.hideMask();
            self.invoke(options.onChange);
        };

        TouchMenuLA.prototype.toggle = function () {
            if (self.isVisible) {
                self.close();
            } else {
                self.open();
            }
        };

        TouchMenuLA.prototype.eventStartMask = function () {
            maskHammer.on('panstart panmove', function (ev) {
                if (ev.center.x <= options.width && self.isVisible) {
                    countStart++;

                    if (countStart == 1) {
                        startPoint = ev.deltaX;
                    }

                    if (ev.deltaX < 0) {
                        draggingX = true;
                        newPos = (ev.deltaX - startPoint) + options.width;
                        self.changeMenuPos();
                        velocity = Math.abs(ev.velocity);
                    }
                }
            });
        };

        TouchMenuLA.prototype.eventEndMask = function () {
            maskHammer.on('panend pancancel', function (ev) {
                self.checkMenuState(ev.deltaX);
                countStart = 0;
            });
        };

        TouchMenuLA.prototype.showMask = function () {

            mask.classList.add('backdrop');
        };

        TouchMenuLA.prototype.hideMask = function () {

            mask.classList.remove('backdrop');
        };

        TouchMenuLA.prototype.invoke = function (fn) {
            if (fn) {
                fn.apply(self);
            }
        };

        function initWithHammer(Hammer) {
            
            if (Hammer) {
                menuHammer = Hammer(options.target, null);
            }

            self.initElements(Hammer);

            if (Hammer) {
                self.touchStartMenu();
                self.touchEndMenu();
                self.eventStartMask();
                self.eventEndMask();
                initEdgeSwipe(Hammer);
            }

            if (!options.disableMask) {
                self.clickMaskClose();
            }
        }

        TouchMenuLA.prototype.initialize = function () {

            options = Object.assign(defaults, options || {});

            // Not ready yet
            if (browser.edge) {
                options.disableEdgeSwipe = true;
            }

            if (browser.touch) {
                require(['hammer'], initWithHammer);
            } else {
                initWithHammer();
            }
        };

        return new TouchMenuLA();
    };
});