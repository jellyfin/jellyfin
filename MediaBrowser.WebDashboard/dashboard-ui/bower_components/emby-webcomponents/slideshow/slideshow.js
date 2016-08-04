define(['dialogHelper', 'inputManager', 'connectionManager', 'layoutManager', 'focusManager', 'apphost', 'loading', 'css!./style', 'material-icons', 'paper-icon-button-light'], function (dialogHelper, inputmanager, connectionManager, layoutManager, focusManager, appHost, loading) {

    function getImageUrl(item, options, apiClient) {

        options = options || {};
        options.type = options.type || "Primary";

        if (typeof (item) === 'string') {
            return apiClient.getScaledImageUrl(item, options);
        }

        if (item.ImageTags && item.ImageTags[options.type]) {

            options.tag = item.ImageTags[options.type];
            return apiClient.getScaledImageUrl(item.Id, options);
        }

        if (options.type == 'Primary') {
            if (item.AlbumId && item.AlbumPrimaryImageTag) {

                options.tag = item.AlbumPrimaryImageTag;
                return apiClient.getScaledImageUrl(item.AlbumId, options);
            }
        }

        return null;
    }

    function getBackdropImageUrl(item, options, apiClient) {

        options = options || {};
        options.type = options.type || "Backdrop";

        // If not resizing, get the original image
        if (!options.maxWidth && !options.width && !options.maxHeight && !options.height) {
            options.quality = 100;
        }

        if (item.BackdropImageTags && item.BackdropImageTags.length) {

            options.tag = item.BackdropImageTags[0];
            return apiClient.getScaledImageUrl(item.Id, options);
        }

        return null;
    }

    function getImgUrl(item, original) {

        var apiClient = connectionManager.getApiClient(item.ServerId);
        var imageOptions = {};

        if (!original) {
            imageOptions.maxWidth = screen.availWidth;
        }
        if (item.BackdropImageTags && item.BackdropImageTags.length) {
            return getBackdropImageUrl(item, imageOptions, apiClient);
        } else {

            if (item.MediaType == 'Photo' && original) {
                return apiClient.getUrl("Items/" + item.Id + "/Download", {
                    api_key: apiClient.accessToken()
                });
            }
            imageOptions.type = "Primary";
            return getImageUrl(item, imageOptions, apiClient);
        }
    }

    function getIcon(icon, cssClass, canFocus, autoFocus) {

        var tabIndex = canFocus ? '' : ' tabindex="-1"';
        autoFocus = autoFocus ? ' autofocus' : '';
        return '<button is="paper-icon-button-light" class="autoSize ' + cssClass + '"' + tabIndex + autoFocus + '><i class="md-icon slideshowButtonIcon">' + icon + '</i></button>';
    }

    return function (options) {

        var self = this;
        var swiperInstance;
        var dlg;
        var currentTimeout;
        var currentIntervalMs;
        var currentOptions;
        var currentIndex;

        function createElements(options) {

            dlg = dialogHelper.createDialog({
                exitAnimationDuration: options.interactive ? 400 : 800,
                size: 'fullscreen',
                autoFocus: false,
                scrollY: false,
                exitAnimation: 'fadeout'
            });

            dlg.classList.add('slideshowDialog');

            var html = '';

            if (options.interactive) {

                var actionButtonsOnTop = layoutManager.mobile;

                html += '<div>';
                html += '<div class="slideshowSwiperContainer"><div class="swiper-wrapper"></div></div>';

                html += getIcon('keyboard_arrow_left', 'btnSlideshowPrevious slideshowButton', false);
                html += getIcon('keyboard_arrow_right', 'btnSlideshowNext slideshowButton', false);

                html += '<div class="topActionButtons">';
                if (actionButtonsOnTop) {
                    if (appHost.supports('filedownload')) {
                        html += getIcon('file_download', 'btnDownload slideshowButton', true);
                    }
                    if (appHost.supports('sharing')) {
                        html += getIcon('share', 'btnShare slideshowButton', true);
                    }
                }
                html += getIcon('close', 'slideshowButton btnSlideshowExit', false);
                html += '</div>';

                if (!actionButtonsOnTop) {
                    html += '<div class="slideshowBottomBar hide">';

                    html += getIcon('pause', 'btnSlideshowPause slideshowButton', true, true);
                    if (appHost.supports('filedownload')) {
                        html += getIcon('file_download', 'btnDownload slideshowButton', true);
                    }
                    if (appHost.supports('sharing')) {
                        html += getIcon('share', 'btnShare slideshowButton', true);
                    }

                    html += '</div>';
                }

                html += '</div>';

            } else {
                html += '<div class="slideshowImage"></div><h1 class="slideshowImageText"></h1>';
            }

            dlg.innerHTML = html;

            if (options.interactive) {
                dlg.querySelector('.btnSlideshowExit').addEventListener('click', function (e) {

                    dialogHelper.close(dlg);
                });
                dlg.querySelector('.btnSlideshowNext').addEventListener('click', nextImage);
                dlg.querySelector('.btnSlideshowPrevious').addEventListener('click', previousImage);

                var btnPause = dlg.querySelector('.btnSlideshowPause');
                if (btnPause) {
                    btnPause.addEventListener('click', playPause);
                }

                var btnDownload = dlg.querySelector('.btnDownload');
                if (btnDownload) {
                    btnDownload.addEventListener('click', download);
                }

                var btnShare = dlg.querySelector('.btnShare');
                if (btnShare) {
                    btnShare.addEventListener('click', share);
                }
            }

            document.body.appendChild(dlg);

            dialogHelper.open(dlg).then(function () {

                stopInterval();
                dlg.parentNode.removeChild(dlg);
            });

            inputmanager.on(window, onInputCommand);
            document.addEventListener('mousemove', onMouseMove);

            dlg.addEventListener('close', onDialogClosed);

            if (options.interactive) {
                loadSwiper(dlg);
            }
        }

        function loadSwiper(dlg) {

            if (currentOptions.slides) {
                dlg.querySelector('.swiper-wrapper').innerHTML = currentOptions.slides.map(getSwiperSlideHtmlFromSlide).join('');
            } else {
                dlg.querySelector('.swiper-wrapper').innerHTML = currentOptions.items.map(getSwiperSlideHtmlFromItem).join('');
            }

            require(['swiper'], function (swiper) {

                swiperInstance = new Swiper(dlg.querySelector('.slideshowSwiperContainer'), {
                    // Optional parameters
                    direction: 'horizontal',
                    loop: options.loop !== false,
                    autoplay: options.interval || 8000,
                    // Disable preloading of all images
                    preloadImages: false,
                    // Enable lazy loading
                    lazyLoading: true,
                    lazyLoadingInPrevNext: true,
                    autoplayDisableOnInteraction: false,
                    initialSlide: options.startIndex || 0,
                    speed: 240
                });

                swiperInstance.on('onLazyImageLoad', onSlideChangeStart);
                swiperInstance.on('onLazyImageReady', onSlideChangeEnd);

                if (layoutManager.mobile) {
                    pause();
                } else {
                    play();
                }
            });
        }

        function getSwiperSlideHtmlFromItem(item) {

            return getSwiperSlideHtmlFromSlide({
                imageUrl: getImgUrl(item),
                originalImage: getImgUrl(item, true),
                //title: item.Name,
                //description: item.Overview
                Id: item.Id,
                ServerId: item.ServerId
            });
        }

        function onSlideChangeStart(swiper, slide, image) {

            //loading.show();
        }

        function onSlideChangeEnd(swiper, slide, image) {

            //loading.hide();
        }

        function getSwiperSlideHtmlFromSlide(item) {

            var html = '';
            html += '<div class="swiper-slide" data-original="' + item.originalImage + '" data-itemid="' + item.Id + '" data-serverid="' + item.ServerId + '">';
            html += '<img data-src="' + item.imageUrl + '" class="swiper-lazy swiper-slide-img">';
            if (item.title || item.subtitle) {
                html += '<div class="slideText">';
                html += '<div class="slideTextInner">';
                if (item.title) {
                    html += '<div class="slideTitle">';
                    html += item.title;
                    html += '</div>';
                }
                if (item.description) {
                    html += '<div class="slideSubtitle">';
                    html += item.description;
                    html += '</div>';
                }
                html += '</div>';
                html += '</div>';
            }
            html += '</div>';

            return html;
        }

        function previousImage() {
            if (swiperInstance) {
                swiperInstance.slidePrev();
            } else {
                stopInterval();
                showNextImage(currentIndex - 1);
            }
        }

        function nextImage() {
            if (swiperInstance) {

                if (options.loop === false) {

                    if (swiperInstance.activeIndex >= swiperInstance.slides.length - 1) {
                        dialogHelper.close(dlg);
                        return;
                    }
                }

                swiperInstance.slideNext();
            } else {
                stopInterval();
                showNextImage(currentIndex + 1);
            }
        }

        function getCurrentImageInfo() {

            if (swiperInstance) {
                var slide = document.querySelector('.swiper-slide-active');

                if (slide) {
                    return {
                        url: slide.getAttribute('data-original'),
                        itemId: slide.getAttribute('data-itemid'),
                        serverId: slide.getAttribute('data-serverid')
                    };
                }
                return null;
            } else {
                return null;
            }
        }

        function download() {

            var imageInfo = getCurrentImageInfo();

            require(['fileDownloader'], function (fileDownloader) {
                fileDownloader.download([imageInfo]);
            });
        }

        function share() {

            var imageInfo = getCurrentImageInfo();

            require(['sharingmanager'], function (sharingManager) {
                sharingManager.showMenu(imageInfo);
            });
        }

        function play() {

            var btnSlideshowPause = dlg.querySelector('.btnSlideshowPause i');
            if (btnSlideshowPause) {
                btnSlideshowPause.innerHTML = "pause";
            }

            swiperInstance.startAutoplay();
        }

        function pause() {

            var btnSlideshowPause = dlg.querySelector('.btnSlideshowPause i');
            if (btnSlideshowPause) {
                btnSlideshowPause.innerHTML = "play_arrow";
            }

            swiperInstance.stopAutoplay();
        }

        function playPause() {

            var paused = dlg.querySelector('.btnSlideshowPause i').innerHTML != "pause";
            if (paused) {
                play();
            } else {
                pause();
            }
        }

        function onDialogClosed() {

            var swiper = swiperInstance;
            if (swiper) {
                swiper.off('onLazyImageLoad');
                swiper.off('onLazyImageReady');
                swiper.destroy(true, true);
                swiperInstance = null;
            }

            inputmanager.off(window, onInputCommand);
            document.removeEventListener('mousemove', onMouseMove);
        }

        function startInterval(options) {

            currentOptions = options;

            stopInterval();
            createElements(options);

            if (!options.interactive) {
                currentIntervalMs = options.interval || 8000;
                showNextImage(options.startIndex || 0, true);
            }
        }

        var _osdOpen = false;
        function isOsdOpen() {
            return _osdOpen;
        }

        function getOsdBottom() {
            return dlg.querySelector('.slideshowBottomBar');
        }

        function showOsd() {

            var bottom = getOsdBottom();
            if (bottom) {
                slideUpToShow(bottom);
                startHideTimer();
            }
        }

        function hideOsd() {

            var bottom = getOsdBottom();
            if (bottom) {
                slideDownToHide(bottom);
            }
        }

        var hideTimeout;
        function startHideTimer() {
            stopHideTimer();
            hideTimeout = setTimeout(hideOsd, 4000);
        }
        function stopHideTimer() {
            if (hideTimeout) {
                clearTimeout(hideTimeout);
                hideTimeout = null;
            }
        }

        function slideUpToShow(elem) {

            if (!elem.classList.contains('hide')) {
                return;
            }

            _osdOpen = true;
            elem.classList.remove('hide');

            requestAnimationFrame(function () {

                var keyframes = [
                  { transform: 'translate3d(0,' + elem.offsetHeight + 'px,0)', opacity: '.3', offset: 0 },
                  { transform: 'translate3d(0,0,0)', opacity: '1', offset: 1 }];
                var timing = { duration: 300, iterations: 1, easing: 'ease-out' };
                elem.animate(keyframes, timing).onfinish = function () {
                    focusManager.focus(elem.querySelector('.btnSlideshowPause'));
                };
            });
        }

        function slideDownToHide(elem) {

            if (elem.classList.contains('hide')) {
                return;
            }

            requestAnimationFrame(function () {

                var keyframes = [
                  { transform: 'translate3d(0,0,0)', opacity: '1', offset: 0 },
                  { transform: 'translate3d(0,' + elem.offsetHeight + 'px,0)', opacity: '.3', offset: 1 }];
                var timing = { duration: 300, iterations: 1, easing: 'ease-out' };
                elem.animate(keyframes, timing).onfinish = function () {
                    elem.classList.add('hide');
                    _osdOpen = false;
                };
            });
        }

        var lastMouseMoveData;
        function onMouseMove(e) {

            var eventX = e.screenX || 0;
            var eventY = e.screenY || 0;

            var obj = lastMouseMoveData;
            if (!obj) {
                lastMouseMoveData = {
                    x: eventX,
                    y: eventY
                };
                return;
            }

            // if coord are same, it didn't move
            if (Math.abs(eventX - obj.x) < 10 && Math.abs(eventY - obj.y) < 10) {
                return;
            }

            obj.x = eventX;
            obj.y = eventY;

            showOsd();
        }

        function onInputCommand(e) {

            switch (e.detail.command) {

                case 'left':
                    if (!isOsdOpen()) {
                        e.preventDefault();
                        previousImage();
                    }
                    break;
                case 'right':
                    if (!isOsdOpen()) {
                        e.preventDefault();
                        nextImage();
                    }
                    break;
                case 'up':
                case 'down':
                case 'select':
                case 'menu':
                case 'info':
                case 'play':
                case 'playpause':
                case 'pause':
                case 'fastforward':
                case 'rewind':
                case 'next':
                case 'previous':
                    showOsd();
                    break;
                default:
                    break;
            }
        }

        function showNextImage(index, skipPreload) {

            index = Math.max(0, index);
            if (index >= currentOptions.items.length) {
                index = 0;
            }
            currentIndex = index;

            var options = currentOptions;
            var items = options.items;
            var item = items[index];
            var imgUrl = getImgUrl(item);

            var onSrcLoaded = function () {
                var cardImageContainer = dlg.querySelector('.slideshowImage');

                var newCardImageContainer = document.createElement('div');
                newCardImageContainer.className = cardImageContainer.className;

                if (options.cover) {
                    newCardImageContainer.classList.add('slideshowImage-cover');
                }

                newCardImageContainer.style.backgroundImage = "url('" + imgUrl + "')";
                newCardImageContainer.classList.add('hide');
                cardImageContainer.parentNode.appendChild(newCardImageContainer);

                if (options.showTitle) {
                    dlg.querySelector('.slideshowImageText').innerHTML = item.Name;
                } else {
                    dlg.querySelector('.slideshowImageText').innerHTML = '';
                }

                newCardImageContainer.classList.remove('hide');
                var onAnimationFinished = function () {

                    var parentNode = cardImageContainer.parentNode;
                    if (parentNode) {
                        parentNode.removeChild(cardImageContainer);
                    }
                };

                if (newCardImageContainer.animate) {

                    var keyframes = [
                            { opacity: '0', offset: 0 },
                            { opacity: '1', offset: 1 }];
                    var timing = { duration: 1200, iterations: 1 };
                    newCardImageContainer.animate(keyframes, timing).onfinish = onAnimationFinished;
                } else {
                    onAnimationFinished();
                }

                stopInterval();
                currentTimeout = setTimeout(function () {
                    showNextImage(index + 1, true);

                }, currentIntervalMs);
            };

            if (!skipPreload) {
                var img = new Image();
                img.onload = onSrcLoaded;
                img.src = imgUrl;
            } else {
                onSrcLoaded();
            }
        }

        function stopInterval() {
            if (currentTimeout) {
                clearTimeout(currentTimeout);
                currentTimeout = null;
            }
        }

        self.show = function () {
            startInterval(options);
        };

        self.hide = function () {

            var dialog = dlg;
            if (dialog) {

                dialogHelper.close(dialog);
            }
        };
    }
});