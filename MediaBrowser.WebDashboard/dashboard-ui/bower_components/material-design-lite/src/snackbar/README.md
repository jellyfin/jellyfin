## Introduction

The Material Design Lite (MDL) **snackbar** component is a container used to notify a user of an operation's status.
It displays at the bottom of the screen.
A snackbar may contain an action button to execute a command for the user.
Actions should undo the committed action or retry it if it failed for example.
Actions should not be to close the snackbar.
By not providing an action, the snackbar becomes a **toast** component.

## Basic Usage:

Start a snackbar with a container div element.
On that container define the `mdl-js-snackbar` and `mdl-snackbar` classes.
It is also beneficial to add the aria live and atomic values to this container.

Within the container create a container element for the message.
This element should have the class `mdl-snackbar__text`.
Leave this element empty!
Text is added when the snackbar is called to be shown.

Second in the container, add a button element.
This element should have the class `mdl-snackbar__action`.
It is recommended to set the type to button to make sure no forms get submitted by accident.
Leave the text content empty here as well!
Do not directly apply any event handlers.

You now have complete markup for the snackbar to function.
All that is left is within your JavaScript to call the `showSnackbar` method on the snackbar container.
This takes a [plain object](#data-object) to configure the snackbar content appropriately.
You may call it multiple consecutive times and messages will stack.

## Examples

All snackbars should be shown through the same element.

#### Markup:

```html
<div aria-live="assertive" aria-atomic="true" aria-relevant="text" class="mdl-snackbar mdl-js-snackbar">
    <div class="mdl-snackbar__text"></div>
    <button type="button" class="mdl-snackbar__action"></button>
</div>
```

> Note: In this example there are a few aria attributes for accessibility. Please modify these as-needed for your site.

### Snackbar

```javascript
var notification = document.querySelector('.mdl-js-snackbar');
var data = {
  message: 'Message Sent',
  actionHandler: function(event) {},
  actionText: 'Undo',
  timeout: 10000
};
notification.MaterialSnackbar.showSnackbar(data);
```

### Toast

```javascript
var notification = document.querySelector('.mdl-js-snackbar');
notification.MaterialSnackbar.showSnackbar(
  {
    message: 'Image Uploaded'
  }
);
```

## CSS Classes

### Blocks

| MDL Class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-snackbar` | Defines the container of the snackbar component. | Required on snackbar container |

### Elements

| MDL Class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-snackbar__text` | Defines the element containing the text of the snackbar. | Required |
| `mdl-snackbar__action` | Defines the element that triggers the action of a snackbar. | Required |

### Modifiers

| MDL Class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-snackbar--active` | Marks the snackbar as active which causes it to display. | Required when active. Controlled in JavaScript |

## Data Object

The Snackbar components `showSnackbar` method takes an object for snackbar data.
The table below shows the properties and their usage.

| Property | Effect | Remarks | Type |
|-----------|--------|---------|---------|
| message   | The text message to display. | Required | String |
| timeout   | The amount of time in milliseconds to show the snackbar. | Optional (default 2750) | Integer |
| actionHandler | The function to execute when the action is clicked. | Optional | Function |
| actionText | The text to display for the action button. | Required if actionHandler is set |  String. |
