# Fanzine Press

A web application for creating newspaper-style fanzine documents with PDF export. Built for football fan publications and independent zines.

## Features

- Create and manage newspaper issues with articles, photos, and ads
- Newspaper-style layout with multi-column support
- Vintage/old newspaper photo effects (sepia, grain, halftone)
- Colophon (editorial credits) support
- PDF export via PuppeteerSharp (HTML to PDF)
- SQLite database — no external database needed
- Simple local deployment

## Tech Stack

- **Backend:** ASP.NET Core (Razor Pages)
- **Frontend:** htmx + vanilla JavaScript
- **Database:** SQLite via Entity Framework Core
- **PDF:** PuppeteerSharp (headless Chromium)
- **Images:** SixLabors.ImageSharp

## Getting Started

```bash
cd src/FanzinePress.Web
dotnet run
```

The app will be available at `https://localhost:5001` (or the port shown in the console).

On first run, the database will be created automatically and PuppeteerSharp will download Chromium (~200MB).

## License

MIT
