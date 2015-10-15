# 3.5.1 Which Hunt

- Fixed a bug when determining the mouse button being pressed
- Fixed a bug when determining the element behind the mouse cursor when `ignoreInputTextSelection` was enabled

# 3.5.0 Input Fanatic

- Added a feature where users are able to select text ranges with their mouse in inputs within a dragula container

# 3.4.1 Input Accomodation

- Fixed a bug where text in inputs inside containers assigned to `dragula` couldn't be selected

# 3.4.0 Event Sourcing

- Events for `cancel`, `remove`, and `shadow` now all provide a `source` parameter in the third position

# 3.3.2 Captain Obvious

- Fixed a bug where `out` would be emitted with an `undefined` container

# 3.3.1 Significant Other

- Fixed a fringe bug [(#207)](https://github.com/bevacqua/dragula/pull/207) where the click handler wouldn't work
- Fixed a bug where `drop` events would sometimes not receive the current sibling

# 3.3.0 Brotherhood

- The `options.moves` callback now receives a fourth parameter, the `sibling` found after `el`
- The `drop` event now receives a fourth parameter, the `sibling` found after `el`

# 3.2.0 Sortable Sauce

- You can now use `options.copySortSource` to enable sorting in `copy`-source containers

# 3.1.0 Copy Paste

- You can now set `options.copy` to a method. It'll be invoked once per drag to ask whether the element being dragged should be treated as a copy or not
- Fixed a bug where starting a drag programatically while an element was being dragged resulted in an exception

# 3.0.7 Crossroads

- Fixed a bug in Webpack builds by updating `crossvent` to `1.5.3`

# 3.0.5 Mouse Rat Rock Band

- Fixed a bug where `mousedown` would be prevented and focusing draggable inputs wouldn't be possible

# 3.0.4 IE is the old IE

- Fixed a bug in IE8 by updating `crossvent` to `1.5.2`

# 3.0.3 Forest Fire

- Fixed a bug in Firefox where dragging links and images would result in issues

# 3.0.2 Clickhood Rainforest

- Fixed a _historical_ bug, where click on anchors would be ignored within `dragula` containers in mobile
- Fixed a bug where events wouldn't be gracefully removed if `drake` were destroyed during a drag event
- Now emits `dragend` after `out` to preserve consistency _(because `drag` is emitted before `over`)_
- Fixed another old bug where attempting to remove elements using `removeOnSpill` on mobile would fail

# 3.0.1 Carjacking

- Fixed a bug in mobile, caused by `3.0.0`, where scrolling would be impossible
- Fixed a bug where dragging would cause text selection in IE8

# 3.0.0 Guilty Conscience

- Removed `addContainer` method, which was previously deprecated
- Removed `removeContainer` method, which was previously deprecated
- Removed `delay` option in favor of using `mousemove`
- Drag events now start on the first occurrence of a `mousemove` event
- If `mousemove` never fires, then the `drag` machinery won't start, either
- Changed default value for `invalid`, now always returns `false` by default
- Added `mirrorContainer` option to determine where the mirror gets appended to _(defaults to `document.body`)_

# 2.1.2 Shady Sibling

- Fixed a bug where `shadow` would trigger multiple times while dragging an element over the same spot

# 2.1.1 Classy Drake

- Fixed a bug where adding and removing classes might've caused issues on elements that had foreign CSS classes
- Added an argument to `cloned` event that specifies the kind of clone. Possible values include `mirror` and `copy` at the moment

# 2.1.0 Over and Out

- Added `over` event that fires whenever an element is dragged over a container _(or whenever a drag event starts)_
- Added `out` event that fires whenever an element is dragged out of a container _(or whenever a drag event ends)_

# 2.0.7 Mayhem

- Fixed a bug caused in `2.0.6` where anything would be regarded as a `drake` container

# 2.0.6 Coruscant

- Fixed a bug where `isContainer` would be called with a `el=null` in some situations

# 2.0.5 Cross Ventilation

- Bumped `crossvent@1.5.0`

# 2.0.4 Transit Overload

- Set `gu-transit` after a drag event has fully started

# 2.0.3 Mice Trap

- Fixed a bug where using `.cancel` would throw an exception

# 2.0.2 Aural Emission

- Replaced `contra.emitter` with `contra@1.9.1/emitter`

# 2.0.1 Copycat

- Fixed a bug where dragging a copy back to origin after hovering over another container would still result in a copy being made if you never spilled the item

# 2.0.0 Containerization

- Deprecated `addContainer` method
- Deprecated `removeContainer` method
- Exposed `dragula.containers` collection
- Introduced dynamic `isContainer` method
- Can now omit `containers` argument to `dragula(containers, options)`
- Can now pass `containers` as an option

# 1.7.0 Clickety Click

- Differentiate between drag and click using `delay` option
- Ability to specify which event targets are `invalid` drag triggers

# 1.6.1 Shadow Drake

- Improved shadow positioning when `revertOnSpill` is `true`

# 1.6.0 Lonely Clown Clone

- Added `'cloned'` event when a DOM element is cloned

# 1.5.1 Touchypants

- Fixed an issue where dragula didn't understand where an element was being dropped

# 1.5.0 Drag Racing

- Introduced drag handles so that elements could only be dragged from a handle element

# 1.4.2 Container Camp

- Fixed a bug where `addContainer` and `removeContainer` wouldn't update the list of available containers
- Fixed a bug where `document.body` would be accessed before it was available if the scripts were loaded in the `<head>`

# 1.4.1 Blood Prince

- Fixed an issue where manually started drag events wouldn't know if position changed when an item was dropped in the source container
- Added minor styling to `gu-mirror`, to visually identify that a drag is in progress

# 1.4.0 Top Fuel

- Added a `dragend` event that's always fired
- Added a `dragging` property to API
- Introduced manual `start` API method
- Introduced `addContainer` and `removeContainer` dynamic API

# 1.3.0 Terror

Introduced an `.end` instance API method that gracefully ends the drag event using the last known valid drop target.

# 1.2.4 Brother in Arms

- The `accepts` option now takes a fourth argument, `sibling`, giving us a hint of the precise position the item would be dropped in

# 1.2.3 Breeding Pool

- Fixed a bug in cross browser behavior that caused the hover effect to ignore scrolling
- Fixed a bug where touch events weren't working in obscure versions of IE

# 1.2.2 Originality Accepted

- Improved `accepts` mechanism so that it always accepts the original starting point

# 1.2.1 Firehose

- Fixed a bug introduced in `1.2.0`
- Fixed a bug where cancelling with `revert` enabled wouldn't respect sort order

# 1.2.0 Firefly

- Introduced `moves` option, used to determine if an item is draggable
- Added a `source` parameter for the `drop` event
- Cancelling a drag event when `revertOnSpill` is `true` will now move the element to its original position in the source element instead of appending it
- Fixed a bug where _"cancellations"_ that ended up leaving the dragged element in the source container but changed sort order would trigger a `cancel` event instead of `drop`
- Fixed a bug where _"drops"_ that ended up leaving the element in the exact same place it was dragged from would end up triggering a `drop` event instead of `cancel`
- Added touch event support

# 1.1.4 Fog Creek

- Added `'shadow'` event to enable easy updates to shadow element as it's moved

# 1.1.3 Drag Queen

- Fixed a bug where `dragula` wouldn't make a copy if the element was dropped outside of a target container
- If a dragged element gets removed for an instance that has `copy` set to `true`, a `cancel` event is raised instead

# 1.1.2 Eavesdropping

- Fixed a bug where _"cancellations"_ that ended up leaving the dragged element somewhere other than the source container wouldn't trigger a `drop` event

# 1.1.1 Slipping Jimmy

- Fixed a bug where the movable shadow wouldn't update properly if the element was hovered over the last position of a container

# 1.1.0 Age of Shadows

- Added a movable shadow that gives visual feedback as to where a dragged item would be dropped
- Added an option to remove dragged elements when they are dropped outside of sanctioned containers
- Added an option to revert dragged elements back to their source container when they are dropped outside of sanctioned containers

# 1.0.1 Consuelo

- Removed `console.log` statement

# 1.0.0 IPO

- Initial Public Release
