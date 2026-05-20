using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;

namespace PDV.Application.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Category, CategoryDto>().ReverseMap();
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier != null ? s.Supplier.Name : null));
        CreateMap<CreateProductDto, Product>();

        CreateMap<Customer, CustomerDto>().ReverseMap();
        CreateMap<CreateCustomerDto, Customer>();

        CreateMap<Supplier, SupplierDto>().ReverseMap();
        CreateMap<CreateSupplierDto, Supplier>();

        CreateMap<Sale, SaleDto>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer != null ? s.Customer.Name : null))
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User != null ? s.User.FullName : null));
        CreateMap<SaleItem, SaleItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : null))
            .ForMember(d => d.Barcode, o => o.MapFrom(s => s.Product != null ? s.Product.Barcode : null))
            .ForMember(d => d.UnitOfMeasure, o => o.MapFrom(s => s.Product != null ? s.Product.UnitOfMeasure : default));
        CreateMap<Payment, PaymentDto>().ReverseMap();

        CreateMap<StockMovement, StockMovementDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : null));

        CreateMap<CashSession, CashSessionDto>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User != null ? s.User.FullName : null));
        CreateMap<CashMovement, CashMovementDto>();

        CreateMap<User, UserDto>();
        CreateMap<CreateUserDto, User>();
    }
}
