using System;
using System.Collections.Generic;
using Backend_Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Data;

public partial class LaptopHarbourDbContext : DbContext
{
    public LaptopHarbourDbContext()
    {
    }

    public LaptopHarbourDbContext(DbContextOptions<LaptopHarbourDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<Complaint> Complaints { get; set; }

    public virtual DbSet<ComplaintMessage> ComplaintMessages { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<MessageLog> MessageLogs { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderAddress> OrderAddresses { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductReview> ProductReviews { get; set; }

    public virtual DbSet<ProductSpecificationValue> ProductSpecificationValues { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<ProductView> ProductViews { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Refund> Refunds { get; set; }

    public virtual DbSet<Return> Returns { get; set; }

    public virtual DbSet<ReviewImage> ReviewImages { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<SearchHistory> SearchHistories { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<SpecificationDefinition> SpecificationDefinitions { get; set; }

    public virtual DbSet<SpecificationOption> SpecificationOptions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserRecentOrder> UserRecentOrders { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserVerification> UserVerifications { get; set; }

    public virtual DbSet<VariantPriceHistory> VariantPriceHistories { get; set; }

    public virtual DbSet<VariantSpecificationOption> VariantSpecificationOptions { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    public virtual DbSet<WishlistItem> WishlistItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__tmp_ms_x__CAA247C83C30046B");

            entity.ToTable("addresses");

            entity.HasIndex(e => e.CityId, "IX_addresses_city_id");

            entity.HasIndex(e => e.UserId, "IX_addresses_user_id");

            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.AddressLine1)
                .HasMaxLength(200)
                .HasColumnName("address_line1");
            entity.Property(e => e.AddressLine2)
                .HasMaxLength(200)
                .HasColumnName("address_line2");
            entity.Property(e => e.CityId).HasColumnName("city_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .HasColumnName("postal_code");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.City).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.CityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__addresses__city___14B10FFA");

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__addresses__user___13BCEBC1");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("PK__brands__5E5A8E2776EA7A2F");

            entity.ToTable("brands");

            entity.HasIndex(e => e.BrandName, "UQ__brands__0C0C3B58C58E067D").IsUnique();

            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.BrandName)
                .HasMaxLength(120)
                .HasColumnName("brand_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__carts__2EF52A2779F94932");

            entity.ToTable("carts");

            entity.HasIndex(e => e.UserId, "IX_carts_user_id");

            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__carts__user_id__038683F8");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__cart_ite__5D9A6C6E621F248A");

            entity.ToTable("cart_items");

            entity.HasIndex(e => e.CartId, "IX_cart_items_cart_id");

            entity.HasIndex(e => e.VariantId, "IX_cart_items_variant_id");

            entity.HasIndex(e => e.VariantSpecificationOptionsId, "IX_cart_items_variant_spec_option_id");

            entity.Property(e => e.CartItemId).HasColumnName("cart_item_id");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.VariantSpecificationOptionsId).HasColumnName("variant_specification_options_id");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__cart_item__cart___1BC821DD");

            entity.HasOne(d => d.Variant).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__cart_item__varia__184C96B4");

            entity.HasOne(d => d.VariantSpecificationOptions).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.VariantSpecificationOptionsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_cart_items_variant_spec_option");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__categori__D54EE9B4B54EA5AF");

            entity.ToTable("categories");

            entity.HasIndex(e => e.ParentCategoryId, "IX_categories_parent_category_id");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryImage)
                .HasMaxLength(500)
                .HasColumnName("category_image");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(120)
                .HasColumnName("category_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ParentCategoryId).HasColumnName("parent_category_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .HasConstraintName("FK__categorie__paren__6E01572D");
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.CityId).HasName("PK__cities__031491A8228165BA");

            entity.ToTable("cities");

            entity.HasIndex(e => e.CountryId, "IX_cities_country_id");

            entity.Property(e => e.CityId).HasColumnName("city_id");
            entity.Property(e => e.CityName)
                .HasMaxLength(100)
                .HasColumnName("city_name");
            entity.Property(e => e.CountryId).HasColumnName("country_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Country).WithMany(p => p.Cities)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__cities__country___656C112C");
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasKey(e => e.ComplaintId).HasName("PK__tmp_ms_x__A771F61C9F9898B7");

            entity.ToTable("complaints");

            entity.HasIndex(e => e.OrderId, "IX_complaints_order_id");

            entity.HasIndex(e => e.UserId, "IX_complaints_user_id");

            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .HasColumnName("description");
            entity.Property(e => e.Image)
                .HasMaxLength(500)
                .HasColumnName("image");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .HasColumnName("priority");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("open")
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(200)
                .HasColumnName("subject");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__complaint__order__62E4AA3C");

            entity.HasOne(d => d.User).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__complaint__user___61F08603");
        });

        modelBuilder.Entity<ComplaintMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__tmp_ms_x__0BBF6EE61DB39029");

            entity.ToTable("complaint_messages");

            entity.HasIndex(e => e.ComplaintId, "IX_complaint_messages_complaint_id");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Image)
                .HasMaxLength(500)
                .HasColumnName("image");
            entity.Property(e => e.Message)
                .HasMaxLength(2000)
                .HasColumnName("message");
            entity.Property(e => e.SenderType)
                .HasMaxLength(20)
                .HasColumnName("sender_type");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintMessages)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__complaint__compl__6A85CC04");
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.CountryId).HasName("PK__countrie__7E8CD055BAFC4AAE");

            entity.ToTable("countries");

            entity.HasIndex(e => e.CountryName, "UQ__countrie__F7018894A77368E0")
                .IsUnique()
                .HasFilter("([country_name] IS NOT NULL)");

            entity.Property(e => e.CountryId).HasColumnName("country_id");
            entity.Property(e => e.CountryName)
                .HasMaxLength(100)
                .HasColumnName("country_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<MessageLog>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__message___0BBF6EE64AC5716B");

            entity.ToTable("message_logs");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.Channel)
                .HasMaxLength(20)
                .HasColumnName("channel");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Recipient)
                .HasMaxLength(200)
                .HasColumnName("recipient");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__notifica__E059842F7EB913D6");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.UserId, "IX_notifications_user_id");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Message)
                .HasMaxLength(1000)
                .HasColumnName("message");
            entity.Property(e => e.TargetAudience)
                .HasMaxLength(20)
                .HasDefaultValue("user")
                .HasColumnName("target_audience");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__notificat__user___0E04126B");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__orders__46596229DF36D567");

            entity.ToTable("orders", tb => tb.HasTrigger("trg_ClearCart_AfterOrder"));

            entity.HasIndex(e => e.PaymentMethodId, "IX_orders_payment_method_id");

            entity.HasIndex(e => e.UserId, "IX_orders_user_id");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(30)
                .HasDefaultValue("pending")
                .HasColumnName("order_status");
            entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PaymentMethodId)
                .HasConstraintName("FK__orders__payment___2DE6D218");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__orders__user_id__084B3915");
        });

        modelBuilder.Entity<OrderAddress>(entity =>
        {
            entity.HasKey(e => e.OrderAddressId).HasName("PK__tmp_ms_x__9A9DCB57081B27D7");

            entity.ToTable("order_addresses");

            entity.HasIndex(e => e.OrderId, "IX_order_addresses_order_id");

            entity.Property(e => e.OrderAddressId).HasColumnName("order_address_id");
            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Phone)
                .HasMaxLength(25)
                .HasColumnName("phone");
            entity.Property(e => e.RecipientName)
                .HasMaxLength(100)
                .HasColumnName("recipient_name");

            entity.HasOne(d => d.Address).WithMany(p => p.OrderAddresses)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__order_add__addre__7E8CC4B1");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderAddresses)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__order_add__order__1975C517");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__order_it__3764B6BC30F2AEFE");

            entity.ToTable("order_items");

            entity.HasIndex(e => e.OrderId, "IX_order_items_order_id");

            entity.HasIndex(e => e.VariantId, "IX_order_items_variant_id");

            entity.HasIndex(e => e.VariantSpecificationOptionsId, "IX_order_items_variant_spec_option_id");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.VariantSpecificationOptionsId).HasColumnName("variant_specification_options_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__order_ite__order__32AB8735");

            entity.HasOne(d => d.Variant).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__order_ite__varia__1A34DF26");

            entity.HasOne(d => d.VariantSpecificationOptions).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.VariantSpecificationOptionsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__order_ite__varia_spec_opt");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.ResetId).HasName("PK__password__40FB0520C78E9B55");

            entity.ToTable("password_reset_tokens");

            entity.HasIndex(e => e.UserId, "IX_password_reset_tokens_user_id");

            entity.Property(e => e.ResetId).HasColumnName("reset_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");
            entity.Property(e => e.ResetToken)
                .HasMaxLength(200)
                .HasColumnName("reset_token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__password___user___047AA831");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__payments__ED1FC9EAA85823D7");

            entity.ToTable("payments");

            entity.HasIndex(e => e.OrderId, "IX_payments_order_id");

            entity.HasIndex(e => e.PaymentMethodId, "IX_payments_payment_method_id");

            entity.HasIndex(e => e.UserId, "IX_payments_user_id");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.TransactionReference)
                .HasMaxLength(200)
                .HasColumnName("transaction_reference");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__payments__order___2057CCD0");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__payments__paymen__22401542");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__payments__user_i__056ECC6A");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("PK__payment___8A3EA9EB26DFA867");

            entity.ToTable("payment_methods");

            entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.MethodName)
                .HasMaxLength(50)
                .HasColumnName("method_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PK__permissi__E5331AFA1DB14039");

            entity.ToTable("permissions");

            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.PermissionName)
                .HasMaxLength(100)
                .HasColumnName("permission_name");
            entity.Property(e => e.PermissionPath)
                .HasMaxLength(200)
                .HasColumnName("permission_path");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__products__47027DF549946710");

            entity.ToTable("products");

            entity.HasIndex(e => e.BrandId, "IX_products_brand_id");

            entity.HasIndex(e => e.CategoryId, "IX_products_category_id");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ProductName)
                .HasMaxLength(200)
                .HasColumnName("product_name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.WarrantyMonths).HasColumnName("warranty_months");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__products__brand___778AC167");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__products__catego__76969D2E");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__product___DC9AC9555EFFCAB4");

            entity.ToTable("product_images");

            entity.HasIndex(e => e.ProductId, "IX_product_images_product_id");

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.IsCover)
                .HasDefaultValue(false)
                .HasColumnName("is_cover");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__product_i__produ__7C4F7684");
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__product___60883D90EEC32F2F");

            entity.ToTable("product_reviews");

            entity.HasIndex(e => e.ProductId, "IX_product_reviews_product_id");

            entity.HasIndex(e => e.UserId, "IX_product_reviews_user_id");

            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ReviewText)
                .HasMaxLength(2000)
                .HasColumnName("review_text");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__product_r__produ__3D2915A8");

            entity.HasOne(d => d.User).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__product_r__user___093F5D4E");
        });

        modelBuilder.Entity<ProductSpecificationValue>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.SpecificationId }).HasName("PK__product___C1DE3736383223F6");

            entity.ToTable("product_specification_values");

            entity.HasIndex(e => e.OptionId, "IX_product_specification_values_option_id");

            entity.HasIndex(e => e.SpecificationId, "IX_product_specification_values_specification_id");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.SpecificationId).HasColumnName("specification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.OptionId).HasColumnName("option_id");
            entity.Property(e => e.ValueBool).HasColumnName("value_bool");
            entity.Property(e => e.ValueNumber)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("value_number");
            entity.Property(e => e.ValueText)
                .HasMaxLength(4000)
                .HasColumnName("value_text");

            entity.HasOne(d => d.Option).WithMany(p => p.ProductSpecificationValues)
                .HasForeignKey(d => d.OptionId)
                .HasConstraintName("FK__product_s__optio__09A971A2");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSpecificationValues)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__product_s__produ__07C12930");

            entity.HasOne(d => d.Specification).WithMany(p => p.ProductSpecificationValues)
                .HasForeignKey(d => d.SpecificationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__product_s__speci__08B54D69");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.VariantId).HasName("PK__tmp_ms_x__EACC68B7FFF3CDF1");

            entity.ToTable("product_variants");

            entity.HasIndex(e => e.ProductId, "IX_product_variants_product_id");

            entity.HasIndex(e => e.Sku, "UQ__tmp_ms_x__DDDF4BE71FE219C8")
                .IsUnique()
                .HasFilter("([sku] IS NOT NULL)");

            entity.HasIndex(e => e.Sku, "UQ__tmp_ms_x__DDDF4BE7F114AB11")
                .IsUnique()
                .HasFilter("([sku] IS NOT NULL)");

            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.DiscountEnd).HasColumnName("discount_end");
            entity.Property(e => e.DiscountPercentage)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discount_percentage");
            entity.Property(e => e.DiscountStart).HasColumnName("discount_start");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Sku)
                .HasMaxLength(80)
                .HasColumnName("sku");
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__product_v__produ__16644E42");
        });

        modelBuilder.Entity<ProductView>(entity =>
        {
            entity.HasKey(e => e.ViewId).HasName("PK__product___B5A34EE238C8872E");

            entity.ToTable("product_views");

            entity.HasIndex(e => e.ProductId, "IX_product_views_product_id");

            entity.HasIndex(e => e.UserId, "IX_product_views_user_id");

            entity.Property(e => e.ViewId).HasColumnName("view_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ViewedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("viewed_at");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductViews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__product_v__produ__5C37ACAD");

            entity.HasOne(d => d.User).WithMany(p => p.ProductViews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__product_v__user___5B438874");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId).HasName("PK__RefreshT__F5845E395EF5989C");

            entity.HasIndex(e => e.UserId, "IX_RefreshTokens_user_id");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Token).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.HasKey(e => e.RefundId).HasName("PK__refunds__897E9EA3DA435AAD");

            entity.ToTable("refunds");

            entity.HasIndex(e => e.PaymentId, "IX_refunds_payment_id");

            entity.HasIndex(e => e.ReturnId, "IX_refunds_return_id");

            entity.Property(e => e.RefundId).HasColumnName("refund_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.RefundedAt).HasColumnName("refunded_at");
            entity.Property(e => e.ReturnId).HasColumnName("return_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");

            entity.HasOne(d => d.Payment).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__refunds__payment__2EA5EC27");

            entity.HasOne(d => d.Return).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.ReturnId)
                .HasConstraintName("FK__refunds__return___2F9A1060");
        });

        modelBuilder.Entity<Return>(entity =>
        {
            entity.HasKey(e => e.ReturnId).HasName("PK__returns__35C234730CD511CF");

            entity.ToTable("returns");

            entity.HasIndex(e => e.OrderId, "IX_returns_order_id");

            entity.HasIndex(e => e.UserId, "IX_returns_user_id");

            entity.Property(e => e.ReturnId).HasColumnName("return_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("requested")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Returns)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__returns__order_i__0E391C95");

            entity.HasOne(d => d.User).WithMany(p => p.Returns)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__returns__user_id__00AA174D");
        });

        modelBuilder.Entity<ReviewImage>(entity =>
        {
            entity.HasIndex(e => e.ReviewId, "IX_ReviewImages_ReviewId");

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewImages)
                .HasForeignKey(d => d.ReviewId)
                .HasConstraintName("FK_review_images_product_reviews");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__roles__760965CCEB9FC4C0");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "UQ__roles__783254B17D3DCD9A").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId }).HasName("PK__role_per__C85A54639C7830C5");

            entity.ToTable("role_permissions");

            entity.HasIndex(e => e.PermissionId, "IX_role_permissions_permission_id");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__role_perm__permi__534D60F1");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__role_perm__role___52593CB8");
        });

        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.HasKey(e => e.SearchId).HasName("PK__search_h__B302268D82080159");

            entity.ToTable("search_history");

            entity.HasIndex(e => e.UserId, "IX_search_history_user_id");

            entity.Property(e => e.SearchId).HasColumnName("search_id");
            entity.Property(e => e.SearchText)
                .HasMaxLength(255)
                .HasColumnName("search_text");
            entity.Property(e => e.SearchedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("searched_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.SearchHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__search_hi__user___019E3B86");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.ShipmentId).HasName("PK__tmp_ms_x__41466E59A956F909");

            entity.ToTable("shipments");

            entity.HasIndex(e => e.OrderId, "IX_shipments_order_id");

            entity.Property(e => e.ShipmentId).HasColumnName("shipment_id");
            entity.Property(e => e.CourierName)
                .HasMaxLength(100)
                .HasColumnName("courier_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
            entity.Property(e => e.ExpectedDeliveryDate).HasColumnName("expected_delivery_date");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ShippedAt).HasColumnName("shipped_at");
            entity.Property(e => e.ShippingCost)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("shipping_cost");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .HasColumnName("tracking_number");

            entity.HasOne(d => d.Order).WithMany(p => p.Shipments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__shipments__order__3429BB53");
        });

        modelBuilder.Entity<SpecificationDefinition>(entity =>
        {
            entity.HasKey(e => e.SpecificationId).HasName("PK__specific__6DC4AC39C948F13C");

            entity.ToTable("specification_definitions");

            entity.Property(e => e.SpecificationId).HasColumnName("specification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DataType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("data_type");
            entity.Property(e => e.IsVariant)
                .HasDefaultValue(false)
                .HasColumnName("is_variant");
            entity.Property(e => e.SpecificationName)
                .HasMaxLength(120)
                .HasColumnName("specification_name");
        });

        modelBuilder.Entity<SpecificationOption>(entity =>
        {
            entity.HasKey(e => e.OptionId).HasName("PK__specific__F4EACE1BD89246C1");

            entity.ToTable("specification_options");

            entity.HasIndex(e => e.SpecificationId, "IX_specification_options_specification_id");

            entity.Property(e => e.OptionId).HasColumnName("option_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.OptionValue)
                .HasMaxLength(120)
                .HasColumnName("option_value");
            entity.Property(e => e.SpecificationId).HasColumnName("specification_id");

            entity.HasOne(d => d.Specification).WithMany(p => p.SpecificationOptions)
                .HasForeignKey(d => d.SpecificationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__specifica__speci__03F0984C");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__tmp_ms_x__B9BE370FC419F2E1");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UQ__tmp_ms_x__AB6E61643A120AD1").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__tmp_ms_x__F3DBC5721B0B15DA").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(80)
                .HasColumnName("first_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsEmailVerified).HasDefaultValue(false);
            entity.Property(e => e.IsPhoneVerified).HasDefaultValue(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(80)
                .HasColumnName("last_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(25)
                .HasColumnName("phone_number");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(80)
                .HasColumnName("username");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__user_pro__AEBB701FBDBEDC89");

            entity.ToTable("user_profiles");

            entity.HasIndex(e => e.UserId, "UQ__user_pro__B9BE370E2BB74815").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.Bio)
                .HasMaxLength(500)
                .HasColumnName("bio");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ProfileImage)
                .HasMaxLength(500)
                .HasColumnName("profile_image");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_prof__user___0B27A5C0");
        });

        modelBuilder.Entity<UserRecentOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__user_rec__3213E83F3FDE9F9C");

            entity.ToTable("user_recent_orders");

            entity.HasIndex(e => e.OrderId, "IX_user_recent_orders_order_id");

            entity.HasIndex(e => e.UserId, "IX_user_recent_orders_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.UserRecentOrders)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_rece__order__17C286CF");

            entity.HasOne(d => d.User).WithMany(p => p.UserRecentOrders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_rece__user___02925FBF");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId }).HasName("PK__user_rol__6EDEA153F0FFE70B");

            entity.ToTable("user_roles");

            entity.HasIndex(e => e.RoleId, "IX_user_roles_role_id");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_role__role___5DCAEF64");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_role__user___0A338187");
        });

        modelBuilder.Entity<UserVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("PK__user_ver__24F17969C715D1FD");

            entity.ToTable("user_verifications");

            entity.HasIndex(e => e.UserId, "IX_user_verifications_user_id");

            entity.Property(e => e.VerificationId).HasColumnName("verification_id");
            entity.Property(e => e.Channel)
                .HasMaxLength(20)
                .HasColumnName("channel");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserVerifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_veri__user___0C1BC9F9");
        });

        modelBuilder.Entity<VariantPriceHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__variant___3213E83F31086D80");

            entity.ToTable("variant_price_history");

            entity.HasIndex(e => e.VariantId, "IX_variant_price_history_variant_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.NewPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("new_price");
            entity.Property(e => e.OldPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("old_price");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.Variant).WithMany(p => p.VariantPriceHistories)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__variant_p__varia__1B29035F");
        });

        modelBuilder.Entity<VariantSpecificationOption>(entity =>
        {
            entity.HasKey(e => new { e.VariantId, e.OptionId }).HasName("PK__variant___4582C456ACC40D3B");

            entity.ToTable("variant_specification_options");

            entity.HasIndex(e => e.OptionId, "IX_variant_specification_options_option_id");

            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.OptionId).HasColumnName("option_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Option).WithMany(p => p.VariantSpecificationOptions)
                .HasForeignKey(d => d.OptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__variant_s__optio__1332DBDC");

            entity.HasOne(d => d.Variant).WithMany(p => p.VariantSpecificationOptions)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__variant_s__varia__1758727B");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("PK__wishlist__6151514EEABA9E73");

            entity.ToTable("wishlists");

            entity.HasIndex(e => e.UserId, "UQ__wishlist__B9BE370E7AA893E5")
                .IsUnique()
                .HasFilter("([user_id] IS NOT NULL)");

            entity.Property(e => e.WishlistId).HasColumnName("wishlist_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Wishlist)
                .HasForeignKey<Wishlist>(d => d.UserId)
                .HasConstraintName("FK__wishlists__user___0662F0A3");
        });

        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(e => e.WishlistItemId).HasName("PK__wishlist__190EBE283CA47503");

            entity.ToTable("wishlist_items");

            entity.HasIndex(e => e.VariantId, "IX_wishlist_items_variant_id");

            entity.HasIndex(e => e.WishlistId, "IX_wishlist_items_wishlist_id");

            entity.Property(e => e.WishlistItemId).HasColumnName("wishlist_item_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.WishlistId).HasColumnName("wishlist_id");

            entity.HasOne(d => d.Variant).WithMany(p => p.WishlistItems)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__wishlist___varia__1940BAED");

            entity.HasOne(d => d.Wishlist).WithMany(p => p.WishlistItems)
                .HasForeignKey(d => d.WishlistId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__wishlist___wishl__25518C17");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
