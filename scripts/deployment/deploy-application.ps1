[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('dev', 'test', 'staging', 'production')][string]$Environment
)
. "$PSScriptRoot/Common.ps1"
$config = Get-DeploymentConfig -Environment $Environment
# Fail on unconfigured settings before building or changing any AWS resources.
$runtime = Get-SectionParameters -Config $config -Section runtime -Environment $Environment
$application = Get-SectionParameters -Config $config -Section application -Environment $Environment
$prefix = "property-intelligence-$Environment"
$foundation = Get-StackOutputs -Name "$prefix-foundation"
$null = Get-StackOutputs -Name "$prefix-data"
$web = Get-StackOutputs -Name "$prefix-web"
$runtime.WebOrigin = $web.WebOrigin
$apiUrl = "https://$($application.ApiDomainName)"
$release = if ($env:GITHUB_SHA) { "$($env:GITHUB_SHA)-$($env:GITHUB_RUN_ID)-$($env:GITHUB_RUN_ATTEMPT)" } else { [guid]::NewGuid().ToString('N') }
$imageTag = "$($foundation.RepositoryUri):$release"
$registry = $foundation.RepositoryUri.Split('/')[0]
$priorApiUrl = $env:VITE_API_BASE_URL
Push-Location $script:RepositoryRoot
try {
    # Build frontend before touching the running application.
    $env:VITE_API_BASE_URL = $apiUrl
    & npm --prefix src/WebApp/property-intelligence-web run build
    if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
    & docker build --platform linux/amd64 --file src/WebApi/PropertyIntelligence.Api/Dockerfile --tag $imageTag .
    if ($LASTEXITCODE -ne 0) { throw 'Container build failed.' }
    $password = Invoke-Aws -Arguments @('ecr', 'get-login-password')
    $password | & docker login --username AWS --password-stdin $registry
    $password = $null
    if ($LASTEXITCODE -ne 0) { throw 'ECR login failed.' }
    & docker push $imageTag
    if ($LASTEXITCODE -ne 0) { throw 'Image push failed.' }
    $digest = (Invoke-Aws -Arguments @('ecr', 'describe-images', '--repository-name', $foundation.RepositoryName,
        '--image-ids', "imageTag=$release", '--query', 'imageDetails[0].imageDigest', '--output', 'text')).Trim()
    if ($digest -notmatch '^sha256:[a-f0-9]{64}$') { throw 'ECR did not return an image digest.' }
    $runtime.ImageUri = "$($foundation.RepositoryUri)@$digest"
    Deploy-Stack -Name "$prefix-runtime" -Template runtime -Parameters $runtime -Execute
    $tasks = Get-StackOutputs -Name "$prefix-runtime"

    # A separate task definition lets schema changes finish before ECS rolls out the API.
    $networkFile = Write-TemporaryJson -Value @{
        awsvpcConfiguration = @{
            subnets = @($foundation.PrivateSubnetIds.Split(','))
            securityGroups = @($foundation.ApplicationSecurityGroupId)
            assignPublicIp = 'DISABLED'
        }
    }
    try {
        $result = (Invoke-Aws -Arguments @('ecs', 'run-task', '--cluster', $foundation.ClusterArn,
            '--launch-type', 'FARGATE', '--task-definition', $tasks.MigrationTaskDefinitionArn,
            '--network-configuration', "file://$networkFile", '--count', '1', '--output', 'json')) | ConvertFrom-Json
        if (@($result.failures).Count -gt 0 -or @($result.tasks).Count -ne 1) {
            throw "Migration task could not start: $($result | ConvertTo-Json -Depth 8)"
        }
        $taskArn = $result.tasks[0].taskArn
        try {
            $null = Invoke-Aws -Arguments @('ecs', 'wait', 'tasks-stopped', '--cluster', $foundation.ClusterArn, '--tasks', $taskArn)
        }
        catch {
            $null = Invoke-Aws -Arguments @('ecs', 'stop-task', '--cluster', $foundation.ClusterArn, '--task', $taskArn, '--reason', 'Migration waiter failed or timed out')
            throw
        }
        $result = (Invoke-Aws -Arguments @('ecs', 'describe-tasks', '--cluster', $foundation.ClusterArn,
            '--tasks', $taskArn, '--output', 'json')) | ConvertFrom-Json
        if (@($result.failures).Count -gt 0 -or @($result.tasks).Count -ne 1) { throw 'Cannot inspect migration result.' }
        $container = @($result.tasks[0].containers | Where-Object name -eq migration)
        if ($container.Count -ne 1 -or !$container[0].PSObject.Properties['exitCode'] -or $container[0].exitCode -ne 0) {
            throw "Migration failed. Inspect $taskArn and /property-intelligence/$Environment/api in CloudWatch. The API service has not been updated."
        }
    }
    finally { Remove-Item -LiteralPath $networkFile -Force }

    foreach ($key in @('VpcId', 'PublicSubnetIds', 'PrivateSubnetIds', 'ApplicationSecurityGroupId', 'LoadBalancerSecurityGroupId', 'ClusterArn')) {
        $application[$key] = $foundation[$key]
    }
    $application.ApiTaskDefinitionArn = $tasks.ApiTaskDefinitionArn
    Deploy-Stack -Name "$prefix-application" -Template application -Parameters $application -Execute
    $healthy = $false
    for ($attempt = 0; $attempt -lt 12; $attempt++) {
        try {
            $health = Invoke-RestMethod -Uri "$apiUrl/health" -TimeoutSec 10
            if ($health.status -eq 'healthy') { $healthy = $true; break }
        }
        catch { Write-Host 'Waiting for API HTTPS health check and DNS propagation.' }
        Start-Sleep -Seconds 5
    }
    if (!$healthy) { throw 'API health check failed; frontend publication was skipped.' }

    # Preserve previous hashed assets for clients still running the old frontend.
    $null = Invoke-Aws -Arguments @('s3', 'sync', 'src/WebApp/property-intelligence-web/dist/',
        "s3://$($web.WebBucketName)/", '--exclude', 'index.html', '--cache-control', 'public,max-age=31536000,immutable')
    $null = Invoke-Aws -Arguments @('s3', 'cp', 'src/WebApp/property-intelligence-web/dist/index.html',
        "s3://$($web.WebBucketName)/index.html", '--cache-control', 'no-cache', '--content-type', 'text/html')
    $null = Invoke-Aws -Arguments @('cloudfront', 'create-invalidation', '--distribution-id', $web.DistributionId, '--paths', '/*')
    Write-Host "Deployed image $($runtime.ImageUri)"
    Write-Host "API: $apiUrl | Frontend: $($web.WebOrigin)"
}
finally {
    $env:VITE_API_BASE_URL = $priorApiUrl
    & docker logout $registry
    Pop-Location
}
