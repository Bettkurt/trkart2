using AutoMapper;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;

namespace TRKart.Business.Mapping;

public class BusinessMappingProfile : Profile
{
    public BusinessMappingProfile()
    {
        // Wallet mappings
        CreateMap<Wallet, WalletDto>().ReverseMap();
        CreateMap<CreateWalletDto, Wallet>();
        
        // Add other business layer mappings here
    }
}
