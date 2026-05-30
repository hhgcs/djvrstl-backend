# DJ VRSTL Backend API Contracts

This document captures the backend surface the frontend will implement against.
The current frontend call map lives in `lib/api.ts`; shared frontend shapes live in `lib/types.ts`.

## Runtime Conventions

- Base URL: frontend reads `NEXT_PUBLIC_API_BASE_URL` and calls `${baseUrl}${path}`.
- Transport: HTTPS JSON API.
- Credentials: frontend sends `credentials: "include"` on every request.
- Request headers: `Content-Type: application/json`.
- Currency: all money values are integer MXN pesos, not floats and not centavos. Example: `14500` means `14,500 MXN`.
- Dates:
  - Calendar dates use `YYYY-MM-DD`.
  - Timestamps use ISO 8601 UTC strings.
- Caching: frontend requests use `cache: "no-store"`; backend should return fresh state for booking, order, and admin routes.
- Error response recommendation:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Human readable message.",
    "fields": {
      "email": "Invalid email."
    }
  }
}
```

Use appropriate HTTP status codes: `400` validation, `401` unauthenticated, `403` unauthorized, `404` missing resource, `409` booking conflict or expired hold, `422` business rule failure, `500` unexpected error.

## Shared Types

```ts
type BookingPackageId = "essentials" | "signature";

type AttendeeRange = "10-99" | "100-199" | "200-299" | "300+";

type FullAddress = {
  street: string;
  exteriorNumber: string;
  interiorNumber?: string;
  neighborhood: string;
  city: string;
  state: string;
  postalCode: string;
  country: "MX";
  references?: string;
};

type CustomerData = {
  name: string;
  email: string;
  phone: string;
  address: FullAddress;
};

type ProductDimensions = {
  height: number;
  width: number;
  length: number;
};

type BookingPricingConfig = {
  currency: "MXN";
  includedHours: number;
  extraHourFee: number;
  minimumDeposit: number;
  packageBasePrices: Record<BookingPackageId, number>;
  packageNames: Record<BookingPackageId, string>;
  packageIncludes?: Record<BookingPackageId, string[]>;
  attendeeRangeFees: Record<AttendeeRange, number>;
  estimateNote: string;
};

type BookingQuote = {
  packageId: BookingPackageId;
  packageName: string;
  date: string;
  durationHours: number;
  attendeeRange: AttendeeRange;
  address: FullAddress;
  subtotal: number;
  attendeeFee: number;
  extraHoursFee: number;
  locationFee: number;
  total: number;
  depositTotal: number;
  remainingBalance: number;
  currency: "MXN";
  note: string;
};

type BookingStatus =
  | "quote"
  | "available"
  | "held"
  | "pending_payment"
  | "confirmed"
  | "expired"
  | "unavailable";

type BookingCalendarEntry = {
  id: string;
  date: string;
  label: string;
  status: "confirmed" | "manual_block" | "hold";
  eventType?: BookingPackageId;
  durationHours?: number;
  attendeeRange?: AttendeeRange;
  customer?: CustomerData;
  total?: number;
  depositTotal?: number;
  remainingBalance?: number;
  notes?: string;
};

type Product = {
  id: string;
  slug: string;
  name: string;
  description: string;
  dimensions: ProductDimensions;
  tags: string[];
  colors: string[];
  price: number;
  active: boolean;
  images: string[];
  amazonUrl?: string;
};

