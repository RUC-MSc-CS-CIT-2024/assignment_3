using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Server;

public class HttpServer
{
    private readonly int _port;

    public HttpServer(int port)
    {
        _port = port;
    }


    public void Run() { 
 
        var server = new TcpListener(IPAddress.Loopback, _port); // IPv4 127.0.0.1 IPv6 ::1
        server.Start();

        Console.WriteLine($"Server started on port {_port}");

        while (true)
        {
            var client = server.AcceptTcpClient();
            Console.WriteLine("Client connected!!!");
            Task.Run(() => HandleClient(client));
        }

    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            string msg = ReadFromStream(stream);
            
            var request = new Request
            {
                Method = FromJson(msg).Method,
                Body = FromJson(msg).Body,
                Date = FromJson(msg).Date,
                Path = FromJson(msg).Path
            };
            
            Console.WriteLine("Message from client: " + msg);

            if (msg == "{}")
            {
                var response = new Response("missing method, missing body, missing date, missing path");
                
                var json = ToJson(response);
                WriteToStream(stream,json);
            }

            if (request.Method == null || request.Date == null || request.Body == null)
            {
                var response = new Response("missing resource or missing body or missing date"); 
            
                var json = ToJson(response);
                WriteToStream(stream,json);
            }
            else
            {
                if (!(new[] { "create", "read", "update", "delete", "echo" }.Contains(request.Method.ToLower())))
                {
                    var response = new Response("illegal method");
            
                    var json = ToJson(response);
                    WriteToStream(stream,json);
                }
                
                if (!IsValidUnixTimestamp(request.Date))
                {
                    var response = new Response("illegal date");
            
                    var json = ToJson(response);
                    WriteToStream(stream,json);
                }

                if (!IsValidJson(request.Body))
                {
                    var response = new Response("illegal body");
            
                    var json = ToJson(response);
                    WriteToStream(stream,json);
                }

                if (request.Method.Trim().Equals("echo", StringComparison.OrdinalIgnoreCase))
                {
                    Response response = new Response("echo") { Body = "Hello World" };
                    var json = ToJson(response);
                    
                    Console.WriteLine("response.body: " + response);
                    Console.WriteLine("json: " + json);
                    WriteToStream(stream,json);
                }
            }

        }
        catch { }
    }
    
    
    public bool IsValidJson(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return false;
        }
        
        try
        {
            var jsonDocument = JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
    
    bool IsValidUnixTimestamp(string timestampStr)
    {
   
        if (long.TryParse(timestampStr, out long timestamp))
        {
            return true;
        }
        return false;
    }

    private string ReadFromStream(NetworkStream stream)
    {
        var buffer = new byte[1024];
        var readCount = stream.Read(buffer);
        return Encoding.UTF8.GetString(buffer, 0, readCount);
    }
    
    private void WriteToStream(NetworkStream stream, string msg)
    {
        var buffer = Encoding.UTF8.GetBytes(msg);
        stream.Write(buffer);
    } 

    public static string ToJson(Response response)
    {
        return JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public static Request? FromJson(string element)
    {
        return JsonSerializer.Deserialize<Request>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}