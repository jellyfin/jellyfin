# RubaXa:Sortable Meteor demo

This demo showcases the two-way integration between the reorderable list
widget [Sortable](https://github.com/RubaXa/Sortable/) and Meteor.js. Meteor
Mongo collections are updated when items are added, removed or reordered, and
the order is persisted.

It also shows list grouping and control over what lists can give or receive
elements. You can only drag elements from the list to the left onto the list
to the right.


## Usage

The example uses the local package from the checkout, with the help of the run script:

### Windows

    git clone https://github.com/RubaXa/Sortable.git
    cd Sortable
    # git checkout dev  # optional
    meteor\example\run.bat

### Elsewhere

    git clone https://github.com/RubaXa/Sortable.git
    cd Sortable
    # git checkout dev  # optional
    meteor/example/run.sh


## [Prior art](http://slides.com/dandv/prior-art)

### Differential

Differential wrote [a blog post on reorderable lists with
Meteor](http://differential.com/blog/sortable-lists-in-meteor-using-jquery-ui) and
[jQuery UI Sortable](http://jqueryui.com/sortable/). It served as inspiration
for integrating [rubaxa:sortable](rubaxa.github.io/Sortable/),
which uses the HTML5 native drag&drop API (not without [its
limitations](https://github.com/RubaXa/Sortable/issues/106)).
The reordering method used by the Differential example can lead to data loss
though, because it calculates the new order of a dropped element as the
arithmetic mean of the elements before and after it. This [runs into limitations
of floating point precision](http://programmers.stackexchange.com/questions/266451/maintain-ordered-collection-by-updating-as-few-order-fields-as-possible)
in JavaScript after <50 reorderings.

### Todos animated

http://todos-dnd-animated.meteor.com/ ([source](https://github.com/nleush/meteor-todos-sortable-animation))
is based on old Meteor Blaze (back then Spark) API, and won't work with current versions.
It does showcase some neat features, such as animation when collection elements
are reordered by another client. It uses jQuery UI Sortable as well, which lacks
some features vs. rubaxa:Sortable, e.g. text selection within the item.

## TODO

* Animation
* Indication that an item is being edited
