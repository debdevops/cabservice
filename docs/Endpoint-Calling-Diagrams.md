# CabService Endpoint Calling Diagrams

This document contains detailed Mermaid diagrams showing all endpoint calling patterns in the CabService architecture.

## 1. Frontend to Backend API Calls

### Passenger App Flow
```mermaid
graph TD
    subgraph "React Frontend"
        PA[Passenger App]
        API[API Service]
        SignalR[SignalR Service]
    end
    
    subgraph "API Gateway"
        APIM[Azure API Management]
    end
    
    subgraph "Microservices"
        PS[Passenger Service]
        DS[Driver Service]
        BS[Booking Service]
        PayS[Payment Service]
        NS[Notification Service]
    end
    
    PA -->|"POST /api/passengers<br/>Register"| API
    PA -->|"GET /api/passengers/{id}<br/>Get Profile"| API
    PA -->|"PUT /api/passengers/{id}<br/>Update Profile"| API
    PA -->|"POST /api/passengers/{id}/verify<br/>Verify Identity"| API
    
    PA -->|"POST /api/bookings<br/>Create Booking"| API
    PA -->|"GET /api/bookings/{id}<br/>Get Booking"| API
    PA -->|"PUT /api/bookings/{id}/cancel<br/>Cancel Booking"| API
    PA -->|"GET /api/bookings/passenger/{id}<br/>Get My Bookings"| API
    PA -->|"POST /api/bookings/estimate-fare<br/>Calculate Fare"| API
    PA -->|"PUT /api/bookings/{id}/rate<br/>Rate Trip"| API
    
    PA -->|"POST /api/payments<br/>Process Payment"| API
    PA -->|"GET /api/payments/passenger/{id}<br/>Payment History"| API
    
    PA -->|"GET /api/notifications/user/{id}<br/>Get Notifications"| API
    PA -->|"PUT /api/notifications/{id}/read<br/>Mark as Read"| API
    PA -->|"GET /api/notifications/user/{id}/unread-count<br/>Unread Count"| API
    
    API --> APIM
    APIM --> PS
    APIM --> BS
    APIM --> PayS
    APIM --> NS
    
    SignalR -.->|"Real-time Updates"| NS
```

### Driver App Flow
```mermaid
graph TD
    subgraph "React Frontend"
        DA[Driver App]
        API[API Service]
        SignalR[SignalR Service]
    end
    
    subgraph "API Gateway"
        APIM[Azure API Management]
    end
    
    subgraph "Microservices"
        DS[Driver Service]
        BS[Booking Service]
        PayS[Payment Service]
        NS[Notification Service]
    end
    
    DA -->|"POST /api/drivers<br/>Register"| API
    DA -->|"GET /api/drivers/{id}<br/>Get Profile"| API
    DA -->|"PUT /api/drivers/{id}<br/>Update Profile"| API
    DA -->|"POST /api/drivers/{id}/verify<br/>Verify Documents"| API
    DA -->|"POST /api/drivers/{id}/status<br/>Update Availability"| API
    DA -->|"POST /api/drivers/{id}/location<br/>Update Location"| API
    DA -->|"GET /api/drivers/{id}/profile<br/>Get Detailed Profile"| API
    
    DA -->|"GET /api/bookings/driver/{id}<br/>Get My Bookings"| API
    DA -->|"PUT /api/bookings/{id}/accept<br/>Accept Booking"| API
    DA -->|"PUT /api/bookings/{id}/start<br/>Start Trip"| API
    DA -->|"PUT /api/bookings/{id}/complete<br/>Complete Trip"| API
    
    DA -->|"GET /api/payments/driver/{id}<br/>Earnings History"| API
    
    DA -->|"GET /api/notifications/user/{id}<br/>Get Notifications"| API
    DA -->|"PUT /api/notifications/{id}/read<br/>Mark as Read"| API
    
    API --> APIM
    APIM --> DS
    APIM --> BS
    APIM --> PayS
    APIM --> NS
    
    SignalR -.->|"Real-time Updates"| NS
```

## 2. Inter-Microservice Communication

