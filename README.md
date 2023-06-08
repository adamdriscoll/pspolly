# PSPolly 

PSPolly is a PowerShell wrapper around [Polly](https://github.com/App-vNext/Polly). 

> Polly is a .NET resilience and transient-fault-handling library that allows developers to express policies such as Retry, Circuit Breaker, Timeout, Bulkhead Isolation, and Fallback in a fluent and thread-safe manner. 

![](https://img.shields.io/powershellgallery/dt/pspolly?style=for-the-badge)

# Installation 

```powershell
Install-Module PSPolly
```

# Usage

## Retry 

Retry a number of times.

```powershell
$Policy = New-PollyPolicy -Retry -RetryCount 10
Invoke-PollyCommand -Policy $Policy -ScriptBlock {
    Write-Host "Trying.."
    throw "Failed"
}
```

Retry a number of times with a delay.

```powershell
$Policy = New-PollyPolicy -Retry -RetryCount 3 -RetryWait @(
    New-TimeSpan -Seconds 3
    New-TimeSpan -Seconds 6
    New-TimeSpan -Seconds 9
)
Invoke-PollyCommand -Policy $Policy -ScriptBlock {
    Write-Host "Trying.."
    throw "Failed"
}
```

Retry a number of times with a configurable delay.

```powershell
$Policy = New-PollyPolicy -Retry -RetryCount 3 -SleepDuration {
    $Random = Get-Random -Min 1 -Max 10
    New-TimeSpan -Seconds $Random
}
Invoke-PollyCommand -Policy $Policy -ScriptBlock {
    Write-Host "Trying.."
    throw "Failed"
}
```

Retry a number of times invoking a function on each error.

```powershell
$Policy = New-PollyPolicy -Retry -RetryCount 3 -OnRetryError {
    param($ex, $timeSpan, $retryAttempt, $context)
    Write-Warning "Retry $retryAttempt and error $ex"
}
Invoke-PollyCommand -Policy $Policy -ScriptBlock {
    Write-Host "Trying.."
    throw "Failed"
}
```

Retry forever.

```powershell
$Policy = New-PollyPolicy -RetryForever -SleepDuration {
    $Random = Get-Random -Min 1 -Max 10
    New-TimeSpan -Seconds $Random
}
Invoke-PollyCommand -Policy $Policy -ScriptBlock {
    Write-Host "Trying.."
    throw "Failed"
}
```

## Circuit Breaker

Pause execution after a certain amount of exceptions occur. Circuit breakers do not catch exceptions but will stop executing the action once the circuit breaker has opened. The initial state of the circuit breaker is closed.

```powershell
$Policy = New-PollyPolicy -CircuitBreaker -ExceptionsAllowedBeforeBreaking 3 -DurationOfBreak (New-TimeSpan -Seconds 5)
1..10 | ForEach-Object {
    try 
    {
        Invoke-PollyCommand -Policy $Policy -ScriptBlock {
            Write-Host "Trying.."
            throw "Failed"
        }
    }
    catch 
    {
        $_
    }
}

```

## Cache

Cache data for the specified time frame. 

An absolute expiration invalidates the cache after a specific amount of time.

```powershell
$Policy = New-PollyPolicy -AbsoluteExpiration (Get-Date).AddHours(1)
Invoke-PollyCommand -Policy $Policy -ScriptBlock {
    Get-Process; Start-Sleep 10
} -OperationKey 'Absolute'
```

A sliding expiration invalidates the cache after it hasn't be accessed for the specified time frame. 

```powershell
$Policy = New-PollyPolicy -SlidingExpiration (New-TimeSpan -Seconds 20)
Invoke-PollyCommand -Policy $Policy -ScriptBlock {
    Get-Process; Start-Sleep 10
} -OperationKey 'Sliding'
```

## Rate Limit

Prevent a particular command from being called too frequently. 

```powershell
$Policy = New-PollyPolicy -Executions 5 -PerTimeSpan (New-TimeSpan -Seconds 10)
1..5 | ForEach-Object {
    Invoke-PollyCommand -Policy $Policy -ScriptBlock {
        "Hello"
    } -OperationKey 'RateLimit'
}
```

## Combine Policies 

Combine policies to create more powerful handling.

```powershell
$CircuitBreaker = New-PollyPolicy -CircuitBreaker -ExceptionsAllowedBeforeBreaking 3 -DurationOfBreak (New-TimeSpan -Seconds 5)
$Retry = New-PollyPolicy -Retry -RetryCount 10
$Policy = Join-PollyPolicy -Policy @($CircuitBreaker, $Retry)
1..10 | ForEach-Object {
    Invoke-PollyCommand -Policy $Policy -ScriptBlock {
        Write-Host "Trying.."
        throw "Failed"
    }
}
```
