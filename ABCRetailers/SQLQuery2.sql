IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Carts' AND xtype='U')
BEGIN
    CREATE TABLE Carts (
        CartId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        ProductId NVARCHAR(100) NOT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        TotalPrice DECIMAL(18,2) NOT NULL,
        AddedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Carts_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
END;

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
BEGIN
    CREATE TABLE Orders (
        OrderId NVARCHAR(100) PRIMARY KEY,
        CustomerId NVARCHAR(100) NOT NULL,
        Username NVARCHAR(100) NOT NULL,
        ProductId NVARCHAR(100) NOT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        OrderDate DATETIME NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice FLOAT NOT NULL,
        TotalPrice FLOAT NOT NULL,
        Status NVARCHAR(50) NOT NULL
    );
END;

-- Users (admin + customer, hashed passwords for Admin@123 and Customer@123)
INSERT INTO Users (Username, Password, Name, Surname, Email, ShippingAddress, Role, CreatedDate)
VALUES
('admin1', '6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=', 'Lerato', 'Mokoena', 'admin@abc.com', '1 Admin Way, Sandton', 'Admin', GETDATE()),
('customer1', 'mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=', 'Sipho', 'Nkosi', 'sipho@abc.com', '45 Main Rd, Midrand', 'Customer', GETDATE());

-- Cart items (ProductId values from your products)
INSERT INTO Carts (UserId, ProductId, ProductName, Quantity, UnitPrice, TotalPrice, AddedDate)
VALUES
(2, '1756329037998_a73f022e27104d3785a06d21c97cd873', 'Nikes', 1, 39.99, 39.99, GETDATE()),
(2, '1756373394074_58e26df5667144f1895276190186def9', 'Perfume', 1, 46.00, 46.00, GETDATE()),
(2, '1756333217732_905a349d3328418d97916ffa91f00222', 'Watch', 1, 16.99, 16.99, GETDATE()),
(2, '1763150476619_0dab2c55abb44c9990476bb5ca73c2ee', 'Face Masks', 1, 9.99, 9.99, GETDATE()),
(2, '1763150583459_02dd85db1621450daff7a2852d8929a6', 'BMW', 1, 30000.00, 30000.00, GETDATE());

DELETE FROM Carts;
DELETE FROM Users;

INSERT INTO Users (Username, Password, Name, Surname, Email, ShippingAddress, Role, CreatedDate) VALUES
('admin1', '6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=', 'Lerato', 'Mokoena', 'admin@abc.com', '1 Admin Way, Sandton', 'Admin', GETDATE()),
('admin2', '6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=', 'Mpumi', 'Klaas', 'mpumi@abc.com', '12 House Street', 'Admin', GETDATE()),
('customer1', 'mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=', 'Sipho', 'Nkosi', 'sipho@abc.com', '45 Main Rd, Midrand', 'Customer', GETDATE()),
('customer2', 'mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=', 'Anne', 'Hlahane', 'anne@abc.com', '29 College Ave', 'Customer', GETDATE()),
('customer3', 'mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=', 'Tom', 'Holland', 'tom@abc.com', '10 Willow Dr', 'Customer', GETDATE());

-- Cart entries tied to customer1 (UserId = 3 unless identity changed; check with SELECT * FROM Users)
INSERT INTO Carts (UserId, ProductId, ProductName, Quantity, UnitPrice, TotalPrice, AddedDate) VALUES
( (SELECT UserId FROM Users WHERE Username='customer1'), '1756329037998_a73f022e27104d3785a06d21c97cd873', 'Nikes', 1, 39.99, 39.99, GETDATE()),
( (SELECT UserId FROM Users WHERE Username='customer1'), '1756373394074_58e26df5667144f1895276190186def9', 'Perfume', 1, 46.00, 46.00, GETDATE()),
( (SELECT UserId FROM Users WHERE Username='customer1'), '1756333217732_905a349d3328418d97916ffa91f00222', 'Watch', 1, 16.99, 16.99, GETDATE()),
( (SELECT UserId FROM Users WHERE Username='customer1'), '1763150476619_0dab2c55abb44c9990476bb5ca73c2ee', 'Face Masks', 1, 9.99, 9.99, GETDATE()),
( (SELECT UserId FROM Users WHERE Username='customer1'), '1763150583459_02dd85db1621450daff7a2852d8929a6', 'BMW', 1, 30000.00, 30000.00, GETDATE());