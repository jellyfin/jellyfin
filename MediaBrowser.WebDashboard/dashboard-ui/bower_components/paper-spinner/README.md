
<!---

This README is automatically generated from the comments in these files:
paper-spinner.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

-->

[![Build Status](https://travis-ci.org/PolymerElements/paper-spinner.svg?branch=master)](https://travis-ci.org/PolymerElements/paper-spinner)

_[Demo and API Docs](https://elements.polymer-project.org/elements/paper-spinner)_


##&lt;paper-spinner&gt;


Material design: [Progress & activity](https://www.google.com/design/spec/components/progress-activity.html)

Element providing material design circular spinner.

    <paper-spinner active></paper-spinner>

The default spinner cycles between four layers of colors; by default they are
blue, red, yellow and green. It can be customized so that it uses one color only
by setting all the layer colors to the same value.

### Accessibility

Alt attribute should be set to provide adequate context for accessibility. If not provided,
it defaults to 'loading'.
Empty alt can be provided to mark the element as decorative if alternative content is provided
in another form (e.g. a text block following the spinner).

    <paper-spinner alt="Loading contacts list" active></paper-spinner>

### Styling

The following custom properties and mixins are available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--paper-spinner-layer-1-color` | Color of the first spinner rotation | `--google-blue-500`
`--paper-spinner-layer-2-color` | Color of the second spinner rotation | `--google-red-500`
`--paper-spinner-layer-3-color` | Color of the third spinner rotation | `--google-yellow-500`
`--paper-spinner-layer-4-color` | Color of the fourth spinner rotation | `--google-green-500`


