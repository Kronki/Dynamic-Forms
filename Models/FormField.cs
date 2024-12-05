using System.ComponentModel.DataAnnotations;

namespace BiznesiImTest.Models
{
    public class FormField
    {
        [Key]
        public int Id { get; set; } // Primary Key
        public string Name { get; set; } = string.Empty; // Field's unique identifier
        public string Label { get; set; } = string.Empty; // Field label
        public string Type { get; set; } = string.Empty; // Field type (e.g., "text", "radio", "email")
        public bool IsRequired { get; set; } // Whether the field is required
        public int Row { get; set; } // Row number for layout
        public int ColumnSpan { get; set; } = 1; // Number of columns the field spans
        public string? Options { get; set; } // JSON string for options (e.g., radio button values)
        public int FormId { get; set; } // Foreign Key
        public Form? Form { get; set; } = default!; // Navigation property
        public string? RegexPattern { get; set; } // Regex for validation (optional)
        public string? ValidationMessage { get; set; } // Validation message to show on regex failure
    }
}
