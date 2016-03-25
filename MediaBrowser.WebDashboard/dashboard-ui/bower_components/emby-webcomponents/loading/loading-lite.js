define(['layoutManager', 'MaterialSpinner', 'css!./loading'], function (layoutManager) {

    return {
        show: function () {
            var elem = document.querySelector('.docspinner');

            if (!elem) {

                elem = document.createElement("div");
                elem.classList.add('docspinner');
                elem.classList.add('mdl-spinner');
                elem.classList.add('mdl-js-spinner');

                if (layoutManager.tv) {
                    elem.classList.add('tv');
                }

                document.body.appendChild(elem);
                componentHandler.upgradeElement(elem, 'MaterialSpinner');
            }

            elem.classList.add('is-active');
            elem.classList.remove('loadingHide');
        },
        hide: function () {
            var elem = document.querySelector('.docspinner');

            if (elem) {

                elem.classList.remove('is-active');
                elem.classList.add('loadingHide');
            }
        }
    };
});