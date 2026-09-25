Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$env:AWS_PAGER = ''
$script:RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))

function Invoke-Aws {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $result = & aws @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "AWS command failed: $($result -join [Environment]::NewLine)" }
    return ($result -join [Environment]::NewLine)
}

function Get-DeploymentConfig {
    param([string]$Environment)
    $path = Join-Path $script:RepositoryRoot "infrastructure/parameters/$Environment.json"
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

function Get-SectionParameters {
    param($Config, [string]$Section, [string]$Environment)
    $values = @{ EnvironmentName = $Environment }
    foreach ($property in $Config.$Section.PSObject.Properties) {
        $value = [string]$property.Value
        if ([string]::IsNullOrWhiteSpace($value) -or $value -match 'REPLACE_ME') {
            throw "Configure '$Section.$($property.Name)' in infrastructure/parameters/$Environment.json before deploying."
        }
        $values[$property.Name] = $value
    }
    return $values
}

function Get-Stack {
    param([string]$Name, [switch]$Optional)
    $response = & aws cloudformation describe-stacks --stack-name $Name --output json 2>&1
    if ($LASTEXITCODE -ne 0) {
        if ($Optional -and ($response -join '') -match 'Stack with id .+ does not exist') { return $null }
        throw "Cannot inspect stack ${Name}: $($response -join [Environment]::NewLine)"
    }
    return (($response -join [Environment]::NewLine) | ConvertFrom-Json).Stacks[0]
}

function Get-StackOutputs {
    param([string]$Name)
    $stack = Get-Stack -Name $Name
    if ($stack.StackStatus -notin @('CREATE_COMPLETE', 'UPDATE_COMPLETE', 'UPDATE_ROLLBACK_COMPLETE')) {
        throw "Stack $Name is not ready: $($stack.StackStatus)"
    }
    $values = @{}
    foreach ($output in $stack.Outputs) { $values[$output.OutputKey] = $output.OutputValue }
    return $values
}

function Write-TemporaryJson {
    param($Value)
    $path = Join-Path ([IO.Path]::GetTempPath()) "property-intelligence-$([guid]::NewGuid().ToString('N')).json"
    [IO.File]::WriteAllText($path, ($Value | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
    return $path
}

function Deploy-Stack {
    param([string]$Name, [string]$Template, [hashtable]$Parameters, [switch]$Execute)
    if ([string]::IsNullOrWhiteSpace($env:CF_EXECUTION_ROLE_ARN)) {
        throw 'CF_EXECUTION_ROLE_ARN must identify the CloudFormation execution role.'
    }
    $existing = Get-Stack -Name $Name -Optional
    $type = if ($null -eq $existing -or $existing.StackStatus -eq 'REVIEW_IN_PROGRESS') { 'CREATE' } else { 'UPDATE' }
    $changeSet = "release-$([guid]::NewGuid().ToString('N'))"
    $entries = @($Parameters.GetEnumerator() | Sort-Object Key | ForEach-Object {
        @{ ParameterKey = $_.Key; ParameterValue = [string]$_.Value }
    })
    $parameterFile = Write-TemporaryJson -Value $entries
    try {
        $templatePath = Join-Path $script:RepositoryRoot "infrastructure/templates/$Template.yml"
        $null = Invoke-Aws -Arguments @('cloudformation', 'create-change-set', '--stack-name', $Name,
            '--change-set-name', $changeSet, '--change-set-type', $type,
            '--template-body', "file://$templatePath", '--parameters', "file://$parameterFile",
            '--capabilities', 'CAPABILITY_NAMED_IAM', '--role-arn', $env:CF_EXECUTION_ROLE_ARN,
            '--description', "Repository deployment: $env:GITHUB_SHA", '--output', 'json')
        # Inspect the change set even when its waiter fails (an empty update is a normal no-op).
        $null = & aws cloudformation wait change-set-create-complete --stack-name $Name --change-set-name $changeSet 2>&1
        $waitExitCode = $LASTEXITCODE
        $change = (Invoke-Aws -Arguments @('cloudformation', 'describe-change-set', '--stack-name', $Name,
            '--change-set-name', $changeSet, '--output', 'json')) | ConvertFrom-Json
        if ($change.Status -eq 'FAILED' -and $change.StatusReason -match "didn't contain changes|No updates are to be performed") {
            Write-Host "$Name has no changes."
            $null = Invoke-Aws -Arguments @('cloudformation', 'delete-change-set', '--stack-name', $Name, '--change-set-name', $changeSet)
            return
        }
        if ($waitExitCode -ne 0 -or $change.Status -ne 'CREATE_COMPLETE') {
            throw "Change set creation failed for ${Name}: $($change | ConvertTo-Json -Depth 8)"
        }
        Write-Host ($change.Changes | ConvertTo-Json -Depth 12)
        Write-Host "Change set: $($change.ChangeSetId)"
        if (!$Execute) {
            Write-Host 'Preview only. No resources were created or updated. Review and execute this exact change set in AWS, or rerun with -Execute to create and apply a fresh change set.'
            return
        }
        $null = Invoke-Aws -Arguments @('cloudformation', 'execute-change-set', '--stack-name', $Name, '--change-set-name', $changeSet)
        $waiter = if ($type -eq 'CREATE') { 'stack-create-complete' } else { 'stack-update-complete' }
        try {
            $null = Invoke-Aws -Arguments @('cloudformation', 'wait', $waiter, '--stack-name', $Name)
        }
        catch {
            Write-Host (Invoke-Aws -Arguments @('cloudformation', 'describe-stack-events', '--stack-name', $Name, '--max-items', '15', '--output', 'table'))
            throw
        }
        Write-Host "$Name deployed successfully."
    }
    finally { Remove-Item -LiteralPath $parameterFile -Force }
}
