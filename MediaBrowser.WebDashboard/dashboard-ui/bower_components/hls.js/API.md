
##Hello hls.js !

###first step : setup and support
first include ```dist/hls.{min}.js``` in your web page and check whether your browser is supporting [MediaSource Extensions][].
[MediaSource Extensions]: http://w3c.github.io/media-source/
just invoke the following static method : ```Hls.isSupported()```

```js
 <script src="dist/hls.{min}.js"></script>
<script>
  if(Hls.isSupported()) {
 	console.log("hello hls.js!");
 }
</script>
```

###second step: instanciate hls object and bind it to```<video>```element
let's

   - create a```<video>```element
   - create a new HLS object
   - bind video element to this HLS object


```js
 <script src="dist/hls.{min}.js"></script>

<video id="video"></video>
<script>
  if(Hls.isSupported()) {
    var video = document.getElementById('video');
    var hls = new Hls();
    // bind them together
    hls.attachMedia(video);
    // MEDIA_ATTACHED event is fired by hls object once MediaSource is ready
    hls.on(Hls.Events.MEDIA_ATTACHED,function() {
		  console.log("video and hls.js are now bound together !");
    });
 }
</script>
```

###third step: load a manifest
you need to provide manifest URL as below:

```js
 <script src="dist/hls.{min}.js"></script>

<video id="video"></video>
<script>
  if(Hls.isSupported()) {
    var video = document.getElementById('video');
    var hls = new Hls();
    // bind them together
    hls.attachMedia(video);
    hls.on(Hls.Events.MEDIA_ATTACHED,function() {
		console.log("video and hls.js are now bound together !");
		hls.loadSource("http://my.streamURL.com/playlist.m3u8");
		hls.on(Hls.Events.MANIFEST_PARSED, function(event,data) {
         console.log("manifest loaded, found " + data.levels.length + " quality level");
		}
     });
 }
</script>
```

###fourth step : control through ```<video>``` element

video is controlled through HTML ```<video>``` element.

HTMLVideoElement control and events could be used seamlessly.

```js
  video.play();
```

###fifth step : error handling

all errors are signalled through a unique single event.

