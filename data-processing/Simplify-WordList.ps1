<# Simplifies a word list to consistent casing. #>
param(
[Parameter(Mandatory = $true)]
[string]
$InputFile,

[Parameter(Mandatory = $true)]
[string]
$OutputFile)

$words = New-Object 'System.Collections.Generic.HashSet[System.String]'

$lineCounter = 0
Write-Output "Processing $file..."
foreach ($line in [IO.File]::ReadAllLines($InputFile))
{
    if (-not [string]::IsNullOrWhiteSpace($line))
    {
        $words.Add($line.ToUpperInvariant()) | Out-Null
    }

    ++$lineCounter
    if ($lineCounter % 50000 -eq 0)
    {
        Write-Output "  Processed $lineCounter lines, $($words.Count) total words found."
    }
}

Write-Output "Saving condensed file..."
[IO.File]::WriteAllLines($OutputFile, $words)
Write-Output "Done."