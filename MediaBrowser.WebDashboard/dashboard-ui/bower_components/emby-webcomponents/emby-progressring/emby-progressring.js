define(['require', 'css!./emby-progressring', 'registerElement'], function (require) {
    'use strict';

    var EmbyProgressRing = Object.create(HTMLDivElement.prototype);

    EmbyProgressRing.createdCallback = function () {

        this.classList.add('progressring');
        var instance = this;

        require(['text!./emby-progressring.template.html'], function (template) {
            instance.innerHTML = template;

            //if (window.MutationObserver) {
            //    // create an observer instance
            //    var observer = new MutationObserver(function (mutations) {
            //        mutations.forEach(function (mutation) {

            //            instance.setProgress(parseFloat(instance.getAttribute('data-progress') || '0'));
            //        });
            //    });

            //    // configuration of the observer:
            //    var config = { attributes: true, childList: false, characterData: false };

            //    // pass in the target node, as well as the observer options
            //    observer.observe(instance, config);

            //    instance.observer = observer;
            //}

            instance.setProgress(parseFloat(instance.getAttribute('data-progress') || '0'));
        });
    };

    EmbyProgressRing.setProgress = function (progress) {

        progress = Math.floor(progress);

        var angle;

        if (progress < 25) {
            angle = -90 + (progress / 100) * 360;

            this.querySelector('.animate-0-25-b').style.transform = 'rotate(' + angle + 'deg)';

            this.querySelector('.animate-25-50-b').style.transform = 'rotate(-90deg)';
            this.querySelector('.animate-50-75-b').style.transform = 'rotate(-90deg)';
            this.querySelector('.animate-75-100-b').style.transform = 'rotate(-90deg)';
        }
        else if (progress >= 25 && progress < 50) {

            angle = -90 + ((progress - 25) / 100) * 360;

            this.querySelector('.animate-0-25-b').style.transform = 'none';
            this.querySelector('.animate-25-50-b').style.transform = 'rotate(' + angle + 'deg)';

            this.querySelector('.animate-50-75-b').style.transform = 'rotate(-90deg)';
            this.querySelector('.animate-75-100-b').style.transform = 'rotate(-90deg)';
        }
        else if (progress >= 50 && progress < 75) {
            angle = -90 + ((progress - 50) / 100) * 360;

            this.querySelector('.animate-0-25-b').style.transform = 'none';
            this.querySelector('.animate-25-50-b').style.transform = 'none';
            this.querySelector('.animate-50-75-b').style.transform = 'rotate(' + angle + 'deg)';

            this.querySelector('.animate-75-100-b').style.transform = 'rotate(-90deg)';
        }
        else if (progress >= 75 && progress <= 100) {
            angle = -90 + ((progress - 75) / 100) * 360;

            this.querySelector('.animate-0-25-b').style.transform = 'none';
            this.querySelector('.animate-25-50-b').style.transform = 'none';
            this.querySelector('.animate-50-75-b').style.transform = 'none';
            this.querySelector('.animate-75-100-b').style.transform = 'rotate(' + angle + 'deg)';
        }

        this.querySelector('.progressring-text').innerHTML = progress + '%';
    };

    EmbyProgressRing.attachedCallback = function () {

    };

    EmbyProgressRing.detachedCallback = function () {


        var observer = this.observer;

        if (observer) {
            // later, you can stop observing
            observer.disconnect();

            this.observer = null;
        }
    };

    document.registerElement('emby-progressring', {
        prototype: EmbyProgressRing,
        extends: 'div'
    });

    return EmbyProgressRing;
});