# Digital Lions API - Render Deployment Guide

## 🚀 Deployment to Render

This API is production-ready for deployment on Render's free tier.

### Prerequisites
- GitHub repository connected to Render
- Render account (free tier supported)

### Deployment Steps

1. **Push to GitHub**
   ```bash
   git add .
   git commit -m "Add Docker support for Render deployment"
   git push origin master
   ```

2. **Create Web Service on Render**
   - Go to https://dashboard.render.com
   - Click "New +" → "Web Service"
   - Connect your GitHub repository
   - Configure:
     - **Name**: `digitallions-api`
     - **Environment**: `Docker`
     - **Region**: Choose closest to your users
     - **Branch**: `master`
     - **Plan**: `Free`

3. **Environment Variables** (Optional)
   Add in Render dashboard if needed:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   DataFilePath=Data/stories.json
   ```

4. **CORS Configuration**
   After deployment, update `appsettings.Production.json` with your frontend domain:
   ```json
   "CorsOrigins": [
     "https://your-frontend-domain.com",
     "https://your-frontend-domain.vercel.app"
   ]
   ```

### 🔍 What Was Configured

#### ✅ Dockerfile (Multi-stage Build)
- **Build Stage**: Uses `mcr.microsoft.com/dotnet/sdk:8.0`
- **Runtime Stage**: Uses `mcr.microsoft.com/dotnet/aspnet:8.0` (smaller image)
- **Port**: Listens on 10000 (Render's default)
- **Environment**: Configured for production

#### ✅ File Persistence
- `Data/stories.json` is included in the container
- Uses `AppContext.BaseDirectory` for correct path resolution
- Configured in `.csproj` with `CopyToOutputDirectory`

#### ✅ Production Configuration
- Created `appsettings.Production.json`
- CORS ready for frontend domains
- Logging configured appropriately

### 📡 API Endpoints (After Deployment)

Your API will be available at: `https://digitallions-api.onrender.com`

**Available Endpoints:**
- `GET /stories` - Get all impact stories
- `GET /stories/{id}` - Get specific story
- `POST /stories/{id}/like` - Increment likes
- `POST /stories` - Create new story
- `GET /swagger` - API documentation (if enabled)

### 🧪 Testing After Deployment

```bash
# Test GET all stories
curl https://digitallions-api.onrender.com/stories

# Test GET single story
curl https://digitallions-api.onrender.com/stories/1

# Test POST like
curl -X POST https://digitallions-api.onrender.com/stories/1/like

# Test with your frontend
# Update your frontend API base URL to: https://digitallions-api.onrender.com
```

### ⚠️ Important Notes

1. **Free Tier Limitations**
   - Service spins down after 15 minutes of inactivity
   - First request after inactivity may take 30-60 seconds
   - 750 hours/month free

2. **Data Persistence**
   - Likes are persisted in `stories.json`
   - **NOTE**: On Render's free tier, the filesystem is ephemeral
   - Data resets when the service restarts
   - For permanent storage, consider upgrading or using a database

3. **CORS**
   - Update production origins after deploying frontend
   - Redeploy API after updating CORS settings

### 🔧 Troubleshooting

**Build Fails**
- Check Render build logs
- Verify all files are committed to Git
- Ensure `.dockerignore` doesn't exclude necessary files

**API Returns 500**
- Check Render logs: Dashboard → Your Service → Logs
- Verify `Data/stories.json` exists in container
- Check file permissions

**CORS Errors**
- Add your frontend domain to `CorsOrigins` in `appsettings.Production.json`
- Commit and redeploy

### 📊 Monitoring

Monitor your API in the Render dashboard:
- **Metrics**: CPU, Memory, Request count
- **Logs**: Real-time application logs
- **Events**: Deployment history

---

## 🎉 Success Criteria

After deployment, verify:
- ✅ Service status shows "Live"
- ✅ `GET /stories` returns 27 stories
- ✅ `POST /stories/1/like` increments correctly
- ✅ No 500 errors in logs
- ✅ Frontend can connect successfully
