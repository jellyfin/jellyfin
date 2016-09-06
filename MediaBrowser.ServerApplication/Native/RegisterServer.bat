rem %1 = udp server port
rem %2 = http server port
rem %3 = https server port
rem %4 = exe path

if [%1]==[] GOTO DONE

netsh advfirewall firewall delete rule name="Port %1" protocol=UDP localport=%1
netsh advfirewall firewall add rule name="Port %1" dir=in action=allow protocol=UDP localport=%1

if [%2]==[] GOTO DONE

netsh advfirewall firewall delete rule name="Port %2" protocol=TCP localport=%2
netsh advfirewall firewall add rule name="Port %2" dir=in action=allow protocol=TCP localport=%2

if [%3]==[] GOTO DONE

netsh advfirewall firewall delete rule name="Port %3" protocol=TCP localport=%3
netsh advfirewall firewall add rule name="Port %3" dir=in action=allow protocol=TCP localport=%3

if [%4]==[] GOTO DONE

netsh advfirewall firewall delete rule name="mediabrowser.serverapplication.exe"
netsh advfirewall firewall delete rule name="Emby Server"

netsh advfirewall firewall add rule name="Emby Server" dir=in action=allow protocol=TCP program=%4 enable=yes
netsh advfirewall firewall add rule name="Emby Server" dir=in action=allow protocol=UDP program=%4 enable=yes


:DONE
Exit