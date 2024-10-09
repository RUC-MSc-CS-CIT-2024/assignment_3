
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

TcpListener listener = new TcpListener(IPAddress.Loopback, 5000);
listener.Start();
Console.WriteLine("Server started");

while (true) {    
    Console.WriteLine("Waiting for connection...");
    // handle asyncously
    HandleConnection(listener.AcceptTcpClient());
}

async void HandleConnection(TcpClient client) {
    try {
        Request req = await client.ReadRequestAsync();

        Response resp = await HandleRequestAsync(req);

        await client.SendResponseAsync(resp);

    } catch (Exception e) {
        if (e.Message.Contains("An existing connection was forcibly closed by the remote host.."))
            Console.WriteLine("Client disconnected");
        else {
            Console.WriteLine($"error: {e.Message}");
            await client.SendResponseAsync(new Response(e.Message));
        }            
    } 
    finally {
        client.Close();
    }
}

async Task<Response> HandleRequestAsync(Request request) {
    if(request.Method == "echo") {
        return new Response($"{(int)StatusCode.Ok} {StatusCode.Ok}", request.Body);
    }
    return new("");
}

static class Util {
    public static async Task SendResponseAsync(this TcpClient client, Response response)
    {
        string jsonBody = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Console.WriteLine($"info: response = {jsonBody}");
        byte[] msg = Encoding.UTF8.GetBytes(jsonBody);
        await client.GetStream().WriteAsync(msg, 0, msg.Length);
    }

    public static async Task<Request> ReadRequestAsync(this TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        //strm.ReadTimeout = 250;
        byte[] resp = new byte[2048];
        using MemoryStream memStream = new ();
        int bytesread = 0;
        do
        {
            bytesread = await stream.ReadAsync(resp, 0, resp.Length);
            await memStream.WriteAsync(resp, 0, bytesread);

        } while (bytesread == 2048);
        
        var requestData = Encoding.UTF8.GetString(memStream.ToArray());
        Console.WriteLine($"info: raw request = {requestData}");
        // using StreamReader reader = new StreamReader(client.GetStream());
        // string responseData = await reader.ReadToEndAsync();
        Request? result = JsonSerializer.Deserialize<Request>(requestData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        string errorMsg = "";
        if (result == null)
            throw new Exception("Failed to deserialize request");
        if (string.IsNullOrEmpty(result.Method))
            errorMsg += "Missing method ";
        else if (!result.IsValidMethod())
            throw new Exception("Illegal method");

        if (result.Method != "echo" && string.IsNullOrEmpty(result.Path))
            errorMsg += "Missing resource ";

        if (string.IsNullOrEmpty(result.Date))
            errorMsg += "Missing date ";
        else if (!int.TryParse(result.Date, out int date))
            errorMsg += "Illegal date ";
        else if (date < 0 || date > DateTimeOffset.Now.ToUnixTimeSeconds())
            errorMsg += "Illegal date ";

        if (result.IsBodyRequired() && string.IsNullOrEmpty(result.Body))
            errorMsg += "Missing body ";
        else if (result.Method == "update" && !IsValidJson(result.Body))
            errorMsg += "Illegal body ";
            
        if (errorMsg != "")
            throw new Exception(errorMsg.TrimEnd());
        
        return result;
    }

    
    static bool IsValidJson(string? strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput)) return false;
        strInput = strInput.Trim();
        if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || // For object
            (strInput.StartsWith("[") && strInput.EndsWith("]")))   // For array
        {
            try
            {
                JsonDocument.Parse(strInput);
                return true;
            }
            catch (JsonException) // Not valid JSON
            {
                return false;
            }
        }
        return false;
    }
}

enum StatusCode {
    Ok = 1,
    Created = 2,
    Updated = 3,
    BadRequest = 4,
    NotFound = 5,
    Error = 6
}
record Request(string Method, string Path, string Date, string? Body = null) {
    public bool IsValidMethod() 
        => Method == "create" 
        || Method == "read" 
        || Method == "update" 
        || Method == "delete" 
        || Method == "echo";

    public bool IsBodyRequired()
        => Method == "create" 
        || Method == "update" 
        || Method == "echo";
}
record Response(string Status, string? Body = null);
