using AutoMapper;
using BiznesiImTest.Models.ViewModels;

namespace BiznesiImTest.Models
{
    public class Profiles : Profile
    {
        public Profiles()
        {
            CreateMap<FormFieldVM, FormField>();
            //CreateMap<List<FormFieldVM>, List<FormField>>();
            CreateMap<FormVM, Form>();
        }
    }
}
