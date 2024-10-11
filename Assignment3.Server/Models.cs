using System.Text.Json;
using System.Text.RegularExpressions;

namespace Assignment3.Server;

record Category(int Cid, string Name);

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

    public void EnsureValidRequest() {
        string errorMsg = "";
        if (string.IsNullOrEmpty(this.Method))
            errorMsg += "Missing method ";
        else if (!this.IsValidMethod())
            errorMsg += "Illegal method";
        if (this.Method != "echo" && string.IsNullOrEmpty(this.Path))
            errorMsg += "Missing resource ";

        if (string.IsNullOrEmpty(this.Date))
                errorMsg += "Missing date ";
        else if (!int.TryParse(this.Date, out int date))
            errorMsg += "Illegal date ";
        else if (date < 0 || date > DateTimeOffset.Now.ToUnixTimeSeconds())
            errorMsg += "Illegal date ";

        if (this.IsBodyRequired() && string.IsNullOrEmpty(this.Body))
            errorMsg += "Missing body ";
        else if (this.Method == "update" && !this.IsValidJson(this.Body))
            errorMsg += "Illegal body ";

        if (errorMsg != "")
            throw new Exception(errorMsg.TrimEnd());
        
        Console.WriteLine("info: Valid request");
    }

    private bool IsValidJson(string? strInput)
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

    public static bool TryParsePathId(Request request, out int? categoryId) {
        categoryId = null;
        string pathPattern = @"^\/api\/categories(\/(?<CategoryId>\d+))?$";
        Match match = Regex.Match(request.Path, pathPattern);
        Group catIdGroup = match.Groups["CategoryId"];

        if (!match.Success)
            return false;
        if (request.IsIdOnPathNotAllowed() && catIdGroup.Success)
            return false;
        if (request.IsIdOnPathRequired() && !catIdGroup.Success)
            return false;

        categoryId = catIdGroup.Success 
            ? int.Parse(catIdGroup.Value) 
            : null;

        return true;
    }
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
