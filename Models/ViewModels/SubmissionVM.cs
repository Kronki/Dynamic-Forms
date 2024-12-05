namespace BiznesiImTest.Models.ViewModels
{
    public class SubmissionVM
    {
        public int FormId { get; set; } // Foreign Key
        public string Data { get; set; } = string.Empty; // Submitted data in JSON format
    }
}
