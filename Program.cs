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

// Record to hold parsed DNS Question information
public record DnsQuestion(
    string Name,
    ushort Type,
    ushort Class
);

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

                // 2. Parse the question section. The question starts right after the 12-byte header.
                (DnsQuestion requestQuestion, _) = ParseQuestion(receivedBytes, 12);
                Console.WriteLine($"Received Query for: {requestQuestion.Name}");

                // 3. Build the response.
                // The IP address is still hardcoded, but the rest is dynamic.
                var ipAddress = IPAddress.Parse("8.8.8.8");

                byte[] responseHeader = BuildResponseHeader(requestHeader);
                byte[] responseQuestion = BuildQuestion(requestQuestion.Name);
                byte[] responseAnswer = BuildAnswer(requestQuestion.Name, ipAddress);

                byte[] responseBytes = responseHeader.Concat(responseQuestion).Concat(responseAnswer).ToArray();
                udpClient.Send(responseBytes, responseBytes.Length, remoteEP);
                Console.WriteLine($"Sent response for {requestQuestion.Name} -> {ipAddress}");
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
    /// Parses the question section from a DNS packet buffer.
    /// </summary>
    /// <param name="buffer">The full packet byte array.</param>
    /// <param name="offset">The starting position of the question section.</param>
    /// <returns>A tuple containing the parsed DnsQuestion and the number of bytes read.</returns>
    private static (DnsQuestion, int) ParseQuestion(byte[] buffer, int offset)
    {
        int initialOffset = offset;
        var labels = new List<string>();

        // Loop to read the domain name labels
        while (buffer[offset] != 0)
        {
            byte length = buffer[offset];
            offset++;
            labels.Add(Encoding.ASCII.GetString(buffer, offset, length));
            offset += length;
        }
        offset++; // Skip the null terminator byte

        string domainName = string.Join('.', labels);

        // Read Type and Class (2 bytes each)
        ushort type = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
        offset += 2;
        ushort qclass = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
        offset += 2;

        var question = new DnsQuestion(domainName, type, qclass);
        int bytesRead = offset - initialOffset;

        return (question, bytesRead);
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

        // Bytes 4-5: Question Count (copy from request)
        header[4] = (byte)(requestHeader.QuestionCount >> 8);
        header[5] = (byte)requestHeader.QuestionCount;

        // Bytes 6-7: Answer Record Count (we will provide 1 answer per question)
        header[6] = (byte)(requestHeader.QuestionCount >> 8);
        header[7] = (byte)requestHeader.QuestionCount;

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
