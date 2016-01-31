(function () {

    function showMenu() {

        var menuItems = [];

        menuItems.push({
            name: Globalize.translate('OptionBackdropSlideshow'),
            id: 'backdrops',
            ironIcon: 'video-library'
        });

        menuItems.push({
            name: Globalize.translate('OptionPhotoSlideshow'),
            id: 'photos',
            ironIcon: 'photo-library'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                callback: function (id) {

                    switch (id) {

                        case 'backdrops':
                            start({

                                ImageTypes: "Backdrop",
                                EnableImageTypes: "Backdrop",
                                IncludeItemTypes: "Movie,Series,MusicArtist,Game"

                            }, {
                                showTitle: true,
                                cover: true
                            });
                            break;
                        case 'photos':
                            start({

                                ImageTypes: "Primary",
                                EnableImageTypes: "Primary",
                                MediaTypes: "Photo"

                            }, {
                                showTitle: false
                            });
                            break;
                        default:
                            break;
                    }
                }
            });

        });
    }

    function createElements() {

        var elem = document.querySelector('.slideshowContainer');

        if (elem) {
            return elem;
        }

        elem = document.createElement('div');
        elem.classList.add('slideshowContainer');

        var html = '';

        html += '<div class="slideshowImage"></div><div class="slideshowImageText"></div>';
        html += '<paper-icon-button icon="cancel" class="btnStopSlideshow" onclick="SlideShow.stop();"></paper-icon-button>';

        elem.innerHTML = html;

        document.body.appendChild(elem);
    }

    function start(query, options) {

        query = $.extend({

            SortBy: "Random",
            Recursive: true,
            Fields: "Taglines",
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary",
            StartIndex: 0,
            Limit: 200

        }, query);

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

            if (result.Items.length) {
                createElements();

                startInterval(result.Items, options);
            } else {
                Dashboard.alert({
                    message: Globalize.translate('NoSlideshowContentFound')
                });
            }
        });
    }

    var currentInterval;
    function startInterval(items, options) {

        stopInterval();

        var index = 1;

        var changeImage = function () {

            if (index >= items.length) {
                index = 0;
            }

            showItemImage(items[index], options);
            index++;

        };

        currentInterval = setInterval(changeImage, 5000);

        changeImage();
        document.body.classList.add('bodyWithPopupOpen');
    }

    function showItemImage(item, options) {

        var imgUrl;

        if (item.BackdropImageTags && item.BackdropImageTags.length) {
            imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                type: "Backdrop",
                maxWidth: screen.availWidth,
                tag: item.BackdropImageTags[0]
            });
        } else {
            imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                type: "Primary",
                maxWidth: Math.min(screen.availWidth, 1280),
                tag: item.ImageTags.Primary
            });
        }

        var cardImageContainer = document.querySelector('.slideshowImage');

        var newCardImageContainer = document.createElement('div');
        newCardImageContainer.className = cardImageContainer.className;

        if (options.cover) {
            newCardImageContainer.classList.add('cover');
        }

        newCardImageContainer.style.backgroundImage = "url('" + imgUrl + "')";

        if (options.showTitle) {
            document.querySelector('.slideshowImageText').innerHTML = item.Name;
        } else {
            document.querySelector('.slideshowImageText').innerHTML = '';
        }

        cardImageContainer.parentNode.appendChild(newCardImageContainer);

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
    }

    function stopInterval() {
        if (currentInterval) {
            clearInterval(currentInterval);
            currentInterval = null;
        }
    }

    function stop() {
        stopInterval();

        var elem = document.querySelector('.slideshowContainer');

        if (elem) {

            var onAnimationFinish = function () {
                elem.parentNode.removeChild(elem);
            };

            if (elem.animate) {
                var animation = fadeOut(elem, 1);
                animation.onfinish = onAnimationFinish;
            } else {
                onAnimationFinish();
            }
        }

        document.body.classList.remove('bodyWithPopupOpen');
    }

    function fadeOut(elem, iterations) {
        var keyframes = [
          { opacity: '1', offset: 0 },
          { opacity: '0', offset: 1 }];
        var timing = { duration: 500, iterations: iterations };
        return elem.animate(keyframes, timing);
    }

    window.SlideShow = {
        showMenu: showMenu,
        stop: stop
    };

})();