/* demuxer web worker.
 *  - listen to worker message, and trigger DemuxerInline upon reception of Fragments.
 *  - provides MP4 Boxes back to main thread using [transferable objects](https://developers.google.com/web/updates/2011/12/Transferable-Objects-Lightning-Fast) in order to minimize message passing overhead.
 */

 import DemuxerInline from '../demux/demuxer-inline';
 import Event from '../events';
 import EventEmitter from 'events';

var DemuxerWorker = function (self) {
  // observer setup
  var observer = new EventEmitter();
  observer.trigger = function trigger (event, ...data) {
    observer.emit(event, event, ...data);
  };

  observer.off = function off (event, ...data) {
    observer.removeListener(event, ...data);
  };
  self.addEventListener('message', function (ev) {
    var data = ev.data;
    //console.log('demuxer cmd:' + data.cmd);
    switch (data.cmd) {
      case 'init':
        self.demuxer = new DemuxerInline(observer, data.typeSupported);
        break;
      case 'demux':
        self.demuxer.push(new Uint8Array(data.data), data.audioCodec, data.videoCodec, data.timeOffset, data.cc, data.level, data.sn, data.duration);
        break;
      default:
        break;
    }
  });

  // listen to events triggered by Demuxer
  observer.on(Event.FRAG_PARSING_INIT_SEGMENT, function(ev, data) {
    self.postMessage({event: ev, tracks : data.tracks, unique : data.unique });
  });

  observer.on(Event.FRAG_PARSING_DATA, function(ev, data) {
    var objData = {event: ev, type: data.type, startPTS: data.startPTS, endPTS: data.endPTS, startDTS: data.startDTS, endDTS: data.endDTS, data1: data.data1.buffer, data2: data.data2.buffer, nb: data.nb};
    // pass data1/data2 as transferable object (no copy)
    self.postMessage(objData, [objData.data1, objData.data2]);
  });

  observer.on(Event.FRAG_PARSED, function(event) {
    self.postMessage({event: event});
  });

  observer.on(Event.ERROR, function(event, data) {
    self.postMessage({event: event, data: data});
  });

  observer.on(Event.FRAG_PARSING_METADATA, function(event, data) {
    var objData = {event: event, samples: data.samples};
    self.postMessage(objData);
  });

  observer.on(Event.FRAG_PARSING_USERDATA, function(event, data) {
    var objData = {event: event, samples: data.samples};
    self.postMessage(objData);
  });

};

export default DemuxerWorker;

