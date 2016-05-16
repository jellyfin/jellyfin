define(['MaterialSpinner', 'css!./loading'], function () {

    var loadingElem;

    return {
        show: function () {
            var elem = loadingElem;

            if (!elem) {

                elem = document.createElement("div");
                loadingElem = elem;

                elem.classList.add('docspinner');
                elem.classList.add('mdl-spinner');
                elem.classList.add('mdl-js-spinner');

                document.body.appendChild(elem);
                componentHandler.upgradeElement(elem, 'MaterialSpinner');
            }

            elem.classList.add('is-active');
            elem.classList.remove('loadingHide');
        },
        hide: function () {
            var elem = loadingElem;

            if (elem) {

                elem.classList.remove('is-active');
                elem.classList.add('loadingHide');
            }
        }
    };
});