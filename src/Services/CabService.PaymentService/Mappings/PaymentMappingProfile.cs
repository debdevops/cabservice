using AutoMapper;
using CabService.PaymentService.DTOs;
using CabService.Shared.Models;

namespace CabService.PaymentService.Mappings;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.Amount + (src.TipAmount ?? 0)));
        
        CreateMap<ProcessPaymentDto, Payment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        
        CreateMap<PaymentBreakdown, PaymentBreakdownDto>();
        CreateMap<PaymentBreakdownDto, PaymentBreakdown>();
        
        CreateMap<RefundInfo, RefundInfoDto>();
        CreateMap<RefundInfoDto, RefundInfo>();
        
        CreateMap<RefundInfo, RefundDto>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentId, opt => opt.Ignore());
    }
}
