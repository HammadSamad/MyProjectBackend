/* =========================================================
   E-COMMERCE DATABASE – SQL SERVER
   Products: Laptops,Accessories
   ========================================================= */

------------------------------------------------------------
-- Database
------------------------------------------------------------
Create Database laptopHarbour_Db;
Use laptopHarbour_Db;
------------------------------------------------------------
-- USERS / ROLES / PERMISSIONS
------------------------------------------------------------

CREATE TABLE roles (
    role_id INT IDENTITY PRIMARY KEY,
    role_name NVARCHAR(50) UNIQUE NOT NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL
);
GO

CREATE TABLE permissions (
    permission_id INT IDENTITY PRIMARY KEY,
    permission_name NVARCHAR(100) NOT NULL,
    permission_path NVARCHAR(200),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL
);
GO

CREATE TABLE role_permissions (
    role_id INT NOT NULL,
    permission_id INT NOT NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (role_id, permission_id),
    FOREIGN KEY (role_id) REFERENCES roles(role_id),
    FOREIGN KEY (permission_id) REFERENCES permissions(permission_id)
);
GO

CREATE TABLE users (
    user_id INT IDENTITY PRIMARY KEY,
    first_name NVARCHAR(80),
    last_name NVARCHAR(80),
    username NVARCHAR(80) UNIQUE NOT NULL,
    email NVARCHAR(255) UNIQUE NOT NULL,
    phone_number NVARCHAR(25),
    password_hash NVARCHAR(500) NOT NULL,
    IsEmailVerified BIT DEFAULT 0,
    IsPhoneVerified BIT DEFAULT 0,
    is_active BIT DEFAULT 1,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL
);
GO

CREATE TABLE user_roles (
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (role_id) REFERENCES roles(role_id)
);
GO

