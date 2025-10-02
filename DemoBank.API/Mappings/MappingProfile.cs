using AutoMapper;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;

namespace DemoBank.API.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<UserRegistrationDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Account mappings
        CreateMap<Account, AccountDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

        CreateMap<CreateAccountDto, Account>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AccountNumber, opt => opt.Ignore())
            .ForMember(dest => dest.Balance, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Transaction mappings
        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ToAccountNumber, opt => opt.MapFrom(src =>
                src.ToAccount != null ? src.ToAccount.AccountNumber : null));

        // Currency mappings
        CreateMap<Currency, CurrencyDto>();

        // Loan mappings
        CreateMap<Loan, LoanDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // Invoice mappings
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // Notification mappings
        CreateMap<Notification, NotificationDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

        // UserSettings mappings
        CreateMap<UserSettings, UserSettingsDto>();
        CreateMap<UserSettingsDto, UserSettings>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
    }
}