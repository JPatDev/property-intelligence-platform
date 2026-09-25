# AWS infrastructure

CloudFormation templates deploy alongside the existing application structure.
No source projects need to move.

| Stack | Responsibility | Deploy after |
| --- | --- | --- |
| bootstrap/github-oidc | GitHub identity provider and scoped deployment role | Existing CloudFormation execution role |
| foundation | ECR, ECS cluster, application/load-balancer security groups | Existing VPC and subnets |
| data | Private RDS PostgreSQL, document S3 bucket, SQS and dead-letter queue | foundation |
| web | Private frontend S3 bucket, CloudFront distribution | bootstrap |
| runtime | API and migration task definitions, IAM roles, CloudWatch logs | foundation, data, web, database users/secrets |
| application | HTTPS ALB, Route 53 alias, ECS service | runtime and successful migration task |

The existing VPC must have two public subnets and two private subnets across at
least two Availability Zones. Private tasks need outbound access to ECR, S3,
Secrets Manager, CloudWatch, and the configured JWT authority. Network creation
is intentionally an account prerequisite; these templates do not provision NAT
gateways or alter existing routes.

Environment files contain identifiers and sizing only. `REPLACE_ME` values fail
deployment early. Never commit connection strings, passwords, access keys, or
tokens. Secrets Manager ARNs are references, not secret values.

Run validation from the repository root:

```powershell
python -m pip install -r infrastructure/requirements-dev.txt
cfn-lint infrastructure/bootstrap/*.yml infrastructure/templates/*.yml
./scripts/deployment/Test-DeploymentStructure.ps1
```

See [deployment operations](../docs/operations/deployment.md) for the complete setup.
