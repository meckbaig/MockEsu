﻿using AutoMapper;
using FluentValidation;
using MediatR;
using ProjectName.Application.Common.BaseRequests;

namespace ProjectName.Application.Services.FeatureName;

public record ServiceMethodQuery : BaseRequest<ServiceMethodResponse>
{

}

public class ServiceMethodResponse : BaseResponse
{
    private class Mapping : Profile
    {
        public Mapping()
        {
            //CreateMap<ServiceMethodItem, ServiceMethodResponse>();
        }
    }
}

public class ServiceMethodQueryValidator : AbstractValidator<ServiceMethodQuery>
{
    public ServiceMethodQueryValidator()
    {
        
    }
}

public class ServiceMethodQueryHandler : IRequestHandler<ServiceMethodQuery, ServiceMethodResponse>
{
    //private readonly IAppDbContext _context;
    //private readonly IMapper _mapper;

    public ServiceMethodQueryHandler()//(IAppDbContext context, IMapper mapper)
    {
        //_context = context;
        //_mapper = mapper;
    }

    public async Task<ServiceMethodResponse> Handle(ServiceMethodQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
