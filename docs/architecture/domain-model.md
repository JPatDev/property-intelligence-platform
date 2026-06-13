# Full Domain Model Specification

Version: 1.0

Status: Foundational Architecture

Product: ClaimsOS

---

# Purpose

The Domain Model defines the core business entities, relationships, aggregates, bounded contexts, and ownership rules within ClaimsOS.

This document serves as the authoritative source for:

* Database Design
* EF Core Models
* API Contracts
* Domain Events
* Workflow Rules
* AI Context Retrieval
* Reporting

---

# Design Principles

## Business First

The model reflects claim-handling operations rather than database design.

## Aggregate Consistency

Each aggregate owns its data and business rules.

## Multi-Tenant First

Every tenant-owned entity contains:

```text id="m5e73n"
OrganizationId
```

## Event Driven

Significant actions emit domain events.

## Auditability

Every critical business action must be traceable.

---

# Bounded Contexts

```text id="s40pnm"
Identity

Organizations

Properties

Claims

Workflow

Documents

Communications

Weather

Timeline

AI

Reporting

Billing
```

---

# High-Level Domain Map

```text id="kqtxhs"
Organization
│
├── Users
├── Properties
├── Claims
├── Playbooks
├── Reports
└── Billing
```

---

# Organization Aggregate

## Purpose

Represents a tenant.

## Aggregate Root

```text id="v0yspz"
Organization
```

## Entities

### Organization

Fields:

```text id="2g9s3w"
Id
Name
Status
CreatedAt
SubscriptionPlan
Settings
```

### Office

```text id="ji4mzf"
Id
OrganizationId
Name
Address
Phone
```

### Team

```text id="v6vdx5"
Id
OrganizationId
Name
```

---

# User Aggregate

## Aggregate Root

```text id="h1odn3"
User
```

## Fields

```text id="0z4m1y"
Id
OrganizationId
Email
FirstName
LastName
Role
Status
```

## Roles

```text id="fn93s7"
Administrator

Manager

Public Adjuster

Assistant

Attorney

Appraiser

Umpire

Contractor

ReadOnly
```

---

# Property Aggregate

## Aggregate Root

```text id="7y4xf0"
Property
```

## Purpose

Central location for all property intelligence.

---

## Property Entity

Fields:

```text id="4nyewv"
Id
OrganizationId
Address
ParcelNumber
County
State
ZipCode

Latitude
Longitude

YearBuilt
RoofType
SquareFeet

CreatedAt
```

---

## Value Objects

### Address

```text id="y40pdh"
Street
City
State
ZipCode
```

### Coordinates

```text id="e7fptz"
Latitude
Longitude
```

---

## Child Entities

### Permit

```text id="l9aw4v"
Id
PropertyId
PermitNumber
PermitType
IssuedDate
```

### PropertyImage

```text id="hqws69"
Id
PropertyId
Source
CaptureDate
```

### PropertyRiskAssessment

```text id="8d2r56"
Id
PropertyId
RiskScore
CalculatedAt
```

---

# Weather Aggregate

## Aggregate Root

```text id="gyv3n6"
WeatherEvent
```

---

## WeatherEvent

```text id="cjlwm4"
Id
EventType
EventDate
Severity
Geometry
Source
```

Types:

```text id="dfsm5x"
Hail

Wind

Tornado

Hurricane

Flood
```

---

## PropertyWeatherImpact

Relationship entity.

```text id="pqhlqq"
PropertyId
WeatherEventId
Distance
ImpactScore
```

---

# Claim Aggregate

## Aggregate Root

```text id="0jkkwm"
Claim
```

This is the most important aggregate.

---

## Claim Entity

Fields:

```text id="0j2l8t"
Id
OrganizationId

ClaimNumber
PolicyNumber

PropertyId

DateOfLoss

CarrierId

ClaimStatus

CurrentStage

AssignedAdjusterId
```

---

## Claim Status

```text id="rjlwmn"
Lead

Intake

Active

Negotiation

Appraisal

Litigation

Closed

Archived
```

---

## Child Entities

### Inspection

```text id="zuc8k9"
Id
ClaimId
InspectionDate
InspectorId
```

### Estimate

```text id="1k0ay8"
Id
ClaimId
EstimateAmount
PreparedDate
```

### CoverageDecision

```text id="83xpbz"
Id
ClaimId
DecisionType
Amount
```

---

# Workflow Aggregate

## Aggregate Root

```text id="otx6ow"
WorkflowInstance
```

---

## WorkflowDefinition

Template.

```text id="9b8qk6"
Id
Name
Version
Status
```

---

## WorkflowInstance

Runtime execution.

```text id="upmuwm"
Id
ClaimId
WorkflowDefinitionId
Status
```

---

## WorkflowStage

```text id="wuw7xp"
Id
WorkflowInstanceId
Name
Status
Sequence
```

---

## WorkflowTask

```text id="yzowr4"
Id
WorkflowStageId

Name
Description

AssignedUserId

DueDate

Priority

Status
```

---

## CompletionGate

```text id="nvfl2k"
Id
TaskId
GateType
Configuration
```

