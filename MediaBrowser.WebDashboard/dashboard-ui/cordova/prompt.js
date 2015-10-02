define([], function () {
    return function (options) {

        var callback = function (result) {

            if (result.buttonIndex == 1) {
                options.callback(result.input1);
            } else {
                options.callback(null);
            }
        };

        var buttonLabels = [Globalize.translate('ButtonOk'), Globalize.translate('ButtonCancel')];

        navigator.notification.prompt(options.text, callback, options.title, buttonLabels, options.defaultText || '');
    };
});