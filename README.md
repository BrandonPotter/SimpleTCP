# SimpleTCP
Straightforward and incredibly useful .NET library to handle the repetitive tasks of spinning up and working with TCP sockets (client and server).

**NuGet Package:** https://www.nuget.org/packages/SimpleTCP/

![Build Status](https://ci.appveyor.com/api/projects/status/felx0b90mwgr4l4n?svg=true)

Want a TCP server that listens on port 8910 on all the IP addresses on the machine?

```cs
var server = new SimpleTcpServer().Start(8910);
```

Want a TCP client that connects to 127.0.0.1 on port 8910?

```cs
var client = new SimpleTcpClient().Connect("127.0.0.1", 8910);
```

Want to send "Hello world!" to the server and get the reply that it sends within 3 seconds?

```cs
var replyMsg = client.WriteLineAndGetReply("Hello world!", TimeSpan.FromSeconds(3));
```

Want to receive a message event on the server each time you see a newline \n (char 13), and echo back any messages that come in?

```cs
server.Delimiter = 0x13;
server.DelimiterDataReceived += (sender, msg) => {
                msg.ReplyLine("You said: " + msg.MessageString);
            };
```

Want to know how many clients are connected to the server?

```cs
int clientsConnected = server.ConnectedClientsCount;
```

Want to change the text encoding that the client and server uses when sending and receiving strings? (The default is ASCII/UTF8.)

```cs
server.StringEncoder = System.Text.ASCIIEncoding.ASCII;
client.StringEncoder = System.Text.ASCIIEncoding.ASCII;
```

Want to get the IP addresses that the server is listening on?

```cs
var listeningIps = server.GetListeningIPs();
```

Want to get only the IPv4 addresses the server is listening on?

```cs
var listeningV4Ips = server.GetListeningIPs().Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
```

Want to make your node.js friends stop saying things like "with node I can spin up a web server in only 4 lines of code"?

```cs
var server = new SimpleTcpServer().Start(80);
server.DataReceived += (sender, msg) => {
                msg.Reply("Content-Type: text/plain\n\nHello from my web server!"); 
                };
```

(But really, this library isn't ideal for web server-ing, so don't do that in prod.)
