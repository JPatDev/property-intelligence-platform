# Database Schema Overview

## Platform Database

PostgreSQL

Extensions:

* PostGIS
* pgvector

## Schemas

identity
organizations
properties
claims
documents
workflow
weather
timeline
ai
reporting
billing

## Core Tables

### organizations.organization

Tenant information

### identity.user

User accounts

### properties.property

Property records

### claims.claim

Claim records

### workflow.workflow

Workflow definitions

### workflow.task

Workflow tasks

### workflow.completion_gate

Task completion requirements

### documents.document

Document records

### weather.weather_event

Weather intelligence

### timeline.timeline_event

Chronological events

### ai.claim_analysis

AI-generated recommendations

## Multi-Tenancy

Every tenant-owned table must include:

* organization_id

## Auditing

All critical tables must include:

* created_by
* created_at
* modified_by
* modified_at

## Soft Deletes

All tenant-owned entities must support:

* is_deleted
* deleted_at
* deleted_by