each error is categorized by :

  - its type:
    - ```Hls.ErrorTypes.NETWORK_ERROR```for network related errors
    - ```Hls.ErrorTypes.MEDIA_ERROR```for media/video related errors
    - ```Hls.ErrorTypes.OTHER_ERROR```for all other errors
  - its details:
    - refer to [Errors details](#Errors)
  - its fatality:
    - ```false```if error is not fatal, hls.js will try to recover it
    - ```true```if error is fatal, an action is required to (try to) recover it.

 full details is described [below](#Errors)


 see sample code below to listen to errors:

```js
  hls.on(Hls.Events.ERROR,function(event,data) {

  var errorType = data.type;
  var errorDetails = data.details;
  var errorFatal = data.fatal;

    switch(data.details) {
      case hls.ErrorDetails.FRAG_LOAD_ERROR:
        // ....
        break;
      default:
        break;
    }
  }
```

#### Fatal Error Recovery

hls.js provides means to 'try to' recover fatal network and media errors, through these 2 methods:

##### ```hls.startLoad()```

should be invoked to recover network error.

##### ```hls.recoverMediaError()```

should be invoked to recover media error

##### error recovery sample code

```js
  hls.on(Hls.Events.ERROR,function(event,data) {
    if(data.fatal) {
      switch(data.type) {
      case Hls.ErrorTypes.NETWORK_ERROR:
      // try to recover network error
        console.log("fatal network error encountered, try to recover");
        hls.startLoad();
        break;
      case Hls.ErrorTypes.MEDIA_ERROR:
        console.log("fatal media error encountered, try to recover");
        hls.recoverMediaError();
        break;
      default:
      // cannot recover
        hls.destroy();
        break;
    }
  }
```

##### ```hls.swapAudioCodec()```

If media error are still raised after calling ```hls.recoverMediaError()```,
calling this method, could be useful to workaround audio codec mismatch.
the workflow should be :

on Media Error : first call ```hls.swapAudioCodec()```, then call ```hls.recoverMediaError()```

###final step : destroying, switching between streams

```hls.destroy()``` should be called to free used resources and destroy hls context.



## Fine Tuning

configuration parameters could be provided to hls.js upon instantiation of Hls Object.

```js

   var config = {
      autoStartLoad : true,
      startPosition : -1,
      capLevelToPlayerSize: false,
      debug : false,
      defaultAudioCodec : undefined,
      maxBufferLength : 30,
      maxMaxBufferLength : 600,
      maxBufferSize : 60*1000*1000,
      maxBufferHole : 0.3,
      maxSeekHole : 2,
      seekHoleNudgeDuration : 0.01,
      maxFragLookUpTolerance : 0.2,
      liveSyncDurationCount : 3,
      liveMaxLatencyDurationCount: 10,
      enableWorker : true,
      enableSoftwareAES: true,
      manifestLoadingTimeOut : 10000,
      manifestLoadingMaxRetry : 6,
      manifestLoadingRetryDelay : 500,
      levelLoadingTimeOut : 10000,
      levelLoadingMaxRetry : 6,
      levelLoadingRetryDelay : 500,
      fragLoadingTimeOut : 20000,
      fragLoadingMaxRetry : 6,
      fragLoadingRetryDelay : 500,
      startFragPrefech : false,
      appendErrorMaxRetry : 3,
      loader : customLoader,
      fLoader: customFragmentLoader,
      pLoader: customPlaylistLoader,
      xhrSetup : XMLHttpRequestSetupCallback,
      abrController : customAbrController,
      timelineController: TimelineController,
      enableCEA708Captions: true,
      abrEwmaFastLive: 5.0,
      abrEwmaSlowLive: 9.0,
      abrEwmaFastVoD: 4.0,
      abrEwmaSlowVoD: 15.0,
      abrEwmaDefaultEstimate: 500000,
      abrBandWidthFactor: 0.8,
      abrBandWidthUpFactor: 0.7
    };


var hls = new Hls(config);
```

#### ```Hls.DefaultConfig get/set```
this getter/setter allows to retrieve and override Hls default configuration.
this configuration will be applied by default to all instances.

#### ```capLevelToPlayerSize```
 (default false)
 
  - if set to true, the adaptive algorithm with limit levels usable in auto-quality by the HTML video element dimensions (width and height)
  - if set to false, levels will not be limited. All available levels could be used in auto-quality mode taking only bandwidth into consideration.
 
#### ```debug```
(default false)

setting ```config.debug=true``` will turn on debug logs on JS console.
a logger object could also be provided for custom logging : ```config.debug=customLogger```

#### ```autoStartLoad```
(default true)

 - if set to true, start level playlist and first fragments will be loaded automatically, after triggering of ```Hls.Events.MANIFEST_PARSED``` event
 - if set to false, an explicit API call (```hls.startLoad(startPosition=-1)```) will be needed to start quality level/fragment loading.

#### ```startPosition```
(default -1)

 - if set to -1, playback will start from initialTime=0 for VoD and according to ```liveSyncDuration/liveSyncDurationCount``` config params for Live
 - Otherwise, playback will start from predefined value. (unless stated otherwise in ```autoStartLoad=false``` mode : in that case startPosition can be overrided using ```hls.startLoad(startPosition)```).


#### ```defaultAudioCodec```
(default undefined)

 if audio codec is not signaled in variant manifest, or if only a stream manifest is provided, hls.js tries to guess audio codec by parsing audio sampling rate in ADTS header. if sampling rate is less or equal than 22050 Hz, then hls.js assumes it is HE-AAC, otherwise it assumes it is AAC-LC. This could result in bad guess, leading to audio decode error, ending up in media error.
 it is possible to hint default audiocodec to hls.js by configuring this value as below:
  - ```mp4a.40.2``` (AAC-LC) or 
  - ```mp4a.40.5``` (HE-AAC) or
  - ```undefined``` (guess based on sampling rate)

#### ```maxBufferLength```
(default 30s)

maximum buffer Length in seconds. if buffer length is/become less than this value, a new fragment will be loaded.
this is the guaranteed buffer length hls.js will try to reach, regardless of maxBufferSize.

#### ```maxBufferSize```
(default 60 MB)

'minimum' maximum buffer size in bytes. if buffer size upfront is bigger than this value, no fragment will be loaded.

#### ```maxBufferHole```
(default 0.3s)

'maximum' inter-fragment buffer hole tolerance that hls.js can cope with when searching for the next fragment to load.
When switching between quality level, fragments might not be perfectly aligned.
This could result in small overlapping or hole in media buffer. This tolerance factor helps cope with this.

#### ```maxSeekHole```
(default 2s)

in case playback is stalled, and a buffered range is available upfront, less than maxSeekHole seconds from current media position,
hls.js will jump over this buffer hole to reach the beginning of this following buffered range.
```maxSeekHole``` allows to configure this jumpable threshold.

#### ```maxStarvationDelay```
(default 2s)

ABR algorithm will always try to choose a quality level that should avoid rebuffering.
In case no quality level with this criteria can be found (lets say for example that buffer length is 1s, but fetching a fragment at lowest quality is predicted to take around 2s ... ie we can forecast around 1s of rebuffering ...) then ABR algorithm will try to find a level that should guarantee less than ```maxStarvationDelay``` of buffering.
this max delay is also used in  automatic start level selection : in that mode ABR controller will ensure that video loading time (ie the time to fetch the first fragment at lowest quality level + the time to fetch the fragment at the appropriate quality level is less than ```maxStarvationDelay``` )

#### ```seekHoleNudgeDuration```
(default 0.01s)

 in case playback is still stalling although a seek over buffer hole just occured, hls.js will seek to next buffer start + (nb of consecutive stalls * seekHoleNudgeDuration to try to restore playback


#### ```maxFragLookUpTolerance```
(default 0.2s)

this tolerance factor is used during fragment lookup.
instead of checking whether buffered.end is located within [start, end] range, frag lookup will be done by checking  within [start-maxFragLookUpTolerance, end-maxFragLookUpTolerance] range

this tolerance factor is used to cope with situations like
buffered.end = 9.991
frag[Ø] : [0,10]
frag[1] : [10,20]
=> buffered.end is within frag[0] range, but as we are close to frag[1], frag[1] should be choosen instead


if maxFragLookUpTolerance=0.2, 
this lookup will be adjusted to 
frag[Ø] : [-0.2,9.8]
frag[1] : [9.8,19.8]
=> this time, buffered.end is within frag[1] range, and frag[1] will be the next fragment to be loaded, as expected.

#### ```maxMaxBufferLength```
(default 600s)

maximum buffer Length in seconds. hls.js will never exceed this value. even if maxBufferSize is not reached yet.

hls.js tries to buffer up to a maximum number of bytes (60 MB by default) rather than to buffer up to a maximum nb of seconds.
this is to mimic the browser behaviour (the buffer eviction algorithm is starting after the browser detects that video buffer size reaches a limit in bytes)

config.maxBufferLength is the minimum guaranteed buffer length that hls.js will try to achieve, even if that value exceeds the amount of bytes 60 MB of memory.
maxMaxBufferLength acts as a capping value, as if bitrate is really low, you could need more than one hour of buffer to fill 60 MB....


#### ```liveSyncDurationCount```
(default 3)

edge of live delay, expressed in multiple of ```EXT-X-TARGETDURATION```.
if set to 3, playback will start from fragment N-3, N being the last fragment of the live playlist.
decreasing this value is likely to cause playback stalls.

#### ```liveMaxLatencyDurationCount```
(default Infinity)

maximum delay allowed from edge of live, expressed in multiple of ```EXT-X-TARGETDURATION```.
if set to 10, the player will seek back to ```liveSyncDurationCount``` whenever the next fragment to be loaded is older than N-10, N being the last fragment of the live playlist.
If set, this value must be stricly superior to ```liveSyncDurationCount```
a value too close from ```liveSyncDurationCount``` is likely to cause playback stalls.

#### ```liveSyncDuration```
(default undefined)

Alternative parameter to ```liveSyncDurationCount```, expressed in seconds vs number of segments.
If defined in the configuration object, ```liveSyncDuration``` will take precedence over the default```liveSyncDurationCount```.
You can't define this parameter and either ```liveSyncDurationCount``` or ```liveMaxLatencyDurationCount``` in your configuration object at the same time.
A value too low (inferior to ~3 segment durations) is likely to cause playback stalls.

#### ```liveMaxLatencyDuration```
(default undefined)

Alternative parameter to ```liveMaxLatencyDurationCount```, expressed in seconds vs number of segments.
If defined in the configuration object, ```liveMaxLatencyDuration``` will take precedence over the default```liveMaxLatencyDurationCount```.
If set, this value must be stricly superior to ```liveSyncDuration``` which must be defined as well.
You can't define this parameter and either ```liveSyncDurationCount``` or ```liveMaxLatencyDurationCount``` in your configuration object at the same time.
A value too close from ```liveSyncDuration``` is likely to cause playback stalls.

#### ```enableWorker```
(default true)

enable webworker (if available on browser) for TS demuxing/MP4 remuxing, to improve performance and avoid lag/frame drops.

#### ```enableSoftwareAES```
(default true)

enable to use JavaScript version AES decryption for fallback of WebCrypto API.

#### ```fragLoadingTimeOut```/```manifestLoadingTimeOut```/```levelLoadingTimeOut```
(default 60000ms for fragment/10000ms for level and manifest)

URL Loader timeout.
A timeout callback will be triggered if loading duration exceeds this timeout.
no further action will be done : the load operation will not be cancelled/aborted.
It is up to the application to catch this event and treat it as needed.
#### ```fragLoadingMaxRetry```/```manifestLoadingMaxRetry```/```levelLoadingMaxRetry```
(default 3)

max nb of load retry
#### ```fragLoadingRetryDelay```/```manifestLoadingRetryDelay```/```levelLoadingRetryDelay```
(default 1000ms)

initial delay between XmlHttpRequest error and first load retry (in ms)
any I/O error will trigger retries every 500ms,1s,2s,4s,8s, ... capped to 64s (exponential backoff)

prefetch start fragment although media not attached
#### ```startFragPrefetch```
(default false)

start prefetching start fragment although media not attached yet

max nb of append retry
#### ```appendErrorMaxRetry```
(default 3)

max number of sourceBuffer.appendBuffer() retry upon error.
such error could happen in loop with UHD streams, when internal buffer is full. (Quota Exceeding Error will be triggered). in that case we need to wait for the browser to evict some data before being able to append buffer correctly.

#### ```loader```
(default : standard XmlHttpRequest based URL loader)

override standard URL loader by a custom one.
could be useful for P2P or stubbing (testing).

Use this, if you want to overwrite both the fragment and the playlist loader.

Note: If fLoader or pLoader are used, they overwrite loader!

```js
var customLoader = function() {

  /* calling load() will start retrieving content at given URL (HTTP GET)
  params :
  url : URL to load
  responseType : xhr response Type (arraybuffer or default response Type for playlist)
  onSuccess : callback triggered upon successful loading of URL.
              it should return xhr event and load stats object {trequest,tfirst,tload}
  onError :   callback triggered if any I/O error is met while loading fragment
  onTimeOut : callback triggered if loading is still not finished after a certain duration
  timeout : timeout after which onTimeOut callback will be triggered(if loading is still not finished after that delay)
  maxRetry : max nb of load retry
  retryDelay : delay between an I/O error and following connection retry (ms). this to avoid spamming the server.
  */
  this.load = function(url,responseType,onSuccess,onError,onTimeOut,timeout,maxRetry,retryDelay) {}

  /* abort any loading in progress */
  this.abort = function() {}
  /* destroy loading context */
  this.destroy = function() {}
  }
```

#### ```fLoader```
(default : undefined)

This enables the manipulation of the fragment loader.
Note: This will overwrite the default loader, as well as your own loader function (see above).
```js
var customFragmentLoader = function() {
    //See loader for details
  }
```

#### ```pLoader```
(default : undefined)

This enables the manipulation of the playlist loader.
Note: This will overwrite the default loader, as well as your own loader function (see above).
```js
var customPlaylistLoader = function() {
    //See loader for details
  }
```

#### ```xhrSetup```
(default : none)

XmlHttpRequest customization callback for default XHR based loader.

parameter should be a function with one single argument (of type XMLHttpRequest).
If ```xhrSetup``` is specified, default loader will invoke it before calling ```xhr.send()```.
This allows user to easily modify/setup XHR. see example below.

```js
var config = {
  xhrSetup: function(xhr, url) {
    xhr.withCredentials = true; // do send cookies
  }
}
```

#### ```abrController```
(default : internal ABR controller)

customized Adaptive Bitrate Streaming Controller

parameter should be a class providing 2 getter/setters and a destroy() method:

 - get/set nextAutoLevel : get/set : return next auto-quality level/force next auto-quality level that should be returned (currently used for emergency switch down)
 - get/set autoLevelCapping : get/set : capping/max level value that could be used by ABR Controller
 - destroy() : should clean-up all used resources

#### ```timelineController```
(default : internal track timeline controller)

customized text track syncronization controller

parameter should be a class a destroy() method:

 - destroy() : should clean-up all used resources

#### ```enableCEA708Captions```
(default : true)

whether or not to enable CEA-708 captions

parameter should be a boolean

#### ```abrEwmaFastLive```
(default : 5.0)

Fast bitrate Exponential moving average half-life, used to compute average bitrate for Live streams.
Half of the estimate is based on the last abrEwmaFastLive seconds of sample history.
Each of the sample is weighted by the fragment loading duration.

parameter should be a float greater than 0

#### ```abrEwmaSlowLive```
(default : 9.0)

Slow bitrate Exponential moving average half-life, used to compute average bitrate for Live streams.
Half of the estimate is based on the last abrEwmaSlowLive seconds of sample history.
Each of the sample is weighted by the fragment loading duration.

parameter should be a float greater than abrEwmaFastLive

#### ```abrEwmaFastVoD```
(default : 4.0)

Fast bitrate Exponential moving average half-life, used to compute average bitrate for VoD streams.
Half of the estimate is based on the last abrEwmaFastVoD seconds of sample history.
Each of the sample is weighted by the fragment loading duration.

parameter should be a float greater than 0

#### ```abrEwmaSlowVoD```
(default : 15.0)

Slow bitrate Exponential moving average half-life, used to compute average bitrate for VoD streams.
Half of the estimate is based on the last abrEwmaSlowVoD seconds of sample history.
Each of the sample is weighted by the fragment loading duration.

parameter should be a float greater than abrEwmaFastVoD

#### ```abrEwmaDefaultEstimate```
(default : 500000)

Default bandwidth estimate in bits/second prior to collecting fragment bandwidth samples.

parameter should be a float


#### ```abrBandWidthFactor```
(default : 0.8)

Scale factor to be applied against measured bandwidth average, to determine whether we can stay on current or lower quality level.
If ```abrBandWidthFactor * bandwidth average < level.bitrate``` then ABR can switch to that level providing that it is equal or less than current level.

#### ```abrBandWidthUpFactor```
(default : 0.7)

Scale factor to be applied against measured bandwidth average, to determine whether  we can switch up to a higher quality level.
If ```abrBandWidthUpFactor * bandwidth average < level.bitrate``` then ABR can switch up to that quality level.

## Video Binding/Unbinding API

#### ```hls.attachMedia(videoElement)```
calling this method will :

 - bind videoElement and hls instance,
 - create MediaSource and set it as video source
 - once MediaSource object is successfully created, MEDIA_ATTACHED event will be fired.

#### ```hls.detachMedia()```
calling this method will :

 - unbind VideoElement from hls instance,
 - signal the end of the stream on MediaSource
 - reset video source (```video.src = ''```)

## Quality switch Control API

by default, hls.js handles quality switch automatically, using heuristics based on fragment loading bitrate and quality level bandwidth exposed in the variant manifest.
it is also possible to manually control quality swith using below API:


#### ```hls.levels```
return array of available quality levels

#### ```hls.currentLevel```
get : return current playback quality level

set : trigger an immediate quality level switch to new quality level. this will pause the video if it was playing, flush the whole buffer, and fetch fragment matching with current position and requested quality level. then resume the video if needed once fetched fragment will have been buffered.
set to -1 for automatic level selection

#### ```hls.nextLevel```
get : return next playback quality level (playback quality level for next buffered fragment). return -1 if next fragment not buffered yet.

set : trigger a quality level switch for next fragment. this could eventually flush already buffered next fragment
set to -1 for automatic level selection

#### ```hls.loadLevel```
get : return last loaded fragment quality level.

set : set quality level for next loaded fragment
set to -1 for automatic level selection

#### ```hls.nextLoadLevel```
get : return quality level that will be used to load next fragment

set : force quality level for next loaded fragment. quality level will be forced only for that fragment.
after a fragment at this quality level has been loaded, ```hls.loadLevel``` will prevail.

#### ```hls.firstLevel```

get :  first level index (index of first level appearing in Manifest. it is usually defined as start level hint for player)

#### ```hls.startLevel```

get/set :  start level index (level of first fragment that will be played back)

  - if not overrided by user : first level appearing in manifest will be used as start level.
  -  if -1 : automatic start level selection, playback will start from level matching download bandwidth (determined from download of first segment)

default value is firstLevel

#### ```hls.autoLevelEnabled```

tell whether auto level selection is enabled or not

#### ```hls.autoLevelCapping```
get/set : capping/max level value that could be used by ABR Controller

default value is -1 (no level capping)



## Network Loading Control API

by default, hls.js will automatically start loading quality level playlists, and fragments after Events.MANIFEST_PARSED event has been triggered (and video element has been attached).

however if ```config.autoStartLoad``` is set to ```false```, the following method needs to be called to manually start playlist and fragments loading:

#### ```hls.startLoad(startPosition=-1)```
start/restart playlist/fragment loading. this is only effective if MANIFEST_PARSED event has been triggered and video element has been attached to hls object.

startPosition is the initial position in the playlist.
if startPosition is not set to -1, it allows to override default startPosition to the one you want (it will bypass hls.config.liveSync* config params for Live for example, so that user can start playback from whatever position)

#### ```hls.stopLoad()```
stop playlist/fragment loading. could be resumed later on by calling ```hls.startLoad()```

## Runtime Events

hls.js fires a bunch of events, that could be registered as below:


```js
hls.on(Hls.Events.LEVEL_LOADED,function(event,data) {
  var level_duration = data.details.totalduration;
});
```
full list of Events available below :

  - `Hls.Events.MEDIA_ATTACHING`  - fired to attach Media to hls instance.
    -  data: { video , mediaSource }
  - `Hls.Events.MEDIA_ATTACHED`  - fired when Media has been succesfully attached to hls instance
    -  data: { video , mediaSource }
  - `Hls.Events.MEDIA_DETACHING`  - fired before detaching Media from hls instance
    -  data: { }
  - `Hls.Events.MEDIA_DETACHED`  - fired when Media has been detached from hls instance
    -  data: { }
  - `Hls.Events.MANIFEST_LOADING`  - fired to signal that a manifest loading starts
    -  data: { url : manifestURL}
  - `Hls.Events.MANIFEST_LOADED`  - fired after manifest has been loaded
    -  data: { levels : [available quality levels] , url : manifestURL, stats : { trequest, tfirst, tload, mtime}}
  - `Hls.Events.MANIFEST_PARSED`  - fired after manifest has been parsed
    -  data: { levels : [available quality levels] , firstLevel : index of first quality level appearing in Manifest}
  - `Hls.Events.LEVEL_LOADING`  - fired when a level playlist loading starts
    -  data: { url : level URL, level : id of level being loaded}
  - `Hls.Events.LEVEL_LOADED`  - fired when a level playlist loading finishes
    -  data: { details : levelDetails object, levelId : id of loaded level, stats : { trequest, tfirst, tload, mtime} }
  - `Hls.Events.LEVEL_UPDATED`  - fired when a level's details have been updated based on previous details, after it has been loaded
    -  data: { details : levelDetails object, level : id of updated level }
  - `Hls.Events.LEVEL_PTS_UPDATED`  - fired when a level's PTS information has been updated after parsing a fragment
    -  data: { details : levelDetails object, level : id of updated level, drift: PTS drift observed when parsing last fragment }
  - `Hls.Events.LEVEL_SWITCH`  - fired when a level switch is requested
    -  data: { level : id of new level, it is the index of the array `Hls.levels` }
  - `Hls.Events.KEY_LOADING`  - fired when a decryption key loading starts
    -  data: { frag : fragment object}
  - `Hls.Events.KEY_LOADED`  - fired when a decryption key loading is completed
    -  data: { frag : fragment object}
  - `Hls.Events.FRAG_LOADING`  - fired when a fragment loading starts
    -  data: { frag : fragment object}
  - `Hls.Events.FRAG_LOAD_PROGRESS`  - fired when a fragment load is in progress
    - data: { frag : fragment object with frag.loaded=stats.loaded, stats : { trequest, tfirst, loaded} }
  - `Hls.Events.FRAG_LOADED`  - fired when a fragment loading is completed
    -  data: { frag : fragment object, payload : fragment payload, stats : { trequest, tfirst, tload, length}}
  - `Hls.Events.FRAG_PARSING_INIT_SEGMENT` - fired when Init Segment has been extracted from fragment
    -  data: { moov : moov MP4 box, codecs : codecs found while parsing fragment}
  - `Hls.Events.FRAG_PARSING_METADATA`  - fired when parsing id3 is completed
      -  data: { samples : [ id3 pes - pts and dts timestamp are relative, values are in seconds]}
  - `Hls.Events.FRAG_PARSING_DATA`  - fired when moof/mdat have been extracted from fragment
    -  data: { moof : moof MP4 box, mdat : mdat MP4 box, startPTS : PTS of first sample, endPTS : PTS of last sample, startDTS : DTS of first sample, endDTS : DTS of last sample, type : stream type (audio or video), nb : number of samples}
  - `Hls.Events.FRAG_PARSED`  - fired when fragment parsing is completed
    -  data: undefined
  - `Hls.Events.FRAG_BUFFERED`  - fired when fragment remuxed MP4 boxes have all been appended into SourceBuffer
    -  data: { frag : fragment object, stats : { trequest, tfirst, tload, tparsed, tbuffered, length} }
  - `Hls.Events.FRAG_CHANGED`  - fired when fragment matching with current video position is changing
    -  data: { frag : fragment object }
  - `Hls.Events.FPS_DROP` - triggered when FPS drop in last monitoring period is higher than given threshold
    -  data: {curentDropped : nb of dropped frames in last monitoring period, currentDecoded: nb of decoded frames in last monitoring period, totalDropped : total dropped frames on this video element}
  - `Hls.Events.ERROR` -  Identifier for an error event
    - data: { type : error Type, details : error details, fatal : is error fatal or not, other error specific data}
  - `Hls.Events.DESTROYING` -  fired when hls.js instance starts destroying. Different from MEDIA_DETACHED as one could want to detach and reattach a video to the instance of hls.js to handle mid-rolls for example.
    - data: { }


##Errors

full list of Errors is described below:


### Network Errors
  - ```Hls.ErrorDetails.MANIFEST_LOAD_ERROR``` - raised when manifest loading fails because of a network error
    - data: { type : ```NETWORK_ERROR```, details : ```Hls.ErrorDetails.MANIFEST_LOAD_ERROR```, fatal : ```true```,url : manifest URL, response : xhr response, loader : URL loader}
  - ```Hls.ErrorDetails.MANIFEST_LOAD_TIMEOUT``` - raised when manifest loading fails because of a timeout
    - data: { type : ```NETWORK_ERROR```, details : ```Hls.ErrorDetails.MANIFEST_LOAD_TIMEOUT```, fatal : ```true```,url : manifest URL, loader : URL loader}
  - ```Hls.ErrorDetails.MANIFEST_PARSING_ERROR``` - raised when manifest parsing failed to find proper content
    - data: { type : ```NETWORK_ERROR```, details : ```Hls.ErrorDetails.MANIFEST_PARSING_ERROR```, fatal : ```true```,url : manifest URL, reason : parsing error reason}
  - ```Hls.ErrorDetails.LEVEL_LOAD_ERROR```raised when level loading fails because of a network error
    - data: { type : ```NETWORK_ERROR```, details : ```Hls.ErrorDetails.LEVEL_LOAD_ERROR```, fatal : ```true```,url : level URL, response : xhr response, loader : URL loader}
  - ```Hls.ErrorDetails.LEVEL_LOAD_TIMEOUT```raised when level loading fails because of a timeout
    - data: { type : ```NETWORK_ERROR```, details : ```Hls.ErrorDetails.LEVEL_LOAD_TIMEOUT```, fatal : ```true```,url : level URL, loader : URL loader}
  - ```Hls.ErrorDetails.LEVEL_SWITCH_ERROR```raised when level switching fails
    - data: { type : ```OTHER_ERROR```, details : ```Hls.ErrorDetails.LEVEL_SWITCH_ERROR```, fatal : ```false```,level : failed level index, reason : failure reason}
  - ```Hls.ErrorDetails.FRAG_LOAD_ERROR```raised when fragment loading fails because of a network error
    - data: { type : ```NETWORK_ERROR```, details : ```Hls.ErrorDetails.FRAG_LOAD_ERROR```, fatal : ```true/false```,frag : fragment object, response : xhr response}
  - ```Hls.ErrorDetails.FRAG_LOOP_LOADING_ERROR```raised upon detection of same fragment being requested in loop
    - data: { type : ```NETWORK_ERROR```, details : ```Hls.ErrorDetails.FRAG_LOOP_LOADING_ERROR```, fatal : ```true/false```,frag : fragment object}
  - ```Hls.ErrorDetails.FRAG_LOAD_TIMEOUT```raised when fragment loading fails because of a timeout
    - data: { type : ```NETWORK_ERROR```, details : ```Hls.ErrorDetails.FRAG_LOAD_TIMEOUT```, fatal : ```true/false```,frag : fragment object}
  - ```Hls.ErrorDetails.FRAG_PARSING_ERROR```raised when fragment parsing fails
    - data: { type : ```NETWORK_ERROR```, details : ```Hls.ErrorDetails.FRAG_PARSING_ERROR```, fatal : ```true/false```, reason : failure reason}


### Media Errors
  - ```Hls.ErrorDetails.MANIFEST_INCOMPATIBLE_CODECS_ERROR```raised when manifest only contains quality level with codecs incompatible with MediaSource Engine.
    - data: { type : ```MEDIA_ERROR```, details : ```Hls.ErrorDetails.MANIFEST_INCOMPATIBLE_CODECS_ERROR```, fatal : ```true```, url : manifest URL}
  - ```Hls.ErrorDetails.BUFFER_APPEND_ERROR```raised when exception is raised while calling buffer append
    - data: { type : ```MEDIA_ERROR```, details : ```Hls.ErrorDetails.BUFFER_APPEND_ERROR```, fatal : ```true```, frag : fragment object}
  - ```Hls.ErrorDetails.BUFFER_APPENDING_ERROR```raised when exception is raised during buffer appending
    - data: { type : ```MEDIA_ERROR```, details : ```Hls.ErrorDetails.BUFFER_APPENDING_ERROR```, fatal : ```false```}
  - ```Hls.ErrorDetails.BUFFER_STALLED_ERROR```raised when playback is stuck because buffer is running out of data
    - data: { type : ```MEDIA_ERROR```, details : ```Hls.ErrorDetails.BUFFER_STALLED_ERROR```, fatal : ```false```}
  - ```Hls.ErrorDetails.BUFFER_FULL_ERROR```raised when no data can be appended anymore in media buffer because it is full. this error is recovered automatically by performing a smooth level switching that empty buffers (without disrupting the playback) and reducing the max buffer length.
    - data: { type : ```MEDIA_ERROR```, details : ```Hls.ErrorDetails.BUFFER_FULL_ERROR```, fatal : ```false```}
  - ```Hls.ErrorDetails.BUFFER_SEEK_OVER_HOLE```raised after hls.js seeks over a buffer hole to unstuck the playback, 
    - data: { type : ```MEDIA_ERROR```, details : ```Hls.ErrorDetails.BUFFER_SEEK_OVER_HOLE```, fatal : ```false```, hole : hole duration}

## Objects
### Level

a level object represents a given quality level.
it contains quality level related info, retrieved from manifest, such as

* level bitrate
* used codecs
* video width/height
* level name
* level URL

see sample Level object below:

```js
{
  url: ['http://levelURL.com','http://levelURLfailover.com']
  bitrate: 246440,
  name: "240",
  codecs: "mp4a.40.5,avc1.42000d",
  width: 320,
  height: 136,
}
```
url is an array, that might contains several items if failover/redundant streams are found in the manifest.

### Level details

level detailed infos contains level details retrieved after level playlist parsing, they are specified below :

* protocol version
* playlist type
* start sequence number
* end sequence number
* level total duration
* level fragment target duration
* array of fragments info
* is this level a live playlist or not ?

see sample object below, available after corresponding LEVEL_LOADED event has been fired:

```js
{
  version: 3,
  type: 'VOD', // null if EXT-X-PLAYLIST-TYPE not present
  startSN: 0,
  endSN: 50,
  totalduration: 510,
  targetduration: 10,
  fragments: Array[51],
  live: false
}
```

### Fragment
the Fragment object contains fragment related info, such as

* fragment URL
* fragment duration
* fragment sequence number
* fragment start offset
* level Id

see sample object below:

```js
{
  duration: 10,
  level : 3,
  sn: 35,
  start : 30,
  url: 'http://fragURL.com'
}
```
