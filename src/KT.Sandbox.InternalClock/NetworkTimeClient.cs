using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using KT.Sandbox.InternalClock.Exceptions;

namespace KT.Sandbox.InternalClock
{
    /// <summary>
    /// This struct returns the current time retrieved from a list of remote 
    /// network time protocol servers.
    /// 
    /// DNS queries are set to timeout quickly which can be adjusted in the
    /// constants.
    /// 
    /// conforms with RFC 2030
    ///     https://www.ntp.org/reflib/rfc/rfc2030.txt
    /// and potentially RFC 4030
    ///     https://www.ntp.org/reflib/rfc/rfc4330.txt
    ///     
    /// adapted from 
    ///     https://stackoverflow.com/questions/1193955/how-to-query-an-ntp-server-using-c
    /// </summary>
    internal struct NetworkTimeClient
    {
        //constants
        private const Int32 DNS_RESOLVE_TIMEOUT_MS = 150;                           //number of ms to wait before failing a dns entry lookup
        private const Int32 NTP_PORT_NUMBER = 123;                                  //default ntp port number
        private const Int32 SOCKET_RECEIVE_TIMEOUT_MS = 500;                        //how long to wait before moving onto the next
        private const String NTP_SERVERS = "us.pool.ntp.org,time.windows.com";      //csv list of available time servers

        /// <summary>
        /// Return the current time using NTP in DateTimeOffset format
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NetworkTimeException"></exception>
        public static DateTimeOffset GetNetworkTime()
        {
            //get a list of all ip addresses from the list of ntp servers
            IEnumerable<IPAddress> ipAddresses = GetIpAddresses(NTP_SERVERS);

            //loop through each ip address until we get a valid ntp response
            foreach (IPAddress ipAddress in ipAddresses)
            {
                DateTimeOffset? response = GetNtpResponse(ipAddress);

                if (response.HasValue)
                    return response.Value;
            }

            //no responses were retrieve.  fale!
            throw new NetworkTimeException($"Unable to retrieve a valid response from any of the following servers: '{ NTP_SERVERS }'.");            
        }

        /// <summary>
        /// Flip endianness of an unsigned 64 bit integer
        /// 
        /// source:
        ///     stackoverflow.com/a/3294698/162671
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static UInt32 SwapEndianness(ulong val)
        {
            return (uint)(
                ((val & 0x000000ff) << 24)
                + ((val & 0x0000ff00) << 8)
                + ((val & 0x00ff0000) >> 8)
                + ((val & 0xff000000) >> 24));
        }

        /// <summary>
        /// Return a unique list the ip addresses based on the dns entries 
        /// from a comma-separated list of domain names
        /// </summary>
        /// <param name="csvDomainNames"></param>
        /// <returns></returns>
        private static IEnumerable<IPAddress> GetIpAddresses(string csvDomainNames)
        {
            //create a collection to track the domains
            Collection<string> domainNames = new();
            
            //split the csv, validate that it is a valid domain name and ensure uniqueness
            foreach(string domainName in csvDomainNames.Split(','))
            {
                if (Uri.CheckHostName(domainName) != UriHostNameType.Unknown && !domainNames.Contains(domainName))
                    domainNames.Add(domainName);
            }

            //get a unique list of ip addresses for each domain name
            Collection<IPAddress> ipAddresses = new();

            foreach(string domainName in domainNames)
            {
                IPAddress[] domainAddressList;

                //check to see if this domain can return an address list
                try
                {
                    domainAddressList = ResolveDnsAddressList(domainName);// Dns.GetHostEntry(domainName).AddressList;
                }
                catch(SocketException)
                {
                    continue;
                }

                //did we get any addresses?
                if (domainAddressList.Length == 0)
                {
                    continue;
                }

                //add the ip address to a list so we can track them for
                //uniqueness (prolly wildly unnecessary) and yield
                foreach (IPAddress ipAddress in domainAddressList)
                {
                    if (!ipAddresses.Contains(ipAddress))
                        ipAddresses.Add(ipAddress);

                    yield return ipAddress;
                }
            }
        }

        /// <summary>
        /// Return ntp response as a DateTimeOffset
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        private static DateTimeOffset? GetNtpResponse(IPAddress ipAddress)
        {
            //TODO: make this asynchronous

            // ntp message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //setting the leap indicator, version Number and mode values
            ntpData[0] = 0x1B; //li = 0 (no warning), vn = 3 (IPv4 only), mode = 3 (Client Mode)

            /*
             *                      1                   2                   3
             *  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
             * +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             * |LI | VN  |Mode |    Stratum    |     Poll      |   Precision   |
             * +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             * 
             * dec:
             *    0     3     3
             * 
             * bin:
             *  0 0 0 1 1 0 1 1
             * 
             * hex:
             *  0x1B = 0b11011
             */

            //the udp port number assigned to ntp is 123
            IPEndPoint ipEndPoint = new(ipAddress, NTP_PORT_NUMBER);

            //ntp uses udp
            try
            {
                using (Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Connect(ipEndPoint);

                    //stops code hang if ntp is blocked
                    socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT_MS;

                    socket.Send(ntpData);
                    socket.Receive(ntpData);
                    socket.Close();
                }
            }
            catch(SocketException)
            {
                //couldn't establish a socket with address.  do nothing and move on.
                return null;
            }            

            //offset to get to the "transmit timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format)
            const byte serverReplyTime = 40;        //read more here: https://www.ntp.org/documentation/4.2.8-series/warp/

            //get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            ulong milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //add response milliseconds to 1/1/1900 to get time in utc
            DateTime networkTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

            //return date with utc offset
            return new DateTimeOffset(networkTime, TimeZoneInfo.Utc.GetUtcOffset(networkTime));
        }

        /// <summary>
        /// Return the dns address list for a hostname, but timeout after
        /// a pre-defined number of milliseconds
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        private static IPAddress[] ResolveDnsAddressList(string hostName)
        {
            //how long to wait before r/nope
            var timeout = TimeSpan.FromMilliseconds(DNS_RESOLVE_TIMEOUT_MS);

            //start task to run inline function to resolve dns address list
            Task<IPAddress[]> task = ResolveAsync(hostName);

            //wait for a bit.  did we get a response in the allotted time?
            if (!task.Wait(timeout))
            {
                //we did not.  return empty
                return Array.Empty<IPAddress>();
            }

            return task.Result;

            //sorry about the following line.  it's way the hell past my bedtime.
            Task<IPAddress[]> ResolveAsync(string hostName) => Task.Run(() => { try { return Dns.GetHostEntry(hostName).AddressList; } catch (SocketException) { return Array.Empty<IPAddress>(); } });
        }
    }
}
