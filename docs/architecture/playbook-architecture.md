# Playbook Architecture

## Status

Draft v1

## Purpose

Playbooks define reusable operating procedures for claims and insurance dispute matters.

A playbook is more than a checklist. It is a versioned operational template that can create workflows, tasks, required documents, communication templates, deadlines, completion gates, and AI guidance.

## Strategic Role

Playbooks enable ClaimsOS to become a public adjusting business operating system.

They allow the platform to standardize claim handling across:

- Claim types
- Jurisdictions
- Firms
- Offices
- Roles
- Matter types

## Example Playbooks

- Florida Wind Claim
- Florida Water Claim
- Hail Claim
- Hurricane Claim
- Commercial Roof Claim
- Appraisal Matter
- Umpire Matter
- Litigation Support Matter
- Contractor Referral Claim
- Reopened Claim

## Core Concepts

### Playbook

A versioned template that defines an operational process.

### Playbook Version

A snapshot of a playbook.

Existing claims must continue using the playbook version they were created with unless explicitly migrated.

### Playbook Stage

Defines major phases of work.

### Playbook Task

Defines tasks that should be created within workflow stages.

### Playbook Document Requirement

Defines required documents.

### Playbook Communication Requirement

Defines required letters, emails, notices, or messages.

### Playbook Rule

Defines conditions, branching logic, or completion gates.

### Playbook Guidance

Defines instructional content and AI context.

## Playbook Lifecycle

Playbooks may have the following statuses:

- Draft
- Active
- Deprecated
- Archived

Only active playbooks may be assigned to new claims.

## Versioning Requirements

Playbook changes must be version controlled.

Rules:

- Editing a draft playbook modifies the draft version.
- Publishing creates an active immutable version.
- Existing claims remain tied to their assigned playbook version.
- Deprecated playbooks remain available for historical claims.
- Archived playbooks are hidden from normal selection.

## Assignment Rules

Playbooks may be assigned based on:

- Claim type
- State
- Loss type
- Carrier
- Property type
- Organization configuration
- Manual user selection

A claim may support one primary playbook and multiple supplemental playbooks.

Example:

Primary:

- Florida Wind Claim

Supplemental:

- Appraisal Matter

## Playbook Output

When applied to a claim, a playbook should generate:

- Workflow instance
- Stages
- Tasks
- Required documents
- Required communications
- Completion gates
- Deadline rules
- Training guidance
- Initial dashboard indicators

## Playbook Data Model

Suggested entities:

- Playbook
- PlaybookVersion
- PlaybookStageDefinition
- PlaybookTaskDefinition
- PlaybookDocumentRequirement
- PlaybookCommunicationRequirement
- PlaybookCompletionGateDefinition
- PlaybookDeadlineRule
- PlaybookGuidance
- PlaybookAssignmentRule

## Integration With Workflow Engine

The Playbook module defines templates.

The Workflow module executes runtime instances.

Playbook definitions should not be mutated by active workflows.

Runtime workflow records should copy the relevant playbook configuration at claim creation time.

## AI Integration

Playbooks should provide AI context.

Examples:

- Explain task purpose
- Suggest next action
- Identify missing evidence
- Generate document drafts
- Recommend escalation strategy

Playbook guidance should be used as grounding context for AI recommendations.

## Marketplace Vision

Long-term, ClaimsOS may support a marketplace where playbooks can be:

- Shared
- Licensed
- Sold
- Reviewed
- Rated
- Installed by organizations

Marketplace playbooks should support publisher metadata and versioning.

## MVP Scope

MVP playbook architecture should support:

- Creating static playbook definitions in configuration or database
- Assigning a playbook to a claim
- Generating workflow stages and tasks
- Generating required document rules
- Generating completion gates
- Versioning playbooks at a basic level

Excluded from MVP:

- Visual playbook designer
- Marketplace
- Playbook sharing
- AI-generated playbooks
- Complex branching

## Agentic Development Notes

Agents should use this document to generate:

- Playbook aggregate
- Playbook EF Core entities
- Playbook-to-workflow generation service
- Playbook APIs
- Versioning logic
- Seed data for initial public adjusting playbooks
