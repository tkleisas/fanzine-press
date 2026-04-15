# Fanzine Press

> A web app for building newspaper-style fanzines, PDF-exportable and
> deeply ink-stained, for football fan publications and other
> lovingly-produced small-circulation delusions.
>
> *Made with AI-Sloptronic™ technology.* Warnings: may contain traces
> of confidently-wrong whitespace, emergent politeness, and stray `<div>`s.

Fanzine Press lets an editor cobble together an issue — title page,
articles, photos, ads, colophon — in a Greek-language Razor Pages UI,
then prints it to a PDF that actually looks like something a minor
football club from the 1970s would have mimeographed in a back office.

It's what happens when a human Product Manager hands specifications
to a Large Language Model and the LLM, rather than suffering an
existential crisis, just ships the thing.

---

## Features

### Issues — the fundamental unit of regret

- Create, edit, publish issues (Draft / Published status).
- Title, issue number, date, motto, header/footer text, colophon.
- Upload a title image. Yes, it goes in the database, because
  filesystems are for people who have backups.
- Per-issue ownership — editors see their own, admins see everything,
  nobody sees what they want.
- One built-in template ("classic"). The architecture swears it
  supports more. The architecture has been known to lie.

### Articles

- Title, subtitle, author, body, column span (1–3), order.
- Plain-text body. No WYSIWYG editor, because a WYSIWYG editor would
  have required convincing the AI not to reinvent TinyMCE from scratch
  four times in a row. We lost that battle.
- Reordering via an order field, because drag-and-drop is a rabbit
  hole that ends in JavaScript frameworks and heartbreak.

### Photos

- Upload images; stored as BLOBs in the SQLite database. This is
  either elegant or a war crime, depending on whom you ask.
- Caption, credit.
- Vintage film effect (CSS sepia/grayscale) per photo, so every issue
  can look like it was scanned from a shoebox.
- Authenticated image API at `/api/images/photo/{id}` with role-based
  access — yes, even the pictures need a login.

### Ads

- Quarter / Half / Full page.
- Text or image ads.
- Ordering. That's it. It's a zine, not Google.

### Layout & Typography

- Three-column newspaper layout with column rules, Georgia serif,
  double-line borders, `#fdf6e3` background, UPPERCASE HEADLINES,
  and all the other little tells that say "yes, this was
  deliberate, no, it is not 2003 anymore".
- A4 page size, 15mm margins, print backgrounds on.
- Colophon section: publication name, editors, contributors,
  contact, license, extra prose.

### PDF Export

- Renders the Preview page through a *real* headless Chrome baked
  into the Docker image (not snap-shim Chromium, we tried, it cried).
- Auth cookies are forwarded to Puppeteer so it can actually fetch
  the protected pages. Machines logging in to pages written for
  humans to show to other humans — the circle of life.
- Output: an A4 PDF that you can print, email, or frame.

### Authentication & Admin

- ASP.NET Core Identity. Email + password, 8+ chars, digit + lowercase.
- Remember-me, 14-day sliding sessions, account lockout.
- Roles: `Admin` and `Editor`.
- Admin panel: list users, create users, toggle roles, delete users.
  Cannot demote or delete yourself — even the software knows better
  than to let you rage-quit at 2am.
- No self-registration, no password reset flow. This is deliberate;
  also it is a feature the AI never got around to building.

### Versioning

- Footer shows `v{SemVer} · {short-git-hash}` so when things break
  you at least know *which* version is broken.
- Stamped into `AssemblyInformationalVersion` at build time via
  `git describe --tags` → Docker `--build-arg` → MSBuild
  `/p:Version` and `/p:SourceRevisionId`. No `.git` directory inside
  the container, no NuGet plugins, no mysteries at runtime.

---

## Tech Stack

- **Backend:** ASP.NET Core 10 Razor Pages, EF Core, SQLite
- **Frontend:** htmx + vanilla JS + Bootstrap (no framework-of-the-month)
- **PDF:** PuppeteerSharp driving system Google Chrome
- **Images:** SixLabors.ImageSharp for the thumbnails that actually
  need resizing; BLOBs everywhere else
