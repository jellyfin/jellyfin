#!/bin/bash
UIDProvided=true

if ! id -u jellyfin > /dev/null; then
  echo "Creating jellyfin user"
  adduser --system --group --disabled-login --uid 1000 jellyfin
fi

if [ -z "${UID}" ]
then
  UIDProvided=false
fi

if [ $UIDProvided = true ]; then
  if [ -z "${GID}" ]
  then
    export GID="${UID}"
  fi
  echo "Setting jellyfin UID to ${UID} and GID to ${GID}"
  usermod -u ${UID} jellyfin
  groupmod -g ${GID} jellyfin
  for i in $(echo ${GIDLIST} | sed "s/,/ /g")
  do
    echo "add extra group $i"
    addgroup jellygroup$i --gid $i
    usermod -G jellygroup$i jellyfin
  done
fi

JellyfinCommand="dotnet /jellyfin/jellyfin.dll --datadir /config --cachedir /cache --ffmpeg /usr/local/bin/ffmpeg"

chown -R jellyfin:jellyfin /config
chown -R jellyfin:jellyfin /cache
su -s /bin/bash -c "$JellyfinCommand" jellyfin
exit $?
