define([], function () {

    function loadImage(elem, url) {

        if (elem.tagName !== "IMG") {

            return new Promise(function (resolve, reject) {

                var tmp = new Image();

                tmp.onload = function () {
                    elem.style.backgroundImage = "url('" + url + "')";
                    resolve(elem);
                };
                tmp.src = url;
            });

        } else {
            elem.setAttribute("src", url);
            return Promise.resolve(elem);
        }
    }

    return {
        loadImage: loadImage
    };

});