# ClaimsOS Project Brain

Version: 1.0

Status: Master AI Context Document

---

# Project Overview

ClaimsOS is a multi-tenant SaaS platform designed for public adjusters, appraisers, umpires, attorneys, contractors, and insurance dispute professionals.

The platform combines:

* Property Intelligence
* Claims Management
* Workflow Enforcement
* AI Claim Strategy
* Public Adjusting Operations

ClaimsOS is not a CRM at the momen, but we might want to do include that in the future.

ClaimsOS is an operating system for insurance claim professionals.

---

# Core Product Vision

The platform should continuously answer:

* What happened?
* What evidence exists?
* What is missing?
* What must happen next?
* Who owns the next action?
* Can the file advance?

The system should actively guide claim progression.

---

# Core Principle

> No File Left Behind

Every active claim must always have:

* Current Stage
* Current Status
* Assigned Owner
* Next Action
* Due Date
* Completion Requirements
* Blockers
* Audit History

No claim should ever become stagnant without visibility.

---

# Strategic Differentiators

## Property Intelligence

Provide evidence and context.

Capabilities:

* Property profiles
* Permit history
* Weather correlation
* Storm intelligence
* Property timelines
* Risk scoring
* Historical imagery references

---

## Workflow Enforcement

The workflow engine controls claim progression.
The workflows are called "pipe lines".

ClaimsOS does not allow users to bypass required processes.

---

## Controlled Task Completion

Critical tasks cannot be marked complete until required evidence exists.

Examples:

* Required documents
* Required communications
* Required approvals
* Required notes
* Required field values

This is a primary differentiator.

---

## Playbooks

Playbooks provide reusable operating procedures.

Examples:

* Florida Wind Claim
* Florida Water Claim
* Hurricane Claim
* Hail Claim
* Commercial Roof Claim
* Appraisal Matter
* Umpire Matter

Playbooks generate:

* Workflow stages
* Tasks
* Deadlines
* Document requirements
* Communication requirements
* AI guidance

---

## AI Claim Strategy

AI should assist users by:

* Identifying missing evidence
* Evaluating claim readiness
* Recommending next actions
* Summarizing documents
* Analyzing policies
* Supporting negotiation strategy

AI assists users but does not make final decisions.

---

# Product Philosophy

ClaimsOS should function as:

Property Intelligence
+
Claims Management
+
Workflow Enforcement
+
AI Guidance
+
Public Adjusting Operating System

The platform should become the operational center of a public adjusting business.

---

# Target Market

Primary:

* Public Adjusting Firms

Secondary:

* Insurance Attorneys
* Appraisers
* Umpires
* Roofing Contractors
* Restoration Contractors

Future:

* Carrier-side consultants
* Independent adjusting firms
* Franchise public adjusting organizations

---

# SaaS Model

ClaimsOS is a commercial SaaS platform.

Deployment target:

Microsoft Azure

Future support:

* Azure Marketplace
* Microsoft AppSource

---

# Multi-Tenant Strategy

Architecture:

Shared Database
Shared Schema

Tenant isolation:

organization_id

Every tenant-owned record must include:

```text
organization_id
```

No cross-tenant data access is permitted.

---

# Technology Stack

Backend:

* .NET 9
* ASP.NET Core
* C#
* MediatR
* FluentValidation

Database:

* PostgreSQL
* PostGIS
* pgvector

Storage:

* Azure Blob Storage

Messaging:

* Azure Service Bus

AI:

* Azure OpenAI
* OpenAI APIs

OCR:

* Azure AI Document Intelligence

Observability:

* Azure Monitor
* Application Insights

Infrastructure:

* Azure
* GitHub Actions
* Bicep (preferred)
* Terraform (optional)

---

# Architecture Style

Primary architecture:

Modular Monolith

The application should remain a modular monolith until scale demands decomposition.

Avoid premature microservices.

---

# Bounded Contexts

ClaimsOS consists of the following bounded contexts:

* Identity
* Organizations
* Properties
* Weather
* Claims
* Workflow
* Playbooks
* Documents
* Communications
* Timeline
* AI
* Reporting
* Billing
* Audit

Each context should own its data and business rules.

---

# Most Important Module

The Workflow Engine is the heart of the platform.

Everything ultimately feeds the workflow engine.

Claims
→ Workflow

Documents
→ Workflow

Communications
→ Workflow

Property Intelligence
→ Workflow

AI
→ Workflow

The workflow engine determines:

* Next action
* Stage progression
* Completion eligibility
* Escalations
* Operational health

---

# Workflow Philosophy

Workflows are generated from Playbooks.

Playbooks define:

* Stages
* Tasks
* Completion Gates
* Required Documents
* Required Communications
* Deadlines
* AI Guidance

Workflow instances execute runtime claim processes.

---

# Domain Model Priorities

Highest-value aggregates:

1. Organization
2. User
3. Property
4. Claim
5. WorkflowInstance
6. TaskInstance
7. Document
8. Communication
9. TimelineEvent
10. Playbook

These entities should be implemented first.

---

# Database Strategy

Database Platform:

PostgreSQL

Required Extensions:

* PostGIS
* pgvector

Schema-per-bounded-context design:

* organizations
* identity
* properties
* weather
* claims
* workflow
* playbooks
* documents
* communications
* timeline
* ai
* reporting
* billing
* audit

---

# Coding Standards

Language:

* C# 13
* .NET 9

Patterns:

* Vertical Slice Architecture
* DDD Lite
* CQRS where beneficial
* MediatR
* Repository abstraction only when necessary

Avoid:

* Generic repository anti-pattern
* Over-engineered abstractions
* Premature microservices

---

# Entity Standards

All tenant-owned entities should contain:

```text
Id
OrganizationId

CreatedAt
CreatedBy

ModifiedAt
ModifiedBy

IsDeleted
DeletedAt
DeletedBy
```

Use UUID primary keys.

Soft deletes are preferred.

---

# Security Principles

Requirements:

* Tenant isolation
* RBAC
* Audit logging
* Encryption at rest
* Encryption in transit
* Secret management via Key Vault
* MFA support

ClaimsOS must be designed as a commercial SaaS platform.

---

# Reporting Philosophy

Management dashboards should identify:

* Stalled files
* Missed deadlines
* Missing documents
* Missing communications
* Blocked workflows
* Escalated claims
* Claims without next actions

Operational visibility is a key differentiator.

---

# AI Agent Instructions

When generating code:

1. Respect modular monolith boundaries.
2. Respect bounded contexts.
3. Respect organization_id isolation.
4. Implement audit fields.
5. Prefer maintainability over cleverness.
6. Do not introduce microservices.
7. Treat Workflow as the most important module.
8. Use PostgreSQL-specific features where beneficial.
9. Use PostGIS for geospatial queries.
10. Use pgvector for semantic search.

---

# Current Priorities

1. Finalize architecture
2. Finalize PostgreSQL schema
3. Generate EF Core entities
4. Generate DbContexts
5. Build Workflow Engine
6. Build Playbook Engine
7. Build Claims Module
8. Build Document Module
9. Build AI/RAG Infrastructure
10. Build SaaS Subscription Platform

---

# Long-Term Vision

ClaimsOS becomes the operating system for insurance claim professionals.

The platform should combine:

* Property Intelligence
* Claim Intelligence
* Workflow Enforcement
* Operational Quality Control
* AI Guidance

into a single unified SaaS platform.

The ultimate goal is to provide a complete "Public Adjusting Business in a Box" solution that can be licensed, deployed, and scaled across organizations.
