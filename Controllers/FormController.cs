using AutoMapper;
using BiznesiImTest.Models;
using BiznesiImTest.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;

namespace BiznesiImTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FormController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public FormController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        [HttpPost("CreateForm")]
        public async Task<IActionResult> CreateForm([FromBody]NewFormVM model)
        {
            var formFields = new List<FormField>();
            foreach(var formFieldVM in model.FormFields)
            {
                var formField = _mapper.Map<FormField>(formFieldVM);
                formFields.Add(formField);
            }
            //var formFields = _mapper.Map<List<FormField>>(model.FormFields);
            var form = _mapper.Map<Form>(model.Form);
            form.Fields = formFields;
            _context.Forms.Add(form);
            var isSaved = await _context.SaveChangesAsync();
            return Ok(isSaved);
        }
        [HttpPost("CreateFormField")]
        public async Task<IActionResult> CreateFormField([FromBody] FormFieldVM model)
        {
            var formField = _mapper.Map<FormField>(model);
            _context.FormFields.Add(formField);
            var isSaved = await _context.SaveChangesAsync();
            return Ok(isSaved);
        }
        [HttpGet("RenderForm/{formId}")]
        public async Task<IActionResult> RenderForm(int formId)
        {
            var form = await _context.Forms
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == formId);

            if (form == null)
            {
                throw new Exception("Form not found.");
            }

            var groupedFields = form.Fields
                .OrderBy(f => f.Row)
                .GroupBy(f => f.Row)
                .ToList();

            var formHtml = $"<h1>{form.Title}</h1><p>{form.Description}</p><form id='{form.Id}'>";

            foreach (var row in groupedFields)
            {
                formHtml += "<div class='row'>";

                foreach (var field in row)
                {
                    var colSpan = field.ColumnSpan;
                    formHtml += $"<div class='col-md-{colSpan}'>";

                    // Add red asterisk for required fields
                    var requiredMarker = field.IsRequired ? "<span style='color: red;'>*</span>" : "";

                    formHtml += $"<label for='{field.Name}'>{field.Label} {requiredMarker}</label>";

                    switch (field.Type.ToLower())
                    {
                        case "radio":
                            if (!string.IsNullOrEmpty(field.Options))
                            {
                                var options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(field.Options);
                                foreach (var option in options)
                                {
                                    formHtml += $@"
                        <div>
                            <input type='radio' id='{field.Name}_{option}' name='{field.Name}' value='{option}' />
                            <label for='{field.Name}_{option}'>{option}</label>
                        </div>";
                                }
                            }
                            break;

                        case "select":
                            if (!string.IsNullOrEmpty(field.Options))
                            {
                                var options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(field.Options);
                                formHtml += $"<select id='{field.Name}' name='{field.Name}' {(field.IsRequired ? "required" : "")}>";

                                if (!options.Contains("multiple"))
                                {
                                    formHtml += $"<option value='' disabled selected>Select {field.Label}</option>";
                                }

                                foreach (var option in options)
                                {
                                    formHtml += $"<option value='{option}'>{option}</option>";
                                }
                                formHtml += "</select>";
                            }
                            break;

                        case "checkbox":
                            if (!string.IsNullOrEmpty(field.Options))
                            {
                                var options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(field.Options);
                                foreach (var option in options)
                                {
                                    formHtml += $@"
                        <div>
                            <input type='checkbox' id='{field.Name}_{option}' name='{field.Name}' value='{option}' />
                            <label for='{field.Name}_{option}'>{option}</label>
                        </div>";
                                }
                            }
                            break;

                        default:
                            formHtml += $@"
                <input type='{field.Type}' 
                       id='{field.Name}' 
                       name='{field.Name}' 
                       {(field.IsRequired ? "required" : "")} 
                       data-regex='{field.RegexPattern}' 
                       data-validation-message='{field.ValidationMessage}' />";
                            break;
                    }

                    formHtml += "</div>";
                }

                formHtml += "</div>";
            }

            formHtml += "<button type='submit'>Submit</button></form>";
            return Ok(formHtml);
        }


        [HttpPost("CreateSubmission")]
        public async Task<IActionResult> CreateSubmission([FromBody] SubmissionVM model)
        {
            // Retrieve the form and its fields
            var form = await _context.Forms
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == model.FormId);

            if (form == null)
            {
                return BadRequest("Form not found.");
            }

            // Parse the submitted data (stored as JSON)
            var submissionData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(model.Data);

            // Validate each field
            foreach (var field in form.Fields)
            {
                if (!submissionData.ContainsKey(field.Name))
                {
                    if (field.IsRequired)
                    {
                        return Ok($"{field.Label} is required.");
                    }
                    continue; // Skip validation for optional fields
                }

                // Retrieve the value for the current field
                var fieldValue = submissionData[field.Name];

                if (field.Type.ToLower() == "select")
                {
                    // Check if the value is single or multiple
                    if (fieldValue is JsonElement jsonElement)
                    {
                        if (jsonElement.ValueKind == JsonValueKind.Array)
                        {
                            // Handle multi-select
                            var selectedOptions = jsonElement.EnumerateArray().Select(x => x.GetString()).ToList();

                            if (field.IsRequired && selectedOptions.Count == 0)
                            {
                                return Ok($"{field.Label} requires at least one selection.");
                            }

                            var validOptions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(field.Options);
                            if (selectedOptions.Any(option => !validOptions.Contains(option)))
                            {
                                return Ok($"Invalid selection(s) for {field.Label}. Allowed options: {string.Join(", ", validOptions)}");
                            }
                        }
                        else if (jsonElement.ValueKind == JsonValueKind.String)
                        {
                            // Handle single-select
                            var value = jsonElement.GetString();
                            var validOptions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(field.Options);

                            if (!validOptions.Contains(value))
                            {
                                return Ok($"Invalid selection for {field.Label}. Allowed options: {string.Join(", ", validOptions)}");
                            }
                        }
                        else
                        {
                            return Ok($"Invalid data for {field.Label}. Expected a string or an array.");
                        }
                    }
                    else
                    {
                        return Ok($"Invalid data type for {field.Label}.");
                    }
                }
                else if (field.Type.ToLower() == "checkbox")
                {
                    // Handle checkboxes: Ensure the value is an array
                    if (fieldValue is not JsonElement jsonCheckbox || jsonCheckbox.ValueKind != JsonValueKind.Array)
                    {
                        return Ok($"Invalid data for {field.Label}. Expected multiple selections.");
                    }

                    // Validate checkbox selections
                    var selectedOptions = jsonCheckbox.EnumerateArray().Select(x => x.GetString()).ToList();

                    if (field.IsRequired && selectedOptions.Count == 0)
                    {
                        return Ok($"{field.Label} requires at least one selection.");
                    }

                    var validOptions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(field.Options);
                    if (selectedOptions.Any(option => !validOptions.Contains(option)))
                    {
                        return Ok($"Invalid selection(s) for {field.Label}. Allowed options: {string.Join(", ", validOptions)}");
                    }
                }
                else
                {
                    // Handle other field types (e.g., text, radio)
                    var value = fieldValue.ToString();
                    if (!string.IsNullOrEmpty(field.RegexPattern))
                    {
                        var isMatch = System.Text.RegularExpressions.Regex.IsMatch(value, field.RegexPattern);
                        if (!isMatch)
                        {
                            return Ok(field.ValidationMessage ?? $"Invalid value for {field.Label}.");
                        }
                    }
                }
            }

            // Save submission
            var submission = _mapper.Map<Submission>(model);
            _context.Submissions.Add(submission);
            var isSaved = await _context.SaveChangesAsync();
            return Ok(isSaved);
        }


        [HttpGet("RenderSubmissions/{formId}")]
        public async Task<IActionResult> RenderSubmissions(int formId)
        {
            // Retrieve the form and its submissions
            var form = await _context.Forms
                .Include(f => f.Fields)
                .Include(f => f.Submissions)
                .FirstOrDefaultAsync(f => f.Id == formId);

            if (form == null)
            {
                return BadRequest("Form not found.");
            }

            // Start building the HTML
            var html = $@"
                <h1 style='color: #333; font-family: Arial, sans-serif;'>Submissions for {form.Title}</h1>
                <p style='font-size: 14px; color: #555; font-family: Arial, sans-serif;'>{form.Description}</p>";

            if (!form.Submissions.Any())
            {
                html += "<p style='color: #888; font-family: Arial, sans-serif;'>No submissions found.</p>";
                return Content(html, "text/html");
            }

            html += @"
                <table style='border-collapse: collapse; width: 100%; margin-top: 20px; font-family: Arial, sans-serif;'>
                <thead>
                <tr style='background-color: #f4f4f4;'>";

            // Render table headers
            foreach (var field in form.Fields)
            {
                html += $"<th style='border: 1px solid #ddd; padding: 8px; text-align: left;'>{field.Label}</th>";
            }
            html += "<th style='border: 1px solid #ddd; padding: 8px; text-align: left;'>Submitted On</th></tr></thead><tbody>";

            // Render table rows
            foreach (var submission in form.Submissions)
            {
                // Deserialize the submission data as a Dictionary<string, object>
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(submission.Data);
                html += "<tr style='border: 1px solid #ddd;'>";

                foreach (var field in form.Fields)
                {
                    if (data.ContainsKey(field.Name))
                    {
                        // Handle checkbox inputs as arrays
                        if (field.Type.ToLower() == "checkbox" && data[field.Name] is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                        {
                            var selectedOptions = jsonElement.EnumerateArray().Select(x => x.GetString()).ToList();
                            html += $"<td style='border: 1px solid #ddd; padding: 8px;'>{string.Join(", ", selectedOptions)}</td>";
                        }
                        else
                        {
                            html += $"<td style='border: 1px solid #ddd; padding: 8px;'>{data[field.Name]}</td>";
                        }
                    }
                    else
                    {
                        html += $"<td style='border: 1px solid #ddd; padding: 8px;'>N/A</td>";
                    }
                }

                html += $"<td style='border: 1px solid #ddd; padding: 8px;'>{submission.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")}</td>";
                html += "</tr>";
            }

            html += "</tbody></table>";
            return Ok(html);
        }
    }
}
