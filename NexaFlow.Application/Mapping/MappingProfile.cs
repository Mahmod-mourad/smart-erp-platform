using AutoMapper;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;

namespace NexaFlow.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Tenant, TenantDto>();
        CreateMap<TeamInvitation, InvitationDto>();
        CreateMap<Branch, BranchDto>();
    }
}
