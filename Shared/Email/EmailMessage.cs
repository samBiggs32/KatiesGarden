namespace KatiesGarden.Models.Email;

public record EmailMessage(
    string FromAddress,
    string FromName,
    string ToAddress,
    string ToName,
    string ReplyToAddress,
    string ReplyToName,
    string Subject,
    string BodyText);
