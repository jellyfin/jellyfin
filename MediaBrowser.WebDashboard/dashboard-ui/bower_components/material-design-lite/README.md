# Material Design Lite

[![GitHub version](https://badge.fury.io/gh/google%2Fmaterial-design-lite.svg)](https://badge.fury.io/gh/google%2Fmaterial-design-lite)
[![npm version](https://badge.fury.io/js/material-design-lite.svg)](https://badge.fury.io/js/material-design-lite)
[![Bower version](https://badge.fury.io/bo/material-design-lite.svg)](https://badge.fury.io/bo/material-design-lite)
[![Gitter version](https://img.shields.io/gitter/room/gitterHQ/gitter.svg)](https://gitter.im/google/material-design-lite)
[![Dependency Status](https://david-dm.org/google/material-design-lite.svg)](https://david-dm.org/google/material-design-lite)

> An implementation of [Material Design](http://www.google.com/design/spec/material-design/introduction.html)
components in vanilla CSS, JS, and HTML

Material Design Lite (MDL) lets you add a Material Design look and feel to your
static content websites. It doesn't rely on any JavaScript frameworks or
libraries. Optimized for cross-device use, gracefully degrades in older
browsers, and offers an experience that is accessible from the get-go.

## Use MDL on your site?

**This document is targeted at developers that will contribute to or compile
MDL. If you are looking to use MDL on your website or web app please head to
[getmdl.io](http://getmdl.io).**

## Browser Support

| IE9 | IE10 | IE11 | Chrome | Opera | Firefox | Safari | Chrome (Android) | Mobile Safari |
|-----|------|------|--------|-------|---------|--------|------------------|---------------|
| B   | A    | A    | A      | A     | A       | A      | A                | A             |

A-grade browsers are fully supported. B-grade browsers will gracefully degrade
to our CSS-only experience.

## Getting Started

### Download / Clone

Clone the repo using Git:

```bash
git clone https://github.com/google/material-design-lite.git
```

Alternatively you can [download](https://github.com/google/material-design-lite/archive/master.zip)
this repository.

Windows users, if you have trouble compiling due to line endings then make sure
you configure git to checkout the repository with `lf` (unix) line endings. This
can be achieved by setting `core.eol`.

```
git config core.eol lf
git config core.autocrlf input
git rm --cached -r .
git reset --hard
```

> Remember, the master branch is considered unstable. Do not use this in
production. Use a tagged state of the repository, npm, or bower for stability!

### What's included

In the repo you'll find the following directories and files.

| File/Folder     | Provides                                       |
|-----------------|------------------------------------------------|
| CONTRIBUTING.md | MDL contribution guidelines.                   |
| docs            | Files for the documentation site.              |
| gulpfile.js     | gulp configuration for MDL.                    |
| LICENSE         | Project license information.                   |
| package.json    | npm package information.                       |
| README.md       | Details for quickly understanding the project. |
| src             | Source code for MDL components.                |
| templates       | Example templates.                             |
| test            | Project test files.                            |

### Build

To get started modifying the components or the docs, first install the necessary
dependencies, from the root of the project:

```bash
npm install && npm install -g gulp
```

> MDL requires NodeJS 0.12.

Next, run the following one-liner to compile the components and the docs and
spawn a local instance of the documentation site:

```bash
gulp all && gulp serve
```

Most changes made to files inside the `src` or the `docs` directory will cause
the page to reload. This page can also be loaded up on physical devices thanks
to BrowserSync.

To build a production version of the components, run:

```bash
gulp
```

This will clean the `dist` folder and rebuild the assets for serving.

### Templates

The `templates/` subdirectory contains a few exemplary usages of MDL. Templates
have their own, quasi-separate gulp pipeline and can be compiled with
`gulp templates`. The templates use the vanilla MDL JS and
[themed](http://www.getmdl.io/customize/index.html) CSS files. Extraneous styles
are kept in a separate CSS file. Use `gulp serve` to take a look at the
templates:

* [Blog Template](http://www.getmdl.io/templates/blog)
* [Dashboard Template](http://www.getmdl.io/templates/dashboard)
* [Text Heavy Webpage Template](http://www.getmdl.io/templates/text-only)
* [Stand Alone Article Template](http://www.getmdl.io/templates/article)
* [Android.com MDL Skin Template](http://www.getmdl.io/templates/android-dot-com)
* [Portfolio Template](http://www.getmdl.io/templates/portfolio)

> Templates are not officially supported in IE9 and legacy browsers that do not
pass the minimum-requirements defined in our
[cutting-the-mustard test](https://github.com/google/material-design-lite/blob/87c48c22416c3e83850f7711365b2a43ba19c5ce/src/mdlComponentHandler.js#L336-L349).

The templates refer to CDN hosted versions of the libraries. If you'd like to
test the templates against locally built MDL libraries you need to run the
`templates:localtestingoverride` gulp task before running `gulp serve`:

```bash
gulp all && gulp templates:localtestingoverride && gulp serve
```

> Beware as any changes to the `templates` directory will automatically revert
the templates local testing overrides. In this case make sure you run the
`templates:localtestingoverride` gulp task again or modify the `watch()`
function in the gulp file.

## Versioning

For transparency into our release cycle and in striving to maintain backward
compatibility, Material Design Lite is maintained under
[the Semantic Versioning guidelines](http://semver.org/). Sometimes we screw up,
but we'll adhere to those rules whenever possible.

## Feature requests

If you find MDL doesn't contain a particular component you think would be
useful, please check the issue tracker in case work has already started on it.
If not, you can request a [new component](https://github.com/Google/material-design-lite/issues/new?title=[Component%20Request]%20{Component}&body=Please%20include:%0A*%20Description%0A*%20Material%20Design%20Spec%20link%0A*%20Use%20Case%28s%29).
Please keep in mind that one of the goals of MDL is to adhere to the Material
Design specs and therefore some requests might not be within the scope of this
project.

## Want to contribute?

If you found a bug, have any questions or want to contribute. Follow our
[guidelines](https://github.com/google/material-design-lite/blob/master/CONTRIBUTING.md),
and help improve the Material Design Lite. For more information visit our
[wiki](https://github.com/google/material-design-lite/wiki).

## Do you include any features that a framework comes with?

Material Design Lite is focused on delivering a vanilla CSS/JS/HTML library of
components. We are not a framework. If you are building a single-page app and
require features like two-way data-binding, templating, CSS scoping and so
forth, we recommend trying out the excellent
[Polymer](http://polymer-project.org) project.

## License

© Google, 2015. Licensed under an
[Apache-2](https://github.com/google/material-design-lite/blob/master/LICENSE)
license.
