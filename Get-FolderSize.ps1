# Save this script as Get-FolderSizes.ps1

param(
    # Specify the main folder path to scan (e.g., your GitHub folder)
    [Parameter(Mandatory = $true)]
    [string]$RootFolder
)

if (!(Test-Path -Path $RootFolder)) {
    Write-Error "Directory '$RootFolder' does not exist."
    exit 1
}

# Get all directories under the root folder
$dirs = Get-ChildItem -Path $RootFolder -Directory -Recurse

# Include the root folder in the analysis
$dirs = $dirs + (Get-Item -Path $RootFolder)

# For each directory, calculate the total size of files it contains
$results = foreach ($dir in $dirs) {
    $files = Get-ChildItem -Path $dir.FullName -File -Recurse -ErrorAction SilentlyContinue
    if ($files) {
        $totalSize = ($files | Measure-Object -Property Length -Sum).Sum
    }
    else {
        $totalSize = 0
    }
    [PSCustomObject]@{
        Directory = $dir.FullName
        'Size (MB)' = [math]::Round($totalSize / 1MB, 2)
    }
}

# Sort by size in descending order and format as a table
$results | Sort-Object -Property 'Size (MB)' -Descending | Format-Table -AutoSize

