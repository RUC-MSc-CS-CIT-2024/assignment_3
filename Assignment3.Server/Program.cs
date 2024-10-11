
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Assignment3.Server;

TcpListener listener = new TcpListener(IPAddress.Loopback, 5000);
listener.Start();
Console.WriteLine("Server started");

while (true) {    
    Console.WriteLine("Waiting for connection...");
    // Handle request asyncously
    HandleConnection(listener.AcceptTcpClient());
}

async void HandleConnection(TcpClient client) {
    Console.WriteLine($"Client '{client.Client.RemoteEndPoint}' connected");
    try {
        Request req = await ReadRequestAsync(client);
        Response resp = HandleRequest(req);
        await SendResponseAsync(client, resp);
    } catch (Exception e) {
        if (e.Message.Contains("An existing connection was forcibly closed by the remote host.."))
            Console.WriteLine($"Client '{client.Client.RemoteEndPoint}' disconnected");
        else {
            Console.WriteLine($"error: {e.Message}");
            await SendResponseAsync(client, new Response(e.Message));
        }            
    } 
    finally {
        client.Close();
    }
}

Response HandleRequest(Request request) {
    request.EnsureValidRequest();

    // Handle echo request
    if(request.Method == "echo")
        return new Response($"1 Ok", request.Body);

    // Parse the path and extract the category id if present
    if(!Request.TryParsePathId(request, out int? categoryId))
        return ResponseFactory.CreateResponse(StatusCode.BadRequest);

    // Deserialize the body if required
    Category? body = request.IsBodyRequired()
        ? JsonSerializer.Deserialize<Category>(request.Body!, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        : null;

    // Handle the request
    switch (request.Method) {
        case "read":
            if (categoryId == null)
                return ResponseFactory.CreateResponse(StatusCode.Ok, CategoryApi.GetCategories());
            
            Category? cat = CategoryApi.GetCategory(categoryId!.Value);
            return cat != null
                ? ResponseFactory.CreateResponse(StatusCode.Ok, cat)
                : ResponseFactory.CreateResponse(StatusCode.NotFound);
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

async Task SendResponseAsync(TcpClient client, Response response)
{
    string jsonBody = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    Console.WriteLine($"info: response = {jsonBody}");
    byte[] msg = Encoding.UTF8.GetBytes(jsonBody);
    await client.GetStream().WriteAsync(msg, 0, msg.Length);
}

async Task<Request> ReadRequestAsync(TcpClient client)
{
    NetworkStream stream = client.GetStream();
    stream.ReadTimeout = 250;
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

    Request? result = JsonSerializer.Deserialize<Request>(requestData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    if (result == null)
        throw new Exception("Failed to deserialize request");
    
    return result;
}
