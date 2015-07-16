/* global jQuery */

// disable all events
(function ($, undefined) {
	"use strict";
	$.jstree.plugins.trigger = function (options, parent) {
		this.init = function (el, options) {
			// do not forget parent
			parent.init.call(this, el, options);
			this._data.trigger.disabled = false;
		};
		this.trigger = function (ev, data) {
			if(!this._data.trigger.disabled) {
				parent.trigger.call(this, ev, data);
			}
		};
		this.disable_events = function () { this._data.trigger.disabled = true; };
		this.enable_events = function () { this._data.trigger.disabled = false; };
	};
})(jQuery);

// more detailed changed event
(function ($, undefined) {
	"use strict";
	$.jstree.plugins.changed = function (options, parent) {
		var last = [], i, j;
		this.trigger = function (ev, data) {
			if(!data) {
				data = {};
			}
			if(ev.replace('.jstree','') === 'changed') {
				data.changed = { selected : [], deselected : [] };
				var tmp = {};
				for(i = 0, j = last.length; i < j; i++) {
					tmp[last[i]] = 1;
				}
				for(i = 0, j = data.selected.length; i < j; i++) {
					if(!tmp[data.selected[i]]) {
						data.changed.selected.push(data.selected[i]);
					}
					else {
						tmp[data.selected[i]] = 2;
					}
				}
				for(i = 0, j = last.length; i < j; i++) {
					if(tmp[last[i]] === 1) {
						data.changed.deselected.push(last[i]);
					}
				}
				last = data.selected.slice();
			}
			parent.trigger.call(this, ev, data);
		};
	};
})(jQuery);

// no hover
(function ($, undefined) {
	"use strict";
	$.jstree.plugins.nohover = function () {
		this.hover_node = $.noop;
	};
})(jQuery);

// conditional select
(function ($, undefined) {
	"use strict";
	$.jstree.defaults.conditionalselect = function () { return true; };
	$.jstree.plugins.conditionalselect = function (options, parent) {
		// own function
		this.activate_node = function (obj, e) {
			if(this.settings.conditionalselect.call(this, this.get_node(obj))) {
				parent.activate_node.call(this, obj, e);
			}
		};
	};
})(jQuery);

// real checkboxes
(function ($, undefined) {
	"use strict";

	var inp = document.createElement("INPUT");
	inp.type = "checkbox";
	inp.className = "jstree-checkbox jstree-realcheckbox";

	$.jstree.defaults.realcheckboxes = {};

	$.jstree.plugins.realcheckboxes = function (options, parent) {
		this.bind = function () {
			parent.bind.call(this);
			this._data.realcheckboxes.uto = false;
			this.element
				.on('changed.jstree uncheck_node.jstree check_node.jstree uncheck_all.jstree check_all.jstree move_node.jstree copy_node.jstree redraw.jstree open_node.jstree ready.jstree loaded.jstree', $.proxy(function () {
						// only if undetermined is in setting
						if(this._data.realcheckboxes.uto) { clearTimeout(this._data.realcheckboxes.uto); }
						this._data.realcheckboxes.uto = setTimeout($.proxy(this._realcheckboxes, this), 50);
					}, this));
		};
		this.redraw_node = function(obj, deep, callback, force_draw) {
			obj = parent.redraw_node.call(this, obj, deep, callback, force_draw);
			if(obj) {
				var i, j, tmp = null, chk = inp.cloneNode(true);
				for(i = 0, j = obj.childNodes.length; i < j; i++) {
					if(obj.childNodes[i] && obj.childNodes[i].className && obj.childNodes[i].className.indexOf("jstree-anchor") !== -1) {
						tmp = obj.childNodes[i];
						break;
					}
				}
				if(tmp) {
					for(i = 0, j = tmp.childNodes.length; i < j; i++) {
						if(tmp.childNodes[i] && tmp.childNodes[i].className && tmp.childNodes[i].className.indexOf("jstree-checkbox") !== -1) {
							tmp = tmp.childNodes[i];
							break;
						}
					}
				}
				if(tmp && tmp.tagName === "I") {
					tmp.style.backgroundColor = "transparent";
					tmp.style.backgroundImage = "none";
					tmp.appendChild(chk);
				}
			}
			return obj;
		};
		this._realcheckboxes = function () {
			var ts = this.settings.checkbox.tie_selection;
			console.log(ts);
			$('.jstree-realcheckbox').each(function () {
				this.checked = (!ts && this.parentNode.parentNode.className.indexOf("jstree-checked") !== -1) || (ts && this.parentNode.parentNode.className.indexOf('jstree-clicked') !== -1);
				this.indeterminate = this.parentNode.className.indexOf("jstree-undetermined") !== -1;
				this.disabled = this.parentNode.parentNode.className.indexOf("disabled") !== -1;
			});
		};
	};
})(jQuery);

