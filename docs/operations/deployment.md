# GitHub Actions and AWS deployment

## Scope and current behavior

The repository preserves every existing solution/project path, business module,
test, and local application setting. Deployment files are additive. GitHub Actions
runs CI on pull requests and `main`; AWS workflows run only through
`workflow_dispatch` on the repository's default branch.

This is a deployment baseline, not a provisioned environment. The templates use
an existing network and DNS zone. AWS credentials, account identifiers, a JWT
issuer, and database users are supplied during account setup.

## One-time AWS and GitHub setup

1. Choose a region and an existing VPC. Supply two public and two private subnet
   IDs across at least two Availability Zones. Public subnets need internet-gateway
   routes; private tasks need outbound connectivity for AWS services and JWT
   discovery. RDS remains private. Use isolated data and preferably separate AWS
   accounts for production. Resource and role names assume one deployment of each
   environment per account; use separate accounts for duplicate environment names.
2. Create a CloudFormation execution role trusted by `cloudformation.amazonaws.com`.
   Its account-reviewed policy must allow the resource operations represented in
   `infrastructure/templates/`: EC2 security groups, ECR, ECS, IAM workload roles
   and PassRole, CloudWatch Logs, RDS, Secrets Manager for RDS-managed credentials,
   S3, SQS, CloudFront, ELBv2, and records in the selected Route 53 hosted zone.
   Allow creation of required service-linked roles on first use. This privileged
   account role is intentionally not self-provisioned by the pipeline.
3. Deploy `infrastructure/bootstrap/github-oidc.yml` using an existing administrative
   AWS session with `CAPABILITY_NAMED_IAM`. Supply `GitHubRepository`,
   `EnvironmentName`, and `CloudFormationExecutionRoleArn`. If the account already
   has the GitHub OIDC provider, supply `ExistingOidcProviderArn`. Reuse the first
   provider output for subsequent environments in the same account.
4. Create GitHub environments named `dev`, `test`, `staging`, and `production` as
   needed. Restrict their deployment branches to the default branch. Set required
   reviewers for production where your GitHub plan supports them; a workflow's
   `environment` field alone does not configure approval rules.
5. Set these **GitHub environment variables**:

   | Variable | Value |
   | --- | --- |
   | `AWS_REGION` | Selected deployment region |
   | `AWS_DEPLOY_ROLE_ARN` | Bootstrap stack's `DeploymentRoleArn` output |
   | `CF_EXECUTION_ROLE_ARN` | Account-managed CloudFormation execution role |

6. Replace values in `infrastructure/parameters/<environment>.json`. Configure an
   existing public Route 53 hosted zone, an API hostname in that zone, and an
   issued ACM certificate for that hostname in the deployment region. The API
   receives an HTTPS listener; the frontend uses CloudFront's default HTTPS domain.
7. Configure the JWT authority and audience expected by the existing identity
   module. AWS containers always use `Production` ASP.NET settings and `JwtBearer`,
   including the `dev` AWS environment. Local development header authentication
   is not enabled in these task definitions. Identity-provider provisioning and
   frontend sign-in remain separate application work; this infrastructure does
   not add a login flow.

OIDC provides temporary AWS credentials without stored AWS access keys. Its trust
policy binds the repository and GitHub environment. If the organization customizes
OIDC subject claims, update the bootstrap trust policy accordingly.
[GitHub OIDC documentation](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-aws)

## Initial stack deployment

Run **Deploy AWS infrastructure**, select the environment, and deploy these stacks:

1. `foundation`
2. `data`
3. `web`

The default `execute: false` creates a preview change set. Review its ARN and
resource changes in the workflow log. To apply exactly that reviewed change set,
execute it in the AWS console with an authorized identity. Running the workflow
again with `execute: true` creates and executes a **new** change set from that run's
commit. CloudFormation previews are not reservations or guarantees of success.
[CloudFormation change sets](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-updating-stacks-changesets.html)

The application workflow requires all three stacks to be complete. Stack names
are `property-intelligence-<environment>-<stack>` and dependency values are read
from stack outputs, rather than copied into parameter files.

## Database initialization

RDS creates a managed administrator secret, exposed only by ARN in the data
stack. Use an authorized private administrative connection to the new database:

1. Install PostGIS with the RDS administrator before running module migrations.
   Confirm the selected RDS PostgreSQL 16 minor version supports the extensions
   your application uses. pgvector can be enabled when an implemented feature needs it.
2. Create a schema-owner login for migrations and a separate runtime login with
   DML permissions. Give the migration login permission to create/own the module
   schemas and tables. Give the runtime login schema USAGE, table SELECT/INSERT/
   UPDATE/DELETE, and necessary sequence permissions. Configure default privileges
   **for the migration owner** so newly migrated tables inherit runtime access.
   Current schemas are defined by each module's DbContext and migrations; review
   those before granting permissions. Do not use the RDS administrator as the API login.
