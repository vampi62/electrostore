﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using electrostore;

#nullable disable

namespace electrostore.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20240819163742_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.16")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("electrostore.Models.Boxs", b =>
                {
                    b.Property<int>("id_box")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("id_store")
                        .HasColumnType("int");

                    b.Property<int>("xend_box")
                        .HasColumnType("int");

                    b.Property<int>("xstart_box")
                        .HasColumnType("int");

                    b.Property<int>("yend_box")
                        .HasColumnType("int");

                    b.Property<int>("ystart_box")
                        .HasColumnType("int");

                    b.HasKey("id_box");

                    b.HasIndex("id_store");

                    b.ToTable("Boxs");
                });

            modelBuilder.Entity("electrostore.Models.BoxsTags", b =>
                {
                    b.Property<int>("id_box")
                        .HasColumnType("int");

                    b.Property<int>("id_tag")
                        .HasColumnType("int");

                    b.HasKey("id_box", "id_tag");

                    b.HasIndex("id_tag");

                    b.ToTable("BoxsTags");
                });

            modelBuilder.Entity("electrostore.Models.Cameras", b =>
                {
                    b.Property<int>("id_camera")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("mdp_camera")
                        .HasColumnType("longtext");

                    b.Property<string>("nom_camera")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("url_camera")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("user_camera")
                        .HasColumnType("longtext");

                    b.HasKey("id_camera");

                    b.ToTable("Cameras");
                });

            modelBuilder.Entity("electrostore.Models.Commands", b =>
                {
                    b.Property<int>("id_command")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("date_command")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("date_livraison_command")
                        .HasColumnType("datetime(6)");

                    b.Property<float>("prix_command")
                        .HasColumnType("float");

                    b.Property<string>("status_command")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("url_command")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("id_command");

                    b.ToTable("Commands");
                });

            modelBuilder.Entity("electrostore.Models.CommandsCommentaires", b =>
                {
                    b.Property<int>("id_commandcommentaire")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("contenu_commandcommentaire")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("date_commandcommentaire")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("date_modif_projetcommentaire")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("id_command")
                        .HasColumnType("int");

                    b.Property<int?>("id_user")
                        .HasColumnType("int");

                    b.HasKey("id_commandcommentaire");

                    b.HasIndex("id_command");

                    b.HasIndex("id_user");

                    b.ToTable("CommandsCommentaires");
                });

            modelBuilder.Entity("electrostore.Models.CommandsItems", b =>
                {
                    b.Property<int>("id_command")
                        .HasColumnType("int");

                    b.Property<int>("id_item")
                        .HasColumnType("int");

                    b.Property<float>("prix_commanditem")
                        .HasColumnType("float");

                    b.Property<int>("qte_commanditem")
                        .HasColumnType("int");

                    b.HasKey("id_command", "id_item");

                    b.HasIndex("id_item");

                    b.ToTable("CommandsItems");
                });

            modelBuilder.Entity("electrostore.Models.IA", b =>
                {
                    b.Property<int>("id_ia")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("date_ia")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("description_ia")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("nom_ia")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("trained_ia")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("id_ia");

                    b.ToTable("IA");
                });

            modelBuilder.Entity("electrostore.Models.IAImgs", b =>
                {
                    b.Property<int>("id_ia")
                        .HasColumnType("int");

                    b.Property<int>("id_img")
                        .HasColumnType("int");

                    b.HasKey("id_ia", "id_img");

                    b.HasIndex("id_img");

                    b.ToTable("IAImgs");
                });

            modelBuilder.Entity("electrostore.Models.Imgs", b =>
                {
                    b.Property<int>("id_img")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("date_img")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("description_img")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("id_item")
                        .HasColumnType("int");

                    b.Property<string>("nom_img")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("url_img")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("id_img");

                    b.HasIndex("id_item");

                    b.ToTable("Imgs");
                });

            modelBuilder.Entity("electrostore.Models.Items", b =>
                {
                    b.Property<int>("id_item")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("datasheet_item")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("description_item")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("id_img")
                        .HasColumnType("int");

                    b.Property<string>("nom_item")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("seuil_min_item")
                        .HasColumnType("int");

                    b.HasKey("id_item");

                    b.HasIndex("id_img");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("electrostore.Models.ItemsBoxs", b =>
                {
                    b.Property<int>("id_item")
                        .HasColumnType("int");

                    b.Property<int>("id_box")
                        .HasColumnType("int");

                    b.Property<int>("qte_itembox")
                        .HasColumnType("int");

                    b.Property<int>("seuil_max_itemitembox")
                        .HasColumnType("int");

                    b.HasKey("id_item", "id_box");

                    b.HasIndex("id_box");

                    b.ToTable("ItemsBoxs");
                });

            modelBuilder.Entity("electrostore.Models.ItemsTags", b =>
                {
                    b.Property<int>("id_item")
                        .HasColumnType("int");

                    b.Property<int>("id_tag")
                        .HasColumnType("int");

                    b.HasKey("id_item", "id_tag");

                    b.HasIndex("id_tag");

                    b.ToTable("ItemsTags");
                });

            modelBuilder.Entity("electrostore.Models.Leds", b =>
                {
                    b.Property<int>("id_led")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("id_store")
                        .HasColumnType("int");

                    b.Property<int>("mqtt_led_id")
                        .HasColumnType("int");

                    b.Property<int>("x_led")
                        .HasColumnType("int");

                    b.Property<int>("y_led")
                        .HasColumnType("int");

                    b.HasKey("id_led");

                    b.HasIndex("id_store");

                    b.ToTable("Leds");
                });

            modelBuilder.Entity("electrostore.Models.Projets", b =>
                {
                    b.Property<int>("id_projet")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime?>("date_fin_projet")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("date_projet")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("description_projet")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("nom_projet")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("status_projet")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("url_projet")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("id_projet");

                    b.ToTable("Projets");
                });

            modelBuilder.Entity("electrostore.Models.ProjetsCommentaires", b =>
                {
                    b.Property<int>("id_projetcommentaire")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("contenu_projetcommentaire")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("date_modif_projetcommentaire")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("date_projetcommentaire")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("id_projet")
                        .HasColumnType("int");

                    b.Property<int?>("id_user")
                        .HasColumnType("int");

                    b.HasKey("id_projetcommentaire");

                    b.HasIndex("id_projet");

                    b.HasIndex("id_user");

                    b.ToTable("ProjetsCommentaires");
                });

            modelBuilder.Entity("electrostore.Models.ProjetsItems", b =>
                {
                    b.Property<int>("id_projet")
                        .HasColumnType("int");

                    b.Property<int>("id_item")
                        .HasColumnType("int");

                    b.Property<int>("qte_projetitem")
                        .HasColumnType("int");

                    b.HasKey("id_projet", "id_item");

                    b.HasIndex("id_item");

                    b.ToTable("ProjetsItems");
                });

            modelBuilder.Entity("electrostore.Models.Stores", b =>
                {
                    b.Property<int>("id_store")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("mqtt_name_store")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("nom_store")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("xlength_store")
                        .HasColumnType("int");

                    b.Property<int>("ylength_store")
                        .HasColumnType("int");

                    b.HasKey("id_store");

                    b.ToTable("Stores");
                });

            modelBuilder.Entity("electrostore.Models.StoresTags", b =>
                {
                    b.Property<int>("id_store")
                        .HasColumnType("int");

                    b.Property<int>("id_tag")
                        .HasColumnType("int");

                    b.HasKey("id_store", "id_tag");

                    b.HasIndex("id_tag");

                    b.ToTable("StoresTags");
                });

            modelBuilder.Entity("electrostore.Models.Tags", b =>
                {
                    b.Property<int>("id_tag")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("nom_tag")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("poids_tag")
                        .HasColumnType("int");

                    b.HasKey("id_tag");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("electrostore.Models.Users", b =>
                {
                    b.Property<int>("id_user")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("email_user")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("mdp_user")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("nom_user")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("prenom_user")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("reset_token")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("reset_token_expiration")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("role_user")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("id_user");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("electrostore.Models.Boxs", b =>
                {
                    b.HasOne("electrostore.Models.Stores", "Store")
                        .WithMany()
                        .HasForeignKey("id_store")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Store");
                });

            modelBuilder.Entity("electrostore.Models.BoxsTags", b =>
                {
                    b.HasOne("electrostore.Models.Boxs", "Box")
                        .WithMany()
                        .HasForeignKey("id_box")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("electrostore.Models.Tags", "Tag")
                        .WithMany()
                        .HasForeignKey("id_tag")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Box");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("electrostore.Models.CommandsCommentaires", b =>
                {
                    b.HasOne("electrostore.Models.Commands", "Command")
                        .WithMany()
                        .HasForeignKey("id_command")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("electrostore.Models.Users", "User")
                        .WithMany()
                        .HasForeignKey("id_user");

                    b.Navigation("Command");

                    b.Navigation("User");
                });

            modelBuilder.Entity("electrostore.Models.CommandsItems", b =>
                {
                    b.HasOne("electrostore.Models.Commands", "Command")
                        .WithMany()
                        .HasForeignKey("id_command")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("electrostore.Models.Items", "Item")
                        .WithMany()
                        .HasForeignKey("id_item")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Command");

                    b.Navigation("Item");
                });

            modelBuilder.Entity("electrostore.Models.IAImgs", b =>
                {
                    b.HasOne("electrostore.Models.IA", "IA")
                        .WithMany()
                        .HasForeignKey("id_ia")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("electrostore.Models.Imgs", "Imgs")
                        .WithMany()
                        .HasForeignKey("id_img")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("IA");

                    b.Navigation("Imgs");
                });

            modelBuilder.Entity("electrostore.Models.Imgs", b =>
                {
                    b.HasOne("electrostore.Models.Items", "Item")
                        .WithMany()
                        .HasForeignKey("id_item")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");
                });

            modelBuilder.Entity("electrostore.Models.Items", b =>
                {
                    b.HasOne("electrostore.Models.Imgs", "Img")
                        .WithMany()
                        .HasForeignKey("id_img");

                    b.Navigation("Img");
                });

            modelBuilder.Entity("electrostore.Models.ItemsBoxs", b =>
                {
                    b.HasOne("electrostore.Models.Boxs", "Box")
                        .WithMany()
                        .HasForeignKey("id_box")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("electrostore.Models.Items", "Item")
                        .WithMany()
                        .HasForeignKey("id_item")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Box");

                    b.Navigation("Item");
                });

            modelBuilder.Entity("electrostore.Models.ItemsTags", b =>
                {
                    b.HasOne("electrostore.Models.Items", "Item")
                        .WithMany()
                        .HasForeignKey("id_item")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("electrostore.Models.Tags", "Tag")
                        .WithMany()
                        .HasForeignKey("id_tag")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("electrostore.Models.Leds", b =>
                {
                    b.HasOne("electrostore.Models.Stores", "Store")
                        .WithMany()
                        .HasForeignKey("id_store")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Store");
                });

            modelBuilder.Entity("electrostore.Models.ProjetsCommentaires", b =>
                {
                    b.HasOne("electrostore.Models.Projets", "Projet")
                        .WithMany()
                        .HasForeignKey("id_projet")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("electrostore.Models.Users", "User")
                        .WithMany()
                        .HasForeignKey("id_user");

                    b.Navigation("Projet");

                    b.Navigation("User");
                });

            modelBuilder.Entity("electrostore.Models.ProjetsItems", b =>
                {
                    b.HasOne("electrostore.Models.Items", "Item")
                        .WithMany()
                        .HasForeignKey("id_item")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("electrostore.Models.Projets", "Projet")
                        .WithMany()
                        .HasForeignKey("id_projet")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");

                    b.Navigation("Projet");
                });

            modelBuilder.Entity("electrostore.Models.StoresTags", b =>
                {
                    b.HasOne("electrostore.Models.Stores", "Store")
                        .WithMany()
                        .HasForeignKey("id_store")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("electrostore.Models.Tags", "Tag")
                        .WithMany()
                        .HasForeignKey("id_tag")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Store");

                    b.Navigation("Tag");
                });
#pragma warning restore 612, 618
        }
    }
}
