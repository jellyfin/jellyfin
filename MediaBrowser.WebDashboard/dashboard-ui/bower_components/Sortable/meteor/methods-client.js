'use strict';

Meteor.methods({
	/**
	 * Update the sortField of documents with given ids in a collection, incrementing it by incDec
	 * @param {String} collectionName - name of the collection to update
	 * @param {String[]} ids - array of document ids
	 * @param {String} orderField - the name of the order field, usually "order"
	 * @param {Number} incDec - pass 1 or -1
	 */
	'rubaxa:sortable/collection-update': function (collectionName, ids, sortField, incDec) {
		var selector = {_id: {$in: ids}}, modifier = {$inc: {}};
		modifier.$inc[sortField] = incDec;
		Mongo.Collection.get(collectionName).update(selector, modifier, {multi: true});
	}
});
