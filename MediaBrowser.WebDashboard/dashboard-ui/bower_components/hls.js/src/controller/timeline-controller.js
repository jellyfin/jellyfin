/*
 * Timeline Controller
*/

import Event from '../events';
import EventHandler from '../event-handler';
import CEA708Interpreter from '../utils/cea-708-interpreter';

class TimelineController extends EventHandler {

  constructor(hls) {
    super(hls, Event.MEDIA_ATTACHING,
                Event.MEDIA_DETACHING,
                Event.FRAG_PARSING_USERDATA,
                Event.MANIFEST_LOADING,
                Event.FRAG_LOADED);

    this.hls = hls;
    this.config = hls.config;

    if (this.config.enableCEA708Captions)
    {
      this.cea708Interpreter = new CEA708Interpreter();
    }
  }

  destroy() {
    EventHandler.prototype.destroy.call(this);
  }

  onMediaAttaching(data) {
    var media = this.media = data.media;
    this.cea708Interpreter.attach(media);
  }

  onMediaDetaching() {
    this.cea708Interpreter.detach();
  }

  onManifestLoading()
  {
    this.lastPts = Number.POSITIVE_INFINITY;
  }

  onFragLoaded(data)
  {
    var pts = data.frag.start; //Number.POSITIVE_INFINITY;

    // if this is a frag for a previously loaded timerange, remove all captions
    // TODO: consider just removing captions for the timerange
    if (pts <= this.lastPts)
    {
      this.cea708Interpreter.clear();
    }

    this.lastPts = pts;
  }

  onFragParsingUserdata(data) {
    // push all of the CEA-708 messages into the interpreter
    // immediately. It will create the proper timestamps based on our PTS value
    for (var i=0; i<data.samples.length; i++)
    {
      this.cea708Interpreter.push(data.samples[i].pts, data.samples[i].bytes);
    }
  }
}

export default TimelineController;
