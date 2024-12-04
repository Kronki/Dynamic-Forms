namespace BiznesiImTest.Models
{
    public class Submission
    {
        public int Id { get; set; } // Unique ID for submission
        public int FormId { get; set; } // Foreign Key
        public Form Form { get; set; } = default!; // Navigation property
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Time of submission
        public string Data { get; set; } = string.Empty; // Submitted data in JSON format
    }
}
