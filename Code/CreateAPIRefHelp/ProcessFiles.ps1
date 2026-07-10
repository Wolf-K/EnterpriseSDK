param(
    [string]$ReleaseVersion,
    [string]$TlbDirectory,
    [string]$DllDirectory,
    [string]$OutputDirectory
)

# Function to call the tool with the specified parameters
function Call-Tool {
    param(
        [string]$FileName,
        [string]$OutputDir,
        [string]$Version
    )
    # Replace 'YourToolCommand' with the actual tool command
    Write-Host "Calling tool for file: $FileName with output directory: $OutputDir and version: $Version"
    & "C:\Users\seli2607\source\repos\Devtopia\ao-build\EnterpriseSDK\CreateAPIRefHelp\CreateAPIRefHelp\bin\Debug\net10.0\CreateAPIRefHelp.exe" $FileName $OutputDir ESRI.Server $Version
}

# Step 1: Empty the output directory
if (Test-Path $OutputDirectory) {
    Write-Host "Clearing output directory: $OutputDirectory"
    Remove-Item -Recurse -Force -Path "$OutputDirectory\*"
} else {
    Write-Host "Output directory does not exist. Creating: $OutputDirectory"
    New-Item -ItemType Directory -Path $OutputDirectory
}

# Step 2: Process TLB files
if (Test-Path $TlbDirectory) {
    Write-Host "Processing TLB files in directory: $TlbDirectory"
    Get-ChildItem -Path $TlbDirectory -Filter *.tlb | ForEach-Object {
        Call-Tool -FileName $_.FullName -OutputDir $OutputDirectory -Version $ReleaseVersion
    }
} else {
    Write-Host "TLB directory does not exist: $TlbDirectory"
}

# Step 3: Process DLL files
if (Test-Path $DllDirectory) {
    Write-Host "Processing DLL files in directory: $DllDirectory"
    Get-ChildItem -Path $DllDirectory -Filter *.dll | ForEach-Object {
        Call-Tool -FileName $_.FullName -OutputDir $OutputDirectory -Version $ReleaseVersion
    }
} else {
    Write-Host "DLL directory does not exist: $DllDirectory"
}