type OrderStatus = "draft" | "pending" | "paid" | "failed" | "quoted";
```

Address contract:

- `CustomerData.address` is the single required address for v1.
- For bookings, it is the event address used to calculate the total and coordinate the event.
- For store checkout, it is the delivery/customer contact address used for checkout, confirmation email, fulfillment, and customer follow-up.
- `ProductDimensions` values are numeric centimeters.

## Public Booking API

### `GET /booking/pricing-config`

Returns current pricing used to render packages and calculate live estimates.

Response `200`:

```json
{
  "currency": "MXN",
  "includedHours": 5,
  "extraHourFee": 1200,
  "minimumDeposit": 1500,
  "packageBasePrices": {
    "essentials": 5500,
    "signature": 7500
  },
  "packageNames": {
    "essentials": "Esencial",
    "signature": "Premium"
  },
  "packageIncludes": {
    "essentials": ["5hrs", "Bocinas", "Cabina", "Luces"],
    "signature": ["5hrs", "4 Bocinas", "Pirotecnia", "Maquinas de CO2"]
  },
  "attendeeRangeFees": {
    "10-99": 0,
    "100-199": 3000,
    "200-299": 5500,
    "300+": 7500
  },
  "estimateNote": "Incluye 5 horas base. El total final se confirma desde backend."
}
```

### `POST /booking/quote`

Calculates the authoritative quote for a date/package/duration/address.

Request:

```json
{
  "packageId": "signature",
  "date": "2026-06-21",
  "durationHours": 6,
  "attendeeRange": "100-199",
  "address": {
    "street": "Av. Alvaro Obregon",
    "exteriorNumber": "120",
    "interiorNumber": "4B",
    "neighborhood": "Roma Norte",
    "city": "Ciudad de Mexico",
    "state": "CDMX",
    "postalCode": "06700",
    "country": "MX",
    "references": "Entrada por la calle lateral"
  }
}
```

Response `200`: `BookingQuote`.

Example response:

```json
{
  "packageId": "signature",
  "packageName": "Premium",
  "date": "2026-06-21",
  "durationHours": 6,
  "attendeeRange": "100-199",
  "address": {
    "street": "Av. Alvaro Obregon",
    "exteriorNumber": "120",
    "interiorNumber": "4B",
    "neighborhood": "Roma Norte",
    "city": "Ciudad de Mexico",
    "state": "CDMX",
    "postalCode": "06700",
    "country": "MX",
    "references": "Entrada por la calle lateral"
  },
  "subtotal": 7500,
  "attendeeFee": 3000,
  "extraHoursFee": 1200,
  "locationFee": 0,
  "total": 11700,
  "depositTotal": 1500,
  "remainingBalance": 10200,
  "currency": "MXN",
  "note": "Incluye 5 horas base. El anticipo minimo para reservar es 1500 MXN."
}
```

Business rules:

- Reject unknown package IDs and attendee ranges.
- Reject dates in the past.
- Require a complete `address`; backend uses it to calculate location fees and validate service coverage.
- Return values calculated from server-side pricing, not frontend totals.
- `depositTotal` must be at least `1500` and `remainingBalance` must equal `total - depositTotal`.

### `GET /booking/availability?date=YYYY-MM-DD`

Checks whether a date can still be held.

Response `200`:

```json
{
  "date": "2026-06-21",
  "available": true
}
```

Unavailable response:

```json
{
  "date": "2026-06-21",
  "available": false,
  "reason": "Fecha bloqueada por evento confirmado."
}
```

Availability must treat `confirmed`, active `hold`, and `manual_block` calendar entries as unavailable.

### `POST /booking/hold`

Creates a temporary booking hold and starts payment checkout for the deposit.

Request:

```json
{
  "quote": {
    "packageId": "signature",
    "packageName": "Premium",
    "date": "2026-06-21",
    "durationHours": 6,
    "attendeeRange": "100-199",
    "address": {
      "street": "Av. Alvaro Obregon",
      "exteriorNumber": "120",
      "interiorNumber": "4B",
      "neighborhood": "Roma Norte",
      "city": "Ciudad de Mexico",
      "state": "CDMX",
      "postalCode": "06700",
      "country": "MX",
      "references": "Entrada por la calle lateral"
    },
    "subtotal": 7500,
    "attendeeFee": 3000,
    "extraHoursFee": 1200,
    "locationFee": 0,
    "total": 11700,
    "depositTotal": 1500,
    "remainingBalance": 10200,
    "currency": "MXN",
    "note": "Incluye 5 horas base."
  },
  "customer": {
    "name": "Mariana Ortiz",
    "email": "mariana@example.com",
    "phone": "5512345678",
    "address": {
      "street": "Av. Alvaro Obregon",
      "exteriorNumber": "120",
      "interiorNumber": "4B",
      "neighborhood": "Roma Norte",
      "city": "Ciudad de Mexico",
      "state": "CDMX",
      "postalCode": "06700",
      "country": "MX",
      "references": "Entrada por la calle lateral"
    }
  }
}
```

Response `200`:

```json
{
  "holdId": "hold_abc123",
  "expiresAt": "2026-06-01T18:15:00.000Z",
  "checkoutUrl": "https://www.mercadopago.com.mx/checkout/v1/redirect?pref_id=..."
}
```

Business rules:

- Recalculate and verify the submitted quote server-side.
- Persist the customer snapshot with the hold/booking so the business can contact the customer and prepare for the event.
- Create checkout for `depositTotal`, not the full event total.
- Hold duration expected by UI: 15 minutes.
- Use an atomic lock/transaction so two users cannot hold the same event date.
- Expire unreconciled holds automatically.
- The current UI stores `checkoutUrl` but does not redirect yet; backend should still return it.

### `GET /booking/status/:id`

Returns authoritative booking/payment status for payment return pages.

Response `200`:

```json
{
  "bookingId": "booking_abc123",
  "status": "confirmed",
  "expiresAt": "2026-06-01T18:15:00.000Z",
  "eventDate": "2026-06-21",
  "customer": {
    "name": "Mariana Ortiz",
    "email": "mariana@example.com",
    "phone": "5512345678",
    "address": {
      "street": "Av. Alvaro Obregon",
      "exteriorNumber": "120",
      "interiorNumber": "4B",
      "neighborhood": "Roma Norte",
      "city": "Ciudad de Mexico",
      "state": "CDMX",
      "postalCode": "06700",
      "country": "MX",
      "references": "Entrada por la calle lateral"
    }
  },
  "depositTotal": 1500,
  "remainingBalance": 10200,
  "total": 11700,
  "currency": "MXN"
}
```

For `held` or `pending_payment`, include `expiresAt` when available.

## Store API

### `GET /catalog/products`

Public catalog endpoint. Return active products only for the public storefront.

Response `200`:

```json
[
  {
    "id": "booth-nova",
    "slug": "cabina-nova",
    "name": "Cabina Nova",
    "description": "Cabina frontal con acabado brillante.",
    "dimensions": {
      "height": 180,
      "width": 70,
      "length": 110
    },
    "tags": ["premium", "iluminacion"],
    "colors": ["Negro", "Blanco"],
    "price": 14500,
    "active": true,
    "images": ["https://cdn.example.com/products/cabina-nova.jpg"],
    "amazonUrl": "https://amazon.com/..."
  }
]
```

### `POST /shipping/validate-zip`

Validates shipping zone and checkout eligibility.

Request:

```json
{
  "zipCode": "06700"
}
```

Response `200`:

```json
{
  "zipCode": "06700",
  "zone": 2,
  "label": "Zona 2",
  "shippingFee": 850,
  "checkoutAllowed": true,
  "message": "Se agrego una tarifa fija de envio confirmada por backend."
}
```

If the zone requires manual quotation, return `checkoutAllowed: false` and a customer-facing `message`.

### `POST /store/checkout`

Creates an order and starts payment checkout. The request must include customer data so the backend can contact the customer after checkout, send the confirmation email, and prepare fulfillment.

Request:

```json
{
  "customer": {
    "name": "Mariana Ortiz",
    "email": "mariana@example.com",
    "phone": "5512345678",
    "address": {
      "street": "Av. Alvaro Obregon",
      "exteriorNumber": "120",
      "interiorNumber": "4B",
      "neighborhood": "Roma Norte",
      "city": "Ciudad de Mexico",
      "state": "CDMX",
      "postalCode": "06700",
      "country": "MX",
      "references": "Entrada por la calle lateral"
    }
  },
  "orderId": "draft",
  "status": "draft",
  "items": [
    {
      "productId": "booth-nova",
      "color": "Negro",
      "quantity": 1,
      "product": {
        "id": "booth-nova",
        "slug": "cabina-nova",
        "name": "Cabina Nova",
        "description": "Cabina frontal con acabado brillante.",
        "dimensions": {
          "height": 180,
          "width": 70,
          "length": 110
        },
        "tags": ["premium"],
        "colors": ["Negro", "Blanco"],
        "price": 14500,
        "active": true,
        "images": ["https://cdn.example.com/products/cabina-nova.jpg"]
      }
    }
  ],
  "summary": {
    "subtotal": 14500,
    "shippingFee": 850,
    "total": 15350,
    "currency": "MXN"
  }
}
```

Response `200`:

```json
{
  "orderId": "order_abc123",
  "checkoutUrl": "https://www.mercadopago.com.mx/checkout/v1/redirect?pref_id=..."
}
```

Business rules:

- Treat frontend product and summary fields as a client snapshot only.
- Re-fetch products by `productId`, validate active status and color, recalculate subtotal/shipping/total.
- Validate `customer.address.postalCode` against the shipping zone result.
- Reject checkout when the validated shipping zone has `checkoutAllowed: false`.
- Persist the customer snapshot and calculated order summary as the frozen state used by payment status pages and confirmation email.
- Send the confirmation email only after payment is confirmed by webhook or verified provider lookup.

### `GET /store/orders/:id`

Returns order status for payment return pages and customer operations.

Response `200`:

```json
{
  "orderId": "order_abc123",
  "status": "paid",
  "customer": {
    "name": "Mariana Ortiz",
    "email": "mariana@example.com",
    "phone": "5512345678",
    "address": {
      "street": "Av. Alvaro Obregon",
      "exteriorNumber": "120",
      "interiorNumber": "4B",
      "neighborhood": "Roma Norte",
      "city": "Ciudad de Mexico",
      "state": "CDMX",
      "postalCode": "06700",
      "country": "MX",
      "references": "Entrada por la calle lateral"
    }
  },
  "items": [],
  "summary": {
    "subtotal": 14500,
    "shippingFee": 850,
    "total": 15350,
    "currency": "MXN"
  }
}
```

## Admin API

Admin endpoints must require an HTTP-only cookie named `admin_session`. The frontend middleware only checks that the cookie exists when `NEXT_PUBLIC_API_BASE_URL` is configured; the backend remains the source of truth for authorization.

Recommended cookie attributes:

- `HttpOnly`
- `Secure` in production
- `SameSite=Lax`
- Path `/`
- Short session TTL with rotation or server-side invalidation

### `GET /admin/session`

Response `200` when a valid session exists:

```json
{
  "authenticated": true,
  "name": "Demo Admin",
  "role": "manager"
}
```

Response `200` for no valid session may be:

```json
{
  "authenticated": false
}
```

Alternatively return `401`; if doing so, update frontend handling as needed.

### `POST /admin/login`

Request:

```json
{
  "email": "admin@example.com",
  "password": "secret"
}
```

Response `200` plus `Set-Cookie: admin_session=...`:

```json
{
  "authenticated": true,
  "name": "Demo Admin",
  "role": "manager"
}
```

Invalid credentials should return `401` or `{ "authenticated": false }`. Prefer `401` for production, with frontend error handling adjusted if needed.

### `POST /admin/logout`

Clears `admin_session`.

Response: `204 No Content`.

### `GET /admin/products`

Returns all products, including inactive products, for catalog management.

Response `200`: `Product[]`.

### `POST /admin/products`

Creates or updates a product. The frontend currently posts one endpoint for both operations.

Request:

```json
{
  "id": "booth-nova",
  "slug": "cabina-nova",
  "name": "Cabina Nova",
  "description": "Cabina frontal con acabado brillante.",
  "dimensions": {
    "height": 180,
    "width": 70,
    "length": 110
  },
  "tags": [],
  "colors": ["Negro"],
  "price": 14500,
  "active": true,
  "images": ["https://cdn.example.com/products/cabina-nova.jpg"],
  "amazonUrl": "https://amazon.com/..."
}
```

Response `200`: saved `Product`.

Backend should generate stable `id` and `slug` when omitted, validate unique slugs, validate positive dimensions in centimeters, and reject invalid image URLs.

### `GET /admin/sales`

Query params:

- `productId?: string`
- `status?: "draft" | "pending" | "paid" | "failed" | "quoted"`
- `search?: string`

Admin sales should include an operational customer snapshot. The admin needs this data to contact the customer, prepare equipment, coordinate delivery/event logistics, and investigate payments without opening a separate detail screen.

Response `200`:

```json
[
  {
    "id": "sale-001",
    "customer": {
      "name": "Mariana Ortiz",
      "email": "mariana@example.com",
      "phone": "5512345678",
      "address": {
        "street": "Av. Alvaro Obregon",
        "exteriorNumber": "120",
        "interiorNumber": "4B",
        "neighborhood": "Roma Norte",
        "city": "Ciudad de Mexico",
        "state": "CDMX",
        "postalCode": "06700",
        "country": "MX",
        "references": "Entrada por la calle lateral"
      }
    },
    "itemName": "Cabina Nova",
    "total": 14500,
    "status": "paid",
    "createdAt": "2026-05-12"
  }
]
```

### `GET /admin/bookings`

Returns calendar entries for the admin panel and booking date blocking.

Response `200`: `BookingCalendarEntry[]`.

### `POST /admin/bookings/manual`

Creates or updates a manual calendar entry.

Request:

```json
{
  "id": "calendar-1",
  "date": "2026-06-21",
  "label": "Mariana Ortiz",
  "status": "confirmed",
  "eventType": "signature",
  "durationHours": 6,
  "attendeeRange": "100-199",
  "customer": {
    "name": "Mariana Ortiz",
    "email": "mariana@example.com",
    "phone": "5512345678",
    "address": {
      "street": "Av. Alvaro Obregon",
      "exteriorNumber": "120",
      "interiorNumber": "4B",
      "neighborhood": "Roma Norte",
      "city": "Ciudad de Mexico",
      "state": "CDMX",
      "postalCode": "06700",
      "country": "MX",
      "references": "Entrada por la calle lateral"
    }
  },
  "total": 11700,
  "depositTotal": 1500,
  "remainingBalance": 10200,
  "notes": "Evento confirmado desde admin."
}
```

Response `200`: saved `BookingCalendarEntry`.

Backend should distinguish manual blocks from confirmed events:

- If only `date`/`notes` are provided, default status can be `manual_block`.
- If customer/event/payment details are provided, default status can be `confirmed`.
- Do not allow overlapping confirmed/manual/active-hold entries for the same date unless business rules explicitly permit it.

### `DELETE /admin/bookings/:id`

Deletes a manual booking/calendar entry or releases a manual block.

Response: `204 No Content`.

## Lead Capture API Gap

The public landing page currently validates a lead form locally and shows a toast. Backend should add this endpoint before production:

### `POST /leads`

Request:

```json
{
  "name": "Mariana Ortiz",
  "email": "mariana@example.com",
  "phone": "5512345678",
  "message": "Boda el 21 de junio en CDMX."
}
```

Response `201`:

```json
{
  "leadId": "lead_abc123",
  "status": "received"
}
```

Requirements:

- Server-side validation and sanitization.
- Anti-spam controls: rate limit by IP/email, honeypot or CAPTCHA if abuse appears.
- Notification path to the business owner, for example email, CRM, Slack, or WhatsApp workflow.

## Payments and Webhooks

The frontend has return pages at:

- `/payments/booking/:status?booking=:bookingId`
- `/payments/store/:status?order=:orderId`

The return page does not trust query params for state; it calls backend status endpoints. Backend must:

- Create checkout preferences for booking deposits and store orders.
- Configure payment success, pending, and failure return URLs to the frontend.
- Receive provider webhooks, verify signatures, and update booking/order state idempotently.
- Store provider payment IDs and raw webhook event IDs for replay protection.
- Confirm bookings only after payment is approved by webhook or verified provider lookup.
- Mark expired holds when no payment arrives before `expiresAt`.
- Send booking and order confirmation emails only after payment approval is verified.
- Include customer contact and address details in internal notifications.

Suggested Mercado Pago mapping:

- `approved` -> booking `confirmed`, order `paid`
- `pending` or `in_process` -> `pending_payment` / `pending`
- `rejected`, `cancelled`, `refunded`, `charged_back` -> `expired` or `failed` depending on domain state

## Infrastructure Checklist

- Database tables/collections:
  - `products`
  - `orders`
  - `order_items`
  - `bookings`
  - `booking_holds`
  - `booking_calendar_blocks`
  - `customers`
  - `addresses`
  - `admin_users`
  - `admin_sessions`
  - `payment_events`
  - `leads`
- Background jobs:
  - Expire booking holds after 15 minutes.
  - Reconcile stale pending payments with payment provider.
  - Optional lead notification retry queue.
  - Retry failed confirmation/internal notification emails.
- Object storage/CDN:
  - Product images should be hosted on a stable CDN or object storage bucket.
  - The frontend currently accepts absolute image URLs.
- Security:
  - CORS must allow the frontend origin and credentials.
  - Admin routes must enforce auth server-side; middleware cookie presence is only UX gating.
  - Rate limit login, lead capture, checkout creation, and hold creation.
  - Validate all client-supplied totals server-side.
  - Treat customer phone, email, and address as PII; limit logging and restrict admin access.
- Observability:
  - Log request IDs, checkout preference IDs, payment IDs, booking IDs, order IDs, and webhook event IDs.
  - Alert on webhook failures, email failures, and high payment reconciliation drift.

## Frontend Environment Variables

```env
NEXT_PUBLIC_API_BASE_URL=https://api.example.com
NEXT_PUBLIC_GA_ID=
NEXT_PUBLIC_HOTJAR_ID=
NEXT_PUBLIC_WHATSAPP_URL=https://wa.me/5215555555555
NEXT_PUBLIC_BLOG_URL=https://blog.djvrstl.com
NEXT_PUBLIC_REVIEWS_SCRIPT_URL=
```

Backend/deployment must also provide secrets server-side, not exposed to the frontend:

- Payment provider access token and webhook signing secret.
- Admin password hashing/pepper or identity provider config.
- Database URL.
- Email/CRM/notification credentials for leads, booking confirmations, and order confirmations.

## Implementation Priority

1. Shared customer/address persistence, admin auth/session endpoints, and CORS with credentials.
2. Products/catalog endpoints with structured dimensions.
3. Booking pricing, quote, availability, hold, and status endpoints with the new attendee tiers, full address, and 1500 MXN minimum deposit.
4. Store shipping validation, checkout creation with customer data, order status, confirmation emails, and payment webhooks.
5. Admin sales/bookings CRUD with operational customer snapshots.
6. Lead capture endpoint and notifications.
