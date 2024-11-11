-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Hôte : mariadb
-- Généré le : jeu. 07 nov. 2024 à 16:30
-- Version du serveur : 10.7.3-MariaDB-1:10.7.3+maria~focal
-- Version de PHP : 8.2.23

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Base de données : `electrostore`
--

-- --------------------------------------------------------

--
-- Structure de la table `Boxs`
--

CREATE TABLE `Boxs` (
  `id_box` int(11) NOT NULL,
  `id_store` int(11) NOT NULL,
  `xstart_box` int(11) NOT NULL,
  `ystart_box` int(11) NOT NULL,
  `xend_box` int(11) NOT NULL,
  `yend_box` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `BoxsTags`
--

CREATE TABLE `BoxsTags` (
  `id_box` int(11) NOT NULL,
  `id_tag` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `Cameras`
--

CREATE TABLE `Cameras` (
  `id_camera` int(11) NOT NULL,
  `nom_camera` longtext NOT NULL,
  `url_camera` longtext NOT NULL,
  `user_camera` longtext DEFAULT NULL,
  `mdp_camera` longtext DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `Commands`
--

CREATE TABLE `Commands` (
  `id_command` int(11) NOT NULL,
  `prix_command` float NOT NULL,
  `url_command` longtext NOT NULL,
  `status_command` longtext NOT NULL,
  `date_command` datetime(6) NOT NULL,
  `date_livraison_command` datetime(6) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `CommandsCommentaires`
--

CREATE TABLE `CommandsCommentaires` (
  `id_commandcommentaire` int(11) NOT NULL,
  `id_user` int(11) DEFAULT NULL,
  `id_command` int(11) NOT NULL,
  `contenu_command_commentaire` longtext NOT NULL,
  `date_command_commentaire` datetime(6) NOT NULL,
  `date_modif_command_commentaire` datetime(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `CommandsDocuments`
--

CREATE TABLE `CommandsDocuments` (
  `id_command_document` int(11) NOT NULL,
  `url_command_document` longtext NOT NULL,
  `name_command_document` longtext NOT NULL,
  `type_command_document` longtext NOT NULL,
  `size_command_document` decimal(65,30) NOT NULL,
  `date_command_document` datetime(6) NOT NULL,
  `id_command` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `CommandsItems`
--

CREATE TABLE `CommandsItems` (
  `id_item` int(11) NOT NULL,
  `id_command` int(11) NOT NULL,
  `qte_command_item` int(11) NOT NULL,
  `prix_command_item` float NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `IA`
--

CREATE TABLE `IA` (
  `id_ia` int(11) NOT NULL,
  `nom_ia` longtext NOT NULL,
  `description_ia` longtext NOT NULL,
  `date_ia` datetime(6) NOT NULL,
  `trained_ia` tinyint(4) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `Imgs`
--

CREATE TABLE `Imgs` (
  `id_img` int(11) NOT NULL,
  `id_item` int(11) NOT NULL,
  `nom_img` longtext NOT NULL,
  `url_img` longtext NOT NULL,
  `description_img` longtext NOT NULL,
  `date_img` datetime(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `Items`
--

CREATE TABLE `Items` (
  `id_item` int(11) NOT NULL,
  `id_img` int(11) DEFAULT NULL,
  `nom_item` longtext NOT NULL,
  `seuil_min_item` int(11) NOT NULL,
  `description_item` longtext NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `ItemsBoxs`
--

CREATE TABLE `ItemsBoxs` (
  `id_box` int(11) NOT NULL,
  `id_item` int(11) NOT NULL,
  `qte_item_box` int(11) NOT NULL,
  `seuil_max_item_item_box` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `ItemsDocuments`
--

CREATE TABLE `ItemsDocuments` (
  `id_item_document` int(11) NOT NULL,
  `url_item_document` longtext NOT NULL,
  `name_item_document` longtext NOT NULL,
  `type_item_document` longtext NOT NULL,
  `size_item_document` decimal(65,30) NOT NULL,
  `date_item_document` datetime(6) NOT NULL,
  `id_item` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `ItemsTags`
--

CREATE TABLE `ItemsTags` (
  `id_tag` int(11) NOT NULL,
  `id_item` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- --------------------------------------------------------

--
-- Structure de la table `JWIAccessToken`
--

CREATE TABLE `JWIAccessToken` (
  `id_jwi_access` char(36) CHARACTER SET ascii NOT NULL,
  `expires_at` datetime(6) NOT NULL,
  `is_revoked` tinyint(1) NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `created_by_ip` longtext NOT NULL,
  `revoked_at` datetime(6) DEFAULT NULL,
  `revoked_by_ip` longtext DEFAULT NULL,
  `id_user` int(11) NOT NULL,
  `revoked_reason` longtext DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `JWIRefreshToken`
--

CREATE TABLE `JWIRefreshToken` (
  `id_jwi_refresh` char(36) CHARACTER SET ascii NOT NULL,
  `expires_at` datetime(6) NOT NULL,
  `is_revoked` tinyint(1) NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `created_by_ip` longtext NOT NULL,
  `revoked_at` datetime(6) DEFAULT NULL,
  `revoked_by_ip` longtext DEFAULT NULL,
  `id_user` int(11) NOT NULL,
  `id_jwi_access` char(36) CHARACTER SET ascii NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
  `revoked_reason` longtext DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `Leds`
--

CREATE TABLE `Leds` (
  `id_led` int(11) NOT NULL,
  `id_store` int(11) NOT NULL,
  `x_led` int(11) NOT NULL,
  `y_led` int(11) NOT NULL,
  `mqtt_led_id` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `Projets`
--

CREATE TABLE `Projets` (
  `id_projet` int(11) NOT NULL,
  `nom_projet` longtext NOT NULL,
  `description_projet` longtext NOT NULL,
  `url_projet` longtext NOT NULL,
  `status_projet` longtext NOT NULL,
  `date_projet` datetime(6) NOT NULL,
  `date_fin_projet` datetime(6) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `ProjetsCommentaires`
--

CREATE TABLE `ProjetsCommentaires` (
  `id_projet_commentaire` int(11) NOT NULL,
  `id_user` int(11) DEFAULT NULL,
  `id_projet` int(11) NOT NULL,
  `contenu_projet_commentaire` longtext NOT NULL,
  `date_projet_commentaire` datetime(6) NOT NULL,
  `date_modif_projet_commentaire` datetime(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `ProjetsDocuments`
--

CREATE TABLE `ProjetsDocuments` (
  `id_projet_document` int(11) NOT NULL,
  `url_projet_document` longtext NOT NULL,
  `name_projet_document` longtext NOT NULL,
  `type_projet_document` longtext NOT NULL,
  `size_projet_document` decimal(65,30) NOT NULL,
  `date_projet_document` datetime(6) NOT NULL,
  `id_projet` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `ProjetsItems`
--

CREATE TABLE `ProjetsItems` (
  `id_projet` int(11) NOT NULL,
  `id_item` int(11) NOT NULL,
  `qte_projet_item` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `Stores`
--

CREATE TABLE `Stores` (
  `id_store` int(11) NOT NULL,
  `nom_store` longtext NOT NULL,
  `xlength_store` int(11) NOT NULL,
  `ylength_store` int(11) NOT NULL,
  `mqtt_name_store` longtext NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `StoresTags`
--

CREATE TABLE `StoresTags` (
  `id_tag` int(11) NOT NULL,
  `id_store` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `Tags`
--

CREATE TABLE `Tags` (
  `id_tag` int(11) NOT NULL,
  `nom_tag` longtext NOT NULL,
  `poids_tag` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `Users`
--

CREATE TABLE `Users` (
  `id_user` int(11) NOT NULL,
  `nom_user` longtext NOT NULL,
  `prenom_user` longtext NOT NULL,
  `email_user` longtext NOT NULL,
  `mdp_user` longtext NOT NULL,
  `role_user` longtext NOT NULL,
  `reset_token` longtext DEFAULT NULL,
  `reset_token_expiration` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `__EFMigrationsHistory`
--

CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Index pour les tables déchargées
--

--
-- Index pour la table `Boxs`
--
ALTER TABLE `Boxs`
  ADD PRIMARY KEY (`id_box`),
  ADD KEY `IX_Boxs_id_store` (`id_store`);

--
-- Index pour la table `BoxsTags`
--
ALTER TABLE `BoxsTags`
  ADD PRIMARY KEY (`id_box`,`id_tag`),
  ADD KEY `IX_BoxsTags_id_tag` (`id_tag`);

--
-- Index pour la table `Cameras`
--
ALTER TABLE `Cameras`
  ADD PRIMARY KEY (`id_camera`);

--
-- Index pour la table `Commands`
--
ALTER TABLE `Commands`
  ADD PRIMARY KEY (`id_command`);

--
-- Index pour la table `CommandsCommentaires`
--
ALTER TABLE `CommandsCommentaires`
  ADD PRIMARY KEY (`id_commandcommentaire`),
  ADD KEY `IX_CommandsCommentaires_id_command` (`id_command`),
  ADD KEY `IX_CommandsCommentaires_id_user` (`id_user`);

--
-- Index pour la table `CommandsDocuments`
--
ALTER TABLE `CommandsDocuments`
  ADD PRIMARY KEY (`id_command_document`);

--
-- Index pour la table `CommandsItems`
--
ALTER TABLE `CommandsItems`
  ADD PRIMARY KEY (`id_command`,`id_item`),
  ADD KEY `IX_CommandsItems_id_item` (`id_item`);

--
-- Index pour la table `IA`
--
ALTER TABLE `IA`
  ADD PRIMARY KEY (`id_ia`);

--
-- Index pour la table `Imgs`
--
ALTER TABLE `Imgs`
  ADD PRIMARY KEY (`id_img`),
  ADD KEY `IX_Imgs_id_item` (`id_item`);

--
-- Index pour la table `Items`
--
ALTER TABLE `Items`
  ADD PRIMARY KEY (`id_item`),
  ADD KEY `IX_Items_id_img` (`id_img`);

--
-- Index pour la table `ItemsBoxs`
--
ALTER TABLE `ItemsBoxs`
  ADD PRIMARY KEY (`id_item`,`id_box`),
  ADD KEY `IX_ItemsBoxs_id_box` (`id_box`);

--
-- Index pour la table `ItemsDocuments`
--
ALTER TABLE `ItemsDocuments`
  ADD PRIMARY KEY (`id_item_document`);

--
-- Index pour la table `ItemsTags`
--
ALTER TABLE `ItemsTags`
  ADD PRIMARY KEY (`id_item`,`id_tag`),
  ADD KEY `IX_ItemsTags_id_tag` (`id_tag`);

--
-- Index pour la table `JWIAccessToken`
--
ALTER TABLE `JWIAccessToken`
  ADD PRIMARY KEY (`id_jwi_access`),
  ADD KEY `IX_JWIAccessToken_id_user` (`id_user`);

--
-- Index pour la table `JWIRefreshToken`
--
ALTER TABLE `JWIRefreshToken`
  ADD PRIMARY KEY (`id_jwi_refresh`),
  ADD KEY `IX_JWIRefreshToken_id_user` (`id_user`),
  ADD KEY `IX_JWIRefreshToken_id_jwi_access` (`id_jwi_access`);

--
-- Index pour la table `Leds`
--
ALTER TABLE `Leds`
  ADD PRIMARY KEY (`id_led`),
  ADD KEY `IX_Leds_id_store` (`id_store`);

--
-- Index pour la table `Projets`
--
ALTER TABLE `Projets`
  ADD PRIMARY KEY (`id_projet`);

--
-- Index pour la table `ProjetsCommentaires`
--
ALTER TABLE `ProjetsCommentaires`
  ADD PRIMARY KEY (`id_projet_commentaire`),
  ADD KEY `IX_ProjetsCommentaires_id_projet` (`id_projet`),
  ADD KEY `IX_ProjetsCommentaires_id_user` (`id_user`);

--
-- Index pour la table `ProjetsDocuments`
--
ALTER TABLE `ProjetsDocuments`
  ADD PRIMARY KEY (`id_projet_document`);

--
-- Index pour la table `ProjetsItems`
--
ALTER TABLE `ProjetsItems`
  ADD PRIMARY KEY (`id_projet`,`id_item`),
  ADD KEY `IX_ProjetsItems_id_item` (`id_item`);

--
-- Index pour la table `Stores`
--
ALTER TABLE `Stores`
  ADD PRIMARY KEY (`id_store`);

--
-- Index pour la table `StoresTags`
--
ALTER TABLE `StoresTags`
  ADD PRIMARY KEY (`id_store`,`id_tag`),
  ADD KEY `IX_StoresTags_id_tag` (`id_tag`);

--
-- Index pour la table `Tags`
--
ALTER TABLE `Tags`
  ADD PRIMARY KEY (`id_tag`);

--
-- Index pour la table `Users`
--
ALTER TABLE `Users`
  ADD PRIMARY KEY (`id_user`);

--
-- Index pour la table `__EFMigrationsHistory`
--
ALTER TABLE `__EFMigrationsHistory`
  ADD PRIMARY KEY (`MigrationId`);

--
-- AUTO_INCREMENT pour les tables déchargées
--

--
-- AUTO_INCREMENT pour la table `Boxs`
--
ALTER TABLE `Boxs`
  MODIFY `id_box` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `Cameras`
--
ALTER TABLE `Cameras`
  MODIFY `id_camera` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `Commands`
--
ALTER TABLE `Commands`
  MODIFY `id_command` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `CommandsCommentaires`
--
ALTER TABLE `CommandsCommentaires`
  MODIFY `id_commandcommentaire` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `CommandsDocuments`
--
ALTER TABLE `CommandsDocuments`
  MODIFY `id_command_document` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `IA`
--
ALTER TABLE `IA`
  MODIFY `id_ia` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `Imgs`
--
ALTER TABLE `Imgs`
  MODIFY `id_img` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `Items`
--
ALTER TABLE `Items`
  MODIFY `id_item` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `ItemsDocuments`
--
ALTER TABLE `ItemsDocuments`
  MODIFY `id_item_document` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `Leds`
--
ALTER TABLE `Leds`
  MODIFY `id_led` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `Projets`
--
ALTER TABLE `Projets`
  MODIFY `id_projet` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `ProjetsCommentaires`
--
ALTER TABLE `ProjetsCommentaires`
  MODIFY `id_projet_commentaire` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `ProjetsDocuments`
--
ALTER TABLE `ProjetsDocuments`
  MODIFY `id_projet_document` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `Stores`
--
ALTER TABLE `Stores`
  MODIFY `id_store` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `Tags`
--
ALTER TABLE `Tags`
  MODIFY `id_tag` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `Users`
--
ALTER TABLE `Users`
  MODIFY `id_user` int(11) NOT NULL AUTO_INCREMENT;

--
-- Contraintes pour les tables déchargées
--

--
-- Contraintes pour la table `Boxs`
--
ALTER TABLE `Boxs`
  ADD CONSTRAINT `FK_Boxs_Stores_id_store` FOREIGN KEY (`id_store`) REFERENCES `Stores` (`id_store`) ON DELETE CASCADE;

--
-- Contraintes pour la table `BoxsTags`
--
ALTER TABLE `BoxsTags`
  ADD CONSTRAINT `FK_BoxsTags_Boxs_id_box` FOREIGN KEY (`id_box`) REFERENCES `Boxs` (`id_box`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_BoxsTags_Tags_id_tag` FOREIGN KEY (`id_tag`) REFERENCES `Tags` (`id_tag`) ON DELETE CASCADE;

--
-- Contraintes pour la table `CommandsCommentaires`
--
ALTER TABLE `CommandsCommentaires`
  ADD CONSTRAINT `FK_CommandsCommentaires_Commands_id_command` FOREIGN KEY (`id_command`) REFERENCES `Commands` (`id_command`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_CommandsCommentaires_Users_id_user` FOREIGN KEY (`id_user`) REFERENCES `Users` (`id_user`) ON DELETE SET NULL;

--
-- Contraintes pour la table `CommandsItems`
--
ALTER TABLE `CommandsItems`
  ADD CONSTRAINT `FK_CommandsItems_Commands_id_command` FOREIGN KEY (`id_command`) REFERENCES `Commands` (`id_command`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_CommandsItems_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE;

--
-- Contraintes pour la table `Imgs`
--
ALTER TABLE `Imgs`
  ADD CONSTRAINT `FK_Imgs_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE;

--
-- Contraintes pour la table `Items`
--
ALTER TABLE `Items`
  ADD CONSTRAINT `FK_Items_Imgs_id_img` FOREIGN KEY (`id_img`) REFERENCES `Imgs` (`id_img`);

--
-- Contraintes pour la table `ItemsBoxs`
--
ALTER TABLE `ItemsBoxs`
  ADD CONSTRAINT `FK_ItemsBoxs_Boxs_id_box` FOREIGN KEY (`id_box`) REFERENCES `Boxs` (`id_box`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_ItemsBoxs_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE;

--
-- Contraintes pour la table `ItemsTags`
--
ALTER TABLE `ItemsTags`
  ADD CONSTRAINT `FK_ItemsTags_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_ItemsTags_Tags_id_tag` FOREIGN KEY (`id_tag`) REFERENCES `Tags` (`id_tag`) ON DELETE CASCADE;

--
-- Contraintes pour la table `JWIAccessToken`
--
ALTER TABLE `JWIAccessToken`
  ADD CONSTRAINT `FK_JWIAccessToken_Users_id_user` FOREIGN KEY (`id_user`) REFERENCES `Users` (`id_user`) ON DELETE CASCADE;

--
-- Contraintes pour la table `JWIRefreshToken`
--
ALTER TABLE `JWIRefreshToken`
  ADD CONSTRAINT `FK_JWIRefreshToken_JWIAccessToken_id_jwi_access` FOREIGN KEY (`id_jwi_access`) REFERENCES `JWIAccessToken` (`id_jwi_access`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_JWIRefreshToken_Users_id_user` FOREIGN KEY (`id_user`) REFERENCES `Users` (`id_user`) ON DELETE CASCADE;

--
-- Contraintes pour la table `Leds`
--
ALTER TABLE `Leds`
  ADD CONSTRAINT `FK_Leds_Stores_id_store` FOREIGN KEY (`id_store`) REFERENCES `Stores` (`id_store`) ON DELETE CASCADE;

--
-- Contraintes pour la table `ProjetsCommentaires`
--
ALTER TABLE `ProjetsCommentaires`
  ADD CONSTRAINT `FK_ProjetsCommentaires_Projets_id_projet` FOREIGN KEY (`id_projet`) REFERENCES `Projets` (`id_projet`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_ProjetsCommentaires_Users_id_user` FOREIGN KEY (`id_user`) REFERENCES `Users` (`id_user`) ON DELETE SET NULL;

--
-- Contraintes pour la table `ProjetsItems`
--
ALTER TABLE `ProjetsItems`
  ADD CONSTRAINT `FK_ProjetsItems_Items_id_item` FOREIGN KEY (`id_item`) REFERENCES `Items` (`id_item`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_ProjetsItems_Projets_id_projet` FOREIGN KEY (`id_projet`) REFERENCES `Projets` (`id_projet`) ON DELETE CASCADE;

--
-- Contraintes pour la table `StoresTags`
--
ALTER TABLE `StoresTags`
  ADD CONSTRAINT `FK_StoresTags_Stores_id_store` FOREIGN KEY (`id_store`) REFERENCES `Stores` (`id_store`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_StoresTags_Tags_id_tag` FOREIGN KEY (`id_tag`) REFERENCES `Tags` (`id_tag`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
