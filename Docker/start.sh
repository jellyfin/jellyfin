#! /bin/sh

# Start MediaBrowser Server
cd /opt/mediabrowser
sed -i -e 's%<dllmap dll="libwebp" target="libwebp.so" os="linux" cpu="!x86,!x86_64"/>%%' /opt/mediabrowser/MediaBrowser.Server.Mono/Imazen.WebP.dll.config
sed -i -e 's%<dllmap dll="libwebp" target="./libwebp/linux/lib64/libwebp.so" os="linux" wordsize="64"/>%%' /opt/mediabrowser/MediaBrowser.Server.Mono/Imazen.WebP.dll.config
sed -i -e 's%<dllmap dll="libwebp" target="./libwebp/linux/lib/libwebp.so" os="linux" wordsize="32"/>%<dllmap dll="libwebp" target="libwebp.so.5" os="linux"/>%' /opt/mediabrowser/MediaBrowser.Server.Mono/Imazen.WebP.dll.config

mono MediaBrowser.Server.Mono.exe -programdata /config
