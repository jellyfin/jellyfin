# MediaBrowser Server
FROM ubuntu:trusty
MAINTAINER Carlos Hernandez <carlos@techbyte.ca>

# Let the container know that there is no tty
ENV DEBIAN_FRONTEND noninteractive

# Set locale to UTF-8
ENV LANGUAGE en_US.UTF-8
ENV LANG en_US.UTF-8
RUN locale-gen en_US en_US.UTF-8
RUN update-locale LANG=en_US.UTF-8
RUN dpkg-reconfigure locales

# FFMpeg PPA
RUN echo "deb http://ppa.launchpad.net/jon-severinsson/ffmpeg/ubuntu trusty main" >> /etc/apt/sources.list
RUN echo "deb-src http://ppa.launchpad.net/jon-severinsson/ffmpeg/ubuntu trusty main" >> /etc/apt/sources.list
RUN apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 1DB8ADC1CFCA9579

RUN apt-mark hold initscripts udev plymouth mountall
RUN apt-get update
RUN apt-get dist-upgrade -qy

# Install MediaBrowser Server build and run dependencies
RUN apt-get install -qy --force-yes mono-complete Libgdiplus unzip git-core wget

# Install FFMpeg
RUN apt-get install -y libjpeg62 libjpeg62-dev libopencore-amrnb0 libopencore-amrwb0 zlib1g zlib1g-dev x264 libmp3lame0
RUN apt-get install -y libav-tools
RUN apt-get install -y ffmpeg

# Build from source
RUN wget https://raw.githubusercontent.com/MediaBrowser/MediaBrowser/master/Tools/Linux_Build_Scripts/MediaBrowser.Mono.Build.sh
RUN sed -i -e s/3\.2\.7/3\.2\.8/g MediaBrowser.Mono.Build.sh
RUN sed -i -e s/MBVERSION\=.*/MBVERSION\=\"docker\"/g MediaBrowser.Mono.Build.sh
RUN chmod +x MediaBrowser.Mono.Build.sh
RUN /MediaBrowser.Mono.Build.sh

# Install
RUN mv /mediabrowser/MediaBrowser.Mono.docker.tar.gz /
RUN tar -zxvf MediaBrowser.Mono.docker.tar.gz 

# Cleanup
RUN apt-get -y autoremove
RUN rm -rf mediabrowser

VOLUME /MediaBrowser.Mono.docker/ProgramData-Server 

ADD ./start.sh /start.sh
RUN chmod u+x  /start.sh

EXPOSE 8096

ENTRYPOINT ["/start.sh"]
