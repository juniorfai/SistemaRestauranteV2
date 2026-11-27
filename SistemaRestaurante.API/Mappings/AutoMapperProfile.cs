using AutoMapper;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.API.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User (nunca se expone el hash ni el SecurityStamp)
            CreateMap<User, UserResponseDTO>()
                .ForMember(dest => dest.RestaurantName,
                    opt => opt.MapFrom(src => src.Restaurant != null ? src.Restaurant.Name : null));

            // Restaurant
            CreateMap<Restaurant, RestaurantResponseDTO>();
            CreateMap<RestaurantRequestDTO, Restaurant>();
            CreateMap<RestaurantCreateRequestDTO, Restaurant>();

            // Customer
            CreateMap<Customer, CustomerResponseDTO>()
                .ForMember(dest => dest.TotalReservations,
                    opt => opt.MapFrom(src => src.Reservations.Count));
            CreateMap<CustomerRequestDTO, Customer>();

            // Table
            CreateMap<Table, TableResponseDTO>()
                .ForMember(dest => dest.RestaurantName,
                    opt => opt.MapFrom(src => src.Restaurant != null ? src.Restaurant.Name : string.Empty))
                .ForMember(dest => dest.IsAvailable,
                    opt => opt.MapFrom(src => src.Status == TableStatus.Available));
            CreateMap<TableRequestDTO, Table>();

            // MenuItem
            CreateMap<MenuItem, MenuItemResponseDTO>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category.ToString()));
            CreateMap<MenuItemRequestDTO, MenuItem>();

            // Reservation
            CreateMap<ReservationRequestDTO, Reservation>()
                .ForMember(dest => dest.ReservationDateTime,
                    opt => opt.MapFrom(src => src.ReservationDateTime ?? default))
                .ForMember(dest => dest.ReservationMenuItems, opt => opt.Ignore());

            CreateMap<Reservation, ReservationResponseDTO>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}" : string.Empty))
                .ForMember(dest => dest.CustomerPhone,
                    opt => opt.MapFrom(src => src.Customer != null ? src.Customer.PhoneNumber : string.Empty))
                .ForMember(dest => dest.TableNumber,
                    opt => opt.MapFrom(src => src.Table != null ? src.Table.TableNumber : 0))
                .ForMember(dest => dest.TableCapacity,
                    opt => opt.MapFrom(src => src.Table != null ? src.Table.Capacity : 0))
                .ForMember(dest => dest.OrderedItems,
                    opt => opt.MapFrom(src => src.ReservationMenuItems.Select(rm => new MenuItemOrderDTO
                    {
                        Name = rm.MenuItem != null ? rm.MenuItem.Name : string.Empty,
                        Quantity = rm.Quantity,
                        UnitPrice = rm.UnitPrice,
                        Subtotal = rm.UnitPrice * rm.Quantity
                    })));
        }
    }
}
