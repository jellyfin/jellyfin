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

# This is the Right Way(tm) to check if we are booted with
# systemd, according to sd_booted(3)
if [ -d /run/systemd/system ]; then
    cmd=systemctl
else
    # Everything else is really hard to figure out, so we just use
    # service(8) if it's available - that works with most init
    # systems/distributions I know of, including FreeBSD
    if type service >/dev/null 2>&1; then
        cmd=service
    else
        # If even service(8) isn't available, we just try /etc/init.d
        # and hope for the best
        if [ -d /etc/init.d ]; then
            cmd=sysv
        else
            echo "Unable to detect a way to restart Jellyfin; bailing out" 1>&2
            echo "Please report this bug to https://github.com/jellyfin/jellyfin/issues" 1>&2
            exit 1
        fi
    fi
fi

if type sudo >/dev/null 2>&1; then
    sudo_command=sudo
else
    sudo_command=
fi

echo "Detected service control platform '$cmd'; using it to restart Jellyfin..."
case $cmd in
    'systemctl')
        # Without systemd-run here, `jellyfin.service`'s shutdown terminates this process too
        $sudo_command systemd-run systemctl restart jellyfin
        ;;
    'service')
        echo "sleep 0.5; $sudo_command service jellyfin start" | at now
        ;;
    'sysv')
        echo "sleep 0.5; /usr/bin/sudo /etc/init.d/jellyfin start" | at now 
        ;;
esac
exit 0
