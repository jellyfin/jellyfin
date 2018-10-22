define(["dialogHelper", "inputManager", "connectionManager", "layoutManager", "focusManager", "browser", "apphost", "loading", "css!./style", "material-icons", "paper-icon-button-light"], function(dialogHelper, inputmanager, connectionManager, layoutManager, focusManager, browser, appHost, loading) {
    "use strict";

    function getImageUrl(item, options, apiClient) {
        return options = options || {}, options.type = options.type || "Primary", "string" == typeof item ? apiClient.getScaledImageUrl(item, options) : item.ImageTags && item.ImageTags[options.type] ? (options.tag = item.ImageTags[options.type], apiClient.getScaledImageUrl(item.Id, options)) : "Primary" === options.type && item.AlbumId && item.AlbumPrimaryImageTag ? (options.tag = item.AlbumPrimaryImageTag, apiClient.getScaledImageUrl(item.AlbumId, options)) : null
    }

    function getBackdropImageUrl(item, options, apiClient) {
        return options = options || {}, options.type = options.type || "Backdrop", options.maxWidth || options.width || options.maxHeight || options.height || (options.quality = 100), item.BackdropImageTags && item.BackdropImageTags.length ? (options.tag = item.BackdropImageTags[0], apiClient.getScaledImageUrl(item.Id, options)) : null
    }

    function getImgUrl(item, original) {
        var apiClient = connectionManager.getApiClient(item.ServerId),
            imageOptions = {};
        return original || (imageOptions.maxWidth = screen.availWidth), item.BackdropImageTags && item.BackdropImageTags.length ? getBackdropImageUrl(item, imageOptions, apiClient) : "Photo" === item.MediaType && original ? apiClient.getItemDownloadUrl(item.Id) : (imageOptions.type = "Primary", getImageUrl(item, imageOptions, apiClient))
    }

    function getIcon(icon, cssClass, canFocus, autoFocus) {
        var tabIndex = canFocus ? "" : ' tabindex="-1"';
        return autoFocus = autoFocus ? " autofocus" : "", '<button is="paper-icon-button-light" class="autoSize ' + cssClass + '"' + tabIndex + autoFocus + '><i class="md-icon slideshowButtonIcon">' + icon + "</i></button>"
    }

    function setUserScalable(scalable) {
        try {
            appHost.setUserScalable(scalable)
        } catch (err) {
            console.log("error in appHost.setUserScalable: " + err)
        }
    }
    return function(options) {
        function createElements(options) {
            dlg = dialogHelper.createDialog({
                exitAnimationDuration: options.interactive ? 400 : 800,
                size: "fullscreen",
                autoFocus: !1,
                scrollY: !1,
                exitAnimation: "fadeout",
                removeOnClose: !0
            }), dlg.classList.add("slideshowDialog");
            var html = "";
            if (options.interactive) {
                var actionButtonsOnTop = layoutManager.mobile;
                html += "<div>", html += '<div class="slideshowSwiperContainer"><div class="swiper-wrapper"></div></div>', html += getIcon("keyboard_arrow_left", "btnSlideshowPrevious slideshowButton hide-mouse-idle-tv", !1), html += getIcon("keyboard_arrow_right", "btnSlideshowNext slideshowButton hide-mouse-idle-tv", !1), html += '<div class="topActionButtons">', actionButtonsOnTop && (appHost.supports("filedownload") && (html += getIcon("file_download", "btnDownload slideshowButton", !0)), appHost.supports("sharing") && (html += getIcon("share", "btnShare slideshowButton", !0))), html += getIcon("close", "slideshowButton btnSlideshowExit hide-mouse-idle-tv", !1), html += "</div>", actionButtonsOnTop || (html += '<div class="slideshowBottomBar hide">', html += getIcon("pause", "btnSlideshowPause slideshowButton", !0, !0), appHost.supports("filedownload") && (html += getIcon("file_download", "btnDownload slideshowButton", !0)), appHost.supports("sharing") && (html += getIcon("share", "btnShare slideshowButton", !0)), html += "</div>"), html += "</div>"
            } else html += '<div class="slideshowImage"></div><h1 class="slideshowImageText"></h1>';
            if (dlg.innerHTML = html, options.interactive) {
                dlg.querySelector(".btnSlideshowExit").addEventListener("click", function(e) {
                    dialogHelper.close(dlg)
                }), dlg.querySelector(".btnSlideshowNext").addEventListener("click", nextImage), dlg.querySelector(".btnSlideshowPrevious").addEventListener("click", previousImage);
                var btnPause = dlg.querySelector(".btnSlideshowPause");
                btnPause && btnPause.addEventListener("click", playPause);
                var btnDownload = dlg.querySelector(".btnDownload");
                btnDownload && btnDownload.addEventListener("click", download);
                var btnShare = dlg.querySelector(".btnShare");
                btnShare && btnShare.addEventListener("click", share)
            }
            setUserScalable(!0), dialogHelper.open(dlg).then(function() {
                setUserScalable(!1), stopInterval()
            }), inputmanager.on(window, onInputCommand), document.addEventListener(window.PointerEvent ? "pointermove" : "mousemove", onPointerMove), dlg.addEventListener("close", onDialogClosed), options.interactive && loadSwiper(dlg)
        }

        function loadSwiper(dlg) {
            currentOptions.slides ? dlg.querySelector(".swiper-wrapper").innerHTML = currentOptions.slides.map(getSwiperSlideHtmlFromSlide).join("") : dlg.querySelector(".swiper-wrapper").innerHTML = currentOptions.items.map(getSwiperSlideHtmlFromItem).join(""), require(["swiper"], function(swiper) {
                swiperInstance = new Swiper(dlg.querySelector(".slideshowSwiperContainer"), {
                    direction: "horizontal",
                    loop: !1 !== options.loop,
                    autoplay: options.interval || 8e3,
                    preloadImages: !1,
                    lazyLoading: !0,
                    lazyLoadingInPrevNext: !0,
                    autoplayDisableOnInteraction: !1,
                    initialSlide: options.startIndex || 0,
                    speed: 240
                }), layoutManager.mobile ? pause() : play()
            })
        }

        function getSwiperSlideHtmlFromItem(item) {
            return getSwiperSlideHtmlFromSlide({
                imageUrl: getImgUrl(item),
                originalImage: getImgUrl(item, !0),
                Id: item.Id,
                ServerId: item.ServerId
            })
        }

        function getSwiperSlideHtmlFromSlide(item) {
            var html = "";
            return html += '<div class="swiper-slide" data-imageurl="' + item.imageUrl + '" data-original="' + item.originalImage + '" data-itemid="' + item.Id + '" data-serverid="' + item.ServerId + '">', html += '<img data-src="' + item.imageUrl + '" class="swiper-lazy swiper-slide-img">', (item.title || item.subtitle) && (html += '<div class="slideText">', html += '<div class="slideTextInner">', item.title && (html += '<h1 class="slideTitle">', html += item.title, html += "</h1>"), item.description && (html += '<div class="slideSubtitle">', html += item.description, html += "</div>"), html += "</div>", html += "</div>"), html += "</div>"
        }

        function previousImage() {
            swiperInstance ? swiperInstance.slidePrev() : (stopInterval(), showNextImage(currentIndex - 1))
        }

        function nextImage() {
            if (swiperInstance) {
                if (!1 === options.loop && swiperInstance.activeIndex >= swiperInstance.slides.length - 1) return void dialogHelper.close(dlg);
                swiperInstance.slideNext()
            } else stopInterval(), showNextImage(currentIndex + 1)
        }

        function getCurrentImageInfo() {
            if (swiperInstance) {
                var slide = document.querySelector(".swiper-slide-active");
                return slide ? {
                    url: slide.getAttribute("data-original"),
                    shareUrl: slide.getAttribute("data-imageurl"),
                    itemId: slide.getAttribute("data-itemid"),
                    serverId: slide.getAttribute("data-serverid")
                } : null
            }
            return null
        }

        function download() {
            var imageInfo = getCurrentImageInfo();
            require(["fileDownloader"], function(fileDownloader) {
                fileDownloader.download([imageInfo])
            })
        }

        function share() {
            var imageInfo = getCurrentImageInfo();
            navigator.share({
                url: imageInfo.shareUrl
            })
        }

        function play() {
            var btnSlideshowPause = dlg.querySelector(".btnSlideshowPause i");
            btnSlideshowPause && (btnSlideshowPause.innerHTML = "pause"), swiperInstance.startAutoplay()
        }

        function pause() {
            var btnSlideshowPause = dlg.querySelector(".btnSlideshowPause i");
            btnSlideshowPause && (btnSlideshowPause.innerHTML = "play_arrow"), swiperInstance.stopAutoplay()
        }

        function playPause() {
            "pause" !== dlg.querySelector(".btnSlideshowPause i").innerHTML ? play() : pause()
        }

        function onDialogClosed() {
            var swiper = swiperInstance;
            swiper && (swiper.destroy(!0, !0), swiperInstance = null), inputmanager.off(window, onInputCommand), document.removeEventListener(window.PointerEvent ? "pointermove" : "mousemove", onPointerMove)
        }

        function startInterval(options) {
            currentOptions = options, stopInterval(), createElements(options), options.interactive || (currentIntervalMs = options.interval || 11e3, showNextImage(options.startIndex || 0, !0))
        }

        function isOsdOpen() {
            return _osdOpen
        }

        function getOsdBottom() {
            return dlg.querySelector(".slideshowBottomBar")
        }

        function showOsd() {
            var bottom = getOsdBottom();
            bottom && (slideUpToShow(bottom), startHideTimer())
        }

        function hideOsd() {
            var bottom = getOsdBottom();
            bottom && slideDownToHide(bottom)
        }

        function startHideTimer() {
            stopHideTimer(), hideTimeout = setTimeout(hideOsd, 4e3)
        }

        function stopHideTimer() {
            hideTimeout && (clearTimeout(hideTimeout), hideTimeout = null)
        }

        function slideUpToShow(elem) {
            if (elem.classList.contains("hide")) {
                _osdOpen = !0, elem.classList.remove("hide");
                var onFinish = function() {
                    focusManager.focus(elem.querySelector(".btnSlideshowPause"))
                };
                if (!elem.animate) return void onFinish();
                requestAnimationFrame(function() {
                    var keyframes = [{
                            transform: "translate3d(0," + elem.offsetHeight + "px,0)",
                            opacity: ".3",
                            offset: 0
                        }, {
                            transform: "translate3d(0,0,0)",
                            opacity: "1",
                            offset: 1
                        }],
                        timing = {
                            duration: 300,
                            iterations: 1,
                            easing: "ease-out"
                        };
                    elem.animate(keyframes, timing).onfinish = onFinish
                })
            }
        }

        function slideDownToHide(elem) {
            if (!elem.classList.contains("hide")) {
                var onFinish = function() {
                    elem.classList.add("hide"), _osdOpen = !1
                };
                if (!elem.animate) return void onFinish();
                requestAnimationFrame(function() {
                    var keyframes = [{
                            transform: "translate3d(0,0,0)",
                            opacity: "1",
                            offset: 0
                        }, {
                            transform: "translate3d(0," + elem.offsetHeight + "px,0)",
                            opacity: ".3",
                            offset: 1
                        }],
                        timing = {
                            duration: 300,
                            iterations: 1,
                            easing: "ease-out"
                        };
                    elem.animate(keyframes, timing).onfinish = onFinish
                })
            }
        }

        function onPointerMove(e) {
            if ("mouse" === (e.pointerType || (layoutManager.mobile ? "touch" : "mouse"))) {
                var eventX = e.screenX || 0,
                    eventY = e.screenY || 0,
                    obj = lastMouseMoveData;
                if (!obj) return void(lastMouseMoveData = {
                    x: eventX,
                    y: eventY
                });
                if (Math.abs(eventX - obj.x) < 10 && Math.abs(eventY - obj.y) < 10) return;
                obj.x = eventX, obj.y = eventY, showOsd()
            }
        }

        function onInputCommand(e) {
            switch (e.detail.command) {
                case "left":
                    isOsdOpen() || (e.preventDefault(), e.stopPropagation(), previousImage());
                    break;
                case "right":
                    isOsdOpen() || (e.preventDefault(), e.stopPropagation(), nextImage());
                    break;
                case "up":
                case "down":
                case "select":
                case "menu":
                case "info":
                case "play":
                case "playpause":
                case "pause":
                    showOsd()
            }
        }

        function showNextImage(index, skipPreload) {
            index = Math.max(0, index), index >= currentOptions.items.length && (index = 0), currentIndex = index;
            var options = currentOptions,
                items = options.items,
                item = items[index],
                imgUrl = getImgUrl(item),
                onSrcLoaded = function() {
                    var cardImageContainer = dlg.querySelector(".slideshowImage"),
                        newCardImageContainer = document.createElement("div");
                    newCardImageContainer.className = cardImageContainer.className, options.cover && newCardImageContainer.classList.add("slideshowImage-cover"), newCardImageContainer.style.backgroundImage = "url('" + imgUrl + "')", newCardImageContainer.classList.add("hide"), cardImageContainer.parentNode.appendChild(newCardImageContainer), options.showTitle ? dlg.querySelector(".slideshowImageText").innerHTML = item.Name : dlg.querySelector(".slideshowImageText").innerHTML = "", newCardImageContainer.classList.remove("hide");
                    var onAnimationFinished = function() {
                        var parentNode = cardImageContainer.parentNode;
                        parentNode && parentNode.removeChild(cardImageContainer)
                    };
                    if (newCardImageContainer.animate) {
                        var keyframes = [{
                                opacity: "0",
                                offset: 0
                            }, {
                                opacity: "1",
                                offset: 1
                            }],
                            timing = {
                                duration: 1200,
                                iterations: 1
                            };
                        newCardImageContainer.animate(keyframes, timing).onfinish = onAnimationFinished
                    } else onAnimationFinished();
                    stopInterval(), currentTimeout = setTimeout(function() {
                        showNextImage(index + 1, !0)
                    }, currentIntervalMs)
                };
            if (skipPreload) onSrcLoaded();
            else {
                var img = new Image;
                img.onload = onSrcLoaded, img.src = imgUrl
            }
        }

        function stopInterval() {
            currentTimeout && (clearTimeout(currentTimeout), currentTimeout = null)
        }
        var swiperInstance, dlg, currentTimeout, currentIntervalMs, currentOptions, currentIndex, self = this;
        browser.chromecast && (options.interactive = !1);
        var hideTimeout, lastMouseMoveData, _osdOpen = !1;
        self.show = function() {
            startInterval(options)
        }, self.hide = function() {
            var dialog = dlg;
            dialog && dialogHelper.close(dialog)
        }
    }
});