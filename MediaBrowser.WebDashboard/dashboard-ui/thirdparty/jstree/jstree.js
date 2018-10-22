! function(factory) {
    "use strict";
    "function" == typeof define && define.amd ? define(["jquery"], factory) : factory("object" == typeof exports ? require("jquery") : jQuery)
}(function($, undefined) {
    "use strict";
    if (!$.jstree) {
        var _temp1, _temp2, instance_counter = 0,
            ccp_node = !1,
            ccp_mode = !1,
            ccp_inst = !1,
            themes_loaded = [],
            src = $("script:last").attr("src"),
            document = window.document,
            _node = document.createElement("LI");
        _node.setAttribute("role", "treeitem"), _temp1 = document.createElement("I"), _temp1.className = "jstree-icon jstree-ocl", _temp1.setAttribute("role", "presentation"), _node.appendChild(_temp1), _temp1 = document.createElement("A"), _temp1.className = "jstree-anchor", _temp1.setAttribute("href", "#"), _temp1.setAttribute("tabindex", "-1"), _temp2 = document.createElement("I"), _temp2.className = "jstree-icon jstree-themeicon", _temp2.setAttribute("role", "presentation"), _temp1.appendChild(_temp2), _node.appendChild(_temp1), _temp1 = _temp2 = null, $.jstree = {
            version: "3.1.1",
            defaults: {
                plugins: []
            },
            plugins: {},
            path: src && -1 !== src.indexOf("/") ? src.replace(/\/[^\/]+$/, "") : "",
            idregex: /[\\:&!^|()\[\]<>@*'+~#";.,=\- \/${}%?`]/g
        }, $.jstree.create = function(el, options) {
            var tmp = new $.jstree.core(++instance_counter),
                opt = options;
            return options = $.extend(!0, {}, $.jstree.defaults, options), opt && opt.plugins && (options.plugins = opt.plugins), $.each(options.plugins, function(i, k) {
                "core" !== i && (tmp = tmp.plugin(k, options[k]))
            }), $(el).data("jstree", tmp), tmp.init(el, options), tmp
        }, $.jstree.destroy = function() {
            $(".jstree:jstree").jstree("destroy"), $(document).off(".jstree")
        }, $.jstree.core = function(id) {
            this._id = id, this._cnt = 0, this._wrk = null, this._data = {
                core: {
                    themes: {
                        name: !1,
                        dots: !1,
                        icons: !1
                    },
                    selected: [],
                    last_error: {},
                    working: !1,
                    worker_queue: [],
                    focused: null
                }
            }
        }, $.jstree.reference = function(needle) {
            var tmp = null,
                obj = null;
            if (!needle || !needle.id || needle.tagName && needle.nodeType || (needle = needle.id), !obj || !obj.length) try {
                obj = $(needle)
            } catch (ignore) {}
            if (!obj || !obj.length) try {
                obj = $("#" + needle.replace($.jstree.idregex, "\\$&"))
            } catch (ignore) {}
            return obj && obj.length && (obj = obj.closest(".jstree")).length && (obj = obj.data("jstree")) ? tmp = obj : $(".jstree").each(function() {
                var inst = $(this).data("jstree");
                if (inst && inst._model.data[needle]) return tmp = inst, !1
            }), tmp
        }, $.fn.jstree = function(arg) {
            var is_method = "string" == typeof arg,
                args = Array.prototype.slice.call(arguments, 1),
                result = null;
            return !(!0 === arg && !this.length) && (this.each(function() {
                var instance = $.jstree.reference(this),
                    method = is_method && instance ? instance[arg] : null;
                if (result = is_method && method ? method.apply(instance, args) : null, instance || is_method || arg !== undefined && !$.isPlainObject(arg) || $.jstree.create(this, arg), (instance && !is_method || !0 === arg) && (result = instance || !1), null !== result && result !== undefined) return !1
            }), null !== result && result !== undefined ? result : this)
        }, $.expr[":"].jstree = $.expr.createPseudo(function(search) {
            return function(a) {
                return $(a).hasClass("jstree") && $(a).data("jstree") !== undefined
            }
        }), $.jstree.defaults.core = {
            data: !1,
            strings: !1,
            check_callback: !1,
            error: $.noop,
            animation: 200,
            multiple: !0,
            themes: {
                name: !1,
                url: !1,
                dir: !1,
                dots: !0,
                icons: !0,
                stripes: !1,
                variant: !1,
                responsive: !1
            },
            expand_selected_onload: !0,
            worker: !0,
            force_text: !1,
            dblclick_toggle: !0
        }, $.jstree.core.prototype = {
            plugin: function(deco, opts) {
                var Child = $.jstree.plugins[deco];
                return Child ? (this._data[deco] = {}, Child.prototype = this, new Child(opts, this)) : this
            },
            init: function(el, options) {
                this._model = {
                    data: {
                        "#": {
                            id: "#",
                            parent: null,
                            parents: [],
                            children: [],
                            children_d: [],
                            state: {
                                loaded: !1
                            }
                        }
                    },
                    changed: [],
                    force_full_redraw: !1,
                    redraw_timeout: !1,
                    default_state: {
                        loaded: !0,
                        opened: !1,
                        selected: !1,
                        disabled: !1
                    }
                }, this.element = $(el).addClass("jstree jstree-" + this._id), this.settings = options, this._data.core.ready = !1, this._data.core.loaded = !1, this._data.core.rtl = "rtl" === this.element.css("direction"), this.element[this._data.core.rtl ? "addClass" : "removeClass"]("jstree-rtl"), this.element.attr("role", "tree"), this.settings.core.multiple && this.element.attr("aria-multiselectable", !0), this.element.attr("tabindex") || this.element.attr("tabindex", "0"), this.bind(), this.trigger("init"), this._data.core.original_container_html = this.element.find(" > ul > li").clone(!0), this._data.core.original_container_html.find("li").addBack().contents().filter(function() {
                    return 3 === this.nodeType && (!this.nodeValue || /^\s+$/.test(this.nodeValue))
                }).remove(), this.element.html("<ul class='jstree-container-ul jstree-children' role='group'><li id='j" + this._id + "_loading' class='jstree-initial-node jstree-loading jstree-leaf jstree-last' role='tree-item'><i class='jstree-icon jstree-ocl'></i><a class='jstree-anchor' href='#'><i class='jstree-icon jstree-themeicon-hidden'></i>" + this.get_string("Loading ...") + "</a></li></ul>"), this.element.attr("aria-activedescendant", "j" + this._id + "_loading"), this._data.core.li_height = this.get_container_ul().children("li").first().height() || 24, this.trigger("loading"), this.load_node("#")
            },
            destroy: function(keep_html) {
                if (this._wrk) try {
                    window.URL.revokeObjectURL(this._wrk), this._wrk = null
                } catch (ignore) {}
                keep_html || this.element.empty(), this.teardown()
            },
            teardown: function() {
                this.unbind(), this.element.removeClass("jstree").removeData("jstree").find("[class^='jstree']").addBack().attr("class", function() {
                    return this.className.replace(/jstree[^ ]*|$/gi, "")
                }), this.element = null
            },
            bind: function() {
                var word = "",
                    tout = null,
                    was_click = 0;
                this.element.on("dblclick.jstree", function() {
                    if (document.selection && document.selection.empty) document.selection.empty();
                    else if (window.getSelection) {
                        var sel = window.getSelection();
                        try {
                            sel.removeAllRanges(), sel.collapse()
                        } catch (ignore) {}
                    }
                }).on("mousedown.jstree", $.proxy(function(e) {
                    e.target === this.element[0] && (e.preventDefault(), was_click = +new Date)
                }, this)).on("mousedown.jstree", ".jstree-ocl", function(e) {
                    e.preventDefault()
                }).on("click.jstree", ".jstree-ocl", $.proxy(function(e) {
                    this.toggle_node(e.target)
                }, this)).on("dblclick.jstree", ".jstree-anchor", $.proxy(function(e) {
                    this.settings.core.dblclick_toggle && this.toggle_node(e.target)
                }, this)).on("click.jstree", ".jstree-anchor", $.proxy(function(e) {
                    e.preventDefault(), e.currentTarget !== document.activeElement && $(e.currentTarget).focus(), this.activate_node(e.currentTarget, e)
                }, this)).on("keydown.jstree", ".jstree-anchor", $.proxy(function(e) {
                    if ("INPUT" === e.target.tagName) return !0;
                    if (32 !== e.which && 13 !== e.which && (e.shiftKey || e.ctrlKey || e.altKey || e.metaKey)) return !0;
                    var o = null;
                    switch (this._data.core.rtl && (37 === e.which ? e.which = 39 : 39 === e.which && (e.which = 37)), e.which) {
                        case 32:
                            e.ctrlKey && (e.type = "click", $(e.currentTarget).trigger(e));
                            break;
                        case 13:
                            e.type = "click", $(e.currentTarget).trigger(e);
                            break;
                        case 37:
                            e.preventDefault(), this.is_open(e.currentTarget) ? this.close_node(e.currentTarget) : (o = this.get_parent(e.currentTarget)) && "#" !== o.id && this.get_node(o, !0).children(".jstree-anchor").focus();
                            break;
                        case 38:
                            e.preventDefault(), o = this.get_prev_dom(e.currentTarget), o && o.length && o.children(".jstree-anchor").focus();
                            break;
                        case 39:
                            e.preventDefault(), this.is_closed(e.currentTarget) ? this.open_node(e.currentTarget, function(o) {
                                this.get_node(o, !0).children(".jstree-anchor").focus()
                            }) : this.is_open(e.currentTarget) && (o = this.get_node(e.currentTarget, !0).children(".jstree-children")[0]) && $(this._firstChild(o)).children(".jstree-anchor").focus();
                            break;
                        case 40:
                            e.preventDefault(), o = this.get_next_dom(e.currentTarget), o && o.length && o.children(".jstree-anchor").focus();
                            break;
                        case 106:
                            this.open_all();
                            break;
                        case 36:
                            e.preventDefault(), o = this._firstChild(this.get_container_ul()[0]), o && $(o).children(".jstree-anchor").filter(":visible").focus();
                            break;
                        case 35:
                            e.preventDefault(), this.element.find(".jstree-anchor").filter(":visible").last().focus()
                    }
                }, this)).on("load_node.jstree", $.proxy(function(e, data) {
                    data.status && ("#" !== data.node.id || this._data.core.loaded || (this._data.core.loaded = !0, this._firstChild(this.get_container_ul()[0]) && this.element.attr("aria-activedescendant", this._firstChild(this.get_container_ul()[0]).id), this.trigger("loaded")), this._data.core.ready || setTimeout($.proxy(function() {
                        if (this.element && !this.get_container_ul().find(".jstree-loading").length) {
                            if (this._data.core.ready = !0, this._data.core.selected.length) {
                                if (this.settings.core.expand_selected_onload) {
                                    var i, j, tmp = [];
                                    for (i = 0, j = this._data.core.selected.length; i < j; i++) tmp = tmp.concat(this._model.data[this._data.core.selected[i]].parents);
                                    for (tmp = $.vakata.array_unique(tmp), i = 0, j = tmp.length; i < j; i++) this.open_node(tmp[i], !1, 0)
                                }
                                this.trigger("changed", {
                                    action: "ready",
                                    selected: this._data.core.selected
                                })
                            }
                            this.trigger("ready")
                        }
                    }, this), 0))
                }, this)).on("keypress.jstree", $.proxy(function(e) {
                    if ("INPUT" === e.target.tagName) return !0;
                    tout && clearTimeout(tout), tout = setTimeout(function() {
                        word = ""
                    }, 500);
                    var chr = String.fromCharCode(e.which).toLowerCase(),
                        col = this.element.find(".jstree-anchor").filter(":visible"),
                        ind = col.index(document.activeElement) || 0,
                        end = !1;
                    if (word += chr, word.length > 1) {
                        if (col.slice(ind).each($.proxy(function(i, v) {
                                if (0 === $(v).text().toLowerCase().indexOf(word)) return $(v).focus(), end = !0, !1
                            }, this)), end) return;
                        if (col.slice(0, ind).each($.proxy(function(i, v) {
                                if (0 === $(v).text().toLowerCase().indexOf(word)) return $(v).focus(), end = !0, !1
                            }, this)), end) return
                    }
                    if (new RegExp("^" + chr + "+$").test(word)) {
                        if (col.slice(ind + 1).each($.proxy(function(i, v) {
                                if ($(v).text().toLowerCase().charAt(0) === chr) return $(v).focus(), end = !0, !1
                            }, this)), end) return;
                        if (col.slice(0, ind + 1).each($.proxy(function(i, v) {
                                if ($(v).text().toLowerCase().charAt(0) === chr) return $(v).focus(), end = !0, !1
                            }, this)), end) return
                    }
                }, this)).on("init.jstree", $.proxy(function() {
                    var s = this.settings.core.themes;
                    this._data.core.themes.dots = s.dots, this._data.core.themes.stripes = s.stripes, this._data.core.themes.icons = s.icons, this.set_theme(s.name || "default", s.url), this.set_theme_variant(s.variant)
                }, this)).on("loading.jstree", $.proxy(function() {
                    this[this._data.core.themes.dots ? "show_dots" : "hide_dots"](), this[this._data.core.themes.icons ? "show_icons" : "hide_icons"](), this[this._data.core.themes.stripes ? "show_stripes" : "hide_stripes"]()
                }, this)).on("blur.jstree", ".jstree-anchor", $.proxy(function(e) {
                    this._data.core.focused = null, $(e.currentTarget).filter(".jstree-hovered").mouseleave(), this.element.attr("tabindex", "0")
                }, this)).on("focus.jstree", ".jstree-anchor", $.proxy(function(e) {
                    var tmp = this.get_node(e.currentTarget);
                    tmp && tmp.id && (this._data.core.focused = tmp.id), this.element.find(".jstree-hovered").not(e.currentTarget).mouseleave(), $(e.currentTarget).mouseenter(), this.element.attr("tabindex", "-1")
                }, this)).on("focus.jstree", $.proxy(function() {
                    if (+new Date - was_click > 500 && !this._data.core.focused) {
                        was_click = 0;
                        var act = this.get_node(this.element.attr("aria-activedescendant"), !0);
                        act && act.find("> .jstree-anchor").focus()
                    }
                }, this)).on("mouseenter.jstree", ".jstree-anchor", $.proxy(function(e) {
                    this.hover_node(e.currentTarget)
                }, this)).on("mouseleave.jstree", ".jstree-anchor", $.proxy(function(e) {
                    this.dehover_node(e.currentTarget)
                }, this))
            },
            unbind: function() {
                this.element.off(".jstree"), $(document).off(".jstree-" + this._id)
            },
            trigger: function(ev, data) {
                data || (data = {}), data.instance = this, this.element.triggerHandler(ev.replace(".jstree", "") + ".jstree", data)
            },
            get_container: function() {
                return this.element
            },
            get_container_ul: function() {
                return this.element.children(".jstree-children").first()
            },
            get_string: function(key) {
                var a = this.settings.core.strings;
                return $.isFunction(a) ? a.call(this, key) : a && a[key] ? a[key] : key
            },
            _firstChild: function(dom) {
                for (dom = dom ? dom.firstChild : null; null !== dom && 1 !== dom.nodeType;) dom = dom.nextSibling;
                return dom
            },
            _nextSibling: function(dom) {
                for (dom = dom ? dom.nextSibling : null; null !== dom && 1 !== dom.nodeType;) dom = dom.nextSibling;
                return dom
            },
            _previousSibling: function(dom) {
                for (dom = dom ? dom.previousSibling : null; null !== dom && 1 !== dom.nodeType;) dom = dom.previousSibling;
                return dom
            },
            get_node: function(obj, as_dom) {
                obj && obj.id && (obj = obj.id);
                var dom;
                try {
                    if (this._model.data[obj]) obj = this._model.data[obj];
                    else if ("string" == typeof obj && this._model.data[obj.replace(/^#/, "")]) obj = this._model.data[obj.replace(/^#/, "")];
                    else if ("string" == typeof obj && (dom = $("#" + obj.replace($.jstree.idregex, "\\$&"), this.element)).length && this._model.data[dom.closest(".jstree-node").attr("id")]) obj = this._model.data[dom.closest(".jstree-node").attr("id")];
                    else if ((dom = $(obj, this.element)).length && this._model.data[dom.closest(".jstree-node").attr("id")]) obj = this._model.data[dom.closest(".jstree-node").attr("id")];
                    else {
                        if (!(dom = $(obj, this.element)).length || !dom.hasClass("jstree")) return !1;
                        obj = this._model.data["#"]
                    }
                    return as_dom && (obj = "#" === obj.id ? this.element : $("#" + obj.id.replace($.jstree.idregex, "\\$&"), this.element)), obj
                } catch (ex) {
                    return !1
                }
            },
            get_path: function(obj, glue, ids) {
                if (!(obj = obj.parents ? obj : this.get_node(obj)) || "#" === obj.id || !obj.parents) return !1;
                var i, j, p = [];
                for (p.push(ids ? obj.id : obj.text), i = 0, j = obj.parents.length; i < j; i++) p.push(ids ? obj.parents[i] : this.get_text(obj.parents[i]));
                return p = p.reverse().slice(1), glue ? p.join(glue) : p
            },
            get_next_dom: function(obj, strict) {
                var tmp;
                if (obj = this.get_node(obj, !0), obj[0] === this.element[0]) {
                    for (tmp = this._firstChild(this.get_container_ul()[0]); tmp && 0 === tmp.offsetHeight;) tmp = this._nextSibling(tmp);
                    return !!tmp && $(tmp)
                }
                if (!obj || !obj.length) return !1;
                if (strict) {
                    tmp = obj[0];
                    do {
                        tmp = this._nextSibling(tmp)
                    } while (tmp && 0 === tmp.offsetHeight);
                    return !!tmp && $(tmp)
                }
                if (obj.hasClass("jstree-open")) {
                    for (tmp = this._firstChild(obj.children(".jstree-children")[0]); tmp && 0 === tmp.offsetHeight;) tmp = this._nextSibling(tmp);
                    if (null !== tmp) return $(tmp)
                }
                tmp = obj[0];
                do {
                    tmp = this._nextSibling(tmp)
                } while (tmp && 0 === tmp.offsetHeight);
                return null !== tmp ? $(tmp) : obj.parentsUntil(".jstree", ".jstree-node").nextAll(".jstree-node:visible").first()
            },
            get_prev_dom: function(obj, strict) {
                var tmp;
                if (obj = this.get_node(obj, !0), obj[0] === this.element[0]) {
                    for (tmp = this.get_container_ul()[0].lastChild; tmp && 0 === tmp.offsetHeight;) tmp = this._previousSibling(tmp);
                    return !!tmp && $(tmp)
                }
                if (!obj || !obj.length) return !1;
                if (strict) {
                    tmp = obj[0];
                    do {
                        tmp = this._previousSibling(tmp)
                    } while (tmp && 0 === tmp.offsetHeight);
                    return !!tmp && $(tmp)
                }
                tmp = obj[0];
                do {
                    tmp = this._previousSibling(tmp)
                } while (tmp && 0 === tmp.offsetHeight);
                if (null !== tmp) {
                    for (obj = $(tmp); obj.hasClass("jstree-open");) obj = obj.children(".jstree-children").first().children(".jstree-node:visible:last");
                    return obj
                }
                return !(!(tmp = obj[0].parentNode.parentNode) || !tmp.className || -1 === tmp.className.indexOf("jstree-node")) && $(tmp)
            },
            get_parent: function(obj) {
                return !(!(obj = this.get_node(obj)) || "#" === obj.id) && obj.parent
            },
            get_children_dom: function(obj) {
                return obj = this.get_node(obj, !0), obj[0] === this.element[0] ? this.get_container_ul().children(".jstree-node") : !(!obj || !obj.length) && obj.children(".jstree-children").children(".jstree-node")
            },
            is_parent: function(obj) {
                return (obj = this.get_node(obj)) && (!1 === obj.state.loaded || obj.children.length > 0)
            },
            is_loaded: function(obj) {
                return (obj = this.get_node(obj)) && obj.state.loaded
            },
            is_loading: function(obj) {
                return (obj = this.get_node(obj)) && obj.state && obj.state.loading
            },
            is_open: function(obj) {
                return (obj = this.get_node(obj)) && obj.state.opened
            },
            is_closed: function(obj) {
                return (obj = this.get_node(obj)) && this.is_parent(obj) && !obj.state.opened
            },
            is_leaf: function(obj) {
                return !this.is_parent(obj)
            },
            load_node: function(obj, callback) {
                var k, l, i, j, c;
                if ($.isArray(obj)) return this._load_nodes(obj.slice(), callback), !0;
                if (!(obj = this.get_node(obj))) return callback && callback.call(this, obj, !1), !1;
                if (obj.state.loaded) {
                    for (obj.state.loaded = !1, k = 0, l = obj.children_d.length; k < l; k++) {
                        for (i = 0, j = obj.parents.length; i < j; i++) this._model.data[obj.parents[i]].children_d = $.vakata.array_remove_item(this._model.data[obj.parents[i]].children_d, obj.children_d[k]);
                        this._model.data[obj.children_d[k]].state.selected && (c = !0, this._data.core.selected = $.vakata.array_remove_item(this._data.core.selected, obj.children_d[k])), delete this._model.data[obj.children_d[k]]
                    }
                    obj.children = [], obj.children_d = [], c && this.trigger("changed", {
                        action: "load_node",
                        node: obj,
                        selected: this._data.core.selected
                    })
                }
                return obj.state.failed = !1, obj.state.loading = !0, this.get_node(obj, !0).addClass("jstree-loading").attr("aria-busy", !0), this._load_node(obj, $.proxy(function(status) {
                    obj = this._model.data[obj.id], obj.state.loading = !1, obj.state.loaded = status, obj.state.failed = !obj.state.loaded;
                    var dom = this.get_node(obj, !0);
                    obj.state.loaded && !obj.children.length && dom && dom.length && !dom.hasClass("jstree-leaf") && dom.removeClass("jstree-closed jstree-open").addClass("jstree-leaf"), dom.removeClass("jstree-loading").attr("aria-busy", !1), this.trigger("load_node", {
                        node: obj,
                        status: status
                    }), callback && callback.call(this, obj, status)
                }, this)), !0
            },
            _load_nodes: function(nodes, callback, is_callback) {
                var i, j, r = !0,
                    c = function() {
                        this._load_nodes(nodes, callback, !0)
                    },
                    m = this._model.data,
                    tmp = [];
                for (i = 0, j = nodes.length; i < j; i++) !m[nodes[i]] || (m[nodes[i]].state.loaded || m[nodes[i]].state.failed) && is_callback || (this.is_loading(nodes[i]) || this.load_node(nodes[i], c), r = !1);
                if (r) {
                    for (i = 0, j = nodes.length; i < j; i++) m[nodes[i]] && m[nodes[i]].state.loaded && tmp.push(nodes[i]);
                    callback && !callback.done && (callback.call(this, tmp), callback.done = !0)
                }
            },
            load_all: function(obj, callback) {
                if (obj || (obj = "#"), !(obj = this.get_node(obj))) return !1;
                var i, j, to_load = [],
                    m = this._model.data,
                    c = m[obj.id].children_d;
                for (obj.state && !obj.state.loaded && to_load.push(obj.id), i = 0, j = c.length; i < j; i++) m[c[i]] && m[c[i]].state && !m[c[i]].state.loaded && to_load.push(c[i]);
                to_load.length ? this._load_nodes(to_load, function() {
                    this.load_all(obj, callback)
                }) : (callback && callback.call(this, obj), this.trigger("load_all", {
                    node: obj
                }))
            },
            _load_node: function(obj, callback) {
                var t, s = this.settings.core.data;
                return s ? $.isFunction(s) ? s.call(this, obj, $.proxy(function(d) {
                    !1 === d && callback.call(this, !1), this["string" == typeof d ? "_append_html_data" : "_append_json_data"](obj, "string" == typeof d ? $($.parseHTML(d)).filter(function() {
                        return 3 !== this.nodeType
                    }) : d, function(status) {
                        callback.call(this, status)
                    })
                }, this)) : "object" == typeof s ? s.url ? (s = $.extend(!0, {}, s), $.isFunction(s.url) && (s.url = s.url.call(this, obj)), $.isFunction(s.data) && (s.data = s.data.call(this, obj)), $.ajax(s).done($.proxy(function(d, t, x) {
                    var type = x.getResponseHeader("Content-Type");
                    return type && -1 !== type.indexOf("json") || "object" == typeof d ? this._append_json_data(obj, d, function(status) {
                        callback.call(this, status)
                    }) : type && -1 !== type.indexOf("html") || "string" == typeof d ? this._append_html_data(obj, $($.parseHTML(d)).filter(function() {
                        return 3 !== this.nodeType
                    }), function(status) {
                        callback.call(this, status)
                    }) : (this._data.core.last_error = {
                        error: "ajax",
                        plugin: "core",
                        id: "core_04",
                        reason: "Could not load node",
                        data: JSON.stringify({
                            id: obj.id,
                            xhr: x
                        })
                    }, this.settings.core.error.call(this, this._data.core.last_error), callback.call(this, !1))
                }, this)).fail($.proxy(function(f) {
                    callback.call(this, !1), this._data.core.last_error = {
                        error: "ajax",
                        plugin: "core",
                        id: "core_04",
                        reason: "Could not load node",
                        data: JSON.stringify({
                            id: obj.id,
                            xhr: f
                        })
                    }, this.settings.core.error.call(this, this._data.core.last_error)
                }, this))) : (t = $.isArray(s) || $.isPlainObject(s) ? JSON.parse(JSON.stringify(s)) : s, "#" === obj.id ? this._append_json_data(obj, t, function(status) {
                    callback.call(this, status)
                }) : (this._data.core.last_error = {
                    error: "nodata",
                    plugin: "core",
                    id: "core_05",
                    reason: "Could not load node",
                    data: JSON.stringify({
                        id: obj.id
                    })
                }, this.settings.core.error.call(this, this._data.core.last_error), callback.call(this, !1))) : "string" == typeof s ? "#" === obj.id ? this._append_html_data(obj, $($.parseHTML(s)).filter(function() {
                    return 3 !== this.nodeType
                }), function(status) {
                    callback.call(this, status)
                }) : (this._data.core.last_error = {
                    error: "nodata",
                    plugin: "core",
                    id: "core_06",
                    reason: "Could not load node",
                    data: JSON.stringify({
                        id: obj.id
                    })
                }, this.settings.core.error.call(this, this._data.core.last_error), callback.call(this, !1)) : callback.call(this, !1) : "#" === obj.id ? this._append_html_data(obj, this._data.core.original_container_html.clone(!0), function(status) {
                    callback.call(this, status)
                }) : callback.call(this, !1)
            },
            _node_changed: function(obj) {
                (obj = this.get_node(obj)) && this._model.changed.push(obj.id)
            },
            _append_html_data: function(dom, data, cb) {
                dom = this.get_node(dom), dom.children = [], dom.children_d = [];
                var tmp, i, j, dat = data.is("ul") ? data.children() : data,
                    par = dom.id,
                    chd = [],
                    dpc = [],
                    m = this._model.data,
                    p = m[par],
                    s = this._data.core.selected.length;
                for (dat.each($.proxy(function(i, v) {
                        (tmp = this._parse_model_from_html($(v), par, p.parents.concat())) && (chd.push(tmp), dpc.push(tmp), m[tmp].children_d.length && (dpc = dpc.concat(m[tmp].children_d)))
                    }, this)), p.children = chd, p.children_d = dpc, i = 0, j = p.parents.length; i < j; i++) m[p.parents[i]].children_d = m[p.parents[i]].children_d.concat(dpc);
                this.trigger("model", {
                    nodes: dpc,
                    parent: par
                }), "#" !== par ? (this._node_changed(par), this.redraw()) : (this.get_container_ul().children(".jstree-initial-node").remove(), this.redraw(!0)), this._data.core.selected.length !== s && this.trigger("changed", {
                    action: "model",
                    selected: this._data.core.selected
                }), cb.call(this, !0)
            },
            _append_json_data: function(dom, data, cb, force_processing) {
                if (null !== this.element) {
                    dom = this.get_node(dom), dom.children = [], dom.children_d = [], data.d && "string" == typeof(data = data.d) && (data = JSON.parse(data)), $.isArray(data) || (data = [data]);
                    var w = null,
                        args = {
                            df: this._model.default_state,
                            dat: data,
                            par: dom.id,
                            m: this._model.data,
                            t_id: this._id,
                            t_cnt: this._cnt,
                            sel: this._data.core.selected
                        },
                        func = function(data, undefined) {
                            data.data && (data = data.data);
                            var tmp, i, j, rslt, dat = data.dat,
                                par = data.par,
                                chd = [],
                                dpc = [],
                                add = [],
                                df = data.df,
                                t_id = data.t_id,
                                t_cnt = data.t_cnt,
                                m = data.m,
                                p = m[par],
                                sel = data.sel,
                                parse_flat = function(d, p, ps) {
                                    ps = ps ? ps.concat() : [], p && ps.unshift(p);
                                    var i, j, c, e, tid = d.id.toString(),
                                        tmp = {
                                            id: tid,
                                            text: d.text || "",
                                            icon: d.icon === undefined || d.icon,
                                            parent: p,
                                            parents: ps,
                                            children: d.children || [],
                                            children_d: d.children_d || [],
                                            data: d.data,
                                            state: {},
                                            li_attr: {
                                                id: !1
                                            },
                                            a_attr: {
                                                href: "#"
                                            },
                                            original: !1
                                        };
                                    for (i in df) df.hasOwnProperty(i) && (tmp.state[i] = df[i]);
                                    if (d && d.data && d.data.jstree && d.data.jstree.icon && (tmp.icon = d.data.jstree.icon), tmp.icon !== undefined && null !== tmp.icon && "" !== tmp.icon || (tmp.icon = !0), d && d.data && (tmp.data = d.data, d.data.jstree))
                                        for (i in d.data.jstree) d.data.jstree.hasOwnProperty(i) && (tmp.state[i] = d.data.jstree[i]);
                                    if (d && "object" == typeof d.state)
                                        for (i in d.state) d.state.hasOwnProperty(i) && (tmp.state[i] = d.state[i]);
                                    if (d && "object" == typeof d.li_attr)
                                        for (i in d.li_attr) d.li_attr.hasOwnProperty(i) && (tmp.li_attr[i] = d.li_attr[i]);
                                    if (tmp.li_attr.id || (tmp.li_attr.id = tid), d && "object" == typeof d.a_attr)
                                        for (i in d.a_attr) d.a_attr.hasOwnProperty(i) && (tmp.a_attr[i] = d.a_attr[i]);
                                    for (d && d.children && !0 === d.children && (tmp.state.loaded = !1, tmp.children = [], tmp.children_d = []), m[tmp.id] = tmp, i = 0, j = tmp.children.length; i < j; i++) c = parse_flat(m[tmp.children[i]], tmp.id, ps), e = m[c], tmp.children_d.push(c), e.children_d.length && (tmp.children_d = tmp.children_d.concat(e.children_d));
                                    return delete d.data, delete d.children, m[tmp.id].original = d, tmp.state.selected && add.push(tmp.id), tmp.id
                                },
                                parse_nest = function(d, p, ps) {
                                    ps = ps ? ps.concat() : [], p && ps.unshift(p);
                                    var i, j, c, e, tmp, tid = !1;
                                    do {
                                        tid = "j" + t_id + "_" + ++t_cnt
                                    } while (m[tid]);
                                    tmp = {
                                        id: !1,
                                        text: "string" == typeof d ? d : "",
                                        icon: "object" != typeof d || d.icon === undefined || d.icon,
                                        parent: p,
                                        parents: ps,
                                        children: [],
                                        children_d: [],
                                        data: null,
                                        state: {},
                                        li_attr: {
                                            id: !1
                                        },
                                        a_attr: {
                                            href: "#"
                                        },
                                        original: !1
                                    };
                                    for (i in df) df.hasOwnProperty(i) && (tmp.state[i] = df[i]);
                                    if (d && d.id && (tmp.id = d.id.toString()), d && d.text && (tmp.text = d.text), d && d.data && d.data.jstree && d.data.jstree.icon && (tmp.icon = d.data.jstree.icon), tmp.icon !== undefined && null !== tmp.icon && "" !== tmp.icon || (tmp.icon = !0), d && d.data && (tmp.data = d.data, d.data.jstree))
                                        for (i in d.data.jstree) d.data.jstree.hasOwnProperty(i) && (tmp.state[i] = d.data.jstree[i]);
                                    if (d && "object" == typeof d.state)
                                        for (i in d.state) d.state.hasOwnProperty(i) && (tmp.state[i] = d.state[i]);
                                    if (d && "object" == typeof d.li_attr)
                                        for (i in d.li_attr) d.li_attr.hasOwnProperty(i) && (tmp.li_attr[i] = d.li_attr[i]);
                                    if (tmp.li_attr.id && !tmp.id && (tmp.id = tmp.li_attr.id.toString()), tmp.id || (tmp.id = tid), tmp.li_attr.id || (tmp.li_attr.id = tmp.id), d && "object" == typeof d.a_attr)
                                        for (i in d.a_attr) d.a_attr.hasOwnProperty(i) && (tmp.a_attr[i] = d.a_attr[i]);
                                    if (d && d.children && d.children.length) {
                                        for (i = 0, j = d.children.length; i < j; i++) c = parse_nest(d.children[i], tmp.id, ps), e = m[c], tmp.children.push(c), e.children_d.length && (tmp.children_d = tmp.children_d.concat(e.children_d));
                                        tmp.children_d = tmp.children_d.concat(tmp.children)
                                    }
                                    return d && d.children && !0 === d.children && (tmp.state.loaded = !1, tmp.children = [], tmp.children_d = []), delete d.data, delete d.children, tmp.original = d, m[tmp.id] = tmp, tmp.state.selected && add.push(tmp.id), tmp.id
                                };
                            if (dat.length && dat[0].id !== undefined && dat[0].parent !== undefined) {
                                for (i = 0, j = dat.length; i < j; i++) dat[i].children || (dat[i].children = []), m[dat[i].id.toString()] = dat[i];
                                for (i = 0, j = dat.length; i < j; i++) m[dat[i].parent.toString()].children.push(dat[i].id.toString()), p.children_d.push(dat[i].id.toString());
                                for (i = 0, j = p.children.length; i < j; i++) tmp = parse_flat(m[p.children[i]], par, p.parents.concat()), dpc.push(tmp), m[tmp].children_d.length && (dpc = dpc.concat(m[tmp].children_d));
                                for (i = 0, j = p.parents.length; i < j; i++) m[p.parents[i]].children_d = m[p.parents[i]].children_d.concat(dpc);
                                rslt = {
                                    cnt: t_cnt,
                                    mod: m,
                                    sel: sel,
                                    par: par,
                                    dpc: dpc,
                                    add: add
                                }
                            } else {
                                for (i = 0, j = dat.length; i < j; i++)(tmp = parse_nest(dat[i], par, p.parents.concat())) && (chd.push(tmp), dpc.push(tmp), m[tmp].children_d.length && (dpc = dpc.concat(m[tmp].children_d)));
                                for (p.children = chd, p.children_d = dpc, i = 0, j = p.parents.length; i < j; i++) m[p.parents[i]].children_d = m[p.parents[i]].children_d.concat(dpc);
                                rslt = {
                                    cnt: t_cnt,
                                    mod: m,
                                    sel: sel,
                                    par: par,
                                    dpc: dpc,
                                    add: add
                                }
                            }
                            if ("undefined" != typeof window && void 0 !== window.document) return rslt;
                            postMessage(rslt)
                        },
                        rslt = function(rslt, worker) {
                            if (null !== this.element) {
                                if (this._cnt = rslt.cnt, this._model.data = rslt.mod, worker) {
                                    var i, j, a = rslt.add,
                                        r = rslt.sel,
                                        s = this._data.core.selected.slice(),
                                        m = this._model.data;
                                    if (r.length !== s.length || $.vakata.array_unique(r.concat(s)).length !== r.length) {
                                        for (i = 0, j = r.length; i < j; i++) - 1 === $.inArray(r[i], a) && -1 === $.inArray(r[i], s) && (m[r[i]].state.selected = !1);
                                        for (i = 0, j = s.length; i < j; i++) - 1 === $.inArray(s[i], r) && (m[s[i]].state.selected = !0)
                                    }
                                }
                                rslt.add.length && (this._data.core.selected = this._data.core.selected.concat(rslt.add)), this.trigger("model", {
                                    nodes: rslt.dpc,
                                    parent: rslt.par
                                }), "#" !== rslt.par ? (this._node_changed(rslt.par), this.redraw()) : this.redraw(!0), rslt.add.length && this.trigger("changed", {
                                    action: "model",
                                    selected: this._data.core.selected
                                }), cb.call(this, !0)
                            }
                        };
                    if (this.settings.core.worker && window.Blob && window.URL && window.Worker) try {
                        null === this._wrk && (this._wrk = window.URL.createObjectURL(new window.Blob(["self.onmessage = " + func.toString()], {
                            type: "text/javascript"
                        }))), !this._data.core.working || force_processing ? (this._data.core.working = !0, w = new window.Worker(this._wrk), w.onmessage = $.proxy(function(e) {
                            rslt.call(this, e.data, !0);
                            try {
                                w.terminate(), w = null
                            } catch (ignore) {}
                            this._data.core.worker_queue.length ? this._append_json_data.apply(this, this._data.core.worker_queue.shift()) : this._data.core.working = !1
                        }, this), args.par ? w.postMessage(args) : this._data.core.worker_queue.length ? this._append_json_data.apply(this, this._data.core.worker_queue.shift()) : this._data.core.working = !1) : this._data.core.worker_queue.push([dom, data, cb, !0])
                    } catch (e) {
                        rslt.call(this, func(args), !1), this._data.core.worker_queue.length ? this._append_json_data.apply(this, this._data.core.worker_queue.shift()) : this._data.core.working = !1
                    } else rslt.call(this, func(args), !1)
                }
            },
            _parse_model_from_html: function(d, p, ps) {
                ps = ps ? [].concat(ps) : [], p && ps.unshift(p);
                var c, e, i, tmp, tid, m = this._model.data,
                    data = {
                        id: !1,
                        text: !1,
                        icon: !0,
                        parent: p,
                        parents: ps,
                        children: [],
                        children_d: [],
                        data: null,
                        state: {},
                        li_attr: {
                            id: !1
                        },
                        a_attr: {
                            href: "#"
                        },
                        original: !1
                    };
                for (i in this._model.default_state) this._model.default_state.hasOwnProperty(i) && (data.state[i] = this._model.default_state[i]);
                if (tmp = $.vakata.attributes(d, !0), $.each(tmp, function(i, v) {
                        if (v = $.trim(v), !v.length) return !0;
                        data.li_attr[i] = v, "id" === i && (data.id = v.toString())
                    }), tmp = d.children("a").first(), tmp.length && (tmp = $.vakata.attributes(tmp, !0), $.each(tmp, function(i, v) {
                        v = $.trim(v), v.length && (data.a_attr[i] = v)
                    })), tmp = d.children("a").first().length ? d.children("a").first().clone() : d.clone(), tmp.children("ins, i, ul").remove(), tmp = tmp.html(), tmp = $("<div />").html(tmp), data.text = this.settings.core.force_text ? tmp.text() : tmp.html(), tmp = d.data(), data.data = tmp ? $.extend(!0, {}, tmp) : null, data.state.opened = d.hasClass("jstree-open"), data.state.selected = d.children("a").hasClass("jstree-clicked"), data.state.disabled = d.children("a").hasClass("jstree-disabled"), data.data && data.data.jstree)
                    for (i in data.data.jstree) data.data.jstree.hasOwnProperty(i) && (data.state[i] = data.data.jstree[i]);
                tmp = d.children("a").children(".jstree-themeicon"), tmp.length && (data.icon = !tmp.hasClass("jstree-themeicon-hidden") && tmp.attr("rel")), data.state.icon !== undefined && (data.icon = data.state.icon), data.icon !== undefined && null !== data.icon && "" !== data.icon || (data.icon = !0), tmp = d.children("ul").children("li");
                do {
                    tid = "j" + this._id + "_" + ++this._cnt
                } while (m[tid]);
                return data.id = data.li_attr.id ? data.li_attr.id.toString() : tid, tmp.length ? (tmp.each($.proxy(function(i, v) {
                    c = this._parse_model_from_html($(v), data.id, ps), e = this._model.data[c], data.children.push(c), e.children_d.length && (data.children_d = data.children_d.concat(e.children_d))
                }, this)), data.children_d = data.children_d.concat(data.children)) : d.hasClass("jstree-closed") && (data.state.loaded = !1), data.li_attr.class && (data.li_attr.class = data.li_attr.class.replace("jstree-closed", "").replace("jstree-open", "")), data.a_attr.class && (data.a_attr.class = data.a_attr.class.replace("jstree-clicked", "").replace("jstree-disabled", "")), m[data.id] = data, data.state.selected && this._data.core.selected.push(data.id), data.id
            },
            _parse_model_from_flat_json: function(d, p, ps) {
                ps = ps ? ps.concat() : [], p && ps.unshift(p);
                var i, j, c, e, tid = d.id.toString(),
                    m = this._model.data,
                    df = this._model.default_state,
                    tmp = {
                        id: tid,
                        text: d.text || "",
                        icon: d.icon === undefined || d.icon,
                        parent: p,
                        parents: ps,
                        children: d.children || [],
                        children_d: d.children_d || [],
                        data: d.data,
                        state: {},
                        li_attr: {
                            id: !1
                        },
                        a_attr: {
                            href: "#"
                        },
                        original: !1
                    };
                for (i in df) df.hasOwnProperty(i) && (tmp.state[i] = df[i]);
                if (d && d.data && d.data.jstree && d.data.jstree.icon && (tmp.icon = d.data.jstree.icon), tmp.icon !== undefined && null !== tmp.icon && "" !== tmp.icon || (tmp.icon = !0), d && d.data && (tmp.data = d.data, d.data.jstree))
                    for (i in d.data.jstree) d.data.jstree.hasOwnProperty(i) && (tmp.state[i] = d.data.jstree[i]);
                if (d && "object" == typeof d.state)
                    for (i in d.state) d.state.hasOwnProperty(i) && (tmp.state[i] = d.state[i]);
                if (d && "object" == typeof d.li_attr)
                    for (i in d.li_attr) d.li_attr.hasOwnProperty(i) && (tmp.li_attr[i] = d.li_attr[i]);
                if (tmp.li_attr.id || (tmp.li_attr.id = tid), d && "object" == typeof d.a_attr)
                    for (i in d.a_attr) d.a_attr.hasOwnProperty(i) && (tmp.a_attr[i] = d.a_attr[i]);
                for (d && d.children && !0 === d.children && (tmp.state.loaded = !1, tmp.children = [], tmp.children_d = []), m[tmp.id] = tmp, i = 0, j = tmp.children.length; i < j; i++) c = this._parse_model_from_flat_json(m[tmp.children[i]], tmp.id, ps), e = m[c], tmp.children_d.push(c), e.children_d.length && (tmp.children_d = tmp.children_d.concat(e.children_d));
                return delete d.data, delete d.children, m[tmp.id].original = d, tmp.state.selected && this._data.core.selected.push(tmp.id), tmp.id
            },
            _parse_model_from_json: function(d, p, ps) {
                ps = ps ? ps.concat() : [], p && ps.unshift(p);
                var i, j, c, e, tmp, tid = !1,
                    m = this._model.data,
                    df = this._model.default_state;
                do {
                    tid = "j" + this._id + "_" + ++this._cnt
                } while (m[tid]);
                tmp = {
                    id: !1,
                    text: "string" == typeof d ? d : "",
                    icon: "object" != typeof d || d.icon === undefined || d.icon,
                    parent: p,
                    parents: ps,
                    children: [],
                    children_d: [],
                    data: null,
                    state: {},
                    li_attr: {
                        id: !1
                    },
                    a_attr: {
                        href: "#"
                    },
                    original: !1
                };
                for (i in df) df.hasOwnProperty(i) && (tmp.state[i] = df[i]);
                if (d && d.id && (tmp.id = d.id.toString()), d && d.text && (tmp.text = d.text), d && d.data && d.data.jstree && d.data.jstree.icon && (tmp.icon = d.data.jstree.icon), tmp.icon !== undefined && null !== tmp.icon && "" !== tmp.icon || (tmp.icon = !0), d && d.data && (tmp.data = d.data, d.data.jstree))
                    for (i in d.data.jstree) d.data.jstree.hasOwnProperty(i) && (tmp.state[i] = d.data.jstree[i]);
                if (d && "object" == typeof d.state)
                    for (i in d.state) d.state.hasOwnProperty(i) && (tmp.state[i] = d.state[i]);
                if (d && "object" == typeof d.li_attr)
                    for (i in d.li_attr) d.li_attr.hasOwnProperty(i) && (tmp.li_attr[i] = d.li_attr[i]);
                if (tmp.li_attr.id && !tmp.id && (tmp.id = tmp.li_attr.id.toString()), tmp.id || (tmp.id = tid), tmp.li_attr.id || (tmp.li_attr.id = tmp.id),
                    d && "object" == typeof d.a_attr)
                    for (i in d.a_attr) d.a_attr.hasOwnProperty(i) && (tmp.a_attr[i] = d.a_attr[i]);
                if (d && d.children && d.children.length) {
                    for (i = 0, j = d.children.length; i < j; i++) c = this._parse_model_from_json(d.children[i], tmp.id, ps), e = m[c], tmp.children.push(c), e.children_d.length && (tmp.children_d = tmp.children_d.concat(e.children_d));
                    tmp.children_d = tmp.children_d.concat(tmp.children)
                }
                return d && d.children && !0 === d.children && (tmp.state.loaded = !1, tmp.children = [], tmp.children_d = []), delete d.data, delete d.children, tmp.original = d, m[tmp.id] = tmp, tmp.state.selected && this._data.core.selected.push(tmp.id), tmp.id
            },
            _redraw: function() {
                var tmp, i, j, nodes = this._model.force_full_redraw ? this._model.data["#"].children.concat([]) : this._model.changed.concat([]),
                    f = document.createElement("UL"),
                    fe = this._data.core.focused;
                for (i = 0, j = nodes.length; i < j; i++)(tmp = this.redraw_node(nodes[i], !0, this._model.force_full_redraw)) && this._model.force_full_redraw && f.appendChild(tmp);
                this._model.force_full_redraw && (f.className = this.get_container_ul()[0].className, f.setAttribute("role", "group"), this.element.empty().append(f)), null !== fe && (tmp = this.get_node(fe, !0), tmp && tmp.length && tmp.children(".jstree-anchor")[0] !== document.activeElement ? tmp.children(".jstree-anchor").focus() : this._data.core.focused = null), this._model.force_full_redraw = !1, this._model.changed = [], this.trigger("redraw", {
                    nodes: nodes
                })
            },
            redraw: function(full) {
                full && (this._model.force_full_redraw = !0), this._redraw()
            },
            draw_children: function(node) {
                var obj = this.get_node(node),
                    i = !1,
                    j = !1,
                    k = !1,
                    d = document;
                if (!obj) return !1;
                if ("#" === obj.id) return this.redraw(!0);
                if (!(node = this.get_node(node, !0)) || !node.length) return !1;
                if (node.children(".jstree-children").remove(), node = node[0], obj.children.length && obj.state.loaded) {
                    for (k = d.createElement("UL"), k.setAttribute("role", "group"), k.className = "jstree-children", i = 0, j = obj.children.length; i < j; i++) k.appendChild(this.redraw_node(obj.children[i], !0, !0));
                    node.appendChild(k)
                }
            },
            redraw_node: function(node, deep, is_callback, force_render) {
                var obj = this.get_node(node),
                    par = !1,
                    ind = !1,
                    old = !1,
                    i = !1,
                    j = !1,
                    k = !1,
                    c = "",
                    d = document,
                    m = this._model.data,
                    f = !1,
                    tmp = null,
                    t = 0,
                    l = 0;
                if (!obj) return !1;
                if ("#" === obj.id) return this.redraw(!0);
                if (deep = deep || 0 === obj.children.length, node = document.querySelector ? this.element[0].querySelector("#" + (-1 !== "0123456789".indexOf(obj.id[0]) ? "\\3" + obj.id[0] + " " + obj.id.substr(1).replace($.jstree.idregex, "\\$&") : obj.id.replace($.jstree.idregex, "\\$&"))) : document.getElementById(obj.id)) node = $(node), is_callback || (par = node.parent().parent()[0], par === this.element[0] && (par = null), ind = node.index()), deep || !obj.children.length || node.children(".jstree-children").length || (deep = !0), deep || (old = node.children(".jstree-children")[0]), f = node.children(".jstree-anchor")[0] === document.activeElement, node.remove();
                else if (deep = !0, !is_callback) {
                    if (!(null === (par = "#" !== obj.parent ? $("#" + obj.parent.replace($.jstree.idregex, "\\$&"), this.element)[0] : null) || par && m[obj.parent].state.opened)) return !1;
                    ind = $.inArray(obj.id, null === par ? m["#"].children : m[obj.parent].children)
                }
                node = _node.cloneNode(!0), c = "jstree-node ";
                for (i in obj.li_attr)
                    if (obj.li_attr.hasOwnProperty(i)) {
                        if ("id" === i) continue;
                        "class" !== i ? node.setAttribute(i, obj.li_attr[i]) : c += obj.li_attr[i]
                    } obj.a_attr.id || (obj.a_attr.id = obj.id + "_anchor"), node.setAttribute("aria-selected", !!obj.state.selected), node.setAttribute("aria-level", obj.parents.length), node.setAttribute("aria-labelledby", obj.a_attr.id), obj.state.disabled && node.setAttribute("aria-disabled", !0), obj.state.loaded && !obj.children.length ? c += " jstree-leaf" : (c += obj.state.opened && obj.state.loaded ? " jstree-open" : " jstree-closed", node.setAttribute("aria-expanded", obj.state.opened && obj.state.loaded)), null !== obj.parent && m[obj.parent].children[m[obj.parent].children.length - 1] === obj.id && (c += " jstree-last"), node.id = obj.id, node.className = c, c = (obj.state.selected ? " jstree-clicked" : "") + (obj.state.disabled ? " jstree-disabled" : "");
                for (j in obj.a_attr)
                    if (obj.a_attr.hasOwnProperty(j)) {
                        if ("href" === j && "#" === obj.a_attr[j]) continue;
                        "class" !== j ? node.childNodes[1].setAttribute(j, obj.a_attr[j]) : c += " " + obj.a_attr[j]
                    } if (c.length && (node.childNodes[1].className = "jstree-anchor " + c), (obj.icon && !0 !== obj.icon || !1 === obj.icon) && (!1 === obj.icon ? node.childNodes[1].childNodes[0].className += " jstree-themeicon-hidden" : -1 === obj.icon.indexOf("/") && -1 === obj.icon.indexOf(".") ? node.childNodes[1].childNodes[0].className += " " + obj.icon + " jstree-themeicon-custom" : (node.childNodes[1].childNodes[0].style.backgroundImage = "url(" + obj.icon + ")", node.childNodes[1].childNodes[0].style.backgroundPosition = "center center", node.childNodes[1].childNodes[0].style.backgroundSize = "auto", node.childNodes[1].childNodes[0].className += " jstree-themeicon-custom")), this.settings.core.force_text ? node.childNodes[1].appendChild(d.createTextNode(obj.text)) : node.childNodes[1].innerHTML += obj.text, deep && obj.children.length && (obj.state.opened || force_render) && obj.state.loaded) {
                    for (k = d.createElement("UL"), k.setAttribute("role", "group"), k.className = "jstree-children", i = 0, j = obj.children.length; i < j; i++) k.appendChild(this.redraw_node(obj.children[i], deep, !0));
                    node.appendChild(k)
                }
                if (old && node.appendChild(old), !is_callback) {
                    for (par || (par = this.element[0]), i = 0, j = par.childNodes.length; i < j; i++)
                        if (par.childNodes[i] && par.childNodes[i].className && -1 !== par.childNodes[i].className.indexOf("jstree-children")) {
                            tmp = par.childNodes[i];
                            break
                        } tmp || (tmp = d.createElement("UL"), tmp.setAttribute("role", "group"), tmp.className = "jstree-children", par.appendChild(tmp)), par = tmp, ind < par.childNodes.length ? par.insertBefore(node, par.childNodes[ind]) : par.appendChild(node), f && (t = this.element[0].scrollTop, l = this.element[0].scrollLeft, node.childNodes[1].focus(), this.element[0].scrollTop = t, this.element[0].scrollLeft = l)
                }
                return obj.state.opened && !obj.state.loaded && (obj.state.opened = !1, setTimeout($.proxy(function() {
                    this.open_node(obj.id, !1, 0)
                }, this), 0)), node
            },
            open_node: function(obj, callback, animation) {
                var t1, t2, d, t;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.open_node(obj[t1], callback, animation);
                    return !0
                }
                if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                if (animation = animation === undefined ? this.settings.core.animation : animation, !this.is_closed(obj)) return callback && callback.call(this, obj, !1), !1;
                if (this.is_loaded(obj)) d = this.get_node(obj, !0), t = this, d.length && (animation && d.children(".jstree-children").length, obj.children.length && !this._firstChild(d.children(".jstree-children")[0]) && this.draw_children(obj), animation ? (this.trigger("before_open", {
                    node: obj
                }), d.children(".jstree-children").css("display", "none").end().removeClass("jstree-closed").addClass("jstree-open").attr("aria-expanded", !0).children(".jstree-children").show(), t.trigger("after_open", {
                    node: obj
                })) : (this.trigger("before_open", {
                    node: obj
                }), d[0].className = d[0].className.replace("jstree-closed", "jstree-open"), d[0].setAttribute("aria-expanded", !0))), obj.state.opened = !0, callback && callback.call(this, obj, !0), d.length || this.trigger("before_open", {
                    node: obj
                }), this.trigger("open_node", {
                    node: obj
                }), animation && d.length || this.trigger("after_open", {
                    node: obj
                });
                else {
                    if (this.is_loading(obj)) return setTimeout($.proxy(function() {
                        this.open_node(obj, callback, animation)
                    }, this), 500);
                    this.load_node(obj, function(o, ok) {
                        return ok ? this.open_node(o, callback, animation) : !!callback && callback.call(this, o, !1)
                    })
                }
            },
            _open_to: function(obj) {
                if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                var i, j, p = obj.parents;
                for (i = 0, j = p.length; i < j; i += 1) "#" !== i && this.open_node(p[i], !1, 0);
                return $("#" + obj.id.replace($.jstree.idregex, "\\$&"), this.element)
            },
            close_node: function(obj, animation) {
                var t1, t2, t, d;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.close_node(obj[t1], animation);
                    return !0
                }
                return !(!(obj = this.get_node(obj)) || "#" === obj.id) && (!this.is_closed(obj) && (animation = animation === undefined ? this.settings.core.animation : animation, t = this, d = this.get_node(obj, !0), d.length && (animation ? (d.children(".jstree-children").attr("style", "display:block !important").end().removeClass("jstree-open").addClass("jstree-closed").attr("aria-expanded", !1).children(".jstree-children").hide(), d.children(".jstree-children").remove(), t.trigger("after_close", {
                    node: obj
                })) : (d[0].className = d[0].className.replace("jstree-open", "jstree-closed"), d.attr("aria-expanded", !1).children(".jstree-children").remove())), obj.state.opened = !1, this.trigger("close_node", {
                    node: obj
                }), void(animation && d.length || this.trigger("after_close", {
                    node: obj
                }))))
            },
            toggle_node: function(obj) {
                var t1, t2;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.toggle_node(obj[t1]);
                    return !0
                }
                return this.is_closed(obj) ? this.open_node(obj) : this.is_open(obj) ? this.close_node(obj) : void 0
            },
            open_all: function(obj, animation, original_obj) {
                if (obj || (obj = "#"), !(obj = this.get_node(obj))) return !1;
                var i, j, _this, dom = "#" === obj.id ? this.get_container_ul() : this.get_node(obj, !0);
                if (!dom.length) {
                    for (i = 0, j = obj.children_d.length; i < j; i++) this.is_closed(this._model.data[obj.children_d[i]]) && (this._model.data[obj.children_d[i]].state.opened = !0);
                    return this.trigger("open_all", {
                        node: obj
                    })
                }
                original_obj = original_obj || dom, _this = this, dom = this.is_closed(obj) ? dom.find(".jstree-closed").addBack() : dom.find(".jstree-closed"), dom.each(function() {
                    _this.open_node(this, function(node, status) {
                        status && this.is_parent(node) && this.open_all(node, animation, original_obj)
                    }, animation || 0)
                }), 0 === original_obj.find(".jstree-closed").length && this.trigger("open_all", {
                    node: this.get_node(original_obj)
                })
            },
            close_all: function(obj, animation) {
                if (obj || (obj = "#"), !(obj = this.get_node(obj))) return !1;
                var i, j, dom = "#" === obj.id ? this.get_container_ul() : this.get_node(obj, !0),
                    _this = this;
                if (!dom.length) {
                    for (i = 0, j = obj.children_d.length; i < j; i++) this._model.data[obj.children_d[i]].state.opened = !1;
                    return this.trigger("close_all", {
                        node: obj
                    })
                }
                dom = this.is_open(obj) ? dom.find(".jstree-open").addBack() : dom.find(".jstree-open"), $(dom.get().reverse()).each(function() {
                    _this.close_node(this, animation || 0)
                }), this.trigger("close_all", {
                    node: obj
                })
            },
            is_disabled: function(obj) {
                return (obj = this.get_node(obj)) && obj.state && obj.state.disabled
            },
            enable_node: function(obj) {
                var t1, t2;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.enable_node(obj[t1]);
                    return !0
                }
                if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                obj.state.disabled = !1, this.get_node(obj, !0).children(".jstree-anchor").removeClass("jstree-disabled").attr("aria-disabled", !1), this.trigger("enable_node", {
                    node: obj
                })
            },
            disable_node: function(obj) {
                var t1, t2;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.disable_node(obj[t1]);
                    return !0
                }
                if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                obj.state.disabled = !0, this.get_node(obj, !0).children(".jstree-anchor").addClass("jstree-disabled").attr("aria-disabled", !0), this.trigger("disable_node", {
                    node: obj
                })
            },
            activate_node: function(obj, e) {
                if (this.is_disabled(obj)) return !1;
                if (this._data.core.last_clicked = this._data.core.last_clicked && this._data.core.last_clicked.id !== undefined ? this.get_node(this._data.core.last_clicked.id) : null, this._data.core.last_clicked && !this._data.core.last_clicked.state.selected && (this._data.core.last_clicked = null), !this._data.core.last_clicked && this._data.core.selected.length && (this._data.core.last_clicked = this.get_node(this._data.core.selected[this._data.core.selected.length - 1])), this.settings.core.multiple && (e.metaKey || e.ctrlKey || e.shiftKey) && (!e.shiftKey || this._data.core.last_clicked && this.get_parent(obj) && this.get_parent(obj) === this._data.core.last_clicked.parent))
                    if (e.shiftKey) {
                        var i, j, o = this.get_node(obj).id,
                            l = this._data.core.last_clicked.id,
                            p = this.get_node(this._data.core.last_clicked.parent).children,
                            c = !1;
                        for (i = 0, j = p.length; i < j; i += 1) p[i] === o && (c = !c), p[i] === l && (c = !c), this.is_disabled(p[i]) || !c && p[i] !== o && p[i] !== l ? this.deselect_node(p[i], !0, e) : this.select_node(p[i], !0, !1, e);
                        this.trigger("changed", {
                            action: "select_node",
                            node: this.get_node(obj),
                            selected: this._data.core.selected,
                            event: e
                        })
                    } else this.is_selected(obj) ? this.deselect_node(obj, !1, e) : this.select_node(obj, !1, !1, e);
                else !this.settings.core.multiple && (e.metaKey || e.ctrlKey || e.shiftKey) && this.is_selected(obj) ? this.deselect_node(obj, !1, e) : (this.deselect_all(!0), this.select_node(obj, !1, !1, e), this._data.core.last_clicked = this.get_node(obj));
                this.trigger("activate_node", {
                    node: this.get_node(obj)
                })
            },
            hover_node: function(obj) {
                if (!(obj = this.get_node(obj, !0)) || !obj.length || obj.children(".jstree-hovered").length) return !1;
                var o = this.element.find(".jstree-hovered"),
                    t = this.element;
                o && o.length && this.dehover_node(o), obj.children(".jstree-anchor").addClass("jstree-hovered"), this.trigger("hover_node", {
                    node: this.get_node(obj)
                }), setTimeout(function() {
                    t.attr("aria-activedescendant", obj[0].id)
                }, 0)
            },
            dehover_node: function(obj) {
                if (!(obj = this.get_node(obj, !0)) || !obj.length || !obj.children(".jstree-hovered").length) return !1;
                obj.children(".jstree-anchor").removeClass("jstree-hovered"), this.trigger("dehover_node", {
                    node: this.get_node(obj)
                })
            },
            select_node: function(obj, supress_event, prevent_open, e) {
                var dom, t1, t2;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.select_node(obj[t1], supress_event, prevent_open, e);
                    return !0
                }
                if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                dom = this.get_node(obj, !0), obj.state.selected || (obj.state.selected = !0, this._data.core.selected.push(obj.id), prevent_open || (dom = this._open_to(obj)), dom && dom.length && dom.attr("aria-selected", !0).children(".jstree-anchor").addClass("jstree-clicked"), this.trigger("select_node", {
                    node: obj,
                    selected: this._data.core.selected,
                    event: e
                }), supress_event || this.trigger("changed", {
                    action: "select_node",
                    node: obj,
                    selected: this._data.core.selected,
                    event: e
                }))
            },
            deselect_node: function(obj, supress_event, e) {
                var t1, t2, dom;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.deselect_node(obj[t1], supress_event, e);
                    return !0
                }
                if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                dom = this.get_node(obj, !0), obj.state.selected && (obj.state.selected = !1, this._data.core.selected = $.vakata.array_remove_item(this._data.core.selected, obj.id), dom.length && dom.attr("aria-selected", !1).children(".jstree-anchor").removeClass("jstree-clicked"), this.trigger("deselect_node", {
                    node: obj,
                    selected: this._data.core.selected,
                    event: e
                }), supress_event || this.trigger("changed", {
                    action: "deselect_node",
                    node: obj,
                    selected: this._data.core.selected,
                    event: e
                }))
            },
            select_all: function(supress_event) {
                var i, j, tmp = this._data.core.selected.concat([]);
                for (this._data.core.selected = this._model.data["#"].children_d.concat(), i = 0, j = this._data.core.selected.length; i < j; i++) this._model.data[this._data.core.selected[i]] && (this._model.data[this._data.core.selected[i]].state.selected = !0);
                this.redraw(!0), this.trigger("select_all", {
                    selected: this._data.core.selected
                }), supress_event || this.trigger("changed", {
                    action: "select_all",
                    selected: this._data.core.selected,
                    old_selection: tmp
                })
            },
            deselect_all: function(supress_event) {
                var i, j, tmp = this._data.core.selected.concat([]);
                for (i = 0, j = this._data.core.selected.length; i < j; i++) this._model.data[this._data.core.selected[i]] && (this._model.data[this._data.core.selected[i]].state.selected = !1);
                this._data.core.selected = [], this.element.find(".jstree-clicked").removeClass("jstree-clicked").parent().attr("aria-selected", !1), this.trigger("deselect_all", {
                    selected: this._data.core.selected,
                    node: tmp
                }), supress_event || this.trigger("changed", {
                    action: "deselect_all",
                    selected: this._data.core.selected,
                    old_selection: tmp
                })
            },
            is_selected: function(obj) {
                return !(!(obj = this.get_node(obj)) || "#" === obj.id) && obj.state.selected
            },
            get_selected: function(full) {
                return full ? $.map(this._data.core.selected, $.proxy(function(i) {
                    return this.get_node(i)
                }, this)) : this._data.core.selected.slice()
            },
            get_top_selected: function(full) {
                var i, j, k, l, tmp = this.get_selected(!0),
                    obj = {};
                for (i = 0, j = tmp.length; i < j; i++) obj[tmp[i].id] = tmp[i];
                for (i = 0, j = tmp.length; i < j; i++)
                    for (k = 0, l = tmp[i].children_d.length; k < l; k++) obj[tmp[i].children_d[k]] && delete obj[tmp[i].children_d[k]];
                tmp = [];
                for (i in obj) obj.hasOwnProperty(i) && tmp.push(i);
                return full ? $.map(tmp, $.proxy(function(i) {
                    return this.get_node(i)
                }, this)) : tmp
            },
            get_bottom_selected: function(full) {
                var i, j, tmp = this.get_selected(!0),
                    obj = [];
                for (i = 0, j = tmp.length; i < j; i++) tmp[i].children.length || obj.push(tmp[i].id);
                return full ? $.map(obj, $.proxy(function(i) {
                    return this.get_node(i)
                }, this)) : obj
            },
            get_state: function() {
                var i, state = {
                    core: {
                        open: [],
                        scroll: {
                            left: this.element.scrollLeft(),
                            top: this.element.scrollTop()
                        },
                        selected: []
                    }
                };
                for (i in this._model.data) this._model.data.hasOwnProperty(i) && "#" !== i && (this._model.data[i].state.opened && state.core.open.push(i), this._model.data[i].state.selected && state.core.selected.push(i));
                return state
            },
            set_state: function(state, callback) {
                if (state) {
                    if (state.core) {
                        var _this, i;
                        if (state.core.open) return $.isArray(state.core.open) && state.core.open.length ? this._load_nodes(state.core.open, function(nodes) {
                            this.open_node(nodes, !1, 0), delete state.core.open, this.set_state(state, callback)
                        }, !0) : (delete state.core.open, this.set_state(state, callback)), !1;
                        if (state.core.scroll) return state.core.scroll && state.core.scroll.left !== undefined && this.element.scrollLeft(state.core.scroll.left), state.core.scroll && state.core.scroll.top !== undefined && this.element.scrollTop(state.core.scroll.top), delete state.core.scroll, this.set_state(state, callback), !1;
                        if (state.core.selected) return _this = this, this.deselect_all(), $.each(state.core.selected, function(i, v) {
                            _this.select_node(v, !1, !0)
                        }), delete state.core.selected, this.set_state(state, callback), !1;
                        for (i in state) state.hasOwnProperty(i) && "core" !== i && -1 === $.inArray(i, this.settings.plugins) && delete state[i];
                        if ($.isEmptyObject(state.core)) return delete state.core, this.set_state(state, callback), !1
                    }
                    return !$.isEmptyObject(state) || (state = null, callback && callback.call(this), this.trigger("set_state"), !1)
                }
                return !1
            },
            refresh: function(skip_loading, forget_state) {
                this._data.core.state = !0 === forget_state ? {} : this.get_state(), forget_state && $.isFunction(forget_state) && (this._data.core.state = forget_state.call(this, this._data.core.state)), this._cnt = 0, this._model.data = {
                    "#": {
                        id: "#",
                        parent: null,
                        parents: [],
                        children: [],
                        children_d: [],
                        state: {
                            loaded: !1
                        }
                    }
                };
                var c = this.get_container_ul()[0].className;
                skip_loading || (this.element.html("<ul class='" + c + "' role='group'><li class='jstree-initial-node jstree-loading jstree-leaf jstree-last' role='treeitem' id='j" + this._id + "_loading'><i class='jstree-icon jstree-ocl'></i><a class='jstree-anchor' href='#'><i class='jstree-icon jstree-themeicon-hidden'></i>" + this.get_string("Loading ...") + "</a></li></ul>"), this.element.attr("aria-activedescendant", "j" + this._id + "_loading")), this.load_node("#", function(o, s) {
                    s && (this.get_container_ul()[0].className = c, this._firstChild(this.get_container_ul()[0]) && this.element.attr("aria-activedescendant", this._firstChild(this.get_container_ul()[0]).id), this.set_state($.extend(!0, {}, this._data.core.state), function() {
                        this.trigger("refresh")
                    })), this._data.core.state = null
                })
            },
            refresh_node: function(obj) {
                if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                var opened = [],
                    to_load = [];
                this._data.core.selected.concat([]);
                to_load.push(obj.id), !0 === obj.state.opened && opened.push(obj.id), this.get_node(obj, !0).find(".jstree-open").each(function() {
                    opened.push(this.id)
                }), this._load_nodes(to_load, $.proxy(function(nodes) {
                    this.open_node(opened, !1, 0), this.select_node(this._data.core.selected), this.trigger("refresh_node", {
                        node: obj,
                        nodes: nodes
                    })
                }, this))
            },
            set_id: function(obj, id) {
                if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                var i, j, m = this._model.data;
                for (id = id.toString(), m[obj.parent].children[$.inArray(obj.id, m[obj.parent].children)] = id, i = 0, j = obj.parents.length; i < j; i++) m[obj.parents[i]].children_d[$.inArray(obj.id, m[obj.parents[i]].children_d)] = id;
                for (i = 0, j = obj.children.length; i < j; i++) m[obj.children[i]].parent = id;
                for (i = 0, j = obj.children_d.length; i < j; i++) m[obj.children_d[i]].parents[$.inArray(obj.id, m[obj.children_d[i]].parents)] = id;
                return i = $.inArray(obj.id, this._data.core.selected), -1 !== i && (this._data.core.selected[i] = id), i = this.get_node(obj.id, !0), i && (i.attr("id", id).children(".jstree-anchor").attr("id", id + "_anchor").end().attr("aria-labelledby", id + "_anchor"), this.element.attr("aria-activedescendant") === obj.id && this.element.attr("aria-activedescendant", id)), delete m[obj.id], obj.id = id, obj.li_attr.id = id, m[id] = obj, !0
            },
            get_text: function(obj) {
                return !(!(obj = this.get_node(obj)) || "#" === obj.id) && obj.text
            },
            set_text: function(obj, val) {
                var t1, t2;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.set_text(obj[t1], val);
                    return !0
                }
                return !(!(obj = this.get_node(obj)) || "#" === obj.id) && (obj.text = val, this.get_node(obj, !0).length && this.redraw_node(obj.id), this.trigger("set_text", {
                    obj: obj,
                    text: val
                }), !0)
            },
            get_json: function(obj, options, flat) {
                if (!(obj = this.get_node(obj || "#"))) return !1;
                options && options.flat && !flat && (flat = []);
                var i, j, tmp = {
                    id: obj.id,
                    text: obj.text,
                    icon: this.get_icon(obj),
                    li_attr: $.extend(!0, {}, obj.li_attr),
                    a_attr: $.extend(!0, {}, obj.a_attr),
                    state: {},
                    data: (!options || !options.no_data) && $.extend(!0, {}, obj.data)
                };
                if (options && options.flat ? tmp.parent = obj.parent : tmp.children = [], !options || !options.no_state)
                    for (i in obj.state) obj.state.hasOwnProperty(i) && (tmp.state[i] = obj.state[i]);
                if (options && options.no_id && (delete tmp.id, tmp.li_attr && tmp.li_attr.id && delete tmp.li_attr.id, tmp.a_attr && tmp.a_attr.id && delete tmp.a_attr.id), options && options.flat && "#" !== obj.id && flat.push(tmp), !options || !options.no_children)
                    for (i = 0, j = obj.children.length; i < j; i++) options && options.flat ? this.get_json(obj.children[i], options, flat) : tmp.children.push(this.get_json(obj.children[i], options));
                return options && options.flat ? flat : "#" === obj.id ? tmp.children : tmp
            },
            create_node: function(par, node, pos, callback, is_loaded) {
                if (null === par && (par = "#"), !(par = this.get_node(par))) return !1;
                if (pos = pos === undefined ? "last" : pos, !pos.toString().match(/^(before|after)$/) && !is_loaded && !this.is_loaded(par)) return this.load_node(par, function() {
                    this.create_node(par, node, pos, callback, !0)
                });
                node || (node = {
                    text: this.get_string("New node")
                }), "string" == typeof node && (node = {
                    text: node
                }), node.text === undefined && (node.text = this.get_string("New node"));
                var tmp, dpc, i, j;
                switch ("#" === par.id && ("before" === pos && (pos = "first"), "after" === pos && (pos = "last")), pos) {
                    case "before":
                        tmp = this.get_node(par.parent), pos = $.inArray(par.id, tmp.children), par = tmp;
                        break;
                    case "after":
                        tmp = this.get_node(par.parent), pos = $.inArray(par.id, tmp.children) + 1, par = tmp;
                        break;
                    case "inside":
                    case "first":
                        pos = 0;
                        break;
                    case "last":
                        pos = par.children.length;
                        break;
                    default:
                        pos || (pos = 0)
                }
                if (pos > par.children.length && (pos = par.children.length), node.id || (node.id = !0), !this.check("create_node", node, par, pos)) return this.settings.core.error.call(this, this._data.core.last_error), !1;
                if (!0 === node.id && delete node.id, !(node = this._parse_model_from_json(node, par.id, par.parents.concat()))) return !1;
                for (tmp = this.get_node(node), dpc = [], dpc.push(node), dpc = dpc.concat(tmp.children_d), this.trigger("model", {
                        nodes: dpc,
                        parent: par.id
                    }), par.children_d = par.children_d.concat(dpc), i = 0, j = par.parents.length; i < j; i++) this._model.data[par.parents[i]].children_d = this._model.data[par.parents[i]].children_d.concat(dpc);
                for (node = tmp, tmp = [], i = 0, j = par.children.length; i < j; i++) tmp[i >= pos ? i + 1 : i] = par.children[i];
                return tmp[pos] = node.id, par.children = tmp, this.redraw_node(par, !0), callback && callback.call(this, this.get_node(node)), this.trigger("create_node", {
                    node: this.get_node(node),
                    parent: par.id,
                    position: pos
                }), node.id
            },
            rename_node: function(obj, val) {
                var t1, t2, old;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.rename_node(obj[t1], val);
                    return !0
                }
                return !(!(obj = this.get_node(obj)) || "#" === obj.id) && (old = obj.text, this.check("rename_node", obj, this.get_parent(obj), val) ? (this.set_text(obj, val), this.trigger("rename_node", {
                    node: obj,
                    text: val,
                    old: old
                }), !0) : (this.settings.core.error.call(this, this._data.core.last_error), !1))
            },
            delete_node: function(obj) {
                var t1, t2, par, pos, tmp, i, j, k, l, c;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.delete_node(obj[t1]);
                    return !0
                }
                if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                if (par = this.get_node(obj.parent), pos = $.inArray(obj.id, par.children), c = !1, !this.check("delete_node", obj, par, pos)) return this.settings.core.error.call(this, this._data.core.last_error), !1;
                for (-1 !== pos && (par.children = $.vakata.array_remove(par.children, pos)), tmp = obj.children_d.concat([]), tmp.push(obj.id), k = 0, l = tmp.length; k < l; k++) {
                    for (i = 0, j = obj.parents.length; i < j; i++) - 1 !== (pos = $.inArray(tmp[k], this._model.data[obj.parents[i]].children_d)) && (this._model.data[obj.parents[i]].children_d = $.vakata.array_remove(this._model.data[obj.parents[i]].children_d, pos));
                    this._model.data[tmp[k]].state.selected && (c = !0, -1 !== (pos = $.inArray(tmp[k], this._data.core.selected)) && (this._data.core.selected = $.vakata.array_remove(this._data.core.selected, pos)))
                }
                for (this.trigger("delete_node", {
                        node: obj,
                        parent: par.id
                    }), c && this.trigger("changed", {
                        action: "delete_node",
                        node: obj,
                        selected: this._data.core.selected,
                        parent: par.id
                    }), k = 0, l = tmp.length; k < l; k++) delete this._model.data[tmp[k]];
                return this.redraw_node(par, !0), !0
            },
            check: function(chk, obj, par, pos, more) {
                obj = obj && obj.id ? obj : this.get_node(obj), par = par && par.id ? par : this.get_node(par);
                var tmp = chk.match(/^move_node|copy_node|create_node$/i) ? par : obj,
                    chc = this.settings.core.check_callback;
                return "move_node" !== chk && "copy_node" !== chk || more && more.is_multi || obj.id !== par.id && $.inArray(obj.id, par.children) !== pos && -1 === $.inArray(par.id, obj.children_d) ? (tmp && tmp.data && (tmp = tmp.data), tmp && tmp.functions && (!1 === tmp.functions[chk] || !0 === tmp.functions[chk]) ? (!1 === tmp.functions[chk] && (this._data.core.last_error = {
                    error: "check",
                    plugin: "core",
                    id: "core_02",
                    reason: "Node data prevents function: " + chk,
                    data: JSON.stringify({
                        chk: chk,
                        pos: pos,
                        obj: !(!obj || !obj.id) && obj.id,
                        par: !(!par || !par.id) && par.id
                    })
                }), tmp.functions[chk]) : !(!1 === chc || $.isFunction(chc) && !1 === chc.call(this, chk, obj, par, pos, more) || chc && !1 === chc[chk]) || (this._data.core.last_error = {
                    error: "check",
                    plugin: "core",
                    id: "core_03",
                    reason: "User config for core.check_callback prevents function: " + chk,
                    data: JSON.stringify({
                        chk: chk,
                        pos: pos,
                        obj: !(!obj || !obj.id) && obj.id,
                        par: !(!par || !par.id) && par.id
                    })
                }, !1)) : (this._data.core.last_error = {
                    error: "check",
                    plugin: "core",
                    id: "core_01",
                    reason: "Moving parent inside child",
                    data: JSON.stringify({
                        chk: chk,
                        pos: pos,
                        obj: !(!obj || !obj.id) && obj.id,
                        par: !(!par || !par.id) && par.id
                    })
                }, !1)
            },
            last_error: function() {
                return this._data.core.last_error
            },
            move_node: function(obj, par, pos, callback, is_loaded, skip_redraw, origin) {
                var t1, t2, old_par, old_pos, new_par, old_ins, is_multi, dpc, tmp, i, j, k, l, p;
                if (par = this.get_node(par), pos = pos === undefined ? 0 : pos, !par) return !1;
                if (!pos.toString().match(/^(before|after)$/) && !is_loaded && !this.is_loaded(par)) return this.load_node(par, function() {
                    this.move_node(obj, par, pos, callback, !0, !1, origin)
                });
                if ($.isArray(obj)) {
                    if (1 !== obj.length) {
                        for (t1 = 0, t2 = obj.length; t1 < t2; t1++)(tmp = this.move_node(obj[t1], par, pos, callback, is_loaded, !1, origin)) && (par = tmp, pos = "after");
                        return this.redraw(), !0
                    }
                    obj = obj[0]
                }
                if (!(obj = obj && obj.id ? obj : this.get_node(obj)) || "#" === obj.id) return !1;
                if (old_par = (obj.parent || "#").toString(), new_par = pos.toString().match(/^(before|after)$/) && "#" !== par.id ? this.get_node(par.parent) : par, old_ins = origin || (this._model.data[obj.id] ? this : $.jstree.reference(obj.id)), is_multi = !old_ins || !old_ins._id || this._id !== old_ins._id, old_pos = old_ins && old_ins._id && old_par && old_ins._model.data[old_par] && old_ins._model.data[old_par].children ? $.inArray(obj.id, old_ins._model.data[old_par].children) : -1, old_ins && old_ins._id && (obj = old_ins._model.data[obj.id]), is_multi) return !!(tmp = this.copy_node(obj, par, pos, callback, is_loaded, !1, origin)) && (old_ins && old_ins.delete_node(obj), tmp);
                switch ("#" === par.id && ("before" === pos && (pos = "first"), "after" === pos && (pos = "last")), pos) {
                    case "before":
                        pos = $.inArray(par.id, new_par.children);
                        break;
                    case "after":
                        pos = $.inArray(par.id, new_par.children) + 1;
                        break;
                    case "inside":
                    case "first":
                        pos = 0;
                        break;
                    case "last":
                        pos = new_par.children.length;
                        break;
                    default:
                        pos || (pos = 0)
                }
                if (pos > new_par.children.length && (pos = new_par.children.length), !this.check("move_node", obj, new_par, pos, {
                        core: !0,
                        origin: origin,
                        is_multi: old_ins && old_ins._id && old_ins._id !== this._id,
                        is_foreign: !old_ins || !old_ins._id
                    })) return this.settings.core.error.call(this, this._data.core.last_error), !1;
                if (obj.parent === new_par.id) {
                    for (dpc = new_par.children.concat(), tmp = $.inArray(obj.id, dpc), -1 !== tmp && (dpc = $.vakata.array_remove(dpc, tmp), pos > tmp && pos--), tmp = [], i = 0, j = dpc.length; i < j; i++) tmp[i >= pos ? i + 1 : i] = dpc[i];
                    tmp[pos] = obj.id, new_par.children = tmp, this._node_changed(new_par.id), this.redraw("#" === new_par.id)
                } else {
                    for (tmp = obj.children_d.concat(), tmp.push(obj.id), i = 0, j = obj.parents.length; i < j; i++) {
                        for (dpc = [], p = old_ins._model.data[obj.parents[i]].children_d, k = 0, l = p.length; k < l; k++) - 1 === $.inArray(p[k], tmp) && dpc.push(p[k]);
                        old_ins._model.data[obj.parents[i]].children_d = dpc
                    }
                    for (old_ins._model.data[old_par].children = $.vakata.array_remove_item(old_ins._model.data[old_par].children, obj.id), i = 0, j = new_par.parents.length; i < j; i++) this._model.data[new_par.parents[i]].children_d = this._model.data[new_par.parents[i]].children_d.concat(tmp);
                    for (dpc = [], i = 0, j = new_par.children.length; i < j; i++) dpc[i >= pos ? i + 1 : i] = new_par.children[i];
                    for (dpc[pos] = obj.id, new_par.children = dpc, new_par.children_d.push(obj.id), new_par.children_d = new_par.children_d.concat(obj.children_d), obj.parent = new_par.id, tmp = new_par.parents.concat(), tmp.unshift(new_par.id), p = obj.parents.length, obj.parents = tmp, tmp = tmp.concat(), i = 0, j = obj.children_d.length; i < j; i++) this._model.data[obj.children_d[i]].parents = this._model.data[obj.children_d[i]].parents.slice(0, -1 * p), Array.prototype.push.apply(this._model.data[obj.children_d[i]].parents, tmp);
                    "#" !== old_par && "#" !== new_par.id || (this._model.force_full_redraw = !0), this._model.force_full_redraw || (this._node_changed(old_par), this._node_changed(new_par.id)), skip_redraw || this.redraw()
                }
                return callback && callback.call(this, obj, new_par, pos), this.trigger("move_node", {
                    node: obj,
                    parent: new_par.id,
                    position: pos,
                    old_parent: old_par,
                    old_position: old_pos,
                    is_multi: old_ins && old_ins._id && old_ins._id !== this._id,
                    is_foreign: !old_ins || !old_ins._id,
                    old_instance: old_ins,
                    new_instance: this
                }), obj.id
            },
            copy_node: function(obj, par, pos, callback, is_loaded, skip_redraw, origin) {
                var t1, t2, dpc, tmp, i, j, node, old_par, new_par, old_ins;
                if (par = this.get_node(par), pos = pos === undefined ? 0 : pos, !par) return !1;
                if (!pos.toString().match(/^(before|after)$/) && !is_loaded && !this.is_loaded(par)) return this.load_node(par, function() {
                    this.copy_node(obj, par, pos, callback, !0, !1, origin)
                });
                if ($.isArray(obj)) {
                    if (1 !== obj.length) {
                        for (t1 = 0, t2 = obj.length; t1 < t2; t1++)(tmp = this.copy_node(obj[t1], par, pos, callback, is_loaded, !0, origin)) && (par = tmp, pos = "after");
                        return this.redraw(), !0
                    }
                    obj = obj[0]
                }
                if (!(obj = obj && obj.id ? obj : this.get_node(obj)) || "#" === obj.id) return !1;
                switch (old_par = (obj.parent || "#").toString(), new_par = pos.toString().match(/^(before|after)$/) && "#" !== par.id ? this.get_node(par.parent) : par, old_ins = origin || (this._model.data[obj.id] ? this : $.jstree.reference(obj.id)), !old_ins || !old_ins._id || this._id !== old_ins._id, old_ins && old_ins._id && (obj = old_ins._model.data[obj.id]), "#" === par.id && ("before" === pos && (pos = "first"), "after" === pos && (pos = "last")), pos) {
                    case "before":
                        pos = $.inArray(par.id, new_par.children);
                        break;
                    case "after":
                        pos = $.inArray(par.id, new_par.children) + 1;
                        break;
                    case "inside":
                    case "first":
                        pos = 0;
                        break;
                    case "last":
                        pos = new_par.children.length;
                        break;
                    default:
                        pos || (pos = 0)
                }
                if (pos > new_par.children.length && (pos = new_par.children.length), !this.check("copy_node", obj, new_par, pos, {
                        core: !0,
                        origin: origin,
                        is_multi: old_ins && old_ins._id && old_ins._id !== this._id,
                        is_foreign: !old_ins || !old_ins._id
                    })) return this.settings.core.error.call(this, this._data.core.last_error), !1;
                if (!(node = old_ins ? old_ins.get_json(obj, {
                        no_id: !0,
                        no_data: !0,
                        no_state: !0
                    }) : obj)) return !1;
                if (!0 === node.id && delete node.id, !(node = this._parse_model_from_json(node, new_par.id, new_par.parents.concat()))) return !1;
                for (tmp = this.get_node(node), obj && obj.state && !1 === obj.state.loaded && (tmp.state.loaded = !1), dpc = [], dpc.push(node), dpc = dpc.concat(tmp.children_d), this.trigger("model", {
                        nodes: dpc,
                        parent: new_par.id
                    }), i = 0, j = new_par.parents.length; i < j; i++) this._model.data[new_par.parents[i]].children_d = this._model.data[new_par.parents[i]].children_d.concat(dpc);
                for (dpc = [], i = 0,
                    j = new_par.children.length; i < j; i++) dpc[i >= pos ? i + 1 : i] = new_par.children[i];
                return dpc[pos] = tmp.id, new_par.children = dpc, new_par.children_d.push(tmp.id), new_par.children_d = new_par.children_d.concat(tmp.children_d), "#" === new_par.id && (this._model.force_full_redraw = !0), this._model.force_full_redraw || this._node_changed(new_par.id), skip_redraw || this.redraw("#" === new_par.id), callback && callback.call(this, tmp, new_par, pos), this.trigger("copy_node", {
                    node: tmp,
                    original: obj,
                    parent: new_par.id,
                    position: pos,
                    old_parent: old_par,
                    old_position: old_ins && old_ins._id && old_par && old_ins._model.data[old_par] && old_ins._model.data[old_par].children ? $.inArray(obj.id, old_ins._model.data[old_par].children) : -1,
                    is_multi: old_ins && old_ins._id && old_ins._id !== this._id,
                    is_foreign: !old_ins || !old_ins._id,
                    old_instance: old_ins,
                    new_instance: this
                }), tmp.id
            },
            cut: function(obj) {
                if (obj || (obj = this._data.core.selected.concat()), $.isArray(obj) || (obj = [obj]), !obj.length) return !1;
                var o, t1, t2, tmp = [];
                for (t1 = 0, t2 = obj.length; t1 < t2; t1++)(o = this.get_node(obj[t1])) && o.id && "#" !== o.id && tmp.push(o);
                if (!tmp.length) return !1;
                ccp_node = tmp, ccp_inst = this, ccp_mode = "move_node", this.trigger("cut", {
                    node: obj
                })
            },
            copy: function(obj) {
                if (obj || (obj = this._data.core.selected.concat()), $.isArray(obj) || (obj = [obj]), !obj.length) return !1;
                var o, t1, t2, tmp = [];
                for (t1 = 0, t2 = obj.length; t1 < t2; t1++)(o = this.get_node(obj[t1])) && o.id && "#" !== o.id && tmp.push(o);
                if (!tmp.length) return !1;
                ccp_node = tmp, ccp_inst = this, ccp_mode = "copy_node", this.trigger("copy", {
                    node: obj
                })
            },
            get_buffer: function() {
                return {
                    mode: ccp_mode,
                    node: ccp_node,
                    inst: ccp_inst
                }
            },
            can_paste: function() {
                return !1 !== ccp_mode && !1 !== ccp_node
            },
            paste: function(obj, pos) {
                if (!((obj = this.get_node(obj)) && ccp_mode && ccp_mode.match(/^(copy_node|move_node)$/) && ccp_node)) return !1;
                this[ccp_mode](ccp_node, obj, pos, !1, !1, !1, ccp_inst) && this.trigger("paste", {
                    parent: obj.id,
                    node: ccp_node,
                    mode: ccp_mode
                }), ccp_node = !1, ccp_mode = !1, ccp_inst = !1
            },
            clear_buffer: function() {
                ccp_node = !1, ccp_mode = !1, ccp_inst = !1, this.trigger("clear_buffer")
            },
            edit: function(obj, default_text, callback) {
                var rtl, w, a, s, t, h1, h2, fn, tmp;
                return !!(obj = this.get_node(obj)) && (!1 === this.settings.core.check_callback ? (this._data.core.last_error = {
                    error: "check",
                    plugin: "core",
                    id: "core_07",
                    reason: "Could not edit node because of check_callback"
                }, this.settings.core.error.call(this, this._data.core.last_error), !1) : (tmp = obj, default_text = "string" == typeof default_text ? default_text : obj.text, this.set_text(obj, ""), obj = this._open_to(obj), tmp.text = default_text, rtl = this._data.core.rtl, w = this.element.width(), a = obj.children(".jstree-anchor"), s = $("<span>"), t = default_text, h1 = $("<div />", {
                    css: {
                        position: "absolute",
                        top: "-200px",
                        left: rtl ? "0px" : "-1000px",
                        visibility: "hidden"
                    }
                }).appendTo("body"), h2 = $("<input />", {
                    value: t,
                    class: "jstree-rename-input",
                    css: {
                        padding: "0",
                        border: "1px solid silver",
                        "box-sizing": "border-box",
                        display: "inline-block",
                        height: this._data.core.li_height + "px",
                        lineHeight: this._data.core.li_height + "px",
                        width: "150px"
                    },
                    blur: $.proxy(function() {
                        var nv, i = s.children(".jstree-rename-input"),
                            v = i.val(),
                            f = this.settings.core.force_text;
                        "" === v && (v = t), h1.remove(), s.replaceWith(a), s.remove(), t = f ? t : $("<div></div>").append($.parseHTML(t)).html(), this.set_text(obj, t), nv = !!this.rename_node(obj, f ? $("<div></div>").text(v).text() : $("<div></div>").append($.parseHTML(v)).html()), nv || this.set_text(obj, t), callback && callback.call(this, tmp, nv)
                    }, this),
                    keydown: function(event) {
                        var key = event.which;
                        27 === key && (this.value = t), 27 !== key && 13 !== key && 37 !== key && 38 !== key && 39 !== key && 40 !== key && 32 !== key || event.stopImmediatePropagation(), 27 !== key && 13 !== key || (event.preventDefault(), this.blur())
                    },
                    click: function(e) {
                        e.stopImmediatePropagation()
                    },
                    mousedown: function(e) {
                        e.stopImmediatePropagation()
                    },
                    keyup: function(event) {
                        h2.width(Math.min(h1.text("pW" + this.value).width(), w))
                    },
                    keypress: function(event) {
                        if (13 === event.which) return !1
                    }
                }), fn = {
                    fontFamily: a.css("fontFamily") || "",
                    fontSize: a.css("fontSize") || "",
                    fontWeight: a.css("fontWeight") || "",
                    fontStyle: a.css("fontStyle") || "",
                    fontStretch: a.css("fontStretch") || "",
                    fontVariant: a.css("fontVariant") || "",
                    letterSpacing: a.css("letterSpacing") || "",
                    wordSpacing: a.css("wordSpacing") || ""
                }, s.attr("class", a.attr("class")).append(a.contents().clone()).append(h2), a.replaceWith(s), h1.css(fn), void h2.css(fn).width(Math.min(h1.text("pW" + h2[0].value).width(), w))[0].select()))
            },
            set_theme: function(theme_name, theme_url) {
                if (!theme_name) return !1;
                if (!0 === theme_url) {
                    var dir = this.settings.core.themes.dir;
                    dir || (dir = $.jstree.path + "/themes"), theme_url = dir + "/" + theme_name + "/style.css"
                }
                theme_url && -1 === $.inArray(theme_url, themes_loaded) && ($("head").append('<link rel="stylesheet" href="' + theme_url + '" type="text/css" />'), themes_loaded.push(theme_url)), this._data.core.themes.name && this.element.removeClass("jstree-" + this._data.core.themes.name), this._data.core.themes.name = theme_name, this.element.addClass("jstree-" + theme_name), this.element[this.settings.core.themes.responsive ? "addClass" : "removeClass"]("jstree-" + theme_name + "-responsive"), this.trigger("set_theme", {
                    theme: theme_name
                })
            },
            get_theme: function() {
                return this._data.core.themes.name
            },
            set_theme_variant: function(variant_name) {
                this._data.core.themes.variant && this.element.removeClass("jstree-" + this._data.core.themes.name + "-" + this._data.core.themes.variant), this._data.core.themes.variant = variant_name, variant_name && this.element.addClass("jstree-" + this._data.core.themes.name + "-" + this._data.core.themes.variant)
            },
            get_theme_variant: function() {
                return this._data.core.themes.variant
            },
            show_stripes: function() {
                this._data.core.themes.stripes = !0, this.get_container_ul().addClass("jstree-striped")
            },
            hide_stripes: function() {
                this._data.core.themes.stripes = !1, this.get_container_ul().removeClass("jstree-striped")
            },
            toggle_stripes: function() {
                this._data.core.themes.stripes ? this.hide_stripes() : this.show_stripes()
            },
            show_dots: function() {
                this._data.core.themes.dots = !0, this.get_container_ul().removeClass("jstree-no-dots")
            },
            hide_dots: function() {
                this._data.core.themes.dots = !1, this.get_container_ul().addClass("jstree-no-dots")
            },
            toggle_dots: function() {
                this._data.core.themes.dots ? this.hide_dots() : this.show_dots()
            },
            show_icons: function() {
                this._data.core.themes.icons = !0, this.get_container_ul().removeClass("jstree-no-icons")
            },
            hide_icons: function() {
                this._data.core.themes.icons = !1, this.get_container_ul().addClass("jstree-no-icons")
            },
            toggle_icons: function() {
                this._data.core.themes.icons ? this.hide_icons() : this.show_icons()
            },
            set_icon: function(obj, icon) {
                var t1, t2, dom, old;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.set_icon(obj[t1], icon);
                    return !0
                }
                return !(!(obj = this.get_node(obj)) || "#" === obj.id) && (old = obj.icon, obj.icon = !0 === icon || null === icon || icon === undefined || "" === icon || icon, dom = this.get_node(obj, !0).children(".jstree-anchor").children(".jstree-themeicon"), !1 === icon ? this.hide_icon(obj) : !0 === icon || null === icon || icon === undefined || "" === icon ? (dom.removeClass("jstree-themeicon-custom " + old).css("background", "").removeAttr("rel"), !1 === old && this.show_icon(obj)) : -1 === icon.indexOf("/") && -1 === icon.indexOf(".") ? (dom.removeClass(old).css("background", ""), dom.addClass(icon + " jstree-themeicon-custom").attr("rel", icon), !1 === old && this.show_icon(obj)) : (dom.removeClass(old).css("background", ""), dom.addClass("jstree-themeicon-custom").css("background", "url('" + icon + "') center center no-repeat").attr("rel", icon), !1 === old && this.show_icon(obj)), !0)
            },
            get_icon: function(obj) {
                return !(!(obj = this.get_node(obj)) || "#" === obj.id) && obj.icon
            },
            hide_icon: function(obj) {
                var t1, t2;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.hide_icon(obj[t1]);
                    return !0
                }
                return !(!(obj = this.get_node(obj)) || "#" === obj) && (obj.icon = !1, this.get_node(obj, !0).children(".jstree-anchor").children(".jstree-themeicon").addClass("jstree-themeicon-hidden"), !0)
            },
            show_icon: function(obj) {
                var t1, t2, dom;
                if ($.isArray(obj)) {
                    for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.show_icon(obj[t1]);
                    return !0
                }
                return !(!(obj = this.get_node(obj)) || "#" === obj) && (dom = this.get_node(obj, !0), obj.icon = !dom.length || dom.children(".jstree-anchor").children(".jstree-themeicon").attr("rel"), obj.icon || (obj.icon = !0), dom.children(".jstree-anchor").children(".jstree-themeicon").removeClass("jstree-themeicon-hidden"), !0)
            }
        }, $.vakata = {}, $.vakata.attributes = function(node, with_values) {
            node = $(node)[0];
            var attr = with_values ? {} : [];
            return node && node.attributes && $.each(node.attributes, function(i, v) {
                -1 === $.inArray(v.name.toLowerCase(), ["style", "contenteditable", "hasfocus", "tabindex"]) && null !== v.value && "" !== $.trim(v.value) && (with_values ? attr[v.name] = v.value : attr.push(v.name))
            }), attr
        }, $.vakata.array_unique = function(array) {
            var i, l, a = [],
                o = {};
            for (i = 0, l = array.length; i < l; i++) o[array[i]] === undefined && (a.push(array[i]), o[array[i]] = !0);
            return a
        }, $.vakata.array_remove = function(array, from, to) {
            var rest = array.slice((to || from) + 1 || array.length);
            return array.length = from < 0 ? array.length + from : from, array.push.apply(array, rest), array
        }, $.vakata.array_remove_item = function(array, item) {
            var tmp = $.inArray(item, array);
            return -1 !== tmp ? $.vakata.array_remove(array, tmp) : array
        };
        var _i = document.createElement("I");
        _i.className = "jstree-icon jstree-checkbox", _i.setAttribute("role", "presentation"), $.jstree.defaults.checkbox = {
                visible: !0,
                three_state: !0,
                whole_node: !0,
                keep_selected_style: !0,
                cascade: "",
                tie_selection: !0
            }, $.jstree.plugins.checkbox = function(options, parent) {
                this.bind = function() {
                    parent.bind.call(this), this._data.checkbox.uto = !1, this._data.checkbox.selected = [], this.settings.checkbox.three_state && (this.settings.checkbox.cascade = "up+down+undetermined"), this.element.on("init.jstree", $.proxy(function() {
                        this._data.checkbox.visible = this.settings.checkbox.visible, this.settings.checkbox.keep_selected_style || this.element.addClass("jstree-checkbox-no-clicked"), this.settings.checkbox.tie_selection && this.element.addClass("jstree-checkbox-selection")
                    }, this)).on("loading.jstree", $.proxy(function() {
                        this[this._data.checkbox.visible ? "show_checkboxes" : "hide_checkboxes"]()
                    }, this)), -1 !== this.settings.checkbox.cascade.indexOf("undetermined") && this.element.on("changed.jstree uncheck_node.jstree check_node.jstree uncheck_all.jstree check_all.jstree move_node.jstree copy_node.jstree redraw.jstree open_node.jstree", $.proxy(function() {
                        this._data.checkbox.uto && clearTimeout(this._data.checkbox.uto), this._data.checkbox.uto = setTimeout($.proxy(this._undetermined, this), 50)
                    }, this)), this.settings.checkbox.tie_selection || this.element.on("model.jstree", $.proxy(function(e, data) {
                        var i, j, m = this._model.data,
                            dpc = (m[data.parent], data.nodes);
                        for (i = 0, j = dpc.length; i < j; i++) m[dpc[i]].state.checked = m[dpc[i]].original && m[dpc[i]].original.state && m[dpc[i]].original.state.checked, m[dpc[i]].state.checked && this._data.checkbox.selected.push(dpc[i])
                    }, this)), -1 === this.settings.checkbox.cascade.indexOf("up") && -1 === this.settings.checkbox.cascade.indexOf("down") || this.element.on("model.jstree", $.proxy(function(e, data) {
                        var c, i, j, k, l, tmp, m = this._model.data,
                            p = m[data.parent],
                            dpc = data.nodes,
                            chd = [],
                            s = this.settings.checkbox.cascade,
                            t = this.settings.checkbox.tie_selection;
                        if (-1 !== s.indexOf("down"))
                            if (p.state[t ? "selected" : "checked"]) {
                                for (i = 0, j = dpc.length; i < j; i++) m[dpc[i]].state[t ? "selected" : "checked"] = !0;
                                this._data[t ? "core" : "checkbox"].selected = this._data[t ? "core" : "checkbox"].selected.concat(dpc)
                            } else
                                for (i = 0, j = dpc.length; i < j; i++)
                                    if (m[dpc[i]].state[t ? "selected" : "checked"]) {
                                        for (k = 0, l = m[dpc[i]].children_d.length; k < l; k++) m[m[dpc[i]].children_d[k]].state[t ? "selected" : "checked"] = !0;
                                        this._data[t ? "core" : "checkbox"].selected = this._data[t ? "core" : "checkbox"].selected.concat(m[dpc[i]].children_d)
                                    } if (-1 !== s.indexOf("up")) {
                            for (i = 0, j = p.children_d.length; i < j; i++) m[p.children_d[i]].children.length || chd.push(m[p.children_d[i]].parent);
                            for (chd = $.vakata.array_unique(chd), k = 0, l = chd.length; k < l; k++)
                                for (p = m[chd[k]]; p && "#" !== p.id;) {
                                    for (c = 0, i = 0, j = p.children.length; i < j; i++) c += m[p.children[i]].state[t ? "selected" : "checked"];
                                    if (c !== j) break;
                                    p.state[t ? "selected" : "checked"] = !0, this._data[t ? "core" : "checkbox"].selected.push(p.id), (tmp = this.get_node(p, !0)) && tmp.length && tmp.attr("aria-selected", !0).children(".jstree-anchor").addClass(t ? "jstree-clicked" : "jstree-checked"), p = this.get_node(p.parent)
                                }
                        }
                        this._data[t ? "core" : "checkbox"].selected = $.vakata.array_unique(this._data[t ? "core" : "checkbox"].selected)
                    }, this)).on(this.settings.checkbox.tie_selection ? "select_node.jstree" : "check_node.jstree", $.proxy(function(e, data) {
                        var i, j, c, tmp, obj = data.node,
                            m = this._model.data,
                            par = this.get_node(obj.parent),
                            dom = this.get_node(obj, !0),
                            s = this.settings.checkbox.cascade,
                            t = this.settings.checkbox.tie_selection;
                        if (-1 !== s.indexOf("down"))
                            for (this._data[t ? "core" : "checkbox"].selected = $.vakata.array_unique(this._data[t ? "core" : "checkbox"].selected.concat(obj.children_d)), i = 0, j = obj.children_d.length; i < j; i++) tmp = m[obj.children_d[i]], tmp.state[t ? "selected" : "checked"] = !0, tmp && tmp.original && tmp.original.state && tmp.original.state.undetermined && (tmp.original.state.undetermined = !1);
                        if (-1 !== s.indexOf("up"))
                            for (; par && "#" !== par.id;) {
                                for (c = 0, i = 0, j = par.children.length; i < j; i++) c += m[par.children[i]].state[t ? "selected" : "checked"];
                                if (c !== j) break;
                                par.state[t ? "selected" : "checked"] = !0, this._data[t ? "core" : "checkbox"].selected.push(par.id), (tmp = this.get_node(par, !0)) && tmp.length && tmp.attr("aria-selected", !0).children(".jstree-anchor").addClass(t ? "jstree-clicked" : "jstree-checked"), par = this.get_node(par.parent)
                            } - 1 !== s.indexOf("down") && dom.length && dom.find(".jstree-anchor").addClass(t ? "jstree-clicked" : "jstree-checked").parent().attr("aria-selected", !0)
                    }, this)).on(this.settings.checkbox.tie_selection ? "deselect_all.jstree" : "uncheck_all.jstree", $.proxy(function(e, data) {
                        var i, j, tmp, obj = this.get_node("#"),
                            m = this._model.data;
                        for (i = 0, j = obj.children_d.length; i < j; i++)(tmp = m[obj.children_d[i]]) && tmp.original && tmp.original.state && tmp.original.state.undetermined && (tmp.original.state.undetermined = !1)
                    }, this)).on(this.settings.checkbox.tie_selection ? "deselect_node.jstree" : "uncheck_node.jstree", $.proxy(function(e, data) {
                        var i, j, tmp, obj = data.node,
                            dom = this.get_node(obj, !0),
                            s = this.settings.checkbox.cascade,
                            t = this.settings.checkbox.tie_selection;
                        if (obj && obj.original && obj.original.state && obj.original.state.undetermined && (obj.original.state.undetermined = !1), -1 !== s.indexOf("down"))
                            for (i = 0, j = obj.children_d.length; i < j; i++) tmp = this._model.data[obj.children_d[i]], tmp.state[t ? "selected" : "checked"] = !1, tmp && tmp.original && tmp.original.state && tmp.original.state.undetermined && (tmp.original.state.undetermined = !1);
                        if (-1 !== s.indexOf("up"))
                            for (i = 0, j = obj.parents.length; i < j; i++) tmp = this._model.data[obj.parents[i]], tmp.state[t ? "selected" : "checked"] = !1, tmp && tmp.original && tmp.original.state && tmp.original.state.undetermined && (tmp.original.state.undetermined = !1), (tmp = this.get_node(obj.parents[i], !0)) && tmp.length && tmp.attr("aria-selected", !1).children(".jstree-anchor").removeClass(t ? "jstree-clicked" : "jstree-checked");
                        for (tmp = [], i = 0, j = this._data[t ? "core" : "checkbox"].selected.length; i < j; i++) - 1 !== s.indexOf("down") && -1 !== $.inArray(this._data[t ? "core" : "checkbox"].selected[i], obj.children_d) || -1 !== s.indexOf("up") && -1 !== $.inArray(this._data[t ? "core" : "checkbox"].selected[i], obj.parents) || tmp.push(this._data[t ? "core" : "checkbox"].selected[i]);
                        this._data[t ? "core" : "checkbox"].selected = $.vakata.array_unique(tmp), -1 !== s.indexOf("down") && dom.length && dom.find(".jstree-anchor").removeClass(t ? "jstree-clicked" : "jstree-checked").parent().attr("aria-selected", !1)
                    }, this)), -1 !== this.settings.checkbox.cascade.indexOf("up") && this.element.on("delete_node.jstree", $.proxy(function(e, data) {
                        for (var i, j, c, tmp, p = this.get_node(data.parent), m = this._model.data, t = this.settings.checkbox.tie_selection; p && "#" !== p.id;) {
                            for (c = 0, i = 0, j = p.children.length; i < j; i++) c += m[p.children[i]].state[t ? "selected" : "checked"];
                            if (c !== j) break;
                            p.state[t ? "selected" : "checked"] = !0, this._data[t ? "core" : "checkbox"].selected.push(p.id), (tmp = this.get_node(p, !0)) && tmp.length && tmp.attr("aria-selected", !0).children(".jstree-anchor").addClass(t ? "jstree-clicked" : "jstree-checked"), p = this.get_node(p.parent)
                        }
                    }, this)).on("move_node.jstree", $.proxy(function(e, data) {
                        var p, c, i, j, tmp, is_multi = data.is_multi,
                            old_par = data.old_parent,
                            new_par = this.get_node(data.parent),
                            m = this._model.data,
                            t = this.settings.checkbox.tie_selection;
                        if (!is_multi)
                            for (p = this.get_node(old_par); p && "#" !== p.id;) {
                                for (c = 0, i = 0, j = p.children.length; i < j; i++) c += m[p.children[i]].state[t ? "selected" : "checked"];
                                if (c !== j) break;
                                p.state[t ? "selected" : "checked"] = !0, this._data[t ? "core" : "checkbox"].selected.push(p.id), (tmp = this.get_node(p, !0)) && tmp.length && tmp.attr("aria-selected", !0).children(".jstree-anchor").addClass(t ? "jstree-clicked" : "jstree-checked"), p = this.get_node(p.parent)
                            }
                        for (p = new_par; p && "#" !== p.id;) {
                            for (c = 0, i = 0, j = p.children.length; i < j; i++) c += m[p.children[i]].state[t ? "selected" : "checked"];
                            if (c === j) p.state[t ? "selected" : "checked"] || (p.state[t ? "selected" : "checked"] = !0, this._data[t ? "core" : "checkbox"].selected.push(p.id), (tmp = this.get_node(p, !0)) && tmp.length && tmp.attr("aria-selected", !0).children(".jstree-anchor").addClass(t ? "jstree-clicked" : "jstree-checked"));
                            else {
                                if (!p.state[t ? "selected" : "checked"]) break;
                                p.state[t ? "selected" : "checked"] = !1, this._data[t ? "core" : "checkbox"].selected = $.vakata.array_remove_item(this._data[t ? "core" : "checkbox"].selected, p.id), (tmp = this.get_node(p, !0)) && tmp.length && tmp.attr("aria-selected", !1).children(".jstree-anchor").removeClass(t ? "jstree-clicked" : "jstree-checked")
                            }
                            p = this.get_node(p.parent)
                        }
                    }, this))
                }, this._undetermined = function() {
                    if (null !== this.element) {
                        var i, j, k, l, o = {},
                            m = this._model.data,
                            t = this.settings.checkbox.tie_selection,
                            s = this._data[t ? "core" : "checkbox"].selected,
                            p = [],
                            tt = this;
                        for (i = 0, j = s.length; i < j; i++)
                            if (m[s[i]] && m[s[i]].parents)
                                for (k = 0, l = m[s[i]].parents.length; k < l; k++) o[m[s[i]].parents[k]] === undefined && "#" !== m[s[i]].parents[k] && (o[m[s[i]].parents[k]] = !0, p.push(m[s[i]].parents[k]));
                        for (this.element.find(".jstree-closed").not(":has(.jstree-children)").each(function() {
                                var tmp2, tmp = tt.get_node(this);
                                if (tmp.state.loaded) {
                                    for (i = 0, j = tmp.children_d.length; i < j; i++)
                                        if (tmp2 = m[tmp.children_d[i]], !tmp2.state.loaded && tmp2.original && tmp2.original.state && tmp2.original.state.undetermined && !0 === tmp2.original.state.undetermined)
                                            for (o[tmp2.id] === undefined && "#" !== tmp2.id && (o[tmp2.id] = !0, p.push(tmp2.id)), k = 0, l = tmp2.parents.length; k < l; k++) o[tmp2.parents[k]] === undefined && "#" !== tmp2.parents[k] && (o[tmp2.parents[k]] = !0, p.push(tmp2.parents[k]))
                                } else if (tmp.original && tmp.original.state && tmp.original.state.undetermined && !0 === tmp.original.state.undetermined)
                                    for (o[tmp.id] === undefined && "#" !== tmp.id && (o[tmp.id] = !0, p.push(tmp.id)), k = 0, l = tmp.parents.length; k < l; k++) o[tmp.parents[k]] === undefined && "#" !== tmp.parents[k] && (o[tmp.parents[k]] = !0, p.push(tmp.parents[k]))
                            }), this.element.find(".jstree-undetermined").removeClass("jstree-undetermined"), i = 0, j = p.length; i < j; i++) m[p[i]].state[t ? "selected" : "checked"] || (s = this.get_node(p[i], !0)) && s.length && s.children(".jstree-anchor").children(".jstree-checkbox").addClass("jstree-undetermined")
                    }
                }, this.redraw_node = function(obj, deep, is_callback, force_render) {
                    if (obj = parent.redraw_node.apply(this, arguments)) {
                        var i, j, tmp = null;
                        for (i = 0, j = obj.childNodes.length; i < j; i++)
                            if (obj.childNodes[i] && obj.childNodes[i].className && -1 !== obj.childNodes[i].className.indexOf("jstree-anchor")) {
                                tmp = obj.childNodes[i];
                                break
                            } tmp && (!this.settings.checkbox.tie_selection && this._model.data[obj.id].state.checked && (tmp.className += " jstree-checked"), tmp.insertBefore(_i.cloneNode(!1), tmp.childNodes[0]))
                    }
                    return is_callback || -1 === this.settings.checkbox.cascade.indexOf("undetermined") || (this._data.checkbox.uto && clearTimeout(this._data.checkbox.uto), this._data.checkbox.uto = setTimeout($.proxy(this._undetermined, this), 50)), obj
                }, this.show_checkboxes = function() {
                    this._data.core.themes.checkboxes = !0, this.get_container_ul().removeClass("jstree-no-checkboxes")
                }, this.hide_checkboxes = function() {
                    this._data.core.themes.checkboxes = !1, this.get_container_ul().addClass("jstree-no-checkboxes")
                }, this.toggle_checkboxes = function() {
                    this._data.core.themes.checkboxes ? this.hide_checkboxes() : this.show_checkboxes()
                }, this.is_undetermined = function(obj) {
                    obj = this.get_node(obj);
                    var i, j, s = this.settings.checkbox.cascade,
                        t = this.settings.checkbox.tie_selection,
                        d = this._data[t ? "core" : "checkbox"].selected,
                        m = this._model.data;
                    if (!obj || !0 === obj.state[t ? "selected" : "checked"] || -1 === s.indexOf("undetermined") || -1 === s.indexOf("down") && -1 === s.indexOf("up")) return !1;
                    if (!obj.state.loaded && !0 === obj.original.state.undetermined) return !0;
                    for (i = 0, j = obj.children_d.length; i < j; i++)
                        if (-1 !== $.inArray(obj.children_d[i], d) || !m[obj.children_d[i]].state.loaded && m[obj.children_d[i]].original.state.undetermined) return !0;
                    return !1
                }, this.activate_node = function(obj, e) {
                    return this.settings.checkbox.tie_selection && (this.settings.checkbox.whole_node || $(e.target).hasClass("jstree-checkbox")) && (e.ctrlKey = !0), this.settings.checkbox.tie_selection || !this.settings.checkbox.whole_node && !$(e.target).hasClass("jstree-checkbox") ? parent.activate_node.call(this, obj, e) : !this.is_disabled(obj) && (this.is_checked(obj) ? this.uncheck_node(obj, e) : this.check_node(obj, e), void this.trigger("activate_node", {
                        node: this.get_node(obj)
                    }))
                }, this.check_node = function(obj, e) {
                    if (this.settings.checkbox.tie_selection) return this.select_node(obj, !1, !0, e);
                    var dom, t1, t2;
                    if ($.isArray(obj)) {
                        for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.check_node(obj[t1], e);
                        return !0
                    }
                    if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                    dom = this.get_node(obj, !0), obj.state.checked || (obj.state.checked = !0, this._data.checkbox.selected.push(obj.id), dom && dom.length && dom.children(".jstree-anchor").addClass("jstree-checked"), this.trigger("check_node", {
                        node: obj,
                        selected: this._data.checkbox.selected,
                        event: e
                    }))
                }, this.uncheck_node = function(obj, e) {
                    if (this.settings.checkbox.tie_selection) return this.deselect_node(obj, !1, e);
                    var t1, t2, dom;
                    if ($.isArray(obj)) {
                        for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.uncheck_node(obj[t1], e);
                        return !0
                    }
                    if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                    dom = this.get_node(obj, !0), obj.state.checked && (obj.state.checked = !1, this._data.checkbox.selected = $.vakata.array_remove_item(this._data.checkbox.selected, obj.id), dom.length && dom.children(".jstree-anchor").removeClass("jstree-checked"), this.trigger("uncheck_node", {
                        node: obj,
                        selected: this._data.checkbox.selected,
                        event: e
                    }))
                }, this.check_all = function() {
                    if (this.settings.checkbox.tie_selection) return this.select_all();
                    var i, j;
                    this._data.checkbox.selected.concat([]);
                    for (this._data.checkbox.selected = this._model.data["#"].children_d.concat(), i = 0, j = this._data.checkbox.selected.length; i < j; i++) this._model.data[this._data.checkbox.selected[i]] && (this._model.data[this._data.checkbox.selected[i]].state.checked = !0);
                    this.redraw(!0), this.trigger("check_all", {
                        selected: this._data.checkbox.selected
                    })
                }, this.uncheck_all = function() {
                    if (this.settings.checkbox.tie_selection) return this.deselect_all();
                    var i, j, tmp = this._data.checkbox.selected.concat([]);
                    for (i = 0, j = this._data.checkbox.selected.length; i < j; i++) this._model.data[this._data.checkbox.selected[i]] && (this._model.data[this._data.checkbox.selected[i]].state.checked = !1);
                    this._data.checkbox.selected = [], this.element.find(".jstree-checked").removeClass("jstree-checked"), this.trigger("uncheck_all", {
                        selected: this._data.checkbox.selected,
                        node: tmp
                    })
                }, this.is_checked = function(obj) {
                    return this.settings.checkbox.tie_selection ? this.is_selected(obj) : !(!(obj = this.get_node(obj)) || "#" === obj.id) && obj.state.checked
                }, this.get_checked = function(full) {
                    return this.settings.checkbox.tie_selection ? this.get_selected(full) : full ? $.map(this._data.checkbox.selected, $.proxy(function(i) {
                        return this.get_node(i)
                    }, this)) : this._data.checkbox.selected
                }, this.get_top_checked = function(full) {
                    if (this.settings.checkbox.tie_selection) return this.get_top_selected(full);
                    var i, j, k, l, tmp = this.get_checked(!0),
                        obj = {};
                    for (i = 0, j = tmp.length; i < j; i++) obj[tmp[i].id] = tmp[i];
                    for (i = 0, j = tmp.length; i < j; i++)
                        for (k = 0, l = tmp[i].children_d.length; k < l; k++) obj[tmp[i].children_d[k]] && delete obj[tmp[i].children_d[k]];
                    tmp = [];
                    for (i in obj) obj.hasOwnProperty(i) && tmp.push(i);
                    return full ? $.map(tmp, $.proxy(function(i) {
                        return this.get_node(i)
                    }, this)) : tmp
                }, this.get_bottom_checked = function(full) {
                    if (this.settings.checkbox.tie_selection) return this.get_bottom_selected(full);
                    var i, j, tmp = this.get_checked(!0),
                        obj = [];
                    for (i = 0, j = tmp.length; i < j; i++) tmp[i].children.length || obj.push(tmp[i].id);
                    return full ? $.map(obj, $.proxy(function(i) {
                        return this.get_node(i)
                    }, this)) : obj
                }, this.load_node = function(obj, callback) {
                    var k, l, tmp;
                    if (!$.isArray(obj) && !this.settings.checkbox.tie_selection && (tmp = this.get_node(obj)) && tmp.state.loaded)
                        for (k = 0, l = tmp.children_d.length; k < l; k++) this._model.data[tmp.children_d[k]].state.checked && (!0, this._data.checkbox.selected = $.vakata.array_remove_item(this._data.checkbox.selected, tmp.children_d[k]));
                    return parent.load_node.apply(this, arguments)
                }, this.get_state = function() {
                    var state = parent.get_state.apply(this, arguments);
                    return this.settings.checkbox.tie_selection ? state : (state.checkbox = this._data.checkbox.selected.slice(), state)
                }, this.set_state = function(state, callback) {
                    var res = parent.set_state.apply(this, arguments);
                    if (res && state.checkbox) {
                        if (!this.settings.checkbox.tie_selection) {
                            this.uncheck_all();
                            var _this = this;
                            $.each(state.checkbox, function(i, v) {
                                _this.check_node(v)
                            })
                        }
                        return delete state.checkbox, this.set_state(state, callback), !1
                    }
                    return res
                }
            }, $.jstree.defaults.contextmenu = {
                select_node: !0,
                show_at_node: !0,
                items: function(o, cb) {
                    return {
                        create: {
                            separator_before: !1,
                            separator_after: !0,
                            _disabled: !1,
                            label: "Create",
                            action: function(data) {
                                var inst = $.jstree.reference(data.reference),
                                    obj = inst.get_node(data.reference);
                                inst.create_node(obj, {}, "last", function(new_node) {
                                    setTimeout(function() {
                                        inst.edit(new_node)
                                    }, 0)
                                })
                            }
                        },
                        rename: {
                            separator_before: !1,
                            separator_after: !1,
                            _disabled: !1,
                            label: "Rename",
                            action: function(data) {
                                var inst = $.jstree.reference(data.reference),
                                    obj = inst.get_node(data.reference);
                                inst.edit(obj)
                            }
                        },
                        remove: {
                            separator_before: !1,
                            icon: !1,
                            separator_after: !1,
                            _disabled: !1,
                            label: "Delete",
                            action: function(data) {
                                var inst = $.jstree.reference(data.reference),
                                    obj = inst.get_node(data.reference);
                                inst.is_selected(obj) ? inst.delete_node(inst.get_selected()) : inst.delete_node(obj)
                            }
                        },
                        ccp: {
                            separator_before: !0,
                            icon: !1,
                            separator_after: !1,
                            label: "Edit",
                            action: !1,
                            submenu: {
                                cut: {
                                    separator_before: !1,
                                    separator_after: !1,
                                    label: "Cut",
                                    action: function(data) {
                                        var inst = $.jstree.reference(data.reference),
                                            obj = inst.get_node(data.reference);
                                        inst.is_selected(obj) ? inst.cut(inst.get_top_selected()) : inst.cut(obj)
                                    }
                                },
                                copy: {
                                    separator_before: !1,
                                    icon: !1,
                                    separator_after: !1,
                                    label: "Copy",
                                    action: function(data) {
                                        var inst = $.jstree.reference(data.reference),
                                            obj = inst.get_node(data.reference);
                                        inst.is_selected(obj) ? inst.copy(inst.get_top_selected()) : inst.copy(obj)
                                    }
                                },
                                paste: {
                                    separator_before: !1,
                                    icon: !1,
                                    _disabled: function(data) {
                                        return !$.jstree.reference(data.reference).can_paste()
                                    },
                                    separator_after: !1,
                                    label: "Paste",
                                    action: function(data) {
                                        var inst = $.jstree.reference(data.reference),
                                            obj = inst.get_node(data.reference);
                                        inst.paste(obj)
                                    }
                                }
                            }
                        }
                    }
                }
            }, $.jstree.plugins.contextmenu = function(options, parent) {
                this.bind = function() {
                    parent.bind.call(this);
                    var ex, ey, last_ts = 0,
                        cto = null;
                    this.element.on("contextmenu.jstree", ".jstree-anchor", $.proxy(function(e, data) {
                        e.preventDefault(), last_ts = e.ctrlKey ? +new Date : 0, (data || cto) && (last_ts = +new Date + 1e4), cto && clearTimeout(cto), this.is_loading(e.currentTarget) || this.show_contextmenu(e.currentTarget, e.pageX, e.pageY, e)
                    }, this)).on("click.jstree", ".jstree-anchor", $.proxy(function(e) {
                        this._data.contextmenu.visible && (!last_ts || +new Date - last_ts > 250) && $.vakata.context.hide(), last_ts = 0
                    }, this)).on("touchstart.jstree", ".jstree-anchor", function(e) {
                        e.originalEvent && e.originalEvent.changedTouches && e.originalEvent.changedTouches[0] && (ex = e.pageX, ey = e.pageY, cto = setTimeout(function() {
                            $(e.currentTarget).trigger("contextmenu", !0)
                        }, 750))
                    }).on("touchmove.vakata.jstree", function(e) {
                        cto && e.originalEvent && e.originalEvent.changedTouches && e.originalEvent.changedTouches[0] && (Math.abs(ex - e.pageX) > 50 || Math.abs(ey - e.pageY) > 50) && clearTimeout(cto)
                    }).on("touchend.vakata.jstree", function(e) {
                        cto && clearTimeout(cto)
                    }), $(document).on("context_hide.vakata.jstree", $.proxy(function() {
                        this._data.contextmenu.visible = !1
                    }, this))
                }, this.teardown = function() {
                    this._data.contextmenu.visible && $.vakata.context.hide(), parent.teardown.call(this)
                }, this.show_contextmenu = function(obj, x, y, e) {
                    if (!(obj = this.get_node(obj)) || "#" === obj.id) return !1;
                    var s = this.settings.contextmenu,
                        d = this.get_node(obj, !0),
                        a = d.children(".jstree-anchor"),
                        o = !1,
                        i = !1;
                    (s.show_at_node || x === undefined || y === undefined) && (o = a.offset(), x = o.left, y = o.top + this._data.core.li_height), this.settings.contextmenu.select_node && !this.is_selected(obj) && this.activate_node(obj, e), i = s.items, $.isFunction(i) && (i = i.call(this, obj, $.proxy(function(i) {
                        this._show_contextmenu(obj, x, y, i)
                    }, this))), $.isPlainObject(i) && this._show_contextmenu(obj, x, y, i)
                }, this._show_contextmenu = function(obj, x, y, i) {
                    var d = this.get_node(obj, !0),
                        a = d.children(".jstree-anchor");
                    $(document).one("context_show.vakata.jstree", $.proxy(function(e, data) {
                        var cls = "jstree-contextmenu jstree-" + this.get_theme() + "-contextmenu";
                        $(data.element).addClass(cls)
                    }, this)), this._data.contextmenu.visible = !0, $.vakata.context.show(a, {
                        x: x,
                        y: y
                    }, i), this.trigger("show_contextmenu", {
                        node: obj,
                        x: x,
                        y: y
                    })
                }
            },
            function($) {
                var right_to_left = !1,
                    vakata_context = {
                        element: !1,
                        reference: !1,
                        position_x: 0,
                        position_y: 0,
                        items: [],
                        html: "",
                        is_visible: !1
                    };
                $.vakata.context = {
                    settings: {
                        hide_onmouseleave: 0,
                        icons: !0
                    },
                    _trigger: function(event_name) {
                        $(document).triggerHandler("context_" + event_name + ".vakata", {
                            reference: vakata_context.reference,
                            element: vakata_context.element,
                            position: {
                                x: vakata_context.position_x,
                                y: vakata_context.position_y
                            }
                        })
                    },
                    _execute: function(i) {
                        return !(!(i = vakata_context.items[i]) || i._disabled && (!$.isFunction(i._disabled) || i._disabled({
                            item: i,
                            reference: vakata_context.reference,
                            element: vakata_context.element
                        })) || !i.action) && i.action.call(null, {
                            item: i,
                            reference: vakata_context.reference,
                            element: vakata_context.element,
                            position: {
                                x: vakata_context.position_x,
                                y: vakata_context.position_y
                            }
                        })
                    },
                    _parse: function(o, is_callback) {
                        if (!o) return !1;
                        is_callback || (vakata_context.html = "", vakata_context.items = []);
                        var tmp, str = "",
                            sep = !1;
                        return is_callback && (str += "<ul>"), $.each(o, function(i, val) {
                            if (!val) return !0;
                            vakata_context.items.push(val), !sep && val.separator_before && (str += "<li class='vakata-context-separator'><a href='#' " + ($.vakata.context.settings.icons ? "" : 'style="margin-left:0px;"') + ">&#160;</a></li>"), sep = !1, str += "<li class='" + (val._class || "") + (!0 === val._disabled || $.isFunction(val._disabled) && val._disabled({
                                item: val,
                                reference: vakata_context.reference,
                                element: vakata_context.element
                            }) ? " vakata-contextmenu-disabled " : "") + "' " + (val.shortcut ? " data-shortcut='" + val.shortcut + "' " : "") + ">", str += "<a href='#' rel='" + (vakata_context.items.length - 1) + "'>", $.vakata.context.settings.icons && (str += "<i ", val.icon && (-1 !== val.icon.indexOf("/") || -1 !== val.icon.indexOf(".") ? str += " style='background:url(\"" + val.icon + "\") center center no-repeat' " : str += " class='" + val.icon + "' "), str += "></i><span class='vakata-contextmenu-sep'>&#160;</span>"), str += ($.isFunction(val.label) ? val.label({
                                item: i,
                                reference: vakata_context.reference,
                                element: vakata_context.element
                            }) : val.label) + (val.shortcut ? ' <span class="vakata-contextmenu-shortcut vakata-contextmenu-shortcut-' + val.shortcut + '">' + (val.shortcut_label || "") + "</span>" : "") + "</a>", val.submenu && (tmp = $.vakata.context._parse(val.submenu, !0)) && (str += tmp), str += "</li>", val.separator_after && (str += "<li class='vakata-context-separator'><a href='#' " + ($.vakata.context.settings.icons ? "" : 'style="margin-left:0px;"') + ">&#160;</a></li>", sep = !0)
                        }), str = str.replace(/<li class\='vakata-context-separator'\><\/li\>$/, ""), is_callback && (str += "</ul>"), is_callback || (vakata_context.html = str, $.vakata.context._trigger("parse")), str.length > 10 && str
                    },
                    _show_submenu: function(o) {
                        if (o = $(o), o.length && o.children("ul").length) {
                            var e = o.children("ul"),
                                x = o.offset().left + o.outerWidth(),
                                y = o.offset().top,
                                w = e.width(),
                                h = e.height(),
                                dw = $(window).width() + $(window).scrollLeft(),
                                dh = $(window).height() + $(window).scrollTop();
                            right_to_left ? o[x - (w + 10 + o.outerWidth()) < 0 ? "addClass" : "removeClass"]("vakata-context-left") : o[x + w + 10 > dw ? "addClass" : "removeClass"]("vakata-context-right"), y + h + 10 > dh && e.css("bottom", "-1px"),
                                e.show()
                        }
                    },
                    show: function(reference, position, data) {
                        var o, e, x, y, w, h, dw, dh;
                        switch (vakata_context.element && vakata_context.element.length && vakata_context.element.width(""), !0) {
                            case !position && !reference:
                                return !1;
                            case !!position && !!reference:
                                vakata_context.reference = reference, vakata_context.position_x = position.x, vakata_context.position_y = position.y;
                                break;
                            case !position && !!reference:
                                vakata_context.reference = reference, o = reference.offset(), vakata_context.position_x = o.left + reference.outerHeight(), vakata_context.position_y = o.top;
                                break;
                            case !!position && !reference:
                                vakata_context.position_x = position.x, vakata_context.position_y = position.y
                        }
                        reference && !data && $(reference).data("vakata_contextmenu") && (data = $(reference).data("vakata_contextmenu")), $.vakata.context._parse(data) && vakata_context.element.html(vakata_context.html), vakata_context.items.length && (vakata_context.element.appendTo("body"), e = vakata_context.element, x = vakata_context.position_x, y = vakata_context.position_y, w = e.width(), h = e.height(), dw = $(window).width() + $(window).scrollLeft(), dh = $(window).height() + $(window).scrollTop(), right_to_left && (x -= e.outerWidth() - $(reference).outerWidth()) < $(window).scrollLeft() + 20 && (x = $(window).scrollLeft() + 20), x + w + 20 > dw && (x = dw - (w + 20)), y + h + 20 > dh && (y = dh - (h + 20)), vakata_context.element.css({
                            left: x,
                            top: y
                        }).show().find("a").first().focus().parent().addClass("vakata-context-hover"), vakata_context.is_visible = !0, $.vakata.context._trigger("show"))
                    },
                    hide: function() {
                        vakata_context.is_visible && (vakata_context.element.hide().find("ul").hide().end().find(":focus").blur().end().detach(), vakata_context.is_visible = !1, $.vakata.context._trigger("hide"))
                    }
                }, $(function() {
                    right_to_left = "rtl" === $("body").css("direction");
                    var to = !1;
                    vakata_context.element = $("<ul class='vakata-context'></ul>"), vakata_context.element.on("mouseenter", "li", function(e) {
                        e.stopImmediatePropagation(), $.contains(this, e.relatedTarget) || (to && clearTimeout(to), vakata_context.element.find(".vakata-context-hover").removeClass("vakata-context-hover").end(), $(this).siblings().find("ul").hide().end().end().parentsUntil(".vakata-context", "li").addBack().addClass("vakata-context-hover"), $.vakata.context._show_submenu(this))
                    }).on("mouseleave", "li", function(e) {
                        $.contains(this, e.relatedTarget) || $(this).find(".vakata-context-hover").addBack().removeClass("vakata-context-hover")
                    }).on("mouseleave", function(e) {
                        $(this).find(".vakata-context-hover").removeClass("vakata-context-hover"), $.vakata.context.settings.hide_onmouseleave && (to = setTimeout(function(t) {
                            return function() {
                                $.vakata.context.hide()
                            }
                        }(), $.vakata.context.settings.hide_onmouseleave))
                    }).on("click", "a", function(e) {
                        e.preventDefault(), $(this).blur().parent().hasClass("vakata-context-disabled") || !1 === $.vakata.context._execute($(this).attr("rel")) || $.vakata.context.hide()
                    }).on("keydown", "a", function(e) {
                        var o = null;
                        switch (e.which) {
                            case 13:
                            case 32:
                                e.type = "mouseup", e.preventDefault(), $(e.currentTarget).trigger(e);
                                break;
                            case 37:
                                vakata_context.is_visible && (vakata_context.element.find(".vakata-context-hover").last().closest("li").first().find("ul").hide().find(".vakata-context-hover").removeClass("vakata-context-hover").end().end().children("a").focus(), e.stopImmediatePropagation(), e.preventDefault());
                                break;
                            case 38:
                                vakata_context.is_visible && (o = vakata_context.element.find("ul:visible").addBack().last().children(".vakata-context-hover").removeClass("vakata-context-hover").prevAll("li:not(.vakata-context-separator)").first(), o.length || (o = vakata_context.element.find("ul:visible").addBack().last().children("li:not(.vakata-context-separator)").last()), o.addClass("vakata-context-hover").children("a").focus(), e.stopImmediatePropagation(), e.preventDefault());
                                break;
                            case 39:
                                vakata_context.is_visible && (vakata_context.element.find(".vakata-context-hover").last().children("ul").show().children("li:not(.vakata-context-separator)").removeClass("vakata-context-hover").first().addClass("vakata-context-hover").children("a").focus(), e.stopImmediatePropagation(), e.preventDefault());
                                break;
                            case 40:
                                vakata_context.is_visible && (o = vakata_context.element.find("ul:visible").addBack().last().children(".vakata-context-hover").removeClass("vakata-context-hover").nextAll("li:not(.vakata-context-separator)").first(), o.length || (o = vakata_context.element.find("ul:visible").addBack().last().children("li:not(.vakata-context-separator)").first()), o.addClass("vakata-context-hover").children("a").focus(), e.stopImmediatePropagation(), e.preventDefault());
                                break;
                            case 27:
                                $.vakata.context.hide(), e.preventDefault()
                        }
                    }).on("keydown", function(e) {
                        e.preventDefault();
                        var a = vakata_context.element.find(".vakata-contextmenu-shortcut-" + e.which).parent();
                        a.parent().not(".vakata-context-disabled") && a.click()
                    }), $(document).on("mousedown.vakata.jstree", function(e) {
                        vakata_context.is_visible && !$.contains(vakata_context.element[0], e.target) && $.vakata.context.hide()
                    }).on("context_show.vakata.jstree", function(e, data) {
                        vakata_context.element.find("li:has(ul)").children("a").addClass("vakata-context-parent"), right_to_left && vakata_context.element.addClass("vakata-context-rtl").css("direction", "rtl"), vakata_context.element.find("ul").hide().end()
                    })
                })
            }($), $.jstree.defaults.dnd = {
                copy: !0,
                open_timeout: 500,
                is_draggable: !0,
                check_while_dragging: !0,
                always_copy: !1,
                inside_pos: 0,
                drag_selection: !0,
                touch: !0,
                large_drop_target: !1,
                large_drag_target: !1
            }, $.jstree.plugins.dnd = function(options, parent) {
                this.bind = function() {
                    parent.bind.call(this), this.element.on("mousedown.jstree touchstart.jstree", this.settings.dnd.large_drag_target ? ".jstree-node" : ".jstree-anchor", $.proxy(function(e) {
                        if (this.settings.dnd.large_drag_target && $(e.target).closest(".jstree-node")[0] !== e.currentTarget) return !0;
                        if ("touchstart" === e.type && (!this.settings.dnd.touch || "selected" === this.settings.dnd.touch && !$(e.currentTarget).closest(".jstree-node").children(".jstree-anchor").hasClass("jstree-clicked"))) return !0;
                        var obj = this.get_node(e.target),
                            mlt = this.is_selected(obj) && this.settings.dnd.drag_selection ? this.get_top_selected().length : 1,
                            txt = mlt > 1 ? mlt + " " + this.get_string("nodes") : this.get_text(e.currentTarget);
                        return this.settings.core.force_text && (txt = $.vakata.html.escape(txt)), obj && obj.id && "#" !== obj.id && (1 === e.which || "touchstart" === e.type) && (!0 === this.settings.dnd.is_draggable || $.isFunction(this.settings.dnd.is_draggable) && this.settings.dnd.is_draggable.call(this, mlt > 1 ? this.get_top_selected(!0) : [obj])) ? (this.element.trigger("mousedown.jstree"), $.vakata.dnd.start(e, {
                            jstree: !0,
                            origin: this,
                            obj: this.get_node(obj, !0),
                            nodes: mlt > 1 ? this.get_top_selected() : [obj.id]
                        }, '<div id="jstree-dnd" class="jstree-' + this.get_theme() + " jstree-" + this.get_theme() + "-" + this.get_theme_variant() + " " + (this.settings.core.themes.responsive ? " jstree-dnd-responsive" : "") + '"><i class="jstree-icon jstree-er"></i>' + txt + '<ins class="jstree-copy" style="display:none;">+</ins></div>')) : void 0
                    }, this))
                }
            }, $(function() {
                var lastmv = !1,
                    laster = !1,
                    opento = !1,
                    marker = $('<div id="jstree-marker">&#160;</div>').hide();
                $(document).on("dnd_start.vakata.jstree", function(e, data) {
                    lastmv = !1, data && data.data && data.data.jstree && marker.appendTo("body")
                }).on("dnd_move.vakata.jstree", function(e, data) {
                    if (opento && clearTimeout(opento), data && data.data && data.data.jstree && (!data.event.target.id || "jstree-marker" !== data.event.target.id)) {
                        var l, t, h, p, i, o, ok, t1, t2, op, ps, pr, ip, tm, ins = $.jstree.reference(data.event.target),
                            ref = !1,
                            off = !1,
                            rel = !1;
                        if (ins && ins._data && ins._data.dnd)
                            if (marker.attr("class", "jstree-" + ins.get_theme() + (ins.settings.core.themes.responsive ? " jstree-dnd-responsive" : "")), data.helper.children().attr("class", "jstree-" + ins.get_theme() + " jstree-" + ins.get_theme() + "-" + ins.get_theme_variant() + " " + (ins.settings.core.themes.responsive ? " jstree-dnd-responsive" : "")).find(".jstree-copy").first()[data.data.origin && (data.data.origin.settings.dnd.always_copy || data.data.origin.settings.dnd.copy && (data.event.metaKey || data.event.ctrlKey)) ? "show" : "hide"](), data.event.target !== ins.element[0] && data.event.target !== ins.get_container_ul()[0] || 0 !== ins.get_container_ul().children().length) {
                                if ((ref = ins.settings.dnd.large_drop_target ? $(data.event.target).closest(".jstree-node").children(".jstree-anchor") : $(data.event.target).closest(".jstree-anchor")) && ref.length && ref.parent().is(".jstree-closed, .jstree-open, .jstree-leaf") && (off = ref.offset(), rel = data.event.pageY - off.top, h = ref.outerHeight(), o = rel < h / 3 ? ["b", "i", "a"] : rel > h - h / 3 ? ["a", "i", "b"] : rel > h / 2 ? ["i", "a", "b"] : ["i", "b", "a"], $.each(o, function(j, v) {
                                        switch (v) {
                                            case "b":
                                                l = off.left - 6, t = off.top, p = ins.get_parent(ref), i = ref.parent().index();
                                                break;
                                            case "i":
                                                ip = ins.settings.dnd.inside_pos, tm = ins.get_node(ref.parent()), l = off.left - 2, t = off.top + h / 2 + 1, p = tm.id, i = "first" === ip ? 0 : "last" === ip ? tm.children.length : Math.min(ip, tm.children.length);
                                                break;
                                            case "a":
                                                l = off.left - 6, t = off.top + h, p = ins.get_parent(ref), i = ref.parent().index() + 1
                                        }
                                        for (ok = !0, t1 = 0, t2 = data.data.nodes.length; t1 < t2; t1++)
                                            if (op = data.data.origin && (data.data.origin.settings.dnd.always_copy || data.data.origin.settings.dnd.copy && (data.event.metaKey || data.event.ctrlKey)) ? "copy_node" : "move_node", ps = i, "move_node" === op && "a" === v && data.data.origin && data.data.origin === ins && p === ins.get_parent(data.data.nodes[t1]) && (pr = ins.get_node(p), ps > $.inArray(data.data.nodes[t1], pr.children) && (ps -= 1)), !(ok = ok && (ins && ins.settings && ins.settings.dnd && !1 === ins.settings.dnd.check_while_dragging || ins.check(op, data.data.origin && data.data.origin !== ins ? data.data.origin.get_node(data.data.nodes[t1]) : data.data.nodes[t1], p, ps, {
                                                    dnd: !0,
                                                    ref: ins.get_node(ref.parent()),
                                                    pos: v,
                                                    origin: data.data.origin,
                                                    is_multi: data.data.origin && data.data.origin !== ins,
                                                    is_foreign: !data.data.origin
                                                })))) {
                                                ins && ins.last_error && (laster = ins.last_error());
                                                break
                                            } if ("i" === v && ref.parent().is(".jstree-closed") && ins.settings.dnd.open_timeout && (opento = setTimeout(function(x, z) {
                                                return function() {
                                                    x.open_node(z)
                                                }
                                            }(ins, ref), ins.settings.dnd.open_timeout)), ok) return lastmv = {
                                            ins: ins,
                                            par: p,
                                            pos: "i" !== v || "last" !== ip || 0 !== i || ins.is_loaded(tm) ? i : "last"
                                        }, marker.css({
                                            left: l + "px",
                                            top: t + "px"
                                        }).show(), data.helper.find(".jstree-icon").first().removeClass("jstree-er").addClass("jstree-ok"), laster = {}, o = !0, !1
                                    }), !0 === o)) return
                            } else {
                                for (ok = !0, t1 = 0, t2 = data.data.nodes.length; t1 < t2 && (ok = ok && ins.check(data.data.origin && (data.data.origin.settings.dnd.always_copy || data.data.origin.settings.dnd.copy && (data.event.metaKey || data.event.ctrlKey)) ? "copy_node" : "move_node", data.data.origin && data.data.origin !== ins ? data.data.origin.get_node(data.data.nodes[t1]) : data.data.nodes[t1], "#", "last", {
                                        dnd: !0,
                                        ref: ins.get_node("#"),
                                        pos: "i",
                                        origin: data.data.origin,
                                        is_multi: data.data.origin && data.data.origin !== ins,
                                        is_foreign: !data.data.origin
                                    })); t1++);
                                if (ok) return lastmv = {
                                    ins: ins,
                                    par: "#",
                                    pos: "last"
                                }, marker.hide(), void data.helper.find(".jstree-icon").first().removeClass("jstree-er").addClass("jstree-ok")
                            } lastmv = !1, data.helper.find(".jstree-icon").removeClass("jstree-ok").addClass("jstree-er"), marker.hide()
                    }
                }).on("dnd_scroll.vakata.jstree", function(e, data) {
                    data && data.data && data.data.jstree && (marker.hide(), lastmv = !1, data.helper.find(".jstree-icon").first().removeClass("jstree-ok").addClass("jstree-er"))
                }).on("dnd_stop.vakata.jstree", function(e, data) {
                    if (opento && clearTimeout(opento), data && data.data && data.data.jstree) {
                        marker.hide().detach();
                        var i, j, nodes = [];
                        if (lastmv) {
                            for (i = 0, j = data.data.nodes.length; i < j; i++) nodes[i] = data.data.origin ? data.data.origin.get_node(data.data.nodes[i]) : data.data.nodes[i];
                            lastmv.ins[data.data.origin && (data.data.origin.settings.dnd.always_copy || data.data.origin.settings.dnd.copy && (data.event.metaKey || data.event.ctrlKey)) ? "copy_node" : "move_node"](nodes, lastmv.par, lastmv.pos, !1, !1, !1, data.data.origin)
                        } else i = $(data.event.target).closest(".jstree"), i.length && laster && laster.error && "check" === laster.error && (i = i.jstree(!0)) && i.settings.core.error.call(this, laster)
                    }
                }).on("keyup.jstree keydown.jstree", function(e, data) {
                    (data = $.vakata.dnd._get()) && data.data && data.data.jstree && data.helper.find(".jstree-copy").first()[data.data.origin && (data.data.origin.settings.dnd.always_copy || data.data.origin.settings.dnd.copy && (e.metaKey || e.ctrlKey)) ? "show" : "hide"]()
                })
            }),
            function($) {
                $.vakata.html = {
                    div: $("<div />"),
                    escape: function(str) {
                        return $.vakata.html.div.text(str).html()
                    },
                    strip: function(str) {
                        return $.vakata.html.div.empty().append($.parseHTML(str)).text()
                    }
                };
                var vakata_dnd = {
                    element: !1,
                    target: !1,
                    is_down: !1,
                    is_drag: !1,
                    helper: !1,
                    helper_w: 0,
                    data: !1,
                    init_x: 0,
                    init_y: 0,
                    scroll_l: 0,
                    scroll_t: 0,
                    scroll_e: !1,
                    scroll_i: !1,
                    is_touch: !1
                };
                $.vakata.dnd = {
                    settings: {
                        scroll_speed: 10,
                        scroll_proximity: 20,
                        helper_left: 5,
                        helper_top: 10,
                        threshold: 5,
                        threshold_touch: 50
                    },
                    _trigger: function(event_name, e) {
                        var data = $.vakata.dnd._get();
                        data.event = e, $(document).triggerHandler("dnd_" + event_name + ".vakata", data)
                    },
                    _get: function() {
                        return {
                            data: vakata_dnd.data,
                            element: vakata_dnd.element,
                            helper: vakata_dnd.helper
                        }
                    },
                    _clean: function() {
                        vakata_dnd.helper && vakata_dnd.helper.remove(), vakata_dnd.scroll_i && (clearInterval(vakata_dnd.scroll_i), vakata_dnd.scroll_i = !1), vakata_dnd = {
                            element: !1,
                            target: !1,
                            is_down: !1,
                            is_drag: !1,
                            helper: !1,
                            helper_w: 0,
                            data: !1,
                            init_x: 0,
                            init_y: 0,
                            scroll_l: 0,
                            scroll_t: 0,
                            scroll_e: !1,
                            scroll_i: !1,
                            is_touch: !1
                        }, $(document).off("mousemove.vakata.jstree touchmove.vakata.jstree", $.vakata.dnd.drag), $(document).off("mouseup.vakata.jstree touchend.vakata.jstree", $.vakata.dnd.stop)
                    },
                    _scroll: function(init_only) {
                        if (!vakata_dnd.scroll_e || !vakata_dnd.scroll_l && !vakata_dnd.scroll_t) return vakata_dnd.scroll_i && (clearInterval(vakata_dnd.scroll_i), vakata_dnd.scroll_i = !1), !1;
                        if (!vakata_dnd.scroll_i) return vakata_dnd.scroll_i = setInterval($.vakata.dnd._scroll, 100), !1;
                        if (!0 === init_only) return !1;
                        var i = vakata_dnd.scroll_e.scrollTop(),
                            j = vakata_dnd.scroll_e.scrollLeft();
                        vakata_dnd.scroll_e.scrollTop(i + vakata_dnd.scroll_t * $.vakata.dnd.settings.scroll_speed), vakata_dnd.scroll_e.scrollLeft(j + vakata_dnd.scroll_l * $.vakata.dnd.settings.scroll_speed), i === vakata_dnd.scroll_e.scrollTop() && j === vakata_dnd.scroll_e.scrollLeft() || $.vakata.dnd._trigger("scroll", vakata_dnd.scroll_e)
                    },
                    start: function(e, data, html) {
                        "touchstart" === e.type && e.originalEvent && e.originalEvent.changedTouches && e.originalEvent.changedTouches[0] && (e.pageX = e.originalEvent.changedTouches[0].pageX, e.pageY = e.originalEvent.changedTouches[0].pageY, e.target = document.elementFromPoint(e.originalEvent.changedTouches[0].pageX - window.pageXOffset, e.originalEvent.changedTouches[0].pageY - window.pageYOffset)), vakata_dnd.is_drag && $.vakata.dnd.stop({});
                        try {
                            e.currentTarget.unselectable = "on", e.currentTarget.onselectstart = function() {
                                return !1
                            }, e.currentTarget.style && (e.currentTarget.style.MozUserSelect = "none")
                        } catch (ignore) {}
                        return vakata_dnd.init_x = e.pageX, vakata_dnd.init_y = e.pageY, vakata_dnd.data = data, vakata_dnd.is_down = !0, vakata_dnd.element = e.currentTarget, vakata_dnd.target = e.target, vakata_dnd.is_touch = "touchstart" === e.type, !1 !== html && (vakata_dnd.helper = $("<div id='vakata-dnd'></div>").html(html).css({
                            display: "block",
                            margin: "0",
                            padding: "0",
                            position: "absolute",
                            top: "-2000px",
                            lineHeight: "16px",
                            zIndex: "10000"
                        })), $(document).on("mousemove.vakata.jstree touchmove.vakata.jstree", $.vakata.dnd.drag), $(document).on("mouseup.vakata.jstree touchend.vakata.jstree", $.vakata.dnd.stop), !1
                    },
                    drag: function(e) {
                        if ("touchmove" === e.type && e.originalEvent && e.originalEvent.changedTouches && e.originalEvent.changedTouches[0] && (e.pageX = e.originalEvent.changedTouches[0].pageX, e.pageY = e.originalEvent.changedTouches[0].pageY, e.target = document.elementFromPoint(e.originalEvent.changedTouches[0].pageX - window.pageXOffset, e.originalEvent.changedTouches[0].pageY - window.pageYOffset)), vakata_dnd.is_down) {
                            if (!vakata_dnd.is_drag) {
                                if (!(Math.abs(e.pageX - vakata_dnd.init_x) > (vakata_dnd.is_touch ? $.vakata.dnd.settings.threshold_touch : $.vakata.dnd.settings.threshold) || Math.abs(e.pageY - vakata_dnd.init_y) > (vakata_dnd.is_touch ? $.vakata.dnd.settings.threshold_touch : $.vakata.dnd.settings.threshold))) return;
                                vakata_dnd.helper && (vakata_dnd.helper.appendTo("body"), vakata_dnd.helper_w = vakata_dnd.helper.outerWidth()), vakata_dnd.is_drag = !0, $.vakata.dnd._trigger("start", e)
                            }
                            var d = !1,
                                w = !1,
                                dh = !1,
                                wh = !1,
                                dw = !1,
                                ww = !1,
                                dt = !1,
                                dl = !1,
                                ht = !1,
                                hl = !1;
                            return vakata_dnd.scroll_t = 0, vakata_dnd.scroll_l = 0, vakata_dnd.scroll_e = !1, $($(e.target).parentsUntil("body").addBack().get().reverse()).filter(function() {
                                return /^auto|scroll$/.test($(this).css("overflow")) && (this.scrollHeight > this.offsetHeight || this.scrollWidth > this.offsetWidth)
                            }).each(function() {
                                var t = $(this),
                                    o = t.offset();
                                if (this.scrollHeight > this.offsetHeight && (o.top + t.height() - e.pageY < $.vakata.dnd.settings.scroll_proximity && (vakata_dnd.scroll_t = 1), e.pageY - o.top < $.vakata.dnd.settings.scroll_proximity && (vakata_dnd.scroll_t = -1)), this.scrollWidth > this.offsetWidth && (o.left + t.width() - e.pageX < $.vakata.dnd.settings.scroll_proximity && (vakata_dnd.scroll_l = 1), e.pageX - o.left < $.vakata.dnd.settings.scroll_proximity && (vakata_dnd.scroll_l = -1)), vakata_dnd.scroll_t || vakata_dnd.scroll_l) return vakata_dnd.scroll_e = $(this), !1
                            }), vakata_dnd.scroll_e || (d = $(document), w = $(window), dh = d.height(), wh = w.height(), dw = d.width(), ww = w.width(), dt = d.scrollTop(), dl = d.scrollLeft(), dh > wh && e.pageY - dt < $.vakata.dnd.settings.scroll_proximity && (vakata_dnd.scroll_t = -1), dh > wh && wh - (e.pageY - dt) < $.vakata.dnd.settings.scroll_proximity && (vakata_dnd.scroll_t = 1), dw > ww && e.pageX - dl < $.vakata.dnd.settings.scroll_proximity && (vakata_dnd.scroll_l = -1), dw > ww && ww - (e.pageX - dl) < $.vakata.dnd.settings.scroll_proximity && (vakata_dnd.scroll_l = 1), (vakata_dnd.scroll_t || vakata_dnd.scroll_l) && (vakata_dnd.scroll_e = d)), vakata_dnd.scroll_e && $.vakata.dnd._scroll(!0), vakata_dnd.helper && (ht = parseInt(e.pageY + $.vakata.dnd.settings.helper_top, 10), hl = parseInt(e.pageX + $.vakata.dnd.settings.helper_left, 10), dh && ht + 25 > dh && (ht = dh - 50), dw && hl + vakata_dnd.helper_w > dw && (hl = dw - (vakata_dnd.helper_w + 2)), vakata_dnd.helper.css({
                                left: hl + "px",
                                top: ht + "px"
                            })), $.vakata.dnd._trigger("move", e), !1
                        }
                    },
                    stop: function(e) {
                        if ("touchend" === e.type && e.originalEvent && e.originalEvent.changedTouches && e.originalEvent.changedTouches[0] && (e.pageX = e.originalEvent.changedTouches[0].pageX, e.pageY = e.originalEvent.changedTouches[0].pageY, e.target = document.elementFromPoint(e.originalEvent.changedTouches[0].pageX - window.pageXOffset, e.originalEvent.changedTouches[0].pageY - window.pageYOffset)), vakata_dnd.is_drag) $.vakata.dnd._trigger("stop", e);
                        else if ("touchend" === e.type && e.target === vakata_dnd.target) {
                            var to = setTimeout(function() {
                                $(e.target).click()
                            }, 100);
                            $(e.target).one("click", function() {
                                to && clearTimeout(to)
                            })
                        }
                        return $.vakata.dnd._clean(), !1
                    }
                }
            }($), $.jstree.defaults.massload = null, $.jstree.plugins.massload = function(options, parent) {
                this.init = function(el, options) {
                    parent.init.call(this, el, options), this._data.massload = {}
                }, this._load_nodes = function(nodes, callback, is_callback) {
                    var s = this.settings.massload;
                    return is_callback && !$.isEmptyObject(this._data.massload) ? parent._load_nodes.call(this, nodes, callback, is_callback) : $.isFunction(s) ? s.call(this, nodes, $.proxy(function(data) {
                        if (data)
                            for (var i in data) data.hasOwnProperty(i) && (this._data.massload[i] = data[i]);
                        parent._load_nodes.call(this, nodes, callback, is_callback)
                    }, this)) : "object" == typeof s && s && s.url ? (s = $.extend(!0, {}, s), $.isFunction(s.url) && (s.url = s.url.call(this, nodes)), $.isFunction(s.data) && (s.data = s.data.call(this, nodes)), $.ajax(s).done($.proxy(function(data, t, x) {
                        if (data)
                            for (var i in data) data.hasOwnProperty(i) && (this._data.massload[i] = data[i]);
                        parent._load_nodes.call(this, nodes, callback, is_callback)
                    }, this)).fail($.proxy(function(f) {
                        parent._load_nodes.call(this, nodes, callback, is_callback)
                    }, this))) : parent._load_nodes.call(this, nodes, callback, is_callback)
                }, this._load_node = function(obj, callback) {
                    var d = this._data.massload[obj.id];
                    return d ? this["string" == typeof d ? "_append_html_data" : "_append_json_data"](obj, "string" == typeof d ? $($.parseHTML(d)).filter(function() {
                        return 3 !== this.nodeType
                    }) : d, function(status) {
                        callback.call(this, status), delete this._data.massload[obj.id]
                    }) : parent._load_node.call(this, obj, callback)
                }
            }, $.jstree.defaults.search = {
                ajax: !1,
                fuzzy: !1,
                case_sensitive: !1,
                show_only_matches: !1,
                show_only_matches_children: !1,
                close_opened_onclear: !0,
                search_leaves_only: !1,
                search_callback: !1
            }, $.jstree.plugins.search = function(options, parent) {
                this.bind = function() {
                    parent.bind.call(this), this._data.search.str = "", this._data.search.dom = $(), this._data.search.res = [], this._data.search.opn = [], this._data.search.som = !1, this._data.search.smc = !1, this.element.on("before_open.jstree", $.proxy(function(e, data) {
                        var i, j, r = this._data.search.res,
                            s = [],
                            o = $();
                        if (r && r.length && (this._data.search.dom = $(this.element[0].querySelectorAll("#" + $.map(r, function(v) {
                                return -1 !== "0123456789".indexOf(v[0]) ? "\\3" + v[0] + " " + v.substr(1).replace($.jstree.idregex, "\\$&") : v.replace($.jstree.idregex, "\\$&")
                            }).join(", #"))), this._data.search.dom.children(".jstree-anchor").addClass("jstree-search"), this._data.search.som && this._data.search.res.length)) {
                            for (i = 0, j = r.length; i < j; i++) s = s.concat(this.get_node(r[i]).parents);
                            s = $.vakata.array_remove_item($.vakata.array_unique(s), "#"), o = s.length ? $(this.element[0].querySelectorAll("#" + $.map(s, function(v) {
                                return -1 !== "0123456789".indexOf(v[0]) ? "\\3" + v[0] + " " + v.substr(1).replace($.jstree.idregex, "\\$&") : v.replace($.jstree.idregex, "\\$&")
                            }).join(", #"))) : $(), this.element.find(".jstree-node").hide().filter(".jstree-last").filter(function() {
                                return this.nextSibling
                            }).removeClass("jstree-last"), o = o.add(this._data.search.dom), this._data.search.smc && this._data.search.dom.children(".jstree-children").find(".jstree-node").show(), o.parentsUntil(".jstree").addBack().show().filter(".jstree-children").each(function() {
                                $(this).children(".jstree-node:visible").eq(-1).addClass("jstree-last")
                            })
                        }
                    }, this)).on("search.jstree", $.proxy(function(e, data) {
                        this._data.search.som && data.nodes.length && (this.element.find(".jstree-node").hide().filter(".jstree-last").filter(function() {
                            return this.nextSibling
                        }).removeClass("jstree-last"), this._data.search.smc && data.nodes.children(".jstree-children").find(".jstree-node").show(), data.nodes.parentsUntil(".jstree").addBack().show().filter(".jstree-children").each(function() {
                            $(this).children(".jstree-node:visible").eq(-1).addClass("jstree-last")
                        }))
                    }, this)).on("clear_search.jstree", $.proxy(function(e, data) {
                        this._data.search.som && data.nodes.length && this.element.find(".jstree-node").css("display", "").filter(".jstree-last").filter(function() {
                            return this.nextSibling
                        }).removeClass("jstree-last")
                    }, this))
                }, this.search = function(str, skip_async, show_only_matches, inside, append, show_only_matches_children) {
                    if (!1 === str || "" === $.trim(str.toString())) return this.clear_search();
                    inside = this.get_node(inside), inside = inside && inside.id ? inside.id : null, str = str.toString();
                    var s = this.settings.search,
                        a = !!s.ajax && s.ajax,
                        m = this._model.data,
                        f = null,
                        r = [],
                        p = [];
                    if (this._data.search.res.length && !append && this.clear_search(), show_only_matches === undefined && (show_only_matches = s.show_only_matches), show_only_matches_children === undefined && (show_only_matches_children = s.show_only_matches_children), !skip_async && !1 !== a) return $.isFunction(a) ? a.call(this, str, $.proxy(function(d) {
                        d && d.d && (d = d.d), this._load_nodes($.isArray(d) ? $.vakata.array_unique(d) : [], function() {
                            this.search(str, !0, show_only_matches, inside, append)
                        }, !0)
                    }, this), inside) : (a = $.extend({}, a), a.data || (a.data = {}), a.data.str = str, inside && (a.data.inside = inside), $.ajax(a).fail($.proxy(function() {
                        this._data.core.last_error = {
                            error: "ajax",
                            plugin: "search",
                            id: "search_01",
                            reason: "Could not load search parents",
                            data: JSON.stringify(a)
                        }, this.settings.core.error.call(this, this._data.core.last_error)
                    }, this)).done($.proxy(function(d) {
                        d && d.d && (d = d.d), this._load_nodes($.isArray(d) ? $.vakata.array_unique(d) : [], function() {
                            this.search(str, !0, show_only_matches, inside, append)
                        }, !0)
                    }, this)));
                    append || (this._data.search.str = str, this._data.search.dom = $(), this._data.search.res = [], this._data.search.opn = [], this._data.search.som = show_only_matches, this._data.search.smc = show_only_matches_children), f = new $.vakata.search(str, !0, {
                        caseSensitive: s.case_sensitive,
                        fuzzy: s.fuzzy
                    }), $.each(m[inside || "#"].children_d, function(ii, i) {
                        var v = m[i];
                        v.text && (s.search_callback && s.search_callback.call(this, str, v) || !s.search_callback && f.search(v.text).isMatch) && (!s.search_leaves_only || v.state.loaded && 0 === v.children.length) && (r.push(i), p = p.concat(v.parents))
                    }), r.length && (p = $.vakata.array_unique(p), this._search_open(p), append ? (this._data.search.dom = this._data.search.dom.add($(this.element[0].querySelectorAll("#" + $.map(r, function(v) {
                        return -1 !== "0123456789".indexOf(v[0]) ? "\\3" + v[0] + " " + v.substr(1).replace($.jstree.idregex, "\\$&") : v.replace($.jstree.idregex, "\\$&")
                    }).join(", #")))), this._data.search.res = $.vakata.array_unique(this._data.search.res.concat(r))) : (this._data.search.dom = $(this.element[0].querySelectorAll("#" + $.map(r, function(v) {
                        return -1 !== "0123456789".indexOf(v[0]) ? "\\3" + v[0] + " " + v.substr(1).replace($.jstree.idregex, "\\$&") : v.replace($.jstree.idregex, "\\$&")
                    }).join(", #"))), this._data.search.res = r), this._data.search.dom.children(".jstree-anchor").addClass("jstree-search")), this.trigger("search", {
                        nodes: this._data.search.dom,
                        str: str,
                        res: this._data.search.res,
                        show_only_matches: show_only_matches
                    })
                }, this.clear_search = function() {
                    this._data.search.dom.children(".jstree-anchor").removeClass("jstree-search"), this.settings.search.close_opened_onclear && this.close_node(this._data.search.opn, 0), this.trigger("clear_search", {
                        nodes: this._data.search.dom,
                        str: this._data.search.str,
                        res: this._data.search.res
                    }), this._data.search.str = "", this._data.search.res = [], this._data.search.opn = [], this._data.search.dom = $()
                }, this._search_open = function(d) {
                    var t = this;
                    $.each(d.concat([]), function(i, v) {
                        if ("#" === v) return !0;
                        try {
                            v = $("#" + v.replace($.jstree.idregex, "\\$&"), t.element)
                        } catch (ignore) {}
                        v && v.length && t.is_closed(v) && (t._data.search.opn.push(v[0].id), t.open_node(v, function() {
                            t._search_open(d)
                        }, 0))
                    })
                }
            },
            function($) {
                $.vakata.search = function(pattern, txt, options) {
                    options = options || {}, options = $.extend({}, $.vakata.search.defaults, options), !1 !== options.fuzzy && (options.fuzzy = !0), pattern = options.caseSensitive ? pattern : pattern.toLowerCase();
                    var matchmask, pattern_alphabet, match_bitapScore, search, MATCH_LOCATION = options.location,
                        MATCH_DISTANCE = options.distance,
                        MATCH_THRESHOLD = options.threshold,
                        patternLen = pattern.length;
                    return patternLen > 32 && (options.fuzzy = !1), options.fuzzy && (matchmask = 1 << patternLen - 1, pattern_alphabet = function() {
                        var mask = {},
                            i = 0;
                        for (i = 0; i < patternLen; i++) mask[pattern.charAt(i)] = 0;
                        for (i = 0; i < patternLen; i++) mask[pattern.charAt(i)] |= 1 << patternLen - i - 1;
                        return mask
                    }(), match_bitapScore = function(e, x) {
                        var accuracy = e / patternLen,
                            proximity = Math.abs(MATCH_LOCATION - x);
                        return MATCH_DISTANCE ? accuracy + proximity / MATCH_DISTANCE : proximity ? 1 : accuracy
                    }), search = function(text) {
                        if (text = options.caseSensitive ? text : text.toLowerCase(), pattern === text || -1 !== text.indexOf(pattern)) return {
                            isMatch: !0,
                            score: 0
                        };
                        if (!options.fuzzy) return {
                            isMatch: !1,
                            score: 1
                        };
                        var i, j, binMin, binMid, lastRd, start, finish, rd, charMatch, textLen = text.length,
                            scoreThreshold = MATCH_THRESHOLD,
                            bestLoc = text.indexOf(pattern, MATCH_LOCATION),
                            binMax = patternLen + textLen,
                            score = 1,
                            locations = [];
                        for (-1 !== bestLoc && (scoreThreshold = Math.min(match_bitapScore(0, bestLoc), scoreThreshold), -1 !== (bestLoc = text.lastIndexOf(pattern, MATCH_LOCATION + patternLen)) && (scoreThreshold = Math.min(match_bitapScore(0, bestLoc), scoreThreshold))), bestLoc = -1, i = 0; i < patternLen; i++) {
                            for (binMin = 0, binMid = binMax; binMin < binMid;) match_bitapScore(i, MATCH_LOCATION + binMid) <= scoreThreshold ? binMin = binMid : binMax = binMid, binMid = Math.floor((binMax - binMin) / 2 + binMin);
                            for (binMax = binMid, start = Math.max(1, MATCH_LOCATION - binMid + 1), finish = Math.min(MATCH_LOCATION + binMid, textLen) + patternLen, rd = new Array(finish + 2), rd[finish + 1] = (1 << i) - 1, j = finish; j >= start; j--)
                                if (charMatch = pattern_alphabet[text.charAt(j - 1)], rd[j] = 0 === i ? (rd[j + 1] << 1 | 1) & charMatch : (rd[j + 1] << 1 | 1) & charMatch | (lastRd[j + 1] | lastRd[j]) << 1 | 1 | lastRd[j + 1], rd[j] & matchmask && (score = match_bitapScore(i, j - 1)) <= scoreThreshold) {
                                    if (scoreThreshold = score, bestLoc = j - 1, locations.push(bestLoc), !(bestLoc > MATCH_LOCATION)) break;
                                    start = Math.max(1, 2 * MATCH_LOCATION - bestLoc)
                                } if (match_bitapScore(i + 1, MATCH_LOCATION) > scoreThreshold) break;
                            lastRd = rd
                        }
                        return {
                            isMatch: bestLoc >= 0,
                            score: score
                        }
                    }, !0 === txt ? {
                        search: search
                    } : search(txt)
                }, $.vakata.search.defaults = {
                    location: 0,
                    distance: 100,
                    threshold: .6,
                    fuzzy: !1,
                    caseSensitive: !1
                }
            }($), $.jstree.defaults.sort = function(a, b) {
                return this.get_text(a) > this.get_text(b) ? 1 : -1
            }, $.jstree.plugins.sort = function(options, parent) {
                this.bind = function() {
                    parent.bind.call(this), this.element.on("model.jstree", $.proxy(function(e, data) {
                        this.sort(data.parent, !0)
                    }, this)).on("rename_node.jstree create_node.jstree", $.proxy(function(e, data) {
                        this.sort(data.parent || data.node.parent, !1), this.redraw_node(data.parent || data.node.parent, !0)
                    }, this)).on("move_node.jstree copy_node.jstree", $.proxy(function(e, data) {
                        this.sort(data.parent, !1), this.redraw_node(data.parent, !0)
                    }, this))
                }, this.sort = function(obj, deep) {
                    var i, j;
                    if ((obj = this.get_node(obj)) && obj.children && obj.children.length && (obj.children.sort($.proxy(this.settings.sort, this)), deep))
                        for (i = 0, j = obj.children_d.length; i < j; i++) this.sort(obj.children_d[i], !1)
                }
            };
        var to = !1;
        $.jstree.defaults.state = {
                key: "jstree",
                events: "changed.jstree open_node.jstree close_node.jstree check_node.jstree uncheck_node.jstree",
                ttl: !1,
                filter: !1
            }, $.jstree.plugins.state = function(options, parent) {
                this.bind = function() {
                    parent.bind.call(this);
                    var bind = $.proxy(function() {
                        this.element.on(this.settings.state.events, $.proxy(function() {
                            to && clearTimeout(to), to = setTimeout($.proxy(function() {
                                this.save_state()
                            }, this), 100)
                        }, this)), this.trigger("state_ready")
                    }, this);
                    this.element.on("ready.jstree", $.proxy(function(e, data) {
                        this.element.one("restore_state.jstree", bind), this.restore_state() || bind()
                    }, this))
                }, this.save_state = function() {
                    var st = {
                        state: this.get_state(),
                        ttl: this.settings.state.ttl,
                        sec: +new Date
                    };
                    $.vakata.storage.set(this.settings.state.key, JSON.stringify(st))
                }, this.restore_state = function() {
                    var k = $.vakata.storage.get(this.settings.state.key);
                    if (k) try {
                        k = JSON.parse(k)
                    } catch (ex) {
                        return !1
                    }
                    return !(k && k.ttl && k.sec && +new Date - k.sec > k.ttl) && (k && k.state && (k = k.state), k && $.isFunction(this.settings.state.filter) && (k = this.settings.state.filter.call(this, k)), !!k && (this.element.one("set_state.jstree", function(e, data) {
                        data.instance.trigger("restore_state", {
                            state: $.extend(!0, {}, k)
                        })
                    }), this.set_state(k), !0))
                }, this.clear_state = function() {
                    return $.vakata.storage.del(this.settings.state.key)
                }
            },
            function($, undefined) {
                $.vakata.storage = {
                    set: function(key, val) {
                        return window.localStorage.setItem(key, val)
                    },
                    get: function(key) {
                        return window.localStorage.getItem(key)
                    },
                    del: function(key) {
                        return window.localStorage.removeItem(key)
                    }
                }
            }($), $.jstree.defaults.types = {
                "#": {},
                default: {}
            }, $.jstree.plugins.types = function(options, parent) {
                this.init = function(el, options) {
                    var i, j;
                    if (options && options.types && options.types.default)
                        for (i in options.types)
                            if ("default" !== i && "#" !== i && options.types.hasOwnProperty(i))
                                for (j in options.types.default) options.types.default.hasOwnProperty(j) && options.types[i][j] === undefined && (options.types[i][j] = options.types.default[j]);
                    parent.init.call(this, el, options), this._model.data["#"].type = "#"
                }, this.refresh = function(skip_loading, forget_state) {
                    parent.refresh.call(this, skip_loading, forget_state), this._model.data["#"].type = "#"
                }, this.bind = function() {
                    this.element.on("model.jstree", $.proxy(function(e, data) {
                        var i, j, m = this._model.data,
                            dpc = data.nodes,
                            t = this.settings.types,
                            c = "default";
                        for (i = 0, j = dpc.length; i < j; i++) c = "default", m[dpc[i]].original && m[dpc[i]].original.type && t[m[dpc[i]].original.type] && (c = m[dpc[i]].original.type), m[dpc[i]].data && m[dpc[i]].data.jstree && m[dpc[i]].data.jstree.type && t[m[dpc[i]].data.jstree.type] && (c = m[dpc[i]].data.jstree.type), m[dpc[i]].type = c, !0 === m[dpc[i]].icon && t[c].icon !== undefined && (m[dpc[i]].icon = t[c].icon);
                        m["#"].type = "#"
                    }, this)), parent.bind.call(this)
                }, this.get_json = function(obj, options, flat) {
                    var i, j, m = this._model.data,
                        opt = options ? $.extend(!0, {}, options, {
                            no_id: !1
                        }) : {},
                        tmp = parent.get_json.call(this, obj, opt, flat);
                    if (!1 === tmp) return !1;
                    if ($.isArray(tmp))
                        for (i = 0, j = tmp.length; i < j; i++) tmp[i].type = tmp[i].id && m[tmp[i].id] && m[tmp[i].id].type ? m[tmp[i].id].type : "default", options && options.no_id && (delete tmp[i].id, tmp[i].li_attr && tmp[i].li_attr.id && delete tmp[i].li_attr.id,
                            tmp[i].a_attr && tmp[i].a_attr.id && delete tmp[i].a_attr.id);
                    else tmp.type = tmp.id && m[tmp.id] && m[tmp.id].type ? m[tmp.id].type : "default", options && options.no_id && (tmp = this._delete_ids(tmp));
                    return tmp
                }, this._delete_ids = function(tmp) {
                    if ($.isArray(tmp)) {
                        for (var i = 0, j = tmp.length; i < j; i++) tmp[i] = this._delete_ids(tmp[i]);
                        return tmp
                    }
                    return delete tmp.id, tmp.li_attr && tmp.li_attr.id && delete tmp.li_attr.id, tmp.a_attr && tmp.a_attr.id && delete tmp.a_attr.id, tmp.children && $.isArray(tmp.children) && (tmp.children = this._delete_ids(tmp.children)), tmp
                }, this.check = function(chk, obj, par, pos, more) {
                    if (!1 === parent.check.call(this, chk, obj, par, pos, more)) return !1;
                    obj = obj && obj.id ? obj : this.get_node(obj), par = par && par.id ? par : this.get_node(par);
                    var tmp, d, i, j, m = obj && obj.id ? more && more.origin ? more.origin : $.jstree.reference(obj.id) : null;
                    switch (m = m && m._model && m._model.data ? m._model.data : null, chk) {
                        case "create_node":
                        case "move_node":
                        case "copy_node":
                            if ("move_node" !== chk || -1 === $.inArray(obj.id, par.children)) {
                                if (tmp = this.get_rules(par), tmp.max_children !== undefined && -1 !== tmp.max_children && tmp.max_children === par.children.length) return this._data.core.last_error = {
                                    error: "check",
                                    plugin: "types",
                                    id: "types_01",
                                    reason: "max_children prevents function: " + chk,
                                    data: JSON.stringify({
                                        chk: chk,
                                        pos: pos,
                                        obj: !(!obj || !obj.id) && obj.id,
                                        par: !(!par || !par.id) && par.id
                                    })
                                }, !1;
                                if (tmp.valid_children !== undefined && -1 !== tmp.valid_children && -1 === $.inArray(obj.type || "default", tmp.valid_children)) return this._data.core.last_error = {
                                    error: "check",
                                    plugin: "types",
                                    id: "types_02",
                                    reason: "valid_children prevents function: " + chk,
                                    data: JSON.stringify({
                                        chk: chk,
                                        pos: pos,
                                        obj: !(!obj || !obj.id) && obj.id,
                                        par: !(!par || !par.id) && par.id
                                    })
                                }, !1;
                                if (m && obj.children_d && obj.parents) {
                                    for (d = 0, i = 0, j = obj.children_d.length; i < j; i++) d = Math.max(d, m[obj.children_d[i]].parents.length);
                                    d = d - obj.parents.length + 1
                                }(d <= 0 || d === undefined) && (d = 1);
                                do {
                                    if (tmp.max_depth !== undefined && -1 !== tmp.max_depth && tmp.max_depth < d) return this._data.core.last_error = {
                                        error: "check",
                                        plugin: "types",
                                        id: "types_03",
                                        reason: "max_depth prevents function: " + chk,
                                        data: JSON.stringify({
                                            chk: chk,
                                            pos: pos,
                                            obj: !(!obj || !obj.id) && obj.id,
                                            par: !(!par || !par.id) && par.id
                                        })
                                    }, !1;
                                    par = this.get_node(par.parent), tmp = this.get_rules(par), d++
                                } while (par)
                            }
                    }
                    return !0
                }, this.get_rules = function(obj) {
                    if (!(obj = this.get_node(obj))) return !1;
                    var tmp = this.get_type(obj, !0);
                    return tmp.max_depth === undefined && (tmp.max_depth = -1), tmp.max_children === undefined && (tmp.max_children = -1), tmp.valid_children === undefined && (tmp.valid_children = -1), tmp
                }, this.get_type = function(obj, rules) {
                    return !!(obj = this.get_node(obj)) && (rules ? $.extend({
                        type: obj.type
                    }, this.settings.types[obj.type]) : obj.type)
                }, this.set_type = function(obj, type) {
                    var t, t1, t2, old_type, old_icon;
                    if ($.isArray(obj)) {
                        for (obj = obj.slice(), t1 = 0, t2 = obj.length; t1 < t2; t1++) this.set_type(obj[t1], type);
                        return !0
                    }
                    return t = this.settings.types, obj = this.get_node(obj), !(!t[type] || !obj) && (old_type = obj.type, old_icon = this.get_icon(obj), obj.type = type, (!0 === old_icon || t[old_type] && t[old_type].icon !== undefined && old_icon === t[old_type].icon) && this.set_icon(obj, t[type].icon === undefined || t[type].icon), !0)
                }
            }, $.jstree.defaults.unique = {
                case_sensitive: !1,
                duplicate: function(name, counter) {
                    return name + " (" + counter + ")"
                }
            }, $.jstree.plugins.unique = function(options, parent) {
                this.check = function(chk, obj, par, pos, more) {
                    if (!1 === parent.check.call(this, chk, obj, par, pos, more)) return !1;
                    if (obj = obj && obj.id ? obj : this.get_node(obj), !(par = par && par.id ? par : this.get_node(par)) || !par.children) return !0;
                    var i, j, n = "rename_node" === chk ? pos : obj.text,
                        c = [],
                        s = this.settings.unique.case_sensitive,
                        m = this._model.data;
                    for (i = 0, j = par.children.length; i < j; i++) c.push(s ? m[par.children[i]].text : m[par.children[i]].text.toLowerCase());
                    switch (s || (n = n.toLowerCase()), chk) {
                        case "delete_node":
                            return !0;
                        case "rename_node":
                            return i = -1 === $.inArray(n, c) || obj.text && obj.text[s ? "toString" : "toLowerCase"]() === n, i || (this._data.core.last_error = {
                                error: "check",
                                plugin: "unique",
                                id: "unique_01",
                                reason: "Child with name " + n + " already exists. Preventing: " + chk,
                                data: JSON.stringify({
                                    chk: chk,
                                    pos: pos,
                                    obj: !(!obj || !obj.id) && obj.id,
                                    par: !(!par || !par.id) && par.id
                                })
                            }), i;
                        case "create_node":
                            return i = -1 === $.inArray(n, c), i || (this._data.core.last_error = {
                                error: "check",
                                plugin: "unique",
                                id: "unique_04",
                                reason: "Child with name " + n + " already exists. Preventing: " + chk,
                                data: JSON.stringify({
                                    chk: chk,
                                    pos: pos,
                                    obj: !(!obj || !obj.id) && obj.id,
                                    par: !(!par || !par.id) && par.id
                                })
                            }), i;
                        case "copy_node":
                            return i = -1 === $.inArray(n, c), i || (this._data.core.last_error = {
                                error: "check",
                                plugin: "unique",
                                id: "unique_02",
                                reason: "Child with name " + n + " already exists. Preventing: " + chk,
                                data: JSON.stringify({
                                    chk: chk,
                                    pos: pos,
                                    obj: !(!obj || !obj.id) && obj.id,
                                    par: !(!par || !par.id) && par.id
                                })
                            }), i;
                        case "move_node":
                            return i = obj.parent === par.id && (!more || !more.is_multi) || -1 === $.inArray(n, c), i || (this._data.core.last_error = {
                                error: "check",
                                plugin: "unique",
                                id: "unique_03",
                                reason: "Child with name " + n + " already exists. Preventing: " + chk,
                                data: JSON.stringify({
                                    chk: chk,
                                    pos: pos,
                                    obj: !(!obj || !obj.id) && obj.id,
                                    par: !(!par || !par.id) && par.id
                                })
                            }), i
                    }
                    return !0
                }, this.create_node = function(par, node, pos, callback, is_loaded) {
                    if (!node || node.text === undefined) {
                        if (null === par && (par = "#"), !(par = this.get_node(par))) return parent.create_node.call(this, par, node, pos, callback, is_loaded);
                        if (pos = pos === undefined ? "last" : pos, !pos.toString().match(/^(before|after)$/) && !is_loaded && !this.is_loaded(par)) return parent.create_node.call(this, par, node, pos, callback, is_loaded);
                        node || (node = {});
                        var tmp, n, dpc, i, j, m = this._model.data,
                            s = this.settings.unique.case_sensitive,
                            cb = this.settings.unique.duplicate;
                        for (n = tmp = this.get_string("New node"), dpc = [], i = 0, j = par.children.length; i < j; i++) dpc.push(s ? m[par.children[i]].text : m[par.children[i]].text.toLowerCase());
                        for (i = 1; - 1 !== $.inArray(s ? n : n.toLowerCase(), dpc);) n = cb.call(this, tmp, ++i).toString();
                        node.text = n
                    }
                    return parent.create_node.call(this, par, node, pos, callback, is_loaded)
                }
            };
        var div = document.createElement("DIV");
        if (div.setAttribute("unselectable", "on"), div.setAttribute("role", "presentation"), div.className = "jstree-wholerow", div.innerHTML = "&#160;", $.jstree.plugins.wholerow = function(options, parent) {
                this.bind = function() {
                    parent.bind.call(this), this.element.on("ready.jstree set_state.jstree", $.proxy(function() {
                        this.hide_dots()
                    }, this)).on("init.jstree loading.jstree ready.jstree", $.proxy(function() {
                        this.get_container_ul().addClass("jstree-wholerow-ul")
                    }, this)).on("deselect_all.jstree", $.proxy(function(e, data) {
                        this.element.find(".jstree-wholerow-clicked").removeClass("jstree-wholerow-clicked")
                    }, this)).on("changed.jstree", $.proxy(function(e, data) {
                        this.element.find(".jstree-wholerow-clicked").removeClass("jstree-wholerow-clicked");
                        var i, j, tmp = !1;
                        for (i = 0, j = data.selected.length; i < j; i++)(tmp = this.get_node(data.selected[i], !0)) && tmp.length && tmp.children(".jstree-wholerow").addClass("jstree-wholerow-clicked")
                    }, this)).on("open_node.jstree", $.proxy(function(e, data) {
                        this.get_node(data.node, !0).find(".jstree-clicked").parent().children(".jstree-wholerow").addClass("jstree-wholerow-clicked")
                    }, this)).on("hover_node.jstree dehover_node.jstree", $.proxy(function(e, data) {
                        "hover_node" === e.type && this.is_disabled(data.node) || this.get_node(data.node, !0).children(".jstree-wholerow")["hover_node" === e.type ? "addClass" : "removeClass"]("jstree-wholerow-hovered")
                    }, this)).on("contextmenu.jstree", ".jstree-wholerow", $.proxy(function(e) {
                        e.preventDefault();
                        var tmp = $.Event("contextmenu", {
                            metaKey: e.metaKey,
                            ctrlKey: e.ctrlKey,
                            altKey: e.altKey,
                            shiftKey: e.shiftKey,
                            pageX: e.pageX,
                            pageY: e.pageY
                        });
                        $(e.currentTarget).closest(".jstree-node").children(".jstree-anchor").first().trigger(tmp)
                    }, this)).on("click.jstree", ".jstree-wholerow", function(e) {
                        e.stopImmediatePropagation();
                        var tmp = $.Event("click", {
                            metaKey: e.metaKey,
                            ctrlKey: e.ctrlKey,
                            altKey: e.altKey,
                            shiftKey: e.shiftKey
                        });
                        $(e.currentTarget).closest(".jstree-node").children(".jstree-anchor").first().trigger(tmp).focus()
                    }).on("click.jstree", ".jstree-leaf > .jstree-ocl", $.proxy(function(e) {
                        e.stopImmediatePropagation();
                        var tmp = $.Event("click", {
                            metaKey: e.metaKey,
                            ctrlKey: e.ctrlKey,
                            altKey: e.altKey,
                            shiftKey: e.shiftKey
                        });
                        $(e.currentTarget).closest(".jstree-node").children(".jstree-anchor").first().trigger(tmp).focus()
                    }, this)).on("mouseover.jstree", ".jstree-wholerow, .jstree-icon", $.proxy(function(e) {
                        return e.stopImmediatePropagation(), this.is_disabled(e.currentTarget) || this.hover_node(e.currentTarget), !1
                    }, this)).on("mouseleave.jstree", ".jstree-node", $.proxy(function(e) {
                        this.dehover_node(e.currentTarget)
                    }, this))
                }, this.teardown = function() {
                    this.settings.wholerow && this.element.find(".jstree-wholerow").remove(), parent.teardown.call(this)
                }, this.redraw_node = function(obj, deep, callback, force_render) {
                    if (obj = parent.redraw_node.apply(this, arguments)) {
                        var tmp = div.cloneNode(!0); - 1 !== $.inArray(obj.id, this._data.core.selected) && (tmp.className += " jstree-wholerow-clicked"), this._data.core.focused && this._data.core.focused === obj.id && (tmp.className += " jstree-wholerow-hovered"), obj.insertBefore(tmp, obj.childNodes[0])
                    }
                    return obj
                }
            }, document.registerElement && Object && Object.create) {
            var proto = Object.create(HTMLElement.prototype);
            proto.createdCallback = function() {
                var i, c = {
                    core: {},
                    plugins: []
                };
                for (i in $.jstree.plugins) $.jstree.plugins.hasOwnProperty(i) && this.attributes[i] && (c.plugins.push(i), this.getAttribute(i) && JSON.parse(this.getAttribute(i)) && (c[i] = JSON.parse(this.getAttribute(i))));
                for (i in $.jstree.defaults.core) $.jstree.defaults.core.hasOwnProperty(i) && this.attributes[i] && (c.core[i] = JSON.parse(this.getAttribute(i)) || this.getAttribute(i));
                $(this).jstree(c)
            };
            try {
                document.registerElement("vakata-jstree", {
                    prototype: proto
                })
            } catch (ignore) {}
        }
    }
});