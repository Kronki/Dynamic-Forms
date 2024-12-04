using AutoMapper;
using BiznesiImTest.Models;
using BiznesiImTest.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

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
            //var formFields = _mapper.Map<List<FormField>>(model.FormFields);
            var form = _mapper.Map<Form>(model.Form);
            var formFields = model.FormFields;
            //var form = model.Form;
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

            // Group fields by rows for rendering
            var groupedFields = form.Fields
                .OrderBy(f => f.Row)
                .GroupBy(f => f.Row)
                .ToList();

            // Start building the HTML for the form
            var formHtml = $"<h1>{form.Title}</h1><p>{form.Description}</p><form>";

            foreach (var row in groupedFields)
            {
                formHtml += "<div class='row'>"; // Start a new row

                foreach (var field in row)
                {
                    var colSpan = field.ColumnSpan * 6; // Assuming a 12-column grid (e.g., Bootstrap)
                    formHtml += $"<div class='col-md-{colSpan}'>";

                    // Add field label
                    formHtml += $"<label for='{field.Name}'>{field.Label}</label>";

                    // Handle different field types
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
                                formHtml += $"<option value='' disabled selected>Select {field.Label}</option>";
                                foreach (var option in options)
                                {
                                    formHtml += $"<option value='{option}'>{option}</option>";
                                }
                                formHtml += "</select>";
                            }
                            break;

                        default: // Handle text, email, etc.
                            formHtml += $@"
                        <input type='{field.Type}' id='{field.Name}' name='{field.Name}' {(field.IsRequired ? "required" : "")} />";
                            break;
                    }

                    formHtml += "</div>"; // Close column
                }

                formHtml += "</div>"; // Close row
            }

            // Add a submit button
            formHtml += "<button type='submit'>Submit</button></form>";
            return Ok(formHtml);
        }
    }
}
