# DJ VRSTL Project Requirements

This is the canonical product and business source of truth for the DJ VRSTL web application.
The backend API wire contract is documented separately in `docs/api-contracts.md`.

## 1. Project Overview

DJ VRSTL is a custom, decoupled web application for a DJ business that provides live event services and sells custom DJ booths.

The platform supports four primary business functions:

1. Event booking for DJ services with server-calculated quotes, availability checks, temporary date holds, and a mandatory Mercado Pago deposit.
2. E-commerce for custom DJ booths with catalog browsing, cart checkout, customer data capture, ZIP-based shipping validation, and Mercado Pago payment.
3. Admin operations for products, sales, booking calendar management, and manual date blocking.
4. Landing page and lead generation for event inquiries, service discovery, social proof, and WhatsApp contact.

## 2. Goals and Non-Goals

### Goals

- Let customers quote DJ services without manual back-and-forth.
- Let customers reserve a date by paying a fixed deposit.
- Prevent double-booking through backend-controlled date holds and confirmed calendar blocks.
- Sell physical DJ booths with clear product data, shipping eligibility, and payment flow.
- Give admins enough operational data to contact customers, prepare equipment, manage sales, and manage bookings.
- Keep the public app separate from the blog/CMS to reduce operational and security coupling.

### Non-Goals for Initial Release

- Multi-day event booking.
- Multiple simultaneous bookings on the same calendar date.
- User accounts for customers.
- Complex inventory management.
- Backend-driven CMS for the main marketing site.
- Full CRM functionality beyond lead capture and admin visibility.

## 3. Approved Tech Stack and Infrastructure

### Frontend

- Framework: Next.js with React.
- Language: TypeScript in strict mode.
- Styling: Tailwind CSS.
- UI components: local custom UI components under `components/ui`.
- Package policy: avoid known-vulnerable packages and keep dependencies minimal.

### Backend

- Platform: .NET Core / ASP.NET Web API.
- Language: C#.
- API style: HTTPS JSON API.
- ORM: Entity Framework Core.

### Database

- PostgreSQL.
- Backend must persist historical price snapshots, payment events, customer data, addresses, products, orders, bookings, booking holds, leads, and admin sessions.

### CMS and Blog

- GhostCMS.
- Hosted separately from the main app, for example `blog.domain.com`.
- The main app may link to the blog but must not depend on the CMS for core booking, checkout, or admin operations.

### Hosting

- Ubuntu Linux VPS.
- Docker containers for app services.
- Nginx as reverse proxy.
- HTTPS required in production.

## 4. Global Data Rules

- Currency is MXN.
- Money values are integer MXN pesos, not floats and not centavos.
- Example: `14500` means `14,500 MXN`.
- Calendar dates use `YYYY-MM-DD`.
- Timestamps use ISO 8601 UTC strings.
- Product dimensions are structured numeric centimeters:

```ts
type ProductDimensions = {
  height: number;
  width: number;
  length: number;
};
```

- Customer data for booking and store checkout must include name, email, phone, and one full structured address:

```ts
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
```

- For bookings, the address is the event address.
- For store checkout, the address is the delivery and customer contact address.
- Customer phone, email, and address are PII and must be protected in logs, admin access, and storage.

## 5. Module A: DJ Event Booking

### User Workflow

1. Customer selects a package.
2. Customer selects event date.
3. Customer selects total duration through included hours plus extra hours.
4. Customer selects attendee range.
5. Customer enters full customer and event address data.
6. Frontend requests an authoritative backend quote.
7. Customer checks availability.
8. Customer creates a 15-minute hold.
9. Customer pays the mandatory deposit through Mercado Pago.
10. Backend confirms payment through webhook or verified provider lookup.
11. Backend marks the booking confirmed and blocks the full date.

### Packages

| Package ID | Display Name | Base Price | Includes |
| --- | --- | ---: | --- |
| `essentials` | Esencial | 5500 MXN | 5 hours, DJ, 1 speaker, booth, lights |
| `signature` | Premium | 7500 MXN | 5 hours, pyrotechnics, CO2, 4 speakers, smoke, lasers, robotic heads |

### Pricing Rules

- Included hours: 5.
- Extra hour fee: 1200 MXN per additional hour.
- Attendee ranges:
  - `10-99`: 0 MXN extra.
  - `100-199`: 3000 MXN extra.
  - `200-299`: 5500 MXN extra.
  - `300+`: 7500 MXN extra.
- The backend is the only authority for quote totals.
- The frontend may show live estimates, but backend must recalculate every submitted quote.
- Full event address is required before final quote/hold because address can affect coverage or location fees.

### Deposit and Balance

- Required deposit: fixed 1500 MXN.
- A booking is not confirmed until the deposit payment is approved.
- `remainingBalance = total - depositTotal`.
- Booking confirmation email must show total, deposit paid, remaining balance, date, package, customer data, and event address.

### Calendar and Concurrency