### Booking Service Internal Calls
```mermaid
graph TD
    subgraph "Booking Service"
        BC[Booking Controller]
        BS[Booking Service]
        DMS[Driver Matching Service]
        FCS[Fare Calculation Service]
    end
    
    subgraph "External Services"
        DS[Driver Service]
        PS[Payment Service]
        NS[Notification Service]
    end
    
    subgraph "Azure Service Bus"
        SB[Service Bus Topics]
    end
    
    BC -->|"CreateBookingAsync"| BS
    BS -->|"FindNearbyDriversAsync"| DMS
    DMS -->|"GET /api/drivers/nearby"| DS
    DS -->|"Available Drivers List"| DMS
    DMS -->|"Driver List"| BS
    
    BS -->|"CalculateFareAsync"| FCS
    FCS -->|"Fare Amount"| BS
    
    BS -->|"Publish BookingCreated"| SB
    SB -->|"BookingCreated Event"| NS
    
    BC -->|"AcceptBookingAsync"| BS
    BS -->|"Publish BookingAccepted"| SB
    SB -->|"BookingAccepted Event"| NS
    
    BC -->|"CompleteBookingAsync"| BS
    BS -->|"Publish TripCompleted"| SB
    SB -->|"TripCompleted Event"| PS
    SB -->|"TripCompleted Event"| NS
```

### Payment Service Integration Flow
```mermaid
graph TD
    subgraph "Payment Service"
        PC[Payment Controller]
        PS[Payment Service]
        RS[Refund Service]
    end
    
    subgraph "External Payment Gateway"
        Stripe[Stripe API]
    end
    
    subgraph "Other Services"
        BS[Booking Service]
        NS[Notification Service]
    end
    
    subgraph "Azure Service Bus"
        SB[Service Bus Topics]
    end
    
    PC -->|"ProcessPaymentAsync"| PS
    PS -->|"POST /charges"| Stripe
    Stripe -->|"Payment Response"| PS
    PS -->|"Publish PaymentProcessed"| SB
    SB -->|"PaymentProcessed Event"| NS
    
    PC -->|"ProcessRefundAsync"| RS
    RS -->|"POST /refunds"| Stripe
    Stripe -->|"Refund Response"| RS
    RS -->|"Publish RefundProcessed"| SB
    SB -->|"RefundProcessed Event"| NS
    
    SB -->|"TripCompleted Event"| PS
    PS -->|"Auto Process Payment"| Stripe
    
    PC -->|"POST /webhook/stripe"| PS
    Stripe -->|"Webhook Events"| PS
```

## 3. Notification Service Communication Hub

### Multi-Channel Notification Flow
```mermaid
graph TD
    subgraph "Notification Service"
        NC[Notification Controller]
        NS[Notification Service]
        ES[Email Service]
        SMS[SMS Service]
        PNS[Push Notification Service]
        TS[Template Service]
    end
    
    subgraph "External Services"
        SendGrid[SendGrid API]
        Twilio[Twilio API]
        Firebase[Firebase FCM]
    end
    
    subgraph "Azure Service Bus"
        SB[Service Bus Subscriptions]
    end
    
    subgraph "SignalR Hub"
        Hub[Notification Hub]
    end
    
    SB -->|"BookingCreated Event"| NS
    SB -->|"BookingAccepted Event"| NS
    SB -->|"TripCompleted Event"| NS
    SB -->|"PaymentProcessed Event"| NS
    
    NS -->|"Get Template"| TS
    TS -->|"Processed Template"| NS
    
    NS -->|"Send Email"| ES
    ES -->|"POST /mail/send"| SendGrid
    
    NS -->|"Send SMS"| SMS
    SMS -->|"POST /messages"| Twilio
    
    NS -->|"Send Push"| PNS
    PNS -->|"POST /fcm/send"| Firebase
    
    NS -->|"Real-time Update"| Hub
    Hub -.->|"SignalR"| Client[Frontend Clients]
    
    NC -->|"SendNotificationAsync"| NS
    NC -->|"SendBulkNotificationsAsync"| NS
    NC -->|"ScheduleNotificationAsync"| NS
```

## 4. Azure Functions Background Processing

