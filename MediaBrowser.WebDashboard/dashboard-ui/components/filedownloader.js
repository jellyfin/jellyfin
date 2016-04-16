define([], function () {

    return function (items) {

        items.forEach(function (item) {
            window.location.href = item.url;
        });
    };
});