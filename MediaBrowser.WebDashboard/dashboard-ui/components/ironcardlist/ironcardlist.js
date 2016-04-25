define(['iron-list', 'lazyload-image'], function () {

    function getTemplate(scrollTarget) {

        var maxPhysical = 200;

        // is="lazyload-image" 

        return new Promise(function (resolve, reject) {

            var xhr = new XMLHttpRequest();
            xhr.open('GET', 'components/ironcardlist/ironcardlist.template.html', true);

            xhr.onload = function (e) {

                var html = this.response;

                html = html.replace('${maxphysical}', maxPhysical);
                html = html.replace('${scrolltarget}', scrollTarget);

                resolve(html);
            }

            xhr.send();
        });
    }

    return {
        getTemplate: getTemplate
    };

});