3. Store each login's full Npgsql connection string in a separate Secrets Manager
   secret as **plain text**, not a JSON object. Include the RDS hostname, database
   `property_intelligence`, username, password, `SSL Mode=VerifyFull`, and
   `Root Certificate=/app/certs/rds-global-bundle.pem`. The image includes the
   public AWS RDS trust bundle. Quote
   password values according to Npgsql connection-string rules. The baseline expects
   the default Secrets Manager encryption key; custom KMS keys also need decrypt
   permissions on the matching ECS execution role.
4. Set `ApplicationConnectionSecretArn` and `MigrationConnectionSecretArn` in the
   environment file. Both task definitions inject `ConnectionStrings__Workflow`;
   all six existing module factories/configurations already fall back to this key.

The API and migrations use different ECS execution roles, so the API execution
role cannot retrieve the migration credential. Secrets are not fetched into
GitHub logs or passed as command-line arguments. Credential rotation requires
fresh tasks to pick up updated secret values.

## Application release

Run **Deploy AWS application** for the target environment. The workflow:

1. Runs the reusable CI workflow, including backend/PostGIS tests, frontend checks,
   template validation, container build, and production-mode health smoke test.
2. Builds the frontend with `VITE_API_BASE_URL=https://<ApiDomainName>`.
3. Builds the API container and all six EF migration bundles from the same commit.
4. Pushes a unique commit/run tag to ECR and resolves its immutable image digest.
5. Updates the `runtime` stack with that digest. This creates task definitions
   without changing the currently running API service.
6. Runs the migration task in the private subnets and requires exit code zero.
7. Updates the `application` stack, waits for CloudFormation/ECS deployment health,
   and checks the public HTTPS `/health` endpoint.
8. Uploads the frontend assets, then `index.html`, and requests a CloudFront
   invalidation. Old hashed assets are retained for clients using a previous page.

Both deployment workflows share an environment concurrency group. A release does
not interrupt an in-flight release. Do not bypass that serialization with competing
manual stack updates. Migration tasks are stopped if their waiter times out;
inspect ECS for running migration tasks before retrying a cancelled GitHub job.

The health endpoint currently checks process liveness, not database readiness.
Migration success provides a schema check, but full authenticated application
verification still requires a real issuer/token. S3 and SQS resources are provisioned
for future adapters; this change does not replace existing module persistence or
implement document storage/messaging integrations.

## Local tooling and container build

Deployment scripts require PowerShell 7, AWS CLI v2, Docker with Linux containers,
and Node/npm. GitHub-hosted Ubuntu runners supply PowerShell, AWS CLI, and Docker;
workflows install the selected Node/.NET/Python versions.

```powershell
docker build --platform linux/amd64 -f src/WebApi/PropertyIntelligence.Api/Dockerfile -t property-intelligence:local .
./scripts/deployment/Test-DeploymentStructure.ps1
```

The Docker build context must be the repository root. The Dockerfile preserves
module references and builds framework-dependent Linux x64 migration bundles.
For a local cloud deployment from an authorized AWS session:

```powershell
$env:AWS_REGION = 'us-east-1'
$env:CF_EXECUTION_ROLE_ARN = '<your execution role ARN>'
./scripts/deployment/deploy-infrastructure.ps1 -Environment dev -Stack foundation
# Add -Execute after reviewing the intended change.
```

## Recovery and operating limits

- ECS uses a deployment circuit breaker with rollback; failed migrations stop the
  pipeline before the service changes. A partially applied multi-module migration
  is not automatically undone. Review its database state before rerunning.
- Use backward-compatible schema changes because old API tasks continue running
  during migrations. Database rollback requires a deliberate recovery plan; app
  rollback does not undo schema changes.
- For API rollback, update the application stack's `ApiTaskDefinitionArn` to a
  known-good retained task-definition revision through a reviewed change set.
  Preserve other parameters. Check schema compatibility first. Frontend bucket
  versioning retains previous `index.html` versions; restore the intended version
  and invalidate CloudFront when reverting the frontend.
- Each environment release currently rebuilds from the chosen default-branch
  commit. Cross-environment promotion of the exact same built artifact is future
  work; these workflows do not claim to provide it.
- Production defaults to two API tasks and Multi-AZ RDS, with database deletion
  protection. Data buckets and ECR are retained; database replacement/deletion
  snapshots are retained. Removing stacks does not remove all retained resources.
- Autoscaling, operational alarms, WAF, custom frontend domains, and budget alerts
  are not included in this baseline. Set sizing and cost controls before deployment;
  RDS, ALB, Fargate, CloudFront, and the supplied network incur AWS charges.
- Application publishing retains old assets and images for rollback. Add a reviewed
  retention policy once release and rollback windows are established.

CI uses the existing tests against a disposable PostGIS database. Template linting
checks CloudFormation schemas, not account quotas, networking routes, issuer
configuration, or actual AWS permissions. The first configured dev deployment is
the end-to-end infrastructure acceptance check.
