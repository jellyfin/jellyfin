Meteor.startup(function () {
  if (Types.find().count() === 0) {
    [
      {
        name: 'String',
        icon: '<span class="glyphicon glyphicon-tag" aria-hidden="true"></span>'
      },
      {
        name: 'Text, multi-line',
        icon: '<i class="mdi-communication-message" aria-hidden="true"></i>'
      },
      {
        name: 'Category',
        icon: '<span class="glyphicon glyphicon-list" aria-hidden="true"></span>'
      },
      {
        name: 'Number',
        icon: '<i class="mdi-image-looks-one" aria-hidden="true"></i>'
      },
      {
        name: 'Date',
        icon: '<span class="glyphicon glyphicon-calendar" aria-hidden="true"></span>'
      },
      {
        name: 'Hyperlink',
        icon: '<span class="glyphicon glyphicon-link" aria-hidden="true"></span>'
      },
      {
        name: 'Image',
        icon: '<span class="glyphicon glyphicon-picture" aria-hidden="true"></span>'
      },
      {
        name: 'Progress',
        icon: '<span class="glyphicon glyphicon-info-sign" aria-hidden="true"></span>'
      },
      {
        name: 'Duration',
        icon: '<span class="glyphicon glyphicon-time" aria-hidden="true"></span>'
      },
      {
        name: 'Map address',
        icon: '<i class="mdi-maps-place" aria-hidden="true"></i>'
      },
      {
        name: 'Relationship',
        icon: '<span class="glyphicon glyphicon-flash" aria-hidden="true"></span>'
      }
    ].forEach(function (type, i) {
        Types.insert({
          name: type.name,
          icon: type.icon,
          order: i
        });
      }
    );
    console.log('Initialized attribute types.');
  }

  if (Attributes.find().count() === 0) {
    [
      { name: 'Name', type: 'String' },
      { name: 'Created at', type: 'Date' },
      { name: 'Link', type: 'Hyperlink' },
      { name: 'Owner', type: 'Relationship' }
    ].forEach(function (attribute, i) {
        Attributes.insert({
          name: attribute.name,
          type: attribute.type,
          order: i
        });
      }
    );
    console.log('Created sample object type.');
  }
});