// no state
(function ($, undefined) {
	"use strict";
	$.jstree.plugins.nostate = function () {
		this.set_state = function (state, callback) {
			if(callback) { callback.call(this); }
			this.trigger('set_state');
		};
	};
})(jQuery);

// no selected in state
(function ($, undefined) {
	"use strict";
	$.jstree.plugins.noselectedstate = function (options, parent) {
		this.get_state = function () {
			var state = parent.get_state.call(this);
			delete state.core.selected;
			return state;
		};
	};
})(jQuery);

// allow search results expanding
(function ($, undefined) {
	"use strict";
	$.jstree.plugins.show_matches_children = function (options, parent) {
		this.bind = function () {
			parent.bind.call(this);
			this.element
				.on('search.jstree before_open.jstree', function (e, data) {
					if(data.instance.settings.search && data.instance.settings.search.show_only_matches) {
						data.instance._data.search.dom.find('.jstree-node')
							.show().filter('.jstree-last').filter(function() { return this.nextSibling; }).removeClass('jstree-last')
							.end().end().end().find(".jstree-children").each(function () { $(this).children(".jstree-node:visible").eq(-1).addClass("jstree-last"); });
					}
				});
		};
	};
})(jQuery);

// additional icon on node (outside of anchor)
(function ($, undefined) {
	"use strict";
	var img = document.createElement('IMG');
	//img.src = "http://www.dpcd.vic.gov.au/__data/assets/image/0004/30667/help.gif";
	img.className = "jstree-questionmark";

	$.jstree.defaults.questionmark = $.noop;
	$.jstree.plugins.questionmark = function (options, parent) {
		this.bind = function () {
			parent.bind.call(this);
			this.element
				.on("click.jstree", ".jstree-questionmark", $.proxy(function (e) {
						e.stopImmediatePropagation();
						this.settings.questionmark.call(this, this.get_node(e.target));
					}, this));
		};
		this.teardown = function () {
			if(this.settings.questionmark) {
				this.element.find(".jstree-questionmark").remove();
			}
			parent.teardown.call(this);
		};
		this.redraw_node = function(obj, deep, callback, force_draw) {
			obj = parent.redraw_node.call(this, obj, deep, callback, force_draw);
			if(obj) {
				var tmp = img.cloneNode(true);
				obj.insertBefore(tmp, obj.childNodes[2]);
			}
			return obj;
		};
	};
})(jQuery);

// auto numbering
(function ($, undefined) {
	"use strict";
	var span = document.createElement('SPAN');
	span.className = "jstree-numbering";

	$.jstree.defaults.numbering = {};
	$.jstree.plugins.numbering = function (options, parent) {
		this.teardown = function () {
			if(this.settings.questionmark) {
				this.element.find(".jstree-numbering").remove();
			}
			parent.teardown.call(this);
		};
		this.get_number = function (obj) {
			obj = this.get_node(obj);
			var ind = $.inArray(obj.id, this.get_node(obj.parent).children) + 1;
			return obj.parent === '#' ? ind : this.get_number(obj.parent) + '.' + ind;
		};
		this.redraw_node = function(obj, deep, callback, force_draw) {
			var i, j, tmp = null, elm = null, org = this.get_number(obj);
			obj = parent.redraw_node.call(this, obj, deep, callback, force_draw);
			if(obj) {
				for(i = 0, j = obj.childNodes.length; i < j; i++) {
					if(obj.childNodes[i] && obj.childNodes[i].className && obj.childNodes[i].className.indexOf("jstree-anchor") !== -1) {
						tmp = obj.childNodes[i];
						break;
					}
				}
				if(tmp) {
					elm = span.cloneNode(true);
					elm.innerHTML = org + '. ';
					tmp.insertBefore(elm, tmp.childNodes[tmp.childNodes.length - 1]);
				}
			}
			return obj;
		};
	};
})(jQuery);

