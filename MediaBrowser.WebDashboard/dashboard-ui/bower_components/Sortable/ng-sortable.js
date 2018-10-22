! function(factory) {
    "use strict";
    "function" == typeof define && define.amd ? define(["angular", "./Sortable"], factory) : "function" == typeof require && "object" == typeof exports && "object" == typeof module ? (require("angular"), factory(angular, require("./Sortable")), module.exports = "ng-sortable") : window.angular && window.Sortable && factory(angular, Sortable)
}(function(angular, Sortable) {
    "use strict";
    var expando = "Sortable:ng-sortable";
    angular.module("ng-sortable", []).constant("ngSortableVersion", "0.4.0").constant("ngSortableConfig", {}).directive("ngSortable", ["$parse", "ngSortableConfig", function($parse, ngSortableConfig) {
        var removed, nextSibling, getSourceFactory = function(el, scope) {
            var ngRepeat = [].filter.call(el.childNodes, function(node) {
                return 8 === node.nodeType && -1 !== node.nodeValue.indexOf("ngRepeat:")
            })[0];
            if (!ngRepeat) return function() {
                return null
            };
            ngRepeat = ngRepeat.nodeValue.match(/ngRepeat:\s*(?:\(.*?,\s*)?([^\s)]+)[\s)]+in\s+([^\s|]+)/);
            var itemsExpr = $parse(ngRepeat[2]);
            return function() {
                return itemsExpr(scope.$parent) || []
            }
        };
        return {
            restrict: "AC",
            scope: {
                ngSortable: "=?"
            },
            link: function(scope, $el) {
                function _emitEvent(evt, item) {
                    var name = "on" + evt.type.charAt(0).toUpperCase() + evt.type.substr(1),
                        source = getSource();
                    options[name] && options[name]({
                        model: item || source[evt.newIndex],
                        models: source,
                        oldIndex: evt.oldIndex,
                        newIndex: evt.newIndex
                    })
                }

                function _sync(evt) {
                    var items = getSource();
                    if (items) {
                        var oldIndex = evt.oldIndex,
                            newIndex = evt.newIndex;
                        if (el !== evt.from) {
                            var prevItems = evt.from[expando]();
                            removed = prevItems[oldIndex], evt.clone ? (removed = angular.copy(removed), prevItems.splice(Sortable.utils.index(evt.clone), 0, prevItems.splice(oldIndex, 1)[0]), evt.from.removeChild(evt.clone)) : prevItems.splice(oldIndex, 1), items.splice(newIndex, 0, removed), evt.from.insertBefore(evt.item, nextSibling)
                        } else items.splice(newIndex, 0, items.splice(oldIndex, 1)[0]);
                        scope.$apply()
                    }
                }
                var sortable, el = $el[0],
                    options = angular.extend(scope.ngSortable || {}, ngSortableConfig),
                    watchers = [],
                    getSource = getSourceFactory(el, scope);
                el[expando] = getSource, sortable = Sortable.create(el, Object.keys(options).reduce(function(opts, name) {
                    return opts[name] = opts[name] || options[name], opts
                }, {
                    onStart: function(evt) {
                        nextSibling = evt.item.nextSibling, _emitEvent(evt), scope.$apply()
                    },
                    onEnd: function(evt) {
                        _emitEvent(evt, removed), scope.$apply()
                    },
                    onAdd: function(evt) {
                        _sync(evt), _emitEvent(evt, removed), scope.$apply()
                    },
                    onUpdate: function(evt) {
                        _sync(evt), _emitEvent(evt)
                    },
                    onRemove: function(evt) {
                        _emitEvent(evt, removed)
                    },
                    onSort: function(evt) {
                        _emitEvent(evt)
                    }
                })), $el.on("$destroy", function() {
                    angular.forEach(watchers, function(unwatch) {
                        unwatch()
                    }), sortable.destroy(), el[expando] = null, el = null, watchers = null, sortable = null, nextSibling = null
                }), angular.forEach(["sort", "disabled", "draggable", "handle", "animation", "group", "ghostClass", "filter", "onStart", "onEnd", "onAdd", "onUpdate", "onRemove", "onSort"], function(name) {
                    watchers.push(scope.$watch("ngSortable." + name, function(value) {
                        void 0 !== value && (options[name] = value, /^on[A-Z]/.test(name) || sortable.option(name, value))
                    }))
                })
            }
        }
    }])
});