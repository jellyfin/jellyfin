#!/usr/bin/env python3
"""Isolated PlaybackInfo regression. No existing server or library is accepted.

Creates a temporary loopback server, generated video and STRM/NFO library.
Use --expect-overwrite to establish the unmodified upstream failure; omit it
for the regression assertion. Retains the fixture/report for inspection.
"""
import argparse
import json
import functools
import http.server
import threading
import os
from pathlib import Path
import secrets
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--server-dll', required=True, type=Path)
    parser.add_argument('--ffmpeg', required=True, type=Path)
    parser.add_argument('--expect-overwrite', action='store_true')
    args = parser.parse_args()
    root = Path(tempfile.mkdtemp(prefix='jellyfin-playback-regression-'))
    os.chmod(root, 0o700)
    for directory in ('data', 'config', 'cache', 'log', 'source', 'movies'):
        (root / directory).mkdir()
    with socket.socket() as sock:
        sock.bind(('127.0.0.1', 0))
        port = sock.getsockname()[1]
    (root / 'config/network.xml').write_text(
        f'<NetworkConfiguration><InternalHttpPort>{port}</InternalHttpPort>'
        '<LocalNetworkAddresses><string>127.0.0.1</string></LocalNetworkAddresses>'
        '<EnableRemoteAccess>false</EnableRemoteAccess></NetworkConfiguration>')
    source = root / 'source/video.mkv'
    subprocess.run([str(args.ffmpeg.resolve()), '-y', '-hide_banner', '-loglevel', 'error',
                    '-f', 'lavfi', '-i', 'color=c=black:s=160x90:r=10', '-t', '1',
                    '-c:v', 'libx264', '-metadata', 'title=Embedded title',
                    '-metadata', 'genre=Embedded genre', str(source)], check=True)
    handler = functools.partial(http.server.SimpleHTTPRequestHandler, directory=str(root / 'source'))
    media_server = http.server.ThreadingHTTPServer(('127.0.0.1', 0), handler)
    threading.Thread(target=media_server.serve_forever, daemon=True).start()
    source_url = f'http://127.0.0.1:{media_server.server_port}/video.mkv'
    cases = [('Locked NFO', True, False), ('Unlocked NFO', False, False),
             ('Empty fields', True, True)]
    original_nfos = {}
    for name, locked, _ in cases:
        folder = root / 'movies' / name
        folder.mkdir()
        (folder / (name + '.strm')).write_text(source_url)
        nfo = folder / 'movie.nfo'
        nfo.write_text(f'<movie><title>{name}</title><genre>Original genre</genre>'
                       '<country>Original country</country><plot>Original overview</plot>'
                       f'<lockdata>{str(locked).lower()}</lockdata></movie>')
        original_nfos[nfo] = nfo.read_bytes()
    console = (root / 'console.log').open('w')
    process = subprocess.Popen([
        'dotnet', str(args.server_dll.resolve()), '--datadir', str(root / 'data'),
        '--configdir', str(root / 'config'), '--cachedir', str(root / 'cache'),
        '--logdir', str(root / 'log'), '--nowebclient', '--nonetchange',
        '--ffmpeg', str(args.ffmpeg.resolve())], stdout=console, stderr=subprocess.STDOUT)
    token = None
    report = {'fixture': str(root), 'expect_overwrite': args.expect_overwrite, 'cases': []}

    def api(path, method='GET', body=None):
        auth = 'MediaBrowser Client="Regression",Device="Fixture",DeviceId="isolated",Version="1"'
        if token:
            auth += f',Token="{token}"'
        request = urllib.request.Request(f'http://127.0.0.1:{port}' + path, method=method,
                                         data=None if body is None else json.dumps(body).encode(),
                                         headers={'Content-Type': 'application/json', 'Authorization': auth})
        with urllib.request.urlopen(request, timeout=60) as response:
            data = response.read()
            return json.loads(data) if data else None

    def wait(condition, label):
        deadline = time.monotonic() + 120
        while time.monotonic() < deadline:
            if process.poll() is not None:
                raise AssertionError('Isolated server exited: ' + str(root / 'console.log'))
            result = condition()
            if result:
                return result
            time.sleep(.25)
        raise AssertionError('Timeout: ' + label)

    def selected(dto):
        return {k: dto.get(k) for k in ('Name', 'Genres', 'ProductionLocations', 'Overview', 'LockedFields', 'LockData')}

    try:
        def ready():
            try:
                return isinstance(api('/Startup/User'), dict)
            except (urllib.error.URLError, TimeoutError, json.JSONDecodeError):
                return False
        wait(ready, 'startup')
        password = secrets.token_urlsafe(24)
        api('/Startup/User', 'POST', {'Name': 'regression', 'Password': password})
        api('/Startup/Configuration', 'POST', {'ServerName': 'Isolated playback regression', 'UICulture': 'en-US'})
        api('/Startup/Complete', 'POST')
        login = api('/Users/AuthenticateByName', 'POST', {'Username': 'regression', 'Pw': password})
        token, uid = login['AccessToken'], login['User']['Id']
        info = api('/System/Info/Public')
        assert info['ServerName'] == 'Isolated playback regression'
        report['server_version'] = info['Version']
        tasks = api('/ScheduledTasks')
        for task in tasks:
            api('/ScheduledTasks/' + task['Id'] + '/Triggers', 'POST', [])
        wait(lambda: not any(t['State'] in ('Running', 'Cancelling') for t in api('/ScheduledTasks')
                             if t['Key'] == 'RefreshLibrary'), 'startup scan')
        query = urllib.parse.urlencode({'name': 'Regression', 'collectionType': 'movies',
                                        'paths': str(root / 'movies'), 'refreshLibrary': 'false'})
        api('/Library/VirtualFolders?' + query, 'POST', {'LibraryOptions': {
            'EnableInternetProviders': False, 'EnableRealtimeMonitor': False,
            'MetadataSavers': [], 'AutomaticRefreshIntervalDays': 0,
            'TypeOptions': [{'Type': 'Movie', 'MetadataFetchers': [], 'ImageFetchers': []}]}})
        api('/Library/Refresh', 'POST')
        def listing():
            return api('/Items?recursive=true&includeItemTypes=Movie&userId=' + uid)['Items']
        wait(lambda: len(listing()) == len(cases), 'import')
        wait(lambda: not any(t['State'] in ('Running', 'Cancelling') for t in api('/ScheduledTasks')
                             if t['Key'] == 'RefreshLibrary'), 'scan completion')
        ids = {i['Name']: i['Id'] for i in listing()}
        def get(item_id):
            return api(f'/Users/{uid}/Items/{item_id}')
        for name, locked, empty in cases:
            item_id = ids[name]
            dto = get(item_id)
            assert dto['Genres'] == ['Original genre'], dto
            dto.update(Genres=[] if empty else ['剧情'], ProductionLocations=[] if empty else ['中国'],
                       Name='API title', Overview='' if empty else 'API overview', LockedFields=[], LockData=False)
            api('/Items/' + item_id, 'POST', dto)
            before = selected(get(item_id))
            assert before['Genres'] == dto['Genres'] and before['ProductionLocations'] == dto['ProductionLocations']
            assert before['LockedFields'] == [] and before['LockData'] is False, before
            entry = {'case': name, 'nfo_lockdata': locked, 'before': before, 'playbacks': []}
            report['cases'].append(entry)
            for _ in range(2):
                playback = api(f'/Items/{item_id}/PlaybackInfo?UserId={uid}', 'POST', {})
                streams = playback['MediaSources'][0]['MediaStreams']
                assert any(s['Type'] == 'Video' and s.get('Width') == 160 for s in streams), streams
                after = selected(get(item_id))
                entry['playbacks'].append(after)
                if not args.expect_overwrite:
                    assert after == before, (name, before, after)
            print(json.dumps(entry, ensure_ascii=False), flush=True)
        assert all(p.read_bytes() == original for p, original in original_nfos.items())
        changed = any(entry['before'] != entry['playbacks'][0] for entry in report['cases'])
        if args.expect_overwrite:
            assert changed, 'Baseline did not demonstrate the reported overwrite'
            for entry in report['cases']:
                after = entry['playbacks'][0]
                if entry['nfo_lockdata']:
                    assert after['Genres'] == ['Original genre']
                    assert after['Overview'] == 'Original overview'
                else:
                    assert after['Genres'] == entry['before']['Genres']
                assert 'Original country' in after['ProductionLocations']
        else:
            assert not changed
            # Failed network probing must keep the previously available streams and descriptions.
            item_id = ids['Locked NFO']
            before_failure = get(item_id)
            unavailable = source.with_suffix('.unavailable')
            source.rename(unavailable)
            try:
                playback = api(f'/Items/{item_id}/PlaybackInfo?UserId={uid}', 'POST', {})
                assert any(s['Type'] == 'Video' and s.get('Width') == 160
                           for s in playback['MediaSources'][0]['MediaStreams'])
                assert selected(get(item_id)) == selected(before_failure)
                assert get(item_id)['RunTimeTicks'] == before_failure['RunTimeTicks']
                report['failed_probe_preserved_data'] = True
            finally:
                unavailable.rename(source)
            # Ordinary explicit replace-all must still read the NFO after this patch.
            item_id = ids['Locked NFO']
            api(f'/Items/{item_id}/Refresh?metadataRefreshMode=FullRefresh&replaceAllMetadata=true', 'POST')
            wait(lambda: get(item_id).get('Genres') == ['Original genre'], 'explicit NFO refresh')
            report['explicit_refresh'] = selected(get(item_id))
        report['passed'] = True
    finally:
        process.terminate()
        try:
            process.wait(timeout=20)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
        console.close()
        media_server.shutdown()
        media_server.server_close()
        (root / 'report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2))
        print('Report: ' + str(root / 'report.json'), flush=True)


if __name__ == '__main__':
    main()
