namespace Server;

public class Response
{
    public string Status { get; set; }
    public string Body { get; set; }

    public Response(String status)
    {
        Status = status;
    }
    /*
    public Response(String status, String body)
    {
        Status = status;
        Body = body;
    }*/
}