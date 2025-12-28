using AutoMapper;
using Restoran.BLL.DTOs;
using Restoran.DAL.Entities;

namespace Restoran.BLL.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Entitet → DTO
            CreateMap<Rezervacija, RezervacijaDto>()
                .ForMember(d => d.ImeGosta, opt => opt.MapFrom(s => s.Gost.ImeGosta))
                .ForMember(d => d.PrezimeGosta, opt => opt.MapFrom(s => s.Gost.PrezimeGosta))
                .ForMember(d => d.BrojStola, opt => opt.MapFrom(s => s.Sto.BrojStola))
                .ForMember(d => d.NarudzbinaProizvod,
                           opt => opt.MapFrom(s => s.Narudzbina == null
                                                   ? "Nema narudžbine"
                                                   : s.Narudzbina.Proizvod));

            // DTO → Entitet (sve što šalješ iz forme)
            CreateMap<RezervacijaDto, Rezervacija>()
                .ForMember(e => e.Narudzbina, opt => opt.Ignore());   // ignoriraj nav-property
        }
    }
}