### Scheduled Functions Flow
```mermaid
graph TD
    subgraph "Azure Functions"
        BTF[Booking Timeout Function]
        PRF[Payment Retry Function]
        DLF[Driver Location Function]
    end
    
    subgraph "Timer Triggers"
        T1[Timer: 5 min]
        T2[Timer: 10 min]
        T3[Timer: 2 min]
    end
    
    subgraph "Microservices"
        BS[Booking Service]
        PS[Payment Service]
        DS[Driver Service]
        NS[Notification Service]
    end
    
    subgraph "External Services"
        Stripe[Stripe API]
    end
    
    subgraph "Azure Cosmos DB"
        DB[(Cosmos DB)]
    end
    
    T1 -->|"Every 5 minutes"| BTF
    BTF -->|"Query Expired Bookings"| DB
    BTF -->|"Cancel Booking"| BS
    BTF -->|"Send Timeout Notification"| NS
    
    T2 -->|"Every 10 minutes"| PRF
    PRF -->|"Query Failed Payments"| DB
    PRF -->|"Retry Payment"| Stripe
    PRF -->|"Update Payment Status"| PS
    PRF -->|"Send Retry Notification"| NS
    
    T3 -->|"Every 2 minutes"| DLF
    DLF -->|"Query Stale Locations"| DB
    DLF -->|"Remove Inactive Drivers"| DS
```

## 5. Complete End-to-End Booking Flow

### Passenger Books a Ride
```mermaid
sequenceDiagram
    participant PA as Passenger App
    participant APIM as API Gateway
    participant BS as Booking Service
    participant DS as Driver Service
    participant PS as Payment Service
    participant NS as Notification Service
    participant SB as Service Bus
    participant DA as Driver App
    participant Stripe as Stripe API
    
    Note over PA: User requests ride
    PA->>APIM: POST /api/bookings/estimate-fare
    APIM->>BS: Calculate fare estimate
    BS->>PA: Return fare estimate
    
    PA->>APIM: POST /api/bookings
    APIM->>BS: Create booking request
    BS->>DS: GET /api/drivers/nearby
    DS->>BS: Return available drivers
    BS->>SB: Publish BookingCreated event
    BS->>PA: Return booking confirmation
    
    SB->>NS: BookingCreated event received
    NS->>PA: Send confirmation (SignalR)
    NS->>DA: Send booking request (Push notification)
    
    Note over DA: Driver accepts booking
    DA->>APIM: PUT /api/bookings/{id}/accept
    APIM->>BS: Accept booking
    BS->>SB: Publish BookingAccepted event
    BS->>DA: Return acceptance confirmation
    
    SB->>NS: BookingAccepted event received
    NS->>PA: Driver assigned (SignalR)
    NS->>DA: Booking confirmed (Push)
    
    Note over DA: Driver starts trip
    DA->>APIM: PUT /api/bookings/{id}/start
    APIM->>BS: Start trip
    BS->>SB: Publish TripStarted event
    
    SB->>NS: TripStarted event received
    NS->>PA: Trip started (SignalR)
    
    Note over DA: Driver completes trip
    DA->>APIM: PUT /api/bookings/{id}/complete
    APIM->>BS: Complete trip
    BS->>SB: Publish TripCompleted event
    
    SB->>PS: TripCompleted event received
    PS->>Stripe: Process payment
    Stripe->>PS: Payment response
    PS->>SB: Publish PaymentProcessed event
    
    SB->>NS: PaymentProcessed event received
    NS->>PA: Payment confirmation (Email + SMS)
    NS->>DA: Payment processed (Push)
    
    PA->>APIM: PUT /api/bookings/{id}/rate
    APIM->>BS: Submit rating
    BS->>PA: Rating confirmation
```

## 6. Error Handling and Retry Patterns

