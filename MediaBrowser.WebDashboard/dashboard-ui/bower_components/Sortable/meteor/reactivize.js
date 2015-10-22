/*
Make a Sortable reactive by binding it to a Mongo.Collection.
Calls `rubaxa:sortable/collection-update` on the server to update the sortField of affected records.

TODO:
  * supply consecutive values if the `order` field doesn't have any
  * .get(DOMElement) - return the Sortable object of a DOMElement
  * create a new _id automatically onAdd if the event.from list had pull: 'clone'
  * support arrays
    * sparse arrays
  * tests
    * drop onto existing empty lists
    * insert back into lists emptied by dropping
    * performance on dragging into long list at the beginning
  * handle failures on Collection operations, e.g. add callback to .insert
  * when adding elements, update ranks just for the half closer to the start/end of the list
  * revisit http://programmers.stackexchange.com/questions/266451/maintain-ordered-collection-by-updating-as-few-order-fields-as-possible
  * reproduce the insidious bug where the list isn't always sorted (fiddle with dragging #1 over #2, then back, then #N before #1)

 */

'use strict';

Template.sortable.created = function () {
	var templateInstance = this;
	// `this` is a template instance that can store properties of our choice - http://docs.meteor.com/#/full/template_inst
	if (templateInstance.setupDone) return;  // paranoid: only run setup once
	// this.data is the data context - http://docs.meteor.com/#/full/template_data
	// normalize all options into templateInstance.options, and remove them from .data
	templateInstance.options = templateInstance.data.options || {};
	Object.keys(templateInstance.data).forEach(function (key) {
		if (key === 'options' || key === 'items') return;
		templateInstance.options[key] = templateInstance.data[key];
		delete templateInstance.data[key];
	});
	templateInstance.options.sortField = templateInstance.options.sortField || 'order';
	// We can get the collection via the .collection property of the cursor, but changes made that way
	// will NOT be sent to the server - https://github.com/meteor/meteor/issues/3271#issuecomment-66656257
	// Thus we need to use dburles:mongo-collection-instances to get a *real* collection
	if (templateInstance.data.items && templateInstance.data.items.collection) {
		// cursor passed via items=; its .collection works client-only and has a .name property
		templateInstance.collectionName = templateInstance.data.items.collection.name;
		templateInstance.collection = Mongo.Collection.get(templateInstance.collectionName);
	}	else if (templateInstance.data.items) {
		// collection passed via items=; does NOT have a .name property, but _name
		templateInstance.collection = templateInstance.data.items;
		templateInstance.collectionName = templateInstance.collection._name;
	}	else if (templateInstance.data.collection) {
	  // cursor passed directly
		templateInstance.collectionName = templateInstance.data.collection.name;
		templateInstance.collection = Mongo.Collection.get(templateInstance.collectionName);
	} else {
		templateInstance.collection = templateInstance.data;  // collection passed directly
		templateInstance.collectionName = templateInstance.collection._name;
	}

	// TODO if (Array.isArray(templateInstance.collection))

	// What if user filters some of the items in the cursor, instead of ordering the entire collection?
	// Use case: reorder by preference movies of a given genre, a filter within all movies.
	// A: Modify all intervening items **that are on the client**, to preserve the overall order
	// TODO: update *all* orders via a server method that takes not ids, but start & end elements - mild security risk
	delete templateInstance.data.options;

	/**
	 * When an element was moved, adjust its orders and possibly the order of
	 * other elements, so as to maintain a consistent and correct order.
	 *
	 * There are three approaches to this:
	 * 1) Using arbitrary precision arithmetic and setting only the order of the moved
	 *    element to the average of the orders of the elements around it -
	 *    http://programmers.stackexchange.com/questions/266451/maintain-ordered-collection-by-updating-as-few-order-fields-as-possible
	 *    The downside is that the order field in the DB will increase by one byte every
	 *    time an element is reordered.
	 * 2) Adjust the orders of the intervening items. This keeps the orders sane (integers)
	 *    but is slower because we have to modify multiple documents.
	 *    TODO: we may be able to update fewer records by only altering the
	 *    order of the records between the newIndex/oldIndex and the start/end of the list.
	 * 3) Use regular precision arithmetic, but when the difference between the orders of the
	 *    moved item and the one before/after it falls below a certain threshold, adjust
	 *    the order of that other item, and cascade doing so up or down the list.
	 *    This will keep the `order` field constant in size, and will only occasionally
	 *    require updating the `order` of other records.
	 *
	 * For now, we use approach #2.
	 *
	 * @param {String} itemId - the _id of the item that was moved
	 * @param {Number} orderPrevItem - the order of the item before it, or null
	 * @param {Number} orderNextItem - the order of the item after it, or null
	 */
	templateInstance.adjustOrders = function adjustOrders(itemId, orderPrevItem, orderNextItem) {
		var orderField = templateInstance.options.sortField;
		var selector = templateInstance.options.selector || {}, modifier = {$set: {}};
		var ids = [];
		var startOrder = templateInstance.collection.findOne(itemId)[orderField];
		if (orderPrevItem !== null) {
			// Element has a previous sibling, therefore it was moved down in the list.
			// Decrease the order of intervening elements.
			selector[orderField] = {$lte: orderPrevItem, $gt: startOrder};
			ids = _.pluck(templateInstance.collection.find(selector, {fields: {_id: 1}}).fetch(), '_id');
			Meteor.call('rubaxa:sortable/collection-update', templateInstance.collectionName, ids, orderField, -1);

			// Set the order of the dropped element to the order of its predecessor, whose order was decreased
			modifier.$set[orderField] = orderPrevItem;
		} else {
			// element moved up the list, increase order of intervening elements
			selector[orderField] = {$gte: orderNextItem, $lt: startOrder};
			ids = _.pluck(templateInstance.collection.find(selector, {fields: {_id: 1}}).fetch(), '_id');
			Meteor.call('rubaxa:sortable/collection-update', templateInstance.collectionName, ids, orderField, 1);

			// Set the order of the dropped element to the order of its successor, whose order was increased
			modifier.$set[orderField] = orderNextItem;
		}
		templateInstance.collection.update(itemId, modifier);
	};

	templateInstance.setupDone = true;
};


