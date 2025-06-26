# Product Catalog - Local Storage Initialization Troubleshooting Guide

This guide helps resolve common issues when running the initialization scripts outside of VS Code terminal.

## New Split Script Approach (Recommended)

The initialization process has been split into two separate scripts for better modularity:

### 1. Start Emulators First
```powershell
.\initialize-emulators.ps1 -VerboseOutput
```

### 2. Initialize Data Structures
```powershell
.\initialize-catalog-data.ps1 -VerboseOutput
```

## Quick Start Options

### Option 1: New Split Scripts (Recommended)
```powershell
# Step 1: Start emulators
.\initialize-emulators.ps1

# Step 2: Initialize database structures and sample data
.\initialize-catalog-data.ps1
```

### Option 2: Legacy Combined Script
```powershell
.\initialize-local-storage.ps1 -VerboseOutput
```

### Option 3: Execution Policy Bypass
```powershell
powershell -ExecutionPolicy Bypass -File "initialize-emulators.ps1"
powershell -ExecutionPolicy Bypass -File "initialize-catalog-data.ps1"
```

## Common Issues and Solutions

### 1. Execution Policy Errors

**Error:** "execution of scripts is disabled on this system"

**Solutions:**
- **Temporary bypass (recommended):**
  ```powershell
  powershell -ExecutionPolicy Bypass -File "initialize-local-storage.ps1"
  ```
- **Set policy for current user:**
  ```powershell
  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
  ```
- **Run as Administrator and set machine policy:**
  ```powershell
  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope LocalMachine
  ```

### 2. Character Encoding Issues

**Symptoms:** Garbled output, strange characters, truncated text

**Solutions:**
- Use the robust script (`initialize-local-storage-robust.ps1`) which handles encoding
- Set console encoding manually:
  ```cmd
  chcp 65001
  powershell -File "initialize-local-storage.ps1"
  ```
- Use Windows Terminal or PowerShell 7+ instead of older PowerShell ISE or cmd.exe

### 3. Administrator Privileges

**Error:** "The requested operation requires elevation"

**Solutions:**
- **Run PowerShell as Administrator:**
  1. Right-click on PowerShell icon
  2. Select "Run as administrator"
  3. Navigate to your project folder
  4. Run the script

- **Use the script's admin detection:**
  - The script will warn if not running as admin but continue with limited functionality
  - Some Cosmos DB operations may fail without admin rights

### 4. Network/Firewall Issues

**Symptoms:** Cannot connect to Cosmos DB Emulator, timeout errors

**Solutions:**
- **Check Windows Firewall:**
  ```powershell
  # Allow Cosmos DB Emulator through firewall
  New-NetFirewallRule -DisplayName "Cosmos DB Emulator" -Direction Inbound -Protocol TCP -LocalPort 8081,8900,8901,8902,10250,10251,10252,10253,10254,10255
  ```
- **Verify Cosmos DB Emulator ports:**
  ```powershell
  netstat -an | findstr "8081"
  ```
- **Reset Cosmos DB Emulator:**
  ```cmd
  "%ProgramFiles%\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" /Shutdown
  "%ProgramFiles%\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" /NoExplorer /NoUI
  ```

### 5. Certificate/SSL Issues

**Error:** SSL certificate errors when connecting to Cosmos DB

**Solutions:**
- **Set environment variable to ignore SSL errors (development only):**
  ```powershell
  $env:NODE_TLS_REJECT_UNAUTHORIZED = "0"
  ```
- **Export and trust Cosmos DB certificate:**
  1. Open https://localhost:8081/_explorer/index.html
  2. Export the certificate
  3. Install it in "Trusted Root Certification Authorities"

### 6. Node.js/npm Issues

**Error:** Node.js or npm not found

**Solutions:**
- **Install Node.js:**
  - Download from https://nodejs.org/
  - Use version 16+ for best compatibility
