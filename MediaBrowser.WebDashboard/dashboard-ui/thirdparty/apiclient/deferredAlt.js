(function (globalScope) {

    globalScope.Deferred = {

        Deferred: function () {

            var self = this;
            var done = [];
            var fail = [];
            var always = [];
            var isOk = false;
            var isDone = false;
            var resolveScope;
            var resolveArgs;

            self.promise = function () {
                return this;
            };

            self.done = function (fn) {
                if (isDone && isOk) {
                    fn.apply(resolveScope || {}, resolveArgs);
                }
                else {
                    done.push(fn);
                }
                return self;
            };

            self.fail = function (fn) {

                if (isDone && !isOk) {
                    fn.apply(resolveScope || {}, resolveArgs);
                }
                else {
                    fail.push(fn);
                }
                return self;
            };

            self.always = function (fn) {
                if (isDone) {
                    fn.apply(resolveScope || {}, resolveArgs);
                }
                else {
                    always.push(fn);
                }
                return self;
            };

            self.resolveWith = function (scope, args) {
                resolveScope = scope;
                resolveArgs = args;
                isOk = true;
                isDone = true;
                self.trigger();
            };

            self.rejectWith = function (scope, args) {
                resolveScope = scope;
                resolveArgs = args;
                isOk = true;
                isDone = true;
                self.trigger();
            };

            self.trigger = function () {

                var i, length;

                if (isOk) {
                    var doneClone = done.splice(0);
                    for (i = 0, length = doneClone.length; i < length; i++) {

                        doneClone[i].apply(resolveScope || {}, resolveArgs);
                    }
                }
                else {
                    var failClone = fail.splice(0);
                    for (i = 0, length = failClone.length; i < length; i++) {

                        failClone[i].apply(resolveScope || {}, resolveArgs);
                    }
                }

                var alwaysClone = fail.splice(0);
                for (i = 0, length = alwaysClone.length; i < length; i++) {

                    alwaysClone[i].apply(resolveScope || {}, resolveArgs);
                }
            };

            return this;
        },

        when: function(promises) {
            
        }
    };

})(window);