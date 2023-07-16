-- phpMyAdmin SQL Dump
-- version 5.2.0
-- https://www.phpmyadmin.net/
--
-- Hôte : 192.168.2.52
-- Généré le : sam. 15 juil. 2023 à 17:44
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
  `id_store` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `cameras`
--

CREATE TABLE `cameras` (
  `id_camera` int(11) UNSIGNED NOT NULL,
  `ip_camera` varchar(50) NOT NULL,
  `port_camera` int(11) UNSIGNED NOT NULL,
  `nom_camera` varchar(50) NOT NULL
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
  `nom_image` varchar(50) DEFAULT NULL
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
  `id_box` int(11) UNSIGNED NOT NULL
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
  `nom_tag` varchar(50) DEFAULT NULL
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
-- Index pour la table `droits`
--
ALTER TABLE `droits`
  ADD PRIMARY KEY (`id_droit`);

--
-- Index pour la table `droits_users`
--
ALTER TABLE `droits_users`
  ADD PRIMARY KEY (`id_user`,`id_droit`),
  ADD KEY `id_droit` (`id_droit`);

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
  ADD CONSTRAINT `apikeys_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `users` (`id_user`);

--
-- Contraintes pour la table `boxs`
--
ALTER TABLE `boxs`
  ADD CONSTRAINT `boxs_ibfk_1` FOREIGN KEY (`id_store`) REFERENCES `stores` (`id_store`);

--
-- Contraintes pour la table `droits_users`
--
ALTER TABLE `droits_users`
  ADD CONSTRAINT `droits_users_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `users` (`id_user`),
  ADD CONSTRAINT `droits_users_ibfk_2` FOREIGN KEY (`id_droit`) REFERENCES `droits` (`id_droit`);

--
-- Contraintes pour la table `objets`
--
ALTER TABLE `objets`
  ADD CONSTRAINT `objets_ibfk_1` FOREIGN KEY (`id_box`) REFERENCES `boxs` (`id_box`);

--
-- Contraintes pour la table `tags_images`
--
ALTER TABLE `tags_images`
  ADD CONSTRAINT `tags_images_ibfk_1` FOREIGN KEY (`id_tag`) REFERENCES `tags` (`id_tag`),
  ADD CONSTRAINT `tags_images_ibfk_2` FOREIGN KEY (`id_image`) REFERENCES `images` (`id_image`);

--
-- Contraintes pour la table `tags_objets`
--
ALTER TABLE `tags_objets`
  ADD CONSTRAINT `tags_objets_ibfk_1` FOREIGN KEY (`id_objet`) REFERENCES `objets` (`id_objet`),
  ADD CONSTRAINT `tags_objets_ibfk_2` FOREIGN KEY (`id_tag`) REFERENCES `tags` (`id_tag`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
