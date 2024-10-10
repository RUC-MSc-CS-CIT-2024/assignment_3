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
            var msg = ReadFromStream(stream);
            Console.WriteLine("Message from client: " + msg);

            // Attempt to deserialize the request
            var request = FromJson(msg);

            // Check for missing Method first
            if (string.IsNullOrEmpty(request.Method))
            {
                var response = new Response { Status = "missing method or missing date" };
                WriteToStream(stream, ToJson(response));
                return;
            }

            // Correct array initialization using curly braces
            string[] validMethods = { "create", "read", "update", "delete", "echo" };

            // Check if the method is valid
            if (!validMethods.Contains(request.Method))
            {
                var response = new Response
                {
                    Status = "illegal method"
                };
                WriteToStream(stream, ToJson(response));
                return;
            }

            // Handle echo case
            if (request.Method == "echo")
            {
                if (string.IsNullOrEmpty(request.Body))
                {
                    var response = new Response { Status = "missing body" };
                    WriteToStream(stream, ToJson(response));
                    return;
                }

                var echoResponse = new Response { Body = request.Body };
                WriteToStream(stream, ToJson(echoResponse));
                return;
            }

            // Validate path and date
            if (string.IsNullOrEmpty(request.Path) || string.IsNullOrEmpty(request.Date))
            {
                var response = new Response { Status = "missing resource" };
                WriteToStream(stream, ToJson(response));
                return;
            }

            // Validate Unix timestamp
            if (!IsValidUnixTimestamp(request.Date))
            {
                var response = new Response { Status = "illegal date" };
                WriteToStream(stream, ToJson(response));
                return;
            }

            // Check for missing body for create and update methods
            if ((request.Method == "create" || request.Method == "update") && string.IsNullOrEmpty(request.Body))
            {
                var response = new Response { Status = "missing body" };
                WriteToStream(stream, ToJson(response));
                return;
            }

            // Check if the body is valid JSON for update
            if (request.Method == "update" && !IsValidJson(request.Body))
            {
                var response = new Response { Status = "illegal body" };
                WriteToStream(stream, ToJson(response));
                return;
            }

            // Define valid paths for read and create methods
            string[] validPaths = { "/api/categories", "/api/categories/" };

            // Handle read and create request validation
            if (request.Method == "read")
            {
                // Check if the path is valid
                if (!validPaths.Contains(request.Path) &&
                    !request.Path.StartsWith("/api/categories/"))
                {
                    var response = new Response { Status = "4 Bad Request" };
                    WriteToStream(stream, ToJson(response));
                    return;
                }
            }

            if (request.Method == "create" && request.Path.Contains("/api/categories/"))
            {
                var parts = request.Path.Split('/');
                if (parts.Length > 3) // If there is an ID present
                {
                    var response = new Response { Status = "4 Bad Request" }; // Invalid request for create
                    WriteToStream(stream, ToJson(response));
                    return;
                }
            }

            // Handle read request for all categories
            if (request.Method == "read" && request.Path.TrimEnd('/') == validPaths[0])
            {
                var categories = new List<object>
                {
                    new { cid = 1, name = "Beverages" },
                    new { cid = 2, name = "Condiments" },
                    new { cid = 3, name = "Confections" }
                };

                var response = new Response
                {
                    Status = "1 Ok",
                    Body = ToJson(categories)
                };
                WriteToStream(stream, ToJson(response));
                return;
            }

            // Handle read request for a single category
            if (request.Method == "read" && request.Path.StartsWith("/api/categories/"))
            {
                // Extract the ID from the path
                var parts = request.Path.Split('/');
                if (parts.Length < 3 || !int.TryParse(parts[^1], out int categoryId))
                {
                    var response = new Response
                    {
                        Status = "4 Bad Request",
                        Body = null
                    };
                    WriteToStream(stream, ToJson(response));
                    return;
                }

                // Define categories for lookup
                var categories = new List<object>
                {
                    new { cid = 1, name = "Beverages" },
                    new { cid = 2, name = "Condiments" },
                    new { cid = 3, name = "Confections" }
                };

                // Find the category by ID
                var category = categories.FirstOrDefault(c => ((dynamic)c).cid == categoryId);

                // If found, respond with the category
                if (category != null)
                {
                    var response = new Response
                    {
                        Status = "1 Ok",
                        Body = ToJson(category)
                    };
                    WriteToStream(stream, ToJson(response));
                }
                else
                {
                    // Handle case where category is not found
                    var errorResponse = new Response
                    {
                        Status = " 5 Not Found",
                        Body = null
                    };
                    WriteToStream(stream, ToJson(errorResponse));
                }

                return;
            }

            // Handle update method
            if (request.Method == "update")
            {
                if (!request.Path.Contains("/api/categories/") || !request.Path.Split('/').Last().All(char.IsDigit))
                {
                    var response = new Response { Status = "4 Bad Request" };
                    WriteToStream(stream, ToJson(response));
                    return;
                }

                if (string.IsNullOrEmpty(request.Body) || !request.Body.Contains("cid"))
                {
                    var response = new Response { Status = "4 Bad Request" };
                    WriteToStream(stream, ToJson(response));
                    return;
                }
            }

            // Handle delete method
            if (request.Method == "delete")
            {
                if (!request.Path.Contains("/api/categories/") || !request.Path.Split('/').Last().All(char.IsDigit))
                {
                    var response = new Response { Status = "4 Bad Request" };
                    WriteToStream(stream, ToJson(response));
                    return;
                }

                // Additional logic for delete can be added here
            }

            // // Check for valid category ID (if applicable)
            // if (request.Path.StartsWith("/api/categories/"))
            // {
            //     var parts = request.Path.Split('/');
            //     if (parts.Length > 2) // Ensure there's an ID provided
            //     {
            //         var idPart = parts[2]; // This should be the ID
            //         if (!int.TryParse(idPart, out int categoryId) || !IsValidCategoryId(categoryId))
            //         {
            //             var response = new Response { Status = "5 not found" }; // Invalid category ID
            //             WriteToStream(stream, ToJson(response));
            //             return;
            //         }
            //     }
            // }


            // Handle update logic
           // Handle update logic
