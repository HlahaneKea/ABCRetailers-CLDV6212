# ABCRetailers Azure Functions - POE Part 2

## Overview
This Functions project contains 5 Azure Functions that handle all storage operations for the ABC Retailers web application.

## Functions Implemented

### 1. CustomersFunctions.cs - Table Storage
**HTTP Triggers**
- `GET /api/customers` - Get all customers
- `GET /api/customers/{id}` - Get specific customer
- `POST /api/customers` - Create new customer
- `PUT /api/customers/{id}` - Update customer
- `DELETE /api/customers/{id}` - Delete customer

**Storage Used**: Azure Table Storage (`customers` table)

### 2. ProductsFunctions.cs - Blob Storage
**HTTP Triggers**
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get specific product
- `POST /api/products` - Create product with image upload
- `PUT /api/products/{id}` - Update product with optional image
- `DELETE /api/products/{id}` - Delete product

**Storage Used**: 
- Azure Table Storage (`products` table)
- Azure Blob Storage (`productimages` container)

### 3. OrdersFunctions.cs - Queue Integration
**HTTP Triggers**
- `GET /api/orders` - Get all orders
- `GET /api/orders/{id}` - Get specific order
- `POST /api/orders` - Submit order to queue (does NOT write to table directly)
- `PUT /api/orders/{id}` - Update order
- `DELETE /api/orders/{id}` - Delete order

**Storage Used**: 
- Azure Queue Storage (`order-processing` queue)
- Azure Table Storage (`orders` table)

**IMPORTANT**: The POST endpoint sends orders to queue. The queue trigger processes them.

### 4. QueueProcessorFunctions.cs - Queue Trigger ⭐
**Queue Trigger**
- `ProcessOrderQueue` - Triggered by messages in `order-processing` queue
- Reads order from queue
- Generates unique Order ID
- Writes order to `orders` table
- Sends notification to `order-notifications` queue

**This is the required Queue Trigger Function for POE Part 2!**

### 5. UploadsFunctions.cs - File Storage
**HTTP Triggers**
- `POST /api/files/upload` - Upload file to single share
- `POST /api/files/upload-multiple` - Upload file to multiple shares
- `GET /api/files/{shareName}/{directoryName}` - List files
- `DELETE /api/files/{shareName}/{directoryName}/{fileName}` - Delete file

**Storage Used**: Azure File Storage (`payment-proofs` and `contracts` shares)

## Configuration Required

### local.settings.json
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "YOUR_STORAGE_CONNECTION_STRING",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureStorageConnection": "YOUR_STORAGE_CONNECTION_STRING"
  }
}
```

### Azure Function App Settings
Add this application setting:
- **Name**: `AzureStorageConnection`
- **Value**: Your storage account connection string

## How Orders Work (Queue Trigger Flow)

1. **Web App** → Calls `POST /api/orders` (OrdersFunctions.cs)
2. **OrdersFunctions.CreateOrder** → Sends message to `order-processing` queue
3. **Queue** → Holds the order message
4. **QueueProcessorFunctions.ProcessOrderQueue** → **TRIGGERED AUTOMATICALLY**
5. **Queue Trigger** → Reads message, creates order in table, sends notification

This ensures:
- ✅ No orders are lost if system crashes
- ✅ Orders processed reliably one by one
- ✅ Meets POE Part 2 requirement for Queue Trigger Function

## Testing Locally

1. Start Functions:
   ```bash
   cd ABCRetailers.Functions
   func start
   ```

2. Functions will run on `http://localhost:7071`

3. Start web app (it will call these Functions)

4. Check terminal output to see functions executing

## Deployment

1. Right-click project → Publish
2. Choose Azure → Azure Function App
3. Select or create Function App
4. Publish
5. Add `AzureStorageConnection` in Azure Portal settings

## NuGet Packages Used

- Microsoft.Azure.Functions.Worker (v1.23.0)
- Microsoft.Azure.Functions.Worker.Sdk (v1.18.0)
- Microsoft.Azure.Functions.Worker.Extensions.Http (v3.2.0)
- Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues (v5.5.0)
- Azure.Data.Tables (v12.9.1)
- Azure.Storage.Blobs (v12.22.2)
- Azure.Storage.Queues (v12.20.1)
- Azure.Storage.Files.Shares (v12.21.0)

## Project Structure

```
ABCRetailers.Functions/
│
├── Functions/
│   ├── CustomersFunctions.cs      (Table)
│   ├── ProductsFunctions.cs       (Blob + Table)
│   ├── OrdersFunctions.cs         (Queue + Table)
│   ├── QueueProcessorFunctions.cs (Queue Trigger)
│   └── UploadsFunctions.cs        (File Storage)
│
├── Entities/
│   └── TableEntities.cs           (CustomerEntity, ProductEntity, OrderEntity)
│
├── Models/
│   └── ApiModels.cs               (DTOs for API)
│
├── Helpers/
│   ├── HttpJson.cs                (JSON serialization)
│   ├── MultipartHelper.cs         (File upload parsing)
│   └── Map.cs                     (Entity ↔ DTO mapping)
│
├── Program.cs                     (Function app startup)
├── host.json                      (Function host config)
└── local.settings.json            (Local config - NOT deployed)
```

## Troubleshooting

**Functions won't start:**
- Check .NET 9.0 SDK is installed
- Verify Azure Functions Core Tools installed
- Run `dotnet restore` in project directory

**Queue trigger not firing:**
- Verify `AzureStorageConnection` is set
- Check queue `order-processing` exists
- Look for errors in function logs

**Can't upload files:**
- Check multipart form data is sent correctly
- Verify file size limits
- Check file share exists in storage account

**Blob upload fails:**
- Ensure `productimages` container exists
- Verify storage connection has write permissions
- Check SAS token generation code

## For POE Part 2 Submission

You need to show:
1. ✅ **4 HTTP-triggered functions** (Customers, Products, Orders, Files)
2. ✅ **1 Queue-triggered function** (QueueProcessorFunctions)
3. ✅ **Screenshots of functions in Azure Portal**
4. ✅ **Screenshots of function code**
5. ✅ **Screenshot of queue message or execution logs**
6. ✅ **Screenshot of uploaded files in File Storage**

**This project provides all of this!**

## Need More Help?

See `POE_PART2_SETUP_GUIDE.md` in the parent directory for complete step-by-step instructions.

