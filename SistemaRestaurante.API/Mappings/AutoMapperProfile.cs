using AutoMapper;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Services;

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
                    opt => opt.MapFrom(src => src.Category.ToString()))
                .ForMember(dest => dest.StationName,
                    opt => opt.MapFrom(src => src.Station != null ? src.Station.Name : string.Empty))
                .ForMember(dest => dest.Allergens,
                    opt => opt.MapFrom(src => AllergenNames(src.Allergens)));
            CreateMap<MenuItemRequestDTO, MenuItem>()
                .ForMember(dest => dest.StationId, opt => opt.MapFrom(src => src.StationId ?? 0))
                .ForMember(dest => dest.Station, opt => opt.Ignore())
                .ForMember(dest => dest.Allergens, opt => opt.MapFrom(src => CombineAllergens(src.Allergens)));

            // Zonas y comandas
            CreateMap<Station, StationResponseDTO>();
            CreateMap<StationRequestDTO, Station>();

            CreateMap<OrderItem, OrderItemResponseDTO>()
                .ForMember(dest => dest.StationName,
                    opt => opt.MapFrom(src => src.Station != null ? src.Station.Name : string.Empty));

            CreateMap<Order, OrderResponseDTO>()
                .ForMember(dest => dest.TableNumber, opt => opt.MapFrom(src => src.Table != null ? src.Table.TableNumber : 0))
                .ForMember(dest => dest.WaiterName, opt => opt.MapFrom(src => src.Waiter != null ? src.Waiter.FullName : string.Empty))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items.Where(i => i.IsActive).OrderBy(i => i.Course).ThenBy(i => i.Id)))
                .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal ?? src.Items
                    .Where(i => i.IsActive && i.Status != OrderItemStatus.Draft && i.Status != OrderItemStatus.Cancelled)
                    .Sum(i => i.UnitPrice * i.Quantity)));

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

        /// <summary>Nombres de los alérgenos marcados (p. ej. ["Gluten", "Milk"]).</summary>
        public static List<string> AllergenNames(Allergens allergens) =>
            Enum.GetValues<Allergens>()
                .Where(a => a != Allergens.None && allergens.HasFlag(a))
                .Select(a => a.ToString())
                .ToList();

        public static Allergens CombineAllergens(IEnumerable<Allergens>? allergens) =>
            allergens?.Aggregate(Allergens.None, (all, a) => all | a) ?? Allergens.None;

        public static BillResponseDTO ToDto(Bill bill) => new()
        {
            OrderId = bill.Order.Id,
            TableNumber = bill.Order.Table?.TableNumber ?? 0,
            Guests = bill.Order.Guests,
            WaiterName = bill.Order.Waiter?.FullName ?? string.Empty,
            OpenedAt = bill.Order.OpenedAt,
            CurrencyCode = bill.CurrencyCode,
            // Varias rondas del mismo plato al mismo precio se ven en una sola línea.
            Lines = bill.Lines
                .GroupBy(i => new { i.MenuItemId, i.Name, i.UnitPrice })
                .Select(g => new BillLineDTO
                {
                    Name = g.Key.Name,
                    UnitPrice = g.Key.UnitPrice,
                    Quantity = g.Sum(i => i.Quantity),
                    LineTotal = g.Sum(i => i.LineTotal),
                })
                .ToList(),
            Subtotal = bill.Subtotal,
            SuggestedTipPercent = bill.SuggestedTipPercent,
            SuggestedTip = bill.SuggestedTip,
            TotalWithSuggestedTip = bill.TotalWithSuggestedTip,
            PendingItems = bill.Lines.Count(i => i.Status != OrderItemStatus.Served),
        };

        /// <summary>Agrupa los platos de cocina en comandas: una por cuenta y zona.</summary>
        public static List<KitchenTicketDTO> ToTickets(IEnumerable<OrderItem> items) =>
            items
                .GroupBy(i => new { i.OrderId, i.StationId })
                .Select(g =>
                {
                    var first = g.First();
                    return new KitchenTicketDTO
                    {
                        OrderId = first.OrderId,
                        TableNumber = first.Order?.Table?.TableNumber ?? 0,
                        WaiterName = first.Order?.Waiter?.FullName ?? string.Empty,
                        Guests = first.Order?.Guests ?? 0,
                        StationId = first.StationId,
                        StationName = first.Station?.Name ?? string.Empty,
                        FirstSentAt = g.Min(i => i.SentAt ?? i.CreatedAt),
                        Items = g.OrderBy(i => i.Course).ThenBy(i => i.Id).Select(i => new KitchenTicketItemDTO
                        {
                            Id = i.Id,
                            Name = i.Name,
                            Quantity = i.Quantity,
                            Notes = i.Notes,
                            Course = i.Course,
                            Status = i.Status,
                            SentAt = i.SentAt,
                        }).ToList(),
                    };
                })
                .OrderBy(t => t.FirstSentAt)
                .ToList();
    }
}
