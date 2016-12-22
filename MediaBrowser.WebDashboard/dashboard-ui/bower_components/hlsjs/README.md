[![Build Status](https://travis-ci.org/dailymotion/hls.js.svg?branch=master)](https://travis-ci.org/dailymotion/hls.js)
[![npm][npm-image]][npm-url]

Join the discussion: [![Slack Status](https://hlsjs.herokuapp.com/badge.svg)](https://hlsjs.herokuapp.com/)

# ![hls.js](https://cloud.githubusercontent.com/assets/616833/19739063/e10be95a-9bb9-11e6-8100-2896f8500138.png)
hls.js is a JavaScript library which implements an [HTTP Live Streaming] client.
It relies on [HTML5 video][] and [MediaSource Extensions][] for playback.

It works by transmuxing MPEG-2 Transport Stream into ISO BMFF (MP4) fragments.
This transmuxing could be performed asynchronously using [Web Worker] if available in the browser.

hls.js does not need any player, it works directly on top of a standard HTML```<video>```element.

hls.js is written in [ECMAScript6], and transpiled in ECMAScript5 using [Babel].

[HTML5 video]: http://www.html5rocks.com/en/tutorials/video/basics/
[MediaSource Extensions]: http://w3c.github.io/media-source/
[HTTP Live Streaming]: http://en.wikipedia.org/wiki/HTTP_Live_Streaming
[Web Worker]: http://caniuse.com/#search=worker
[ECMAScript6]: https://github.com/ericdouglas/ES6-Learning#articles--tutorials
[Babel]: https://babeljs.io

## Demo

[http://dailymotion.github.io/hls.js/demo](http://dailymotion.github.io/hls.js/demo)

## Getting Started

```js
<script src="https://cdn.jsdelivr.net/hls.js/latest/hls.min.js"></script>
<video id="video"></video>
<script>
  if(Hls.isSupported()) {
    var video = document.getElementById('video');
    var hls = new Hls();
    hls.loadSource('http://www.streambox.fr/playlists/test_001/stream.m3u8');
    hls.attachMedia(video);
    hls.on(Hls.Events.MANIFEST_PARSED,function() {
      video.play();
  });
 }
</script>
```

## Video Control

Video is controlled through HTML ```<video>``` element.

HTMLVideoElement control and events could be used seamlessly.

## they use hls.js in production !


|[<img src="http://i.cdn.turner.com/adultswim/big/img/global/adultswim.jpg" width="120">](http://www.adultswim.com/videos/streams)|[<img src="http://www.brandsoftheworld.com/sites/default/files/styles/logo-thumbnail/public/042013/new_akamai_logo_cmyk_0.png?itok=cm0ZSgjN" width="120">](https://www.akamai.com)|[<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/1/1a/Canal%2B.svg/2000px-Canal%2B.svg.png" width="120">](https://www.canalplus.fr)|[<img src="http://press.dailymotion.com/fr/wp-content/uploads/sites/4/2010/06/LOGO-PRESS-BLOG.png" width="120">](http://www.dailymotion.com)|
|---|---|---|---|
|[<img src="https://flowplayer.org/media/img/logo-blue.png" width="120">](http://www.flowplayer.org)|[<img src="https://avatars1.githubusercontent.com/u/12554082?s=240" width="120">](http://www.foxsports.com.au)|[<img src="https://cloud.githubusercontent.com/assets/244265/12556435/dfaceb48-c353-11e5-971b-2c4429725469.png" width="120">](http://www.globo.com)|[<img src="https://cloud.githubusercontent.com/assets/13573755/20111430/80a6bebc-a5e8-11e6-8dc1-619b1e936f6d.png" width="120">](https://www.gl-systemhaus.de/)|
|[<img src="https://cloud.githubusercontent.com/assets/6525783/20801836/700490de-b7ea-11e6-82bd-e249f91c7bae.jpg" width="120">](http://nettrek.de/)|[<img src="https://cloud.githubusercontent.com/assets/244265/12556385/999aa884-c353-11e5-9102-79df54384498.png" width="120">](https://www.nytimes.com/)|[<img src="https://cloud.githubusercontent.com/assets/1798553/20356424/ba158574-ac24-11e6-95e1-1ae591b11a0a.png" width="120">](https://www.peer5.com/)|[<img src="https://cloud.githubusercontent.com/assets/4909096/20925062/e26e6fc8-bbb4-11e6-99a5-d4762274a342.png" width="120">](http://qbrick.com/)|
|[<img src="https://www.radiantmediaplayer.com/images/radiantmediaplayer-new-logo-640.jpg" width="120">](https://www.radiantmediaplayer.com/)|[<img src="https://camo.githubusercontent.com/eacade2264a6325191b6cb9bf7a8c0d05a5b628d/68747470733a2f2f7777772e7274732e63682f68756d6d696e67626972642f7265732f696d616765732f7274732f6c6f676f2d7274732d726f7567652e737667" width="120">](http://www.rts.ch/)|[<img src="https://cloud.githubusercontent.com/assets/12702747/19316434/0a3601de-9067-11e6-85e2-936b1cb099a0.png" width="120">](https://www.snapstream.com/)|[<img src="https://www.streamamg.com/wp-content/themes/barebones/_assets/images/streamamg-logo.png" width="120">](https://www.streamamg.com/)|
|[<img src="https://streamsharkio.sa.metacdn.com/wp-content/uploads/2015/10/streamshark-dark.svg" width="120">](https://streamshark.io/)|[<img src="https://camo.githubusercontent.com/9580f10e9bfa8aa7fba52c5cb447bee0757e33da/68747470733a2f2f7777772e7461626c6f74762e636f6d2f7374617469632f696d616765732f7461626c6f5f6c6f676f2e706e67" width="120">](http://my.tablotv.com/)|[<img src="https://cloud.githubusercontent.com/assets/1798553/20356423/ba13fd6c-ac24-11e6-971c-be30c75592ef.png" width="120">](https://www.streamroot.io/)|[<img src="http://vignette1.wikia.nocookie.net/tedtalks/images/c/c0/TED_logo.png/revision/20150915192527" width="120">](https://www.ted.com/)|
|[<img src="https://www.seeklogo.net/wp-content/uploads/2014/12/twitter-logo-vector-download.jpg" width="120">](https://twitter.com/)|[<img src="https://cloud.githubusercontent.com/assets/8201317/20566816/bc33f51c-b196-11e6-9bd3-afb71a06460b.png" width="120">](http://vwflow.com)|


## Player Integration

hls.js is (being) integrated in the following players:

 - [Akamai Adaptive Media Player (AMP)] (https://www.akamai.com/us/en/solutions/products/media-delivery/adaptive-media-player.jsp)
 - [Clappr] (https://github.com/clappr/clappr)
 - [Flowplayer] (https://www.flowplayer.org)  through [flowplayer-hlsjs] (https://github.com/flowplayer/flowplayer-hlsjs)
 - [MediaElement.js] (http://mediaelementjs.com/) integrated in [3.x-dev] (https://github.com/johndyer/mediaelement/tree/3.x-dev)
 - [Videojs] (http://videojs.com) through [Videojs-hlsjs] (https://github.com/benjipott/videojs-hlsjs)
 - [Videojs] (http://videojs.com) through [videojs-hls.js] (https://github.com/streamroot/videojs-hls.js). hls.js is integrated as a SourceHandler -- new feature in Video.js 5.
 - [Videojs] (http://videojs.com) through [videojs-contrib-hls.js](https://github.com/Peer5/videojs-contrib-hls.js). Production ready plug-in with full fallback compatibility built-in.


## Chrome/Firefox integration

made by [gramk](https://github.com/gramk/chrome-hls),plays hls from address bar and m3u8 links 

 - Chrome [native-hls] (https://chrome.google.com/webstore/detail/native-hls-playback/emnphkkblegpebimobpbekeedfgemhof)
 - Firefox [native-hls] (https://addons.mozilla.org/en-US/firefox/addon/native_hls_playback/)



## Dependencies

No external JS libs are needed.
Prepackaged build is included in the [dist] (dist) folder:

 - [hls.js] (dist/hls.js)
 - [hls.min.js] (dist/hls.min.js)

If you want to bundle the application yourself, use node

```
npm install hls.js
```

## Installation

Either directly include dist/hls.js or dist/hls.min.js

Or type

```sh
npm install --save hls.js
```

## Compatibility
hls.js is compatible with browsers supporting MSE with 'video/MP4' inputs.
As of today, it is supported on:

 * Chrome for Android 34+
 * Chrome for Desktop 34+
 * Firefox for Android 41+
 * Firefox for Desktop 42+
 * IE11+ for Windows 8.1+
 * Edge for Windows 10+
 * Opera for Desktop
 * Vivaldi for Desktop
 * Safari for Mac 8+ (beta)

## CORS

All HLS resources must be delivered with [CORS headers](https://developer.mozilla.org/en-US/docs/Web/HTTP/Access_control_CORS) permitting `GET` requests.

## Features

  - VoD & Live playlists
    - DVR support on Live playlists
  - MPEG-2 TS container
    - ITU-T Rec. H.264 and ISO/IEC 14496-10 Elementary Stream
    - ISO/IEC 13818-7 ADTS AAC Elementary Stream
    - ISO/IEC 11172-3 / ISO/IEC 13818-3 (MPEG-1/2 Audio Layer III) Elementary Stream
    - Packetized metadata (ID3) Elementary Stream
  - AAC container (audio only streams)
  - Adaptive streaming
    - Manual & Auto Quality Switching
      - 3 Quality Switching modes are available (controllable through API means)
      	- Instant switching (immediate quality switch at current video position)
      	- Smooth switching (quality switch for next loaded fragment)
      	- Bandwidth conservative switching (quality switch change for next loaded fragment, without flushing the buffer)
      - In Auto-Quality mode, emergency switch down in case bandwidth is suddenly dropping to minimize buffering.        
  - Accurate Seeking on VoD & Live (not limited to fragment or keyframe boundary)
  - Ability to seek in buffer and back buffer without redownloading segments
  - Built-in Analytics
    - Every internal events could be monitored (Network Events,Video Events)
    - Playback session metrics are also exposed
  - Resilience to errors
    - Retry mechanism embedded in the library
    - Recovery actions could be triggered fix fatal media or network errors
  - [Redundant/Failover Playlists](https://developer.apple.com/library/ios/documentation/NetworkingInternet/Conceptual/StreamingMediaGuide/UsingHTTPLiveStreaming/UsingHTTPLiveStreaming.html#//apple_ref/doc/uid/TP40008332-CH102-SW22)
  - Timed Metadata for HTTP Live Streaming (in ID3 format, carried in MPEG-2 TS)
  - AES-128 decryption (AES-128 mode)
  - CEA-708 captions
  - Alternate Audio Track Rendition (Master Playlist with alternative Audio) for VoD and Live playlists  

## Not Supported (Yet)

  - WebVTT container
  - MP3 Elementary Stream in Edge for Windows 10+
  - MP3 container

### Supported M3U8 tags

  - `#EXTM3U`
  - `#EXTINF`
  - `#EXT-X-STREAM-INF` (adaptive streaming)
  - `#EXT-X-ENDLIST` (Live playlist)
  - `#EXT-X-MEDIA-SEQUENCE`
  - `#EXT-X-TARGETDURATION`
  - `#EXT-X-DISCONTINUITY`
  - `#EXT-X-DISCONTINUITY-SEQUENCE`  
  - `#EXT-X-BYTERANGE`
  - `#EXT-X-KEY` (https://tools.ietf.org/html/draft-pantos-http-live-streaming-08#section-3.4.4)
  - `#EXT-X-PROGRAM-DATE-TIME` (https://tools.ietf.org/html/draft-pantos-http-live-streaming-18#section-4.3.2.6)
  - `EXT-X-START:TIME-OFFSET=x` (https://tools.ietf.org/html/draft-pantos-http-live-streaming-18#section-4.3.5.2)


## API and Configuration Parameters

hls.js can be configured and controlled easily, click [here](API.md) for details.


## License

  hls.js is released under [Apache 2.0 License](LICENSE)

## Contributing

Pull requests are welcome. Here is a quick guide on how to start.

 - First, checkout the repository and install required dependencies
```sh
git clone https://github.com/dailymotion/hls.js.git
# setup dev environement
cd hls.js
npm install
# build dist/hls.js, watch file change for rebuild and launch demo page
npm run dev
# lint
npm run lint
```
 - Use [EditorConfig](http://editorconfig.org/) or at least stay consistent to the file formats defined in the `.editorconfig` file.
 - Develop in a topic branch, not master
 - Don't commit the updated `dist/hls.js` file in your PR. We'll take care of generating an updated build right before releasing a new tagged version.

## Design

Click [here](design.md) for details.

[npm-image]: https://img.shields.io/npm/v/hls.js.svg?style=flat
[npm-url]: https://npmjs.org/package/hls.js