Template.sortable.rendered = function () {
  var templateInstance = this;
	var orderField = templateInstance.options.sortField;

	// sorting was changed within the list
	var optionsOnUpdate = templateInstance.options.onUpdate;
	templateInstance.options.onUpdate = function sortableUpdate(/**Event*/event) {
		var itemEl = event.item;  // dragged HTMLElement
		event.data = Blaze.getData(itemEl);
		if (event.newIndex < event.oldIndex) {
			// Element moved up in the list. The dropped element has a next sibling for sure.
			var orderNextItem = Blaze.getData(itemEl.nextElementSibling)[orderField];
			templateInstance.adjustOrders(event.data._id, null, orderNextItem);
		} else if (event.newIndex > event.oldIndex) {
			// Element moved down in the list. The dropped element has a previous sibling for sure.
			var orderPrevItem = Blaze.getData(itemEl.previousElementSibling)[orderField];
			templateInstance.adjustOrders(event.data._id, orderPrevItem, null);
		} else {
			// do nothing - drag and drop in the same location
		}
		if (optionsOnUpdate) optionsOnUpdate(event);
	};

	// element was added from another list
	var optionsOnAdd = templateInstance.options.onAdd;
	templateInstance.options.onAdd = function sortableAdd(/**Event*/event) {
		var itemEl = event.item;  // dragged HTMLElement
		event.data = Blaze.getData(itemEl);
		// let the user decorate the object with additional properties before insertion
		if (optionsOnAdd) optionsOnAdd(event);

		// Insert the new element at the end of the list and move it where it was dropped.
		// We could insert it at the beginning, but that would lead to negative orders.
		var sortSpecifier = {}; sortSpecifier[orderField] = -1;
		event.data.order = templateInstance.collection.findOne({}, { sort: sortSpecifier, limit: 1 }).order + 1;
		// TODO: this can obviously be optimized by setting the order directly as the arithmetic average, with the caveats described above
		var newElementId = templateInstance.collection.insert(event.data);
		event.data._id = newElementId;
		if (itemEl.nextElementSibling) {
			var orderNextItem = Blaze.getData(itemEl.nextElementSibling)[orderField];
			templateInstance.adjustOrders(newElementId, null, orderNextItem);
		} else {
			// do nothing - inserted after the last element
		}
		// remove the dropped HTMLElement from the list because we have inserted it in the collection, which will update the template
		itemEl.parentElement.removeChild(itemEl);
	};

	// element was removed by dragging into another list
	var optionsOnRemove = templateInstance.options.onRemove;
	templateInstance.options.onRemove = function sortableRemove(/**Event*/event) {
		var itemEl = event.item;  // dragged HTMLElement
		event.data = Blaze.getData(itemEl);
		// don't remove from the collection if group.pull is clone or false
		if (typeof templateInstance.options.group === 'undefined'
				|| typeof templateInstance.options.group.pull === 'undefined'
				|| templateInstance.options.group.pull === true
		) templateInstance.collection.remove(event.data._id);
		if (optionsOnRemove) optionsOnRemove(event);
	};

	// just compute the `data` context
	['onStart', 'onEnd', 'onSort', 'onFilter'].forEach(function (eventHandler) {
		if (templateInstance.options[eventHandler]) {
			var userEventHandler = templateInstance.options[eventHandler];
			templateInstance.options[eventHandler] = function (/**Event*/event) {
				var itemEl = event.item;  // dragged HTMLElement
				event.data = Blaze.getData(itemEl);
				userEventHandler(event);
			};
		}
	});

	templateInstance.sortable = Sortable.create(templateInstance.firstNode.parentElement, templateInstance.options);
	// TODO make the object accessible, e.g. via Sortable.getSortableById() or some such
};


Template.sortable.destroyed = function () {
	if(this.sortable) this.sortable.destroy();
};
