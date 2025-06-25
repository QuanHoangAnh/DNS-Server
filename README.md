# DNS Server

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 9.0">
  <img src="https://img.shields.io/badge/Protocol-UDP-blue?style=for-the-badge" alt="UDP Protocol">
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT License">
</p>

> A lightweight DNS server implementation in C# that handles basic DNS queries and responses with support for DNS name compression.

## About The Project

This DNS server is a simple yet functional implementation that demonstrates the core concepts of the Domain Name System (DNS) protocol. Built as an educational project, it showcases how DNS queries are parsed, processed, and responded to at the protocol level.

The server handles incoming DNS queries over UDP, parses the DNS packet structure including headers and questions, and responds with appropriate DNS answers. It includes support for DNS name compression (pointer handling) and follows RFC-compliant DNS packet formatting.

### Why This Project Exists

- **Educational Purpose**: Understand the inner workings of DNS at the packet level
- **Protocol Learning**: Hands-on experience with binary protocol parsing and network programming
- **Foundation Building**: A starting point for more complex DNS server implementations
- **Network Programming**: Demonstrates UDP socket programming in C#

<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#about-the-project">About The Project</a></li>
    <li><a href="#key-features">Key Features</a></li>
    <li><a href="#built-with">Built With</a></li>
    <li><a href="#getting-started">Getting Started</a></li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#dns-protocol-implementation">DNS Protocol Implementation</a></li>
    <li><a href="#architecture">Architecture</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
  </ol>
</details>

## Key Features

- ✨ **DNS Query Parsing**: Handles standard DNS query packets with proper header and question parsing
- 🔄 **Name Compression Support**: Implements DNS name compression (pointer) handling for efficient packet processing
- 🌐 **UDP Protocol**: Uses UDP sockets for fast, connectionless DNS communication
- 📦 **A Record Responses**: Returns IPv4 address records for all queries (currently hardcoded to 8.8.8.8)
- 🔍 **Multi-Question Support**: Can handle multiple questions in a single DNS packet
- 🛡️ **Error Handling**: Robust error handling with proper DNS response codes
- 📝 **Detailed Logging**: Console logging for debugging and monitoring DNS traffic

## Built With

* [![.NET][.NET]][.NET-url]
* [![C#][CSharp]][CSharp-url]

## Getting Started

### Prerequisites

You must have .NET 9.0 or higher installed on your system.

* Download and install .NET 9.0 from [Microsoft's official website](https://dotnet.microsoft.com/download)

### Installation

1. Clone the repository
   ```sh
   git clone https://github.com/QuanHoangAnh/DNS-Server.git
   ```
2. Navigate to the project directory
   ```sh
   cd DNS-Server
   ```
3. Build the project
   ```sh
   dotnet build
   ```
4. Run the DNS server
   ```sh
   dotnet run
   ```

The server will start listening on port **2053** by default.

## Usage

Once the server is running, you can test it using various DNS query tools:

### Using dig (Linux/macOS/WSL)
```bash
dig @localhost -p 2053 example.com
```

### Using nslookup (Windows/Linux/macOS)
```bash
nslookup example.com localhost 2053
```

### Example Output
```
DNS Server listening on port 2053...

Waiting for a UDP packet...
Received a packet from: 127.0.0.1:54321
Received Query with ID: 12345, OpCode: 0
Parsed Question: example.com
Sent 1 answer(s).
```

The server will respond to all A record queries with the IP address `8.8.8.8` (Google's public DNS server).

## DNS Protocol Implementation

This implementation covers the following DNS protocol aspects:

### Supported Features
- **DNS Header Parsing**: Complete 12-byte header parsing with all standard fields
- **Question Section**: Parsing of domain names, query types, and classes
- **Answer Section**: Generation of A record responses with proper TTL
- **Name Compression**: Support for DNS pointer compression (RFC 1035)
- **OpCode Handling**: Proper response to standard queries and error responses for unsupported operations

### DNS Packet Structure
The server handles the standard DNS packet format:
```
+---------------------+
|        Header       |  (12 bytes)
+---------------------+
|       Question      |  (variable)
+---------------------+
|        Answer       |  (variable)
+---------------------+
|      Authority      |  (not implemented)
+---------------------+
|      Additional     |  (not implemented)
+---------------------+
```

### Current Limitations
- Only supports A record queries (IPv4 addresses)
- Returns hardcoded IP address (8.8.8.8) for all queries
- No actual domain resolution or forwarding
- No support for Authority or Additional sections
- No caching mechanism

## Architecture

The DNS server follows a simple, single-threaded architecture:

### Core Components

1. **DnsHeader Record**: Immutable data structure for DNS header fields
2. **DnsQuestion Record**: Represents parsed DNS question data
3. **Main Server Loop**: Handles incoming UDP packets in a blocking loop
4. **Parsing Methods**: 
   - `ParseHeader()`: Extracts DNS header information
   - `ParseQuestion()`: Parses question sections with name decompression
   - `DecodeDnsName()`: Handles DNS name encoding and compression pointers
5. **Response Building**:
   - `BuildResponseHeader()`: Creates proper DNS response headers
   - `BuildQuestion()`: Reconstructs question section for response
   - `BuildAnswer()`: Generates A record answers

### Design Patterns Used
- **Record Types**: Immutable data structures for DNS components
- **Tuple Returns**: Methods return both parsed data and bytes consumed

## Running Tests

Currently, this project doesn't include automated tests, but you can manually test the DNS server functionality:

### Manual Testing Steps

1. Start the DNS server:
   ```sh
   dotnet run
   ```

2. In another terminal, test with dig:
   ```bash
   dig @localhost -p 2053 google.com
   ```

3. Verify the response contains:
   - Query ID matching the request
   - Response flag set (QR=1)
   - Answer section with 8.8.8.8 IP address
   - TTL of 60 seconds

### Expected Response Format
```
;; QUESTION SECTION:
;google.com.                    IN      A

;; ANSWER SECTION:
google.com.             60      IN      A       8.8.8.8
```

## Configuration

The DNS server uses hardcoded configuration values that can be modified in the source code:

### Configurable Parameters

- **Port**: Default is `2053` (line 41 in Program.cs)
- **Response IP**: Default is `8.8.8.8` (line 87 in Program.cs)
- **TTL**: Default is `60` seconds (line 302 in Program.cs)

To modify these values, edit the corresponding lines in `Program.cs` and rebuild the project.


### Development Guidelines

- Follow C# coding conventions and best practices
- Add comprehensive comments for complex DNS protocol handling
- Ensure error handling for all network operations
- Test with various DNS query tools before submitting

## License

Distributed under the MIT License. See `LICENSE` for more information.

## Contact

Project Link: [https://github.com/yourusername/DNS-Server](https://github.com/yourusername/DNS-Server)

### Acknowledgements

* [RFC 1035 - Domain Names Implementation and Specification](https://tools.ietf.org/html/rfc1035)
* [Microsoft .NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
* [DNS Protocol Overview](https://www.cloudflare.com/learning/dns/what-is-dns/)
* [Best README Template](https://github.com/othneildrew/Best-README-Template)

<!-- MARKDOWN LINKS & IMAGES -->
[.NET]: https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white
[.NET-url]: https://dotnet.microsoft.com/
[CSharp]: https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white
[CSharp-url]: https://docs.microsoft.com/en-us/dotnet/csharp/
