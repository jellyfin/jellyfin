Reactive reorderable lists with [Sortable](http://rubaxa.github.io/Sortable/),
backed by [Meteor.js](http://meteor.com) collections:

* new elements arriving in the collection will update the list as you expect
* elements removed from the collection will be removed from the list
* drag and drop between lists updates collections accordingly

Demo: http://rubaxa-sortable.meteor.com

# Meteor

If you're new to Meteor, here's what the excitement is all about -
[watch the first two minutes](https://www.youtube.com/watch?v=fsi0aJ9yr2o); you'll be hooked by 1:28.
That screencast is from 2012. In the meantime, Meteor has become a mature JavaScript-everywhere web
development framework. Read more at [Why Meteor](http://wiki.dandascalescu.com/essays/why_meteor).


# Usage

Simplest invocation - order will be lost when the page is refreshed:

```handlebars
{{#sortable <collection|cursor|array>}}
```

Persist the sort order in the 'order' field of each document in the collection:

*Client:*

```handlebars
{{#sortable items=<collection|cursor|array> sortField="order"}}
```

*Server:*

```js
Sortable.collections = <collectionName>;  // the name, not the variable
```

Along with `items`, `sortField` is the only Meteor-specific option. If it's missing, the package will
assume there is a field called "order" in the collection, holding unique `Number`s such that every
`order` differs from that before and after it by at least 1. Basically, keep to 0, 1, 2, ... .
Try not to depend on a particular format for this field; it *is* though guaranteed that a `sort` will
produce lexicographical order, and that the order will be maintained after an arbitrary number of
reorderings, unlike with [naive solutions](http://programmers.stackexchange.com/questions/266451/maintain-ordered-collection-by-updating-as-few-order-fields-as-possible).

Remember to declare on the server which collections you want to be reorderable from the client.
Otherwise, the library will error because the client would be able to modify numerical fields in
any collection, which represents a security risk.


## Passing options to the Sortable library

    {{#sortable items=<collection|cursor|array> option1=value1 option2=value2...}}
    {{#sortable items=<collection|cursor|array> options=myOptions}}

For available options, please refer to [the main README](../README.md#options). You can pass them directly
or under the `options` object. Direct options (`key=value`) override those in `options`. It is best
to pass presentation-related options directly, and functionality-related settings in an `options`
object, as this will enable designers to work without needing to inspect the JavaScript code:

    <template name="myTemplate">
      ...
      {{#sortable items=Players handle=".sortable-handle" ghostClass="sortable-ghost" options=playerOptions}}
    </template>

Define the options in a helper for the template that calls Sortable:

```js
Template.myTemplate.helpers({
    playerOptions: function () {
        return {
            group: {
                name: "league",
                pull: true,
                put: false
            },
            sort: false
        };
    }
});
```

#### Meteor-specific options

* `selector` - you can specify a collection selector if your list operates only on a subset of the collection. Example:

```js
Template.myTemplate.helpers({
   playerOptions: function() {
      return {
         selector: { city: 'San Francisco' }
      }
   }
});
```


## Events

All the original Sortable events are supported. In addition, they will receive
the data context in `event.data`. You can access `event.data.order` this way:

```handlebars
{{#sortable items=players options=playersOptions}}
```

```js
Template.myTemplate.helpers({
    playersOptions: function () {
        return {
            onSort: function(/**Event*/event) {
                console.log('Moved player #%d from %d to %d',
                    event.data.order, event.oldIndex, event.newIndex
                );
            }
        };
    }
});
```


# Issues

If you encounter an issue while using this package, please CC @dandv when you file it in this repo.


# TODO

* Array support
* Tests
* Misc. - see reactivize.js
* [GitHub issues](https://github.com/RubaXa/Sortable/labels/%E2%98%84%20meteor)
