ABC Retailers Web Application
Overview
ABC Retailers is a modern, cloud-based e-commerce management system built with ASP.NET Core. It addresses the challenges faced by a rapidly growing online retailer by leveraging Azure Storage services for scalable and reliable data handling. The application manages customer profiles, product inventory, order processing, and file uploads, replacing an outdated on-premises system.
This project is part of a Portfolio of Evidence (POE) for module PROG6212, demonstrating the use of Azure compute and data storage components to create a robust solution.
Features

Customer Management: CRUD operations for customer profiles stored in Azure Table Storage.
Product Management: Add, edit, delete products with image uploads to Azure Blob Storage.
Order Processing: Create orders, update stock, and send notifications via Azure Queue Storage. Includes real-time stock checks and status updates.
File Uploads: Upload proof of payment or dummy contracts to Azure Blob Storage and File Shares.
Dashboard: Home page with featured products and counts of customers, products, and orders.
Scalability & Reliability: Automatic initialization of Azure resources; handles peak loads with cloud migration benefits.
Deployment: Easily deployable to Azure App Service for web accessibility.

Technologies Used

Framework: ASP.NET Core MVC (.NET 9)
Azure Services:

Table Storage: For structured data (Customers, Products, Orders).
Blob Storage: For images and multimedia (containers: product-images, payment-proofs).
Queue Storage: For messaging (queues: order-notifications, stock-updates).
File Shares: For dummy contracts (share: contracts with directory payments).


Other Libraries: Azure SDKs (Azure.Data.Tables, Azure.Storage.Blobs, etc.), JSON serialization.
Development Tools: Visual Studio, GitHub for version control.

Test Features

Navigate to /Customer for customer CRUD.
/Product for products (upload images).
/Order for orders (triggers queues).
/Upload for file uploads.
Add at least 5 records each and verify in Azure Portal.
