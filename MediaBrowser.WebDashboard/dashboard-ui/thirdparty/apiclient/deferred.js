(function (globalScope) {

    globalScope.Deferred = {

        Deferred: function () {
            return jQuery.Deferred();
        },

        when: function (promises) {

            return jQuery.when(promises);
        }

    };

})(window);