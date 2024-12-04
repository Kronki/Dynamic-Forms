namespace BiznesiImTest.Models.ViewModels
{
    public class FormFieldVM
    {
        public string Name { get; set; } = string.Empty; // Field's unique identifier
        public string Label { get; set; } = string.Empty; // Field label
        public string Type { get; set; } = string.Empty; // Field type (e.g., "text", "radio", "email")
        public bool IsRequired { get; set; } // Whether the field is required
        public int Row { get; set; } // Row number for layout
        public int ColumnSpan { get; set; } = 1; // Number of columns the field spans
        public string? Options { get; set; } // JSON string for options (e.g., radio button values)
        public int FormId { get; set; } // Foreign Key
    }
}
