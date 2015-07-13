/**
 * ### Massload plugin
 *
 * Adds massload functionality to jsTree, so that multiple nodes can be loaded in a single request (only useful with lazy loading).
 */
/*globals jQuery, define, exports, require, document */
(function (factory) {
	"use strict";
	if (typeof define === 'function' && define.amd) {
		define('jstree.massload', ['jquery','jstree'], factory);
	}
	else if(typeof exports === 'object') {
		factory(require('jquery'), require('jstree'));
	}
	else {
		factory(jQuery, jQuery.jstree);
	}
}(function ($, jstree, undefined) {
	"use strict";

	if($.jstree.plugins.massload) { return; }

	/**
	 * massload configuration
	 *
	 * It is possible to set this to a standard jQuery-like AJAX config.
	 * In addition to the standard jQuery ajax options here you can supply functions for `data` and `url`, the functions will be run in the current instance's scope and a param will be passed indicating which node IDs need to be loaded, the return value of those functions will be used.
	 *
	 * You can also set this to a function, that function will receive the node IDs being loaded as argument and a second param which is a function (callback) which should be called with the result.
	 *
	 * Both the AJAX and the function approach rely on the same return value - an object where the keys are the node IDs, and the value is the children of that node as an array.
	 *
	 *	{
	 *		"id1" : [{ "text" : "Child of ID1", "id" : "c1" }, { "text" : "Another child of ID1", "id" : "c2" }],
	 *		"id2" : [{ "text" : "Child of ID2", "id" : "c3" }]
	 *	}
	 * 
	 * @name $.jstree.defaults.massload
	 * @plugin massload
	 */
	$.jstree.defaults.massload = null;
	$.jstree.plugins.massload = function (options, parent) {
		this.init = function (el, options) {
			parent.init.call(this, el, options);
			this._data.massload = {};
		};
		this._load_nodes = function (nodes, callback, is_callback) {
			var s = this.settings.massload;
			if(is_callback && !$.isEmptyObject(this._data.massload)) {
				return parent._load_nodes.call(this, nodes, callback, is_callback);
			}
			if($.isFunction(s)) {
				return s.call(this, nodes, $.proxy(function (data) {
					if(data) {
						for(var i in data) {
							if(data.hasOwnProperty(i)) {
								this._data.massload[i] = data[i];
							}
						}
					}
					parent._load_nodes.call(this, nodes, callback, is_callback);
				}, this));
			}
			if(typeof s === 'object' && s && s.url) {
				s = $.extend(true, {}, s);
				if($.isFunction(s.url)) {
					s.url = s.url.call(this, nodes);
				}
				if($.isFunction(s.data)) {
					s.data = s.data.call(this, nodes);
				}
				return $.ajax(s)
					.done($.proxy(function (data,t,x) {
							if(data) {
								for(var i in data) {
									if(data.hasOwnProperty(i)) {
										this._data.massload[i] = data[i];
									}
								}
							}
							parent._load_nodes.call(this, nodes, callback, is_callback);
						}, this))
					.fail($.proxy(function (f) {
							parent._load_nodes.call(this, nodes, callback, is_callback);
						}, this));
			}
			return parent._load_nodes.call(this, nodes, callback, is_callback);
		};
		this._load_node = function (obj, callback) {
			var d = this._data.massload[obj.id];
			if(d) {
				return this[typeof d === 'string' ? '_append_html_data' : '_append_json_data'](obj, typeof d === 'string' ? $($.parseHTML(d)).filter(function () { return this.nodeType !== 3; }) : d, function (status) {
					callback.call(this, status);
					delete this._data.massload[obj.id];
				});
			}
			return parent._load_node.call(this, obj, callback);
		};
	};
}));