---

## TaskDependency

```text id="8xhmlq"
TaskId
DependsOnTaskId
```

---

# Playbook Aggregate

## Aggregate Root

```text id="vl5w9s"
Playbook
```

---

## Playbook

```text id="3a9wcr"
Id
OrganizationId

Name

ClaimType

Version

Status
```

---

## Child Entities

### PlaybookStage

### PlaybookTask

### PlaybookDocumentRequirement

### PlaybookCommunicationRequirement

### PlaybookRule

---

# Document Aggregate

## Aggregate Root

```text id="79pcuv"
Document
```

---

## Document

```text id="xtdlkb"
Id

OrganizationId

ClaimId

PropertyId

DocumentType

FileName

StoragePath

Version

Status
```

---

## Document Types

```text id="4bd3ja"
Contract

Policy

Estimate

Inspection Report

Photo

Correspondence

Carrier Letter

Invoice
```

---

## Child Entities

### OCRResult

```text id="9gup50"
Id
DocumentId
ExtractedText
```

### DocumentVersion

```text id="pk5mk0"
Id
DocumentId
VersionNumber
```

---

# Communication Aggregate

## Aggregate Root

```text id="k3r5s2"
Communication
```

---

## Communication

```text id="x8sdl4"
Id

ClaimId

Direction

Channel

Subject

SentAt
```

---

## Channels

```text id="3c80i4"
Email

SMS

Phone

Letter

Portal
```

---

# Timeline Aggregate

## Aggregate Root

```text id="k1xf7j"
TimelineEvent
```

---

## TimelineEvent

```text id="o7j1aj"
Id

ClaimId

PropertyId

EventType

OccurredAt

Description
```

---

## Event Types

```text id="e9m5ru"
Storm

Permit

Inspection

Document

Communication

Workflow

Settlement
```

---

# AI Aggregate

## Aggregate Root

```text id="ew6yn7"
ClaimAnalysis
```

---

## ClaimAnalysis

```text id="2bz06e"
Id

ClaimId

ReadinessScore

AnalysisDate
```

---

## Child Entities

### MissingEvidence

```text id="hffwvr"
Id
ClaimAnalysisId
Description
Severity
```

### Recommendation

```text id="iay1y5"
Id
ClaimAnalysisId
Priority
Description
```

### RiskIndicator

```text id="9j40nf"
Id
ClaimAnalysisId
RiskType
Severity
```

---

# Carrier Intelligence Aggregate

## Aggregate Root

```text id="1mry3t"
Carrier
```

---

## Carrier

```text id="t1k5t7"
Id

Name

NAICCode

Website
```

---

## CarrierProfile

```text id="tdsrti"
Id

CarrierId

AverageResponseTime

EscalationNotes
```

---

## CarrierStrategy

```text id="aj44zx"
Id

CarrierId

ClaimType

RecommendedApproach
```

---

# Reporting Aggregate

## Aggregate Root

```text id="5ww8wl"
ReportDefinition
```

---

## ReportDefinition

```text id="1vuk0n"
Id
Name
Category
```

---

## ReportExecution

```text id="n7r5za"
Id
ReportDefinitionId
GeneratedAt
```

---

# Billing Aggregate

## Aggregate Root

```text id="1zcf4h"
Subscription
```

---

## Subscription

```text id="ij9t6u"
Id
OrganizationId
Plan
Status
RenewalDate
```

---

# Core Domain Events

```text id="9nxs6m"
OrganizationCreated

UserInvited

PropertyCreated

PermitAdded

WeatherEventImported

ClaimCreated

ClaimAssigned

InspectionScheduled

DocumentUploaded

WorkflowStarted

TaskAssigned

TaskCompleted

TaskBlocked

StageAdvanced

CarrierNotified

DeadlineMissed

ClaimEscalated

ClaimClosed
```

---

# Aggregate Ownership Rules

## Property Owns

```text id="e36q4n"
Permits

Property Images

Property Risk Assessments
```

## Claim Owns

```text id="r0p2jy"
Inspections

Estimates

Coverage Decisions
```

## Workflow Owns

```text id="ccdk8g"
Stages

Tasks

Completion Gates

Dependencies
```

## Document Owns

```text id="jlwmzb"
Versions

OCR Results
```

---

# Database Mapping Strategy

Each aggregate maps to a PostgreSQL schema.

```text id="bz4zj8"
identity

organizations

properties

claims

workflow

documents

communications

weather

timeline

ai

reporting

billing
```

---

# MVP Aggregate Scope

Version 1 should fully implement:

```text id="we6p1y"
Organization

User

Property

Claim

Workflow

Workflow Task

Completion Gate

Document

Communication

Timeline Event
```

Version 2 adds:

```text id="vvjvcv"
Carrier Intelligence

AI Analysis

Playbook Marketplace

Advanced Reporting
```

---

# Domain Model Guiding Principle

The platform is not a CRM.

The platform is an operational system that continuously answers:

```text id="b7qzg4"
What happened?

What must happen next?

Who is responsible?

What evidence exists?

Can this file advance?
```

Every aggregate exists to support those questions.
