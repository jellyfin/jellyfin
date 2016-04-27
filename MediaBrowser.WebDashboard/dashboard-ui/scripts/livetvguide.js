define(['tvguide', 'events'], function (tvguide, events) {

    function onGuideLoaded() {

        var context = this.options.element;

        require(["headroom"], function () {

            // construct an instance of Headroom, passing the element
            var headroom = new Headroom(context.querySelector('.tvGuideHeader'), {
                // or scroll tolerance per direction
                scroller: context.querySelector('.guideVerticalScroller'),

                onPin: function () {
                    context.classList.remove('headroomUnpinned');
                },
                // callback when unpinned, `this` is headroom object
                onUnpin: function () {
                    context.classList.add('headroomUnpinned');
                }
            });
            // initialise
            headroom.init();
        });
    }

    window.LiveTvPage.initGuideTab = function (page, tabContent) {

    };

    window.LiveTvPage.renderGuideTab = function (page, tabContent) {

        if (!page.guideInstance) {

            page.guideInstance = new tvguide({
                element: tabContent
            });

            events.on(page.guideInstance, 'load', onGuideLoaded);

        }
    };

});