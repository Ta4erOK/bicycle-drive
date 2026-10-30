/* =====================================================================
   ООО «ВелосипедДрайв» — схема базы данных (3 нормальная форма)
   СУБД: Microsoft SQL Server
   Скрипт создаёт базу данных BicycleDrive и все объекты схемы.
   ===================================================================== */

IF DB_ID(N'BicycleDrive') IS NOT NULL
BEGIN
    ALTER DATABASE [BicycleDrive] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [BicycleDrive];
END
GO

CREATE DATABASE [BicycleDrive];
GO

USE [BicycleDrive];
GO

/* ------------------------- Справочники ------------------------------ */

CREATE TABLE dbo.Roles (
    Id   int           IDENTITY(1,1) NOT NULL,
    Name nvarchar(100) NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (Id),
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);

CREATE TABLE dbo.Units (
    Id   int          IDENTITY(1,1) NOT NULL,
    Name nvarchar(20) NOT NULL,
    CONSTRAINT PK_Units PRIMARY KEY (Id),
    CONSTRAINT UQ_Units_Name UNIQUE (Name)
);

CREATE TABLE dbo.Categories (
    Id   int           IDENTITY(1,1) NOT NULL,
    Name nvarchar(150) NOT NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (Id),
    CONSTRAINT UQ_Categories_Name UNIQUE (Name)
);

CREATE TABLE dbo.Manufacturers (
    Id   int           IDENTITY(1,1) NOT NULL,
    Name nvarchar(150) NOT NULL,
    CONSTRAINT PK_Manufacturers PRIMARY KEY (Id),
    CONSTRAINT UQ_Manufacturers_Name UNIQUE (Name)
);

CREATE TABLE dbo.Suppliers (
    Id   int           IDENTITY(1,1) NOT NULL,
    Name nvarchar(150) NOT NULL,
    CONSTRAINT PK_Suppliers PRIMARY KEY (Id),
    CONSTRAINT UQ_Suppliers_Name UNIQUE (Name)
);

CREATE TABLE dbo.Statuses (
    Id   int          IDENTITY(1,1) NOT NULL,
    Name nvarchar(50) NOT NULL,
    CONSTRAINT PK_Statuses PRIMARY KEY (Id),
    CONSTRAINT UQ_Statuses_Name UNIQUE (Name)
);

CREATE TABLE dbo.PickupPoints (
    Id      int           IDENTITY(1,1) NOT NULL,
    Address nvarchar(300) NOT NULL,
    CONSTRAINT PK_PickupPoints PRIMARY KEY (Id),
    CONSTRAINT UQ_PickupPoints_Address UNIQUE (Address)
);

/* -------------------------- Пользователи ---------------------------- */

CREATE TABLE dbo.Users (
    Id       int           IDENTITY(1,1) NOT NULL,
    RoleId   int           NOT NULL,
    FullName nvarchar(150) NOT NULL,
    Login    nvarchar(100) NOT NULL,
    Password nvarchar(100) NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Login UNIQUE (Login),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id)
);

/* ----------------------------- Товары ------------------------------- */

CREATE TABLE dbo.Products (
    Id             int            IDENTITY(1,1) NOT NULL,
    Article        nvarchar(50)   NOT NULL,
    Name           nvarchar(300)  NOT NULL,
    Description    nvarchar(max)  NULL,
    CategoryId     int            NOT NULL,
    ManufacturerId int            NOT NULL,
    SupplierId     int            NOT NULL,
    UnitId         int            NOT NULL,
    Price          decimal(10,2)  NOT NULL,
    Quantity       int            NOT NULL,
    Discount       decimal(5,2)   NOT NULL CONSTRAINT DF_Products_Discount DEFAULT (0),
    ImagePath      nvarchar(300)  NULL,
    CONSTRAINT PK_Products PRIMARY KEY (Id),
    CONSTRAINT UQ_Products_Article UNIQUE (Article),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id),
    CONSTRAINT FK_Products_Manufacturers FOREIGN KEY (ManufacturerId) REFERENCES dbo.Manufacturers (Id),
    CONSTRAINT FK_Products_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (Id),
    CONSTRAINT FK_Products_Units FOREIGN KEY (UnitId) REFERENCES dbo.Units (Id),
    CONSTRAINT CK_Products_Price CHECK (Price >= 0),
    CONSTRAINT CK_Products_Quantity CHECK (Quantity >= 0),
    CONSTRAINT CK_Products_Discount CHECK (Discount >= 0 AND Discount < 100)
);

/* ----------------------------- Заказы ------------------------------- */

CREATE TABLE dbo.Orders (
    Id            int  IDENTITY(1,1) NOT NULL,
    Number        int  NOT NULL,
    UserId        int  NOT NULL,
    PickupPointId int  NOT NULL,
    StatusId      int  NOT NULL,
    OrderDate     date NOT NULL,
    DeliveryDate  date NULL,
    Code          int  NOT NULL,
    CONSTRAINT PK_Orders PRIMARY KEY (Id),
    CONSTRAINT UQ_Orders_Number UNIQUE (Number),
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id),
    CONSTRAINT FK_Orders_PickupPoints FOREIGN KEY (PickupPointId) REFERENCES dbo.PickupPoints (Id),
    CONSTRAINT FK_Orders_Statuses FOREIGN KEY (StatusId) REFERENCES dbo.Statuses (Id)
);

CREATE TABLE dbo.OrderItems (
    Id        int IDENTITY(1,1) NOT NULL,
    OrderId   int NOT NULL,
    ProductId int NOT NULL,
    Quantity  int NOT NULL,
    CONSTRAINT PK_OrderItems PRIMARY KEY (Id),
    CONSTRAINT UQ_OrderItems_Order_Product UNIQUE (OrderId, ProductId),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id),
    CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0)
);
GO

/* ---------------------------- Индексы ------------------------------- */

CREATE INDEX IX_Products_CategoryId ON dbo.Products (CategoryId);
CREATE INDEX IX_Products_ManufacturerId ON dbo.Products (ManufacturerId);
CREATE INDEX IX_Products_SupplierId ON dbo.Products (SupplierId);
CREATE INDEX IX_Orders_UserId ON dbo.Orders (UserId);
CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems (OrderId);
CREATE INDEX IX_OrderItems_ProductId ON dbo.OrderItems (ProductId);
GO
