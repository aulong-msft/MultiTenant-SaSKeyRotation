namespace RotationPoc.SecretMessage;
/// <summary>
/// EmailMessage data model
/// </summary>
public class SecretMessage
{
    private string CommandPrimaryConnectionString { get; init; }

    private string ResponsePrimaryConnectionString { get; init; }



    public SecretMessage(string command, string response)
    {
        CommandPrimaryConnectionString = command;
        ResponsePrimaryConnectionString = response;
    }
}