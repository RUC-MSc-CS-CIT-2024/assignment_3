using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public class Server
{
    private readonly int _port;

    public Server(int port)
    {
        _port = port;
    }


    public void run()
    {
        var server = new TcpListener(IPAddress.Loopback, _port); // IPv4 127.0.0.1 IPv6 ::1
        server.Start();

        Console.WriteLine($"Server started on port {_port}");

        while (true)
        {
            var client = server.AcceptTcpClient();
            Console.WriteLine("Client connected!");
            
            // uses this multithreading for handling all the test running at once.
            Task.Run(() => HandleClient(client));
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            string msg = ReadFromStream(stream);
            Console.WriteLine("Message from client: " + msg);

            if (msg == "{}")
            {
                var response = new Response
                {
                    Status = "missing method"
                };

                var json = ToJson(response);
                WriteToStream(stream, json);
                return;
            }
            else
            {
                var request = FromJson(msg);
                if (request == null)
                {
                }

                string[] validMethodes = ["create", "read", "update", "delete", "echo"];

                if (!validMethodes.Contains(request.Method))
                {
                    var response = new Response
                    {
                        Status = "illegal method"
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                }
            }
        }
        catch
        {
        }
    }


    private string ReadFromStream(NetworkStream stream)
    {
        var buffer = new byte[1024];
        using (var ms = new MemoryStream())
        {
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, bytesRead);

                // Break if we received less than the buffer size, which means the message has ended
                if (bytesRead < buffer.Length)
                    break;
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }


    private void WriteToStream(NetworkStream stream, string msg)
    {
        var buffer = Encoding.UTF8.GetBytes(msg);
        stream.Write(buffer);
    }

    // Converting the response to JSON 
    public static string ToJson(Response response)
    {
        return JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // Converting the Request from JSON
    public static Request? FromJson(string element)
    {
        return JsonSerializer.Deserialize<Request>(element,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}