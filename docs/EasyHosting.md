# Easiest Hosting Path

GitHub is best for storing the source code. GitHub Pages is not suitable for this app because it only hosts static files and does not run ASP.NET Core server code.

The easiest no-Azure path is:

1. Push this repository to GitHub.
2. Create a Render account.
3. In Render, choose **New > Blueprint**.
4. Connect the GitHub repository.
5. Select `render.yaml`.
6. Deploy.

Render will build the existing Dockerfile and run the Blazor web app on port `8080`.

## Important Data Note

The hosted container cannot access your local file:

```text
%USERPROFILE%\Downloads\IT Asset inv.xlsx
```

For first cloud boot, the Docker image uses:

```text
/app/App_Data/ITAssetImportTemplate.xlsx
```

For real production data, configure durable storage or Azure Blob/SQL-backed import instead of relying on a file inside the container.

## Other Easy Options

- Azure App Service: best long-term enterprise option when you get Azure access.
- Render: easiest from GitHub with the included `render.yaml`.
- Railway: also easy with the included Dockerfile.
- Fly.io: good Docker hosting, more CLI-oriented.
