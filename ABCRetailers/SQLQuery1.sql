-- Users table
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Surname NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    ShippingAddress NVARCHAR(500),
    Role NVARCHAR(20) NOT NULL DEFAULT 'Customer',
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Carts table
CREATE TABLE Carts (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ProductId NVARCHAR(100) NOT NULL,
    ProductName NVARCHAR(255) NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,
    AddedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

-- Helpful indexes
CREATE INDEX IX_Carts_UserId ON Carts(UserId);
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);

     SELECT Username, Role FROM Users;
     SELECT * FROM Carts;
     SELECT * FROM Orders;

        SELECT UserId, Username, Password, Role FROM Users;

        -- Users table (admin + customer)
INSERT INTO Users (Username, Password, Name, Surname, Email, ShippingAddress, Role, CreatedDate)
VALUES
('admin1', '3F01A9CF91CF67C6F92823C8AEF194E9152F27134156F8E45E915F74D49BEAF3', 'Lerato', 'Mokoena', 'admin@abc.com', '1 Admin Way, Sandton', 'Admin', GETDATE()),
('customer1', '2E5C28D8275C0B8FE72A593A9F0A40FD67AEB5CBA5B2A3640CC75C0CFF5B8070', 'Sipho', 'Nkosi', 'sipho@abc.com', '45 Main Rd, Midrand', 'Customer', GETDATE());

-- Cart items for the customer (UserId = 2)
INSERT INTO Carts (UserId, ProductId, ProductName, Quantity, UnitPrice, TotalPrice, AddedDate)
VALUES
(2, 'prod-1001', 'Designer Perfume', 2, 460.00, 920.00, GETDATE()),
(2, 'prod-1002', 'Leather Sneakers', 1, 850.00, 850.00, GETDATE());

/*UPDATE Users
SET Password = 'E1C7D509F5B57C9374FB2D57FE6DFF7F45CBB1F733BB9B4C0C7F57915566EA82'
WHERE Username = 'admin1';

UPDATE Users
SET Password = '5303D62D9217CF4CAB1CE661A639C09FA748DD1B0F26F281297737CC0C2A66E8'
WHERE Username = 'customer1';*/

UPDATE Users
SET Password = '6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc='
WHERE Username = 'admin1';

UPDATE Users
SET Password = 'mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag='
WHERE Username = 'customer1';

UPDATE Carts
SET UserId = 1004
WHERE UserId = 2;

INSERT INTO Carts (UserId, ProductId, ProductName, Quantity, UnitPrice, TotalPrice, AddedDate)
VALUES
(1004, '1756329037998_a73f022e27104d3785a06d21c97cd873', 'Nikes', 1, 39.99, 39.99, GETDATE()),
(1004, '1756373394074_58e26df5667144f1895276190186def9', 'Perfume', 1, 46.00, 46.00, GETDATE()),
(1004, '1756333217732_905a349d3328418d97916ffa91f00222', 'Watch', 1, 16.99, 16.99, GETDATE()),
(1004, '1763150476619_0dab2c55abb44c9990476bb5ca73c2ee', 'Face masks', 1, 9.99, 9.99, GETDATE()),
(1004, '1763150583459_02dd85db1621450daff7a2852d8929a6', 'BMW', 1, 30000.00, 30000.00, GETDATE());