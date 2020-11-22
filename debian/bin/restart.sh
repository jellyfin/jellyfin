#!/bin/bash

# restart.sh - Jellyfin server restart script
# Part of the Jellyfin project (https://github.com/jellyfin)
#
# This script restarts the Jellyfin daemon on Linux when using
# the Restart button on the admin dashboard. It supports the
# systemctl, service, and traditional /etc/init.d (sysv) restart
# methods, chosen automatically by which one is found first (in
# that order).
#
# This script is used by the Debian/Ubuntu/Fedora/CentOS packages.

get_service_command() {
    for command in systemctl service; do
        if which $command &>/dev/null; then
            echo $command && return
        fi
    done
    echo "sysv"
}

cmd="$( get_service_command )"
echo "Detected service control platform '$cmd'; using it to restart Jellyfin..."
case $cmd in
    'systemctl')
        sleep 2; /usr/bin/sudo $( which systemctl ) restart jellyfin || /usr/bin/sudo $( which systemctl ) start jellyfin
        ;;
    'service')
        sleep 2; /usr/bin/sudo $( which service ) jellyfin restart || /usr/bin/sudo $( which service ) jellyfin start
        ;;
    'sysv')
        sleep 2; /usr/bin/sudo /etc/init.d/jellyfin restart || /usr/bin/sudo /etc/init.d/jellyfin start
        ;;
esac
exit 0
