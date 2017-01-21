define(['loading'], function (loading) {
    'use strict';

    function reload(context, itemId) {

        loading.show();

        if (itemId) {
            require(['metadataEditor'], function (metadataEditor) {

                metadataEditor.embed(context.querySelector('.editPageInnerContent'), itemId, ApiClient.serverInfo().Id);
            });
        } else {
            context.querySelector('.editPageInnerContent').innerHTML = '';
            loading.hide();
        }
    }

    return function (view, params) {

        view.addEventListener('viewshow', function () {
            reload(this, MetadataEditor.getCurrentItemId());
        });

        MetadataEditor.setCurrentItemId(null);

        view.querySelector('.libraryTree').addEventListener('itemclicked', function (event) {

            var data = event.detail;

            if (data.id != MetadataEditor.getCurrentItemId()) {

                MetadataEditor.setCurrentItemId(data.id);
                reload(view, data.id);
            }
        });
    };
});
