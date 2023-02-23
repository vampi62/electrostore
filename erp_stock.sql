-- phpMyAdmin SQL Dump
-- version 5.2.0
-- https://www.phpmyadmin.net/
--
-- Hôte : 192.168.2.52
-- Généré le : jeu. 23 fév. 2023 à 21:40
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
-- Base de données : `erp_stock`
--

DELIMITER $$
--
-- Procédures
--
CREATE DEFINER=`root`@`%` PROCEDURE `test_recursif` (IN `objet_groupe_id` INT UNSIGNED)   WITH RECURSIVE returnlist AS (
  SELECT
    id_groupe,
    nom,
    racine
  FROM groupe
  WHERE id_groupe = objet_groupe_id
 
  UNION ALL
   
  SELECT    groupe.id_groupe,
            groupe.nom,
            groupe.racine
  FROM groupe, returnlist
  WHERE groupe.id_groupe = returnlist.racine
)
 
SELECT   returnlist.nom AS nom_dossier,
       groupe.nom AS nom_racine,
       returnlist.id_groupe AS id_groupe
FROM returnlist
LEFT JOIN groupe
ON returnlist.racine = groupe.id_groupe
ORDER BY returnlist.racine$$

DELIMITER ;

-- --------------------------------------------------------

--
-- Structure de la table `box`
--

