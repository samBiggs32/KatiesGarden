namespace KatiesGarden.Models.Email;

public static class ContactEmailBuilder
{
    public const string FromName = "Katie's Garden Website";
    public const string ToName = "Katie's Garden";
    public const string SubjectPrefix = "[Website Enquiry]";

    public static EmailMessage Build(ContactUsForm form, string senderEmail, string recipientEmail)
    {
        var contactName = $"{form.FirstName} {form.LastName}";
        return new EmailMessage(
            FromAddress: senderEmail,
            FromName: FromName,
            ToAddress: recipientEmail,
            ToName: ToName,
            ReplyToAddress: form.EmailAddress,
            ReplyToName: contactName,
            Subject: $"{SubjectPrefix} {form.EmailSubject}",
            BodyText: $"Dear Katie,\n\n{form.EmailBody}\n\n" +
                      $"--\nMany thanks\n{contactName}" +
                      $"\nEmail: {form.EmailAddress}\nPhone: {form.ContactNumber}");
    }
}
