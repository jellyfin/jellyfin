Swipebox
================================

A touchable jQuery lightbox.

[View project page](http://brutaldesign.github.com/swipebox)

##What is Swipebox ?

Swipebox is a jQuery "lightbox" plugin for desktop, mobile and tablet.

##Features

- Swipe gestures for mobile
- Keyboard Navigation for desktop
- CSS transitions with jQuery fallback
- Retina support for UI icons
- Easy CSS customization
- Video, Images and Inline content

###Compatibility

Chrome, Safari, Firefox, Opera, IE9+, IOS4+, Android, windows phone.

##Usage

###Javascript

Include jquery and the swipebox script in your head tags or right before your body closing tag.

```html
<script src="lib/jquery-1.9.0.js"></script>
<script src="src/js/jquery.swipebox.js"></script>
```

###CSS

Include the swipebox CSS style in your head tags.

```html
<link rel="stylesheet" href="src/css/swipebox.css">
```

###HTML

Use a specific class for your links and use the title attribute as caption.

```html
<a href="big/image.jpg" class="swipebox" title="My Caption">
```

###Fire the plugin

Bind the swipebox behaviour on every link with the "swipebox" class.

```javascript
$( '.swipebox' ).swipebox();
```

###Options

```javascript
useCSS : true, // false will force the use of jQuery for animations
initialIndexOnArray: 0, // which image index to init when a array is passed
hideCloseButtonOnMobile : false, // true will hide the close button on mobile devices
hideBarsDelay : 3000, // delay before hiding bars on desktop
videoMaxWidth : 1140, // videos max width
beforeOpen: function(){} , // called before opening
afterOpen: null, // called after opening
afterClose: function(){}, // called after closing
loopAtEnd: false, // true will return to the first image after the last image is reached
autoplayVideos: false // true will autoplay Youtube and Vimeo videos
queryStringData: {} // plain object with custom query string arguments to pass/override for video URLs,
toggleClassOnLoad: '' // CSS class that can be toggled when the slide will be loaded (like 'hidden' of Bootstrap)
```

###Pull Requests

I want to keep this plugin as simple as possible. I won't merge pull requests for additional features such as download buttons, social like buttons, IE8 compatibility etc... But feel free to fork the project and customize it to suit to your needs. Most wanted PR are for bug fixes. Also, a future improvement will be to allow zoom on touchable devices.

If you want to submit a pull request please be sure to grunt the whole thing (mostly jshintrc validation and minified file) and send me a demo URL. Also, please comment your code.

Thanks for your understanding and thank you all for your helpful support!
