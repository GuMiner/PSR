<# Condenses crosswords into a single file for PosgreSQL importing, with no duplicates. #>
param(
[Parameter(Mandatory = $true)]
[string]
$InputFolder,

[Parameter(Mandatory = $true)]
[string]
$OutputFile)

$global:duplicateCounter = 0
$global:resultCounter = 0
$badFiles = 0
$cluesWithAnswers = New-Object 'System.Collections.Generic.Dictionary[System.String, System.Collections.Generic.List[System.String]]'

function Process-ClueAnswer
{
    param($Clue, $Answer, $Output)
    
    ++$global:resultCounter
    $simplifiedClue = $clue.Substring($clue.IndexOf(" ") + 1).Replace([char]0x0027, "'").Replace([char]0x0026, "&").Replace("AMP;", "").ToUpperInvariant()
    $simplifiedClue = $simplifiedClue.Replace("</SPAN>", "").Replace('<SPAN STYLE="TEXT-DECORATION:LINE-THROUGH">', "")
    $answerUpper = $Answer.Replace([char]0x0027, "'").Replace([char]0x0026, "&").Replace("AMP;", "").ToUpperInvariant()
    $answerUpper = $answerUpper.Replace("</SPAN>", "").Replace('<SPAN STYLE="TEXT-DECORATION:LINE-THROUGH">', "")
    
    if (-not $Output.ContainsKey($simplifiedClue))
    {
        $Output.Add($simplifiedClue, (New-Object 'System.Collections.Generic.List[System.String]'))
    }

    $hasAnswer = $false
    foreach ($knownAnswer in $Output[$simplifiedClue])
    {
        if ($knownAnswer.Equals($answerUpper))
        {
            ++$global:duplicateCounter
            $hasAnswer = $true
            $break
        }
    }
    
    if (-not $hasAnswer)
    {
        $Output[$simplifiedClue].Add($answerUpper)
    }
}

# Recursively scan directories
$directoriesToScan = New-Object 'System.Collections.Generic.Stack[System.String]'
$directoriesToScan.Push($InputFolder)
while ($directoriesToScan.Count -ne 0)
{
    $currentDirectory = $directoriesToScan.Pop();
    foreach ($directory in [IO.Directory]::GetDirectories($currentDirectory))
    {
        $directoriesToScan.Push($directory);
    }
    
    Write-Output "Processing $currentDirectory..."
    foreach ($file in [IO.Directory]::GetFiles($currentDirectory))
    {
        if ($file.EndsWith(".json", [StringComparison]::OrdinalIgnoreCase))
        {
            $fileText = [IO.File]::ReadAllText($file)
            if ($fileText.StartsWith("<!DOCTYPE html>"))
            {
                ++$badFiles
                Write-Warning "  Bad file: $file"
                continue
            }
            $crosswordContent = ConvertFrom-Json -InputObject $fileText
            $counter = 0
            foreach ($clue in $crosswordContent.clues.across)
            {
                $answer = $crosswordContent.answers.across[$counter]
                ++$counter
                
                Process-ClueAnswer -Clue:$clue -Answer:$answer -Output:$cluesWithAnswers
            }
            
            $counter = 0
            foreach ($clue in $crosswordContent.clues.down)
            {
                $answer = $crosswordContent.answers.down[$counter]
                ++$counter
                
                Process-ClueAnswer -Clue:$clue -Answer:$answer -Output:$cluesWithAnswers
            }
        }
    }
    
    Write-Output "Processed ($global:duplicateCounter duplicates, $global:resultCounter results ($badFiles bad files))."
# Early exit for testability.
#   if ($counter -gt 10) 
#   {
#       break
#   }
}

Write-Output "Reformatting and saving condensed crossword data..."
$crosswordData = New-Object 'System.Collections.Generic.List[System.String]'
foreach ($key in $cluesWithAnswers.Keys)
{
    foreach ($value in $cluesWithAnswers[$key])
    {
        $newKey = $key
        if ($key.Contains(",") -or $key.Contains('"') -or $key.Contains("`r") -or $key.Contains("`n"))
        {
            $newKey = '"' + $key.Replace('"', '""') + '"'
        }
    
        $newValue = $value
        if ($value.Contains(",") -or $value.Contains('"') -or $value.Contains("`r") -or $value.Contains("`n"))
        {
            $newValue = '"' + $value.Replace('"', '""') + '"'
        }
    
        $crosswordData.Add("$newKey,$newValue")
    }
}

[IO.File]::WriteAllLines($OutputFile, $crosswordData)
Write-Output "Done."