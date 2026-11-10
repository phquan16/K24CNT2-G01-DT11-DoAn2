using FoodOrder.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace FoodOrder.Data;

public partial class FoodOrderContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public FoodOrderContext(DbContextOptions<FoodOrderContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductIngredient> ProductIngredients { get; set; }

    public virtual DbSet<ProductOption> ProductOptions { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Table> Tables { get; set; }

    public virtual DbSet<TableSession> TableSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>().Property(u => u.Id).HasColumnName("UserId");
        modelBuilder.Entity<IdentityRole>().Property(r => r.Id).HasColumnName("RoleId");
        modelBuilder.Entity<IdentityUserClaim<string>>().Property(c => c.Id).HasColumnName("UserClaimId");
        modelBuilder.Entity<IdentityRoleClaim<string>>().Property(c => c.Id).HasColumnName("RoleClaimId");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
