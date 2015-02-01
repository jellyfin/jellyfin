#docker MBServer

## Description:

This is a Dockerfile for "MediaBrowser Server" - (http://mediabrowser.tv/)

## Build from docker file:

```
git clone --depth=1 https://github.com/MediaBrowser/MediaBrowser.git 
cd MediaBrowser/Docker
docker build --rm=true -t mbserver . 
```

## Volumes:

#### `/config`

Configuration files and state of MediaBrowser Server folder. (i.e. /opt/appdata/mediabrowser)

## Docker run command:

```
docker run -d --net=host -v /*your_config_location*:/config -v /*your_media_location*:/media -v /etc/localtime:/etc/localtime:ro --name=mbserver mediabrowser/mbserver

```
