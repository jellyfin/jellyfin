define(['dialog', 'globalize'], function (dialog, globalize) {
    'use strict';

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
            name: options.cancelText || globalize.translate('sharedcomponents#ButtonCancel'),
            id: 'cancel',
            type: options.primary === 'cancel' ? 'submit' : 'cancel'
        });

        items.push({
            name: options.confirmText || globalize.translate('sharedcomponents#ButtonOk'),
            id: 'ok',
            type: options.primary === 'cancel' ? 'cancel' : 'submit'
        });

        options.buttons = items;

        return dialog(options).then(function (result) {
            if (result === 'ok') {
                return Promise.resolve();
            }

            return Promise.reject();
        });
    };
});