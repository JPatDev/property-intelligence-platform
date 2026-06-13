# Domain Model

## Core Aggregate Hierarchy

Organization
├── Users
├── Properties
├── Claims
├── Documents
├── Reports
└── Billing

## Property Aggregate

Property
├── Permits
├── Weather Events
├── Inspections
├── Documents
├── Claims
└── Timeline Events

## Claim Aggregate

Claim
├── Workflow
├── Tasks
├── Documents
├── Communications
├── Estimates
├── Inspections
├── Timeline Events
└── AI Analysis

## Workflow Aggregate

Workflow
├── Workflow Stage
├── Workflow Task
├── Completion Gate
├── Dependency Rule
├── Escalation Rule
└── Approval Rule

## Document Aggregate

Document
├── Versions
├── OCR Results
├── Metadata
├── Signatures
└── AI Analysis

## AI Aggregate

Claim Analysis
├── Readiness Score
├── Missing Evidence
├── Carrier Strategy
├── Policy Analysis
└── Recommendations

## Core Value Objects

Address
Parcel Number
Policy Number
Claim Number
Email Address
Phone Number

## Domain Events

ClaimCreated
TaskCompleted
StageAdvanced
DocumentUploaded
DeadlineMissed
WorkflowBlocked
CarrierNotified
InspectionScheduled
