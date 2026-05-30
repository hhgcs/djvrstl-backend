# Ghost CMS

Ghost runs separately from the booking, store, and admin systems. The main
frontend links to the blog through `NEXT_PUBLIC_BLOG_URL`; it does not depend on
Ghost to render core app pages.

## Local Run

From the repository root:

```powershell
docker compose -f djvrstl-solution\compose.yaml up -d ghost ghost-mysql
```

Open:

- Blog: `http://localhost:2368`
- Admin setup: `http://localhost:2368/ghost`

## Production Setup

1. Copy `djvrstl-solution/.env.example` to `djvrstl-solution/.env`.
2. Set strong MySQL passwords and `GHOST_URL=https://blog.djvrstl.com`.
3. Start/recreate the Ghost services:

```powershell
docker compose --env-file djvrstl-solution\.env -f djvrstl-solution\compose.yaml up -d ghost ghost-mysql
```

4. Route `blog.djvrstl.com` to port `2368` through the VPS reverse proxy.
   `nginx/blog.djvrstl.com.conf.example` is a starting point.
5. Issue TLS for `blog.djvrstl.com`.
6. In the frontend deployment, set:

```env
NEXT_PUBLIC_BLOG_URL=https://blog.djvrstl.com
```

## Persistence

The compose file creates two named volumes:

- `djvrstl-ghost-content`: Ghost images, themes, settings, and content files.
- `djvrstl-ghost-mysql-data`: Ghost MySQL data.

Back up both volumes before server migrations.
