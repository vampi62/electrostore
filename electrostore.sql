-- phpMyAdmin SQL Dump
-- version 5.2.0
-- https://www.phpmyadmin.net/
--
-- Hôte : 192.168.2.52
-- Généré le : mer. 19 avr. 2023 à 16:29
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
-- Structure de la table `box`
--

CREATE TABLE `box` (
  `id_box` int(11) UNSIGNED NOT NULL,
  `ymin` int(11) UNSIGNED NOT NULL,
  `ymax` int(11) UNSIGNED NOT NULL,
  `xmin` int(11) UNSIGNED NOT NULL,
  `xmax` int(11) UNSIGNED NOT NULL,
  `compartiment` int(11) UNSIGNED NOT NULL,
  `id_store` int(11) UNSIGNED NOT NULL,
  `id_utilisateur` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `groupe`
--

CREATE TABLE `groupe` (
  `id_groupe` int(11) UNSIGNED NOT NULL,
  `nom_groupe` varchar(50) NOT NULL,
  `id_utilisateur` int(11) UNSIGNED NOT NULL,
  `id_type_groupe` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `groupe_box`
--

CREATE TABLE `groupe_box` (
  `id_groupe` int(11) UNSIGNED NOT NULL,
  `id_box` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `groupe_store`
--

CREATE TABLE `groupe_store` (
  `id_groupe` int(11) UNSIGNED NOT NULL,
  `id_store` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `groupe_utilisateur`
--

CREATE TABLE `groupe_utilisateur` (
  `id_groupe` int(11) UNSIGNED NOT NULL,
  `id_utilisateur` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `location`
--

CREATE TABLE `location` (
  `id_location` int(11) UNSIGNED NOT NULL,
  `nom` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `objet`
--

CREATE TABLE `objet` (
  `id_objet` int(11) UNSIGNED NOT NULL,
  `nom` char(50) NOT NULL,
  `quant` int(11) UNSIGNED NOT NULL,
  `compartient` int(11) UNSIGNED NOT NULL,
  `description` text NOT NULL,
  `id_box` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `role`
--

CREATE TABLE `role` (
  `id_role` int(11) UNSIGNED NOT NULL,
  `nom` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `store`
--

CREATE TABLE `store` (
  `id_store` int(11) UNSIGNED NOT NULL,
  `nom` varchar(50) NOT NULL,
  `x` int(11) UNSIGNED NOT NULL,
  `y` int(11) UNSIGNED NOT NULL,
  `id_esp` varchar(50) NOT NULL,
  `piece` varchar(50) NOT NULL,
  `id_utilisateur` int(11) UNSIGNED NOT NULL,
  `id_location` int(11) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `tag`
--

CREATE TABLE `tag` (
  `id_tag` int(11) UNSIGNED NOT NULL,
  `nom` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `tag_box`
--

CREATE TABLE `tag_box` (
  `id_tag` int(11) UNSIGNED NOT NULL,
  `id_box` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `tag_objet`
--

CREATE TABLE `tag_objet` (
  `id_objet` int(11) UNSIGNED NOT NULL,
  `id_tag` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `type_groupe`
--

CREATE TABLE `type_groupe` (
  `id_type_groupe` int(11) UNSIGNED NOT NULL,
  `nom` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `utilisateur`
--

CREATE TABLE `utilisateur` (
  `id_utilisateur` int(11) UNSIGNED NOT NULL,
  `mdp` varchar(50) NOT NULL,
  `email` varchar(50) NOT NULL,
  `reset` varchar(50) NOT NULL,
  `id_role` int(11) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Index pour les tables déchargées
--

--
-- Index pour la table `box`
--
ALTER TABLE `box`
  ADD PRIMARY KEY (`id_box`),
  ADD KEY `box_store_FK` (`id_store`),
  ADD KEY `box_utilisateur0_FK` (`id_utilisateur`);

--
-- Index pour la table `groupe`
--
ALTER TABLE `groupe`
  ADD PRIMARY KEY (`id_groupe`),
  ADD KEY `groupe_type_groupe0_FK` (`id_type_groupe`),
  ADD KEY `groupe_utilisateur_FK` (`id_utilisateur`);

--
-- Index pour la table `groupe_box`
--
ALTER TABLE `groupe_box`
  ADD PRIMARY KEY (`id_groupe`,`id_box`),
  ADD KEY `groupe_box_box0_FK` (`id_box`);

--
-- Index pour la table `groupe_store`
--
ALTER TABLE `groupe_store`
  ADD PRIMARY KEY (`id_groupe`,`id_store`),
  ADD KEY `groupe_store_store0_FK` (`id_store`);

--
-- Index pour la table `groupe_utilisateur`
--
ALTER TABLE `groupe_utilisateur`
  ADD PRIMARY KEY (`id_groupe`,`id_utilisateur`),
  ADD KEY `groupe_utilisateur_utilisateur0_FK` (`id_utilisateur`);

--
-- Index pour la table `location`
--
ALTER TABLE `location`
  ADD PRIMARY KEY (`id_location`);

--
-- Index pour la table `objet`
--
ALTER TABLE `objet`
  ADD PRIMARY KEY (`id_objet`),
  ADD KEY `objet_box_FK` (`id_box`);

--
-- Index pour la table `role`
--
ALTER TABLE `role`
  ADD PRIMARY KEY (`id_role`);

--
-- Index pour la table `store`
--
ALTER TABLE `store`
  ADD PRIMARY KEY (`id_store`),
  ADD KEY `store_location0_FK` (`id_location`),
  ADD KEY `store_utilisateur_FK` (`id_utilisateur`);

--
-- Index pour la table `tag`
--
ALTER TABLE `tag`
  ADD PRIMARY KEY (`id_tag`);

--
-- Index pour la table `tag_box`
--
ALTER TABLE `tag_box`
  ADD PRIMARY KEY (`id_tag`,`id_box`),
  ADD KEY `tag_box_box0_FK` (`id_box`);

--
-- Index pour la table `tag_objet`
--
ALTER TABLE `tag_objet`
  ADD PRIMARY KEY (`id_objet`,`id_tag`),
  ADD KEY `tag_objet_tag0_FK` (`id_tag`);

--
-- Index pour la table `type_groupe`
--
ALTER TABLE `type_groupe`
  ADD PRIMARY KEY (`id_type_groupe`);

--
-- Index pour la table `utilisateur`
--
ALTER TABLE `utilisateur`
  ADD PRIMARY KEY (`id_utilisateur`),
  ADD KEY `utilisateur_role_FK` (`id_role`);

--
-- AUTO_INCREMENT pour les tables déchargées
--

--
-- AUTO_INCREMENT pour la table `box`
--
ALTER TABLE `box`
  MODIFY `id_box` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `groupe`
--
ALTER TABLE `groupe`
  MODIFY `id_groupe` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `location`
--
ALTER TABLE `location`
  MODIFY `id_location` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `objet`
--
ALTER TABLE `objet`
  MODIFY `id_objet` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `role`
--
ALTER TABLE `role`
  MODIFY `id_role` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `store`
--
ALTER TABLE `store`
  MODIFY `id_store` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `tag`
--
ALTER TABLE `tag`
  MODIFY `id_tag` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `type_groupe`
--
ALTER TABLE `type_groupe`
  MODIFY `id_type_groupe` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `utilisateur`
--
ALTER TABLE `utilisateur`
  MODIFY `id_utilisateur` int(11) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- Contraintes pour les tables déchargées
--

--
-- Contraintes pour la table `box`
--
ALTER TABLE `box`
  ADD CONSTRAINT `box_store_FK` FOREIGN KEY (`id_store`) REFERENCES `store` (`id_store`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `box_utilisateur0_FK` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`) ON UPDATE CASCADE;

--
-- Contraintes pour la table `groupe`
--
ALTER TABLE `groupe`
  ADD CONSTRAINT `groupe_type_groupe0_FK` FOREIGN KEY (`id_type_groupe`) REFERENCES `type_groupe` (`id_type_groupe`) ON UPDATE CASCADE,
  ADD CONSTRAINT `groupe_utilisateur_FK` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`) ON UPDATE CASCADE;

--
-- Contraintes pour la table `groupe_box`
--
ALTER TABLE `groupe_box`
  ADD CONSTRAINT `groupe_box_box0_FK` FOREIGN KEY (`id_box`) REFERENCES `box` (`id_box`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `groupe_box_groupe_FK` FOREIGN KEY (`id_groupe`) REFERENCES `groupe` (`id_groupe`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `groupe_store`
--
ALTER TABLE `groupe_store`
  ADD CONSTRAINT `groupe_store_groupe_FK` FOREIGN KEY (`id_groupe`) REFERENCES `groupe` (`id_groupe`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `groupe_store_store0_FK` FOREIGN KEY (`id_store`) REFERENCES `store` (`id_store`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `groupe_utilisateur`
--
ALTER TABLE `groupe_utilisateur`
  ADD CONSTRAINT `groupe_utilisateur_groupe_FK` FOREIGN KEY (`id_groupe`) REFERENCES `groupe` (`id_groupe`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `groupe_utilisateur_utilisateur0_FK` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `objet`
--
ALTER TABLE `objet`
  ADD CONSTRAINT `objet_box_FK` FOREIGN KEY (`id_box`) REFERENCES `box` (`id_box`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `store`
--
ALTER TABLE `store`
  ADD CONSTRAINT `store_location0_FK` FOREIGN KEY (`id_location`) REFERENCES `location` (`id_location`) ON UPDATE CASCADE,
  ADD CONSTRAINT `store_utilisateur_FK` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`) ON UPDATE CASCADE;

--
-- Contraintes pour la table `tag_box`
--
ALTER TABLE `tag_box`
  ADD CONSTRAINT `tag_box_box0_FK` FOREIGN KEY (`id_box`) REFERENCES `box` (`id_box`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `tag_box_tag_FK` FOREIGN KEY (`id_tag`) REFERENCES `tag` (`id_tag`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `tag_objet`
--
ALTER TABLE `tag_objet`
  ADD CONSTRAINT `tag_objet_objet_FK` FOREIGN KEY (`id_objet`) REFERENCES `objet` (`id_objet`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `tag_objet_tag0_FK` FOREIGN KEY (`id_tag`) REFERENCES `tag` (`id_tag`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `utilisateur`
--
ALTER TABLE `utilisateur`
  ADD CONSTRAINT `utilisateur_role_FK` FOREIGN KEY (`id_role`) REFERENCES `role` (`id_role`) ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
