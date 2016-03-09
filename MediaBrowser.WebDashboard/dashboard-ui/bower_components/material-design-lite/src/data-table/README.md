## Introduction

The Material Design Lite (MDL) **data-table** component is an enhanced version of the standard HTML `<table>`. A data-table consists of rows and columns of well-formatted data, presented with appropriate user interaction capabilities.

Tables are a ubiquitous feature of most user interfaces, regardless of a site's content or function. Their design and use is therefore an important factor in the overall user experience. See the data-table component's [Material Design specifications page](http://www.google.com/design/spec/components/data-tables.html) for details.

The available row/column/cell types in a data-table are mostly self-formatting; that is, once the data-table is defined, the individual cells require very little specific attention. For example, the rows exhibit shading behavior on mouseover and selection, numeric values are automatically formatted by default, and the addition of a single class makes the table rows individually or collectively selectable. This makes the data-table component convenient and easy to code for the developer, as well as attractive and intuitive for the user.

### To include an MDL **data-table** component:

&nbsp;1. Code a `<table>` element. Include `<thead>` and `<tbody>` elements to hold the title and data rows, respectively.
```html
<table>
  <thead>
  </thead>
  <tbody>
  </tbody>
</table>
```

&nbsp;2. Add one or more MDL classes, separated by spaces, to the table using the `class` attribute.
```html
<table class="mdl-data-table mdl-js-data-table">
  <thead>
  </thead>
  <tbody>
  </tbody>
</table>
```

&nbsp;2. Inside the `<thead>`, code exactly one table row `<tr>` containing one table header cell `<th>` for each column, and include the desired text in the header cells. To ensure proper header alignment, add the "non-numeric" MDL class to the header cell of text-only columns. (Data cells are formatted as numeric by default.)
```html
<table class="mdl-data-table mdl-js-data-table">
  <thead>
    <tr>
      <th class="mdl-data-table__cell--non-numeric">Name</th>
      <th>Age</th>
      <th>ID Number</th>
    </tr>
  </thead>
  <tbody>
  </tbody>
</table>
```

&nbsp;3. Inside the `<tbody>`, code one table row `<tr>` for each data row and one table data cell `<td>` for each column in the row. As with the header cells, add the "non-numeric" MDL class to text-only data cells to ensure proper alignment.
```html
<table class="mdl-data-table mdl-js-data-table">
  <thead>
    <tr>
      <th class="mdl-data-table__cell--non-numeric">Name</th>
      <th>Age</th>
      <th>ID Number</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td class="mdl-data-table__cell--non-numeric">Don Aubrey</td>
      <td>25</td>
      <td>49021</td>
    </tr>
    <tr>
      <td class="mdl-data-table__cell--non-numeric">Sophia Carson</td>
      <td>32</td>
      <td>10258</td>
    </tr>
    <tr>
      <td class="mdl-data-table__cell--non-numeric">Steve Moreno</td>
      <td>29</td>
      <td>12359</td>
    </tr>
  </tbody>
</table>
```

The data-table component is ready for use.

#### Examples

A data-table with a "master" select checkbox and individual row select checkboxes.
```html
<table class="mdl-data-table mdl-js-data-table mdl-data-table--selectable">
  <thead>
    <tr>
      <th class="mdl-data-table__cell--non-numeric">Material</th>
      <th>Quantity</th>
      <th>Unit price</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td class="mdl-data-table__cell--non-numeric">Acrylic (Transparent)</td>
      <td>250</td>
      <td>$2.90</td>
    </tr>
    <tr>
      <td class="mdl-data-table__cell--non-numeric">Plywood (Birch)</td>
      <td>50</td>
      <td>$1.25</td>
    </tr>
    <tr>
      <td class="mdl-data-table__cell--non-numeric">Laminate (Gold on Blue)</td>
      <td>10</td>
      <td>$12.35</td>
    </tr>
  </tbody>
</table>
```

A data-table without select checkboxes containing mostly text data.
```html
<table class="mdl-data-table mdl-js-data-table">
  <thead>
    <tr>
      <th class="mdl-data-table__cell--non-numeric">Name</th>
      <th class="mdl-data-table__cell--non-numeric">Nickname</th>
      <th>Age</th>
      <th class="mdl-data-table__cell--non-numeric">Living?</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td class="mdl-data-table__cell--non-numeric">John Lennon</td>
      <td class="mdl-data-table__cell--non-numeric">The smart one</td>
      <td>40</td>
      <td class="mdl-data-table__cell--non-numeric">No</td>
    </tr>
    <tr>
      <td class="mdl-data-table__cell--non-numeric">Paul McCartney</td>
      <td class="mdl-data-table__cell--non-numeric">The cute one</td>
      <td>73</td>
      <td class="mdl-data-table__cell--non-numeric">Yes</td>
    </tr>
    <tr>
      <td class="mdl-data-table__cell--non-numeric">George Harrison</td>
      <td class="mdl-data-table__cell--non-numeric">The shy one</td>
      <td>58</td>
      <td class="mdl-data-table__cell--non-numeric">No</td>
    </tr>
    <tr>
      <td class="mdl-data-table__cell--non-numeric">Ringo Starr</td>
      <td class="mdl-data-table__cell--non-numeric">The funny one</td>
      <td>74</td>
      <td class="mdl-data-table__cell--non-numeric">Yes</td>
    </tr>
  </tbody>
</table>
```

A table that has name and values for the checkboxes.
```html
<table class="mdl-data-table mdl-js-data-table mdl-data-table--selectable">
  <thead>
    <tr>
      <th class="mdl-data-table__cell--non-numeric">Material</th>
      <th>Quantity</th>
      <th>Unit price</th>
    </tr>
  </thead>
  <tbody>
    <tr data-mdl-data-table-selectable-name="materials[]" data-mdl-data-table-selectable-value="acrylic">
      <td class="mdl-data-table__cell--non-numeric">Acrylic (Transparent)</td>
      <td>250</td>
      <td>$2.90</td>
    </tr>
    <tr data-mdl-data-table-selectable-name="materials[]" data-mdl-data-table-selectable-value="plywood">
      <td class="mdl-data-table__cell--non-numeric">Plywood (Birch)</td>
      <td>50</td>
      <td>$1.25</td>
    </tr>
    <tr data-mdl-data-table-selectable-name="materials[]" data-mdl-data-table-selectable-value="laminate">
      <td class="mdl-data-table__cell--non-numeric">Laminate (Gold on Blue)</td>
      <td>10</td>
      <td>$12.35</td>
    </tr>
  </tbody>
</table>
```

## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the data-table. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-data-table` | Defines table as an MDL component | Required on table element|
| `mdl-js-data-table` | Assigns basic MDL behavior to table | Required on table element|
| `mdl-data-table--selectable` | Applies all/individual selectable behavior (checkboxes) | Optional; goes on table element |
| `mdl-data-table__header--sorted-ascending` | Applies visual styling to indicate the column is sorted in ascending order | Optional; goes on table header (`th`) |
| `mdl-data-table__header--sorted-descending` | Applies visual styling to indicate the column is sorted in descending order | Optional; goes on table header (`th`) |
| `mdl-data-table__cell--non-numeric` | Applies text formatting to data cell | Optional; goes on both table header and table data cells |
| (none) | Applies numeric formatting to header or data cell (default) |  |
