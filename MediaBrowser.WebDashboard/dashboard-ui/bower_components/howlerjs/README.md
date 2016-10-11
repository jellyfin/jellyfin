**NOTE:** Work is currently ongoing in the 2.0 branch. No ETA for release as of yet, but the core is fairly stable and is already under heavy production use. [Click here to view the 2.0 beta branch](https://github.com/goldfire/howler.js/tree/2.0).

![howler.js](http://goldfirestudios.com/proj/howlerjs/howlerjs_logo.png "howler.js")

## Description
[**howler.js**](http://howlerjs.com) is an audio library for the modern web. It defaults to [Web Audio API](https://dvcs.w3.org/hg/audio/raw-file/tip/webaudio/specification.html) and falls back to [HTML5 Audio](http://www.whatwg.org/specs/web-apps/current-work/#the-audio-element).

More documentation, examples and demos can be found at **[howlerjs.com](http://howlerjs.com)**.

### Features
* Defaults to Web Audio API
* Falls back to HTML5 Audio
* Supports multiple file formats to support all browsers
* Automatic caching for Web Audio API
* Implements cache pool for HTML5 Audio
* Per-sound and global mute/unmute and volume control
* Playback of multiple sounds at the same time
* Easy sound sprite definition and playback
* Fade in/out sounds
* Supports Web Audio 3D sound positioning
* Methods can be chained
* Uses no outside libraries, just pure JavaScript
* Lightweight; 12kb minified filesize (4kb gzipped)

### Browser Compatibility
Tested in the following browsers/versions:
* Google Chrome 4.0+
* Internet Explorer 9.0+
* Firefox 3.5+
* Safari 4.0+
* Mobile Safari 6.0+ (after user input)
* Opera 10.5+

## Documentation

### Examples

##### Most basic, play an MP3:
```javascript
var sound = new Howl({
  urls: ['sound.mp3']
}).play();
```

##### More playback options:
```javascript
var sound = new Howl({
  urls: ['sound.mp3', 'sound.ogg', 'sound.wav'],
  autoplay: true,
  loop: true,
  volume: 0.5,
  onend: function() {
    console.log('Finished!');
  }
});
```

##### Define and play a sound sprite:
```javascript
var sound = new Howl({
  urls: ['sounds.mp3', 'sounds.ogg'],
  sprite: {
    blast: [0, 1000],
    laser: [2000, 3000],
    winner: [4000, 7500]
  }
});

// shoot the laser!
sound.play('laser');
```

### Properties
* **autoplay**: `Boolean` *(`false` by default)* Set to `true` to automatically start playback when sound is loaded.
* **buffer**: `Boolean` *(`false` by default)* Set to `true` to force HTML5 Audio. This should be used for large audio files so that you don't have to wait for the full file to be downloaded and decoded before playing.
* **format**: `String` *(`null` by default)* howler.js automatically detects your file format from the URL, but you may also specify a format in situations where URL extraction won't work.
* **loop**: `Boolean` *(`false` by default)* Set to `true` to automatically loop the sound forever.
* **sprite**: `Object` *(`{}` by default)* Define a sound sprite for the sound. The offset and duration are defined in milliseconds. A third (optional) parameter is available to set a sprite as looping.
```
Example:
{
  key: [offset, duration, (loop)]
}
```
* **volume**: `Number` *(`1.0` by default)* The volume of the specific track, from `0.0` to `1.0`.
* **urls**: `Array` *(`[]` by default)* The source URLs to the track(s) to be loaded for the sound. These should be in order of preference, howler.js will automatically load the first one that is compatible with the current browser. If your files have no extensions, you will need to explicitly specify the format using the `format` property.
* **rate**: `Number` *(`1.0` by default)* The rate of playback (Web Audio API only). 1.0 is normal speed, while negative values play in reverse.
* **model**: `String` *(`equalpower` by default)* Sets the `panningModel` used by Web Audio API. Usually this should not be touched as howler handles it automatically, but it can be overridden with `equalpower` or `HRTF`.
* **onend**: `Function` *(`function(){}` by default)* Fire when the sound finishes playing (if it is looping, it'll fire at the end of each loop).
* **onload**: `Function` *(`function(){}` by default)* Fires when the sound is loaded.
* **onloaderror**: `Function` *(`function(){}` by default)* Fires when the sound is unable to load. Error message is passed as first parameter.
* **onpause**: `Function` *(`function(){}` by default)* Fires when the sound has been paused.
* **onplay**: `Function` *(`function(){}` by default)* Fires when the sound begins playing.

### Methods
* **play**: Begins playback of sound. Will continue from previous point if sound has been previously paused.
  * *sprite*: `String` (optional) Plays from the defined sprite key.
  * *callback*: `Function` (optional) Fires when playback begins and returns the `soundId`, which is the unique identifier for this specific playback instance.
* **pause**: Pauses playback of sound, saving the `pos` of playback.
  * *id*: `Number` (optional) The play instance ID.
* **stop**: Stops playback of sound, resetting `pos` to `0`.
  * *id*: `Number` (optional) The play instance ID.
* **mute**: Mutes the sound, but doesn't pause the playback.
  * *id*: `Number` (optional) The play instance ID.
* **unmute**: Unmutes the sound.
  * *id*: `Number` (optional) The play instance ID.
* **fade**: Fade a currently playing sound between two volumes.
  * *from*: `Number` Volume to fade from (`0.0` to `1.0`).
  * *to*: `Number` Volume to fade to (`0.0` to `1.0`).
  * *duration*: `Number` Time in milliseconds to fade.
  * *callback*: `Function` (optional) Fires when fade is complete.
  * *id*: `Number` (optional) The play instance ID.
* [DEPRECATED] **fadeIn**: Fade in the current sound.
  * *to*: `Number` Volume to fade to (`0.0` to `1.0`).
  * *duration*: `Number` Time in milliseconds to fade.
  * *callback*: `Function` (optional) Fires when fade is complete.
* [DEPRECATED] **fadeOut**: Fade out the current sound and pause when finished.
  * *to*: `Number` Volume to fade to (`0.0` to `1.0`).
  * *duration*: `Number` Time in milliseconds to fade.
  * *callback*: `Function` (optional) Fires when fade is complete.
  * *id*: `Number` (optional) The play instance ID.
* **loop**: Get/set whether to loop the sound.
  * *loop*: `Boolean` (optional) To loop or not to loop, that is the question.
* **pos**: Get/set the position of playback.
  * *position*: `Number` (optional) The position to move current playback to (in seconds).
  * *id*: `Number` (optional) The play instance ID.
* **pos3d**: Get/set the 3D position of the audio source. The most common usage is to set the `x` position to affect the left/right ear panning. Setting the value higher than `1.0` will begin to decrease the volume of the sound as it moves further away. **This only works with Web Audio API.**
  * *x*: `Number` The x-position of the sound.
  * *y*: `Number` The y-position of the sound.
  * *z*: `Number` The z-position of the sound.
  * *id*: `Number` (optional) The play instance ID.
* **sprite**: Get/set sound sprite definition.
  * *sprite*: `Object` (optional) See above for sound sprite definition.
* **volume**: Get/set volume of this sound.
  * *volume*: `Number` (optional) Volume from `0.0` to `1.0`.
  * *id*: `Number` (optional) The play instance ID.
* **urls**: Get/set the URLs to be pulled from to play in this source.
  * *urls*: `Array` (optional) Changes the source files for this `Howl` object.
* **on**: Call/set custom events. Multiple events can be added by calling this multiple times.
  * *event*: `String` Name of event to fire/set.
  * *function*: `Function` (optional) Define function to fire on event.
* **off**: Remove custom events that you've set.
  * *event*: `String` Name of event.
  * *function*: `Function` (optional) The listener to remove.
* **unload**: Unload and destroy a Howl object. This will immediately stop all play instances attached to this sound and remove it from the cache.

### Global Methods
The following methods are used to modify all sounds globally, and are called from the `Howler` object.

* **mute**: Mutes all sounds.
* **unmute**: Unmutes all sounds and restores them to their previous volume.
* **volume**: Get/set the global volume for all sounds.
  * *volume*: `Number` (optional) Volume from `0.0` to `1.0`.
* **codecs**: Check supported audio codecs.
  * *ext*: `String` File extension. One of: "mp3", "opus", "ogg", "wav", "aac", "m4a", "mp4", "weba".

### iOS Playback
By default, audio on iOS is locked until a sound is played within a user interaction, and then it plays normally the rest of the page session ([Apple documentation](https://developer.apple.com/library/safari/documentation/audiovideo/conceptual/using_html5_audio_video/PlayingandSynthesizingSounds/PlayingandSynthesizingSounds.html)). The default behavior of howler.js is to attempt to silently unlock audio playback by playing an empty buffer on the first `touchend` event. This behavior can be disabled by calling:

```javascript
Howler.iOSAutoEnable = false;
```

## License

Copyright (c) 2013-2014 James Simpson and GoldFire Studios, Inc.

Released under the MIT License.
