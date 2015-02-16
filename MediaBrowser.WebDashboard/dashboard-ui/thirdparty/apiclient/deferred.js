(function (globalScope) {

    globalScope.DeferredBuilder = {

        Deferred: function () {
            return jQuery.Deferred();
        },

        when: function (promises) {

            return jQuery.when(promises);
        }

    };

})(window);