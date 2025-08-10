# Cab Service Architecture & Flow Diagrams

This document contains comprehensive diagrams explaining the Cab Service Event-Driven Microservice Architecture.

## Technology Stack Overview

### Backend Technologies
- **.NET 8** - All microservices built with latest .NET
- **Azure App Services** - Hosting platform for microservices
- **Azure Cosmos DB** - NoSQL database with partition key strategy
- **Azure Service Bus** - Event-driven messaging system
- **Azure Functions** - Background processing and scheduled tasks
- **SignalR** - Real-time communication hub
- **Azure Key Vault** - Secrets and configuration management
- **Application Insights** - Monitoring and telemetry

### Frontend Technologies
- **React 18** with TypeScript
- **Tailwind CSS** - Utility-first CSS framework
- **Axios** - HTTP client for API communication
- **SignalR Client** - Real-time communication
- **Azure MSAL** - Microsoft Authentication Library
- **React Query** - Server state management
- **React Router** - Client-side routing

### External Integrations
- **Stripe** - Payment processing
- **SendGrid** - Email notifications
- **Twilio** - SMS notifications
- **Firebase** - Push notifications

## 1. High-Level System Architecture

```mermaid
graph TB
    subgraph "Frontend Layer"
        ReactApp[React.js App<br/>Azure Storage + CDN]
    end
    
    subgraph "API Gateway Layer"
        APIM[Azure API Management<br/>Gateway]
    end
    
    subgraph "Microservices Layer"
        PS[Passenger Service<br/>Azure App Service]
        DS[Driver Service<br/>Azure App Service]
        BS[Booking Service<br/>Azure App Service]
        PayS[Payment Service<br/>Azure App Service]
        NS[Notification Service<br/>Azure App Service]
    end
    
    subgraph "Event Processing Layer"
        AF[Azure Functions<br/>Background Processing]
        SB[Azure Service Bus<br/>Event Messaging]
    end
    
    subgraph "Data Layer"
        CosmosDB[(Azure Cosmos DB<br/>NoSQL Database)]
        KV[Azure Key Vault<br/>Secrets Management]
    end
    
    subgraph "External Services"
        Stripe[Stripe<br/>Payment Gateway]
        SendGrid[SendGrid<br/>Email Service]
        Twilio[Twilio<br/>SMS Service]
        Firebase[Firebase<br/>Push Notifications]
    end
    
    subgraph "Monitoring Layer"
        AI[Application Insights<br/>Monitoring & Telemetry]
    end
    
    ReactApp --> APIM
    APIM --> PS
    APIM --> DS
    APIM --> BS
    APIM --> PayS
    APIM --> NS
    
    ReactApp -.->|SignalR| NS
    
    PS --> SB
    DS --> SB
    BS --> SB
    PayS --> SB
    NS --> SB
    
    SB --> AF
    AF --> CosmosDB
    AF --> NS
    
    PS --> CosmosDB
    DS --> CosmosDB
    BS --> CosmosDB
    PayS --> CosmosDB
    NS --> CosmosDB
    
    PayS --> Stripe
    NS --> SendGrid
    NS --> Twilio
    NS --> Firebase
    
    PS --> KV
    DS --> KV
    BS --> KV
    PayS --> KV
    NS --> KV
    
    PS --> AI
    DS --> AI
    BS --> AI
    PayS --> AI
    NS --> AI
    AF --> AI
```

## 2. Microservices API Endpoints

### Passenger Service API
- `GET /api/passengers/{id}` - Get passenger profile
- `POST /api/passengers` - Register new passenger
- `PUT /api/passengers/{id}` - Update passenger profile
- `DELETE /api/passengers/{id}` - Delete passenger
- `GET /api/passengers/{id}/profile` - Get detailed profile
- `POST /api/passengers/{id}/verify` - Verify passenger identity
- `GET /api/passengers/search` - Search passengers

### Driver Service API
- `GET /api/drivers/{id}` - Get driver profile
- `POST /api/drivers` - Register new driver
- `PUT /api/drivers/{id}` - Update driver profile
- `DELETE /api/drivers/{id}` - Delete driver
- `POST /api/drivers/{id}/verify` - Verify driver documents
- `POST /api/drivers/{id}/status` - Update driver availability
- `POST /api/drivers/{id}/location` - Update driver location
- `GET /api/drivers/nearby` - Find nearby drivers
- `GET /api/drivers/{id}/profile` - Get detailed profile

