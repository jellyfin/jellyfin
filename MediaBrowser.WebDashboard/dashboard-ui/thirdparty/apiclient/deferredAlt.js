(function (global) {
    function isArray(arr) {
        return Object.prototype.toString.call(arr) === '[object Array]';
    }

    function foreach(arr, handler) {
        if (isArray(arr)) {
            for (var i = 0; i < arr.length; i++) {
                handler(arr[i]);
            }
        }
        else
            handler(arr);
    }

    function D(fn) {
        var status = 'pending',
			doneFuncs = [],
			failFuncs = [],
			progressFuncs = [],
			resultArgs = null,

		promise = {
		    done: function () {
		        for (var i = 0; i < arguments.length; i++) {
		            // skip any undefined or null arguments
		            if (!arguments[i]) {
		                continue;
		            }

		            if (isArray(arguments[i])) {
		                var arr = arguments[i];
		                for (var j = 0; j < arr.length; j++) {
		                    // immediately call the function if the deferred has been resolved
		                    if (status === 'resolved') {
		                        arr[j].apply(this, resultArgs);
		                    }

		                    doneFuncs.push(arr[j]);
		                }
		            }
		            else {
		                // immediately call the function if the deferred has been resolved
		                if (status === 'resolved') {
		                    arguments[i].apply(this, resultArgs);
		                }

		                doneFuncs.push(arguments[i]);
		            }
		        }

		        return this;
		    },

		    fail: function () {
		        for (var i = 0; i < arguments.length; i++) {
		            // skip any undefined or null arguments
		            if (!arguments[i]) {
		                continue;
		            }

		            if (isArray(arguments[i])) {
		                var arr = arguments[i];
		                for (var j = 0; j < arr.length; j++) {
		                    // immediately call the function if the deferred has been resolved
		                    if (status === 'rejected') {
		                        arr[j].apply(this, resultArgs);
		                    }

		                    failFuncs.push(arr[j]);
		                }
		            }
		            else {
		                // immediately call the function if the deferred has been resolved
		                if (status === 'rejected') {
		                    arguments[i].apply(this, resultArgs);
		                }

		                failFuncs.push(arguments[i]);
		            }
		        }

		        return this;
		    },

		    always: function () {
		        return this.done.apply(this, arguments).fail.apply(this, arguments);
		    },

		    progress: function () {
		        for (var i = 0; i < arguments.length; i++) {
		            // skip any undefined or null arguments
		            if (!arguments[i]) {
		                continue;
		            }

		            if (isArray(arguments[i])) {
		                var arr = arguments[i];
		                for (var j = 0; j < arr.length; j++) {
		                    // immediately call the function if the deferred has been resolved
		                    if (status === 'pending') {
		                        progressFuncs.push(arr[j]);
		                    }
		                }
		            }
		            else {
		                // immediately call the function if the deferred has been resolved
		                if (status === 'pending') {
		                    progressFuncs.push(arguments[i]);
		                }
		            }
		        }

		        return this;
		    },

		    then: function () {
		        // fail callbacks
		        if (arguments.length > 1 && arguments[1]) {
		            this.fail(arguments[1]);
		        }

		        // done callbacks
		        if (arguments.length > 0 && arguments[0]) {
		            this.done(arguments[0]);
		        }

		        // notify callbacks
		        if (arguments.length > 2 && arguments[2]) {
		            this.progress(arguments[2]);
		        }
		    },

		    promise: function (obj) {
		        if (obj == null) {
		            return promise;
		        } else {
		            for (var i in promise) {
		                obj[i] = promise[i];
		            }
		            return obj;
		        }
		    },

		    state: function () {
		        return status;
		    },

		    debug: function () {
		        console.log('[debug]', doneFuncs, failFuncs, status);
		    },

		    isRejected: function () {
		        return status === 'rejected';
		    },

		    isResolved: function () {
		        return status === 'resolved';
		    },

		    pipe: function (done, fail, progress) {
		        return D(function (def) {
		            foreach(done, function (func) {
		                // filter function
		                if (typeof func === 'function') {
		                    deferred.done(function () {
		                        var returnval = func.apply(this, arguments);
		                        // if a new deferred/promise is returned, its state is passed to the current deferred/promise
		                        if (returnval && typeof returnval === 'function') {
		                            returnval.promise().then(def.resolve, def.reject, def.notify);
		                        }
		                        else {	// if new return val is passed, it is passed to the piped done
		                            def.resolve(returnval);
		                        }
		                    });
		                }
		                else {
		                    deferred.done(def.resolve);
		                }
		            });

		            foreach(fail, function (func) {
		                if (typeof func === 'function') {
		                    deferred.fail(function () {
		                        var returnval = func.apply(this, arguments);

		                        if (returnval && typeof returnval === 'function') {
		                            returnval.promise().then(def.resolve, def.reject, def.notify);
		                        } else {
		                            def.reject(returnval);
		                        }
		                    });
		                }
		                else {
		                    deferred.fail(def.reject);
		                }
		            });
		        }).promise();
		    }
		},

		deferred = {
		    resolveWith: function (context) {
		        if (status === 'pending') {
		            status = 'resolved';
		            var args = resultArgs = (arguments.length > 1) ? arguments[1] : [];
		            for (var i = 0; i < doneFuncs.length; i++) {
		                doneFuncs[i].apply(context, args);
		            }
		        }
		        return this;
		    },

		    rejectWith: function (context) {
		        if (status === 'pending') {
		            status = 'rejected';
		            var args = resultArgs = (arguments.length > 1) ? arguments[1] : [];
		            for (var i = 0; i < failFuncs.length; i++) {
		                failFuncs[i].apply(context, args);
		            }
		        }
		        return this;
		    },

		    notifyWith: function (context) {
		        if (status === 'pending') {
		            var args = resultArgs = (arguments.length > 1) ? arguments[1] : [];
		            for (var i = 0; i < progressFuncs.length; i++) {
		                progressFuncs[i].apply(context, args);
		            }
		        }
		        return this;
		    },

		    resolve: function () {
		        return this.resolveWith(this, arguments);
		    },

		    reject: function () {
		        return this.rejectWith(this, arguments);
		    },

		    notify: function () {
		        return this.notifyWith(this, arguments);
		    }
		}

        var obj = promise.promise(deferred);

        if (fn) {
            fn.apply(obj, [obj]);
        }

        return obj;
    }

    D.when = function () {
        if (arguments.length < 2) {
            var obj = arguments.length ? arguments[0] : undefined;
            if (obj && (typeof obj.isResolved === 'function' && typeof obj.isRejected === 'function')) {
                return obj.promise();
            }
            else {
                return D().resolve(obj).promise();
            }
        }
        else {
            return (function (args) {
                var df = D(),
					size = args.length,
					done = 0,
					rp = new Array(size);	// resolve params: params of each resolve, we need to track down them to be able to pass them in the correct order if the master needs to be resolved

                for (var i = 0; i < args.length; i++) {
                    (function (j) {
                        var obj = null;

                        if (args[j].done) {
                            args[j].done(function () { rp[j] = (arguments.length < 2) ? arguments[0] : arguments; if (++done == size) { df.resolve.apply(df, rp); } })
                            .fail(function () { df.reject(arguments); });
                        } else {
                            obj = args[j];
                            args[j] = new Deferred();

                            args[j].done(function () { rp[j] = (arguments.length < 2) ? arguments[0] : arguments; if (++done == size) { df.resolve.apply(df, rp); } })
                            .fail(function () { df.reject(arguments); }).resolve(obj);
                        }
                    })(i);
                }

                return df.promise();
            })(arguments);
        }
    }

    global.Deferred = D;
})(window);

(function (globalScope) {

    globalScope.DeferredBuilder = {

        Deferred: function () {
            return new globalScope.Deferred();
        },

        when: function (promises) {

            return globalScope.Deferred.when(promises);
        }

    };

})(window);