[![Build Status](https://travis-ci.org/kaimallea/isMobile.png)](https://travis-ci.org/kaimallea/isMobile)
[![Node dependencies status](https://david-dm.org/kaimallea/isMobile.png)](https://david-dm.org/kaimallea/isMobile)

# isMobile

A simple JS library that detects mobile devices.

## Why use isMobile?

You probably shouldn't use this library unless you absolutely have to. In most cases, good [responsive design](https://en.wikipedia.org/wiki/Responsive_web_design) solves the problem of controlling how to
render things across different screen sizes. But there are always edge cases. If you have an eddge case,
then this library might be for you.

I had very specific requirements for a project when I created this:

**`- Redirect all iPhones, iPods, Android phones, and seven inch devices to the mobile site.`**

Yep, at the time, a completely separate site had already been created for mobile devices. So I couldn't depend on media queries, feature detection, graceful degradation, progressive enhancement, or any of the cool techniques for selectively displaying things. I had to find a way to redirect visitors on certain devices to the mobile site.

I couldn't do detection on the back-end, because the entire site was generated as HTML, and then cached and served by a [CDN](https://en.wikipedia.org/wiki/Content_delivery_network), so I had to do the detection client-side.

So I resorted to User-Agent (UA) sniffing.

I tried to keep the script small (**currently ~1.4k bytes, minified**) and simple, because it would need to execute in the `<head>`, which is generally a bad idea, since JS blocks the downloading and rendering of all assets while it parses and executes. In the case of mobile redirection, I don't mind so much, because I want to start the redirect as soon as possible, before the device has a chance to start downloading and rendering other stuff. For non-mobile platforms, the script should execute fast, so the browser can quickly get back to downloading and rendering.

## How it works

isMobile runs quickly during initial page load to detect mobile devices; it then creates a JavaScript object with the results.

## Devices detected by isMobile

The following properties of the global `isMobile` object will either be `true` or `false`

### Apple devices

* `isMobile.apple.phone`
* `isMobile.apple.ipod`
* `isMobile.apple.tablet`
* `isMobile.apple.device` (any mobile Apple device)

### Android devices

* `isMobile.android.phone`
* `isMobile.android.tablet`
* `isMobile.android.device` (any mobile Android device)

### Amazon Silk devices (also passes Android checks)

* `isMobile.amazon.phone`
* `isMobile.amazon.tablet`
* `isMobile.amazon.device` (any mobile Amazon Silk device)

### Windows devices

* `isMobile.windows.phone`
* `isMobile.windows.tablet`
* `isMobile.windows.device` (any mobile Windows device)

### Specific seven inch devices

* `isMobile.seven_inch`
	* `true` if the device is one of the following 7" devices:
		- Nexus 7
		- Kindle Fire
		- Nook Tablet 7 inch
		- Galaxy Tab 7 inch

### "Other" devices

* `isMobile.other.blackberry_10`
* `isMobile.other.blackberry`
* `isMobile.other.opera` (Opera Mini)
* `isMobile.other.firefox`
* `isMobile.other.chrome`
* `isMobile.other.device` (any "Other" device)

### Aggregate Groupings

* `isMobile.any` - any device matched
* `isMobile.phone` - any device in the 'phone' groups above
* `isMobile.tablet` - any device in the 'tablet' groups above


## Example Usage

I include the minified version of the script, inline, and at the top of the `<head>`. Cellular connections tend to suck, so it would be wasteful overhead to open another connection, just to download ~1.4kb of JS:


```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <script>
        // Minified version of isMobile included in the HTML since it's small
        !function(a){var b=/iPhone/i,c=/iPod/i,d=/iPad/i,e=/(?=.*\bAndroid\b)(?=.*\bMobile\b)/i,f=/Android/i,g=/IEMobile/i,h=/(?=.*\bWindows\b)(?=.*\bARM\b)/i,i=/BlackBerry/i,j=/BB10/i,k=/Opera Mini/i,l=/(?=.*\bFirefox\b)(?=.*\bMobile\b)/i,m=new RegExp("(?:Nexus 7|BNTV250|Kindle Fire|Silk|GT-P1000)","i"),n=function(a,b){return a.test(b)},o=function(a){var o=a||navigator.userAgent,p=o.split("[FBAN");return"undefined"!=typeof p[1]&&(o=p[0]),this.apple={phone:n(b,o),ipod:n(c,o),tablet:!n(b,o)&&n(d,o),device:n(b,o)||n(c,o)||n(d,o)},this.android={phone:n(e,o),tablet:!n(e,o)&&n(f,o),device:n(e,o)||n(f,o)},this.windows={phone:n(g,o),tablet:n(h,o),device:n(g,o)||n(h,o)},this.other={blackberry:n(i,o),blackberry10:n(j,o),opera:n(k,o),firefox:n(l,o),device:n(i,o)||n(j,o)||n(k,o)||n(l,o)},this.seven_inch=n(m,o),this.any=this.apple.device||this.android.device||this.windows.device||this.other.device||this.seven_inch,this.phone=this.apple.phone||this.android.phone||this.windows.phone,this.tablet=this.apple.tablet||this.android.tablet||this.windows.tablet,"undefined"==typeof window?this:void 0},p=function(){var a=new o;return a.Class=o,a};"undefined"!=typeof module&&module.exports&&"undefined"==typeof window?module.exports=o:"undefined"!=typeof module&&module.exports&&"undefined"!=typeof window?module.exports=p():"function"==typeof define&&define.amd?define("isMobile",[],a.isMobile=p()):a.isMobile=p()}(this);


        // My own arbitrary use of isMobile, as an example
        (function () {
            var MOBILE_SITE = '/mobile/index.html', // site to redirect to
                NO_REDIRECT = 'noredirect'; // cookie to prevent redirect

            // I only want to redirect iPhones, Android phones and a handful of 7" devices
            if (isMobile.apple.phone || isMobile.android.phone || isMobile.seven_inch) {

                // Only redirect if the user didn't previously choose
                // to explicitly view the full site. This is validated
                // by checking if a "noredirect" cookie exists
                if ( document.cookie.indexOf(NO_REDIRECT) === -1 ) {
                    document.location = MOBILE_SITE;
                }
            }
        })();
    </script>
</head>
<body>
    <!-- imagine lots of html and content -->
</body>
</html>
```

### node.js usage

#####Installation
`npm install ismobilejs`

#####Usage
```
var isMobile = require('ismobilejs');
console.log(isMobile(req.headers['user-agent']).any);
```
