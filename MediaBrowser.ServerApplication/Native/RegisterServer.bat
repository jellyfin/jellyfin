rem %1 = http server port
rem %2 = http server url
rem %3 = udp server port

if [%1]==[] GOTO DONE

netsh advfirewall firewall delete rule name="Port %1" protocol=TCP localport=%1
netsh advfirewall firewall add rule name="Port %1" dir=in action=allow protocol=TCP localport=%1

if [%2]==[] GOTO DONE

netsh http del urlacl url="%2" user="NT AUTHORITY\Authenticated Users"
netsh http add urlacl url="%2" user="NT AUTHORITY\Authenticated Users"

if [%3]==[] GOTO DONE

netsh advfirewall firewall delete rule name="Port %3" protocol=UDP localport=%3
netsh advfirewall firewall add rule name="Port %3" dir=in action=allow protocol=UDP localport=%3


:DONE
Exit