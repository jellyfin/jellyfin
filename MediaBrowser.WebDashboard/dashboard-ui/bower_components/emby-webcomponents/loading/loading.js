define(['layoutManager', 'paper-spinner', 'css!./loading'], function (layoutManager) {

    return {
        show: function () {
            var elem = document.querySelector('.docspinner');

            if (!elem) {

                elem = document.createElement("paper-spinner");
                elem.classList.add('docspinner');

                if (layoutManager.tv) {
                    elem.classList.add('tv');
                }

                document.body.appendChild(elem);
            }

            elem.active = true;
            elem.classList.remove('loadingHide');
        },
        hide: function () {
            var elem = document.querySelector('.docspinner');

            if (elem) {

                elem.active = false;
                elem.classList.add('loadingHide');
            }
        }
    };
});