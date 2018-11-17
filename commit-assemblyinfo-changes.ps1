# Copyright (c) 2018 The nanoFramework project contributors
# See LICENSE file in the project root for full license information.

# update assembly info in nf-interpreter if this is tag
if ($env:APPVEYOR_REPO_TAG -eq "true")
{
    'Updating assembly version in nf-interpreter...' | Write-Host -ForegroundColor White -NoNewline

    # name of source file with the native declaration
    $nativeFile = "win_dev_i2c_native.cpp"

    #  find assembly declaration
    $assemblyDeclarationPath = (Get-ChildItem -Path ".\*" -Include $nativeFile -Recurse)
    $filecontent = Get-Content($assemblyDeclarationPath)
    $assemblyChecksum  = $filecontent -match '(0x.{8})'
    $assemblyChecksum  = $assemblyChecksum -replace "," , ""
    $assemblyChecksum  = $assemblyChecksum -replace "    " , ""

    # clone nf-interpreter repo (only a shallow clone with last commit)
    git clone https://github.com/nanoframework/nf-interpreter -b develop --depth 1 -q
    cd nf-interpreter > $null

    # new branch name
    $newBranch = "$env:APPVEYOR_REPO_BRANCH-nfbot/update-version/Windows.Devices.I2c/$env:MyNuGetVersion"

    # create branch to perform updates
    git checkout -b "$newBranch" develop -q
    
    # replace version in assembly declaration
    $newVersion = $env:NBGV_AssemblyFileVersion -replace "\." , ", "
    $newVersion = "{ $newVersion }"
    
    $versionRegex = "\{\s*\d+\,\s*\d+\,\s*\d+\,\s*\d+\s*}"
    $assemblyFiles = (Get-ChildItem -Path ".\*" -Include $nativeFile -Recurse)

    foreach($file in $assemblyFiles)
    {
        # replace checksum
        $filecontent = Get-Content($file)
        attrib $file -r
        $filecontent -replace  "0x.{8}", $assemblyChecksum | Out-File $file -Encoding utf8

        # replace version
        $filecontent = Get-Content($file)
        attrib $file -r
        $filecontent -replace $versionRegex, $newVersion | Out-File $file -Encoding utf8
    }

    # check if anything was changed
    $repoStatus = "$(git status --short --porcelain)"

    if ($repoStatus -eq "") 
    {
        # nothing changed

        cd ..
    }
    else
    {
        $commitMessage = "Update Windows.Devices.I2c version to $env:MyNuGetVersion"

        # commit changes
        git add -A 2>&1
        git commit -m"$commitMessage" -m"[version update]" -q
        git push --set-upstream origin "$newBranch" --porcelain -q > $null
    
        # start PR
        $prRequestBody = @{title="$commitMessage";body="$commitMessage`n`nStarted from https://github.com/$env:APPVEYOR_REPO_NAME/commit/$env:APPVEYOR_REPO_COMMIT`n`n[version update]";head="$newBranch";base="develop"} | ConvertTo-Json
        $githubApiEndpoint = "https://api.github.com/repos/nanoframework/nf-interpreter/pulls"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

        try 
        {
            $result = Invoke-RestMethod -Method Post -UserAgent [Microsoft.PowerShell.Commands.PSUserAgent]::InternetExplorer -Uri  $githubApiEndpoint -Header @{"Authorization"="Basic $env:GitRestAuth"} -ContentType "application/json" -Body $prRequestBody
            'Started PR with version update...' | Write-Host -ForegroundColor White -NoNewline
            'OK' | Write-Host -ForegroundColor Green
        }
        catch 
        {
            $result = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($result)
            $reader.BaseStream.Position = 0
            $reader.DiscardBufferedData()
            $responseBody = $reader.ReadToEnd();

            "Error starting PR: $responseBody" | Write-Host -ForegroundColor Red
        }

        # move back to home folder
        &  cd .. > $null
    }
}
