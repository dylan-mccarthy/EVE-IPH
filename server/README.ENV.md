# Environment Configuration

This project uses environment variables to store sensitive configuration data like API credentials.

## Setup Instructions

1. Copy the `.env.example` file to `.env` in the server directory:
   ```bash
   cd server
   cp .env.example .env
   ```

2. Edit the `.env` file and add your EVE Online SSO credentials:
   ```env
   EVE_SSO_CLIENT_ID=your_actual_client_id
   EVE_SSO_CLIENT_SECRET=your_actual_client_secret
   ```

3. Get your credentials from [EVE Developers](https://developers.eveonline.com/):
   - Create a new application
   - Set the callback URL to: `http://localhost:5173/auth/callback`
   - Select the required scopes (see `appsettings.json` for the full list)
   - Copy the Client ID and Secret to your `.env` file

## Important Security Notes

- **NEVER commit the `.env` file to git** - it contains sensitive credentials
- The `.env.example` file is safe to commit and should be updated when new variables are added
- In production, use proper secrets management (Azure Key Vault, AWS Secrets Manager, etc.)

## Configuration Hierarchy

The application loads configuration in this order:
1. `appsettings.json` (base configuration)
2. `appsettings.Development.json` (overrides for development environment)
3. Environment variables from `.env` file (overrides both config files)
4. System environment variables (highest priority)

## Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `EVE_SSO_CLIENT_ID` | EVE Online SSO Client ID | Yes |
| `EVE_SSO_CLIENT_SECRET` | EVE Online SSO Client Secret | Yes |
