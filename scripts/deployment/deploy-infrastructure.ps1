[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('dev', 'test', 'staging', 'production')][string]$Environment,
    [Parameter(Mandatory)][ValidateSet('foundation', 'data', 'web')][string]$Stack,
    [switch]$Execute
)
. "$PSScriptRoot/Common.ps1"
$config = Get-DeploymentConfig -Environment $Environment
$parameters = Get-SectionParameters -Config $config -Section $Stack -Environment $Environment
$prefix = "property-intelligence-$Environment"
if ($Stack -eq 'data') {
    $foundation = Get-StackOutputs -Name "$prefix-foundation"
    foreach ($key in @('VpcId', 'PrivateSubnetIds', 'ApplicationSecurityGroupId')) {
        $parameters[$key] = $foundation[$key]
    }
}
Deploy-Stack -Name "$prefix-$Stack" -Template $Stack -Parameters $parameters -Execute:$Execute
