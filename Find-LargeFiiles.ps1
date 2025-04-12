# Save this script as Find-LargeFiles.ps1

param(
    # Specify the directory to search.
    [Parameter(Mandatory = $true)]
    [string]$DirectoryPath
)

# Check if the specified directory exists
if (!(Test-Path -Path $DirectoryPath)) {
    Write-Error "Directory '$DirectoryPath' does not exist."
    exit 1
}

# Recursively get all files from the directory, sort by file size descending,
# and select the full path and a nicely formatted size in megabytes.
Get-ChildItem -Path $DirectoryPath -Recurse -File | 
    Sort-Object -Property Length -Descending | 
    Select-Object -Property FullName,
        @{Name="Size (MB)"; Expression={[math]::Round($_.Length / 1MB, 2)}} |
    Format-Table -AutoSize
# Save this script as Find-LargeFiles.ps1
#
# param(
#     # Specify the directory to search.
#         [Parameter(Mandatory = $true)]
#             [string]$DirectoryPath
#             )
#
#             # Check if the specified directory exists
#             if (!(Test-Path -Path $DirectoryPath)) {
#                 Write-Error "Directory '$DirectoryPath' does not exist."
#                     exit 1
#                     }
#
#                     # Recursively get all files from the directory, sort by file size descending,
#                     # and select the full path and a nicely formatted size in megabytes.
#                     Get-ChildItem -Path $DirectoryPath -Recurse -File | 
#                         Sort-Object -Property Length -Descending | 
#                             Select-Object -Property FullName,
#                                     @{Name="Size (MB)"; Expression={[math]::Round($_.Length / 1MB, 2)}} |
#                                         Format-Table -AutoSize
#
