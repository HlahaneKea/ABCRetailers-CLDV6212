# ABCRetailers ASP.NET MVC Web App – POE Part 3

## Overview
This MVC project adds SQL‑backed authentication, shopping cart workflow, and admin order processing to the ABC Retailers solution. It integrates with the Azure Functions (Part 2) API for legacy storage operations while persisting login + cart data in Azure SQL.

## Features Implemented

### 1. SQL Login & Role Management
- MVC registration + login screens
- Passwords stored as SHA256 hashes
- Roles: `Admin` and `Customer`
- Session middleware keeps users authenticated for 30 minutes
- Landing routes send Admins to `AdminDashboard` and Customers to `CustomerDashboard`

**Storage Used:** Azure SQL Database (`Users` table)

### 2. Shopping Cart & Checkout
- Customers add products (from Functions API) into a SQL cart
- Cart items persisted per user (`Carts` table)
- Quantity updates and removal handled via AJAX with anti-forgery tokens
- Checkout produces an order summary and redirects to confirmation

**Storage Used:** Azure SQL Database (`Carts` table)

### 3. Admin Order Management
- `ManageOrders` view pulls orders (from Azure Table Storage via Functions)
- Admins can update statuses to `PROCESSED`
- Provides evidence for “administrators update status” requirement

**Storage Used:** Azure Table Storage (`orders` table via Functions API)

### 4. Dual Storage Integration
- Azure SQL: authentication, carts
- Azure Storage (Table/Blob/Queue/File): customers, products, orders, uploads (via Part 2 Functions)

## Configuration Required

### appsettings.json
{
"ConnectionStrings": {
"AzureStorage": "AZURE_STORAGE_CONN",
"DefaultConnection": "Server=tcp:SERVER.database.windows.net,1433;Initial Catalog=AbcRetailersDB;User ID=USER;Password=PASSWORD;..."
},
"Functions": {
"BaseUrl": "https://<function-app>.azurewebsites.net/api/",
"FunctionKey": ""
},
"UseFunctions": true
}

> For local debugging, `appsettings.Development.json` sets `UseFunctions=false` and blank `BaseUrl` so the app can run without the Functions host.

### SQL Database
Run the Part 3 SQL script to create tables and seed users/carts:
CREATE TABLE Users (...);
CREATE TABLE Carts (...);
INSERT INTO Users (...); -- at least 5 entries (admins + customers)
INSERT INTO Carts (...); -- cart items for testing/screenshotsAdjust passwords using the SHA256 hashes from `LoginController.HashPassword`.

## Running Locally
1. Ensure SQL Server LocalDB (or Azure SQL via connection string) is reachable.
2. `dotnet restore` (or build in Visual Studio).
3. Update `local.settings.json` for Functions if you’re running them simultaneously.
4. Hit F5 → app opens at `/Login/Login`.
5. Use seeded credentials (`admin1/Admin@123`, `customer1/Customer@123`) to test.

## Deployment (Azure App Service)
1. Set `DefaultConnection` and `AzureStorage` connection strings in App Service → Configuration.
2. Publish profile: `ABCRetailers11 - Web Deploy`.
3. After deployment, run through:
   - Customer login → cart → checkout → confirmation
   - Admin login → Manage Orders → update status
4. Capture screenshots of the live URL for submission.

## Testing Scenarios
- ✅ Customer can log in, view dashboard, add/update cart, checkout.
- ✅ SQL `Users` shows ≥5 entries (mix of roles).
- ✅ SQL `Carts` reflects cart contents.
- ✅ Admin Manage Orders displays orders from Storage, status updates to `PROCESSED`.
- ✅ Functions fallback works when `UseFunctions`=false (local debugger only).

## Troubleshooting
| Issue | Fix |
| --- | --- |
| “Invalid username/password” | Ensure hashes in SQL match `HashPassword`. |
| Cart update error | Verify anti-forgery token is on the cart page and AJAX call. |
| Functions API errors | Check `Functions.BaseUrl` + key; set `UseFunctions=false` if running without the host. |
| Publish 401 | Re-download publish profile or re-enter App Service credentials. |
| GitHub push blocked | Remove secrets (SQL passwords, function keys) before committing. |

## Evidence Checklist (POE Part 3)
1. 🖼 Azure Portal screenshots (SQL server + DB creation).
2. 🖼 SSMS/VS showing `Users` (≥5 rows) and `Carts`.
3. 🖼 Customer login, dashboard, cart, checkout, confirmation.
4. 🖼 Admin dashboard + Manage Orders with status change.
5. 🖼 Deployment proof (VS publish success + live site URL).
6. 🔗 GitHub repo link (`https://github.com/HlahaneKea/ABCRetailers-CLDV6212`).
7. 🔗 Deployed website URL (`https://st10445678.azurewebsites.net`).
8. 📹 6‑minute walkthrough video (All functionality Parts 1–3).

