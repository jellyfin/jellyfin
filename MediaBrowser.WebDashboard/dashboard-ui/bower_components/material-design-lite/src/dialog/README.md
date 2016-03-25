## Introduction

The Material Design Lite (MDL) **dialog** component allows for verification of
user actions, simple data input, and alerts to provide extra information to users.

## Basic Usage

To use the dialog component, you must be using a browser that supports the [dialog element](http://www.w3.org/TR/2013/CR-html5-20130806/interactive-elements.html#the-dialog-element).
Only Chrome and Opera have native support at the time of writing.
For other browsers you will need to include the [dialog polyfill](https://github.com/GoogleChrome/dialog-polyfill) or create your own.

Once you have dialog support create a dialog element.
The element when using the polyfill **must** be a child of the `body` element.
Within that container, add a content element with the class `mdl-dialog__content`.
Add you content, then create an action container with the class `mdl-dialog__actions`.
Finally for the markup, add your buttons within this container for triggering dialog functions.

Keep in mind, the order is automatically reversed for actions.
Material Design requires that the primary (confirmation) action be displayed last.
So, the first action you create will appear last on the action bar.
This allows for more natural coding and tab ordering while following the specification.

Remember to add the event handlers for your action items.
After your dialog markup is created, add the event listeners to the page to trigger the dialog to show.

For example:

```javascript
  var button = document.querySelector('button');
  var dialog = document.querySelector('dialog');
  button.addEventListener('click', function() {
    dialog.showModal();
    /* Or dialog.show(); to show the dialog without a backdrop. */
  });
```

## Examples

### Simple Dialog

See this example live in [codepen](http://codepen.io/Garbee/full/EPoaMj/).

```html
<body>
  <button id="show-dialog" type="button" class="mdl-button">Show Dialog</button>
  <dialog class="mdl-dialog">
    <h4 class="mdl-dialog__title">Allow data collection?</h4>
    <div class="mdl-dialog__content">
      <p>
        Allowing us to collect data will let us get you the information you want faster.
      </p>
    </div>
    <div class="mdl-dialog__actions">
      <button type="button" class="mdl-button">Agree</button>
      <button type="button" class="mdl-button close">Disagree</button>
    </div>
  </dialog>
  <script>
    var dialog = document.querySelector('dialog');
    var showDialogButton = document.querySelector('#show-dialog');
    if (! dialog.showModal) {
      dialogPolyfill.registerDialog(dialog);
    }
    showDialogButton.addEventListener('click', function() {
      dialog.showModal();
    });
    dialog.querySelector('.close').addEventListener('click', function() {
      dialog.close();
    });
  </script>
</body>
```

### Dialog with full width actions

See this example live in [codepen](http://codepen.io/Garbee/full/JGMowG/).

```html
<body>
  <button type="button" class="mdl-button show-modal">Show Modal</button>
  <dialog class="mdl-dialog">
    <div class="mdl-dialog__content">
      <p>
        Allow this site to collect usage data to improve your experience?
      </p>
    </div>
    <div class="mdl-dialog__actions mdl-dialog__actions--full-width">
      <button type="button" class="mdl-button">Agree</button>
      <button type="button" class="mdl-button close">Disagree</button>
    </div>
  </dialog>
  <script>
    var dialog = document.querySelector('dialog');
    var showModalButton = document.querySelector('.show-modal');
    if (! dialog.showModal) {
      dialogPolyfill.registerDialog(dialog);
    }
    showModalButton.addEventListener('click', function() {
      dialog.showModal();
    });
    dialog.querySelector('.close').addEventListener('click', function() {
      dialog.close();
    });
  </script>
</body>
```

## CSS Classes

### Blocks

| MDL Class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-dialog` | Defines the container of the dialog component. | Required on dialog container. |

### Elements

| MDL Class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-dialog__title` | Defines the title container in the dialog. | Optional on title container. |
| `mdl-dialog__content` | Defines the content container of the dialog. | Required on content container. |
| `mdl-dialog__actions` | Defines the actions container in the dialog. | Required on action container. |

### Modifiers

| MDL Class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-dialog__actions--full-width` | Modifies the actions to each take the full width of the container. This makes each take their own line. | Optional on action container. |
