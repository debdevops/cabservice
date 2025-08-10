# PowerShell script to generate high-quality PNG files from Mermaid diagrams
Write-Host "Generating PNG files from Mermaid diagrams..." -ForegroundColor Green

# Create diagrams directory if it doesn't exist
if (!(Test-Path "diagrams")) {
    New-Item -ItemType Directory -Path "diagrams"
}

# Generate PNGs from existing .mmd files
$mmdFiles = Get-ChildItem -Path "diagrams" -Filter "*.mmd"

foreach ($file in $mmdFiles) {
    $inputFile = $file.FullName
    $outputFile = $inputFile -replace '\.mmd$', '.png'
    
    Write-Host "Converting $($file.Name)..." -ForegroundColor Yellow
    
    try {
        & mmdc -i $inputFile -o $outputFile -t dark -b white -s 2
        Write-Host "Generated: $($file.BaseName).png" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to generate: $($file.Name)" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
    }
}

Write-Host "Done! Check the diagrams folder for PNG files." -ForegroundColor Green
