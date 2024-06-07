

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class Program
{
    static void Main()
    {
        // Get the local IP address
        string localIP = GetLocalIPAddress();

        // Extract the subnet from the local IP address
        string subnet = localIP.Substring(0, localIP.LastIndexOf('.') + 1);

        Console.WriteLine("Scanning local network...");

        // Iterate through IP addresses in the subnet
        for (int i = 1; i <= 254; i++)
        {
            string ipAddress = subnet + i.ToString();

            // Ping the IP address
            Ping ping = new Ping();
            PingReply reply = ping.Send(ipAddress, 100);

            // Check if the ping was successful
            if (reply.Status == IPStatus.Success)
            {
                string hostname = GetHostName(ipAddress);
                Console.WriteLine("Device found: " + ipAddress + " (" + hostname + ")");
            }else if (reply.Status==IPStatus.TimedOut)
            {
                Console.WriteLine("Ping Failed, now checking MAC Address");
                IPAddress ip = IPAddress.Parse(ipAddress);
                PhysicalAddress mac = GetMacAddress(ip);

                // Check if a MAC address was found
                if (mac != null)
                {
                    Console.WriteLine("MAC Device found: " + ipAddress);
                }
            }
        }

        Console.WriteLine("Scan complete.");
    }

    static string GetHostName(string ipAddress)
    {
        try
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
            return hostEntry.HostName;
        }
        catch (SocketException)
        {
            return "Unknown";
        }
    }

    static string GetLocalIPAddress()
    {
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address.ToString();
        }
    }

    static PhysicalAddress GetMacAddress(IPAddress ipAddress)
    {
        try
        {
            IPHostEntry host = Dns.GetHostEntry(ipAddress);
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    PhysicalAddress mac = GetPhysicalAddress(ip);
                    if (mac != null)
                    {
                        return mac;
                    }
                }
            }

            return null;
        }
        catch (SocketException e)
        {
            Console.WriteLine($"No MAC Address found for IP Address {ipAddress}");
            return null;
        }
    }

    static PhysicalAddress GetPhysicalAddress(IPAddress ipAddress)
    {
        byte[] macBytes = new byte[6];
        uint macAddrLen = (uint)macBytes.Length;

        if (SendARP((int)ipAddress.Address, 0, macBytes, ref macAddrLen) == 0)
        {
            return new PhysicalAddress(macBytes);
        }
        else
        {
            return null;
        }
    }
    [System.Runtime.InteropServices.DllImport("iphlpapi.dll", ExactSpelling = true)]
    static extern int SendARP(int destIp, int srcIP, byte[] macAddr, ref uint macAddrLen);
}