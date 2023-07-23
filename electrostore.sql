-- phpMyAdmin SQL Dump
-- version 5.2.0
-- https://www.phpmyadmin.net/
--
-- Hôte : 192.168.2.52
-- Généré le : mer. 19 juil. 2023 à 20:24
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
-- Structure de la table `apikeys`
--

CREATE TABLE `apikeys` (
  `id_apikey` int(11) UNSIGNED NOT NULL,
  `nom_apikey` varchar(50) NOT NULL,
  `key_apikey` varchar(200) NOT NULL,
  `id_user` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `boxs`
--

CREATE TABLE `boxs` (
  `id_box` int(11) UNSIGNED NOT NULL,
  `ymin_box` tinyint(11) UNSIGNED NOT NULL,
  `xmin_box` tinyint(11) UNSIGNED NOT NULL,
  `ymax_box` tinyint(11) UNSIGNED NOT NULL,
  `xmax_box` tinyint(11) UNSIGNED NOT NULL,
  `id_store` int(11) UNSIGNED NOT NULL,
  `nbr_compartiment` tinyint(3) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `cameras`
--

CREATE TABLE `cameras` (
  `id_camera` int(11) UNSIGNED NOT NULL,
  `ip_camera` varchar(50) NOT NULL,
  `port_camera` int(11) UNSIGNED NOT NULL,
  `flux_camera` varchar(50) NOT NULL,
  `snapshot_camera` varchar(50) NOT NULL,
  `nom_camera` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `commandes`
--

CREATE TABLE `commandes` (
  `id_commande` int(11) UNSIGNED NOT NULL,
  `nom_commande` varchar(50) NOT NULL,
  `lien_commande` varchar(200) NOT NULL,
  `date_commande` date NOT NULL,
  `status_commande` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `commandes_objets`
--

CREATE TABLE `commandes_objets` (
  `id_commande` int(11) UNSIGNED NOT NULL,
  `id_objet` int(11) UNSIGNED NOT NULL,
  `prix_ttc_U_commandes_objets` float UNSIGNED NOT NULL,
  `quant_commandes_objets` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `droits`
--

CREATE TABLE `droits` (
  `id_droit` int(11) UNSIGNED NOT NULL,
  `nom_droit` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `droits_users`
--

CREATE TABLE `droits_users` (
  `id_user` int(11) UNSIGNED NOT NULL,
  `id_droit` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `images`
--

CREATE TABLE `images` (
  `id_image` int(11) UNSIGNED NOT NULL,
  `nom_image` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `objets`
--

CREATE TABLE `objets` (
  `id_objet` int(11) UNSIGNED NOT NULL,
  `nom_objet` varchar(50) NOT NULL,
  `quant_objet` int(11) UNSIGNED NOT NULL,
  `description_objet` varchar(50) NOT NULL,
  `id_box` int(11) UNSIGNED NOT NULL,
  `min_quant_objet` int(11) UNSIGNED DEFAULT NULL,
  `lien_achat_objet` varchar(200) DEFAULT NULL,
  `qrcodeint_objet` varchar(200) NOT NULL,
  `qrcodeext_objet` varchar(200) DEFAULT NULL,
  `compartiment_objet` tinyint(3) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `projets`
--

CREATE TABLE `projets` (
  `id_projet` int(11) UNSIGNED NOT NULL,
  `nom_projet` varchar(50) NOT NULL,
  `description_projet` text NOT NULL,
  `lien_projet` varchar(200) NOT NULL,
  `status_projet` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `projets_objets`
--

CREATE TABLE `projets_objets` (
  `id_projet` int(11) UNSIGNED NOT NULL,
  `id_objet` int(11) UNSIGNED NOT NULL,
  `quant_projets_objets` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `projets_users`
--

CREATE TABLE `projets_users` (
  `id_projet` int(11) UNSIGNED NOT NULL,
  `id_user` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `stores`
--

CREATE TABLE `stores` (
  `id_store` int(11) UNSIGNED NOT NULL,
  `nom_store` varchar(50) NOT NULL,
  `x_store` tinyint(11) UNSIGNED NOT NULL,
  `y_store` tinyint(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `tags`
--

CREATE TABLE `tags` (
  `id_tag` int(11) UNSIGNED NOT NULL,
  `nom_tag` varchar(50) NOT NULL
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
-- Structure de la table `tags_images`
--

CREATE TABLE `tags_images` (
  `id_tag` int(11) UNSIGNED NOT NULL,
  `id_image` int(11) UNSIGNED NOT NULL
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
-- Structure de la table `users`
--

CREATE TABLE `users` (
  `id_user` int(11) UNSIGNED NOT NULL,
  `pseudo_user` varchar(50) NOT NULL,
  `mdp_user` varchar(200) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Index pour les tables déchargées
--

--
-- Index pour la table `apikeys`
--
ALTER TABLE `apikeys`
  ADD PRIMARY KEY (`id_apikey`),
  ADD UNIQUE KEY `key_apikey` (`key_apikey`),
  ADD KEY `id_user` (`id_user`);

--
-- Index pour la table `boxs`
--
ALTER TABLE `boxs`
  ADD PRIMARY KEY (`id_box`),
  ADD KEY `id_store` (`id_store`);

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
  ADD PRIMARY KEY (`id_commande`,`id_objet`),
  ADD KEY `commandes_objets_ibfk_2` (`id_objet`);

--
-- Index pour la table `droits`
--
ALTER TABLE `droits`
  ADD PRIMARY KEY (`id_droit`);

--
-- Index pour la table `droits_users`
--
ALTER TABLE `droits_users`
  ADD PRIMARY KEY (`id_user`,`id_droit`),
  ADD KEY `droits_users_ibfk_2` (`id_droit`);

--
-- Index pour la table `images`
--
ALTER TABLE `images`
  ADD PRIMARY KEY (`id_image`);

--
-- Index pour la table `objets`
--
ALTER TABLE `objets`
  ADD PRIMARY KEY (`id_objet`),
  ADD KEY `id_box` (`id_box`);

--
-- Index pour la table `projets`
--
ALTER TABLE `projets`
  ADD PRIMARY KEY (`id_projet`);

--
-- Index pour la table `projets_objets`
--
ALTER TABLE `projets_objets`
  ADD PRIMARY KEY (`id_projet`,`id_objet`),
  ADD KEY `projet_objets_ibfk_2` (`id_objet`);

--
-- Index pour la table `projets_users`
--
ALTER TABLE `projets_users`
  ADD PRIMARY KEY (`id_projet`,`id_user`),
  ADD KEY `projets_users_ibfk_2` (`id_user`);

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
  ADD KEY `id_tag` (`id_tag`);

--
-- Index pour la table `tags_images`
--
ALTER TABLE `tags_images`
  ADD PRIMARY KEY (`id_tag`,`id_image`),
  ADD KEY `id_image` (`id_image`);

--
-- Index pour la table `tags_objets`
--
ALTER TABLE `tags_objets`
  ADD PRIMARY KEY (`id_objet`,`id_tag`),
  ADD KEY `id_tag` (`id_tag`);

--
-- Index pour la table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id_user`);

--
-- AUTO_INCREMENT pour les tables déchargées
--

--
-- AUTO_INCREMENT pour la table `apikeys`
--
ALTER TABLE `apikeys`
  MODIFY `id_apikey` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

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
-- AUTO_INCREMENT pour la table `droits`
--
ALTER TABLE `droits`
  MODIFY `id_droit` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `images`
--
ALTER TABLE `images`
  MODIFY `id_image` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

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
-- AUTO_INCREMENT pour la table `users`
--
ALTER TABLE `users`
  MODIFY `id_user` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- Contraintes pour les tables déchargées
--

--
-- Contraintes pour la table `apikeys`
--
ALTER TABLE `apikeys`
  ADD CONSTRAINT `apikeys_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `users` (`id_user`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `boxs`
--
ALTER TABLE `boxs`
  ADD CONSTRAINT `boxs_ibfk_1` FOREIGN KEY (`id_store`) REFERENCES `stores` (`id_store`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `commandes_objets`
--
ALTER TABLE `commandes_objets`
  ADD CONSTRAINT `commandes_objets_ibfk_1` FOREIGN KEY (`id_commande`) REFERENCES `commandes` (`id_commande`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `commandes_objets_ibfk_2` FOREIGN KEY (`id_objet`) REFERENCES `objets` (`id_objet`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `droits_users`
--
ALTER TABLE `droits_users`
  ADD CONSTRAINT `droits_users_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `users` (`id_user`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `droits_users_ibfk_2` FOREIGN KEY (`id_droit`) REFERENCES `droits` (`id_droit`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `objets`
--
ALTER TABLE `objets`
  ADD CONSTRAINT `objets_ibfk_1` FOREIGN KEY (`id_box`) REFERENCES `boxs` (`id_box`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `projets_objets`
--
ALTER TABLE `projets_objets`
  ADD CONSTRAINT `projet_objets_ibfk_1` FOREIGN KEY (`id_projet`) REFERENCES `projets` (`id_projet`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `projet_objets_ibfk_2` FOREIGN KEY (`id_objet`) REFERENCES `objets` (`id_objet`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `projets_users`
--
ALTER TABLE `projets_users`
  ADD CONSTRAINT `projets_users_ibfk_1` FOREIGN KEY (`id_projet`) REFERENCES `projets` (`id_projet`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `projets_users_ibfk_2` FOREIGN KEY (`id_user`) REFERENCES `users` (`id_user`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `tags_boxs`
--
ALTER TABLE `tags_boxs`
  ADD CONSTRAINT `tags_boxs_ibfk_1` FOREIGN KEY (`id_box`) REFERENCES `boxs` (`id_box`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `tags_boxs_ibfk_2` FOREIGN KEY (`id_tag`) REFERENCES `tags` (`id_tag`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `tags_images`
--
ALTER TABLE `tags_images`
  ADD CONSTRAINT `tags_images_ibfk_1` FOREIGN KEY (`id_tag`) REFERENCES `tags` (`id_tag`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `tags_images_ibfk_2` FOREIGN KEY (`id_image`) REFERENCES `images` (`id_image`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `tags_objets`
--
ALTER TABLE `tags_objets`
  ADD CONSTRAINT `tags_objets_ibfk_1` FOREIGN KEY (`id_objet`) REFERENCES `objets` (`id_objet`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `tags_objets_ibfk_2` FOREIGN KEY (`id_tag`) REFERENCES `tags` (`id_tag`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
