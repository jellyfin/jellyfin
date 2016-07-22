define(['dialog', 'globalize'], function (dialog, globalize) {

    return function (text, title) {

        var options;
        if (typeof text === 'string') {
            options = {
                title: title,
                text: text
            };
        } else {
            options = text;
        }

        var items = [];

        items.push({
            name: globalize.translate('sharedcomponents#ButtonOk'),
            id: 'ok'
        });

        options.buttons = items;

        return dialog(options).then(function (result) {
            if (result == 'ok') {
                return Promise.resolve();
            }

            return Promise.reject();
        });
    };
});