- **Verify installation:**
  ```cmd
  node --version
  npm --version
  ```
- **Update npm:**
  ```cmd
  npm install -g npm@latest
  ```

### 7. Redis Issues

**Error:** Redis not found or won't start

**Solutions:**
- **Windows native Redis:**
  ```powershell
  # Download from GitHub releases
  # https://github.com/tporadowski/redis/releases
  ```
- **WSL with Ubuntu:**
  ```bash
  sudo apt update
  sudo apt install redis-server
  sudo service redis-server start
  ```
- **Docker (alternative):**
  ```cmd
  docker run --name redis -p 6379:6379 -d redis:alpine
  ```

### 8. Process Already Running Issues

**Error:** "Port already in use" or "Process already running"

**Solutions:**
- **Kill existing processes:**
  ```powershell
  # Cosmos DB Emulator
  Get-Process -Name "CosmosDB.Emulator" | Stop-Process -Force
  
  # Redis
  Get-Process -Name "redis-server" | Stop-Process -Force
  ```
- **Check port usage:**
  ```cmd
  netstat -ano | findstr "8081"  REM Cosmos DB
  netstat -ano | findstr "6379"  REM Redis
  ```

## Script-Specific Troubleshooting

### Emulator Initialization (initialize-emulators.ps1)

**Common Issues:**
- Cosmos DB Emulator not installed
- Redis not available (Windows or WSL)
- Administrator privileges required

**Solutions:**
```powershell
# Check if running with proper flags
.\initialize-emulators.ps1 -SkipCosmosDB -VerboseOutput  # Redis only
.\initialize-emulators.ps1 -SkipRedis -VerboseOutput     # Cosmos DB only
```

### Data Initialization (initialize-catalog-data.ps1)

**Common Issues:**
- Emulators not running
- .NET SDK not available
- Project dependencies not restored

**Solutions:**
```powershell
# Ensure emulators are running first
.\initialize-emulators.ps1

# Then initialize data for specific backend
.\initialize-catalog-data.ps1 -Backend CosmosDB -VerboseOutput
.\initialize-catalog-data.ps1 -Backend Redis -VerboseOutput
```

## Environment-Specific Solutions

### Windows PowerShell 5.1
- Use the batch wrapper for better compatibility
- Some Unicode characters may not display correctly

### PowerShell 7+
- Better cross-platform support
- Improved Unicode handling
- Use the robust script for best results

### Windows Terminal
- Better rendering and Unicode support
- Recommended terminal for running scripts

### Command Prompt (cmd.exe)
- Use the batch wrapper
- Limited color support
- May have encoding issues

## Testing Your Setup

Run the diagnostic script to identify specific issues:
```powershell
.\diagnose-script-issues.ps1
```

## Manual Verification Steps

1. **Check Cosmos DB Emulator:**
   - Open: https://localhost:8081/_explorer/index.html
   - Should show Data Explorer interface

2. **Check Redis:**
   ```cmd
   redis-cli ping
   ```
   Should return: `PONG`

3. **Test the application:**
   ```cmd
   dotnet run --project ProductCatalog
   ```
   Select different backends to verify connectivity

## Getting Help

If you continue to have issues:

1. Run the diagnostic script and save output
2. Check Windows Event Viewer for additional error details
3. Try running with different PowerShell hosts (Windows PowerShell vs PowerShell 7)
4. Verify all prerequisites are installed and accessible

## Useful Commands for Troubleshooting

```powershell
# Check PowerShell version and execution policy
$PSVersionTable
Get-ExecutionPolicy -List

# Check if running as admin
([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

# List running processes related to our tools
Get-Process | Where-Object {$_.ProcessName -like "*Cosmos*" -or $_.ProcessName -like "*redis*"}

# Check network ports
netstat -an | findstr "8081\|6379"

# Test basic network connectivity
Test-NetConnection -ComputerName localhost -Port 8081
```
