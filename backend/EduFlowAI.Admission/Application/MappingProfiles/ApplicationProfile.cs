using AutoMapper;
using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Domain.Entities;
using System;

namespace EduFlowAI.Admission.Application.MappingProfiles
{
    public class ApplicationProfile : Profile
    {
        public ApplicationProfile()
        {
            CreateMap<Domain.Entities.Application, ApplicationDto>();
            //.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            //.ForMember(dest => dest.Preferences, opt => opt.MapFrom(src => src.Preferences));

            CreateMap<ApplicationPreference, PreferenceDto>();

            CreateMap<ApplicationRequestDto, Domain.Entities.Application>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ApplicantUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore());

            // Needed to map the list inside the Request Dto
            CreateMap<PreferenceDto, ApplicationPreference>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ApplicationId, opt => opt.Ignore())
                .ForMember(dest => dest.Application, opt => opt.Ignore());
        }
    }
}
