paper-collapse-item [![Bower version](https://badge.fury.io/bo/paper-collapse-item.svg)](http://badge.fury.io/bo/paper-collapse-item)
=========

`paper-collapse-item` provides a Material Design [item with header and collapsible content](https://www.google.com/design/spec/components/lists.html). The web component is built with [Polymer 1.x](https://www.polymer-project.org).

![Screenshot](/doc/screenshot.png "Screenshot")


## Usage

`bower install paper-collapse-item`

```html
<paper-collapse-item icon="icons:favorite" header="Item 1" opened>
  Lots of very interesting content.
</paper-collapse-item>
<paper-collapse-item icon="icons:info" header="Item 2">
  Lots of very interesting content.
</paper-collapse-item>
<paper-collapse-item icon="icons:help" header="Item 3">
  Lots of very interesting content.
</paper-collapse-item>
```


## Properties

These properties are available for `paper-collapse-item`:

Property   | Type    | Description
---------- | ------- | ----------------------------
**icon**   | String  | Icon that is shown in the header row
**header** | String  | Text in the header row
**opened** | Boolean | Status flag if the content section is opened


## Continuous integration

[Travis-CI](https://travis-ci.org/Collaborne/paper-collapse-item) [![Travis state](https://travis-ci.org/Collaborne/paper-collapse-item.svg?branch=master)](https://travis-ci.org/Collaborne/paper-collapse-item)


## License

    This software is licensed under the Apache 2 license, quoted below.

    Copyright 2011-2015 Collaborne B.V. <http://github.com/Collaborne/>

    Licensed under the Apache License, Version 2.0 (the "License"); you may not
    use this file except in compliance with the License. You may obtain a copy of
    the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
    WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
    License for the specific language governing permissions and limitations under
    the License.
    