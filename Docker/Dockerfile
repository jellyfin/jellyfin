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
#RUN usermod -u 99 nobody
#RUN usermod -g 100 nobody

# Update ubuntu
RUN apt-mark hold initscripts udev plymouth mountall
RUN apt-get -q update
RUN apt-get dist-upgrade -qy && apt-get -q update

# Install MediaBrowser run dependencies
RUN apt-get install -qy --force-yes libmono-cil-dev Libgdiplus unzip wget

# Download latest release from Dropbox
RUN mkdir MBServer && cd MBServer &&  wget -O MBServer.Mono.zip https://www.dropbox.com/s/07hh1g4x9xo28jb/MBServer.Mono.zip?dl=1 && unzip MBServer.Mono.zip

# Uncomment for unRAID
#RUN chown -R nobody:users /MBServer

# Cleanup
RUN apt-get -y autoremove && rm /MBServer/MBServer.Mono.zip

VOLUME /MediaBrowser.Mono.docker/ProgramData-Server 

ADD ./start.sh /start.sh
RUN chmod u+x  /start.sh

EXPOSE 8096

# uncomment for unRAID
#USER nobody
ENTRYPOINT ["/start.sh"]
