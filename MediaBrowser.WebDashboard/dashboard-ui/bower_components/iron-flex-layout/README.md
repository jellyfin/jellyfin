
<!---

This README is automatically generated from the comments in these files:
iron-flex-layout.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/iron-flex-layout.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-flex-layout)

_[Demo and API docs](https://elements.polymer-project.org/elements/iron-flex-layout)_


##&lt;iron-flex-layout&gt;

The `<iron-flex-layout>` component provides simple ways to use
[CSS flexible box layout](https://developer.mozilla.org/en-US/docs/Web/Guide/CSS/Flexible_boxes),
also known as flexbox. This component provides two different ways to use flexbox:

1. [Layout classes](https://github.com/PolymerElements/iron-flex-layout/tree/master/iron-flex-layout-classes.html).
The layout class stylesheet provides a simple set of class-based flexbox rules, that
let you specify layout properties directly in markup. You must include this file
in every element that needs to use them.

Sample use:

   <link rel="import" href="../iron-flex-layout/iron-flex-layout-classes.html">

   <style is="custom-style" include="iron-flex iron-flex-alignment"></style>

   <div class="layout horizontal layout-start">
     <div>cross axis start alignment</div>
   </div>

1. [Custom CSS mixins](https://github.com/PolymerElements/iron-flex-layout/blob/master/iron-flex-layout.html).
The mixin stylesheet includes custom CSS mixins that can be applied inside a CSS rule using the `@apply` function.



Please note that the old [/deep/ layout classes](https://github.com/PolymerElements/iron-flex-layout/tree/master/classes)
are deprecated, and should not be used. To continue using layout properties
directly in markup, please switch to using the new `dom-module`-based
[layout classes](https://github.com/PolymerElements/iron-flex-layout/tree/master/iron-flex-layout-classes.html).
Please note that the new version does not use `/deep/`, and therefore requires you
to import the `dom-modules` in every element that needs to use them.

A complete [guide](https://elements.polymer-project.org/guides/flex-layout) to `<iron-flex-layout>` is available.