### Booking Service API
- `POST /api/bookings` - Create new booking
- `GET /api/bookings/{id}` - Get booking details
- `PUT /api/bookings/{id}/cancel` - Cancel booking
- `PUT /api/bookings/{id}/accept` - Driver accepts booking
- `PUT /api/bookings/{id}/start` - Start trip
- `PUT /api/bookings/{id}/complete` - Complete trip
- `GET /api/bookings/passenger/{passengerId}` - Get passenger bookings
- `GET /api/bookings/driver/{driverId}` - Get driver bookings
- `POST /api/bookings/estimate-fare` - Calculate fare estimate
- `GET /api/bookings/nearby-drivers` - Find available drivers
- `PUT /api/bookings/{id}/rate` - Rate completed trip

### Payment Service API
- `POST /api/payments` - Process payment
- `GET /api/payments/{id}` - Get payment details
- `GET /api/payments/booking/{bookingId}` - Get booking payments
- `POST /api/payments/{id}/retry` - Retry failed payment
- `POST /api/payments/{id}/refund` - Process refund
- `GET /api/payments/passenger/{passengerId}` - Get passenger payments
- `GET /api/payments/driver/{driverId}` - Get driver payments
- `POST /api/payments/webhook/stripe` - Stripe webhook handler
- `GET /api/payments/{id}/status` - Get payment status

### Notification Service API
- `POST /api/notifications` - Send notification
- `GET /api/notifications/{id}` - Get notification details
- `GET /api/notifications/user/{userId}` - Get user notifications
- `PUT /api/notifications/{id}/read` - Mark notification as read
- `PUT /api/notifications/user/{userId}/read-all` - Mark all as read
- `DELETE /api/notifications/{id}` - Delete notification
- `POST /api/notifications/bulk` - Send bulk notifications
- `GET /api/notifications/user/{userId}/unread-count` - Get unread count
- `POST /api/notifications/schedule` - Schedule notification
- `GET /api/notifications/templates` - Get notification templates

## 3. Event-Driven Flow Architecture

```mermaid
graph LR
    subgraph "Event Publishers"
        PS[Passenger Service]
        DS[Driver Service]
        BS[Booking Service]
        PayS[Payment Service]
    end
    
    subgraph "Azure Service Bus"
        BE[booking-events]
        PE[payment-events]
        DE[driver-events]
        NE[notification-events]
    end
    
    subgraph "Event Subscribers"
        AF[Azure Functions]
        NS[Notification Service]
    end
    
    PS --> NE
    DS --> DE
    BS --> BE
    PayS --> PE
    
    BE --> AF
    PE --> AF
    DE --> AF
    NE --> NS
    
    AF --> NS
    AF --> CosmosDB[(Cosmos DB)]
```

## 4. Azure Functions Background Processing

### BookingTimeoutFunction
- **Trigger**: Timer (every 5 minutes)
- **Purpose**: Handle booking timeouts and expired bookings
- **Actions**: Cancel expired bookings, notify passengers, update driver availability

### PaymentRetryFunction
- **Trigger**: Timer (every 10 minutes)
- **Purpose**: Retry failed payments and process pending refunds
- **Actions**: Retry failed Stripe payments, process refund requests

### DriverLocationFunction
- **Trigger**: Timer (every 2 minutes)
- **Purpose**: Clean up stale driver location data
- **Actions**: Remove inactive drivers from location tracking

## 5. Complete Booking Flow

```mermaid
sequenceDiagram
    participant P as Passenger App
    participant P as Passenger App
    participant BS as Booking Service
    participant DS as Driver Service
    participant PS as Payment Service
    participant NS as Notification Service
    participant SB as Service Bus
    participant AF as Azure Functions
    participant Stripe as Stripe API
    participant AF as Azure Functions
    
    P->>API: Request Ride
    API->>BS: Create Booking
    BS->>SB: Publish BookingRequested Event
    SB->>NS: Notify Passenger
    SB->>AF: Trigger Driver Matching
    
    P->>BS: Create Booking Request
    BS->>DS: Find Available Drivers
    DS-->>BS: Return Driver List
    BS->>SB: Publish BookingCreated Event
    SB->>NS: Trigger Notification
    NS->>P: Send Booking Confirmation (SignalR)
    NS->>DS: Notify Nearby Drivers (Push)
    
    Note over DS: Driver Accepts Booking
    DS->>SB: Publish DriverAccepted Event
    SB->>BS: Update Booking Status
    SB->>NS: Trigger Notification
    NS->>P: Notify Passenger (SignalR + Push)
    
    Note over BS: Trip Completed
    BS->>SB: Publish TripCompleted Event
    SB->>PS: Trigger Payment Processing
    PS->>Stripe: Process Payment
    Stripe-->>PS: Payment Response
    PS->>SB: Publish PaymentProcessed Event
    SB->>NS: Trigger Notification
    NS->>P: Send Payment Confirmation (Email + SMS)
    
    Note over AF: Background Processing
    AF->>BS: Check Booking Timeouts
    AF->>PS: Retry Failed Payments
    AF->>DS: Clean Stale Locationses
```

