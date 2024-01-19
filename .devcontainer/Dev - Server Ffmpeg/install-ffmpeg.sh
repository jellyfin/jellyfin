#!/bin/bash

wget https://repo.jellyfin.org/releases/server/ubuntu/versions/jellyfin-ffmpeg/6.0.1-1/jellyfin-ffmpeg6_6.0.1-1-jammy_amd64.deb -O ffmpeg.deb

sudo apt update
sudo apt install -f ./ffmpeg.deb -y
rm ffmpeg.deb
