# Database Architecture

## Status
Draft v1

## Purpose
This document defines the database architecture for ClaimsOS.

ClaimsOS will use PostgreSQL as the primary relational database with support for geospatial analysis through PostGIS and AI vector search through pgvector.

## Database Platform
Primary database:
- PostgreSQL

Required extensions:
- PostGIS
- pgvector
- uuid-ossp or equivalent UUID generation strategy, if needed

## Design Principles

### Relational First
Core business data should be modeled relationally.

### Schema Per Bounded Context
PostgreSQL schemas should separate bounded contexts.

### Multi-Tenant Isolation
Tenant-owned data must include `organization_id`.

### Auditability
Critical entities must include audit fields.

### Soft Deletes
Business entities should generally be soft deleted, not physically deleted.

### Geospatial Native
Property coordinates, parcel geometry, storm paths, and weather impacts should use PostGIS types where appropriate.

### Vector Native
Document chunks, AI memory, and semantic search records should use pgvector.

## PostgreSQL Schemas
Recommended schemas:
- identity
- organizations
- properties
- weather
- claims
- documents
- communications
- workflow
- playbooks
- timeline
- ai
- reporting
- billing
- audit

## Core Table Standards
Most tenant-owned tables should include:

```sql
id uuid primary key,
organization_id uuid not null,
created_at timestamptz not null,
created_by uuid null,
modified_at timestamptz null,
modified_by uuid null,
is_deleted boolean not null default false,
deleted_at timestamptz null,
deleted_by uuid null
```

Exact concurrency implementation may vary depending on EF Core configuration.

## Multi-Tenancy Model
Initial strategy:
- Shared database
- Shared schema
- Tenant isolation using `organization_id`

Future strategy:
- Optional dedicated database per enterprise tenant
- Optional read replicas for reporting
- Optional data warehouse export

Application queries must always be tenant-aware.

Row-level security may be evaluated after MVP.

## Core Schemas and Responsibilities

### organizations
Owns tenants, offices, teams, and tenant settings.

### identity
Owns application user profiles, roles, and permissions.

Authentication may be delegated to an external identity provider, but ClaimsOS still requires local user profile and organization mapping.

### properties
Owns property profile, permits, property images, and property risk assessments.

### weather
Owns weather event records and property weather impact relationships.

### claims
Owns claim record, inspections, estimates, coverage decisions, and claim assignments.

### documents
Owns document metadata, versions, OCR results, and storage references.

### communications
Owns emails, letters, SMS records, call logs, and communication templates.

### workflow
Owns workflow instances, stages, tasks, gates, blockers, dependencies, and escalations.

### playbooks
Owns playbook definitions, versions, task templates, document requirements, and communication requirements.

### timeline
Owns chronological events related to claims and properties.

### ai
Owns AI analyses, embeddings, document chunks, recommendations, and risk indicators.

### reporting
Owns report definitions, report executions, and saved dashboard views.

### audit
Owns immutable audit log records.

## PostGIS Usage
Use PostGIS for:
- Property point locations
- Parcel boundaries
- Weather event polygons
- Storm paths
- Impact radius calculations
- Distance queries
- Nearby affected property analysis

Example:

```sql
create index idx_property_location
on properties.property
using gist (location);
```

Example query:

```sql
select *
from weather.weather_event e
where ST_DWithin(e.geometry, :property_location, :distance_meters);
```

## pgvector Usage
Use pgvector for:
- Document chunk embeddings
- Policy search
- Claim file semantic search
- AI context retrieval
- Playbook guidance retrieval

Example table:

```sql
create table ai.document_chunk (
    id uuid primary key,
    organization_id uuid not null,
    document_id uuid not null,
    chunk_index integer not null,
    chunk_text text not null,
    embedding vector(3072),
    created_at timestamptz not null
);
```

Embedding dimension should be configurable based on the selected embedding model.

## Indexing Strategy
Required index categories:
- Tenant indexes on `organization_id`
- Foreign key indexes
- Date indexes for deadlines and timeline queries
- GIST indexes for PostGIS geometry
- Vector indexes for pgvector search
- Partial indexes for active records
- Unique indexes for natural identifiers where appropriate

Example:

```sql
create index idx_claim_org_status
on claims.claim (organization_id, status)
where is_deleted = false;
```

## Migration Strategy
Use EF Core migrations for application schema changes.

Consider separating:
- Application schema migrations
- Database extension setup
- Seed data
- Reference data
- Playbook seed data

A dedicated migration runner may be useful for deployments.

## Data Retention
ClaimsOS should support configurable retention policies.

Important data types:
- Claims
- Documents
- Communications
- Audit logs
- AI analyses
- Generated reports

Deletion should be carefully controlled due to legal and claim-handling implications.

## Backup and Recovery
Requirements:
- Automated daily backups
- Point-in-time recovery
- Environment-specific retention
- Tested restore process
- Separate backup strategy for object storage

## MVP Scope
MVP database architecture should implement:
- organizations
- identity
- properties
- claims
- documents
- workflow
- playbooks
- timeline
- audit

Weather and AI schemas may be partially implemented depending on MVP priorities.

## Agentic Development Notes
Agents should use this document to generate:
- PostgreSQL schema layout
- EF Core DbContext boundaries
- Entity configurations
- Migration strategy
- Index definitions
- Tenant-aware repository patterns
- PostGIS and pgvector integration code