### Payment Failure Recovery Flow
```mermaid
graph TD
    subgraph "Payment Processing"
        PS[Payment Service]
        Stripe[Stripe API]
    end
    
    subgraph "Azure Functions"
        PRF[Payment Retry Function]
    end
    
    subgraph "Notification System"
        NS[Notification Service]
    end
    
    subgraph "Database"
        DB[(Cosmos DB)]
    end
    
    PS -->|"Payment Request"| Stripe
    Stripe -->|"Payment Failed"| PS
    PS -->|"Log Failed Payment"| DB
    PS -->|"Send Failure Event"| NS
    NS -->|"Notify Customer"| Customer[Customer]
    
    PRF -->|"Query Failed Payments"| DB
    PRF -->|"Retry Payment"| Stripe
    Stripe -->|"Success/Failure"| PRF
    PRF -->|"Update Status"| DB
    PRF -->|"Send Result Event"| NS
    
    style Stripe fill:#ff6b6b
    style PRF fill:#4ecdc4
    style NS fill:#45b7d1
```

## 7. Authentication and Authorization Flow

### Azure MSAL Authentication
```mermaid
sequenceDiagram
    participant User as User
    participant React as React App
    participant MSAL as Azure MSAL
    participant AAD as Azure AD
    participant APIM as API Gateway
    participant MS as Microservices
    
    User->>React: Login request
    React->>MSAL: initiate login
    MSAL->>AAD: Redirect to login
    AAD->>User: Present login form
    User->>AAD: Enter credentials
    AAD->>MSAL: Return access token
    MSAL->>React: Token received
    React->>React: Store token in localStorage
    
    Note over React: Making API calls
    React->>APIM: API request with Bearer token
    APIM->>AAD: Validate token
    AAD->>APIM: Token validation result
    APIM->>MS: Forward request if valid
    MS->>APIM: Response
    APIM->>React: API response
    
    Note over React: Token refresh
    React->>MSAL: Check token expiry
    MSAL->>AAD: Silent token refresh
    AAD->>MSAL: New access token
    MSAL->>React: Updated token
```

## 8. Real-Time Communication with SignalR

### SignalR Hub Connections
```mermaid
graph TD
    subgraph "Frontend Clients"
        PA[Passenger App]
        DA[Driver App]
        Admin[Admin Dashboard]
    end
    
    subgraph "SignalR Hub"
        Hub[Notification Hub]
        CM[Connection Manager]
        GM[Group Manager]
    end
    
    subgraph "Notification Service"
        NS[Notification Service]
        RT[Real-time Service]
    end
    
    subgraph "Event Sources"
        BS[Booking Service]
        PS[Payment Service]
        DS[Driver Service]
    end
    
    PA -->|"Connect with JWT"| Hub
    DA -->|"Connect with JWT"| Hub
    Admin -->|"Connect with JWT"| Hub
    
    Hub -->|"Manage Connections"| CM
    Hub -->|"Join Groups"| GM
    
    BS -->|"Booking Events"| NS
    PS -->|"Payment Events"| NS
    DS -->|"Driver Events"| NS
    
    NS -->|"Real-time Updates"| RT
    RT -->|"Send to Groups"| Hub
    
    Hub -.->|"Booking Updates"| PA
    Hub -.->|"Trip Requests"| DA
    Hub -.->|"System Alerts"| Admin
```

## 9. Data Access Patterns

### Cosmos DB Partition Strategy
```mermaid
graph TD
    subgraph "Microservices"
        PS[Passenger Service]
        DS[Driver Service]
        BS[Booking Service]
        PayS[Payment Service]
        NS[Notification Service]
    end
    
    subgraph "Cosmos DB Containers"
        PassengerDB[(passengers<br/>partition: /id)]
        DriverDB[(drivers<br/>partition: /id)]
        DeviceDB[(deviceTokens<br/>partition: /userId)]
        BookingDB[(bookings<br/>partition: /passengerId)]
        PaymentDB[(payments<br/>partition: /bookingId)]
        NotificationDB[(notifications<br/>partition: /userId)]
        TemplateDB[(templates<br/>partition: /type)]
    end
    
    PS -->|"CRUD Operations"| PassengerDB
    DS -->|"CRUD Operations"| DriverDB
    DS -->|"Location Updates"| DeviceDB
    BS -->|"Booking Management"| BookingDB
    PayS -->|"Payment Processing"| PaymentDB
    NS -->|"User Notifications"| NotificationDB
    NS -->|"Template Management"| TemplateDB
    
    style PassengerDB fill:#e1f5fe
    style DriverDB fill:#f3e5f5
    style DeviceDB fill:#e8f5e8
    style BookingDB fill:#fff3e0
    style PaymentDB fill:#fce4ec
    style NotificationDB fill:#f1f8e9
    style TemplateDB fill:#e3f2fd
```

