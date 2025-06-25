// Program.cs

// Import necessary namespaces for networking
using System.Net;
using System.Net.Sockets;
using System.Text;

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

                // As per the challenge, our task is to respond back with a UDP packet.
                // The content doesn't matter for this stage.
                byte[] responseBytes = Encoding.ASCII.GetBytes("Response from DNS server");

                // Send the response packet back to the client that sent the request.
                // The 'remoteEP' object now contains the correct destination address and port.
                udpClient.Send(responseBytes, responseBytes.Length, remoteEP);
                Console.WriteLine("Sent response packet back to the client.");
            }
            catch (Exception e)
            {
                // Log any errors that occur during the process.
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }
    }
}
