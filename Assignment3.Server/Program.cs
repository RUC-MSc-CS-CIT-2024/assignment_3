
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Assignment3.Server;

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

        Response resp = HandleRequest(req);

        await client.SendResponseAsync(resp);

    } catch (Exception e) {
        if (e.Message.Contains("An existing connection was forcibly closed by the remote host.."))
            Console.WriteLine($"Client '{client.Client.RemoteEndPoint}' disconnected");
        else {
            Console.WriteLine($"error: {e.Message}");
            await client.SendResponseAsync(new Response(e.Message));
        }            
    } 
    finally {
        client.Close();
    }
}

Response HandleRequest(Request request) {
    EnsureValidRequest(request);

    if(request.Method == "echo")
        return new Response($"1 Ok", request.Body);

    string pathPattern = @"^\/api\/categories(\/(?<CategoryId>\d+))?$";
    Match match = Regex.Match(request.Path, pathPattern);

    if (!match.Success)
        return ResponseFactory.CreateResponse(StatusCode.BadRequest);

    Group catIdGroup = match.Groups["CategoryId"];
    if (request.IsIdOnPathRequired() && !catIdGroup.Success)
        return ResponseFactory.CreateResponse(StatusCode.BadRequest);
    else if (request.IsIdOnPathNotAllowed() && catIdGroup.Success)
        return ResponseFactory.CreateResponse(StatusCode.BadRequest);

    Category? body = request.IsBodyRequired()
        ? JsonSerializer.Deserialize<Category>(request.Body!, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        : null;
    
    int? categoryId = 
        catIdGroup.Success 
        ? int.Parse(catIdGroup.Value) 
        : null;

    switch (request.Method) {
        case "read":
            if (categoryId == null)
                return ResponseFactory.CreateResponse(StatusCode.Ok, CategoryApi.GetCategories());
            
            Category? cat = CategoryApi.GetCategory(categoryId!.Value);
            return cat == null
                ? ResponseFactory.CreateResponse(StatusCode.NotFound)
                : ResponseFactory.CreateResponse(StatusCode.Ok, cat);
        case "create":
            Category newCat = CategoryApi.CreateCategory(body!);
            return ResponseFactory.CreateResponse(StatusCode.Created, newCat);
        case "update":
            return CategoryApi.UpdateCategory(categoryId!.Value, body!) 
                ? ResponseFactory.CreateResponse(StatusCode.Updated)
                : ResponseFactory.CreateResponse(StatusCode.NotFound);
        case "delete":
            return CategoryApi.DeleteCategory(categoryId!.Value) 
                ? ResponseFactory.CreateResponse(StatusCode.Ok)
                : ResponseFactory.CreateResponse(StatusCode.NotFound);
        default:
            return ResponseFactory.CreateResponse(StatusCode.BadRequest);
    }
}

void EnsureValidRequest(Request request) {
    string errorMsg = "";
    if (string.IsNullOrEmpty(request.Method))
        errorMsg += "Missing method ";
    else if (!request.IsValidMethod())
        errorMsg += "Illegal method";
    if (request.Method != "echo" && string.IsNullOrEmpty(request.Path))
        errorMsg += "Missing resource ";

    if (string.IsNullOrEmpty(request.Date))
            errorMsg += "Missing date ";
    else if (!int.TryParse(request.Date, out int date))
        errorMsg += "Illegal date ";
    else if (date < 0 || date > DateTimeOffset.Now.ToUnixTimeSeconds())
        errorMsg += "Illegal date ";

    if (request.IsBodyRequired() && string.IsNullOrEmpty(request.Body))
        errorMsg += "Missing body ";
    else if (request.Method == "update" && !IsValidJson(request.Body))
        errorMsg += "Illegal body ";

    if (errorMsg != "")
        throw new Exception(errorMsg.TrimEnd());
    
    Console.WriteLine("info: Valid request");
}

bool IsValidJson(string? strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput)) return false;
        strInput = strInput.Trim();
        if ((strInput.StartsWith("{") && strInput.EndsWith("}")) // For object
            || (strInput.StartsWith("[") && strInput.EndsWith("]")))   // For array
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

        if (result == null)
            throw new Exception("Failed to deserialize request");
        
        return result;
    }
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
    
    public bool IsIdOnPathRequired()
        => Method == "update"
        || Method == "delete";
    
    public bool IsIdOnPathNotAllowed()
        => Method == "create";
}
public record Response(string Status, string? Body = null);
public enum StatusCode {
    Ok = 1,
    Created = 2,
    Updated = 3,
    BadRequest = 4,
    NotFound = 5,
    Error = 6
}
public static class ResponseFactory {

    public static Response CreateResponse(StatusCode statusCode) {
        string status = statusCode switch {
            StatusCode.Ok => "1 Ok",
            StatusCode.Created => "2 Created",
            StatusCode.Updated => "3 Updated",
            StatusCode.BadRequest => "4 Bad Request",
            StatusCode.NotFound => "5 Not Found",
            StatusCode.Error => "6 Error",
            _ => throw new NotImplementedException()
        };
        return new Response(status);
    }
    public static Response CreateResponse<T>(StatusCode statusCode, T body) {
        
        return CreateResponse(statusCode) 
            with { Body = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) };
    }
}
