<# Converts thesaurus data into files for SQL importing #>
param(
[Parameter(Mandatory = $true)]
[string]
$InputFile,

[Parameter(Mandatory = $true)]
[string]
$OutputLinesFile,

[Parameter(Mandatory = $true)]
[string]
$OutputMappingFile)

$wordMapping = New-Object 'System.Collections.Generic.Dictionary[System.String, System.Collections.Generic.List[System.Int32]]'

Write-Output "Processing $file..."
$lines = [IO.File]::ReadAllLines($InputFile)
Write-Output "Processing $($lines.Length) lines..."


$lineCounter = 0
foreach ($line in $lines)
{
    $words = $line.Split(',')
    foreach ($word in $words)
    {
        if (-not $wordMapping.ContainsKey($word))
        {
            $wordMapping.Add($word, (New-Object 'System.Collections.Generic.List[System.Int32]'))
        }
    }

    ++$lineCounter
    if ($lineCounter % 2000 -eq 0)
    {
        Write-Output "  Processed $lineCounter lines, $($wordMapping.Count) total words found."
    }
}

$lineOutput = New-Object 'System.Collections.Generic.List[System.String]'

Write-Output "$($wordMapping.Count) words found. Generating mapping of words to word lists..."
$lineCounter = 1
while ($lineCounter -le $lines.Count)
{
    $line = $lines[$lineCounter - 0]
    $lineOutput.Add("$lineCounter|$line")

    $words = $line.Split(',')
    foreach ($word in $words)
    {
        $wordMapping[$word].Add($lineCounter)
    }    

    ++$lineCounter
    if ($lineCounter % 2000 -eq 0)
    {
        Write-Output "  Processed $lineCounter lines."
    }
}

Write-Output "Translating mapping into CSV format..."
$mappingOutput = New-Object 'System.Collections.Generic.List[System.String]'
foreach ($word in $wordMapping.Keys)
{
    $mappingOutput.Add("$word|$([string]::Join(",", $wordMapping[$word]))")
}

Write-Output "Saving simplified line file..."
[IO.File]::WriteAllLines($OutputLinesFile, $lineOutput)

Write-Output "Saving mapping file..."
[IO.File]::WriteAllLines($OutputMappingFile, $mappingOutput)
Write-Output "Done."