# DJVrstl System Design

## High-Level Architecture Diagram

```mermaid
flowchart TB
    subgraph Users[Users]
        C1[Event Clients]
        C2[Booth Buyers]
        A1[Admin Staff]
    end

    subgraph Frontend[Frontend - React]
        LP[Landing & Lead Gen]
        BEUI[Booking Engine UI]
        STO[Storefront UI]
        ADUI[Admin Dashboard UI]
        WS[Global WhatsApp FAB]
    end

    subgraph Edge[Edge & Delivery]
        NGINX[Nginx Reverse Proxy]
        DOCKER[Docker Containers on Ubuntu VPS]
    end

    subgraph Backend[Backend - ASP.NET Core Web API]
        AUTH[JWT Auth]
        BOOK[Booking Service\nPricing + Calendar Lock]
        SHOP[E-Commerce Service\nCatalog + Cart + Shipping Zones]
        ORD[Order Service\nPrice Snapshot Integrity]
        ADMIN[Admin Service]
        WEBHOOK[Mercado Pago Webhook Handler]
        NOTIF[Email Notification Service\nSMTP/SendGrid]
    end

    subgraph Data[Data Layer]
        PG[(PostgreSQL + EF Core)]
    end

    subgraph External[External Services]
        MP[Mercado Pago API]
        SMTP[SMTP/SendGrid]
        GA[Google Analytics]
        HJ[Hotjar]
        WA[WhatsApp]
        CMS[Ghost/WordPress\nblog.domain.com]
    end

    C1 --> LP
    C1 --> BEUI
    C2 --> STO
    A1 --> ADUI

    LP --> GA
    LP --> HJ
    BEUI --> GA
    STO --> GA

    LP --> NGINX
    BEUI --> NGINX
    STO --> NGINX
    ADUI --> NGINX
    WS --> WA

    NGINX --> DOCKER
    DOCKER --> AUTH
    DOCKER --> BOOK
    DOCKER --> SHOP
    DOCKER --> ORD
    DOCKER --> ADMIN
    DOCKER --> WEBHOOK
    DOCKER --> NOTIF

    BOOK --> PG
    SHOP --> PG
    ORD --> PG
    ADMIN --> PG
    AUTH --> PG

    BEUI --> MP
    STO --> MP
    MP --> WEBHOOK
    WEBHOOK --> BOOK
    WEBHOOK --> ORD

    NOTIF --> SMTP

    LP -. Content Link .-> CMS
```

## Key Business Rule Flows

1. **Booking lock flow**: checkout attempt locks selected date for 15 minutes; webhook-confirmed deposit blocks whole day.
2. **Geofenced shipping flow**: ZIP code determines Zone 1 (free), Zone 2 (flat fee), or Zone 3 (disable checkout and route to WhatsApp quote).
3. **Order integrity flow**: order line items store immutable purchase-time price snapshots.
