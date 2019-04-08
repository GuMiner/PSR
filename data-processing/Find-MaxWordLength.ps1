<# Finds the maximum word length of a word #>
param(
[Parameter(Mandatory = $true)]
[string]
$InputFile)

$lineCounter = 0
$maxLength = 0
Write-Output "Processing $file..."
foreach ($line in [IO.File]::ReadAllLines($InputFile))
{
    if ($line.Length -gt $maxLength)
    {
        $maxLength = $line.Length
    }
    
    ++$lineCounter
    if ($lineCounter % 50000 -eq 0)
    {
        Write-Output "  Processed $lineCounter lines..."
    }
}

Write-Output "Found the maximum length word was $maxLength characters long."
Write-Output "Done."