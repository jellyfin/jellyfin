
<!---

This README is automatically generated from the comments in these files:
iron-location.html  iron-query-params.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/iron-location.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-location)

_[Demo and API docs](https://elements.polymer-project.org/elements/iron-location)_


##&lt;iron-location&gt;

The `iron-location` element manages binding to and from the current URL.

iron-location is the first, and lowest level element in the Polymer team's
routing system. This is a beta release of iron-location as we continue work
on higher level elements, and as such iron-location may undergo breaking
changes.

#### Properties

When the URL is: `/search?query=583#details` iron-location's properties will be:

* path: `'/search'`
* query: `'query=583'`
* hash: `'details'`

These bindings are bidirectional. Modifying them will in turn modify the URL.

iron-location is only active while it is attached to the document.

#### Links

While iron-location is active in the document it will intercept clicks on links
within your site, updating the URL pushing the updated URL out through the
databinding system. iron-location only intercepts clicks with the intent to
open in the same window, so middle mouse clicks and ctrl/cmd clicks work fine.

You can customize this behavior with the `urlSpaceRegex`.

#### Dwell Time

iron-location protects against accidental history spamming by only adding
entries to the user's history if the URL stays unchanged for `dwellTime`
milliseconds.



<!-- No docs for <iron-query-params> found. -->
