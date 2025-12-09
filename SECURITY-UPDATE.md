# Security Update: Environment Variables

## Changes Made

### 1. Created Environment Files
- **`.env`**: Contains actual credentials (excluded from git)
- **`.env.example`**: Template file showing required variables (safe to commit)
- **`README.ENV.md`**: Documentation for environment setup

### 2. Updated Configuration Files
- **`appsettings.json`**: Removed ClientId and ClientSecret (now empty strings)
- **`appsettings.Development.json`**: Removed ClientId and ClientSecret (now empty strings)

### 3. Updated Code
- **`server.csproj`**: Added DotNetEnv package (v3.1.1)
- **`Program.cs`**: Added environment variable loading and configuration override

### 4. Updated Git Ignore
- **`.gitignore`**: Added `.env` and `**/.env` patterns
- **`server/.gitignore`**: Already had `.env` excluded

## How It Works

1. On startup, `DotNetEnv.Env.Load()` reads the `.env` file
2. Environment variables are loaded into the process
3. Program.cs overrides configuration values from environment variables
4. Configuration hierarchy: appsettings.json → appsettings.Development.json → .env → system env vars

## Benefits

✅ **Security**: Credentials no longer stored in version control  
✅ **Flexibility**: Easy to change credentials without editing config files  
✅ **Best Practice**: Standard approach for managing secrets in development  
✅ **Team Friendly**: Each developer can use their own credentials  

## For Team Members

When checking out this project, you need to:

1. Copy `server/.env.example` to `server/.env`
2. Add your own EVE SSO credentials to the `.env` file
3. Get credentials from https://developers.eveonline.com/

## Next Steps for Production

For production deployments, consider:
- Azure Key Vault for Azure deployments
- AWS Secrets Manager for AWS deployments
- Docker secrets for containerized deployments
- Environment variables set at the hosting platform level