if (request.Method == "update" && request.Path.StartsWith("/api/categories/"))
{
    // Extract the ID from the path
    var parts = request.Path.Split('/');
    if (parts.Length < 3 || !int.TryParse(parts[^1], out int categoryId))
    {
        var response = new Response { Status = "5 not found", Body = "Invalid category ID" };
        WriteToStream(stream, ToJson(response));
        return;
    }

    // Parse the request body as JSON to extract update info
    using (JsonDocument doc = JsonDocument.Parse(request.Body))
    {
        if (doc.RootElement.TryGetProperty("cid", out JsonElement cidElement) &&
            cidElement.TryGetInt32(out int bodyCid) &&
            doc.RootElement.TryGetProperty("name", out JsonElement nameElement))
        {
            string updatedName = nameElement.GetString();

            // Check if the cid from the body matches the categoryId from the path
            if (bodyCid != categoryId)
            {
                var response = new Response { Status = "5 not found", Body = "Category ID mismatch" };
                WriteToStream(stream, ToJson(response));
                return;
            }

            // Simulate categories as an in-memory list (this would normally be a database)
            var categories = new List<Category>
            {
                new Category { cid = 1, Name = "Beverages" },
                new Category { cid = 2, Name = "Condiments" },
                new Category { cid = 3, Name = "Confections" }
            };

            // Find the category by ID
            var category = categories.FirstOrDefault(c => c.cid == categoryId);

            if (category != null)
            {
                // Update the category name
                category.Name = updatedName; // Update with the new name from the request

                // Respond with the updated category
                var response = new Response
                {
                    Status = "3 updated",
                    Body = ToJson(new
                    {
                        cid = category.cid,            // Keep the cid as is
                        name = updatedName              // Use the updated name directly from the request
                    })
                };
                WriteToStream(stream, ToJson(response));
            }
            else
            {
                // Handle case where category is not found
                var errorResponse = new Response
                {
                    Status = "5 not found",
                    Body = $"Category with ID {categoryId} not found"
                };
                WriteToStream(stream, ToJson(errorResponse));
            }
        }
        else
        {
            // If either "cid" or "name" is missing in the request body
            var response = new Response { Status = "4 Bad Request", Body = "Invalid request body" };
            WriteToStream(stream, ToJson(response));
        }
    }
}

        }

        catch

        {
        }
    }


    private bool IsValidCategoryId(int categoryId)
    {
        // Define the valid category IDs
        var validCategoryIds = new List<int> { 1, 2, 3 }; // Existing category IDs
        return validCategoryIds.Contains(categoryId);
    }

    private bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        json = json.Trim();

        // Simple checks for basic JSON format
        return (json.StartsWith("{") && json.EndsWith("}")) ||
               (json.StartsWith("[") && json.EndsWith("]"));
    }

    private bool IsValidUnixTimestamp(string timestamp)
    {
        if (string.IsNullOrEmpty(timestamp))
            return false;

        return long.TryParse(timestamp, out var result) && result >= 0;
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
        stream.Flush();
    }

    // Converting the response to JSON 
    // Overload for any object
    public static string ToJson(object obj)
    {
        return JsonSerializer.Serialize(obj,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }


    // Converting the Request from JSON
    public static Request? FromJson(string element)
    {
        return JsonSerializer.Deserialize<Request>(element,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}