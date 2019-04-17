#!/bin/bash

NAME=jellyfin

restart_cmds=(
  "systemctl restart ${NAME}"
  "service ${NAME} restart"
  "/etc/init.d/${NAME} restart"
  "s6-svc -t /var/run/s6/services/${NAME}"
)

for restart_cmd in "${restart_cmds[@]}"; do
  cmd=$(echo "$restart_cmd" | awk '{print $1}')
  cmd_loc=$(command -v ${cmd})
  if [[ -n "$cmd_loc" ]]; then
    restart_cmd=$(echo "$restart_cmd" | sed -e "s%${cmd}%${cmd_loc}%")
    echo "sleep 2; sudo $restart_cmd > /dev/null 2>&1" | at now > /dev/null 2>&1
    exit 0
  fi
done
