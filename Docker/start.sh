#! /bin/sh

# Start MediaBrowser Server
cd /opt/mediabrowser

mono MediaBrowser.Server.Mono.exe -programdata /config