- Creating a hold locks the selected date for 15 minutes.
- If payment is not approved before hold expiration, the date is released.
- Once payment is approved, the full calendar date is blocked.
- The backend must use an atomic transaction or equivalent lock to prevent two users from holding or confirming the same date.
- Availability must treat confirmed bookings, manual blocks, and active holds as unavailable.

### Booking Statuses

Supported statuses:

- `quote`
- `available`
- `held`
- `pending_payment`
- `confirmed`
- `expired`
- `unavailable`

## 6. Module B: E-Commerce Store

### User Workflow

1. Customer browses active DJ booth products.
2. Customer adds products to cart.
3. Customer enters ZIP code.
4. Backend validates shipping zone.
5. Customer enters full customer and delivery address data.
6. Frontend submits checkout.
7. Backend recalculates product totals and shipping.
8. Backend creates Mercado Pago checkout.
9. Customer pays through Mercado Pago.
10. Backend confirms payment through webhook or verified provider lookup.
11. Backend stores order state and sends customer/admin confirmation.

### Product Requirements

Each product must include:

- ID.
- Slug.
- Name.
- Description.
- Dimensions in centimeters: height, width, length.
- Tags.
- Available colors.
- Price in integer MXN pesos.
- Active/inactive flag.
- Product images.
- Optional Amazon redirect URL.

Inactive products must not appear in the public storefront but must remain available to admin and historical sales records.

### Shipping Geofencing

Before checkout, the customer must validate a ZIP code.

Shipping zones:

- Zone 1: within 5 km of Estadio Azteca, free shipping, 0 MXN.
- Zone 2: rest of CDMX, flat shipping fee of 200 MXN.
- Zone 3: outside CDMX, Mercado Pago checkout is blocked and replaced with a WhatsApp shipping quote call-to-action.

The backend returns the final shipping decision. The frontend must display the backend result.

### Price Snapshot Rule

- The backend must never trust frontend product prices or totals.
- On checkout, backend must fetch current products by product ID.
- Backend must validate product active status and selected color.
- Backend must recalculate subtotal, shipping, and total.
- Backend must persist the exact purchased item price in order item snapshots.
- Future product price changes must never alter historical order receipts or sales reporting.

## 7. Module C: Admin Dashboard

### Authentication

- Admin authentication is backend-enforced.
- The backend issues an HTTP-only cookie named `admin_session`.
- Recommended cookie attributes:
  - `HttpOnly`.
  - `Secure` in production.
  - `SameSite=Lax`.
  - Path `/`.
  - Short session TTL with server-side invalidation or rotation.
- Frontend middleware may redirect unauthenticated users for UX, but backend authorization is the source of truth.

### Catalog Management

Admin can:

- Create products.
- Edit products.
- Toggle active/inactive status.
- Manage product dimensions, colors, tags, price, images, and optional Amazon URL.

Product deletion should be implemented as soft delete or inactive status to protect historical sales data.

### Sales Module

Admin can:

- View e-commerce orders.
- Filter sales by status.
- Search by customer or item.
- Filter by specific item/product.
- Sort sales by relevant operational fields when backend support is available.

Sales rows must include operational customer snapshot data:

- Name.
- Email.
- Phone.
- Full address.
- Item name.
- Total.
- Status.
- Created date.

### Bookings Module

Admin can:

- View booking calendar entries.
- View event details.
- View customer contact and event address.
- View total price, deposit paid, and remaining balance.
- Create manual calendar blocks.
- Create or edit manual bookings for off-system clients.
- Delete manual blocks when appropriate.

Manual blocks may omit customer data. Confirmed manual bookings should include customer and event details.

## 8. Module D: Landing Page and Lead Generation

### Hero

- High-quality video or image-led hero.
- DJ VRSTL logo/name must be visible in the first viewport.
- Primary call to action: `Agendar Evento`, linking to the booking module.

### Services Section

Show the services included in the offering:

- Servicio de DJ profesional.
- Cabina de DJ.
- Audio de alta calidad.
- Iluminacion ambiental y efectos.
- Musica personalizada segun el tipo de evento.
- Montaje y operacion profesional.

### Event Types

Show supported event types:

- Eventos privados.
- Eventos en hoteles.
- Eventos corporativos.
- Pool parties.
- Bodas / aniversarios.
- Cumpleanos.

### Social Proof

- Integrate Google Business and Facebook reviews when provider widgets/scripts are available.
- The page must have a polished fallback if external review scripts fail to load.

### Lead Form

General lead capture form fields:

- Name.
- Phone.
- Email.
- Message or event description.

Requirements:

- Frontend validation for basic UX.
- Backend validation and sanitization to prevent XSS/injection.
- Backend anti-abuse controls such as rate limiting and honeypot/CAPTCHA if needed.
- Lead notification to the business owner by email, CRM, Slack, or WhatsApp workflow.

### WhatsApp

- Floating WhatsApp action button must be globally accessible on public pages.
- Out-of-zone bookings or shipping cases should route users to WhatsApp quote flows.

