
<!---

This README is automatically generated from the comments in these files:
iron-scroll-threshold.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

_[Demo and API Docs](https://elements.polymer-project.org/elements/iron-scroll-threshold)_


##&lt;iron-scroll-threshold&gt;

`iron-scroll-threshold` is a utility element that listens for `scroll` events from a
scrollable region and fires events to indicate when the scroller has reached a pre-defined
limit, specified in pixels from the upper and lower bounds of the scrollable region.
This element may wrap a scrollable region and will listen for `scroll` events bubbling
through it from its children.  In this case, care should be taken that only one scrollable
region with the same orientation as this element is contained within. Alternatively,
the `scrollTarget` property can be set/bound to a non-child scrollable region, from which
it will listen for events.

Once a threshold has been reached, a `lower-threshold` or `upper-threshold` event will
be fired, at which point the user may perform actions such as lazily-loading more data
to be displayed. After any work is done, the user must then clear the threshold by
calling the `clearTriggers` method on this element, after which it will
begin listening again for the scroll position to reach the threshold again assuming
the content in the scrollable region has grown. If the user no longer wishes to receive
events (e.g. all data has been exhausted), the threshold property in question (e.g.
`lowerThreshold`) may be set to a falsy value to disable events and clear the associated
triggered property.

### Example

```html
<iron-scroll-threshold on-lower-threshold="loadMoreData">
  <div>content</div>
</iron-scroll-threshold>
```

```js
  loadMoreData: function() {
    // load async stuff. e.g. XHR
    asyncStuff(function done() {
      ironScrollTheshold.clearTriggers();
    });
  }
```

### Using dom-repeat

```html
<iron-scroll-threshold on-lower-threshold="loadMoreData">
  <dom-repeat items="[[items]]">
    <template>
      <div>[[index]]</div>
    </template>
  </dom-repeat>
</iron-scroll-threshold>
```

### Using iron-list

```html
<iron-scroll-threshold on-lower-threshold="loadMoreData" id="threshold">
  <iron-list scroll-target="threshold" items="[[items]]">
    <template>
      <div>[[index]]</div>
    </template>
  </iron-list>
</iron-scroll-threshold>
```


