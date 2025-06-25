// Program.cs

// Import necessary namespaces for networking
using System.Net;
using System.Net.Sockets;
using System.Text;

// A record is a convenient, immutable data structure for holding our parsed header fields.
public record DnsHeader(
    ushort PacketId,
    byte Flags,
    ushort QuestionCount,
    ushort AnswerCount,
    ushort AuthorityCount,
    ushort AdditionalCount
)
{
    // Extracts the OPCODE from the flags byte.
    public byte OpCode => (byte)((Flags >> 3) & 0b1111);

    // Extracts the Recursion Desired (RD) bit from the flags byte.
    public bool RecursionDesired => (Flags & 0b1) == 0b1;
}

public class DnsServer
{
    public static void Main(string[] args)
    {
        // The port our DNS server will listen on, as specified by the challenge.
        const int port = 2053;

        // A UdpClient is a high-level class for sending and receiving UDP packets.
        // By initializing it with a port number, we are telling it to "bind" to that port
        // and listen for incoming data. The 'using' statement ensures the client is
        // properly closed and disposed of when we're done.
        using var udpClient = new UdpClient(port);
        Console.WriteLine($"DNS Server listening on port {port}...");

        // An IPEndPoint represents a network endpoint as an IP address and a port number.
        // We initialize it to listen on any IP address and any port. The Receive method
        // will populate this object with the sender's actual IP and port.
        var remoteEP = new IPEndPoint(IPAddress.Any, 0);

        // We want our server to run forever, so we use an infinite loop.
        while (true)
        {
            try
            {
                Console.WriteLine("\nWaiting for a UDP packet...");

                // The Receive method is "blocking" - it will pause execution here until
                // a packet is received. The received data is returned as a byte array.
                // The 'remoteEP' object is passed by reference and will be filled with
                // the sender's address information.
                byte[] receivedBytes = udpClient.Receive(ref remoteEP);

                Console.WriteLine($"Received a packet from: {remoteEP}");

                // 1. Parse the header from the incoming query packet.
                DnsHeader requestHeader = ParseHeader(receivedBytes);
                Console.WriteLine($"Received Query with ID: {requestHeader.PacketId}, OpCode: {requestHeader.OpCode}");

                // For now, we still respond to a hardcoded domain and IP.
                const string domainName = "codecrafters.io";
                var ipAddress = IPAddress.Parse("8.8.8.8");

                // 2. Build the response header based on the parsed request header.
                byte[] responseHeader = BuildResponseHeader(requestHeader);

                // 3. Build the question and answer sections as before.
                byte[] questionSection = BuildQuestion(domainName);
                byte[] answerSection = BuildAnswer(domainName, ipAddress);

                // 4. Combine all parts and send the response.
                byte[] responseBytes = responseHeader.Concat(questionSection).Concat(answerSection).ToArray();
                udpClient.Send(responseBytes, responseBytes.Length, remoteEP);
                Console.WriteLine($"Sent response for {domainName} -> {ipAddress}");
            }
            catch (Exception e)
            {
                // Log any errors that occur during the process.
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Parses the 12-byte header from a DNS packet.
    /// </summary>
    private static DnsHeader ParseHeader(byte[] buffer)
    {
        // Read big-endian ushorts from the buffer
        ushort ReadBigEndianUshort(int offset) => (ushort)((buffer[offset] << 8) | buffer[offset + 1]);

        return new DnsHeader(
            PacketId: ReadBigEndianUshort(0),
            Flags: buffer[2], // We only need the first flags byte for now
            QuestionCount: ReadBigEndianUshort(4),
            AnswerCount: ReadBigEndianUshort(6),
            AuthorityCount: ReadBigEndianUshort(8),
            AdditionalCount: ReadBigEndianUshort(10)
        );
    }

    /// <summary>
    /// Builds the response header based on the request header.
    /// </summary>
    private static byte[] BuildResponseHeader(DnsHeader requestHeader)
    {
        var header = new byte[12];

        // Bytes 0-1: Copy the Packet ID from the request
        header[0] = (byte)(requestHeader.PacketId >> 8);
        header[1] = (byte)requestHeader.PacketId;

        // Byte 2: Set the flags
        // QR=1 (Response), OpCode from request, AA=0, TC=0, RD from request
        byte qr = 1;
        byte opCode = requestHeader.OpCode;
        byte aa = 0;
        byte tc = 0;
        byte rd = (byte)(requestHeader.RecursionDesired ? 1 : 0);
        header[2] = (byte)((qr << 7) | (opCode << 3) | (aa << 2) | (tc << 1) | rd);

        // Byte 3: Set the rest of the flags
        // RA=0, Z=0, RCODE
        byte ra = 0;
        byte z = 0;
        byte rCode = (requestHeader.OpCode == 0) ? (byte)0 : (byte)4; // 0=NoError, 4=NotImplemented
        header[3] = (byte)((ra << 7) | (z << 4) | rCode);

        // Bytes 4-5: Question Count (we will answer 1 question)
        header[4] = 0;
        header[5] = 1;

        // Bytes 6-7: Answer Record Count (we will provide 1 answer)
        header[6] = 0;
        header[7] = 1;

        // NSCOUNT and ARCOUNT are 0
        return header;
    }

    /// <summary>
    /// Builds the question section of a DNS packet.
    /// </summary>
    /// <param name="domainName">The domain name to query (e.g., "codecrafters.io").</param>
    /// <returns>A byte array representing the DNS question section.</returns>
    public static byte[] BuildQuestion(string domainName)
    {
        var byteList = new List<byte>();

        // 1. Encode the domain name into labels
        string[] labels = domainName.Split('.');
        foreach (string label in labels)
        {
            // Add the length of the label as a single byte.
            byteList.Add((byte)label.Length);
            // Add the ASCII bytes of the label itself.
            byteList.AddRange(Encoding.ASCII.GetBytes(label));
        }
        // Terminate the domain name with a null byte.
        byteList.Add(0);

        // 2. Add the Query Type (1 for A record) in big-endian format.
        byteList.Add(0);
        byteList.Add(1);

        // 3. Add the Query Class (1 for IN) in big-endian format.
        byteList.Add(0);
        byteList.Add(1);

        return byteList.ToArray();
    }

    /// <summary>
    /// Builds the answer section (a Resource Record) for a DNS packet.
    /// </summary>
    /// <param name="domainName">The domain name the answer is for.</param>
    /// <param name="ipAddress">The IP address to include in the answer.</param>
    /// <returns>A byte array representing the DNS answer section.</returns>
    public static byte[] BuildAnswer(string domainName, IPAddress ipAddress)
    {
        var byteList = new List<byte>();

        // 1. Encode the domain name (same as in the question)
        string[] labels = domainName.Split('.');
        foreach (string label in labels)
        {
            byteList.Add((byte)label.Length);
            byteList.AddRange(Encoding.ASCII.GetBytes(label));
        }
        byteList.Add(0);

        // 2. Add Type (A record = 1)
        byteList.AddRange(new byte[] { 0, 1 });

        // 3. Add Class (IN = 1)
        byteList.AddRange(new byte[] { 0, 1 });

        // 4. Add TTL (Time-To-Live). We'll use 60 seconds.
        const int ttl = 60;
        byteList.Add((byte)(ttl >> 24));
        byteList.Add((byte)(ttl >> 16));
        byteList.Add((byte)(ttl >> 8));
        byteList.Add((byte)ttl);

        // 5. Add RDLENGTH (Resource Data Length). For an IPv4 address, this is 4.
        const short rdlength = 4;
        byteList.Add((byte)(rdlength >> 8));
        byteList.Add((byte)rdlength);

        // 6. Add RDATA (Resource Data). The actual IP address bytes.
        byteList.AddRange(ipAddress.GetAddressBytes());

        return byteList.ToArray();
    }
}