## 9. Payments, Webhooks, and Emails

### Mercado Pago

Mercado Pago is the primary payment gateway.

Required flows:

- Booking deposit checkout for 1500 MXN.
- Store order checkout for full cart total.
- Success, pending, and failure return URLs back to the frontend.
- Webhooks for payment status confirmation.
- Webhook signature verification.
- Idempotent payment event handling.
- Provider payment IDs and webhook event IDs stored for replay protection.

The frontend return pages must not trust query parameters as payment truth. They must call backend status endpoints.

Suggested payment mapping:

- `approved`: booking `confirmed`, order `paid`.
- `pending` or `in_process`: booking `pending_payment`, order `pending`.
- `rejected`, `cancelled`, `refunded`, `charged_back`: booking `expired` or order `failed`, depending on state.

### Transactional Email

Use SMTP, SendGrid, or equivalent transactional email provider.

Required emails:

- Store order receipt.
- Booking confirmation showing total, deposit paid, and balance due.
- Admin alert for new paid booking.
- Admin alert for new paid store order.
- Lead capture alert.

Emails must be sent only after payment approval is verified for payment-dependent flows.

## 10. API and Data Requirements

The backend API must implement the contracts in `docs/api-contracts.md`.

Critical data entities:

- Products.
- Product images.
- Customers.
- Addresses.
- Orders.
- Order items with price snapshots.
- Bookings.
- Booking holds.
- Booking calendar blocks.
- Admin users.
- Admin sessions.
- Payment events.
- Leads.

The API must:

- Support CORS with credentials for the frontend origin.
- Validate every client-submitted total server-side.
- Return customer-facing validation errors where possible.
- Avoid logging sensitive customer address, phone, or email values unnecessarily.

## 11. Analytics and Third-Party Integrations

### Analytics

- Google Analytics is supported through frontend script injection.
- Hotjar is supported through frontend script injection.
- Analytics IDs must be environment-configured.

### Reviews

- Google Business reviews integration.
- Facebook reviews integration.
- Frontend fallback content required when scripts fail or are not configured.

### Blog

- GhostCMS is hosted separately.
- The main app links to the blog URL through environment configuration.

## 12. Security, Privacy, and Reliability

- HTTPS required in production.
- Admin endpoints must enforce authentication server-side.
- Rate limit login, lead capture, checkout creation, and booking hold creation.
- Customer data is PII and must be protected.
- Webhooks must be signature-verified and idempotent.
- Booking holds must expire automatically.
- Pending payments should be reconciled with Mercado Pago.
- Failed transactional emails should be retried or surfaced to admin/ops.
- Product images should be served through stable object storage or CDN.

## 13. Environment Configuration

Frontend public environment variables:

```env
NEXT_PUBLIC_API_BASE_URL=https://api.example.com
NEXT_PUBLIC_GA_ID=
NEXT_PUBLIC_HOTJAR_ID=
NEXT_PUBLIC_WHATSAPP_URL=https://wa.me/5215555555555
NEXT_PUBLIC_BLOG_URL=https://blog.djvrstl.com
NEXT_PUBLIC_REVIEWS_SCRIPT_URL=
```

Backend/server-only configuration:

- PostgreSQL connection string.
- Mercado Pago access token.
- Mercado Pago webhook signing secret.
- Admin auth/session secrets.
- SMTP/SendGrid credentials.
- Object storage/CDN credentials, if used.

## 14. Acceptance Criteria

### Booking

- A customer can generate a backend-confirmed quote using package, date, duration, attendee range, and full address.
- Quote response includes total, fixed 1500 MXN deposit, and remaining balance.
- A customer can create a 15-minute hold for an available date.
- A paid deposit confirms the booking and blocks the full date.
- An unpaid expired hold releases the date.

### Store

- Public storefront shows only active products.
- Product dimensions render as structured centimeter values.
- Customer cannot checkout until shipping ZIP is validated and customer/address data is complete.
- Zone 1 shipping is 0 MXN.
- Zone 2 shipping is 200 MXN.
- Zone 3 blocks checkout and offers WhatsApp quote path.
- Paid orders retain historical item price snapshots.

### Admin

- Unauthenticated users cannot access protected admin APIs.
- Admin can manage products without deleting historical sales data.
- Admin can view sales with customer and address data.
- Admin can view booking balances and customer/event details.
- Admin can manually block dates.

### Lead Generation

- Lead form validates required fields.
- Backend sanitizes and stores leads.
- Business owner receives a lead notification.
- WhatsApp CTA is globally available on public pages.

## 15. Known Follow-Up Alignment Items

- Update any frontend or mock fallback values that still use pre-source-of-truth shipping fees.
- Add backend implementation for `POST /leads`.
- Add backend sorting support for admin sales when the sales module moves beyond the current UI filters.
- Decide whether distance-based booking location fees are needed beyond the current `locationFee` contract field.
