! function(factory) {
    "use strict";
    if ("function" == typeof define && define.amd) define(["knockout"], factory);
    else if ("function" == typeof require && "object" == typeof exports && "object" == typeof module) {
        var ko = require("knockout");
        factory(ko)
    } else factory(window.ko)
}(function(ko) {
    "use strict";
    var init = function(element, valueAccessor, allBindings, viewModel, bindingContext, sortableOptions) {
            var options = buildOptions(valueAccessor, sortableOptions);
            ["onStart", "onEnd", "onRemove", "onAdd", "onUpdate", "onSort", "onFilter"].forEach(function(e) {
                (options[e] || eventHandlers[e]) && (options[e] = function(eventType, parentVM, parentBindings, handler, e) {
                    var itemVM = ko.dataFor(e.item),
                        bindings = ko.utils.peekObservable(parentBindings()),
                        bindingHandlerBinding = bindings.sortable || bindings.draggable,
                        collection = bindingHandlerBinding.collection || bindingHandlerBinding.foreach;
                    handler && handler(e, itemVM, parentVM, collection, bindings), eventHandlers[eventType] && eventHandlers[eventType](e, itemVM, parentVM, collection, bindings)
                }.bind(void 0, e, viewModel, allBindings, options[e]))
            });
            var sortableElement = Sortable.create(element, options);
            return ko.utils.domNodeDisposal.addDisposeCallback(element, function() {
                sortableElement.destroy()
            }), ko.bindingHandlers.template.init(element, valueAccessor)
        },
        update = function(element, valueAccessor, allBindings, viewModel, bindingContext, sortableOptions) {
            return ko.bindingHandlers.template.update(element, valueAccessor, allBindings, viewModel, bindingContext)
        },
        eventHandlers = function(handlers) {
            var moveOperations = [],
                tryMoveOperation = function(e, itemVM, parentVM, collection, parentBindings) {
                    var currentOperation = {
                            event: e,
                            itemVM: itemVM,
                            parentVM: parentVM,
                            collection: collection,
                            parentBindings: parentBindings
                        },
                        existingOperation = moveOperations.filter(function(op) {
                            return op.itemVM === currentOperation.itemVM
                        })[0];
                    if (existingOperation) {
                        moveOperations.splice(moveOperations.indexOf(existingOperation), 1);
                        var removeOperation = "remove" === currentOperation.event.type ? currentOperation : existingOperation,
                            addOperation = "add" === currentOperation.event.type ? currentOperation : existingOperation;
                        moveItem(itemVM, removeOperation.collection, addOperation.collection, addOperation.event.clone, addOperation.event)
                    } else moveOperations.push(currentOperation)
                },
                moveItem = function(itemVM, from, to, clone, e) {
                    var fromArray = from(),
                        originalIndex = fromArray.indexOf(itemVM),
                        newIndex = e.newIndex;
                    e.item.previousElementSibling && (newIndex = fromArray.indexOf(ko.dataFor(e.item.previousElementSibling)), originalIndex > newIndex && (newIndex += 1)), e.item.parentNode.removeChild(e.item), fromArray.splice(originalIndex, 1), from.valueHasMutated(), clone && from !== to && (fromArray.splice(originalIndex, 0, itemVM), from.valueHasMutated()), to().splice(newIndex, 0, itemVM), to.valueHasMutated()
                };
            return handlers.onRemove = tryMoveOperation, handlers.onAdd = tryMoveOperation, handlers.onUpdate = function(e, itemVM, parentVM, collection, parentBindings) {
                moveItem(itemVM, collection, collection, !1, e)
            }, handlers
        }({}),
        buildOptions = function(bindingOptions, options) {
            var merge = function(into, from) {
                    for (var prop in from) "[object Object]" === Object.prototype.toString.call(from[prop]) ? ("[object Object]" !== Object.prototype.toString.call(into[prop]) && (into[prop] = {}), into[prop] = merge(into[prop], from[prop])) : into[prop] = from[prop];
                    return into
                },
                unwrappedOptions = ko.utils.peekObservable(bindingOptions()).options || {};
            return options = merge({}, options), unwrappedOptions.group && "[object Object]" !== Object.prototype.toString.call(unwrappedOptions.group) && (unwrappedOptions.group = {
                name: unwrappedOptions.group
            }), merge(options, unwrappedOptions)
        };
    ko.bindingHandlers.draggable = {
        sortableOptions: {
            group: {
                pull: "clone",
                put: !1
            },
            sort: !1
        },
        init: function(element, valueAccessor, allBindings, viewModel, bindingContext) {
            return init(element, valueAccessor, allBindings, viewModel, 0, ko.bindingHandlers.draggable.sortableOptions)
        },
        update: function(element, valueAccessor, allBindings, viewModel, bindingContext) {
            return update(element, valueAccessor, allBindings, viewModel, bindingContext, ko.bindingHandlers.draggable.sortableOptions)
        }
    }, ko.bindingHandlers.sortable = {
        sortableOptions: {
            group: {
                pull: !0,
                put: !0
            }
        },
        init: function(element, valueAccessor, allBindings, viewModel, bindingContext) {
            return init(element, valueAccessor, allBindings, viewModel, 0, ko.bindingHandlers.sortable.sortableOptions)
        },
        update: function(element, valueAccessor, allBindings, viewModel, bindingContext) {
            return update(element, valueAccessor, allBindings, viewModel, bindingContext, ko.bindingHandlers.sortable.sortableOptions)
        }
    }
});