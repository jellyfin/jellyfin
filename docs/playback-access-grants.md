# Delegated playback access

Casting devices need permission to fetch media resources without receiving the
initiating user's account credentials. Playback grants provide that delegation in
core, including when the initiating user is an administrator.

## Issuance

An authenticated account can request a grant with
`POST /Items/{itemId}/PlaybackAccess`, supplying `MediaSourceId` and `DeviceId` in
the JSON body. Core generates a playback session ID and checks the current user's
playback permission, parental schedule, item visibility, and media source. API
keys and playback credentials cannot issue grants for arbitrary users.

Trusted server integrations can call `IPlaybackAccessManager.CreateAsync` using
the user from an authorized playback command and a unique playback session ID.
Device discovery alone must not issue grants. `DeviceId` is playback accounting
metadata; it does not authenticate the physical renderer.

Set `StreamInfo.PlaybackToken` and `StreamInfo.PlaySessionId` from the grant before
building the media URL. The URL builder suppresses account credentials when a
playback credential is present.

## Enforcement

The separate `PlaybackAccess` authentication scheme accepts `PlaybackToken` only
on explicitly marked media endpoints. The streaming policy additionally applies
the existing default authorization checks, including the owner's remote access
and parental schedule restrictions. Playback principals receive no account role
or general API credential.

Each request must match the grant's item, media source and playback session. HLS
requests must also match its device ID. Only GET and HEAD are accepted. Duplicate
scope values, packed legacy parameters, live stream overrides, and conflicting
subtitle item/source overrides are rejected. Stream preparation verifies that a
reused transcode job has the authorized media source and honors the owner's
transcoding and remuxing permissions.

HLS master and variant playlists propagate the credential to media segments,
including fMP4 initialization segments. Subtitle playlists propagate it to text
subtitle segments. Ordinary API endpoints do not accept this scheme, and putting
a playback credential in `ApiKey` does not turn it into an account token.

## Lifetime

Grants use 256-bit random opaque credentials held only in server memory. They
expire after four hours of inactivity or an absolute maximum of 24 hours.
Successful resource requests can extend the idle lifetime, never the absolute
lifetime. Server restart invalidates all grants. Expired entries are periodically
removed; issuance has a bounded capacity.

Playback stop events revoke matching grants. Clients can also use
`DELETE /PlaybackAccess/{playSessionId}` with their account credentials; trusted
integrations can call `Revoke`. Revocation allows a 30-second grace period for
outstanding requests and cannot be extended by subsequent resource requests or
repeated stop notifications.

## Initial scope

This implementation supports finite on-demand HLS and subtitle delivery. Live
streams, progressive playback grants, and trickplay resources are outside this
initial resource set. Delegated HLS manifests omit trickplay. Existing account
authentication continues to work for those features.

A leaked playback URL permits reads of the granted media during its lifetime.
It never grants account or administrative access. LAN restrictions can reduce
exposure further, but are not a replacement for resource scope.
