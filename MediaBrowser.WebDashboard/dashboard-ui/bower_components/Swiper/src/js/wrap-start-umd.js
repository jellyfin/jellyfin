(function (root, factory) {
	'use strict';

	if (typeof define === 'function' && define.amd) {
		// AMD. Register as an anonymous module.
		define(['jquery'], factory);
	} else if (typeof exports === 'object') {
		// Node. Does not work with strict CommonJS, but
		// only CommonJS-like environments that support module.exports,
		// like Node.
		module.exports = factory(require('jquery'));
	} else {
		// Browser globals (root is window)
		root.Swiper = factory(root.jQuery);
	}
}(this, function ($) {
	'use strict';
