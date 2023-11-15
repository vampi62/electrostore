-- phpMyAdmin SQL Dump
-- version 5.2.0
-- https://www.phpmyadmin.net/
--
-- Hôte : 192.168.2.52
-- Généré le : mer. 15 nov. 2023 à 15:58
-- Version du serveur : 10.7.3-MariaDB-1:10.7.3+maria~focal
-- Version de PHP : 8.0.19

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
-- Structure de la table `boxs`
--

CREATE TABLE `boxs` (
  `id_box` int(11) UNSIGNED NOT NULL,
  `pos_x_origin_box` tinyint(4) UNSIGNED NOT NULL,
  `pos_y_origin_box` tinyint(4) UNSIGNED NOT NULL,
  `taille_x_box` tinyint(4) UNSIGNED NOT NULL,
  `taille_y_box` tinyint(4) UNSIGNED NOT NULL,
  `separateur_box` tinyint(4) UNSIGNED NOT NULL,
  `id_store` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `cameras`
--

CREATE TABLE `cameras` (
  `id_camera` int(11) UNSIGNED NOT NULL,
  `nom_camera` varchar(50) DEFAULT NULL,
  `ip_camera` varchar(50) DEFAULT NULL,
  `port_uri_config_camera` varchar(50) DEFAULT NULL,
  `port_uri_video_camera` varchar(50) DEFAULT NULL,
  `port_uri_picture_camera` varchar(50) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `commandes`
--

CREATE TABLE `commandes` (
  `id_commande` int(11) UNSIGNED NOT NULL,
  `date_commande` date NOT NULL,
  `fournisseur_commande` varchar(50) NOT NULL,
  `status_commande` varchar(50) NOT NULL,
  `suivi_commandes` varchar(50) DEFAULT NULL,
  `date_reception_commande` date DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `commandes_objets`
--

CREATE TABLE `commandes_objets` (
  `id_objet` int(11) UNSIGNED NOT NULL,
  `id_commande` int(11) UNSIGNED NOT NULL,
  `prix_commandes_objets` decimal(15,2) UNSIGNED NOT NULL,
  `quant_commandes_objets` tinyint(4) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `model_imgs`
--

CREATE TABLE `model_imgs` (
  `id_model_img` int(11) UNSIGNED NOT NULL,
  `id_model_objet` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `model_imgs_version_models`
--

CREATE TABLE `model_imgs_version_models` (
  `id_model_img` int(11) UNSIGNED NOT NULL,
  `id_version_model` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `model_objets`
--

CREATE TABLE `model_objets` (
  `id_model_objet` int(11) UNSIGNED NOT NULL,
  `nom_model_objet` varchar(50) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `objets`
--

CREATE TABLE `objets` (
  `id_objet` int(11) UNSIGNED NOT NULL,
  `quant_objet` varchar(50) NOT NULL,
  `seuil_alerte_objet` int(11) UNSIGNED DEFAULT NULL,
  `max_objet` int(11) UNSIGNED NOT NULL,
  `description_objet` varchar(300) DEFAULT NULL,
  `id_box` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `projets`
--

CREATE TABLE `projets` (
  `id_projet` int(11) UNSIGNED NOT NULL,
  `nom_projet` varchar(50) NOT NULL,
  `status_projet` varchar(50) NOT NULL,
  `url_plan_projets` varchar(50) DEFAULT NULL,
  `date_debut_projet` date NOT NULL,
  `date_fin_projet` date DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `projets_objets`
--

CREATE TABLE `projets_objets` (
  `id_objet` int(11) UNSIGNED NOT NULL,
  `id_projet` int(11) UNSIGNED NOT NULL,
  `quant_projets_objets` tinyint(4) UNSIGNED NOT NULL,
  `status_projets_objets` varchar(50) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `stores`
--

CREATE TABLE `stores` (
  `id_store` int(11) UNSIGNED NOT NULL,
  `nom_store` varchar(50) DEFAULT NULL,
  `taille_x_store` tinyint(4) UNSIGNED NOT NULL,
  `taille_y_store` tinyint(4) UNSIGNED NOT NULL,
  `sens_store` tinyint(1) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `tags`
--

CREATE TABLE `tags` (
  `id_tag` int(11) UNSIGNED NOT NULL,
  `nom_tag` varchar(50) NOT NULL,
  `poid_tag` tinyint(4) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `tags_boxs`
--

CREATE TABLE `tags_boxs` (
  `id_box` int(11) UNSIGNED NOT NULL,
  `id_tag` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `tags_model_objets`
--

CREATE TABLE `tags_model_objets` (
  `id_tag` int(11) UNSIGNED NOT NULL,
  `id_model_objet` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `tags_objets`
--

CREATE TABLE `tags_objets` (
  `id_objet` int(11) UNSIGNED NOT NULL,
  `id_tag` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `utilisateurs`
--

CREATE TABLE `utilisateurs` (
  `id_utilisateur` int(11) UNSIGNED NOT NULL,
  `pseudo_utilisateur` varchar(50) NOT NULL,
  `mdp_utilisateur` varchar(250) NOT NULL,
  `id_version_model` int(11) UNSIGNED DEFAULT NULL,
  `id_camera` int(11) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `version_models`
--

CREATE TABLE `version_models` (
  `id_version_model` int(11) UNSIGNED NOT NULL,
  `date_version_model` date NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Index pour les tables déchargées
--

--
-- Index pour la table `boxs`
--
ALTER TABLE `boxs`
  ADD PRIMARY KEY (`id_box`),
  ADD KEY `boxs_ibfk_1` (`id_store`);

--
-- Index pour la table `cameras`
--
ALTER TABLE `cameras`
  ADD PRIMARY KEY (`id_camera`);

--
-- Index pour la table `commandes`
--
ALTER TABLE `commandes`
  ADD PRIMARY KEY (`id_commande`);

--
-- Index pour la table `commandes_objets`
--
ALTER TABLE `commandes_objets`
  ADD PRIMARY KEY (`id_objet`,`id_commande`),
  ADD KEY `commandes_objets_ibfk_2` (`id_commande`);

--
-- Index pour la table `model_imgs`
--
ALTER TABLE `model_imgs`
  ADD PRIMARY KEY (`id_model_img`),
  ADD KEY `model_imgs_ibfk_1` (`id_model_objet`);

--
-- Index pour la table `model_imgs_version_models`
--
ALTER TABLE `model_imgs_version_models`
  ADD PRIMARY KEY (`id_model_img`,`id_version_model`),
  ADD KEY `model_imgs_version_models_ibfk_2` (`id_version_model`);

--
-- Index pour la table `model_objets`
--
ALTER TABLE `model_objets`
  ADD PRIMARY KEY (`id_model_objet`);

--
-- Index pour la table `objets`
--
ALTER TABLE `objets`
  ADD PRIMARY KEY (`id_objet`),
  ADD KEY `objets_ibfk_1` (`id_box`);

--
-- Index pour la table `projets`
--
ALTER TABLE `projets`
  ADD PRIMARY KEY (`id_projet`);

--
-- Index pour la table `projets_objets`
--
ALTER TABLE `projets_objets`
  ADD PRIMARY KEY (`id_objet`,`id_projet`),
  ADD KEY `projets_objets_ibfk_2` (`id_projet`);

--
-- Index pour la table `stores`
--
ALTER TABLE `stores`
  ADD PRIMARY KEY (`id_store`);

--
-- Index pour la table `tags`
--
ALTER TABLE `tags`
  ADD PRIMARY KEY (`id_tag`);

--
-- Index pour la table `tags_boxs`
--
ALTER TABLE `tags_boxs`
  ADD PRIMARY KEY (`id_box`,`id_tag`),
  ADD KEY `tags_boxs_ibfk_2` (`id_tag`);

--
-- Index pour la table `tags_model_objets`
--
ALTER TABLE `tags_model_objets`
  ADD PRIMARY KEY (`id_tag`,`id_model_objet`),
  ADD KEY `tags_model_objets_ibfk_2` (`id_model_objet`);

--
-- Index pour la table `tags_objets`
--
ALTER TABLE `tags_objets`
  ADD PRIMARY KEY (`id_objet`,`id_tag`),
  ADD KEY `tags_objets_ibfk_2` (`id_tag`);

--
-- Index pour la table `utilisateurs`
--
ALTER TABLE `utilisateurs`
  ADD PRIMARY KEY (`id_utilisateur`),
  ADD KEY `utilisateurs_ibfk_1` (`id_version_model`),
  ADD KEY `utilisateurs_ibfk_2` (`id_camera`);

--
-- Index pour la table `version_models`
--
ALTER TABLE `version_models`
  ADD PRIMARY KEY (`id_version_model`);

--
-- AUTO_INCREMENT pour les tables déchargées
--

--
-- AUTO_INCREMENT pour la table `boxs`
--
ALTER TABLE `boxs`
  MODIFY `id_box` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `cameras`
--
ALTER TABLE `cameras`
  MODIFY `id_camera` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `commandes`
--
ALTER TABLE `commandes`
  MODIFY `id_commande` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `model_imgs`
--
ALTER TABLE `model_imgs`
  MODIFY `id_model_img` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `model_objets`
--
ALTER TABLE `model_objets`
  MODIFY `id_model_objet` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `objets`
--
ALTER TABLE `objets`
  MODIFY `id_objet` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `projets`
--
ALTER TABLE `projets`
  MODIFY `id_projet` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `stores`
--
ALTER TABLE `stores`
  MODIFY `id_store` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `tags`
--
ALTER TABLE `tags`
  MODIFY `id_tag` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `utilisateurs`
--
ALTER TABLE `utilisateurs`
  MODIFY `id_utilisateur` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `version_models`
--
ALTER TABLE `version_models`
  MODIFY `id_version_model` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- Contraintes pour les tables déchargées
--

--
-- Contraintes pour la table `boxs`
--
ALTER TABLE `boxs`
  ADD CONSTRAINT `boxs_ibfk_1` FOREIGN KEY (`id_store`) REFERENCES `stores` (`id_store`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `commandes_objets`
--
ALTER TABLE `commandes_objets`
  ADD CONSTRAINT `commandes_objets_ibfk_1` FOREIGN KEY (`id_objet`) REFERENCES `objets` (`id_objet`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `commandes_objets_ibfk_2` FOREIGN KEY (`id_commande`) REFERENCES `commandes` (`id_commande`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `model_imgs`
--
ALTER TABLE `model_imgs`
  ADD CONSTRAINT `model_imgs_ibfk_1` FOREIGN KEY (`id_model_objet`) REFERENCES `model_objets` (`id_model_objet`) ON UPDATE CASCADE;

--
-- Contraintes pour la table `model_imgs_version_models`
--
ALTER TABLE `model_imgs_version_models`
  ADD CONSTRAINT `model_imgs_version_models_ibfk_1` FOREIGN KEY (`id_model_img`) REFERENCES `model_imgs` (`id_model_img`) ON UPDATE CASCADE,
  ADD CONSTRAINT `model_imgs_version_models_ibfk_2` FOREIGN KEY (`id_version_model`) REFERENCES `version_models` (`id_version_model`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `objets`
--
ALTER TABLE `objets`
  ADD CONSTRAINT `objets_ibfk_1` FOREIGN KEY (`id_box`) REFERENCES `boxs` (`id_box`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `projets_objets`
--
ALTER TABLE `projets_objets`
  ADD CONSTRAINT `projets_objets_ibfk_1` FOREIGN KEY (`id_objet`) REFERENCES `objets` (`id_objet`) ON UPDATE CASCADE,
  ADD CONSTRAINT `projets_objets_ibfk_2` FOREIGN KEY (`id_projet`) REFERENCES `projets` (`id_projet`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `tags_boxs`
--
ALTER TABLE `tags_boxs`
  ADD CONSTRAINT `tags_boxs_ibfk_1` FOREIGN KEY (`id_box`) REFERENCES `boxs` (`id_box`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `tags_boxs_ibfk_2` FOREIGN KEY (`id_tag`) REFERENCES `tags` (`id_tag`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `tags_model_objets`
--
ALTER TABLE `tags_model_objets`
  ADD CONSTRAINT `tags_model_objets_ibfk_1` FOREIGN KEY (`id_tag`) REFERENCES `tags` (`id_tag`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `tags_model_objets_ibfk_2` FOREIGN KEY (`id_model_objet`) REFERENCES `model_objets` (`id_model_objet`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `tags_objets`
--
ALTER TABLE `tags_objets`
  ADD CONSTRAINT `tags_objets_ibfk_1` FOREIGN KEY (`id_objet`) REFERENCES `objets` (`id_objet`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `tags_objets_ibfk_2` FOREIGN KEY (`id_tag`) REFERENCES `tags` (`id_tag`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `utilisateurs`
--
ALTER TABLE `utilisateurs`
  ADD CONSTRAINT `utilisateurs_ibfk_1` FOREIGN KEY (`id_version_model`) REFERENCES `version_models` (`id_version_model`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `utilisateurs_ibfk_2` FOREIGN KEY (`id_camera`) REFERENCES `cameras` (`id_camera`) ON DELETE SET NULL ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