CREATE TABLE `box` (
  `id_box` int(11) NOT NULL,
  `ymin` int(11) NOT NULL,
  `ymax` int(11) NOT NULL,
  `xmin` int(11) NOT NULL,
  `xmax` int(11) NOT NULL,
  `compartiment` int(11) NOT NULL,
  `id_store` int(11) NOT NULL,
  `id_groupe` int(11) DEFAULT NULL,
  `id_utilisateur` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `groupe`
--

CREATE TABLE `groupe` (
  `id_groupe` int(11) NOT NULL,
  `nom_groupe` varchar(50) NOT NULL,
  `id_utilisateur` int(11) NOT NULL,
  `id_groupe_parent` int(11) DEFAULT NULL,
  `id_type_groupe` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `objet`
--

CREATE TABLE `objet` (
  `id_objet` int(11) NOT NULL,
  `nom` char(50) NOT NULL,
  `quant` int(11) NOT NULL,
  `compartient` int(11) NOT NULL,
  `description` text NOT NULL,
  `id_box` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `role`
--

CREATE TABLE `role` (
  `id_role` int(11) NOT NULL,
  `nom` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `store`
--

CREATE TABLE `store` (
  `id_store` int(11) NOT NULL,
  `nom` varchar(50) NOT NULL,
  `x` int(11) NOT NULL,
  `y` int(11) NOT NULL,
  `id_esp` varchar(50) NOT NULL,
  `piece` varchar(50) NOT NULL,
  `id_groupe` int(11) DEFAULT NULL,
  `id_utilisateur` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `type_groupe`
--

CREATE TABLE `type_groupe` (
  `id_type_groupe` int(11) NOT NULL,
  `nom` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `utilisateur`
--

CREATE TABLE `utilisateur` (
  `id_utilisateur` int(11) NOT NULL,
  `mdp` varchar(50) NOT NULL,
  `email` varchar(50) NOT NULL,
  `reset` varchar(50) NOT NULL,
  `id_role` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Structure de la table `utilisateur_groupe`
--

CREATE TABLE `utilisateur_groupe` (
  `id_groupe` int(11) NOT NULL,
  `id_utilisateur` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Index pour les tables déchargées
--

--
-- Index pour la table `box`
--
ALTER TABLE `box`
  ADD PRIMARY KEY (`id_box`),
  ADD KEY `box_groupe1_FK` (`id_groupe`),
  ADD KEY `box_store0_FK` (`id_store`),
  ADD KEY `box_utilisateur2_FK` (`id_utilisateur`);

--
-- Index pour la table `groupe`
--
ALTER TABLE `groupe`
  ADD PRIMARY KEY (`id_groupe`),
  ADD KEY `groupe_type_groupe2_FK` (`id_type_groupe`),
  ADD KEY `groupe_groupe1_FK` (`id_groupe_parent`),
  ADD KEY `groupe_utilisateur0_FK` (`id_utilisateur`);

--
-- Index pour la table `objet`
--
ALTER TABLE `objet`
  ADD PRIMARY KEY (`id_objet`),
  ADD KEY `objet_box0_FK` (`id_box`);

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
  ADD KEY `store_groupe0_FK` (`id_groupe`),
  ADD KEY `store_utilisateur1_FK` (`id_utilisateur`);

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
  ADD KEY `utilisateur_role0_FK` (`id_role`);

--
-- Index pour la table `utilisateur_groupe`
--
ALTER TABLE `utilisateur_groupe`
  ADD PRIMARY KEY (`id_groupe`,`id_utilisateur`),
  ADD KEY `utilisateur_groupe_utilisateur1_FK` (`id_utilisateur`);

--
-- AUTO_INCREMENT pour les tables déchargées
--

--
-- AUTO_INCREMENT pour la table `box`
--
ALTER TABLE `box`
  MODIFY `id_box` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `groupe`
--
ALTER TABLE `groupe`
  MODIFY `id_groupe` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `objet`
--
ALTER TABLE `objet`
  MODIFY `id_objet` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `role`
--
ALTER TABLE `role`
  MODIFY `id_role` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `store`
--
ALTER TABLE `store`
  MODIFY `id_store` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `type_groupe`
--
ALTER TABLE `type_groupe`
  MODIFY `id_type_groupe` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `utilisateur`
--
ALTER TABLE `utilisateur`
  MODIFY `id_utilisateur` int(11) NOT NULL AUTO_INCREMENT;

--
-- Contraintes pour les tables déchargées
--

--
-- Contraintes pour la table `box`
--
ALTER TABLE `box`
  ADD CONSTRAINT `box_groupe1_FK` FOREIGN KEY (`id_groupe`) REFERENCES `groupe` (`id_groupe`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `box_store0_FK` FOREIGN KEY (`id_store`) REFERENCES `store` (`id_store`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `box_utilisateur2_FK` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`) ON DELETE NO ACTION ON UPDATE CASCADE;

--
-- Contraintes pour la table `groupe`
--
ALTER TABLE `groupe`
  ADD CONSTRAINT `groupe_groupe1_FK` FOREIGN KEY (`id_groupe_parent`) REFERENCES `groupe` (`id_groupe`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `groupe_type_groupe2_FK` FOREIGN KEY (`id_type_groupe`) REFERENCES `type_groupe` (`id_type_groupe`),
  ADD CONSTRAINT `groupe_utilisateur0_FK` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `objet`
--
ALTER TABLE `objet`
  ADD CONSTRAINT `objet_box0_FK` FOREIGN KEY (`id_box`) REFERENCES `box` (`id_box`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `store`
--
ALTER TABLE `store`
  ADD CONSTRAINT `store_groupe0_FK` FOREIGN KEY (`id_groupe`) REFERENCES `groupe` (`id_groupe`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `store_utilisateur1_FK` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`) ON DELETE NO ACTION ON UPDATE CASCADE;

--
-- Contraintes pour la table `utilisateur`
--
ALTER TABLE `utilisateur`
  ADD CONSTRAINT `utilisateur_role0_FK` FOREIGN KEY (`id_role`) REFERENCES `role` (`id_role`);

--
-- Contraintes pour la table `utilisateur_groupe`
--
ALTER TABLE `utilisateur_groupe`
  ADD CONSTRAINT `utilisateur_groupe_groupe0_FK` FOREIGN KEY (`id_groupe`) REFERENCES `groupe` (`id_groupe`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `utilisateur_groupe_utilisateur1_FK` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
