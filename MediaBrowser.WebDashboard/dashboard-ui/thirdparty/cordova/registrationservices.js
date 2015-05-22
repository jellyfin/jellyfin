(function () {

    function validatePlayback(deferred) {

        var platform = (device.platform || '').toLowerCase();

        // Don't require validation on android
        if (platform.indexOf('android') != -1) {
            deferred.resolve();
            return;
        }

        deferred.resolve();
    }

    function validateLiveTV(deferred) {

        var platform = (device.platform || '').toLowerCase();

        // Don't require validation if not android
        if (platform.indexOf('android') == -1) {
            deferred.resolve();
            return;
        }

        deferred.resolve();
    }

    window.RegistrationServices = {

        renderPluginInfo: function (page, pkg, pluginSecurityInfo) {


        },

        addRecurringFields: function (page, period) {

        },

        initSupporterForm: function (page) {

            $('.recurringSubscriptionCancellationHelp', page).html('');
        },

        validateFeature: function (name) {
            var deferred = DeferredBuilder.Deferred();

            if (name == 'playback') {
                validatePlayback();
            } else if (name == 'livetv') {
                validateLiveTV();
            } else {
                deferred.resolve();
            }

            return deferred.promise();
        }
    };

})();