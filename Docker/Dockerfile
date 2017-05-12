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

# Set user nobody to uid and gid of unRAID, uncomment for unRAID
RUN usermod -u 99 nobody
RUN usermod -g 100 nobody

# Update ubuntu
RUN apt-mark hold initscripts udev plymouth mountall
RUN apt-get -q update
RUN apt-get dist-upgrade -qy && apt-get -q update

# Install MediaBrowser run dependencies
RUN apt-get install -qy --force-yes libmono-cil-dev Libgdiplus mediainfo wget libwebp-dev

# Download latest release
RUN wget https://github.com/MediaBrowser/MediaBrowser.Releases/raw/master/Server/mediabrowser.deb && dpkg -i mediabrowser.deb && apt-get install -f

# Uncomment for unRAID
RUN chown -R nobody:users /opt/mediabrowser

# Cleanup
RUN apt-get -y autoremove && rm mediabrowser.deb
RUN mkdir /config && chown -R nobody:users /config

VOLUME /config 

ADD ./start.sh /start.sh
RUN chmod u+x  /start.sh

# Default MB3 HTTP/tcp server port
EXPOSE 8096/tcp
# Default MB3 HTTPS/tcp server port
EXPOSE 8920/tcp
# UDP server port
EXPOSE 7359/udp
# ssdp port for UPnP / DLNA discovery
EXPOSE 1900/udp
# Run as default unRAID user nobody
USER nobody
ENTRYPOINT ["/start.sh"]
