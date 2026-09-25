# AWS SaaS Architecture

## Decision
AWS is the platform deployment target. AWS CloudFormation (YAML) is the preferred infrastructure-as-code approach, reflecting the team's experience.

The services below are proposed. Infrastructure has not yet been provisioned; region, sizing, and availability targets remain to be selected.

## Proposed Core Services
- Amazon ECS with AWS Fargate for the ASP.NET Core application and workers
- Amazon ECR for container images
- Application Load Balancer for application ingress
- Amazon RDS for PostgreSQL; validate PostGIS and pgvector versions against the selected engine version
- Amazon S3 for documents and object storage
- AWS Secrets Manager for secrets
- Amazon SQS for background jobs; Amazon SNS for fan-out where needed
- Amazon Textract for OCR, subject to document extraction quality tests
- Amazon CloudWatch for logs, metrics, and alarms
- OpenTelemetry for tracing
- Amazon CloudFront for edge delivery where needed

## AI Provider
Evaluate Amazon Bedrock and direct OpenAI APIs against analysis quality, privacy, regional availability, and cost. AWS hosting does not determine the model provider.

## Deployment Strategy
- Maintain CloudFormation YAML templates under `infrastructure/`.
- Parameterize Dev, Test, Staging, and Production with isolated configuration and data.
- Use GitHub Actions with AWS role federation for deployment credentials.
- Validate templates and review change sets before deployment.
- Define retention and backup policies for persistent resources.

## Non-Functional Requirements
- Define availability and disaster recovery targets before selecting production redundancy.
- Keep the database private and use least-privilege workload permissions.
- Encrypt data in transit and at rest.
- Configure autoscaling, health checks, log retention, and alarms.
- Test database restores and document recovery procedures.
- Establish cost budgets before provisioning environments.

## References
- [ECS with CloudFormation](https://docs.aws.amazon.com/en_gb/AmazonECS/latest/developerguide/ecs-with-cloudformation.html)
- [CloudFormation change sets](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-updating-stacks-changesets.html)
- [RDS PostgreSQL extension versions](https://docs.aws.amazon.com/AmazonRDS/latest/PostgreSQLReleaseNotes/postgresql-extensions.html)
