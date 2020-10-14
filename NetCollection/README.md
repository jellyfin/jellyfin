# NetworkCollection - A collection of useful bits of network coding.

NetCollection 
=============

An enumeration of IPObjects.

IPObjects (abstract object) - A base class for a network object (ip address and mask), including various function for network comparisons.

IPNetAddress - A representation of an IP address and subnet mask, which can be either IPv4 or IPv6, and can be defined by varous methods.
```
usage:

var address = IPNetAddress.Parse("10.1.10.0/24");
Logger.LogInformation("Network address is : {addr}", address.NetworkAddress);
Logger.LogInformation("Mask : {mask}", address.Mask);
Logger.LogInformation("Prefix : {prefix}", address.PrefixLength);
```

IPHost - A cached representation of a host object, which is only resolved as required.

usage:
```
var host = new IPHost("helloworld.com");
if (host.HasAddress)
{
    Logger.LogInformation("IP address is {ip}", host.Address;
}
```
UdpHelper
=========
Helper functions for creating everything you need for a multi-interface udp server that implements callbacks.

eg. Get a free port from the range 1-3402.
```
if ("1-3042".TryParseRange(out (int Min, int Max) range))
{
  var port = UdpHelper.GetUdpPortFromRange(range);
  
  var socket = CreateUdpBroadcastSocket(port, ip4: true, ip6: true);
}
```
Component: SsdpServer
=====================
As the name suggests a SSDP server that listens, notifies and provides the functionality to reply.

usage:
```
public bool AddressValidator(IPAddress address)
{
  return IPObjects.IsPrivateAddressRange(address);
}

var interfaces = new NetCollection();
interfaces.Add(IPNetAddress.Parse("10.0.0.0/8"));
interfaces.Add(IPNetAddress.Parse("192.168.0.0/16"));

var server = SsdpServer.GetOrCreateInstance(Logger, interfaces, AddressValidator, ipv4Enabled: true, ipv6Enabled : true);
server.Tracing = true;
server.SendMulticastSSDP(....)
```
Component: SsdpLocator
======================
Locates SSDP devices on the network. Just register what SSDP packet you are listening for, and away you go.






<a href="https://badge.fury.io/nu/NetworkCollection"><img src="https://badge.fury.io/nu/NetworkCollection.svg" alt="NuGet version" height="18"></a>
