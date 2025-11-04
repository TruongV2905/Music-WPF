# 🔐 Setup Spotify API Credentials

## ⚠️ IMPORTANT: Never commit API keys to Git!

### Step 1: Get Spotify API Keys

1. Go to https://developer.spotify.com/dashboard
2. Log in with your Spotify account
3. Click "Create app"
4. Fill in app details:
   - App name: Music WPF App
   - App description: A WPF music application
   - Redirect URIs: http://localhost
5. Accept terms and click "Create"
6. Copy your **Client ID** and **Client Secret**

### Step 2: Configure Credentials

#### Option A: Environment Variables (Recommended)

**Windows (PowerShell):**
```powershell
[System.Environment]::SetEnvironmentVariable('SPOTIFY_CLIENT_ID', 'your_client_id_here', 'User')
[System.Environment]::SetEnvironmentVariable('SPOTIFY_CLIENT_SECRET', 'your_client_secret_here', 'User')
```

**Windows (Command Prompt):**
```cmd
setx SPOTIFY_CLIENT_ID "your_client_id_here"
setx SPOTIFY_CLIENT_SECRET "your_client_secret_here"
```

**Restart your IDE/Terminal after setting environment variables!**

#### Option B: appsettings.json (Local only)

1. Edit `Group1.MusicApp/appsettings.json`:
```json
{
  "Spotify": {
    "ClientId": "your_actual_client_id_here",
    "ClientSecret": "your_actual_client_secret_here"
  }
}
```

2. Make sure `appsettings.json` is in `.gitignore` (already configured)

### Step 3: Verify Setup

Run the application:
```bash
cd D:\Project\Music-WPF
dotnet run --project Group1.MusicApp
```

If you see authentication errors, double-check your credentials.

## 🔒 Security Best Practices

1. ✅ Never hardcode API keys in source code
2. ✅ Use environment variables or config files
3. ✅ Add sensitive files to `.gitignore`
4. ✅ Rotate keys regularly
5. ✅ Use different keys for dev/production

## 🚨 If Keys Are Exposed

1. **Immediately revoke** the exposed keys at https://developer.spotify.com/dashboard
2. Generate new keys
3. Update your local configuration
4. Consider using Git tools to remove keys from history:
   - BFG Repo-Cleaner: https://rtyley.github.io/bfg-repo-cleaner/
   - git-filter-repo: https://github.com/newren/git-filter-repo

## 📞 Need Help?

- Spotify API Docs: https://developer.spotify.com/documentation/web-api
- Contact: Check repository issues
