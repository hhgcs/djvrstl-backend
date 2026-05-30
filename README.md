# DJVrstl System Design

## High-Level Architecture Diagram

```mermaid
flowchart TB
      subgraph Users["Users"]
          C1["Event Clients"]
          C2["Booth Buyers"]
          A1["Admin Staff"]
      end

      subgraph Frontend["Frontend - Next.js"]
          LP["Landing + Lead Form"]
          BEUI["Booking Flow UI"]
          STO["Storefront + Cart UI"]
          PAYUI["Payment Status Pages"]
          ADLOGIN["Admin Login UI"]
          ADUI["Admin Dashboard UI"]
          MW["Next Middleware<br/>/admin route guard"]
          WS["Global WhatsApp FAB"]
      end

      subgraph Edge["Edge & Delivery"]
          NGINX["Nginx Reverse Proxy"]
          DOCKER["Docker Containers / VPS Runtime"]
      end

      subgraph Backend["Backend - ASP.NET Core Web API"]
          AUTH["Cookie Auth<br/>HTTP-only admin_session"]
          LEADS["Lead Service<br/>POST /leads"]
          BOOK["Booking Service<br/>Pricing + Quote + Availability + Hold"]
          SHOP["Catalog + Shipping Service"]
          ORD["Store Checkout + Order Status"]
          ADMIN["Admin Service<br/>Products + Sales + Bookings"]
          PAY["Payment Provider Abstraction<br/>Fake / Mercado Pago"]
          WEBHOOK["Mercado Pago Webhook Handler"]
          NOTIF["Notification Service<br/>Local / SMTP-ready"]
          JOBS["Hosted Jobs<br/>Seed Data + Initial Admin + Hold Expiration"]
      end

      subgraph Data["Data Layer"]
          PG[("PostgreSQL + EF Core")]
          T1["products"]
          T2["orders + order_items"]
          T3["bookings + booking_holds"]
          T4["booking_calendar_blocks"]
          T5["admin_users + admin_sessions"]
          T6["payment_events"]
          T7["leads"]
      end

      subgraph External["External Services"]
          MP["Mercado Pago API"]
          SMTP["SMTP / SendGrid"]
          GA["Google Analytics"]
          HJ["Hotjar"]
          WA["WhatsApp"]
          CMS["Ghost / WordPress<br/>blog.domain.com"]
      end

      C1 --> LP
      C1 --> BEUI
      C2 --> STO
      A1 --> ADLOGIN
      A1 --> ADUI

      LP --> GA
      LP --> HJ
      BEUI --> GA
      STO --> GA
      WS --> WA
      LP -. "Content link" .-> CMS

      LP --> NGINX
      BEUI --> NGINX
      STO --> NGINX
      PAYUI --> NGINX
      ADLOGIN --> MW
      ADUI --> MW
      MW --> NGINX

      NGINX --> DOCKER
      DOCKER --> LEADS
      DOCKER --> BOOK
      DOCKER --> SHOP
      DOCKER --> ORD
      DOCKER --> AUTH
      DOCKER --> ADMIN
      DOCKER --> PAY
      DOCKER --> WEBHOOK
      DOCKER --> NOTIF
      DOCKER --> JOBS

      ADLOGIN -->|"POST /admin/login"| AUTH
      ADUI -->|"credentials: include"| ADMIN

      BEUI -->|"GET /booking/pricing-config"| BOOK
      BEUI -->|"POST /booking/quote"| BOOK
      BEUI -->|"GET /booking/availability"| BOOK
      BEUI -->|"POST /booking/hold"| BOOK

      STO -->|"GET /catalog/products"| SHOP
      STO -->|"POST /shipping/validate-zip"| SHOP
      STO -->|"POST /store/checkout"| ORD

      PAYUI -->|"GET /booking/status/:id"| BOOK
      PAYUI -->|"GET /store/orders/:id"| ORD

      PAY --> MP
      BOOK --> PAY
      ORD --> PAY
      MP --> WEBHOOK
      WEBHOOK --> BOOK
      WEBHOOK --> ORD

      NOTIF --> SMTP

      LEADS --> PG
      BOOK --> PG
      SHOP --> PG
      ORD --> PG
      AUTH --> PG
      ADMIN --> PG
      WEBHOOK --> PG
      JOBS --> PG

      PG --> T1
      PG --> T2
      PG --> T3
      PG --> T4
      PG --> T5
      PG --> T6
      PG --> T7
```

## Key Business Rule Flows

1. **Booking lock flow**: checkout attempt locks selected date for 15 minutes; webhook-confirmed deposit blocks whole day.
2. **Geofenced shipping flow**: ZIP code determines Zone 1 (free), Zone 2 (flat fee), or Zone 3 (disable checkout and route to WhatsApp quote).
3. **Order integrity flow**: order line items store immutable purchase-time price snapshots.