## 4. Data Flow Architecture

```mermaid
graph TD
    subgraph "User Interfaces"
        PA[Passenger App]
        DA[Driver App]
    end
    
    subgraph "API Layer"
        APIM[API Management Gateway]
    end
    
    subgraph "Business Logic"
        MS[Microservices]
    end
    
    subgraph "Event Processing"
        SB[Service Bus]
        AF[Azure Functions]
    end
    
    subgraph "Data Storage"
        DB[(Cosmos DB)]
        Cache[Redis Cache]
    end
    
    subgraph "External APIs"
        Maps[Maps API]
        Payment[Payment Gateway]
        Comm[Communication APIs]
    end
    
    PA --> APIM
    DA --> APIM
    APIM --> MS
    MS --> SB
    SB --> AF
    AF --> DB
    MS --> DB
    MS --> Cache
    MS --> Maps
    MS --> Payment
    MS --> Comm
```

## 5. Security & Authentication Flow

```mermaid
graph TB
    subgraph "Client Applications"
        React[React App]
        Mobile[Mobile Apps]
    end
    
    subgraph "Identity Provider"
        AAD[Azure AD/Entra ID]
    end
    
    subgraph "API Gateway"
        APIM[API Management<br/>JWT Validation]
    end
    
    subgraph "Microservices"
        Services[Protected APIs<br/>Role-based Authorization]
    end
    
    React --> AAD
    Mobile --> AAD
    AAD --> React
    AAD --> Mobile
    
    React --> APIM
    Mobile --> APIM
    
    APIM --> Services
    
    Note over APIM: "Validates JWT tokens<br/>Enforces rate limits<br/>API policies"
    Note over Services: "Role-based access<br/>Passenger, Driver, Admin"
```

## 6. Real-time Communication Flow

```mermaid
graph LR
    subgraph "Event Sources"
        BS[Booking Service]
        DS[Driver Service]
        PS[Payment Service]
    end
    
    subgraph "Event Processing"
        SB[Service Bus]
        NS[Notification Service]
    end
    
    subgraph "Real-time Delivery"
        SignalR[SignalR Hubs]
        Push[Push Notifications]
        Email[Email/SMS]
    end
    
    subgraph "Client Applications"
        Web[Web App]
        Mobile[Mobile Apps]
    end
    
    BS --> SB
    DS --> SB
    PS --> SB
    
    SB --> NS
    NS --> SignalR
    NS --> Push
    NS --> Email
    
    SignalR --> Web
    Push --> Mobile
    Email --> Web
    Email --> Mobile
```

## 7. Azure Infrastructure Overview

```mermaid
graph TB
    subgraph "Azure Resource Group"
        subgraph "Compute Services"
            AS[App Services<br/>5 Microservices]
            AF[Azure Functions<br/>Background Processing]
        end
        
        subgraph "Data Services"
            Cosmos[(Cosmos DB<br/>Multi-database)]
            SB[Service Bus<br/>Event Messaging]
        end
        
        subgraph "Web Services"
            APIM[API Management<br/>Gateway]
            Storage[Storage Account<br/>Static Website]
            CDN[CDN<br/>Global Distribution]
        end
        
        subgraph "Security & Monitoring"
            KV[Key Vault<br/>Secrets]
            AI[Application Insights<br/>Monitoring]
            AAD[Azure AD<br/>Authentication]
        end
    end
    
    AS --> Cosmos
    AS --> SB
    AS --> KV
    AS --> AI
    AF --> Cosmos
    AF --> SB
    APIM --> AS
    Storage --> CDN
```
