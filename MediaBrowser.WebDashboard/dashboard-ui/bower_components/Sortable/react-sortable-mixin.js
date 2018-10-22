! function(factory) {
    "use strict";
    "undefined" != typeof module && void 0 !== module.exports ? module.exports = factory(require("./Sortable")) : "function" == typeof define && define.amd ? define(["./Sortable"], factory) : window.SortableMixin = factory(Sortable)
}(function(Sortable) {
    "use strict";

    function _getModelName(component) {
        return component.sortableOptions && component.sortableOptions.model || _defaultOptions.model
    }

    function _getModelItems(component) {
        var name = _getModelName(component);
        return (component.state && component.state[name] || component.props[name]).slice()
    }

    function _extend(dst, src) {
        for (var key in src) src.hasOwnProperty(key) && (dst[key] = src[key]);
        return dst
    }
    var _nextSibling, _activeComponent, _defaultOptions = {
        ref: "list",
        model: "items",
        animation: 100,
        onStart: "handleStart",
        onEnd: "handleEnd",
        onAdd: "handleAdd",
        onUpdate: "handleUpdate",
        onRemove: "handleRemove",
        onSort: "handleSort",
        onFilter: "handleFilter",
        onMove: "handleMove"
    };
    return {
        sortableMixinVersion: "0.1.1",
        _sortableInstance: null,
        componentDidMount: function() {
            var DOMNode, options = _extend(_extend({}, _defaultOptions), this.sortableOptions || {}),
                copyOptions = _extend({}, options),
                emitEvent = function(type, evt) {
                    var method = this[options[type]];
                    method && method.call(this, evt, this._sortableInstance)
                }.bind(this);
            "onStart onEnd onAdd onSort onUpdate onRemove onFilter onMove".split(" ").forEach(function(name) {
                copyOptions[name] = function(evt) {
                    if ("onStart" === name) _nextSibling = evt.item.nextElementSibling, _activeComponent = this;
                    else if ("onAdd" === name || "onUpdate" === name) {
                        evt.from.insertBefore(evt.item, _nextSibling);
                        var remoteItems, item, newState = {},
                            remoteState = {},
                            oldIndex = evt.oldIndex,
                            newIndex = evt.newIndex,
                            items = _getModelItems(this);
                        "onAdd" === name ? (remoteItems = _getModelItems(_activeComponent), item = remoteItems.splice(oldIndex, 1)[0], items.splice(newIndex, 0, item), remoteState[_getModelName(_activeComponent)] = remoteItems) : items.splice(newIndex, 0, items.splice(oldIndex, 1)[0]), newState[_getModelName(this)] = items, copyOptions.stateHandler ? this[copyOptions.stateHandler](newState) : this.setState(newState), this !== _activeComponent && _activeComponent.setState(remoteState)
                    }
                    setTimeout(function() {
                        emitEvent(name, evt)
                    }, 0)
                }.bind(this)
            }, this), DOMNode = this.getDOMNode() ? (this.refs[options.ref] || this).getDOMNode() : this.refs[options.ref] || this, this._sortableInstance = Sortable.create(DOMNode, copyOptions)
        },
        componentWillReceiveProps: function(nextProps) {
            var newState = {},
                modelName = _getModelName(this),
                items = nextProps[modelName];
            items && (newState[modelName] = items, this.setState(newState))
        },
        componentWillUnmount: function() {
            this._sortableInstance.destroy(), this._sortableInstance = null
        }
    }
});