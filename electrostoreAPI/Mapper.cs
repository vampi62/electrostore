using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;

namespace ElectrostoreAPI;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateBoxDto, Boxs>();
        CreateMap<Boxs, ReadBoxDto>();
        CreateMap<Boxs, ReadExtendedBoxDto>();
        CreateMap<UpdateBulkBoxByStoreDto, UpdateBoxDto>();

        CreateMap<CreateBoxTagDto, BoxsTags>();
        CreateMap<BoxsTags, ReadBoxTagDto>();
        CreateMap<BoxsTags, ReadExtendedBoxTagDto>()
            .ForMember(dest => dest.box, opt => opt.MapFrom(src => src.Box))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));

        CreateMap<CreateCameraDto, Cameras>();
        CreateMap<Cameras, ReadCameraDto>();

        CreateMap<CreateCarrierDto, Carriers>();
        CreateMap<Carriers, ReadCarrierDto>();

        CreateMap<CreateCommandCommentDto, CommandsComments>();
        CreateMap<CommandsComments, ReadCommandCommentDto>();
        CreateMap<CommandsComments, ReadExtendedCommandCommentDto>()
            .ForMember(dest => dest.command, opt => opt.MapFrom(src => src.Command))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));

        CreateMap<CreateCommandDocumentDto, CommandsDocuments>();
        CreateMap<CommandsDocuments, ReadCommandDocumentDto>();

        CreateMap<CreateCommandDto, Commands>();
        CreateMap<Commands, ReadCommandDto>();
        CreateMap<Commands, ReadExtendedCommandDto>();

        CreateMap<CreateCommandHistoryDto, CommandsHistory>();
        CreateMap<CommandsHistory, ReadCommandHistoryDto>();

        CreateMap<CreateCommandItemDto, CommandsItems>();
        CreateMap<CommandsItems, ReadCommandItemDto>();
        CreateMap<CommandsItems, ReadExtendedCommandItemDto>()
            .ForMember(dest => dest.command, opt => opt.MapFrom(src => src.Command))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));

        CreateMap<CreateCronJobDto, CronJobs>();
        CreateMap<CronJobs, ReadCronJobDto>();

        CreateMap<CreateEquipementDto, Equipements>();
        CreateMap<Equipements, ReadEquipementDto>();
        CreateMap<Equipements, ReadExtendedEquipementDto>();

        CreateMap<CreateEquipementBoxDto, EquipementsBoxs>();
        CreateMap<EquipementsBoxs, ReadEquipementBoxDto>();
        CreateMap<EquipementsBoxs, ReadExtendedEquipementBoxDto>()
            .ForMember(dest => dest.box, opt => opt.MapFrom(src => src.Box))
            .ForMember(dest => dest.equipement, opt => opt.MapFrom(src => src.Equipement));

        CreateMap<CreateEquipementCommentDto, EquipementsComments>();
        CreateMap<EquipementsComments, ReadEquipementCommentDto>();
        CreateMap<EquipementsComments, ReadExtendedEquipementCommentDto>()
            .ForMember(dest => dest.equipement, opt => opt.MapFrom(src => src.Equipement))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));

        CreateMap<CreateEquipementDocumentDto, EquipementsDocuments>();
        CreateMap<EquipementsDocuments, ReadEquipementDocumentDto>();

        CreateMap<CreateEquipementMaintenanceDto, EquipementsMaintenances>();
        CreateMap<EquipementsMaintenances, ReadEquipementMaintenanceDto>();
        CreateMap<EquipementsMaintenances, ReadExtendedEquipementMaintenanceDto>()
            .ForMember(dest => dest.equipement, opt => opt.MapFrom(src => src.Equipement))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));

        CreateMap<EquipementsStatus, ReadEquipementStatusDto>();
        CreateMap<EquipementsStatus, ReadExtendedEquipementStatusDto>()
            .ForMember(dest => dest.equipement, opt => opt.MapFrom(src => src.Equipement));
        CreateMap<CreateEquipementStatusDto, EquipementsStatus>();

        CreateMap<CreateEquipementTagDto, EquipementsTags>();
        CreateMap<EquipementsTags, ReadEquipementTagDto>();
        CreateMap<EquipementsTags, ReadExtendedEquipementTagDto>()
            .ForMember(dest => dest.equipement, opt => opt.MapFrom(src => src.Equipement))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));

        CreateMap<CreateAIDto, AI>();
        CreateMap<AI, ReadAIDto>();

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

        CreateMap<ItemsHistory, ReadItemHistoryDto>();
        CreateMap<ItemsHistory, ReadExtendedItemHistoryDto>()
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item))
            .ForMember(dest => dest.box, opt => opt.MapFrom(src => src.Box))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));

        CreateMap<CreateItemTagDto, ItemsTags>();
        CreateMap<ItemsTags, ReadItemTagDto>();
        CreateMap<ItemsTags, ReadExtendedItemTagDto>()
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));

        CreateMap<JwiAccessTokens, ReadAccessTokenDto>();

        CreateMap<JwiRefreshTokens, ReadRefreshTokenDto>();

        CreateMap<CreateLedDto, Leds>();
        CreateMap<Leds, ReadLedDto>();
        CreateMap<UpdateBulkLedByStoreDto, UpdateLedDto>();

        CreateMap<CreateProjectCommentDto, ProjectsComments>();
        CreateMap<ProjectsComments, ReadProjectCommentDto>();
        CreateMap<ProjectsComments, ReadExtendedProjectCommentDto>()
            .ForMember(dest => dest.project, opt => opt.MapFrom(src => src.Project))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));

        CreateMap<CreateProjectDocumentDto, ProjectsDocuments>();
        CreateMap<ProjectsDocuments, ReadProjectDocumentDto>();

        CreateMap<CreateProjectDto, Projects>();
        CreateMap<Projects, ReadProjectDto>();
        CreateMap<Projects, ReadExtendedProjectDto>();

        CreateMap<CreateProjectItemDto, ProjectsItems>();
        CreateMap<ProjectsItems, ReadProjectItemDto>();
        CreateMap<ProjectsItems, ReadExtendedProjectItemDto>()
            .ForMember(dest => dest.project, opt => opt.MapFrom(src => src.Project))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));

        CreateMap<ProjectsStatus, ReadProjectStatusDto>();
        CreateMap<ProjectsStatus, ReadExtendedProjectStatusDto>()
            .ForMember(dest => dest.project, opt => opt.MapFrom(src => src.Project));
        CreateMap<CreateProjectStatusDto, ProjectsStatus>();

        CreateMap<CreateProjectTagDto, ProjectTags>();
        CreateMap<ProjectTags, ReadProjectTagDto>();
        CreateMap<ProjectTags, ReadExtendedProjectTagDto>();

        CreateMap<CreateProjectProjectTagDto, ProjectsProjectTags>();
        CreateMap<ProjectsProjectTags, ReadProjectProjectTagDto>();
        CreateMap<ProjectsProjectTags, ReadExtendedProjectProjectTagDto>()
            .ForMember(dest => dest.project_tag, opt => opt.MapFrom(src => src.ProjectTag))
            .ForMember(dest => dest.project, opt => opt.MapFrom(src => src.Project));

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

        CreateMap<CreateUserPushSubscriptionDto, UserPushSubscriptions>();
        CreateMap<UserPushSubscriptions, ReadUserPushSubscriptionDto>();

        CreateMap<CreateUserDto, Users>();
        CreateMap<Users, ReadUserDto>();
        CreateMap<Users, ReadExtendedUserDto>();
    }
}