#!/bin/sh

NAME=jellyfin
restart_cmd="/usr/bin/systemctl restart ${NAME}"
echo "sleep 2; sudo $restart_cmd > /dev/null 2>&1" | at now > /dev/null 2>&1
exit 0