# High-Quality PNG Generation Script for CabService Diagrams
Write-Host "Generating high-quality PNG files from Mermaid diagrams..." -ForegroundColor Green

# Create diagrams directory if it doesn't exist
if (!(Test-Path "diagrams")) {
    New-Item -ItemType Directory -Path "diagrams"
}

# Define optimal settings for high-quality, clear, large images
$scale = 4
$width = 2400
$height = 1800
$theme = "dark"
$background = "white"

# List of diagrams to generate
$diagrams = @(
    "01-frontend-backend-calls",
    "02-driver-app-flow", 
    "03-inter-service-communication",
    "04-notification-hub",
    "05-end-to-end-booking",
    "06-azure-functions-flow",
    "07-signalr-realtime",
    "08-external-services",
    "09-api-gateway-routing"
)

Write-Host "Using settings: Scale=$scale, Width=$width, Height=$height" -ForegroundColor Yellow

foreach ($diagram in $diagrams) {
    $inputFile = "diagrams/$diagram.mmd"
    $outputFile = "diagrams/$diagram.png"
    
    if (Test-Path $inputFile) {
        Write-Host "Converting $diagram..." -ForegroundColor Cyan
        
        try {
            & mmdc -i $inputFile -o $outputFile -t $theme -b $background -s $scale --width $width --height $height
            Write-Host "Generated: $diagram.png" -ForegroundColor Green
        }
        catch {
            Write-Host "Failed: $diagram" -ForegroundColor Red
            Write-Host $_.Exception.Message -ForegroundColor Red
        }
    }
    else {
        Write-Host "Missing: $inputFile" -ForegroundColor Yellow
    }
}

Write-Host "Completed! All PNG files generated in diagrams folder." -ForegroundColor Green
Write-Host "Settings used: 4x scale, 2400x1800 resolution for maximum clarity" -ForegroundColor Green
