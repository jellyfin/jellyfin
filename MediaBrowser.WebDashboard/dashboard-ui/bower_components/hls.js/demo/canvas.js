
  var eventLeftMargin = 180;
  var eventRightMargin = 0;

  function canvasLoadEventUpdate(canvas, minTime, maxTime, events) {
    var ctx = canvas.getContext('2d');
    for (var i =0, y_offset = 20; i < events.length; i++) {
      var event = events[i], start = event.time, end = event.time + event.duration + event.latency;
      if((start >= minTime && start <= maxTime)) {
        y_offset+=20;
      }
    }
    canvas.height = y_offset;

    ctx.fillStyle = "green";
    ctx.globalAlpha = 0.5;
    ctx.fillRect(0,0,eventLeftMargin, canvas.height);
    ctx.fillRect(canvas.width-eventRightMargin,0,eventRightMargin, canvas.height);
    ctx.globalAlpha = 1;

    //draw legend
    var x_offset = 5;
    ctx.font = "12px Arial";

    legend = "load event";
    ctx.fillStyle = "black";
    ctx.fillText(legend,x_offset,15);
    x_offset = eventLeftMargin+5;


    legend = 'start - end';
    ctx.fillStyle = "black";
    ctx.fillText(legend,x_offset,15);
    x_offset += ctx.measureText(legend).width+5;

    legend = '[latency';
    ctx.fillStyle = "orange";
    ctx.fillText(legend,x_offset,15);
    x_offset += ctx.measureText(legend).width+5;

    legend = 'loading';
    ctx.fillStyle = "green";
    ctx.fillText(legend,x_offset,15);
    x_offset += ctx.measureText(legend).width+5;

    legend = 'parsing';
    ctx.fillStyle = "blue";
    ctx.fillText(legend,x_offset,15);
    x_offset += ctx.measureText(legend).width+5;

    legend = 'appending]';
    ctx.fillStyle = "red";
    ctx.fillText(legend,x_offset,15);
    x_offset += ctx.measureText(legend).width+5;

    legend = 'size bitrate';
    ctx.fillStyle = "black";
    ctx.fillText(legend,x_offset,15);
    x_offset += ctx.measureText(legend).width+5;

    for (i =0, y_offset = 20; i < events.length; i++) {
      var event = events[i], start = Math.round(event.time), end = Math.round(event.time + event.duration + event.latency);
      if((start >= minTime && start <= maxTime)) {
        canvasDrawLoadEvent(ctx,y_offset,event,minTime,maxTime);
        y_offset+=20;
      }
    }
  }

  function canvasVideoEventUpdate(canvas, minTime, maxTime, events) {
    var ctx = canvas.getContext('2d');
    for (var i =0, y_offset = 20; i < events.length; i++) {
      var event = events[i], start = event.time, end = event.time;
      if((start >= minTime && start <= maxTime)) {
        y_offset+=20;
      }
    }
    canvas.height = y_offset;
    ctx.fillStyle = "green";
    ctx.globalAlpha = 0.5;
    ctx.fillRect(0,0,eventLeftMargin, canvas.height);
    ctx.fillRect(canvas.width-eventRightMargin,0,eventRightMargin, canvas.height);
    ctx.globalAlpha = 1;

    //draw legend
    var x_offset = 5;
    ctx.font = "12px Arial";

    legend = 'video event';
    ctx.fillStyle = "black";
    ctx.fillText(legend,x_offset,15);

    x_offset = eventLeftMargin+5;
    legend = 'time';
    ctx.fillStyle = "black";
    ctx.fillText(legend,x_offset,15);

    x_offset += ctx.measureText(legend).width+5;
    legend = '[duration]';
    ctx.fillStyle = "blue";
    ctx.fillText(legend,x_offset,15);

    for (i =0, y_offset = 20; i < events.length; i++) {
      var event = events[i], start = Math.round(event.time), end = Math.round(event.time);
      if((start >= minTime && start <= maxTime)) {
        canvasDrawVideoEvent(ctx,y_offset,event,minTime,maxTime);
        y_offset+=20;
      }
    }
  }

  function canvasBufferWindowUpdate(canvas, minTime, maxTime, focusTime, events) {
    var ctx = canvas.getContext('2d'),
    minTimeBuffer, minTimePos,focusTimeBuffer,focusTimePos,
    bufferChartStart = eventLeftMargin,
    bufferChartWidth = ctx.canvas.width-eventLeftMargin-eventRightMargin;
    ctx.clearRect (0,0,canvas.width, canvas.height);

    if(events.length === 0) {
      return;
    }

    ctx.fillStyle = "green";
    ctx.globalAlpha = 0.5;
    ctx.fillRect(0,0,eventLeftMargin, canvas.height);
    ctx.globalAlpha = 1;

    //draw legend
    var x_offset = 5;
    var y_offset = 0;
    ctx.font = "15px Arial";

    var maxBuffer = 0, firstEventIdx = -1, focusEventIdx= -1, event;
    for (var i =0 ; i < events.length; i++) {
      event = events[i];
      maxBuffer = Math.max(maxBuffer, event.buffer+event.pos);
      if(firstEventIdx === -1 && event.time >= minTime) {
        firstEventIdx = Math.max(0,i-1);
      }
      if(focusEventIdx === -1 && event.time >= focusTime) {
        focusEventIdx = Math.max(0,i-1);
      }
    }
    // compute position and buffer length at pos minTime using linear approximation
    if((firstEventIdx+1) < events.length) {
      minTimePos = events[firstEventIdx].pos + (minTime-events[firstEventIdx].time)*(events[firstEventIdx+1].pos-events[firstEventIdx].pos)/(events[firstEventIdx+1].time-events[firstEventIdx].time);
      minTimeBuffer = minTimePos + events[firstEventIdx].buffer + (minTime-events[firstEventIdx].time)*(events[firstEventIdx+1].buffer-events[firstEventIdx].buffer)/(events[firstEventIdx+1].time-events[firstEventIdx].time);
    } else {
      minTimeBuffer = 0;
      minTimePos = 0;
    }

    // compute position and buffer length at pos focusTime using linear approximation
    if((focusEventIdx+1) < events.length) {
      focusTimePos = events[focusEventIdx].pos + (focusTime-events[focusEventIdx].time)*(events[focusEventIdx+1].pos-events[focusEventIdx].pos)/(events[focusEventIdx+1].time-events[focusEventIdx].time);
      focusTimeBuffer = events[focusEventIdx].buffer + (focusTime-events[focusEventIdx].time)*(events[focusEventIdx+1].buffer-events[focusEventIdx].buffer)/(events[focusEventIdx+1].time-events[focusEventIdx].time);
    } else {
      focusTimePos = 0;
      focusTimeBuffer = 0;
    }

    maxBuffer*=1.1;

    y_offset += 15;
    legend = 'play pos/buffer zoomed';
    ctx.fillStyle = "black";
    ctx.fillText(legend,x_offset,y_offset);

    y_offset += 15;
    legend = '[' + minTime + ',' + maxTime + ']';
    ctx.fillText(legend,x_offset,y_offset);

    y_offset += 15;
    legend = 'focus time:' + focusTime + ' ms';
    ctx.fillText(legend,x_offset,y_offset);

    y_offset += 15;
    legend = 'focus position:' + Math.round(focusTimePos) + ' ms';
    ctx.fillText(legend,x_offset,y_offset);

    y_offset += 15;
    legend = 'focus buffer:' + Math.round(focusTimeBuffer) + ' ms';
    ctx.fillText(legend,x_offset,y_offset);

    ctx.fillStyle = "blue";
    ctx.beginPath();
    ctx.moveTo(bufferChartStart, ctx.canvas.height);
    ctx.lineTo(bufferChartStart, ctx.canvas.height*(1 - minTimeBuffer/maxBuffer));
    for (var i =firstEventIdx+1 ; i < events.length; i++) {
      event = events[i];
      x_offset = bufferChartStart + (bufferChartWidth*(event.time-minTime))/(maxTime-minTime);
      y_offset = ctx.canvas.height*(1 - (event.buffer+event.pos)/maxBuffer);
      ctx.lineTo(x_offset,y_offset);
    }
    ctx.lineTo(x_offset, canvas.height);
    ctx.fill();

    ctx.fillStyle = "brown";
    ctx.beginPath();
    ctx.moveTo(bufferChartStart, ctx.canvas.height);
    ctx.lineTo(bufferChartStart, ctx.canvas.height*(1 - minTimePos/maxBuffer));
    for (var i =firstEventIdx+1 ; i < events.length; i++) {
      event = events[i];
      x_offset = bufferChartStart + (bufferChartWidth*(event.time-minTime))/(maxTime-minTime);
      y_offset = ctx.canvas.height*(1 - (event.pos)/maxBuffer);
      ctx.lineTo(x_offset,y_offset);
    }
    ctx.lineTo(x_offset, canvas.height);
    ctx.fill();

    ctx.fillStyle = "white";
    ctx.fillRect(canvas.width-eventRightMargin,0,eventRightMargin, canvas.height);
    ctx.fillStyle = "green";
    ctx.globalAlpha = 0.5;
    ctx.fillRect(canvas.width-eventRightMargin,0,eventRightMargin, canvas.height);
    ctx.globalAlpha = 1;

    ctx.fillStyle = "black";
    x_offset = bufferChartStart + (bufferChartWidth*(focusTime-minTime))/(maxTime-minTime);
    ctx.moveTo(x_offset, ctx.canvas.height);
    y_offset = ctx.canvas.height*(1 - (focusTimePos+focusTimeBuffer)/maxBuffer);
    ctx.lineTo(x_offset,y_offset);
    ctx.stroke();
  }

  function canvasBufferTimeRangeUpdate(canvas, minTime, maxTime, windowMinTime, windowMaxTime, events) {
    var ctx = canvas.getContext('2d'),
    bufferChartStart = eventLeftMargin,
    bufferChartWidth = ctx.canvas.width-eventLeftMargin-eventRightMargin,
    x_offset = 0,y_offset = 0,
    event;
    ctx.clearRect (0,0,canvas.width, canvas.height);

    ctx.fillStyle = "green";
    ctx.globalAlpha = 0.5;
    ctx.fillRect(0,0,eventLeftMargin, canvas.height);
    ctx.fillRect(canvas.width-eventRightMargin,0,eventRightMargin, canvas.height);
    ctx.globalAlpha = 1;

    x_offset = 5;
    y_offset = 15;
    legend = 'play pos/buffer';
    ctx.fillStyle = "black";
    ctx.font = "15px Arial";
    ctx.fillText(legend,x_offset,y_offset);

    if(events.length === 0) {
      return;
    }

    var maxBuffer = 0;
    for (var i =0 ; i < events.length; i++) {
      maxBuffer = Math.max(maxBuffer, events[i].buffer + events[i].pos);
    }

    y_offset+=15;
    legend = 'last pos:' + events[events.length-1].pos  + ' ms';
    ctx.fillText(legend,x_offset,y_offset);

    y_offset+=15;
    legend = 'last buffer:' + events[events.length-1].buffer  + ' ms';
    ctx.fillText(legend,x_offset,y_offset);

    y_offset+=15;
    legend = 'max buffer:' + maxBuffer  + ' ms';
    ctx.fillText(legend,x_offset,y_offset);

    y_offset += 15;
    legend = 'nb samples:' + events.length;
    ctx.fillText(legend,x_offset,y_offset);

    maxBuffer*=1.1;

    ctx.fillStyle = "blue";
    ctx.beginPath();
    ctx.moveTo(bufferChartStart, ctx.canvas.height);
    for (var i =0 ; i < events.length; i++) {
      event = events[i];
      x_offset = bufferChartStart + (bufferChartWidth*(event.time-minTime))/(maxTime-minTime);
      y_offset = ctx.canvas.height*(1 - (event.buffer+event.pos)/maxBuffer);
      ctx.lineTo(x_offset,y_offset);
    }
    ctx.lineTo(x_offset, canvas.height);
    ctx.fill();

    ctx.fillStyle = "brown";
    ctx.beginPath();
    ctx.moveTo(bufferChartStart, ctx.canvas.height);
    for (var i =0 ; i < events.length; i++) {
      event = events[i];
      x_offset = bufferChartStart + (bufferChartWidth*(event.time-minTime))/(maxTime-minTime);
      y_offset = ctx.canvas.height*(1 - event.pos/maxBuffer);
      ctx.lineTo(x_offset,y_offset);
    }
    ctx.lineTo(x_offset, canvas.height);
    ctx.fill();

    ctx.globalAlpha = 0.7;
    ctx.fillStyle = "grey";
    var x_start = bufferChartStart;
    var x_w = bufferChartWidth*(windowMinTime-minTime)/(maxTime-minTime);
    ctx.fillRect(x_start,0,x_w, canvas.height);
    var x_start = bufferChartStart+bufferChartWidth*(windowMaxTime-minTime)/(maxTime-minTime);
    var x_w = canvas.width-x_start-eventRightMargin;
    ctx.fillRect(x_start,0,x_w, canvas.height);
    ctx.globalAlpha = 1;
  }

  function canvasBitrateEventUpdate(canvas, minTime, maxTime, windowMinTime, windowMaxTime, levelEvents, bitrateEvents) {
    var ctx = canvas.getContext('2d'),
    bufferChartStart = eventLeftMargin,
    bufferChartWidth = ctx.canvas.width-eventLeftMargin-eventRightMargin,
    x_offset = 0,y_offset = 0,
    event, maxLevel, minLevel, sumLevel, maxBitrate, minBitrate, sumDuration;
    ctx.clearRect (0,0,canvas.width, canvas.height);

    if(levelEvents.length === 0) {
      return;
    }

    maxBitrate = minBitrate = bitrateEvents[0].bitrate;
    sumLevel = sumDuration = 0;
    for (var i =0 ; i < bitrateEvents.length; i++) {
      sumLevel += bitrateEvents[i].duration*bitrateEvents[i].level;
      sumDuration += bitrateEvents[i].duration;
      maxBitrate = Math.max(maxBitrate, bitrateEvents[i].bitrate);
      minBitrate = Math.min(minBitrate, bitrateEvents[i].bitrate);
    }

    maxLevel = minLevel = levelEvents[0].id;
    for (var i =0 ; i < levelEvents.length; i++) {
      maxLevel = Math.max(maxLevel, levelEvents[i].id);
      minLevel = Math.min(minLevel, levelEvents[i].id);
    }

    ctx.fillStyle = "green";
    ctx.globalAlpha = 0.5;
    ctx.fillRect(0,0,eventLeftMargin, canvas.height);
    ctx.fillRect(canvas.width-eventRightMargin,0,eventRightMargin, canvas.height);
    ctx.globalAlpha = 1;

    x_offset = 5;
    y_offset = 0;
    ctx.fillStyle = "black";
    ctx.font = "15px Arial";

    y_offset+=15;
    legend = 'last bitrate:' + (bitrateEvents[bitrateEvents.length-1].bitrate/1000).toFixed(2) + "Mb/s";
    ctx.fillText(legend,x_offset,y_offset);

    y_offset+=15;
    legend = 'min bitrate:' + (minBitrate/1000).toFixed(2)  + "Mb/s";
    ctx.fillText(legend,x_offset,y_offset);

    y_offset+=15;
    legend = 'max bitrate:' + (maxBitrate/1000).toFixed(2)  + "Mb/s";
    ctx.fillText(legend,x_offset,y_offset);

    y_offset+=15;
    legend = 'min/last/max level:' + minLevel + '/' + levelEvents[levelEvents.length-1].id + '/' + maxLevel;
    ctx.fillText(legend,x_offset,y_offset);

    y_offset += 15;
    legend = 'nb level switch:' + (levelEvents.length-1);
    ctx.fillText(legend,x_offset,y_offset);

    y_offset += 15;
    legend = 'average level:' + (sumLevel/sumDuration).toFixed(2);
    ctx.fillText(legend,x_offset,y_offset);

    maxBitrate*=1.1;

    ctx.strokeStyle = "blue";
    ctx.beginPath();
    ctx.moveTo(bufferChartStart, ctx.canvas.height);
    for (var i =0 ; i < bitrateEvents.length; i++) {
      event = bitrateEvents[i];
      x_offset = bufferChartStart + (bufferChartWidth*(event.time-minTime))/(maxTime-minTime);
      y_offset = ctx.canvas.height*(1 - event.bitrate/maxBitrate);
      ctx.lineTo(x_offset,y_offset);
    }
    ctx.lineTo(bufferChartStart+bufferChartWidth, y_offset);
    ctx.stroke();

    ctx.strokeStyle = "black";
    ctx.beginPath();
    x_offset = bufferChartStart;
    y_offset =  ctx.canvas.height;
    ctx.moveTo(x_offset, y_offset);
    for (var i =0 ; i < levelEvents.length; i++) {
      event = levelEvents[i];
      x_offset = bufferChartStart + (bufferChartWidth*(event.time-minTime))/(maxTime-minTime);
      ctx.lineTo(x_offset,y_offset);
      y_offset = ctx.canvas.height*(1 - event.bitrate/maxBitrate);
      ctx.lineTo(x_offset,y_offset);
    }
    ctx.lineTo(bufferChartStart+bufferChartWidth, y_offset);
    ctx.stroke();

    ctx.globalAlpha = 0.7;
    ctx.fillStyle = "grey";
    var x_start = bufferChartStart;
    var x_w = bufferChartWidth*(windowMinTime-minTime)/(maxTime-minTime);
    ctx.fillRect(x_start,0,x_w, canvas.height);
    var x_start = bufferChartStart+bufferChartWidth*(windowMaxTime-minTime)/(maxTime-minTime);
    var x_w = canvas.width-x_start-eventRightMargin;
    ctx.fillRect(x_start,0,x_w, canvas.height);
    ctx.globalAlpha = 1;

  }


  function canvasDrawLoadEvent(ctx,yoffset,event,minTime,maxTime) {
    var legend,offset,x_start,x_w,
    networkChartStart = eventLeftMargin,
    networkChartWidth = ctx.canvas.width-eventLeftMargin-eventRightMargin,
    tend = Math.round(event.time + event.duration + event.latency);

   //draw start
    ctx.fillStyle = "black";
    ctx.font = "12px Arial";
    legend = Math.round(event.time);
    offset = ctx.measureText(legend).width+5;
    x_start = networkChartStart-offset+networkChartWidth*(event.time-minTime)/(maxTime-minTime);
    ctx.fillText(legend,x_start,yoffset+12);

    //draw latency rectangle
    ctx.fillStyle = "orange";
    x_start = networkChartStart + networkChartWidth*(event.time-minTime)/(maxTime-minTime);
    x_w = networkChartWidth*event.latency/(maxTime-minTime);
    ctx.fillRect(x_start,yoffset,x_w, 15);
    //draw download rectangle
    ctx.fillStyle = "green";
    x_start = networkChartStart + networkChartWidth*(event.time+event.latency-minTime)/(maxTime-minTime);
    x_w = networkChartWidth*event.load/(maxTime-minTime);
    ctx.fillRect(x_start,yoffset,x_w, 15);

    if(event.parsing) {
      //draw parsing rectangle
      ctx.fillStyle = "blue";
      x_start = networkChartStart + networkChartWidth*(event.time+event.latency+event.load-minTime)/(maxTime-minTime);
      x_w = networkChartWidth*event.parsing/(maxTime-minTime);
      ctx.fillRect(x_start,yoffset,x_w, 15);

      if(event.buffer) {
        //draw buffering rectangle
        ctx.fillStyle = "red";
        x_start = networkChartStart + networkChartWidth*(event.time+event.latency+event.load+event.parsing-minTime)/(maxTime-minTime);
        x_w = networkChartWidth*event.buffer/(maxTime-minTime);
        ctx.fillRect(x_start,yoffset,x_w, 15);
      }
    }

   //draw end time
    ctx.fillStyle = "black";
    ctx.font = "12px Arial";
    legend = tend;
    x_start += x_w + 5;
    ctx.fillText(legend,x_start,yoffset+12);
    x_start += ctx.measureText(legend).width+5;

    legend = "[" + Math.round(event.latency);
    ctx.fillStyle = "orange";
    ctx.fillText(legend,x_start,yoffset+12);
    x_start += ctx.measureText(legend).width+5;

    legend = Math.round(event.load);
    if(!event.parsing) legend += "]";
    ctx.fillStyle = "green";
    ctx.fillText(legend,x_start,yoffset+12);
    x_start += ctx.measureText(legend).width+5;

    if(event.parsing) {
      legend = Math.round(event.parsing);
      if(!event.buffer) legend +="]";
      ctx.fillStyle = "blue";
      ctx.fillText(legend,x_start,yoffset+12);
      x_start += ctx.measureText(legend).width+5;

      if(event.buffer) {
        legend = Math.round(event.buffer) + "]";
        ctx.fillStyle = "red";
        ctx.fillText(legend,x_start,yoffset+12);
        x_start += ctx.measureText(legend).width+5;
      }
    }

    if(event.size) {
      if(event.size > 1000*1000) {
        legend = (event.size/1000000).toFixed(1) + 'MB';
      } else {
        legend = Math.round(event.size/1000) + 'kB';
      }
      ctx.fillStyle = "black";
      ctx.fillText(legend,x_start,yoffset+12);
      x_start += ctx.measureText(legend).width+5;
    }

    if(event.bw) {
      if(event.bw > 1000) {
        legend = (event.bw/1000).toFixed(1) + 'Mbps';
      } else {
        legend = event.bw + ' kbps';
      }
      ctx.fillStyle = "black";
      ctx.fillText(legend,x_start,yoffset+12);
      x_start += ctx.measureText(legend).width+5;
    }

    // draw event name
    ctx.fillStyle = "black";
    ctx.font = "15px Arial";
    legend = event.type;
    if(event.id2 !== undefined) {
      legend += ' ' + event.id2;
    }
    if(event.id !== undefined) {
      if(event.type === 'fragment') {
      legend += ' @';
      }
      legend += ' ' + event.id;
    }
    if(event.start !== undefined) {
      legend += ' [' + event.start + ',' + event.end + ']';
    }
    ctx.fillText(legend,5,yoffset+15);
  }

  function canvasDrawVideoEvent(ctx,yoffset,event,minTime,maxTime) {
    var legend,offset,x_start,x_w,
    networkChartStart = eventLeftMargin,
    networkChartWidth = ctx.canvas.width-eventLeftMargin-eventRightMargin;

    // draw event name
    ctx.fillStyle = "black";
    ctx.font = "15px Arial";
    legend = event.type;
    if (event.name) legend+= ':' + event.name;
    ctx.fillText(legend,5,yoffset+15);


   //draw start time
    ctx.fillStyle = "black";
    ctx.font = "12px Arial";
    legend = Math.round(event.time);
    offset = ctx.measureText(legend).width+5;
    x_start = networkChartStart-offset+networkChartWidth*(event.time-minTime)/(maxTime-minTime);
    ctx.fillText(legend,x_start,yoffset+12);


    //draw event rectangle
    x_start = networkChartStart + networkChartWidth*(event.time-minTime)/(maxTime-minTime);
    if(event.duration) {
      x_w = networkChartWidth*event.duration/(maxTime-minTime);
    } else {
      x_w = 1;
    }
    ctx.fillRect(x_start,yoffset,x_w, 15);

    if(event.duration) {

     //draw end time
      ctx.fillStyle = "black";
      ctx.font = "12px Arial";
      legend = Math.round(event.time+event.duration);
      x_start += x_w + 5;
      ctx.fillText(legend,x_start,yoffset+12);
      x_start += ctx.measureText(legend).width+5;

      legend = "[" + Math.round(event.duration) + "]";
      ctx.fillStyle = "blue";
      ctx.fillText(legend,x_start,yoffset+12);
    }
  }
