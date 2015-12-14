
<!---

This README is automatically generated from the comments in these files:
iron-iconset-svg.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

-->

[![Build Status](https://travis-ci.org/PolymerElements/iron-iconset-svg.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-iconset-svg)

_[Demo and API Docs](https://elements.polymer-project.org/elements/iron-iconset-svg)_


##&lt;iron-iconset-svg&gt;


The `iron-iconset-svg` element allows users to define their own icon sets
that contain svg icons. The svg icon elements should be children of the
`iron-iconset-svg` element. Multiple icons should be given distinct id's.

Using svg elements to create icons has a few advantages over traditional
bitmap graphics like jpg or png. Icons that use svg are vector based so
they are resolution independent and should look good on any device. They
are stylable via css. Icons can be themed, colorized, and even animated.

Example:

    <iron-iconset-svg name="my-svg-icons" size="24">
      <svg>
        <defs>
          <g id="shape">
            <rect x="50" y="50" width="50" height="50" />
            <circle cx="50" cy="50" r="50" />
          </g>
        </defs>
      </svg>
    </iron-iconset-svg>

This will automatically register the icon set "my-svg-icons" to the iconset
database.  To use these icons from within another element, make a
`iron-iconset` element and call the `byId` method
to retrieve a given iconset. To apply a particular icon inside an
element use the `applyIcon` method. For example:

    iconset.applyIcon(iconNode, 'car');


