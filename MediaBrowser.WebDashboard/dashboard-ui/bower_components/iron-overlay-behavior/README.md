# iron-overlay-behavior
Makes an element an overlay with an optional backdrop.

`iron-overlay-behavior` displays an element on top of other content. It starts out hidden and is
displayed by calling `open()` or setting the `opened` property to `true`. It may be closed by
calling `close()` or `cancel()`, or by setting the `opened` property to `false`.

The difference between `close()` and `cancel()` is user intent. `close()` generally implies that
the user acknowledged the content of the overlay. By default, it will cancel whenever the user taps
outside it or presses the escape key. This behavior can be turned off via the `no-cancel-on-esc-key`
and the `no-cancel-on-outside-click` properties.
