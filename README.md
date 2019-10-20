# TcpIncrementor Service

A simple Service that receives commands and responds to them on a TCP connection.
There are three commands that the service understands:
* `send [initial] [delay] [increment]`: sends the client an incrementing sequence of numbers that start with `[initial]`, is incremented by `[increment]` and send every `[delay]` seconds.
All parameters are integer values and are separated by a single space character.
* `stop`: stops the incrementing sequence sending from the service.
* `kill`: terminates the TCP session

## Getting Started

### Prerequisites
* [.NET Core SDK 3.0](https://dotnet.microsoft.com/download/dotnet-core/3.0)

### Installing
* Checkout this repository from github `git clone https://github.com/Ben10k/TcpIncrementor.git`
* Open the project in your favourite IDE and Run the project from UI or start it from console by `dotnet run`

## Usage

The Service is configurable within the [appsettings.json](appsettings.json) file.

The client can be any 8-bit ASCII based TCP client like netcat or Telnet.
An example with netcat:
* Connect: `nc localhost 1025`
* Start sending numbers starting with 1, delay 2 seconds and increment it by 5 each time: `send 1 2 5`
* Stop sending numbers: `stop`
