$OutputPath = "$PSScriptRoot\bin\Release\netstandard2.0\publish"

task Build {
    Set-Location $PSScriptRoot
    
    dotnet publish -c Release

    Copy-Item "$PSScriptRoot\PSPolly.psd1" -Destination "$OutputPath"
}

task Publish {
    Rename-Item -Path $OutputPath -NewName "PSPolly"
    $ModulePath = "$PSScriptRoot\bin\Release\netstandard2.0\PSPolly"
    Publish-Module -Path $ModulePath -NuGetApiKey $Env:APIKEY
}

task . Build