CREATE TABLE user_profiles (
    profile_id INT IDENTITY PRIMARY KEY,
    user_id INT NOT NULL UNIQUE,          -- One profile per user
    profile_image NVARCHAR(500),          -- Store image URL or path
    bio NVARCHAR(500) NULL,               -- short bio
    updated_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
GO


------------------------------------------------------------
-- OTP / Verification 
------------------------------------------------------------

CREATE TABLE user_verifications (
    verification_id BIGINT IDENTITY PRIMARY KEY,
    user_id INT NOT NULL,
    channel NVARCHAR(20), -- email, whatsapp
    code NVARCHAR(10),
    expires_at DATETIME2,
    is_used BIT DEFAULT 0,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
GO

------------------------------------------------------------
-- User / Notification 
------------------------------------------------------------

CREATE TABLE notifications (
    notification_id BIGINT IDENTITY PRIMARY KEY,
    user_id INT NULL, -- NULL = broadcast
    title NVARCHAR(200),
    message NVARCHAR(1000),
    type NVARCHAR(30), -- offer, order, system
    target_audience NVARCHAR(20) DEFAULT 'user', -- user, admin, both
    is_read BIT DEFAULT 0,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
GO

------------------------------------------------------------
-- Email / WhatsApp Log 
------------------------------------------------------------

CREATE TABLE message_logs (
    message_id BIGINT IDENTITY PRIMARY KEY,
    user_id INT,
    channel NVARCHAR(20), -- email, whatsapp
    recipient NVARCHAR(200),
    message NVARCHAR(MAX),
    status NVARCHAR(20) CHECK (status IN ('pending','sent','failed')), -- sent, failed
    created_at DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

------------------------------------------------------------
-- LOCATION / ADDRESSES
------------------------------------------------------------

CREATE TABLE countries (
    country_id INT IDENTITY PRIMARY KEY,
    country_name NVARCHAR(100) UNIQUE,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE cities (
    city_id INT IDENTITY PRIMARY KEY,
    country_id INT NOT NULL,
    city_name NVARCHAR(100),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (country_id) REFERENCES countries(country_id)
);
GO

CREATE TABLE addresses (
    address_id INT IDENTITY PRIMARY KEY,
    user_id INT NOT NULL,
    city_id INT NOT NULL,
    address_line1 NVARCHAR(200),
    address_line2 NVARCHAR(200),
    postal_code NVARCHAR(20),
    IsDefault BIT DEFAULT 0,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (city_id) REFERENCES cities(city_id)
);
GO

------------------------------------------------------------
-- CATEGORY / BRAND
------------------------------------------------------------

CREATE TABLE categories (
    category_id INT IDENTITY PRIMARY KEY,
    parent_category_id INT NULL,
    category_name NVARCHAR(120),
    category_image NVARCHAR(500),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    FOREIGN KEY (parent_category_id) REFERENCES categories(category_id)
);
GO

CREATE TABLE brands (
    brand_id INT IDENTITY PRIMARY KEY,
    brand_name NVARCHAR(120) UNIQUE,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL
);
GO

------------------------------------------------------------
-- PRODUCTS
------------------------------------------------------------

CREATE TABLE products (
    product_id INT IDENTITY PRIMARY KEY,
    category_id INT NOT NULL,
    brand_id INT NOT NULL,
    product_name NVARCHAR(200),
    description NVARCHAR(MAX),
    warranty_months INT,
    is_active BIT DEFAULT 1,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    FOREIGN KEY (category_id) REFERENCES categories(category_id),
    FOREIGN KEY (brand_id) REFERENCES brands(brand_id)
);
GO

CREATE TABLE product_images (
    image_id INT IDENTITY PRIMARY KEY,
    product_id INT NOT NULL,
    image_url NVARCHAR(500),
    is_cover BIT DEFAULT 0,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (product_id) REFERENCES products(product_id)
);
GO

------------------------------------------------------------
-- SPECIFICATIONS
------------------------------------------------------------

CREATE TABLE specification_definitions (
    specification_id INT IDENTITY PRIMARY KEY,
    specification_name NVARCHAR(120),
    data_type VARCHAR(20), -- text, number, bool, date, option
    is_variant BIT DEFAULT 0,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE specification_options (
    option_id INT IDENTITY PRIMARY KEY,
    specification_id INT NOT NULL,
    option_value NVARCHAR(120),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (specification_id) REFERENCES specification_definitions(specification_id)
);
GO

CREATE TABLE product_specification_values (
    product_id INT NOT NULL,
    specification_id INT NOT NULL,
    value_text NVARCHAR(4000),
    value_number DECIMAL(18,2),
    value_bool BIT,
    option_id INT NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (product_id, specification_id),
    FOREIGN KEY (product_id) REFERENCES products(product_id),
    FOREIGN KEY (specification_id) REFERENCES specification_definitions(specification_id),
    FOREIGN KEY (option_id) REFERENCES specification_options(option_id)
);
GO

------------------------------------------------------------
-- PRODUCT VARIANTS
------------------------------------------------------------

CREATE TABLE product_variants (
    variant_id INT IDENTITY PRIMARY KEY,
    product_id INT NOT NULL,
    sku NVARCHAR(80) UNIQUE,
    price DECIMAL(18,2),
    stock INT,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    FOREIGN KEY (product_id) REFERENCES products(product_id)
);
GO

CREATE TABLE variant_specification_options (
    variant_id INT NOT NULL,
    option_id INT NOT NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (variant_id, option_id),
    FOREIGN KEY (variant_id) REFERENCES product_variants(variant_id),
    FOREIGN KEY (option_id) REFERENCES specification_options(option_id)
);
GO

------------------------------------------------------------
-- CART & WISHLIST
------------------------------------------------------------

CREATE TABLE carts (
    cart_id INT IDENTITY PRIMARY KEY,
    user_id INT NOT NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
GO

CREATE TABLE cart_items (
    cart_item_id INT IDENTITY PRIMARY KEY,
    cart_id INT NOT NULL,
    variant_id INT NOT NULL,
    quantity INT CHECK (quantity > 0),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (cart_id) REFERENCES carts(cart_id),
    FOREIGN KEY (variant_id) REFERENCES product_variants(variant_id)
);
GO

CREATE TABLE wishlists (
    wishlist_id INT IDENTITY PRIMARY KEY,
    user_id INT UNIQUE,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
GO

CREATE TABLE wishlist_items (
    wishlist_item_id INT IDENTITY PRIMARY KEY,
    wishlist_id INT NOT NULL,
    variant_id INT NOT NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (wishlist_id) REFERENCES wishlists(wishlist_id),
    FOREIGN KEY (variant_id) REFERENCES product_variants(variant_id)
);
GO

------------------------------------------------------------
-- ORDERS / PAYMENTS
------------------------------------------------------------

CREATE TABLE payment_methods (
    payment_method_id INT IDENTITY PRIMARY KEY,
    method_name NVARCHAR(50),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE orders (
    order_id BIGINT IDENTITY PRIMARY KEY,
    user_id INT NOT NULL,
    total_amount DECIMAL(18,2),
    payment_method_id INT,
    order_status NVARCHAR(30) DEFAULT ('pending') NOT NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (payment_method_id) REFERENCES payment_methods(payment_method_id),
    CONSTRAINT [chk_order_status] CHECK ([order_status]='cancelled' OR [order_status]='delivered' OR [order_status]='shipped' OR [order_status]='pending')
);
GO

CREATE TABLE order_items (
    order_item_id BIGINT IDENTITY PRIMARY KEY,
    order_id BIGINT NOT NULL,
    variant_id INT NOT NULL,
    quantity INT CHECK (quantity > 0),
    price DECIMAL(18,2),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (order_id) REFERENCES orders(order_id),
    FOREIGN KEY (variant_id) REFERENCES product_variants(variant_id)
);
GO

CREATE TABLE order_addresses (
    order_address_id BIGINT IDENTITY PRIMARY KEY,
    order_id BIGINT NOT NULL,
    full_address NVARCHAR(500),
    phone NVARCHAR(25),
    recipient_name NVARCHAR(100),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (order_id) REFERENCES orders(order_id)
);
GO

------------------------------------------------------------
-- REVIEWS / PRICE HISTORY
------------------------------------------------------------

CREATE TABLE product_reviews (
    review_id INT IDENTITY PRIMARY KEY,
    user_id INT NOT NULL,
    product_id INT NOT NULL,
    rating INT CHECK (rating BETWEEN 1 AND 5),
    review_text NVARCHAR(2000),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (product_id) REFERENCES products(product_id)
);
GO

CREATE TABLE variant_price_history (
    id BIGINT IDENTITY PRIMARY KEY,
    variant_id INT NOT NULL,
    old_price DECIMAL(18,2),
    new_price DECIMAL(18,2),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (variant_id) REFERENCES product_variants(variant_id)
);
GO

------------------------------------------------------------
-- SHIPPING
------------------------------------------------------------
CREATE TABLE shipments (
    shipment_id BIGINT IDENTITY PRIMARY KEY,
    order_id BIGINT NOT NULL,
    tracking_number NVARCHAR(100),
    courier_name NVARCHAR(100),
    shipping_cost DECIMAL(18,2) NOT NULL DEFAULT 0,
    status NVARCHAR(30), -- pending, shipped, delivered
    expected_delivery_date DATETIME2 (7)  NULL,
    shipped_at DATETIME2 NULL,
    delivered_at DATETIME2 NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (order_id) REFERENCES orders(order_id)
);
GO

------------------------------------------------------------
-- RETURN PRODUCT
------------------------------------------------------------
CREATE TABLE returns (
    return_id BIGINT IDENTITY PRIMARY KEY,
    order_id BIGINT NOT NULL,
    user_id INT NOT NULL,
    reason NVARCHAR(500),
    status NVARCHAR(30) DEFAULT ('requested') NOT NULL, -- requested, approved, rejected, refunded
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (order_id) REFERENCES orders(order_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT [chk_return_status] CHECK ([status]='refunded' OR [status]='rejected' OR [status]='approved' OR [status]='requested')
);
GO

------------------------------------------------------------
-- SEARCH HISTORY
------------------------------------------------------------
CREATE TABLE search_history (
    search_id BIGINT IDENTITY PRIMARY KEY,
    user_id INT NULL, -- NULL = guest user
    search_text NVARCHAR(255),
    searched_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
GO

------------------------------------------------------------
-- USER RECENT ORDERS
------------------------------------------------------------
CREATE TABLE user_recent_orders (
    id BIGINT IDENTITY PRIMARY KEY,
    user_id INT NOT NULL,
    order_id BIGINT NOT NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (order_id) REFERENCES orders(order_id)
);
GO

------------------------------------------------------------
-- PASSWORD RESET TOKENS
------------------------------------------------------------
CREATE TABLE password_reset_tokens (
    reset_id BIGINT IDENTITY PRIMARY KEY,
    user_id INT NOT NULL,
    reset_token NVARCHAR(200) NOT NULL,
    expires_at DATETIME2 NOT NULL,
    is_used BIT DEFAULT 0,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
GO


CREATE TABLE RefreshTokens (
    RefreshTokenId INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    Token NVARCHAR(500) NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_RefreshTokens_Users 
        FOREIGN KEY (user_id) REFERENCES Users(user_id)
);
GO


------------------------------------------------------------
-- PAYMENTS
------------------------------------------------------------
CREATE TABLE payments (
    payment_id BIGINT IDENTITY PRIMARY KEY,
    order_id BIGINT NOT NULL,
    user_id INT NOT NULL,
    payment_method_id INT NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    status NVARCHAR(30) DEFAULT ('pending') NOT NULL, -- pending, success, failed, refunded
    transaction_reference NVARCHAR(200), -- from Stripe, PayPal, JazzCash, etc
    paid_at DATETIME2 NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (order_id) REFERENCES orders(order_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (payment_method_id) REFERENCES payment_methods(payment_method_id),
    CONSTRAINT [chk_payment_status] CHECK ([status]='refunded' OR [status]='failed' OR [status]='success' OR [status]='pending')
);
GO

CREATE TABLE complaints (
    complaint_id BIGINT IDENTITY PRIMARY KEY,
    user_id INT NOT NULL,
    order_id BIGINT NULL,
    upload_image NVARCHAR(500),
    subject NVARCHAR(200),
    description NVARCHAR(2000),
    status NVARCHAR(30) DEFAULT ('open'),       -- open, in_progress, resolved, closed
    priority NVARCHAR(20),     -- low, medium, high
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (order_id) REFERENCES orders(order_id),
    CONSTRAINT [chk_complaint_status] CHECK ([status]='closed' OR [status]='resolved' OR [status]='in_progress' OR [status]='open')
);
GO

CREATE TABLE complaint_messages (
    message_id BIGINT IDENTITY PRIMARY KEY,
    complaint_id BIGINT NOT NULL,
    sender_type NVARCHAR(20), -- user, admin
    upload_image NVARCHAR(500),
    message NVARCHAR(2000),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (complaint_id) REFERENCES complaints(complaint_id)
);
GO

------------------------------------------------------------
-- REFUNDS
------------------------------------------------------------
CREATE TABLE refunds (
    refund_id BIGINT IDENTITY PRIMARY KEY,
    payment_id BIGINT NOT NULL,
    return_id BIGINT NULL,
    amount DECIMAL(18,2) NOT NULL,
    reason NVARCHAR(500),
    status NVARCHAR(30), -- pending, processed, failed
    refunded_at DATETIME2 NULL,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (payment_id) REFERENCES payments(payment_id),
    FOREIGN KEY (return_id) REFERENCES returns(return_id)
);
GO

CREATE TABLE product_views (
    view_id BIGINT IDENTITY PRIMARY KEY,
    user_id INT NULL,        -- NULL = guest
    product_id INT NOT NULL,
    viewed_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (product_id) REFERENCES products(product_id)
);
GO


------------------------------------------------------------
-- Triggers
------------------------------------------------------------

------------------------------------------------------------
--  Decrease stock after order
------------------------------------------------------------
IF OBJECT_ID('trg_DecreaseStock_OnOrder', 'TR') IS NOT NULL
    DROP TRIGGER trg_DecreaseStock_OnOrder;
GO

CREATE TRIGGER trg_DecreaseStock_OnOrder
ON order_items
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE pv
    SET pv.stock = pv.stock - i.quantity
    FROM product_variants pv
    INNER JOIN inserted i ON pv.variant_id = i.variant_id;
END
GO

------------------------------------------------------------
--  Increase stock after return
------------------------------------------------------------
IF OBJECT_ID('trg_IncreaseStock_OnReturn', 'TR') IS NOT NULL
    DROP TRIGGER trg_IncreaseStock_OnReturn;
GO

CREATE TRIGGER trg_IncreaseStock_OnReturn
ON returns
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Increase stock by quantity returned
    UPDATE pv
    SET pv.stock = pv.stock + oi.quantity
    FROM product_variants pv
    INNER JOIN order_items oi 
        ON pv.variant_id = oi.variant_id
    INNER JOIN inserted r 
        ON oi.order_id = r.order_id;
END
GO


------------------------------------------------------------
--  Log variant price change
------------------------------------------------------------
IF OBJECT_ID('trg_LogVariantPriceChange', 'TR') IS NOT NULL
    DROP TRIGGER trg_LogVariantPriceChange;
GO

CREATE TRIGGER trg_LogVariantPriceChange
ON product_variants
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO variant_price_history (variant_id, old_price, new_price)
    SELECT i.variant_id, d.price, i.price
    FROM inserted i
    INNER JOIN deleted d ON i.variant_id = d.variant_id
    WHERE i.price <> d.price;
END
GO

------------------------------------------------------------
--  Notify on order status change
------------------------------------------------------------
IF OBJECT_ID('trg_OrderStatusChange_Notify', 'TR') IS NOT NULL
    DROP TRIGGER trg_OrderStatusChange_Notify;
GO

CREATE TRIGGER trg_OrderStatusChange_Notify
ON orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO notifications(user_id, title, message, type)
    SELECT o.user_id,
           'Order Status Changed',
           'Your order #' + CAST(o.order_id AS NVARCHAR) + ' is now ' + o.order_status,
           'order'
    FROM inserted o
    INNER JOIN deleted d ON o.order_id = d.order_id
    WHERE o.order_status <> d.order_status;
END
GO

------------------------------------------------------------
--  Notify on refund processed
------------------------------------------------------------
IF OBJECT_ID('trg_RefundProcessed_Notify', 'TR') IS NOT NULL
    DROP TRIGGER trg_RefundProcessed_Notify;
GO

CREATE TRIGGER trg_RefundProcessed_Notify
ON refunds
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO notifications(user_id, title, message, type)
    SELECT p.user_id,
           'Refund Processed',
           'Your refund #' + CAST(r.refund_id AS NVARCHAR) + ' has been processed',
           'order'
    FROM inserted r
    INNER JOIN payments p ON r.payment_id = p.payment_id
    WHERE r.status = 'processed';
END
GO

------------------------------------------------------------
--  Notify on return status change
------------------------------------------------------------
IF OBJECT_ID('trg_ReturnStatus_Notify', 'TR') IS NOT NULL
    DROP TRIGGER trg_ReturnStatus_Notify;
GO

CREATE TRIGGER trg_ReturnStatus_Notify
ON returns
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO notifications(user_id, title, message, type)
    SELECT r.user_id,
           'Return Status Updated',
           'Your return request #' + CAST(r.return_id AS NVARCHAR) + ' is now ' + r.status,
           'order'
    FROM inserted r
    INNER JOIN deleted d ON r.return_id = d.return_id
    WHERE r.status <> d.status;
END
GO

------------------------------------------------------------
-- Notify on shipment status change
------------------------------------------------------------
IF OBJECT_ID('trg_ShipmentStatus_Notify', 'TR') IS NOT NULL
    DROP TRIGGER trg_ShipmentStatus_Notify;
GO

CREATE TRIGGER trg_ShipmentStatus_Notify
ON shipments
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO notifications(user_id, title, message, type)
    SELECT o.user_id,
           'Shipment Status Updated',
           'Your shipment #' + CAST(s.shipment_id AS NVARCHAR) + ' is now ' + s.status,
           'order'
    FROM inserted s
    INNER JOIN orders o ON s.order_id = o.order_id
    INNER JOIN deleted d ON s.shipment_id = d.shipment_id
    WHERE s.status <> d.status;
END
GO

------------------------------------------------------------
-- Notify on complaint status change
------------------------------------------------------------
IF OBJECT_ID('trg_ComplaintStatus_Notify', 'TR') IS NOT NULL
    DROP TRIGGER trg_ComplaintStatus_Notify;
GO

CREATE TRIGGER trg_ComplaintStatus_Notify
ON complaints
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO notifications(user_id, title, message, type)
    SELECT c.user_id,
           'Complaint Status Updated',
           'Your complaint #' + CAST(c.complaint_id AS NVARCHAR) + ' is now ' + c.status,
           'system'
    FROM inserted c
    INNER JOIN deleted d ON c.complaint_id = d.complaint_id
    WHERE c.status <> d.status;
END
GO

------------------------------------------------------------
-- Cleanup cart items after cart deletion
------------------------------------------------------------
IF OBJECT_ID('trg_CleanupCartItems', 'TR') IS NOT NULL
    DROP TRIGGER trg_CleanupCartItems;
GO

CREATE TRIGGER trg_CleanupCartItems
ON carts
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DELETE ci
    FROM cart_items ci
    INNER JOIN deleted d ON ci.cart_id = d.cart_id;
END
GO

------------------------------------------------------------
-- Cleanup wishlist items after wishlist deletion
------------------------------------------------------------
IF OBJECT_ID('trg_CleanupWishlistItems', 'TR') IS NOT NULL
    DROP TRIGGER trg_CleanupWishlistItems;
GO

CREATE TRIGGER trg_CleanupWishlistItems
ON wishlists
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DELETE wi
    FROM wishlist_items wi
    INNER JOIN deleted d ON wi.wishlist_id = d.wishlist_id;
END
GO

------------------------------------------------------------
-- Low stock notification
------------------------------------------------------------
IF OBJECT_ID('trg_LowStock_Notify', 'TR') IS NOT NULL
    DROP TRIGGER trg_LowStock_Notify;
GO

CREATE TRIGGER trg_LowStock_Notify
ON product_variants
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO notifications(user_id, title, message, type)
    SELECT NULL,
           'Low Stock Alert',
           'Variant #' + CAST(i.variant_id AS NVARCHAR) + ' stock is low: ' + CAST(i.stock AS NVARCHAR),
           'system'
    FROM inserted i
    INNER JOIN deleted d ON i.variant_id = d.variant_id
    WHERE i.stock < 5 AND i.stock <> d.stock;
END
GO

------------------------------------------------------------
-- Update updated_at automatically
-- Only for tables that have updated_at
------------------------------------------------------------

-- Users
IF OBJECT_ID('trg_UpdateUpdatedAt_Users', 'TR') IS NOT NULL
    DROP TRIGGER trg_UpdateUpdatedAt_Users;
GO

CREATE TRIGGER trg_UpdateUpdatedAt_Users
ON users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE users
    SET updated_at = SYSUTCDATETIME()
    FROM inserted i
    WHERE users.user_id = i.user_id;
END
GO

-- Orders
IF OBJECT_ID('trg_UpdateUpdatedAt_Orders', 'TR') IS NOT NULL
    DROP TRIGGER trg_UpdateUpdatedAt_Orders;
GO

CREATE TRIGGER trg_UpdateUpdatedAt_Orders
ON orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE orders
    SET updated_at = SYSUTCDATETIME()
    FROM inserted i
    WHERE orders.order_id = i.order_id;
END
GO

-- Product Variants
IF OBJECT_ID('trg_UpdateUpdatedAt_ProductVariants', 'TR') IS NOT NULL
    DROP TRIGGER trg_UpdateUpdatedAt_ProductVariants;
GO

CREATE TRIGGER trg_UpdateUpdatedAt_ProductVariants
ON product_variants
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE product_variants
    SET updated_at = SYSUTCDATETIME()
    FROM inserted i
    WHERE product_variants.variant_id = i.variant_id;
END
GO

-- Products
IF OBJECT_ID('trg_UpdateUpdatedAt_Products', 'TR') IS NOT NULL
    DROP TRIGGER trg_UpdateUpdatedAt_Products;
GO

CREATE TRIGGER trg_UpdateUpdatedAt_Products
ON products
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE products
    SET updated_at = SYSUTCDATETIME()
    FROM inserted i
    WHERE products.product_id = i.product_id;
END
GO

-- Complaints
IF OBJECT_ID('trg_UpdateUpdatedAt_Complaints', 'TR') IS NOT NULL
    DROP TRIGGER trg_UpdateUpdatedAt_Complaints;
GO

CREATE TRIGGER trg_UpdateUpdatedAt_Complaints
ON complaints
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE complaints
    SET updated_at = SYSUTCDATETIME()
    FROM inserted i
    WHERE complaints.complaint_id = i.complaint_id;
END
GO

IF OBJECT_ID('trg_UpdateUpdatedAt_UserProfiles', 'TR') IS NOT NULL
    DROP TRIGGER trg_UpdateUpdatedAt_UserProfiles;
GO

CREATE TRIGGER trg_UpdateUpdatedAt_UserProfiles
ON user_profiles
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE user_profiles
    SET updated_at = SYSUTCDATETIME()
    FROM inserted i
    WHERE user_profiles.profile_id = i.profile_id;
END
GO

IF OBJECT_ID('trg_AutoCancel_UnpaidOrders', 'TR') IS NOT NULL
    DROP TRIGGER trg_AutoCancel_UnpaidOrders;
GO

CREATE TRIGGER trg_AutoCancel_UnpaidOrders
ON orders
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE orders
    SET order_status = 'cancelled'
    WHERE order_status = 'pending'
      AND created_at <= DATEADD(MINUTE, -30, SYSUTCDATETIME());
END
GO

IF OBJECT_ID('trg_CleanupOldProductViews', 'TR') IS NOT NULL
    DROP TRIGGER trg_CleanupOldProductViews;
GO

CREATE TRIGGER trg_CleanupOldProductViews
ON product_views
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Delete views older than 7 days
    DELETE FROM product_views
    WHERE viewed_at <= DATEADD(DAY, -7, SYSUTCDATETIME());
END
GO
