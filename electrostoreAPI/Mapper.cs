using AutoMapper;

using electrostore.Models;
using electrostore.Dto;

namespace electrostore;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateBoxDto, Boxs>();
        CreateMap<Boxs, ReadBoxDto>();
        CreateMap<Boxs, ReadExtendedBoxDto>();

        CreateMap<CreateBoxTagDto, BoxsTags>();
        CreateMap<BoxsTags, ReadBoxTagDto>();
        CreateMap<BoxsTags, ReadExtendedBoxTagDto>()
            .ForMember(dest => dest.box, opt => opt.MapFrom(src => src.Box))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));

        CreateMap<CreateCameraDto, Cameras>();
        CreateMap<Cameras, ReadCameraDto>();

        CreateMap<CreateCommandCommentaireDto, CommandsCommentaires>();
        CreateMap<CommandsCommentaires, ReadCommandCommentaireDto>();
        CreateMap<CommandsCommentaires, ReadExtendedCommandCommentaireDto>()
            .ForMember(dest => dest.command, opt => opt.MapFrom(src => src.Command))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));

        CreateMap<CreateCommandDocumentDto, CommandsDocuments>();
        CreateMap<CommandsDocuments, ReadCommandDocumentDto>();

        CreateMap<CreateCommandDto, Commands>();
        CreateMap<Commands, ReadCommandDto>();
        CreateMap<Commands, ReadExtendedCommandDto>();

        CreateMap<CreateCommandItemDto, CommandsItems>();
        CreateMap<CommandsItems, ReadCommandItemDto>();
        CreateMap<CommandsItems, ReadExtendedCommandItemDto>()
            .ForMember(dest => dest.command, opt => opt.MapFrom(src => src.Command))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));

        CreateMap<CreateIADto, IA>();
        CreateMap<IA, ReadIADto>();

        CreateMap<CreateImgDto, Imgs>();
        CreateMap<Imgs, ReadImgDto>();

        CreateMap<CreateItemBoxDto, ItemsBoxs>();
        CreateMap<ItemsBoxs, ReadItemBoxDto>();
        CreateMap<ItemsBoxs, ReadExtendedItemBoxDto>()
            .ForMember(dest => dest.box, opt => opt.MapFrom(src => src.Box))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));

        CreateMap<CreateItemDocumentDto, ItemsDocuments>();
        CreateMap<ItemsDocuments, ReadItemDocumentDto>();

        CreateMap<CreateItemDto, Items>();
        CreateMap<Items, ReadItemDto>();
        CreateMap<Items, ReadExtendedItemDto>();

        CreateMap<CreateItemTagDto, ItemsTags>();
        CreateMap<ItemsTags, ReadItemTagDto>();
        CreateMap<ItemsTags, ReadExtendedItemTagDto>()
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));

        CreateMap<JwiAccessTokens, ReadAccessTokenDto>();

        CreateMap<JwiRefreshTokens, ReadRefreshTokenDto>();

        CreateMap<CreateLedDto, Leds>();
        CreateMap<Leds, ReadLedDto>();

        CreateMap<CreateProjetCommentaireDto, ProjetsCommentaires>();
        CreateMap<ProjetsCommentaires, ReadProjetCommentaireDto>();
        CreateMap<ProjetsCommentaires, ReadExtendedProjetCommentaireDto>()
            .ForMember(dest => dest.projet, opt => opt.MapFrom(src => src.Projet))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));

        CreateMap<CreateProjetDocumentDto, ProjetsDocuments>();
        CreateMap<ProjetsDocuments, ReadProjetDocumentDto>();

        CreateMap<CreateProjetDto, Projets>();
        CreateMap<Projets, ReadProjetDto>();
        CreateMap<Projets, ReadExtendedProjetDto>();

        CreateMap<CreateProjetItemDto, ProjetsItems>();
        CreateMap<ProjetsItems, ReadProjetItemDto>();
        CreateMap<ProjetsItems, ReadExtendedProjetItemDto>()
            .ForMember(dest => dest.projet, opt => opt.MapFrom(src => src.Projet))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));

        CreateMap<CreateProjetTagDto, ProjetTags>();
        CreateMap<ProjetTags, ReadProjetTagDto>();
        CreateMap<ProjetTags, ReadExtendedProjetTagDto>();

        CreateMap<CreateProjetProjetTagDto, ProjetsProjetTags>();
        CreateMap<ProjetsProjetTags, ReadProjetProjetTagDto>();
        CreateMap<ProjetsProjetTags, ReadExtendedProjetProjetTagDto>()
            .ForMember(dest => dest.projet_tag, opt => opt.MapFrom(src => src.ProjetTag))
            .ForMember(dest => dest.projet, opt => opt.MapFrom(src => src.Projet));

        CreateMap<CreateStoreDto, Stores>();
        CreateMap<Stores, ReadStoreDto>();
        CreateMap<Stores, ReadExtendedStoreDto>();

        CreateMap<CreateStoreTagDto, StoresTags>();
        CreateMap<StoresTags, ReadStoreTagDto>();
        CreateMap<StoresTags, ReadExtendedStoreTagDto>()
            .ForMember(dest => dest.store, opt => opt.MapFrom(src => src.Store))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));

        CreateMap<CreateTagDto, Tags>();
        CreateMap<Tags, ReadTagDto>();
        CreateMap<Tags, ReadExtendedTagDto>();

        CreateMap<CreateUserDto, Users>();
        CreateMap<Users, ReadUserDto>();
        CreateMap<Users, ReadExtendedUserDto>();
    }
}