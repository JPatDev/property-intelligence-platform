# Dependency-free regression checks: all AWS calls are intercepted in this process.
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../Common.ps1"
$originalRole = $env:CF_EXECUTION_ROLE_ARN
$env:CF_EXECUTION_ROLE_ARN = 'arn:aws:iam::123456789012:role/test-cloudformation'
$script:Calls = [Collections.Generic.List[string]]::new()
$script:Scenario = 'create'

function aws {
    $arguments = @($args)
    $script:Calls.Add(($arguments -join ' '))
    $global:LASTEXITCODE = 0
    switch ($arguments[1]) {
        'describe-stacks' {
            if ($script:Scenario -eq 'create') {
                $global:LASTEXITCODE = 255
                'An error occurred (ValidationError): Stack with id test-stack does not exist'
            }
            elseif ($script:Scenario -eq 'access-denied') {
                $global:LASTEXITCODE = 255
                'An error occurred (AccessDenied): not authorized to describe stacks'
            }
            else { '{"Stacks":[{"StackStatus":"UPDATE_COMPLETE","Outputs":[]}]}' }
        }
        'create-change-set' { '{"Id":"test-change-set"}' }
        'wait' {
            if ($script:Scenario -in @('empty', 'invalid') -and $arguments[2] -eq 'change-set-create-complete') {
                $global:LASTEXITCODE = 255
            }
        }
        'describe-change-set' {
            if ($script:Scenario -eq 'empty') {
                '{"Status":"FAILED","StatusReason":"The submitted information didn''t contain changes."}'
            }
            elseif ($script:Scenario -eq 'invalid') {
                '{"Status":"FAILED","StatusReason":"Template permissions are invalid"}'
            }
            else { '{"Status":"CREATE_COMPLETE","ChangeSetId":"test-change-set","Changes":[{"ResourceChange":{"Action":"Add","LogicalResourceId":"Example"}}]}' }
        }
        'execute-change-set' { '' }
        'delete-change-set' { '' }
        default { throw "Unexpected mocked AWS call: $($arguments -join ' ')" }
    }
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (!$Condition) { throw $Message }
}

try {
    Deploy-Stack -Name test-stack -Template foundation -Parameters @{ EnvironmentName = 'dev' }
    Assert-True -Condition ($script:Calls.Exists({ param($call) $call -match '--change-set-type CREATE' })) -Message 'Missing stacks must use CREATE.'
    Assert-True -Condition (!$script:Calls.Exists({ param($call) $call -match 'execute-change-set' })) -Message 'Preview must not execute changes.'

    $script:Calls.Clear()
    Deploy-Stack -Name test-stack -Template foundation -Parameters @{ EnvironmentName = 'dev' } -Execute
    Assert-True -Condition ($script:Calls.Exists({ param($call) $call -match 'wait stack-create-complete' })) -Message 'Initial deployment must wait for stack creation.'

    $script:Scenario = 'update'
    $script:Calls.Clear()
    Deploy-Stack -Name test-stack -Template foundation -Parameters @{ EnvironmentName = 'dev' } -Execute
    Assert-True -Condition ($script:Calls.Exists({ param($call) $call -match '--change-set-type UPDATE' })) -Message 'Existing stacks must use UPDATE.'
    Assert-True -Condition ($script:Calls.Exists({ param($call) $call -match 'wait stack-update-complete' })) -Message 'Updates must wait for stack completion.'

    $script:Scenario = 'empty'
    $script:Calls.Clear()
    Deploy-Stack -Name test-stack -Template foundation -Parameters @{ EnvironmentName = 'dev' } -Execute
    Assert-True -Condition (!$script:Calls.Exists({ param($call) $call -match 'execute-change-set' })) -Message 'Empty updates must not be executed.'

    foreach ($scenario in @('invalid', 'access-denied')) {
        $script:Scenario = $scenario
        $script:Calls.Clear()
        $failed = $false
        try { Deploy-Stack -Name test-stack -Template foundation -Parameters @{ EnvironmentName = 'dev' } -Execute }
        catch { $failed = $true }
        Assert-True -Condition $failed -Message "$scenario must fail the deployment."
        Assert-True -Condition (!$script:Calls.Exists({ param($call) $call -match 'execute-change-set' })) -Message "$scenario must not execute changes."
    }

    $unconfigured = '{"foundation":{"VpcId":"REPLACE_ME"}}' | ConvertFrom-Json
    $failed = $false
    try { $null = Get-SectionParameters -Config $unconfigured -Section foundation -Environment dev }
    catch { $failed = $true }
    Assert-True -Condition $failed -Message 'Unconfigured environment values must be rejected.'
    Write-Host 'All deployment planning regression checks passed (no AWS calls made).'
}
finally { $env:CF_EXECUTION_ROLE_ARN = $originalRole }
