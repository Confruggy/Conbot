﻿// <auto-generated />
using System;
using Conbot.WelcomePlugin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Conbot.WelcomePlugin.Migrations
{
    [DbContext(typeof(WelcomeContext))]
    partial class WelcomeContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("Conbot.WelcomePlugin.WelcomeConfiguration", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("GoodbyeChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("GoodbyeMessageTemplate")
                        .HasColumnType("TEXT");

                    b.Property<bool>("ShowGoodbyeMessages")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowWelcomeMessages")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("WelcomeChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("WelcomeMessageTemplate")
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId");

                    b.ToTable("Configurations");
                });
#pragma warning restore 612, 618
        }
    }
}
