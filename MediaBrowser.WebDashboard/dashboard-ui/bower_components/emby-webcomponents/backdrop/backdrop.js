define(['browser', 'css!./style'], function (browser) {

    function enableAnimation(elem) {

        if (browser.mobile) {
            return false;
        }

        return elem.animate;
    }

    function backdrop() {

        var self = this;
        var isDestroyed;

        self.load = function (url, parent, existingBackdropImage) {

            var img = new Image();
            img.onload = function () {

                if (isDestroyed) {
                    return;
                }

                var backdropImage = document.createElement('div');
                backdropImage.classList.add('backdropImage');
                backdropImage.classList.add('displayingBackdropImage');
                backdropImage.style.backgroundImage = "url('" + url + "')";
                backdropImage.setAttribute('data-url', url);

                parent.appendChild(backdropImage);

                if (!enableAnimation(backdropImage)) {
                    if (existingBackdropImage && existingBackdropImage.parentNode) {
                        existingBackdropImage.parentNode.removeChild(existingBackdropImage);
                    }
                    internalBackdrop(true);
                    return;
                }

                var animation = fadeIn(backdropImage, 1);
                currentAnimation = animation;
                animation.onfinish = function () {

                    if (animation == currentAnimation) {
                        currentAnimation = null;
                    }
                    if (existingBackdropImage && existingBackdropImage.parentNode) {
                        existingBackdropImage.parentNode.removeChild(existingBackdropImage);
                    }
                };

                internalBackdrop(true);
            };
            img.src = url;
        };

        var currentAnimation;
        function fadeIn(elem, iterations) {
            var keyframes = [
              { opacity: '0', offset: 0 },
              { opacity: '1', offset: 1 }];
            var timing = { duration: 800, iterations: iterations, easing: 'ease-in' };
            return elem.animate(keyframes, timing);
        }

        function cancelAnimation() {
            var animation = currentAnimation;
            if (animation) {
                console.log('Cancelling backdrop animation');
                animation.cancel();
                currentAnimation = null;
            }
        }

        self.destroy = function () {

            isDestroyed = true;
            cancelAnimation();
        };
    }

    var backdropContainer;
    function getBackdropContainer() {

        if (!backdropContainer) {
            backdropContainer = document.querySelector('.backdropContainer');
        }

        if (!backdropContainer) {
            backdropContainer = document.createElement('div');
            backdropContainer.classList.add('backdropContainer');
            document.body.insertBefore(backdropContainer, document.body.firstChild);
        }

        return backdropContainer;
    }

    function clearBackdrop(clearAll) {

        if (currentLoadingBackdrop) {
            currentLoadingBackdrop.destroy();
            currentLoadingBackdrop = null;
        }

        var elem = getBackdropContainer();
        elem.innerHTML = '';

        if (clearAll) {
            hasExternalBackdrop = false;
        }
        internalBackdrop(false);
    }

    var skinContainer;
    function setSkinContainerBackgroundEnabled() {

        if (!skinContainer) {
            skinContainer = document.querySelector('.skinContainer');
        }

        if (hasInternalBackdrop || hasExternalBackdrop) {
            skinContainer.classList.add('withBackdrop');
        } else {
            skinContainer.classList.remove('withBackdrop');
        }
    }

    var hasInternalBackdrop;
    function internalBackdrop(enabled) {
        hasInternalBackdrop = enabled;
        setSkinContainerBackgroundEnabled();
    }

    var hasExternalBackdrop;
    function externalBackdrop(enabled) {
        hasExternalBackdrop = enabled;
        setSkinContainerBackgroundEnabled();
    }

    function getRandom(min, max) {
        return Math.floor(Math.random() * (max - min) + min);
    }

    var currentLoadingBackdrop;
    function setBackdropImage(url) {

        if (currentLoadingBackdrop) {
            currentLoadingBackdrop.destroy();
            currentLoadingBackdrop = null;
        }

        var elem = getBackdropContainer();
        var existingBackdropImage = elem.querySelector('.displayingBackdropImage');

        if (existingBackdropImage && existingBackdropImage.getAttribute('data-url') == url) {
            if (existingBackdropImage.getAttribute('data-url') == url) {
                return;
            }
            existingBackdropImage.classList.remove('displayingBackdropImage');
        }

        var instance = new backdrop();
        instance.load(url, elem, existingBackdropImage);
        currentLoadingBackdrop = instance;
    }

    function setBackdrops(items) {

        var images = items.map(function (i) {

            if (i.BackdropImageTags && i.BackdropImageTags.length > 0) {
                return {
                    id: i.Id,
                    tag: i.BackdropImageTags[0],
                    serverId: i.ServerId
                };
            }

            if (i.ParentBackdropItemId && i.ParentBackdropImageTags && i.ParentBackdropImageTags.length) {

                return {
                    id: i.ParentBackdropItemId,
                    tag: i.ParentBackdropImageTags[0],
                    serverId: i.ServerId
                };
            }
            return null;

        }).filter(function (i) {
            return i != null;
        });

        if (images.length) {

            var index = getRandom(0, images.length - 1);
            var item = images[index];

            require(['connectionManager'], function (connectionManager) {

                var apiClient = connectionManager.getApiClient(item.serverId);
                var imgUrl = apiClient.getScaledImageUrl(item.id, {
                    type: "Backdrop",
                    tag: item.tag,
                    //maxWidth: window.innerWidth,
                    quality: 100
                });

                setBackdrop(imgUrl);
            });

        } else {
            clearBackdrop();
        }
    }

    function setBackdrop(url) {

        if (url) {
            setBackdropImage(url);

        } else {
            clearBackdrop();
        }
    }

    return {

        setBackdrops: setBackdrops,
        setBackdrop: setBackdrop,
        clear: clearBackdrop,
        externalBackdrop: externalBackdrop
    };

});