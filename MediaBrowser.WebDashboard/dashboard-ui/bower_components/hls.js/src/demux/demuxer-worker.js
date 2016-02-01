/* demuxer web worker.
 *  - listen to worker message, and trigger DemuxerInline upon reception of Fragments.
 *  - provides MP4 Boxes back to main thread using [transferable objects](https://developers.google.com/web/updates/2011/12/Transferable-Objects-Lightning-Fast) in order to minimize message passing overhead.
 */

 import DemuxerInline from '../demux/demuxer-inline';
 import Event from '../events';
 import EventEmitter from 'events';
 import MP4Remuxer from '../remux/mp4-remuxer';

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
    //console.log('demuxer cmd:' + ev.data.cmd);
    switch (ev.data.cmd) {
      case 'init':
        self.demuxer = new DemuxerInline(observer,MP4Remuxer);
        break;
      case 'demux':
        var data = ev.data;
        self.demuxer.push(new Uint8Array(data.data), data.audioCodec, data.videoCodec, data.timeOffset, data.cc, data.level, data.sn, data.duration);
        break;
      default:
        break;
    }
  });

  // listen to events triggered by TS Demuxer
  observer.on(Event.FRAG_PARSING_INIT_SEGMENT, function(ev, data) {
    var objData = {event: ev};
    var objTransferable = [];
    if (data.audioCodec) {
      objData.audioCodec = data.audioCodec;
      objData.audioMoov = data.audioMoov.buffer;
      objData.audioChannelCount = data.audioChannelCount;
      objTransferable.push(objData.audioMoov);
    }
    if (data.videoCodec) {
      objData.videoCodec = data.videoCodec;
      objData.videoMoov = data.videoMoov.buffer;
      objData.videoWidth = data.videoWidth;
      objData.videoHeight = data.videoHeight;
      objTransferable.push(objData.videoMoov);
    }
    // pass moov as transferable object (no copy)
    self.postMessage(objData,objTransferable);
  });

  observer.on(Event.FRAG_PARSING_DATA, function(ev, data) {
    var objData = {event: ev, type: data.type, startPTS: data.startPTS, endPTS: data.endPTS, startDTS: data.startDTS, endDTS: data.endDTS, moof: data.moof.buffer, mdat: data.mdat.buffer, nb: data.nb};
    // pass moof/mdat data as transferable object (no copy)
    self.postMessage(objData, [objData.moof, objData.mdat]);
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

