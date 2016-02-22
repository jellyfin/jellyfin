define(['layoutManager', 'dialogText'], function (layoutManager, dialogText) {

    function showTvConfirm(options) {
        return new Promise(function (resolve, reject) {

            require(['actionsheet'], function (actionSheet) {

                var items = [];

                items.push({
                    name: dialogText.get('Ok'),
                    id: 'ok'
                });

                items.push({
                    name: dialogText.get('Cancel'),
                    id: 'cancel'
                });

                actionsheet.show({

                    title: options.title,
                    items: items

                }).then(function (id) {

                    switch (id) {
                    
                        case 'ok':
                            resolve();
                            break;
                        default:
                            reject();
                            break;
                    }

                }, reject);
            });
        });
    }

    function showConfirm(options) {

    }

    return function (options) {

        if (typeof options === 'string') {
            options = {
                title: '',
                text: options
            };
        }

        if (layoutManager.tv) {
            return showTvConfirm(options);
        }

        return showConfirm(options);
    };
});