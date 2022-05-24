Install-Module InvokeBuild -Force -Scope CurrentUser
Invoke-Build -Task @('Build', 'Publish')