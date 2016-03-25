## Introduction
Lists present multiple line items vertically as a single continuous element. Refer the [Material Design
Spec](https://www.google.com/design/spec/components/lists.html) to know more about the content options.

### To include the MDL **list** component:

## Create a List with basic items.

&nbsp;1. Code a `<ul>` element with the class `mdl-list`; this is the "outer" container, intended to hold all of the list's content.
```html
<ul class='mdl-list'>
</ul>
```
&nbsp;2. Code as many `<li>` elements as required with the class `mdl-list__item`; this is intended to hold all of the **item's** content.
```html
<ul class='mdl-list'>
  <li class="mdl-list__item"></li>
  <li class="mdl-list__item"></li>
  <li class="mdl-list__item"></li>
</ul>
```

&nbsp;3. Add your content as the children of the `<li>`, with the appropriate content type modification class for example .
```html
<ul class='mdl-list'>
  <li class="mdl-list__item">
    <span class="mdl-list__item-primary-content"></span>
  </li>
  <li class="mdl-list__item">
    <span class="mdl-list__item-primary-content"></span>
  </li>
  <li class="mdl-list__item">
    <span class="mdl-list__item-primary-content"></span>
  </li>
</ul>
```

## Configuration options

The MDL CSS classes apply various predefined visual enhancements to the list. The table below lists the available classes and their effects.

| MDL Class        | Effect           | Remark  |
| ------------- |:-------------:| -----:|
| .mdl-list | Defines list as an MDL component| - |
| .mdl-list__item | Defines the List's Items | required |
| .mdl-list__item--two-line | Defines the List's Items as Two Line | Optional Two Line List Variant |
| .mdl-list__item--three-line | Defines the List's Items  as a Three Line | Optional Three Line List Variant |
| .mdl-list__item-primary-content | Defines the primary content sub-division |-|
| .mdl-list__item-avatar | Defines the avatar sub-division |-|
| .mdl-list__item-icon | Defines the icon sub-division |-|
| .mdl-list__item-secondary-content | Defines the secondary content sub-division | requires `.mdl-list__item-two-line` or `.mdl-list__item-three-line` |
| .mdl-list__item-secondary-info | Defines the information sub-division |requires `.mdl-list__item-two-line` or `.mdl-list__item-three-line` |
| .mdl-list__item-secondary-action | Defines the Action sub-division | requires `.mdl-list__item-two-line` or `.mdl-list__item-three-line` |
| .mdl-list__item-text-body | Defines the Text Body sub-division | requires `.mdl-list__item-three-line` |
