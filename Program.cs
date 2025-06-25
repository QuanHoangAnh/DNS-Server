// Program.cs

// Import necessary namespaces for networking
using System.Net;
using System.Net.Sockets;

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

                // For this stage, we don't care about the content of the received packet,
                // but it's good practice to log that we received something.
                Console.WriteLine($"Received a packet from: {remoteEP}");
                Console.WriteLine($"Packet size: {receivedBytes.Length} bytes");

                // For this stage, the tester expects a hardcoded ID of 1234.
                const ushort packetId = 1234;

                // Build the DNS header response.
                byte[] responseBytes = BuildHeader(packetId);

                // Send the DNS header back to the client.
                udpClient.Send(responseBytes, responseBytes.Length, remoteEP);
                Console.WriteLine("Sent DNS Header response.");
            }
            catch (Exception e)
            {
                // Log any errors that occur during the process.
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Builds a 12-byte DNS header with hardcoded values for this stage.
    /// </summary>
    /// <param name="packetId">The 16-bit packet identifier.</param>
    /// <returns>A 12-byte array representing the DNS header.</returns>
    public static byte[] BuildHeader(ushort packetId)
    {
        // A DNS header is always 12 bytes long.
        var header = new byte[12];

        // Bytes 0-1: Packet Identifier (ID)
        // A random ID assigned by the query. The response must have the same ID.
        // We need to store it in Big-Endian format (most significant byte first).
        header[0] = (byte)(packetId >> 8); // Get the high byte
        header[1] = (byte)packetId;        // Get the low byte

        // Bytes 2-3: Flags
        // This is a set of bitfields. We'll set them according to the spec.
        //
        // Byte 2: QR(1) OPCODE(0000) AA(0) TC(0) RD(0)  -> 10000000 -> 0x80
        header[2] = 0b1000_0000; // QR = 1 (Response), RD = 0 (Recursion not desired)

        // Byte 3: RA(0) Z(000) RCODE(0000) -> 00000000 -> 0x00
        header[3] = 0b0000_0000; // RA = 0 (Recursion not available), RCODE = 0 (No Error)

        // Bytes 4-5: Question Count (QDCOUNT)
        // For this stage, we have 0 questions.
        header[4] = 0;
        header[5] = 0;

        // Bytes 6-7: Answer Record Count (ANCOUNT)
        // For this stage, we have 0 answers.
        header[6] = 0;
        header[7] = 0;

        // Bytes 8-9: Authority Record Count (NSCOUNT)
        // For this stage, we have 0 authority records.
        header[8] = 0;
        header[9] = 0;

        // Bytes 10-11: Additional Record Count (ARCOUNT)
        // For this stage, we have 0 additional records.
        header[10] = 0;
        header[11] = 0;

        return header;
    }
}
