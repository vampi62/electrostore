CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `Cameras` (
    `id_camera` int NOT NULL AUTO_INCREMENT,
    `nom_camera` longtext CHARACTER SET utf8mb4 NOT NULL,
    `url_camera` longtext CHARACTER SET utf8mb4 NOT NULL,
    `user_camera` longtext CHARACTER SET utf8mb4 NULL,
    `mdp_camera` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Cameras` PRIMARY KEY (`id_camera`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Commands` (
    `id_command` int NOT NULL AUTO_INCREMENT,
    `prix_command` float NOT NULL,
    `url_command` longtext CHARACTER SET utf8mb4 NOT NULL,
    `status_command` longtext CHARACTER SET utf8mb4 NOT NULL,
    `date_command` datetime(6) NOT NULL,
    `date_livraison_command` datetime(6) NULL,
    CONSTRAINT `PK_Commands` PRIMARY KEY (`id_command`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `IA` (
    `id_ia` int NOT NULL AUTO_INCREMENT,
    `nom_ia` longtext CHARACTER SET utf8mb4 NOT NULL,
    `description_ia` longtext CHARACTER SET utf8mb4 NOT NULL,
    `date_ia` datetime(6) NOT NULL,
    `trained_ia` tinyint(1) NOT NULL,
    CONSTRAINT `PK_IA` PRIMARY KEY (`id_ia`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Projets` (
    `id_projet` int NOT NULL AUTO_INCREMENT,
    `nom_projet` longtext CHARACTER SET utf8mb4 NOT NULL,
    `description_projet` longtext CHARACTER SET utf8mb4 NOT NULL,
    `url_projet` longtext CHARACTER SET utf8mb4 NOT NULL,
    `status_projet` longtext CHARACTER SET utf8mb4 NOT NULL,
    `date_projet` datetime(6) NOT NULL,
    `date_fin_projet` datetime(6) NULL,
    CONSTRAINT `PK_Projets` PRIMARY KEY (`id_projet`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Stores` (
    `id_store` int NOT NULL AUTO_INCREMENT,
    `nom_store` longtext CHARACTER SET utf8mb4 NOT NULL,
    `xlength_store` int NOT NULL,
    `ylength_store` int NOT NULL,
    `mqtt_name_store` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Stores` PRIMARY KEY (`id_store`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Tags` (
    `id_tag` int NOT NULL AUTO_INCREMENT,
    `nom_tag` longtext CHARACTER SET utf8mb4 NOT NULL,
    `poids_tag` int NOT NULL,
    CONSTRAINT `PK_Tags` PRIMARY KEY (`id_tag`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Users` (
    `id_user` int NOT NULL AUTO_INCREMENT,
    `nom_user` longtext CHARACTER SET utf8mb4 NOT NULL,
    `prenom_user` longtext CHARACTER SET utf8mb4 NOT NULL,
    `email_user` longtext CHARACTER SET utf8mb4 NOT NULL,
    `mdp_user` longtext CHARACTER SET utf8mb4 NOT NULL,
    `role_user` longtext CHARACTER SET utf8mb4 NOT NULL,
    `reset_token` longtext CHARACTER SET utf8mb4 NULL,
    `reset_token_expiration` datetime(6) NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`id_user`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `CommandsDocuments` (
    `id_command_document` int NOT NULL AUTO_INCREMENT,
    `url_command_document` longtext CHARACTER SET utf8mb4 NOT NULL,
    `name_command_document` longtext CHARACTER SET utf8mb4 NOT NULL,
    `type_command_document` longtext CHARACTER SET utf8mb4 NOT NULL,
    `size_command_document` decimal(65,30) NOT NULL,
    `date_command_document` datetime(6) NOT NULL,
    `id_command` int NOT NULL,
    CONSTRAINT `PK_CommandsDocuments` PRIMARY KEY (`id_command_document`),
    CONSTRAINT `FK_CommandsDocuments_Commands_id_command` FOREIGN KEY (`id_command`) REFERENCES `Commands` (`id_command`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ProjetsDocuments` (
    `id_projet_document` int NOT NULL AUTO_INCREMENT,
    `url_projet_document` longtext CHARACTER SET utf8mb4 NOT NULL,
    `name_projet_document` longtext CHARACTER SET utf8mb4 NOT NULL,
    `type_projet_document` longtext CHARACTER SET utf8mb4 NOT NULL,
    `size_projet_document` decimal(65,30) NOT NULL,
    `date_projet_document` datetime(6) NOT NULL,
    `id_projet` int NOT NULL,
    CONSTRAINT `PK_ProjetsDocuments` PRIMARY KEY (`id_projet_document`),
    CONSTRAINT `FK_ProjetsDocuments_Projets_id_projet` FOREIGN KEY (`id_projet`) REFERENCES `Projets` (`id_projet`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Boxs` (
    `id_box` int NOT NULL AUTO_INCREMENT,
    `id_store` int NOT NULL,
    `xstart_box` int NOT NULL,
    `ystart_box` int NOT NULL,
    `xend_box` int NOT NULL,
    `yend_box` int NOT NULL,
    CONSTRAINT `PK_Boxs` PRIMARY KEY (`id_box`),
    CONSTRAINT `FK_Boxs_Stores_id_store` FOREIGN KEY (`id_store`) REFERENCES `Stores` (`id_store`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Leds` (
    `id_led` int NOT NULL AUTO_INCREMENT,
    `id_store` int NOT NULL,
    `x_led` int NOT NULL,
    `y_led` int NOT NULL,
    `mqtt_led_id` int NOT NULL,
    CONSTRAINT `PK_Leds` PRIMARY KEY (`id_led`),
    CONSTRAINT `FK_Leds_Stores_id_store` FOREIGN KEY (`id_store`) REFERENCES `Stores` (`id_store`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `StoresTags` (
    `id_tag` int NOT NULL,
    `id_store` int NOT NULL,
    CONSTRAINT `PK_StoresTags` PRIMARY KEY (`id_store`, `id_tag`),
    CONSTRAINT `FK_StoresTags_Stores_id_store` FOREIGN KEY (`id_store`) REFERENCES `Stores` (`id_store`) ON DELETE CASCADE,
    CONSTRAINT `FK_StoresTags_Tags_id_tag` FOREIGN KEY (`id_tag`) REFERENCES `Tags` (`id_tag`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `CommandsCommentaires` (
    `id_command_commentaire` int NOT NULL AUTO_INCREMENT,
    `id_user` int NULL,
    `id_command` int NOT NULL,
    `contenu_command_commentaire` longtext CHARACTER SET utf8mb4 NOT NULL,
    `date_command_commentaire` datetime(6) NOT NULL,
    `date_modif_command_commentaire` datetime(6) NOT NULL,
    CONSTRAINT `PK_CommandsCommentaires` PRIMARY KEY (`id_command_commentaire`),
    CONSTRAINT `FK_CommandsCommentaires_Commands_id_command` FOREIGN KEY (`id_command`) REFERENCES `Commands` (`id_command`) ON DELETE CASCADE,
    CONSTRAINT `FK_CommandsCommentaires_Users_id_user` FOREIGN KEY (`id_user`) REFERENCES `Users` (`id_user`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `JWIAccessToken` (
    `id_jwi_access` char(36) COLLATE ascii_general_ci NOT NULL,
    `expires_at` datetime(6) NOT NULL,
    `is_revoked` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `created_by_ip` longtext CHARACTER SET utf8mb4 NOT NULL,
    `revoked_at` datetime(6) NULL,
    `revoked_by_ip` longtext CHARACTER SET utf8mb4 NULL,
    `revoked_reason` longtext CHARACTER SET utf8mb4 NULL,
    `id_user` int NOT NULL,
    CONSTRAINT `PK_JWIAccessToken` PRIMARY KEY (`id_jwi_access`),
    CONSTRAINT `FK_JWIAccessToken_Users_id_user` FOREIGN KEY (`id_user`) REFERENCES `Users` (`id_user`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ProjetsCommentaires` (
    `id_projet_commentaire` int NOT NULL AUTO_INCREMENT,
    `id_user` int NULL,
    `id_projet` int NOT NULL,
    `contenu_projet_commentaire` longtext CHARACTER SET utf8mb4 NOT NULL,
    `date_projet_commentaire` datetime(6) NOT NULL,
    `date_modif_projet_commentaire` datetime(6) NOT NULL,
    CONSTRAINT `PK_ProjetsCommentaires` PRIMARY KEY (`id_projet_commentaire`),
    CONSTRAINT `FK_ProjetsCommentaires_Projets_id_projet` FOREIGN KEY (`id_projet`) REFERENCES `Projets` (`id_projet`) ON DELETE CASCADE,
    CONSTRAINT `FK_ProjetsCommentaires_Users_id_user` FOREIGN KEY (`id_user`) REFERENCES `Users` (`id_user`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `BoxsTags` (
    `id_box` int NOT NULL,
    `id_tag` int NOT NULL,
    CONSTRAINT `PK_BoxsTags` PRIMARY KEY (`id_box`, `id_tag`),
    CONSTRAINT `FK_BoxsTags_Boxs_id_box` FOREIGN KEY (`id_box`) REFERENCES `Boxs` (`id_box`) ON DELETE CASCADE,
    CONSTRAINT `FK_BoxsTags_Tags_id_tag` FOREIGN KEY (`id_tag`) REFERENCES `Tags` (`id_tag`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `JWIRefreshToken` (
    `id_jwi_refresh` char(36) COLLATE ascii_general_ci NOT NULL,
    `expires_at` datetime(6) NOT NULL,
    `is_revoked` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `created_by_ip` longtext CHARACTER SET utf8mb4 NOT NULL,
    `revoked_at` datetime(6) NULL,
    `revoked_by_ip` longtext CHARACTER SET utf8mb4 NULL,
    `revoked_reason` longtext CHARACTER SET utf8mb4 NULL,
    `id_user` int NOT NULL,
    `id_jwi_access` char(36) COLLATE ascii_general_ci NOT NULL,
    CONSTRAINT `PK_JWIRefreshToken` PRIMARY KEY (`id_jwi_refresh`),
    CONSTRAINT `FK_JWIRefreshToken_JWIAccessToken_id_jwi_access` FOREIGN KEY (`id_jwi_access`) REFERENCES `JWIAccessToken` (`id_jwi_access`) ON DELETE CASCADE,
    CONSTRAINT `FK_JWIRefreshToken_Users_id_user` FOREIGN KEY (`id_user`) REFERENCES `Users` (`id_user`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `CommandsItems` (
    `id_item` int NOT NULL,
    `id_command` int NOT NULL,
    `qte_command_item` int NOT NULL,
    `prix_command_item` float NOT NULL,
    CONSTRAINT `PK_CommandsItems` PRIMARY KEY (`id_command`, `id_item`),
    CONSTRAINT `FK_CommandsItems_Commands_id_command` FOREIGN KEY (`id_command`) REFERENCES `Commands` (`id_command`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Imgs` (
    `id_img` int NOT NULL AUTO_INCREMENT,
    `id_item` int NOT NULL,
    `nom_img` longtext CHARACTER SET utf8mb4 NOT NULL,
    `url_img` longtext CHARACTER SET utf8mb4 NOT NULL,
    `description_img` longtext CHARACTER SET utf8mb4 NOT NULL,
    `date_img` datetime(6) NOT NULL,
    CONSTRAINT `PK_Imgs` PRIMARY KEY (`id_img`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Items` (
    `id_item` int NOT NULL AUTO_INCREMENT,
    `id_img` int NULL,
    `nom_item` longtext CHARACTER SET utf8mb4 NOT NULL,
    `seuil_min_item` int NOT NULL,
    `description_item` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Items` PRIMARY KEY (`id_item`),
    CONSTRAINT `FK_Items_Imgs_id_img` FOREIGN KEY (`id_img`) REFERENCES `Imgs` (`id_img`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `ItemsBoxs` (
    `id_box` int NOT NULL,
    `id_item` int NOT NULL,
    `qte_item_box` int NOT NULL,
    `seuil_max_item_item_box` int NOT NULL,
    CONSTRAINT `PK_ItemsBoxs` PRIMARY KEY (`id_item`, `id_box`),
    CONSTRAINT `FK_ItemsBoxs_Boxs_id_box` FOREIGN KEY (`id_box`) REFERENCES `Boxs` (`id_box`) ON DELETE CASCADE,
    CONSTRAINT `FK_ItemsBoxs_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ItemsDocuments` (
    `id_item_document` int NOT NULL AUTO_INCREMENT,
    `url_item_document` longtext CHARACTER SET utf8mb4 NOT NULL,
    `name_item_document` longtext CHARACTER SET utf8mb4 NOT NULL,
    `type_item_document` longtext CHARACTER SET utf8mb4 NOT NULL,
    `size_item_document` decimal(65,30) NOT NULL,
    `date_item_document` datetime(6) NOT NULL,
    `id_item` int NOT NULL,
    CONSTRAINT `PK_ItemsDocuments` PRIMARY KEY (`id_item_document`),
    CONSTRAINT `FK_ItemsDocuments_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ItemsTags` (
    `id_tag` int NOT NULL,
    `id_item` int NOT NULL,
    CONSTRAINT `PK_ItemsTags` PRIMARY KEY (`id_item`, `id_tag`),
    CONSTRAINT `FK_ItemsTags_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE,
    CONSTRAINT `FK_ItemsTags_Tags_id_tag` FOREIGN KEY (`id_tag`) REFERENCES `Tags` (`id_tag`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ProjetsItems` (
    `id_projet` int NOT NULL,
    `id_item` int NOT NULL,
    `qte_projet_item` int NOT NULL,
    CONSTRAINT `PK_ProjetsItems` PRIMARY KEY (`id_projet`, `id_item`),
    CONSTRAINT `FK_ProjetsItems_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE,
    CONSTRAINT `FK_ProjetsItems_Projets_id_projet` FOREIGN KEY (`id_projet`) REFERENCES `Projets` (`id_projet`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Boxs_id_store` ON `Boxs` (`id_store`);

CREATE INDEX `IX_BoxsTags_id_tag` ON `BoxsTags` (`id_tag`);

CREATE INDEX `IX_CommandsCommentaires_id_command` ON `CommandsCommentaires` (`id_command`);

CREATE INDEX `IX_CommandsCommentaires_id_user` ON `CommandsCommentaires` (`id_user`);

CREATE INDEX `IX_CommandsDocuments_id_command` ON `CommandsDocuments` (`id_command`);

CREATE INDEX `IX_CommandsItems_id_item` ON `CommandsItems` (`id_item`);

CREATE INDEX `IX_Imgs_id_item` ON `Imgs` (`id_item`);

CREATE INDEX `IX_Items_id_img` ON `Items` (`id_img`);

CREATE INDEX `IX_ItemsBoxs_id_box` ON `ItemsBoxs` (`id_box`);

CREATE INDEX `IX_ItemsDocuments_id_item` ON `ItemsDocuments` (`id_item`);

CREATE INDEX `IX_ItemsTags_id_tag` ON `ItemsTags` (`id_tag`);

CREATE INDEX `IX_JWIAccessToken_id_user` ON `JWIAccessToken` (`id_user`);

CREATE INDEX `IX_JWIRefreshToken_id_jwi_access` ON `JWIRefreshToken` (`id_jwi_access`);

CREATE INDEX `IX_JWIRefreshToken_id_user` ON `JWIRefreshToken` (`id_user`);

CREATE INDEX `IX_Leds_id_store` ON `Leds` (`id_store`);

CREATE INDEX `IX_ProjetsCommentaires_id_projet` ON `ProjetsCommentaires` (`id_projet`);

CREATE INDEX `IX_ProjetsCommentaires_id_user` ON `ProjetsCommentaires` (`id_user`);

CREATE INDEX `IX_ProjetsDocuments_id_projet` ON `ProjetsDocuments` (`id_projet`);

CREATE INDEX `IX_ProjetsItems_id_item` ON `ProjetsItems` (`id_item`);

CREATE INDEX `IX_StoresTags_id_tag` ON `StoresTags` (`id_tag`);

ALTER TABLE `CommandsItems` ADD CONSTRAINT `FK_CommandsItems_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE;

ALTER TABLE `Imgs` ADD CONSTRAINT `FK_Imgs_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20241111162515_initDB', '7.0.16');

COMMIT;

START TRANSACTION;

ALTER TABLE `Projets` RENAME COLUMN `date_projet` TO `date_debut_projet`;

ALTER TABLE `Users` MODIFY COLUMN `role_user` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Users` MODIFY COLUMN `reset_token` char(36) COLLATE ascii_general_ci NULL;

ALTER TABLE `Users` MODIFY COLUMN `prenom_user` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Users` MODIFY COLUMN `nom_user` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Users` MODIFY COLUMN `mdp_user` varchar(255) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Users` MODIFY COLUMN `email_user` varchar(100) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Tags` MODIFY COLUMN `nom_tag` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Stores` MODIFY COLUMN `nom_store` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Stores` MODIFY COLUMN `mqtt_name_store` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `ProjetsDocuments` MODIFY COLUMN `url_projet_document` varchar(150) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `ProjetsDocuments` MODIFY COLUMN `type_projet_document` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `ProjetsDocuments` MODIFY COLUMN `name_projet_document` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `ProjetsCommentaires` MODIFY COLUMN `contenu_projet_commentaire` varchar(455) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Projets` MODIFY COLUMN `url_projet` varchar(150) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Projets` MODIFY COLUMN `status_projet` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Projets` MODIFY COLUMN `nom_projet` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Projets` MODIFY COLUMN `description_projet` varchar(500) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `JWIRefreshToken` MODIFY COLUMN `revoked_reason` varchar(50) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `JWIRefreshToken` MODIFY COLUMN `revoked_by_ip` varchar(50) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `JWIRefreshToken` MODIFY COLUMN `created_by_ip` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `JWIAccessToken` MODIFY COLUMN `revoked_reason` varchar(50) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `JWIAccessToken` MODIFY COLUMN `revoked_by_ip` varchar(50) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `JWIAccessToken` MODIFY COLUMN `created_by_ip` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `ItemsDocuments` MODIFY COLUMN `url_item_document` varchar(150) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `ItemsDocuments` MODIFY COLUMN `type_item_document` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `ItemsDocuments` MODIFY COLUMN `name_item_document` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Items` MODIFY COLUMN `nom_item` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Items` MODIFY COLUMN `description_item` varchar(500) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Imgs` MODIFY COLUMN `url_img` varchar(150) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Imgs` MODIFY COLUMN `nom_img` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Imgs` MODIFY COLUMN `description_img` varchar(500) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `IA` MODIFY COLUMN `nom_ia` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `IA` MODIFY COLUMN `description_ia` varchar(500) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `CommandsDocuments` MODIFY COLUMN `url_command_document` varchar(150) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `CommandsDocuments` MODIFY COLUMN `type_command_document` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `CommandsDocuments` MODIFY COLUMN `name_command_document` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `CommandsCommentaires` MODIFY COLUMN `contenu_command_commentaire` varchar(455) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Commands` MODIFY COLUMN `url_command` varchar(150) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Commands` MODIFY COLUMN `status_command` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Cameras` MODIFY COLUMN `user_camera` varchar(50) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Cameras` MODIFY COLUMN `url_camera` varchar(150) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Cameras` MODIFY COLUMN `nom_camera` varchar(50) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Cameras` MODIFY COLUMN `mdp_camera` varchar(50) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20241217193833_AddLimitColumn', '7.0.16');

COMMIT;

