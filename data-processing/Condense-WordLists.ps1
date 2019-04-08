<# Condenses a folder with newline-deliminated words into a single file with no duplicates. #>
param(
[Parameter(Mandatory = $true)]
[string]
$InputFolder,

[Parameter(Mandatory = $true)]
[string]
$OutputFile)

$words = New-Object 'System.Collections.Generic.HashSet[System.String]'

$lineCounter = 0
foreach ($file in [IO.Directory]::GetFiles($InputFolder))
{
    Write-Output "Processing $file..."
    foreach ($line in [IO.File]::ReadAllLines($file))
    {
        if (-not [string]::IsNullOrWhiteSpace($line))
        {
            $words.Add($line) | Out-Null
        }

        ++$lineCounter
        if ($lineCounter % 10000 -eq 0)
        {
            Write-Output "  Processed $lineCounter lines, $($words.Count) total words found."
        }
    }
}

Write-Output "Saving condensed file..."
[IO.File]::WriteAllLines($OutputFile, $words)
Write-Output "Done."