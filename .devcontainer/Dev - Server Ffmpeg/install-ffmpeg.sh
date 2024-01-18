#!/bin/bash

sudo wget https://repo.jellyfin.org/releases/server/ubuntu/versions/jellyfin-ffmpeg/6.0-8/jellyfin-ffmpeg6_6.0-8-focal_amd64.deb -O ffmpeg.deb
sudo apt install -f ./ffmpeg.deb -y
rm ffmpeg.deb