
<!---

This README is automatically generated from the comments in these files:
paper-progress.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/paper-progress.svg?branch=master)](https://travis-ci.org/PolymerElements/paper-progress)

_[Demo and API docs](https://elements.polymer-project.org/elements/paper-progress)_


##&lt;paper-progress&gt;

Material design: [Progress & activity](https://www.google.com/design/spec/components/progress-activity.html)

The progress bars are for situations where the percentage completed can be
determined. They give users a quick sense of how much longer an operation
will take.

Example:

```html
<paper-progress value="10"></paper-progress>
```

There is also a secondary progress which is useful for displaying intermediate
progress, such as the buffer level during a streaming playback progress bar.

Example:

```html
<paper-progress value="10" secondary-progress="30"></paper-progress>
```

### Styling progress bar:

To change the active progress bar color:

```css
paper-progress {
   --paper-progress-active-color: #e91e63;
}
```

To change the secondary progress bar color:

```css
paper-progress {
  --paper-progress-secondary-color: #f8bbd0;
}
```

To change the progress bar background color:

```css
paper-progress {
  --paper-progress-container-color: #64ffda;
}
```

Add the class `transiting` to a paper-progress to animate the progress bar when
the value changed. You can also customize the transition:

```css
paper-progress {
  --paper-progress-transition-duration: 0.08s;
  --paper-progress-transition-timing-function: ease;
  --paper-progress-transition-transition-delay: 0s;
}
```

The following mixins are available for styling:

| Custom property | Description | Default |
| --- | --- | --- |
| `--paper-progress-container-color` | Mixin applied to container | `--google-grey-300` |
| `--paper-progress-transition-duration` | Duration of the transition | `0.008s` |
| `--paper-progress-transition-timing-function` | The timing function for the transition | `ease` |
| `--paper-progress-transition-delay` | delay for the transition | `0s` |
| `--paper-progress-active-color` | The color of the active bar | `--google-green-500` |
| `--paper-progress-secondary-color` | The color of the secondary bar | `--google-green-100` |
| `--paper-progress-disabled-active-color` | The color of the active bar if disabled | `--google-grey-500` |
| `--paper-progress-disabled-secondary-color` | The color of the secondary bar if disabled | `--google-grey-300` |
| `--paper-progress-height` | The height of the progress bar | `4px` |


