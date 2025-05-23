using AutoMapper;

using electrostore.Models;
using electrostore.Dto;

namespace electrostore;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Boxs, ReadBoxDto>();
        CreateMap<Boxs, ReadExtendedBoxDto>();

        CreateMap<BoxsTags, ReadBoxTagDto>();
        CreateMap<BoxsTags, ReadExtendedBoxTagDto>()
            .ForMember(dest => dest.box, opt => opt.MapFrom(src => src.Box))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));

        CreateMap<Cameras, ReadCameraDto>();

        CreateMap<CommandsCommentaires, ReadCommandCommentaireDto>();
        CreateMap<CommandsCommentaires, ReadExtendedCommandCommentaireDto>()
            .ForMember(dest => dest.command, opt => opt.MapFrom(src => src.Command))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));

        CreateMap<CommandsDocuments, ReadCommandDocumentDto>();

        CreateMap<Commands, ReadCommandDto>();
        CreateMap<Commands, ReadExtendedCommandDto>();

        CreateMap<CommandsItems, ReadCommandItemDto>();
        CreateMap<CommandsItems, ReadExtendedCommandItemDto>()
            .ForMember(dest => dest.command, opt => opt.MapFrom(src => src.Command))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));

        CreateMap<IA, ReadIADto>();

        CreateMap<Imgs, ReadImgDto>();

        CreateMap<ItemsBoxs, ReadItemBoxDto>();
        CreateMap<ItemsBoxs, ReadExtendedItemBoxDto>()
            .ForMember(dest => dest.box, opt => opt.MapFrom(src => src.Box))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));

        CreateMap<ItemsDocuments, ReadItemDocumentDto>();

        CreateMap<Items, ReadItemDto>();
        CreateMap<Items, ReadExtendedItemDto>();

        CreateMap<ItemsTags, ReadItemTagDto>();
        CreateMap<ItemsTags, ReadExtendedItemTagDto>()
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));

        CreateMap<JwiAccessTokens, ReadAccessTokenDto>();

        CreateMap<JwiRefreshTokens, ReadRefreshTokenDto>();

        CreateMap<Leds, ReadLedDto>();

        CreateMap<ProjetsCommentaires, ReadProjetCommentaireDto>();
        CreateMap<ProjetsCommentaires, ReadExtendedProjetCommentaireDto>()
            .ForMember(dest => dest.projet, opt => opt.MapFrom(src => src.Projet))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));

        CreateMap<ProjetsDocuments, ReadProjetDocumentDto>();

        CreateMap<Projets, ReadProjetDto>();
        CreateMap<Projets, ReadExtendedProjetDto>();

        CreateMap<ProjetsItems, ReadProjetItemDto>();
        CreateMap<ProjetsItems, ReadExtendedProjetItemDto>()
            .ForMember(dest => dest.projet, opt => opt.MapFrom(src => src.Projet))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));

        CreateMap<Stores, ReadStoreDto>();
        CreateMap<Stores, ReadExtendedStoreDto>();

        CreateMap<StoresTags, ReadStoreTagDto>();
        CreateMap<StoresTags, ReadExtendedStoreTagDto>()
            .ForMember(dest => dest.store, opt => opt.MapFrom(src => src.Store))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));

        CreateMap<Tags, ReadTagDto>();
        CreateMap<Tags, ReadExtendedTagDto>();

        CreateMap<Users, ReadUserDto>();
        CreateMap<Users, ReadExtendedUserDto>();
    }
}