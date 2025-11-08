# SurveyBucks Configuration Guide

## Security Best Practices

This application follows security best practices by **NOT** storing sensitive credentials in source control. All sensitive configuration is managed through environment-specific files and environment variables.

## Configuration Files

### `appsettings.json`
- Contains **non-sensitive** default configuration
- **Committed to source control**
- Sensitive values are left empty and must be provided via environment-specific files

### `appsettings.Development.json`
- Contains development environment secrets
- **NOT committed to source control** (in .gitignore)
- Used during local development
- Copy from `appsettings.Example.json` and fill in your values

### `appsettings.Production.json`
- Contains production environment secrets
- **NOT committed to source control** (in .gitignore)
- Should be deployed separately via CI/CD pipeline or Azure App Configuration

### `appsettings.Example.json`
- Template showing all required configuration
- **Committed to source control** as documentation
- Use this as a reference to create your environment-specific files

## Local Development Setup

### 1. Create Development Configuration

```bash
cd WebApi
cp appsettings.Example.json appsettings.Development.json
```

### 2. Update Development Configuration

Edit `appsettings.Development.json` and provide your actual values:

```json
{
  "ConnectionStrings": {
    "SmarterConnection": "YOUR_ACTUAL_CONNECTION_STRING"
  },
  "JwtSettings": {
    "Key": "YOUR_SECRET_KEY_AT_LEAST_32_CHARS"
  },
  "EmailSettings": {
    "SmtpUsername": "your-email@example.com",
    "SmtpPassword": "your-email-password"
  }
}
```

### 3. Generate Secure JWT Key

Use one of these methods to generate a secure JWT key:

**PowerShell:**
```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

**Linux/Mac:**
```bash
openssl rand -base64 32
```

**Online (use with caution):**
- https://randomkeygen.com/ (CodeIgniter Encryption Keys section)

## Production Deployment

### Option 1: Azure App Service Configuration

1. Go to Azure Portal → Your App Service → Configuration
2. Add Application Settings for each secret:
   - `ConnectionStrings__SmarterConnection`
   - `JwtSettings__Key`
   - `EmailSettings__SmtpPassword`
   - etc.

### Option 2: Azure Key Vault (Recommended)

1. Create an Azure Key Vault
2. Store secrets in Key Vault
3. Configure App Service to use Key Vault references:
   ```
   @Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/DbPassword)
   ```

### Option 3: Environment Variables

Set environment variables on your hosting platform:

**Linux/Mac:**
```bash
export ConnectionStrings__SmarterConnection="your-connection-string"
export JwtSettings__Key="your-jwt-key"
export EmailSettings__SmtpPassword="your-password"
```

**Windows:**
```cmd
setx ConnectionStrings__SmarterConnection "your-connection-string"
setx JwtSettings__Key "your-jwt-key"
setx EmailSettings__SmtpPassword "your-password"
```

**Docker:**
```yaml
environment:
  - ConnectionStrings__SmarterConnection=your-connection-string
  - JwtSettings__Key=your-jwt-key
  - EmailSettings__SmtpPassword=your-password
```

## Required Secrets

### Database Connection
- **Key:** `ConnectionStrings:SmarterConnection`
- **Example:** `Data Source=server;Initial Catalog=db;User Id=user;Password=pass`

### JWT Authentication
- **Key:** `JwtSettings:Key`
- **Requirement:** Minimum 32 characters, cryptographically random
- **Example:** Generated using methods above

### Email Service
- **Keys:**
  - `EmailSettings:SmtpUsername`
  - `EmailSettings:SmtpPassword`
- **Note:** Configure with your SMTP provider credentials

### OAuth Providers (Optional)
If using social login, configure:
- `Authentication:Google:ClientId` and `ClientSecret`
- `Authentication:Facebook:AppId` and `AppSecret`
- `Authentication:Microsoft:ClientId` and `ClientSecret`

## Database Setup

### 1. Create Database Schema

Run the following scripts in order:

```bash
# 1. Create tables
sqlcmd -S your-server -d your-database -i Scripts/TableCreate.sql

# 2. Create stored procedures
sqlcmd -S your-server -d your-database -i Scripts/StoredProcedures.sql
```

### 2. Run Identity Migrations

The application will automatically create Identity tables on first run. Alternatively:

```bash
cd WebApi
dotnet ef database update
```

### 3. Seed Initial Data

The application automatically seeds:
- Admin and Client roles
- Survey status values (from TableCreate.sql)

## Verification

### Test Configuration Loading

Run the application and check logs:

```bash
cd WebApi
dotnet run
```

Look for:
- ✅ Database connection successful
- ✅ JWT configuration loaded
- ✅ Email service configured
- ✅ Roles seeded successfully

### Test API

Navigate to: `https://localhost:7051/swagger`

Try the health check endpoint (if configured) or attempt registration.

## Troubleshooting

### Configuration Not Loading

**Issue:** Application can't find configuration values

**Solution:**
1. Check file is named exactly `appsettings.Development.json`
2. Ensure file is in `WebApi/` directory
3. Verify `ASPNETCORE_ENVIRONMENT` is set to `Development`

### Database Connection Fails

**Issue:** Cannot connect to SQL Server

**Solution:**
1. Verify connection string format
2. Check firewall rules allow your IP
3. Ensure SQL Server authentication is enabled
4. Test connection using SQL Server Management Studio

### JWT Errors

**Issue:** "IDX10503: Signature validation failed"

**Solution:**
1. Ensure JWT Key is at least 32 characters
2. Verify Key is the same across all instances
3. Check Issuer and Audience match configuration

## Security Checklist

Before deploying to production:

- [ ] All secrets removed from `appsettings.json`
- [ ] `appsettings.Development.json` is in .gitignore
- [ ] Production secrets stored in Azure Key Vault or secure configuration
- [ ] JWT key is cryptographically random and at least 32 characters
- [ ] Database credentials use principle of least privilege
- [ ] SSL/TLS enforced for all connections
- [ ] CORS configured for specific origins only
- [ ] File upload directory has proper permissions

## Support

For issues or questions:
- Check logs in Azure Portal → App Service → Log Stream
- Review Application Insights for errors
- Contact development team

---

**⚠️ IMPORTANT:** Never commit files containing real credentials to source control!
