using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BiznesiImTest.Models
{
    public class Form
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<FormField>? Fields { get; set; } = new List<FormField>();
        public List<Submission>? Submissions { get; set; } = new List<Submission>();
    }
}
