#!/bin/bash
echo "Starting Jellyfin..."
exec /jellyfin/jellyfin --ffmpeg ${JELLYFIN_FFMPEG}
