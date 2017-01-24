##design principle

design idea is pretty simple :

   - main functionalities are splitted into several subsystems
   - all subsytems are instantiated by Hls instance.
   - each subsystem heavily relies on events for internal/external communications.
   - Events are handled using [browserified](http://browserify.org/) [EventEmitter](https://nodejs.org/api/events.html)

##code structure

  - [src/errors.js][]
    - definition of Hls.ErrorTypes and Hls.ErrorDetails
  - [src/event-handler.js][]
    - helper class simplifying Hls event handling, event error catching
  - [src/events.js][]
    - definition of Hls.Events
  - [src/hls.js][]
    - definition of Hls Class. instantiate all subcomponents.
  - [src/index.js][]
    - needed for ES6 export
  - [src/controller/abr-controller.js][]
    - in charge of determining auto quality level.
    - auto quality switch algorithm is bitrate based : fragment loading bitrate is monitored and smoothed using 2 exponential weighted moving average (a fast one, to adapt quickly on bandwidth drop and a slow one, to avoid ramping up too quickly on bandwidth increase)
    - in charge of **monitoring fragment loading speed** (by monitoring data received from FRAG_LOAD_PROGRESS event)
     - "expected time of fragment load completion" is computed using "fragment loading instant bandwidth".
     - this time is compared to the "expected time of buffer starvation".
     - if we have less than 2 fragments buffered and if "expected time of fragment load completion" is bigger than "expected time of buffer starvation" and also bigger than duration needed to load fragment at next quality level (determined by auto quality switch algorithm), current fragment loading is aborted, stream-controller will **trigger an emergency switch down**.
  - [src/controller/audio-stream-controller.js][]
    - audio stream controller is in charge of filling audio buffer in case alternate audio tracks are used
    - if buffer is not filled up appropriately (i.e. as per defined maximum buffer size, it will trigger the following actions:
        - retrieve "not buffered" media position greater then current playback position. this is performed by comparing audio sourcebuffer.buffered and media.currentTime.
        - retrieve URL of fragment matching with this media position, and appropriate audio track
        - trigger KEY_LOADING event (only if fragment is encrypted)
        - trigger FRAG_LOADING event
        - **trigger fragment demuxing** on FRAG_LOADED
        - trigger BUFFER_CODECS on FRAG_PARSING_INIT_SEGMENT
        - trigger BUFFER_APPENDING on FRAG_PARSING_DATA
        - once FRAG_PARSED is received an all segments have been appended (BUFFER_APPENDED) then audio stream controller will recheck whether it needs to buffer more data.
  - [src/controller/audio-track-controller.js][]
    - audio track controller is handling alternate audio track set/get ((re)loading tracks/switching)
  - [src/controller/buffer-controller.js][]
    - in charge of:
        - resetting media buffer upon BUFFER_RESET event reception
        - initializing [SourceBuffer](http://www.w3.org/TR/media-source/#sourcebuffer) with appropriate codecs info upon BUFFER_CODECS event reception
        - appending MP4 boxes in [SourceBuffer](http://www.w3.org/TR/media-source/#sourcebuffer) upon BUFFER_APPENDING
        - trigger BUFFER_APPENDED event upon successful buffer appending
        - flushing specified buffer range upon reception of BUFFER_FLUSHING event
        - trigger BUFFER_FLUSHED event upon successful buffer flushing
  - [src/controller/cap-level-controller.js][]
    - in charge of determining best quality level to actual size (dimensions: width and height) of the player
  - [src/controller/ewma-bandwidth-estimator.js][]
    - Exponential Weighted Moving Average bandwidth estimator, heavily inspired from shaka-player
      - Tracks bandwidth samples and estimates available bandwidth, based on the minimum of two exponentially-weighted moving averages with different half-lives.
      - one fast average with a short half-life: useful to quickly react to bandwidth drop and switch rendition down quickly
      - one slow average with a long half-life: useful to slowly react to bandwidth increase and avoid switching up rendition to quickly
      - bandwidth estimate is Math.min(fast average,slow average)
      - average half-life are configurable , refer to abrEwma* config params
  - [src/controller/fps-controller.js][]
    - in charge of monitoring frame rate, and fire FPS_DROP event in case FPS drop exceeds configured threshold. disabled for now.
  - [src/controller/level-controller.js][]
    - level controller is handling quality level set/get ((re)loading stream manifest/switching levels)  
    - in charge of scheduling playlist (re)loading
    - monitor fragment/key/level loading error. handle fallback mechanism in case of errors : switch to redundant level, then level switch down till lowest rendition is reached. if `LEVEL_LOAD_ERROR`/`LEVEL_LOAD_TIMEOUT` is raised while media.currentTime is not buffered  and it is not possible to fallback (either because we are already on the lowest rendition or because we are in manual level selection), then `LEVEL_LOAD_ERROR`/`LEVEL_LOAD_TIMEOUT` will be converted into a fatal error.
    - a timer is armed to periodically refresh active live playlist.
  - [src/controller/stream-controller.js][]
    - stream controller is in charge of:
      - **ensuring that media buffer is filled as per defined quality selection logic**.
    - if buffer is not filled up appropriately (i.e. as per defined maximum buffer size, or as per defined quality level), stream controller will trigger the following actions:
        - retrieve "not buffered" media position greater then current playback position. this is performed by comparing video.buffered and video.currentTime.
          - if there are holes in video.buffered, smaller than config.maxBufferHole, they will be ignored.
        - retrieve URL of fragment matching with this media position, and appropriate quality level
        - trigger KEY_LOADING event (only if fragment is encrypted)
        - trigger FRAG_LOADING event
        - **trigger fragment demuxing** on FRAG_LOADED
        - trigger BUFFER_RESET on MANIFEST_PARSED or startLoad()
        - trigger BUFFER_CODECS on FRAG_PARSING_INIT_SEGMENT
        - trigger BUFFER_APPENDING on FRAG_PARSING_DATA
        - once FRAG_PARSED is received an all segments have been appended (BUFFER_APPENDED) then buffer controller will recheck whether it needs to buffer more data.
      - **monitor current playback quality level** (buffer controller maintains a map between media position and quality level)
      - **monitor playback progress** : if playhead is not moving for more than `config.lowBufferWatchdogPeriod` although it should (video metadata is known and video is not ended, nor paused, nor in seeking state) and if we have less than 500ms buffered upfront, and if there is a new buffer range available upfront, less than `config.maxSeekHole` from currentTime, then hls.js will **jump over the buffer hole** and seek to the beginning of this new buffered range, to "unstuck" the playback.
      500 ms is a "magic number" that has been set to overcome browsers not always stopping playback at the exact end of a buffered range.
      these holes in media buffered are often encountered on stream discontinuity or on quality level switch. holes could be "large" especially if fragments are not starting with a keyframe.
       if playhead is stuck for more than `config.highBufferWatchdogPeriod` second in a buffered area, hls.js will nudge currentTime until playback recovers (it will retry every seconds, and report a fatal error after config.maxNudgeRetry retries)
    - convert non-fatal `FRAG_LOAD_ERROR`/`FRAG_LOAD_TIMEOUT`/`KEY_LOAD_ERROR`/`KEY_LOAD_TIMEOUT` error into fatal error when media position is not buffered and max load retry has been reached 
    - stream controller actions are scheduled by a tick timer (invoked every 100ms) and actions are controlled by a state machine.
  - [src/controller/timeline-controller.js][]
    - Manages pulling CEA-708 caption data from the fragments, running them through the cea-608-parser, and handing them off to a display class, which defaults to src/utils/cues.js
  - [src/crypt/aes.js][]
    - AES 128 software decryption routine, low level class handling decryption of 128 bit of data.
  - [src/crypt/aes128-decrypter.js][]  
    - AES 128-CBC software decryption routine, high-level class handling cipher-block chaining (CBC), and that should also handle padding (TODO).
  - [src/crypt/decrypter.js][]
    - decrypter interface, use either WebCrypto API if available and enabled, or fallback on AES 128 software decryption routine.
  - [src/demux/aacdemuxer.js][]
    - AAC ES demuxer
      - extract ADTS samples from AAC ES
  - [src/demux/adts.js][]
    - ADTS header parser helper, extract audio config from ADTS header. used by AAC ES and TS demuxer.
  - [src/demux/demuxer.js][]
    - demuxer abstraction interface, that will either use a [Worker](https://en.wikipedia.org/wiki/Web_worker) to demux or demux inline depending on config/browser capabilities.
    - also handle fragment decryption using WebCrypto API (fragment decryption is performed in main thread)
    - if Worker are disabled. demuxing will be performed in the main thread.
    - if Worker are available/enabled,
      - demuxer will instantiate a Worker
      - post/listen to Worker message,
      - and redispatch events as expected by hls.js.
    - Fragments are sent as [transferable objects](https://developers.google.com/web/updates/2011/12/Transferable-Objects-Lightning-Fast) in order to minimize message passing overhead.
  - [src/demux/demuxer-inline.js][]
    - inline demuxer.
    - probe fragments and instantiate appropriate demuxer depending on content type (TSDemuxer, AACDemuxer, ...)
  - [src/demux/demuxer-worker.js][]
    - demuxer web worker.
    - listen to worker message, and trigger DemuxerInline upon reception of Fragments.
    - provides MP4 Boxes back to main thread using [transferable objects](https://developers.google.com/web/updates/2011/12/Transferable-Objects-Lightning-Fast) in order to minimize message passing overhead.
  - [src/demux/exp-golomb.js][]
    - utility class to extract Exponential-Golomb coded data. needed by TS demuxer for SPS parsing.
  - [src/demux/id3.js][]
    - utility class that detect and parse ID3 tags, used by AAC demuxer
  - [src/demux/tsdemuxer.js][]
    - highly optimized TS demuxer:
     - parse PAT, PMT
     - extract PES packet from audio and video PIDs
     - extract AVC/H264 NAL units,AAC/ADTS samples, MP3/MPEG audio samples from PES packet
     - trigger the remuxer upon parsing completion
     - it also tries to workaround as best as it can audio codec switch (HE-AAC to AAC and vice versa), without having to restart the MediaSource.
     - it also controls the remuxing process :
      - upon discontinuity or level switch detection, it will also notifies the remuxer so that it can reset its state.
  - [src/helper/aac.js][]
    - helper class to create silent AAC frames (useful to handle streams with audio holes)
  - [src/helper/buffer-helper.js][]
    - helper class, providing methods dealing buffer length retrieval (given a media position, it will return the upfront buffer length, next buffer position ...)
  - [src/helper/level-helper.js][]
    - helper class providing methods dealing with playlist sliding and fragment duration drift computation : after fragment parsing, start/end fragment timestamp will be used to adjust potential playlist drifts and live playlist sliding.
  - [src/loader/fragment-loader.js][]
    - in charge of loading fragments, use xhr-loader if not overrided by user config
  - [src/loader/key-loader.js][]
   - in charge of loading decryption key    
  - [src/loader/playlist-loader.js][]
   - in charge of loading manifest, and level playlists, use xhr-loader if not overrided by user config.
  - [src/remux/dummy-remuxer.js][]
   - example dummy remuxer
  - [src/remux/mp4-generator.js][]
   - in charge of generating MP4 boxes
     - generate Init Segment (moov)
     - generate samples Box (moof and mdat)
  - [src/remux/mp4-remuxer.js][]
   - in charge of converting AVC/AAC/MP3 samples provided by demuxer into fragmented ISO BMFF boxes, compatible with MediaSource
   - this remuxer is able to deal with small gaps between fragments and ensure timestamp continuity. it is also able to create audio padding (silent AAC audio frames) in case there is a significant audio 'hole' in the stream.
   - it notifies remuxing completion using events (```FRAG_PARSING_INIT_SEGMENT```, ```FRAG_PARSING_DATA``` and ```FRAG_PARSED```)
  - [src/utils/attr-list.js][]
    - Attribute List parsing helper class, used by playlist-loader
  - [src/utils/binary-search.js][]
    - binary search helper class
  - [src/utils/cea-608-parser.js][]
    - Port of dash.js class of the same name to ECMAScript. This class outputs "Screen" objects which contain rows of characters that can be rendered by a separate class.
  - [src/utils/cues.js][]
    - Default CC renderer. Translates Screen objects from cea-608-parser into HTML5 VTTCue objects, rendered by the video tag
  - [src/utils/ewma.js][]
    - compute [exponential weighted moving average](https://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average)
  - [src/utils/hex.js][]
    - Hex dump utils, useful for debug
  - [src/utils/logger.js][]
    - logging utils, useful for debug
  - [src/utils/polyfill.js][]
    - ArrayBuffer.slice polyfill
  - [src/utils/url.js][]
    - convert base+relative URL into absolute URL
  - [src/utils/xhr-loader.js][]
    - XmlHttpRequest wrapper. it handles standard HTTP GET but also retries and timeout.
    - retries : if xhr fails, HTTP GET will be retried after a predetermined delay. this delay is increasing following an exponential backoff. after a predetemined max number of retries, an error callback will be triggered.
    - timeout: if load exceeds max allowed duration, a timeout callback will be triggered. it is up to the callback to decides whether the connection should be cancelled or not.


[src/errors.js]: src/errors.js
[src/event-handler.js]: src/event-handler.js
[src/events.js]: src/events.js
[src/hls.js]: src/hls.js
[src/index.js]: src/index.js
[src/controller/abr-controller.js]: src/controller/abr-controller.js
[src/controller/audio-stream-controller.js]: src/controller/audio-stream-controller.js
[src/controller/audio-track-controller.js]: src/controller/audio-track-controller.js
[src/controller/buffer-controller.js]: src/controller/buffer-controller.js
[src/controller/cap-level-controller.js]: src/controller/cap-level-controller.js
[src/controller/ewma-bandwidth-estimator.js]: src/controller/ewma-bandwidth-estimator.js
[src/controller/fps-controller.js]: src/controller/fps-controller.js
[src/controller/level-controller.js]: src/controller/level-controller.js
[src/controller/stream-controller.js]: src/controller/stream-controller.js
[src/controller/timeline-controller.js]: src/controller/timeline-controller.js
[src/crypt/aes.js]: src/crypt/aes.js
[src/crypt/aes128-decrypter.js]: src/crypt/aes128-decrypter.js
[src/crypt/decrypter.js]: src/crypt/decrypter.js
[src/demux/aacdemuxer.js]: src/demux/aacdemuxer.js
[src/demux/adts.js]: src/demux/adts.js
[src/demux/demuxer.js]: src/demux/demuxer.js
[src/demux/demuxer-inline.js]: src/demux/demuxer-inline.js
[src/demux/demuxer-worker.js]: src/demux/demuxer-worker.js
[src/demux/exp-golomb.js]: src/demux/exp-golomb.js
[src/demux/id3.js]: src/demux/id3.js
[src/demux/tsdemuxer.js]: src/demux/tsdemuxer.js
[src/helper/aac.js]: src/helper/aac.js
[src/helper/buffer-helper.js]: src/helper/buffer-helper.js
[src/helper/level-helper.js]: src/helper/level-helper.js
[src/loader/fragment-loader.js]: src/loader/fragment-loader.js
[src/loader/key-loader.js]: src/loader/key-loader.js
[src/loader/playlist-loader.js]: src/loader/playlist-loader.js
[src/remux/dummy-remuxer.js]: src/remux/dummy-remuxer.js
[src/remux/mp4-generator.js]: src/remux/mp4-generator.js
[src/remux/passthrough-remuxer.js]: src/remux/passthrough-remuxer.js
[src/remux/mp4-remuxer.js]: src/remux/mp4-remuxer.js
[src/utils/attr-list.js]: src/utils/attr-list.js
[src/utils/binary-search.js]: src/utils/binary-search.js
[src/utils/cea-608-parser.js]: src/utils/cea-608-parser.js
[src/utils/cues.js]: src/utils/cues.js
[src/utils/ewma.js]: src/utils/ewma.js
[src/utils/hex.js]: src/utils/hex.js
[src/utils/logger.js]: src/utils/logger.js
[src/utils/polyfill.js]: src/utils/polyfill.js
[src/utils/url.js]: src/utils/url.js
[src/utils/xhr-loader.js]: src/utils/xhr-loader.js


## Error detection and Handling

  - ```MANIFEST_LOAD_ERROR``` is raised by [src/loader/playlist-loader.js][] upon xhr failure detected by [src/utils/xhr-loader.js][]. this error is marked as fatal only after manifestLoadingMaxRetry has been reached and will not be recovered automatically. a call to ```hls.loadSource(manifestURL)``` could help recover it.
  - ```MANIFEST_LOAD_TIMEOUT``` is raised by [src/loader/playlist-loader.js][] upon xhr timeout detected by [src/utils/xhr-loader.js][]. this error is marked as fatal and will not be recovered automatically. a call to ```hls.loadSource(manifestURL)``` could help recover it.
  - ```MANIFEST_PARSING_ERROR``` is raised by [src/loader/playlist-loader.js][] if Manifest parsing fails (no EXTM3U delimiter, no levels found in Manifest, ...)
  - ```LEVEL_LOAD_ERROR``` is raised by [src/loader/playlist-loader.js][] upon xhr failure detected by [src/utils/xhr-loader.js][]. this error is marked as fatal only after levelLoadingMaxRetry has been reached and will not be recovered automatically. a call to ```hls.startLoad()``` could help recover it.
  - ```LEVEL_LOAD_TIMEOUT``` is raised by [src/loader/playlist-loader.js][] upon xhr timeout detected by [src/utils/xhr-loader.js][]. this error is marked as fatal and will not be recovered automatically. a call to ```hls.startLoad()``` could help recover it.
  - ```LEVEL_SWITCH_ERROR``` is raised by [src/controller/level-controller.js][] if user tries to switch to an invalid level (invalid/out of range level id)
  - ```AUDIO_TRACK_LOAD_ERROR``` is raised by [src/loader/playlist-loader.js][] upon xhr failure detected by [src/utils/xhr-loader.js][]. this error is marked as fatal and will not be recovered automatically. a call to ```hls.startLoad()``` could help recover it.
  - ```AUDIO_TRACK_LOAD_TIMEOUT``` is raised by [src/loader/playlist-loader.js][] upon xhr timeout detected by [src/utils/xhr-loader.js][]. this error is marked as fatal and will not be recovered automatically. a call to ```hls.startLoad()``` could help recover it.
  - ```FRAG_LOAD_ERROR``` is raised by [src/loader/fragment-loader.js][] upon xhr failure detected by [src/utils/xhr-loader.js][].
    - if auto level switch is enabled and loaded frag level is greater than 0, or if media.currentTime is buffered, this error is not fatal: in that case [src/controller/level-controller.js][] will trigger an emergency switch down to level 0.
    - if frag level is 0 or auto level switch is disabled and media.currentTime is not buffered, this error is marked as fatal and a call to ```hls.startLoad()``` could help recover it.
  - ```FRAG_LOOP_LOADING_ERROR``` is raised by [src/controller/stream-controller.js][] upon detection of same fragment being requested in loop. this could happen with badly formatted fragments.
    - if auto level switch is enabled and loaded frag level is greater than 0, this error is not fatal: in that case [src/controller/level-controller.js][] will trigger an emergency switch down to level 0.
    - if frag level is 0 or auto level switch is disabled, this error is marked as fatal and a call to ```hls.startLoad()``` could help recover it.
  - ```FRAG_LOAD_TIMEOUT``` is raised by [src/loader/fragment-loader.js][] upon xhr timeout detected by [src/utils/xhr-loader.js][].
    - if auto level switch is enabled and loaded frag level is greater than 0, this error is not fatal: in that case [src/controller/level-controller.js][] will trigger an emergency switch down to level 0.
    - if frag level is 0 or auto level switch is disabled, this error is marked as fatal and a call to ```hls.startLoad()``` could help recover it.
  - ```FRAG_DECRYPT_ERROR``` is raised by [src/demux/demuxer.js][] upon fragment decrypting error. this error is fatal. 
  - ```FRAG_PARSING_ERROR``` is raised by [src/demux/tsdemuxer.js][] upon TS parsing error. this error is not fatal.
  - ```REMUX_ALLOC_ERROR``` is raised by [src/remux/mp4-remuxer.js][] upon memory allocation error while remuxing. this error is not fatal if in auto-mode and loaded frag level is greater than 0. in that case a level switch down will occur.
  - ```KEY_LOAD_ERROR``` is raised by [src/loader/key-loader.js][] upon xhr failure detected by [src/utils/xhr-loader.js][].
    - if auto level switch is enabled and loaded frag level is greater than 0, this error is not fatal: in that case [src/controller/level-controller.js][] will trigger an emergency switch down to level 0.
    - if frag level is 0 or auto level switch is disabled, this error is marked as fatal and a call to ```hls.startLoad()``` could help recover it.
  - ```KEY_LOAD_TIMEOUT``` is raised by [src/loader/key-loader.js][] upon xhr timeout detected by [src/utils/xhr-loader.js][].
    - if auto level switch is enabled and loaded frag level is greater than 0, this error is not fatal: in that case [src/controller/level-controller.js][] will trigger an emergency switch down to level 0.
    - if frag level is 0 or auto level switch is disabled, this error is marked as fatal and a call to ```hls.startLoad()``` could help recover it.  
  - ```BUFFER_ADD_CODEC_ERROR``` is raised by [src/controller/buffer-controller.js][] when an exception is raised when calling mediaSource.addSourceBuffer(). this error is non fatal.
  - ```BUFFER_APPEND_ERROR``` is raised by [src/controller/buffer-controller.js][] when an exception is raised when calling sourceBuffer.appendBuffer(). this error is non fatal and become fatal after config.appendErrorMaxRetry retries. when fatal, a call to ```hls.recoverMediaError()``` could help recover it.
  - ```BUFFER_APPENDING_ERROR``` is raised by [src/controller/buffer-controller.js][] after SourceBuffer appending error. this error is fatal and a call to ```hls.recoverMediaError()``` could help recover it.
  - ```BUFFER_STALLED_ERROR``` is raised by [src/controller/stream-controller.js][] if playback is stalling because of buffer underrun
  - ```BUFFER_FULL_ERROR``` is raised by [src/controller/buffer-controller.js][] if sourcebuffer is full
  - ```BUFFER_SEEK_OVER_HOLE``` is raised by [src/controller/stream-controller.js][] when hls.js seeks over a buffer hole after playback stalls
  - ```BUFFER_NUDGE_ON_STALL``` is raised by [src/controller/stream-controller.js][] when hls.js nudge currentTime (when playback is stuck for more than 1s in a buffered area)
  - ```INTERNAL_EXCEPTION``` is raised by [src/event-handler.js][] when a runtime exception is triggered by an internal Hls event handler. this error is non-fatal.