// selecting a node opens it
(function ($, undefined) {
	"use strict";
	$.jstree.plugins.selectopens = function (options, parent) {
		this.bind = function () {
			parent.bind.call(this);
			this.element.on('select_node.jstree', function (e, data) { data.instance.open_node(data.node); });
		};
	};
})(jQuery);

// massloading
(function ($, undefined) {
	"use strict";
	$.jstree.defaults.massload = function (nodes, callback) {
		callback(false);
	};
	$.jstree.plugins.massload = function (options, parent) {
		this.init = function (el, options) {
			parent.init.call(this, el, options);
			this._data.massload = {};
		};
		this._load_nodes = function (nodes, callback, is_callback) {
			if(is_callback && !$.isEmptyObject(this._data.massload)) {
				return parent._load_nodes.call(this, nodes, callback, is_callback);
			}
			this.settings.massload.call(this, nodes, $.proxy(function (data) {
				if(data) {
					for(var i in data) {
						if(data.hasOwnProperty(i)) {
							this._data.massload[i] = data[i];
						}
					}
				}
				parent._load_nodes.call(this, nodes, callback, is_callback);
			}, this));
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
})(jQuery);

// object as data
(function ($, undefined) {
	"use strict";
	$.jstree.defaults.datamodel = {};
	$.jstree.plugins.datamodel = function (options, parent) {
		this.init = function (el, options) {
			this._data.datamodel = {};
			parent.init.call(this, el, options);
		};
		this._datamodel = function (id, nodes, callback) {
			var i = 0, j = nodes.length, tmp = [], obj = null;
			for(; i < j; i++) {
				this._data.datamodel[nodes[i].getID()] = nodes[i];
				obj = {
					id : nodes[i].getID(),
					text : nodes[i].getText(),
					children : nodes[i].hasChildren()
				};
				if(nodes[i].getExtra) {
					obj = nodes[i].getExtra(obj); // icon, type
				}
				tmp.push(obj);
			}
			return this._append_json_data(id, tmp, $.proxy(function (status) {
				callback.call(this, status);
			}, this));
		};
		this._load_node = function (obj, callback) {
			var id = obj.id;
			var nd = obj.id === "#" ? this.settings.core.data : this._data.datamodel[obj.id].getChildren($.proxy(function (nodes) {
				this._datamodel(id, nodes, callback);
			}, this));
			if($.isArray(nd)) {
				this._datamodel(id, nd, callback);
			}
		};
	};
})(jQuery);
/* demo of the above
function treeNode(val) {
	var id = ++treeNode.counter;
	this.getID = function () {
		return id;
	};
	this.getText = function () {
		return val.toString();
	};
	this.getExtra = function (obj) {
		obj.icon = false;
		return obj;
	};
	this.hasChildren = function () {
		return true;
	};
	this.getChildren = function () {
		return [
			new treeNode(Math.pow(val, 2)),
			new treeNode(Math.sqrt(val)),
		];
	};
}
treeNode.counter = 0;
			
$('#jstree').jstree({
	'core': {
		'data': [
					new treeNode(2),
					new treeNode(3),
					new treeNode(4),
					new treeNode(5)
				]
	},
	plugins : ['datamodel']
});
*/

// paste override
(function ($, undefined) {
	"use strict";
	$.jstree.plugins.pastewithpos = function () {
		this.paste = function (obj, pos) {
			obj = this.get_node(obj);
			if(!obj || !ccp_mode || !ccp_mode.match(/^(copy_node|move_node)$/) || !ccp_node) { return false; }
			if(this[ccp_mode](ccp_node, obj, pos)) {
				this.trigger('paste', { "parent" : obj.id, "node" : ccp_node, "mode" : ccp_mode });
			}
			ccp_node = false;
			ccp_mode = false;
			ccp_inst = false;
		};
	};
})(jQuery);