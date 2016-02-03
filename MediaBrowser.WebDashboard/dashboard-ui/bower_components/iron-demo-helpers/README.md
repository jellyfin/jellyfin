
<!---

This README is automatically generated from the comments in these files:
demo-snippet.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

-->

_[Demo and API Docs](https://elements.polymer-project.org/elements/iron-demo-helpers)_


##&lt;demo-snippet&gt;


`demo-snippet` is a helper element that displays the source of a code snippet and
its rendered demo. It can be used for both native elements and
Polymer elements.

    Example of a native element demo

        <demo-snippet>
          <template>
            <input type="date">
          </template>
        </demo-snippet>

    Example of a Polymer <paper-checkbox> demo

        <demo-snippet>
          <template>
            <paper-checkbox>Checkbox</paper-checkbox>
            <paper-checkbox checked>Checkbox</paper-checkbox>
          </template>
        </demo-snippet>

### Styling

The following custom properties and mixins are available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--demo-snippet` | Mixin applied to the entire element | `{}`
`--demo-snippet-demo` | Mixin applied to just the demo section | `{}`
`--demo-snippet-code` | Mixin applied to just the code section | `{}`


