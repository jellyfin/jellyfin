// Define an object type by dragging together attributes

Template.typeDefinition.helpers({
  types: function () {
    return Types.find({}, { sort: { order: 1 } });
  },
  typesOptions: {
    sortField: 'order',  // defaults to 'order' anyway
    group: {
      name: 'typeDefinition',
      pull: 'clone',
      put: false
    },
    sort: false  // don't allow reordering the types, just the attributes below
  },

  attributes: function () {
    return Attributes.find({}, {
      sort: { order: 1 },
      transform: function (doc) {
        doc.icon = Types.findOne({name: doc.type}).icon;
        return doc;
      }
    });
  },
  attributesOptions: {
    group: {
      name: 'typeDefinition',
      put: true
    },
    onAdd: function (event) {
      delete event.data._id; // Generate a new id when inserting in the Attributes collection. Otherwise, if we add the same type twice, we'll get an error that the ids are not unique.
      delete event.data.icon;
      event.data.type = event.data.name;
      event.data.name = 'Rename me (double click)'
    },
    // event handler for reordering attributes
    onSort: function (event) {
      console.log('Item %s went from #%d to #%d',
          event.data.name, event.oldIndex, event.newIndex
      );
    }
  }
});

Template.sortableItemTarget.events({
  'dblclick .name': function (event, template) {
    // Make the name editable. We should use an existing component, but it's
    // in a sorry state - https://github.com/arillo/meteor-x-editable/issues/1
    var name = template.$('.name');
    var input = template.$('input');
    if (input.length) {  // jQuery never returns null - http://stackoverflow.com/questions/920236/how-can-i-detect-if-a-selector-returns-null
      input.show();
    } else {
      input = $('<input class="form-control" type="text" placeholder="' + this.name + '" style="display: inline">');
      name.after(input);
    }
    name.hide();
    input.focus();
  },
  'blur input[type=text]': function (event, template) {
    // commit the change to the name, if any
    var input = template.$('input');
    input.hide();
    template.$('.name').show();
    // TODO - what is the collection here? We'll hard-code for now.
    // https://github.com/meteor/meteor/issues/3303
    if (this.name !== input.val() && this.name !== '')
      Attributes.update(this._id, {$set: {name: input.val()}});
  },
  'keydown input[type=text]': function (event, template) {
    if (event.which === 27) {
      // ESC - discard edits and keep existing value
      template.$('input').val(this.name);
      event.preventDefault();
      event.target.blur();
    } else if (event.which === 13) {
      // ENTER
      event.preventDefault();
      event.target.blur();
    }
  }
});

// you can add events to all Sortable template instances
Template.sortable.events({
  'click .close': function (event, template) {
    // `this` is the data context set by the enclosing block helper (#each, here)
    template.collection.remove(this._id);
    // custom code, working on a specific collection
    if (Attributes.find().count() === 0) {
      Meteor.setTimeout(function () {
        Attributes.insert({
          name: 'Not nice to delete the entire list! Add some attributes instead.',
          type: 'String',
          order: 0
        })
      }, 1000);
    }
  }
});
