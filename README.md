# Cab Service - Event-Driven Architecture

A comprehensive cab service solution built with microservices architecture on Azure.

## Architecture Overview

This solution consists of 5 microservices:
- **Passenger Service** - Manages passenger profiles and preferences
- **Driver Service** - Handles driver registration and availability
- **Booking Service** - Manages ride bookings and matching
- **Payment Service** - Processes payments and billing
- **Notification Service** - Handles real-time notifications

## Technology Stack

- **.NET 8** - API development
- **Azure App Services** - Hosting
- **Azure Service Bus** - Messaging with pub/sub pattern
- **Azure Functions** - Event processing
- **Azure Cosmos DB** - Data storage
- **YAML** - Infrastructure as Code

## Getting Started

This project is currently being set up. More documentation will be added as development progresses.

## Project Structure

```
/src
  /PassengerService
  /DriverService
  /BookingService
  /PaymentService
  /NotificationService
/infrastructure
  /azure-resources
/docs
  /architecture
```
