using System.ComponentModel.DataAnnotations;

namespace KatiesGarden.Web.Client.Models
{
    public class ContactUsForm
    {        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
        public string ContactNumber { get; set; }
    }
}