- **Auth:** ASP.NET Core Identity with Data Protection keys persisted
  to a volume so cookies survive container redeploys

---

## Running it locally

```bash
cd src/FanzinePress.Web
dotnet run
```

First run:

1. Database is created automatically (SQLite file next to the app).
2. PuppeteerSharp will try to download a Chromium (~200 MB) unless
   `FANZINE_CHROMIUM_PATH` is set.
3. A bootstrap admin user is created from `FANZINE_ADMIN_EMAIL` /
   `FANZINE_ADMIN_PASSWORD` env vars, falling back to
   `admin@fanzinepress.local` / `ChangeMe123!`. Change it. Seriously.

Then browse to the URL the dotnet runtime prints and log in.

---

## Running it in Docker

```bash
docker build -t fanzine-press:latest \
    --build-arg APP_VERSION=$(git describe --tags --always --match '[0-9]*') \
    --build-arg GIT_SHA=$(git rev-parse --short HEAD) \
    .

docker run --rm \
    -p 5055:8080 \
    -v /var/lib/fanzine-press:/data \
    -e FANZINE_ADMIN_EMAIL=you@example.com \
    -e FANZINE_ADMIN_PASSWORD='something better than ChangeMe123!' \
    fanzine-press:latest
```

The image installs Google Chrome from Google's apt repo (Ubuntu's
own `chromium` package is a snap-shim that refuses to start in
containers — we found out the hard way, so you don't have to).

### Sub-path hosting behind nginx

The image is built to run behind a reverse proxy at a sub-path like
`/fanzine-press`:

- `FANZINE_BEHIND_PROXY=true` — disables in-container HTTPS redirect.
- `FANZINE_PATH_BASE=/fanzine-press` — wires up `UsePathBase(...)`.
- `FANZINE_DATA_PROTECTION_KEYS=/data/dp-keys` — persists cookie
  encryption keys across container restarts.
- nginx: `proxy_pass http://127.0.0.1:5055;` with **no trailing
  slash** — the full `/fanzine-press/...` path is forwarded and
  stripped inside the app. See `deploy/nginx-fanzine-press.snippet`.

---

## Deploying to a server

There are two deploy scripts in `deploy/` that do the same thing,
one per shell religion:

- `deploy/build-and-deploy.sh` — bash, prefers SSH key auth,
  `sshpass` fallback.
- `deploy/build-and-deploy.ps1` — PowerShell, uses PuTTY
  `plink`/`pscp`, supports password or `.ppk` key.

Both scripts:

1. Compute `APP_VERSION` from `git describe --tags --always --dirty`
   and `GIT_SHA` from `git rev-parse --short HEAD`.
2. Pack the source tree (excluding `.git`, `bin`, `obj`, uploads,
   local DBs) into a tarball.
3. Upload it, extract on the remote, `docker build` with the
   version build-args, then `systemctl restart fanzine-press.service`.
4. Check the service comes back active and report the version.

The systemd unit (`deploy/fanzine-press.service`) runs
`docker run --rm` with an `--env-file` and bind-mounts `/data` on
the host to `/data` in the container, so the SQLite database and
uploaded images persist.

---

## Architecture notes nobody asked for

- **Images in the database** — the BLOB approach means backups are
  one file. Migration is a `Program.cs` startup hook that sweeps
  `wwwroot/uploads/` into the DB the first time an old instance
  starts up against the new schema.
- **No registration flow** — deliberate. Admins create editors;
  the world is better this way.
- **The AI wrote most of the code** — and then the human asked
  hard questions about it until it stopped being wrong.

---

## Credits

- **Product Manager:** T. Kleisas — wrangled requirements, tested
  in production like a champion, made executive decisions like
  "actually, the footer should say *both* the version AND the hash".
- **Software Engineer:** Claude (Anthropic), an LLM that suffers
  no ego when the compiler disagrees with it. Alignment achieved
  primarily via `dotnet build` exit codes.

The division of labor was roughly:
the PM decided *what* and *why*;
the AI decided *how* (subject to review);
the compiler decided whether either of them were right.

---

## License

MIT. Use it, fork it, print an issue about your local team,
remember that the only AI alignment that really matters
is aligning the columns in the CSS.
