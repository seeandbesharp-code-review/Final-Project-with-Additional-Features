using AutoMapper;
using ChineseRaffleApi.Dto;
using ChineseRaffleApi.Models;

namespace ChineseRaffleApi.Mapping
{
    public class TicketProfile : Profile
    {
        public TicketProfile()
        {
            CreateMap<Ticket, GetTicketDto>()
                .ForMember(dest => dest.GiftTitle, opt => opt.MapFrom(src => src.Gift != null ? src.Gift.Title : "No Gift Assigned"))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : "Anonymous"));

            CreateMap<AddTicketDto, Ticket>();
        }
    }
}