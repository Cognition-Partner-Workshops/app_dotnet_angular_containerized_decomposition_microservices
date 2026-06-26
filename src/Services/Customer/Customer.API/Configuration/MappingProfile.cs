using AutoMapper;
using Customer.API.ViewModels;

namespace Customer.API.Configuration;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Domain.Entities.Customer, CustomerVM>()
            .ForMember(d => d.Gender, opt => opt.MapFrom(s => s.Gender.ToString()));
        CreateMap<CustomerVM, Domain.Entities.Customer>()
            .ForMember(d => d.Gender, opt => opt.MapFrom(s => Enum.Parse<Domain.Entities.Gender>(s.Gender ?? "None", ignoreCase: true)));
    }
}