## 10. External Service Integration Patterns

### Third-Party Service Calls
```mermaid
graph TD
    subgraph "CabService Microservices"
        PS[Payment Service]
        NS[Notification Service]
    end
    
    subgraph "Stripe Integration"
        PS -->|"POST /v1/charges"| Stripe[Stripe API]
        PS -->|"POST /v1/refunds"| Stripe
        PS -->|"GET /v1/charges/{id}"| Stripe
        Stripe -->|"Webhook Events"| PS
    end
    
    subgraph "SendGrid Integration"
        NS -->|"POST /v3/mail/send"| SendGrid[SendGrid API]
        NS -->|"GET /v3/templates"| SendGrid
        SendGrid -->|"Email Status"| NS
    end
    
    subgraph "Twilio Integration"
        NS -->|"POST /Messages"| Twilio[Twilio API]
        NS -->|"GET /Messages/{sid}"| Twilio
        Twilio -->|"SMS Status"| NS
    end
    
    subgraph "Firebase Integration"
        NS -->|"POST /fcm/send"| Firebase[Firebase FCM]
        NS -->|"POST /fcm/send (batch)"| Firebase
        Firebase -->|"Push Status"| NS
    end
    
    style Stripe fill:#635bff
    style SendGrid fill:#1a82e2
    style Twilio fill:#f22f46
    style Firebase fill:#ffca28
```

## 11. API Gateway Routing Patterns

### Azure API Management Routing
```mermaid
graph TD
    subgraph "Client Applications"
        React[React Frontend]
        Mobile[Mobile Apps]
        Postman[API Testing]
    end
    
    subgraph "Azure API Management"
        APIM[API Gateway]
        Auth[Authentication]
        Rate[Rate Limiting]
        Cache[Response Caching]
        Log[Request Logging]
    end
    
    subgraph "Backend Services"
        PS[Passenger Service<br/>:7001]
        DS[Driver Service<br/>:7002]
        BS[Booking Service<br/>:7003]
        PayS[Payment Service<br/>:7004]
        NS[Notification Service<br/>:7005]
    end
    
    React --> APIM
    Mobile --> APIM
    Postman --> APIM
    
    APIM --> Auth
    Auth --> Rate
    Rate --> Cache
    Cache --> Log
    
    Log -->|"Route: /api/passengers/*"| PS
    Log -->|"Route: /api/drivers/*"| DS
    Log -->|"Route: /api/bookings/*"| BS
    Log -->|"Route: /api/payments/*"| PayS
    Log -->|"Route: /api/notifications/*"| NS
    
    style APIM fill:#0078d4
    style Auth fill:#107c10
    style Rate fill:#d83b01
    style Cache fill:#5c2d91
    style Log fill:#ca5010
```

---

## How to Generate PNG Images from Mermaid Diagrams

### Method 1: Using Mermaid CLI
```bash
# Install Mermaid CLI
npm install -g @mermaid-js/mermaid-cli

# Generate PNG from markdown file
mmdc -i Endpoint-Calling-Diagrams.md -o diagrams/ -t dark -b white
```

### Method 2: Using Online Mermaid Editor
1. Visit https://mermaid.live/
2. Copy each diagram code block
3. Paste into the editor
4. Click "Export" → "PNG"
5. Download the generated image

### Method 3: Using VS Code Extension
1. Install "Mermaid Preview" extension
2. Open the markdown file
3. Right-click on diagram → "Export as PNG"

### Recommended Naming Convention
- `01-frontend-backend-calls.png`
- `02-inter-service-communication.png`
- `03-notification-hub.png`
- `04-azure-functions-flow.png`
- `05-end-to-end-booking.png`
- `06-error-handling.png`
- `07-authentication-flow.png`
- `08-signalr-realtime.png`
- `09-cosmos-db-patterns.png`
- `10-external-services.png`
- `11-api-gateway-routing.png`
```
