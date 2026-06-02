using AutoMapper;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SportsLeague.API.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Customer mappings
            CreateMap<Customer, CustomerResponseDTO>()
                .ForMember(dest => dest.TotalReservations,
                    opt => opt.MapFrom(src => src.Reservations.Count));
            CreateMap<CustomerRequestDTO, Customer>();

            // Table mappings
            CreateMap<Table, TableResponseDTO>()
                .ForMember(dest => dest.RestaurantName,
                    opt => opt.MapFrom(src => src.Restaurant.Name))
                .ForMember(dest => dest.IsAvailable,
                    opt => opt.MapFrom(src => src.Status == SistemaRestaurante.Domain.Enums.TableStatus.Available));
            CreateMap<TableRequestDTO, Table>();

            // MenuItem mappings
            CreateMap<MenuItem, MenuItemResponseDTO>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category.ToString()));

            // Reservation mappings
            CreateMap<Reservation, ReservationResponseDTO>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => $"{src.Customer.FirstName} {src.Customer.LastName}"))
                .ForMember(dest => dest.TableNumber,
                    opt => opt.MapFrom(src => src.Table.TableNumber))
                .ForMember(dest => dest.OrderedItems,
                    opt => opt.MapFrom(src => src.ReservationMenuItems.Select(rm => new MenuItemOrderDTO
                    {
                        Name = rm.MenuItem.Name,
                        Quantity = rm.Quantity,
                        UnitPrice = rm.UnitPrice,
                        Subtotal = rm.UnitPrice * rm.Quantity
                    })));
        }
    }
}
