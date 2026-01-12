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
    password_hash VARBINARY(256) NOT NULL, -- Added by assistant
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
    status NVARCHAR(20), -- sent, failed
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
    order_status NVARCHAR(30),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (payment_method_id) REFERENCES payment_methods(payment_method_id)
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
