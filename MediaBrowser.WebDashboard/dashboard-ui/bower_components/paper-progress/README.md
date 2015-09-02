paper-progress
===================

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

Add the class `transiting` to a `<paper-progress>` to animate the progress bar when
the value changed. You can also customize the transition:

```css
paper-progress {
  --paper-progress-transition-duration: 0.08s;
  --paper-progress-transition-timing-function: ease;
  --paper-progress-transition-transition-delay: 0s;
}
```

The following mixins are available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--paper-progress-container` | Mixin applied to container | `{}`
