# Sortable
Sortable is a minimalist JavaScript library for reorderable drag-and-drop lists.

Demo: http://rubaxa.github.io/Sortable/


## Features

 * Supports touch devices and [modern](http://caniuse.com/#search=drag) browsers (including IE9)
 * Can drag from one list to another or within the same list
 * CSS animation when moving items
 * Supports drag handles *and selectable text* (better than voidberg's html5sortable)
 * Smart auto-scrolling
 * Built using native HTML5 drag and drop API
 * Supports [Meteor](meteor/README.md), [AngularJS](#ng), [React](#react) and [Polymer](#polymer)
 * Supports any CSS library, e.g. [Bootstrap](#bs)
 * Simple API
 * [CDN](#cdn)
 * No jQuery (but there is [support](#jq))


<br/>


### Articles
 * [Sortable v1.0 — New capabilities](https://github.com/RubaXa/Sortable/wiki/Sortable-v1.0-—-New-capabilities/) (December 22, 2014)
 * [Sorting with the help of HTML5 Drag'n'Drop API](https://github.com/RubaXa/Sortable/wiki/Sorting-with-the-help-of-HTML5-Drag'n'Drop-API/) (December 23, 2013)


<br/>


### Usage
```html
<ul id="items">
	<li>item 1</li>
	<li>item 2</li>
	<li>item 3</li>
</ul>
```

```js
var el = document.getElementById('items');
var sortable = Sortable.create(el);
```

You can use any element for the list and its elements, not just `ul`/`li`. Here is an [example with `div`s](http://jsbin.com/luxero/2/edit?html,js,output).


---


### Options
```js
var sortable = new Sortable(el, {
	group: "name",  // or { name: "...", pull: [true, false, clone], put: [true, false, array] }
	sort: true,  // sorting inside list
	delay: 0, // time in milliseconds to define when the sorting should start
	disabled: false, // Disables the sortable if set to true.
	store: null,  // @see Store
	animation: 150,  // ms, animation speed moving items when sorting, `0` — without animation
	handle: ".my-handle",  // Drag handle selector within list items
	filter: ".ignore-elements",  // Selectors that do not lead to dragging (String or Function)
	draggable: ".item",  // Specifies which items inside the element should be sortable
	ghostClass: "sortable-ghost",  // Class name for the drop placeholder
	chosenClass: "sortable-chosen",  // Class name for the chosen item
	dataIdAttr: 'data-id',
	
	forceFallback: false,  // ignore the HTML5 DnD behaviour and force the fallback to kick in
	fallbackClass: "sortable-fallback"  // Class name for the cloned DOM Element when using forceFallback
	fallbackOnBody: false  // Appends the cloned DOM Element into the Document's Body
	
	scroll: true, // or HTMLElement
	scrollSensitivity: 30, // px, how near the mouse must be to an edge to start scrolling.
	scrollSpeed: 10, // px
	
	setData: function (dataTransfer, dragEl) {
		dataTransfer.setData('Text', dragEl.textContent);
	},

	// dragging started
	onStart: function (/**Event*/evt) {
		evt.oldIndex;  // element index within parent
	},
	
	// dragging ended
	onEnd: function (/**Event*/evt) {
		evt.oldIndex;  // element's old index within parent
		evt.newIndex;  // element's new index within parent
	},

	// Element is dropped into the list from another list
	onAdd: function (/**Event*/evt) {
		var itemEl = evt.item;  // dragged HTMLElement
		evt.from;  // previous list
		// + indexes from onEnd
	},

	// Changed sorting within list
	onUpdate: function (/**Event*/evt) {
		var itemEl = evt.item;  // dragged HTMLElement
		// + indexes from onEnd
	},

	// Called by any change to the list (add / update / remove)
	onSort: function (/**Event*/evt) {
		// same properties as onUpdate
	},

	// Element is removed from the list into another list
	onRemove: function (/**Event*/evt) {
		// same properties as onUpdate
	},

	// Attempt to drag a filtered element
	onFilter: function (/**Event*/evt) {
		var itemEl = evt.item;  // HTMLElement receiving the `mousedown|tapstart` event.
	},
	
	// Event when you move an item in the list or between lists
	onMove: function (/**Event*/evt) {
		// Example: http://jsbin.com/tuyafe/1/edit?js,output
		evt.dragged; // dragged HTMLElement
		evt.draggedRect; // TextRectangle {left, top, right и bottom}
		evt.related; // HTMLElement on which have guided
		evt.relatedRect; // TextRectangle
		// return false; — for cancel
	}
});
```


---


#### `group` option
To drag elements from one list into another, both lists must have the same `group` value.
You can also define whether lists can give away, give and keep a copy (`clone`), and receive elements.

 * name: `String` — group name
 * pull: `true|false|'clone'` — ability to move from the list. `clone` — copy the item, rather than move.
 * put: `true|false|["foo", "bar"]` — whether elements can be added from other lists, or an array of group names from which elements can be taken. Demo: http://jsbin.com/naduvo/2/edit?html,js,output


---


#### `sort` option
Sorting inside list.

Demo: http://jsbin.com/xizeh/2/edit?html,js,output


---


#### `delay` option
Time in milliseconds to define when the sorting should start.

Demo: http://jsbin.com/xizeh/4/edit?html,js,output


---


#### `disabled` options
Disables the sortable if set to `true`.

Demo: http://jsbin.com/xiloqu/1/edit?html,js,output

```js
var sortable = Sortable.create(list);

document.getElementById("switcher").onclick = function () {
	var state = sortable.option("disabled"); // get

	sortable.option("disabled", !state); // set
};
```


---


#### `handle` option
To make list items draggable, Sortable disables text selection by the user.
That's not always desirable. To allow text selection, define a drag handler,
which is an area of every list element that allows it to be dragged around.

Demo: http://jsbin.com/newize/1/edit?html,js,output

```js
Sortable.create(el, {
	handle: ".my-handle"
});
```

```html
<ul>
	<li><span class="my-handle">::</span> list item text one
	<li><span class="my-handle">::</span> list item text two
</ul>
```

```css
.my-handle {
	cursor: move;
	cursor: -webkit-grabbing;
}
```


---


#### `filter` option


```js
Sortable.create(list, {
	filter: ".js-remove, .js-edit",
	onFilter: function (evt) {
		var item = evt.item,
			ctrl = evt.target;

		if (Sortable.utils.is(ctrl, ".js-remove")) {  // Click on remove button
			item.parentNode.removeChild(item); // remove sortable item
		}
		else if (Sortable.utils.is(ctrl, ".js-edit")) {  // Click on edit link
			// ...
		}
	}
})
```


---


#### `ghostClass` option
Class name for the drop placeholder (default `sortable-ghost`).

Demo: http://jsbin.com/hunifu/1/edit?css,js,output

```css
.ghost {
  opacity: 0.4;
}
```

```js
Sortable.create(list, {
  ghostClass: "ghost"
});
```


---


#### `chosenClass` option
Class name for the chosen item  (default `sortable-chosen`).

Demo: http://jsbin.com/hunifu/edit?html,css,js,output

```css
.chosen {
  color: #fff;
  background-color: #c00;
}
```

```js
Sortable.create(list, {
  delay: 500,
  chosenClass: "chosen"
});
```


---


#### `forceFallback` option
If set to `true`, the Fallback for non HTML5 Browser will be used, even if we are using an HTML5 Browser.
This gives us the possiblity to test the behaviour for older Browsers even in newer Browser, or make the Drag 'n Drop feel more consistent between Desktop , Mobile and old Browsers.

On top of that, the Fallback always generates a copy of that DOM Element and appends the class `fallbackClass` definied in the options. This behaviour controls the look of this 'dragged' Element.

Demo: http://jsbin.com/pucurizace/edit?html,css,js,output


---


#### `scroll` option
If set to `true`, the page (or sortable-area) scrolls when coming to an edge.

Demo:
 - `window`: http://jsbin.com/boqugumiqi/1/edit?html,js,output 
 - `overflow: hidden`: http://jsbin.com/kohamakiwi/1/edit?html,js,output


---


#### `scrollSensitivity` option
Defines how near the mouse must be to an edge to start scrolling.


---


#### `scrollSpeed` option
The speed at which the window should scroll once the mouse pointer gets within the `scrollSensitivity` distance.


---


<a name="ng"></a>
### Support AngularJS
Include [ng-sortable.js](ng-sortable.js)

Demo: http://jsbin.com/naduvo/1/edit?html,js,output

```html
<div ng-app="myApp" ng-controller="demo">
	<ul ng-sortable>
		<li ng-repeat="item in items">{{item}}</li>
	</ul>

	<ul ng-sortable="{ group: 'foobar' }">
		<li ng-repeat="item in foo">{{item}}</li>
	</ul>

	<ul ng-sortable="barConfig">
		<li ng-repeat="item in bar">{{item}}</li>
	</ul>
</div>
```


```js
angular.module('myApp', ['ng-sortable'])
	.controller('demo', ['$scope', function ($scope) {
		$scope.items = ['item 1', 'item 2'];
		$scope.foo = ['foo 1', '..'];
		$scope.bar = ['bar 1', '..'];
		$scope.barConfig = {
			group: 'foobar',
			animation: 150,
			onSort: function (/** ngSortEvent */evt){
				// @see https://github.com/RubaXa/Sortable/blob/master/ng-sortable.js#L18-L24
			}
		};
	}]);
```


---


<a name="react"></a>
### Support React
Include [react-sortable-mixin.js](react-sortable-mixin.js).
See [more options](react-sortable-mixin.js#L26).


```jsx
var SortableList = React.createClass({
	mixins: [SortableMixin],

	getInitialState: function() {
		return {
			items: ['Mixin', 'Sortable']
		};
	},

	handleSort: function (/** Event */evt) { /*..*/ },

	render: function() {
		return <ul>{
			this.state.items.map(function (text) {
				return <li>{text}</li>
			})
		}</ul>
	}
});

React.render(<SortableList />, document.body);


//
// Groups
//
var AllUsers = React.createClass({
	mixins: [SortableMixin],

	sortableOptions: {
		ref: "user",
		group: "shared",
		model: "users"
	},

	getInitialState: function() {
		return { users: ['Abbi', 'Adela', 'Bud', 'Cate', 'Davis', 'Eric']; };
	},

	render: function() {
		return (
			<h1>Users</h1>
			<ul ref="user">{
				this.state.users.map(function (text) {
					return <li>{text}</li>
				})
			}</ul>
		);
	}
});

var ApprovedUsers = React.createClass({
	mixins: [SortableMixin],
	sortableOptions: { group: "shared" },

	getInitialState: function() {
		return { items: ['Hal', 'Judy']; };
	},

	render: function() {
		return <ul>{
			this.state.items.map(function (text) {
				return <li>{text}</li>
			})
		}</ul>
	}
});

React.render(<div>
	<AllUsers/>
	<hr/>
	<ApprovedUsers/>
</div>, document.body);
```


---


<a name="ko"></a>
### Support KnockoutJS
Include [knockout-sortable.js](knockout-sortable.js)

```html
<div data-bind="sortable: {foreach: yourObservableArray, options: {/* sortable options here */}}">
	<!-- optional item template here -->
</div>

<div data-bind="draggable: {foreach: yourObservableArray, options: {/* sortable options here */}}">
	<!-- optional item template here -->
</div>
```

Using this bindingHandler sorts the observableArray when the user sorts the HTMLElements.

The sortable/draggable bindingHandlers supports the same syntax as Knockouts built in [template](http://knockoutjs.com/documentation/template-binding.html) binding except for the `data` option, meaning that you could supply the name of a template or specify a separate templateEngine. The difference between the sortable and draggable handlers is that the draggable has the sortable `group` option set to `{pull:'clone',put: false}` and the `sort` option set to false by default (overridable).

Other attributes are:
*	options: an object that contains settings for the underlaying sortable, ie `group`,`handle`, events etc.
*	collection: if your `foreach` array is a computed then you would supply the underlaying observableArray that you would like to sort here.


---

<a name="polymer"></a>
### Support Polymer
```html

<link rel="import" href="bower_components/Sortable/Sortable-js.html">

<sortable-js handle=".handle">
  <template is="dom-repeat" items={{names}}>
    <div>{{item}}</div>
  </template>
<sortable-js>
```

### Method


##### option(name:`String`[, value:`*`]):`*`
Get or set the option.



##### closest(el:`String`[, selector:`HTMLElement`]):`HTMLElement|null`
For each element in the set, get the first element that matches the selector by testing the element itself and traversing up through its ancestors in the DOM tree.


##### toArray():`String[]`
Serializes the sortable's item `data-id`'s (`dataIdAttr` option) into an array of string.


##### sort(order:`String[]`)
Sorts the elements according to the array.

```js
var order = sortable.toArray();
sortable.sort(order.reverse()); // apply
```


##### save()
Save the current sorting (see [store](#store))


##### destroy()
Removes the sortable functionality completely.


---


<a name="store"></a>
### Store
Saving and restoring of the sort.

```html
<ul>
	<li data-id="1">order</li>
	<li data-id="2">save</li>
	<li data-id="3">restore</li>
</ul>
```

```js
Sortable.create(el, {
	group: "localStorage-example",
	store: {
		/**
		 * Get the order of elements. Called once during initialization.
		 * @param   {Sortable}  sortable
		 * @returns {Array}
		 */
		get: function (sortable) {
			var order = localStorage.getItem(sortable.options.group);
			return order ? order.split('|') : [];
		},

		/**
		 * Save the order of elements. Called onEnd (when the item is dropped).
		 * @param {Sortable}  sortable
		 */
		set: function (sortable) {
			var order = sortable.toArray();
			localStorage.setItem(sortable.options.group, order.join('|'));
		}
	}
})
```


---


<a name="bs"></a>
### Bootstrap
Demo: http://jsbin.com/luxero/2/edit?html,js,output

```html
<!-- Latest compiled and minified CSS -->
<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.1/css/bootstrap.min.css"/>


<!-- Latest Sortable -->
<script src="http://rubaxa.github.io/Sortable/Sortable.js"></script>


<!-- Simple List -->
<ul id="simpleList" class="list-group">
	<li class="list-group-item">This is <a href="http://rubaxa.github.io/Sortable/">Sortable</a></li>
	<li class="list-group-item">It works with Bootstrap...</li>
	<li class="list-group-item">...out of the box.</li>
	<li class="list-group-item">It has support for touch devices.</li>
	<li class="list-group-item">Just drag some elements around.</li>
</ul>

<script>
    // Simple list
    Sortable.create(simpleList, { /* options */ });
</script>
```


---


### Static methods & properties



##### Sortable.create(el:`HTMLElement`[, options:`Object`]):`Sortable`
Create new instance.


---


##### Sortable.active:`Sortable`
Link to the active instance.


---


##### Sortable.utils
* on(el`:HTMLElement`, event`:String`, fn`:Function`) — attach an event handler function
* off(el`:HTMLElement`, event`:String`, fn`:Function`) — remove an event handler
* css(el`:HTMLElement`)`:Object` — get the values of all the CSS properties
* css(el`:HTMLElement`, prop`:String`)`:Mixed` — get the value of style properties
* css(el`:HTMLElement`, prop`:String`, value`:String`) — set one CSS properties
* css(el`:HTMLElement`, props`:Object`) — set more CSS properties
* find(ctx`:HTMLElement`, tagName`:String`[, iterator`:Function`])`:Array` — get elements by tag name
* bind(ctx`:Mixed`, fn`:Function`)`:Function` — Takes a function and returns a new one that will always have a particular context
* is(el`:HTMLElement`, selector`:String`)`:Boolean` — check the current matched set of elements against a selector
* closest(el`:HTMLElement`, selector`:String`[, ctx`:HTMLElement`])`:HTMLElement|Null` — for each element in the set, get the first element that matches the selector by testing the element itself and traversing up through its ancestors in the DOM tree
* toggleClass(el`:HTMLElement`, name`:String`, state`:Boolean`) — add or remove one classes from each element


---


<a name="cdn"></a>
### CDN

```html
<!-- CDNJS :: Sortable (https://cdnjs.com/) -->
<script src="//cdnjs.cloudflare.com/ajax/libs/Sortable/1.4.2/Sortable.min.js"></script>


<!-- jsDelivr :: Sortable (http://www.jsdelivr.com/) -->
<script src="//cdn.jsdelivr.net/sortable/1.4.2/Sortable.min.js"></script>


<!-- jsDelivr :: Sortable :: Latest (http://www.jsdelivr.com/) -->
<script src="//cdn.jsdelivr.net/sortable/latest/Sortable.min.js"></script>
```


---


<a name="jq"></a>
### jQuery compatibility
To assemble plugin for jQuery, perform the following steps:

```bash
  cd Sortable
  npm install
  grunt jquery
```

Now you can use `jquery.fn.sortable.js`:<br/>
(or `jquery.fn.sortable.min.js` if you run `grunt jquery:min`)

```js
  $("#list").sortable({ /* options */ }); // init
  
  $("#list").sortable("widget"); // get Sortable instance
  
  $("#list").sortable("destroy"); // destroy Sortable instance
  
  $("#list").sortable("{method-name}"); // call an instance method
  
  $("#list").sortable("{method-name}", "foo", "bar"); // call an instance method with parameters
```

And `grunt jquery:mySortableFunc` → `jquery.fn.mySortableFunc.js`

---


### Contributing (Issue/PR)

Please, [read this](CONTRIBUTING.md). 


---


## MIT LICENSE
Copyright 2013-2015 Lebedev Konstantin <ibnRubaXa@gmail.com>
http://rubaxa.github.io/Sortable/

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

