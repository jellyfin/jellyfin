// Adapter for "promises-aplus-tests" test runner

var path = require("path");
var Promise = require(path.join(__dirname,"lib","npo.src.js"));

module.exports.deferred = function __deferred__() {
	var o = {};
	o.promise = new Promise(function __Promise__(resolve,reject){
		o.resolve = resolve;
		o.reject = reject;
	});
	return o;
};

module.exports.resolved = function __resolved__(val) {
	return Promise.resolve(val);
};

module.exports.rejected = function __rejected__(reason) {
	return Promise.reject(reason);
};
