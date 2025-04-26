using AutoMapper;

using electrostore.Models;
using electrostore.Dto;

namespace electrostore;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Boxs, ReadBoxDto>();
        CreateMap<Boxs, ReadExtendedBoxDto>()
            .ForMember(dest => dest.id_box, opt => opt.MapFrom(src => src.id_box))
            .ForMember(dest => dest.id_store, opt => opt.MapFrom(src => src.id_store))
            .ForMember(dest => dest.xstart_box, opt => opt.MapFrom(src => src.xstart_box))
            .ForMember(dest => dest.ystart_box, opt => opt.MapFrom(src => src.ystart_box))
            .ForMember(dest => dest.xend_box, opt => opt.MapFrom(src => src.xend_box))
            .ForMember(dest => dest.yend_box, opt => opt.MapFrom(src => src.yend_box))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.store, opt => opt.MapFrom(src => src.Store))
            .ForMember(dest => dest.box_tags_count, opt => opt.MapFrom(src => src.BoxsTags.Count))
            .ForMember(dest => dest.item_boxs_count, opt => opt.MapFrom(src => src.ItemsBoxs.Count))
            .ForMember(dest => dest.box_tags, opt => opt.MapFrom(src => src.BoxsTags))
            .ForMember(dest => dest.item_boxs, opt => opt.MapFrom(src => src.ItemsBoxs));

        CreateMap<BoxsTags, ReadBoxTagDto>();
        CreateMap<BoxsTags, ReadExtendedBoxTagDto>()
            .ForMember(dest => dest.id_tag, opt => opt.MapFrom(src => src.id_tag))
            .ForMember(dest => dest.id_box, opt => opt.MapFrom(src => src.id_box))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.box, opt => opt.MapFrom(src => src.Box))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));
        
        CreateMap<Cameras, ReadCameraDto>();

        CreateMap<CommandsCommentaires, ReadCommandCommentaireDto>();
        CreateMap<CommandsCommentaires, ReadExtendedCommandCommentaireDto>()
            .ForMember(dest => dest.id_command_commentaire, opt => opt.MapFrom(src => src.id_command_commentaire))
            .ForMember(dest => dest.id_command, opt => opt.MapFrom(src => src.id_command))
            .ForMember(dest => dest.id_user, opt => opt.MapFrom(src => src.id_user))
            .ForMember(dest => dest.contenu_command_commentaire, opt => opt.MapFrom(src => src.contenu_command_commentaire))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.command, opt => opt.MapFrom(src => src.Command))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));
        
        CreateMap<CommandsDocuments, ReadCommandDocumentDto>();

        CreateMap<Commands, ReadCommandDto>();
        CreateMap<Commands, ReadExtendedCommandDto>()
            .ForMember(dest => dest.id_command, opt => opt.MapFrom(src => src.id_command))
            .ForMember(dest => dest.prix_command, opt => opt.MapFrom(src => src.prix_command))
            .ForMember(dest => dest.url_command, opt => opt.MapFrom(src => src.url_command))
            .ForMember(dest => dest.status_command, opt => opt.MapFrom(src => src.status_command))
            .ForMember(dest => dest.date_command, opt => opt.MapFrom(src => src.date_command))
            .ForMember(dest => dest.date_livraison_command, opt => opt.MapFrom(src => src.date_livraison_command))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.commands_commentaires_count, opt => opt.MapFrom(src => src.CommandsCommentaires.Count))
            .ForMember(dest => dest.commands_documents_count, opt => opt.MapFrom(src => src.CommandsDocuments.Count))
            .ForMember(dest => dest.commands_items_count, opt => opt.MapFrom(src => src.CommandsItems.Count))
            .ForMember(dest => dest.commands_commentaires, opt => opt.MapFrom(src => src.CommandsCommentaires))
            .ForMember(dest => dest.commands_documents, opt => opt.MapFrom(src => src.CommandsDocuments))
            .ForMember(dest => dest.commands_items, opt => opt.MapFrom(src => src.CommandsItems));
        
        CreateMap<CommandsItems, ReadCommandItemDto>();
        CreateMap<CommandsItems, ReadExtendedCommandItemDto>()
            .ForMember(dest => dest.id_command, opt => opt.MapFrom(src => src.id_command))
            .ForMember(dest => dest.id_item, opt => opt.MapFrom(src => src.id_item))
            .ForMember(dest => dest.qte_command_item, opt => opt.MapFrom(src => src.qte_command_item))
            .ForMember(dest => dest.prix_command_item, opt => opt.MapFrom(src => src.prix_command_item))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.command, opt => opt.MapFrom(src => src.Command))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));
        
        CreateMap<IA, ReadIADto>();

        CreateMap<Imgs, ReadImgDto>();

        CreateMap<ItemsBoxs, ReadItemBoxDto>();
        CreateMap<ItemsBoxs, ReadExtendedItemBoxDto>()
            .ForMember(dest => dest.id_box, opt => opt.MapFrom(src => src.id_box))
            .ForMember(dest => dest.id_item, opt => opt.MapFrom(src => src.id_item))
            .ForMember(dest => dest.qte_item_box, opt => opt.MapFrom(src => src.qte_item_box))
            .ForMember(dest => dest.seuil_max_item_item_box, opt => opt.MapFrom(src => src.seuil_max_item_item_box))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.box, opt => opt.MapFrom(src => src.Box))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));
        
        CreateMap<ItemsDocuments, ReadItemDocumentDto>();

        CreateMap<Items, ReadItemDto>();
        CreateMap<Items, ReadExtendedItemDto>()
            .ForMember(dest => dest.id_item, opt => opt.MapFrom(src => src.id_item))
            .ForMember(dest => dest.nom_item, opt => opt.MapFrom(src => src.nom_item))
            .ForMember(dest => dest.seuil_min_item, opt => opt.MapFrom(src => src.seuil_min_item))
            .ForMember(dest => dest.description_item, opt => opt.MapFrom(src => src.description_item))
            .ForMember(dest => dest.id_img, opt => opt.MapFrom(src => src.id_img))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.item_tags_count, opt => opt.MapFrom(src => src.ItemsTags.Count))
            .ForMember(dest => dest.item_boxs_count, opt => opt.MapFrom(src => src.ItemsBoxs.Count))
            .ForMember(dest => dest.command_items_count, opt => opt.MapFrom(src => src.CommandsItems.Count))
            .ForMember(dest => dest.projet_items_count, opt => opt.MapFrom(src => src.ProjetsItems.Count))
            .ForMember(dest => dest.item_documents_count, opt => opt.MapFrom(src => src.ItemsDocuments.Count))
            .ForMember(dest => dest.item_tags, opt => opt.MapFrom(src => src.ItemsTags))
            .ForMember(dest => dest.item_boxs, opt => opt.MapFrom(src => src.ItemsBoxs))
            .ForMember(dest => dest.command_items, opt => opt.MapFrom(src => src.CommandsItems))
            .ForMember(dest => dest.projet_items, opt => opt.MapFrom(src => src.ProjetsItems))
            .ForMember(dest => dest.item_documents, opt => opt.MapFrom(src => src.ItemsDocuments));
        
        CreateMap<ItemsTags, ReadItemTagDto>();
        CreateMap<ItemsTags, ReadExtendedItemTagDto>()
            .ForMember(dest => dest.id_tag, opt => opt.MapFrom(src => src.id_tag))
            .ForMember(dest => dest.id_item, opt => opt.MapFrom(src => src.id_item))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));
        
        CreateMap<JwiAccessTokens, ReadAccessTokenDto>();

        CreateMap<JwiRefreshTokens, ReadRefreshTokenDto>();

        CreateMap<Leds, ReadLedDto>();

        CreateMap<ProjetsCommentaires, ReadProjetCommentaireDto>();
        CreateMap<ProjetsCommentaires, ReadExtendedProjetCommentaireDto>()
            .ForMember(dest => dest.id_projet_commentaire, opt => opt.MapFrom(src => src.id_projet_commentaire))
            .ForMember(dest => dest.id_projet, opt => opt.MapFrom(src => src.id_projet))
            .ForMember(dest => dest.id_user, opt => opt.MapFrom(src => src.id_user))
            .ForMember(dest => dest.contenu_projet_commentaire, opt => opt.MapFrom(src => src.contenu_projet_commentaire))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.projet, opt => opt.MapFrom(src => src.Projet))
            .ForMember(dest => dest.user, opt => opt.MapFrom(src => src.User));
        
        CreateMap<ProjetsDocuments, ReadProjetDocumentDto>();

        CreateMap<Projets, ReadProjetDto>();
        CreateMap<Projets, ReadExtendedProjetDto>()
            .ForMember(dest => dest.id_projet, opt => opt.MapFrom(src => src.id_projet))
            .ForMember(dest => dest.nom_projet, opt => opt.MapFrom(src => src.nom_projet))
            .ForMember(dest => dest.description_projet, opt => opt.MapFrom(src => src.description_projet))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.projets_commentaires_count, opt => opt.MapFrom(src => src.ProjetsCommentaires.Count))
            .ForMember(dest => dest.projets_documents_count, opt => opt.MapFrom(src => src.ProjetsDocuments.Count))
            .ForMember(dest => dest.projets_items_count, opt => opt.MapFrom(src => src.ProjetsItems.Count))
            .ForMember(dest => dest.projets_commentaires, opt => opt.MapFrom(src => src.ProjetsCommentaires))
            .ForMember(dest => dest.projets_documents, opt => opt.MapFrom(src => src.ProjetsDocuments))
            .ForMember(dest => dest.projets_items, opt => opt.MapFrom(src => src.ProjetsItems));
        
        CreateMap<ProjetsItems, ReadProjetItemDto>();
        CreateMap<ProjetsItems, ReadExtendedProjetItemDto>()
            .ForMember(dest => dest.id_projet, opt => opt.MapFrom(src => src.id_projet))
            .ForMember(dest => dest.id_item, opt => opt.MapFrom(src => src.id_item))
            .ForMember(dest => dest.qte_projet_item, opt => opt.MapFrom(src => src.qte_projet_item))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.projet, opt => opt.MapFrom(src => src.Projet))
            .ForMember(dest => dest.item, opt => opt.MapFrom(src => src.Item));
        
        CreateMap<Stores, ReadStoreDto>();
        CreateMap<Stores, ReadExtendedStoreDto>()
            .ForMember(dest => dest.id_store, opt => opt.MapFrom(src => src.id_store))
            .ForMember(dest => dest.nom_store, opt => opt.MapFrom(src => src.nom_store))
            .ForMember(dest => dest.xlength_store, opt => opt.MapFrom(src => src.xlength_store))
            .ForMember(dest => dest.ylength_store, opt => opt.MapFrom(src => src.ylength_store))
            .ForMember(dest => dest.mqtt_name_store, opt => opt.MapFrom(src => src.mqtt_name_store))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.boxs_count, opt => opt.MapFrom(src => src.Boxs.Count))
            .ForMember(dest => dest.leds_count, opt => opt.MapFrom(src => src.Leds.Count))
            .ForMember(dest => dest.stores_tags_count, opt => opt.MapFrom(src => src.StoresTags.Count))
            .ForMember(dest => dest.boxs, opt => opt.MapFrom(src => src.Boxs))
            .ForMember(dest => dest.leds, opt => opt.MapFrom(src => src.Leds))
            .ForMember(dest => dest.stores_tags, opt => opt.MapFrom(src => src.StoresTags));
        
        CreateMap<StoresTags, ReadStoreTagDto>();
        CreateMap<StoresTags, ReadExtendedStoreTagDto>()
            .ForMember(dest => dest.id_tag, opt => opt.MapFrom(src => src.id_tag))
            .ForMember(dest => dest.id_store, opt => opt.MapFrom(src => src.id_store))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.store, opt => opt.MapFrom(src => src.Store))
            .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Tag));
        
        CreateMap<Tags, ReadTagDto>();
        CreateMap<Tags, ReadExtendedTagDto>()
            .ForMember(dest => dest.id_tag, opt => opt.MapFrom(src => src.id_tag))
            .ForMember(dest => dest.nom_tag, opt => opt.MapFrom(src => src.nom_tag))
            .ForMember(dest => dest.poids_tag, opt => opt.MapFrom(src => src.poids_tag))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.stores_tags_count, opt => opt.MapFrom(src => src.StoresTags.Count))
            .ForMember(dest => dest.items_tags_count, opt => opt.MapFrom(src => src.ItemsTags.Count))
            .ForMember(dest => dest.boxs_tags_count, opt => opt.MapFrom(src => src.BoxsTags.Count))
            .ForMember(dest => dest.stores_tags, opt => opt.MapFrom(src => src.StoresTags))
            .ForMember(dest => dest.items_tags, opt => opt.MapFrom(src => src.ItemsTags))
            .ForMember(dest => dest.boxs_tags, opt => opt.MapFrom(src => src.BoxsTags));
        
        CreateMap<Users, ReadUserDto>()
            .ForMember(dest => dest.id_user, opt => opt.MapFrom(src => src.id_user))
            .ForMember(dest => dest.nom_user, opt => opt.MapFrom(src => src.nom_user))
            .ForMember(dest => dest.prenom_user, opt => opt.MapFrom(src => src.prenom_user))
            .ForMember(dest => dest.email_user, opt => opt.MapFrom(src => src.email_user))
            .ForMember(dest => dest.role_user, opt => opt.MapFrom(src => src.role_user))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at));
        CreateMap<Users, ReadExtendedUserDto>()
            .ForMember(dest => dest.id_user, opt => opt.MapFrom(src => src.id_user))
            .ForMember(dest => dest.nom_user, opt => opt.MapFrom(src => src.nom_user))
            .ForMember(dest => dest.prenom_user, opt => opt.MapFrom(src => src.prenom_user))
            .ForMember(dest => dest.email_user, opt => opt.MapFrom(src => src.email_user))
            .ForMember(dest => dest.role_user, opt => opt.MapFrom(src => src.role_user))
            .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.created_at))
            .ForMember(dest => dest.updated_at, opt => opt.MapFrom(src => src.updated_at))
            .ForMember(dest => dest.projets_commentaires_count, opt => opt.MapFrom(src => src.ProjetsCommentaires.Count))
            .ForMember(dest => dest.commands_commentaires_count, opt => opt.MapFrom(src => src.CommandsCommentaires.Count))
            .ForMember(dest => dest.projets_commentaires, opt => opt.MapFrom(src => src.ProjetsCommentaires))
            .ForMember(dest => dest.commands_commentaires, opt => opt.MapFrom(src => src.CommandsCommentaires));
    }
}