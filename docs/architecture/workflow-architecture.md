# Workflow Architecture

## Status
Draft v1

## Purpose
The Workflow Architecture defines how ClaimsOS controls claim progression, enforces quality gates, assigns work, manages deadlines, and prevents claim files from becoming stagnant.

The workflow system is not a simple task list. It is the operational control layer of the platform.

Core principle:

> No file left behind.

Every active claim must have:
- Current workflow state
- Current stage
- Next required action
- Responsible owner
- Deadline
- Completion requirements
- Blockers, if any
- Audit trail

## Architectural Role
The Workflow module coordinates activity across:
- Claims
- Documents
- Communications
- Property Intelligence
- Weather Intelligence
- AI Claim Strategy
- Reporting
- Playbooks

It should be treated as a first-class bounded context.

## Core Concepts

### Workflow Definition
A reusable template that defines how a claim or matter should be handled.

Examples:
- Florida Wind Claim
- Florida Water Claim
- Hail Claim
- Appraisal Matter
- Umpire Matter
- Commercial Roof Claim

### Workflow Instance
A runtime execution of a workflow definition assigned to a specific claim.

### Stage
A major phase of work.

Example stages:
- Lead Intake
- Contract Execution
- Carrier Notification
- Investigation
- Inspection
- Estimate Preparation
- Submission
- Negotiation
- Escalation
- Appraisal
- Closeout

### Task
A specific action that must be completed.

### Completion Gate
A rule or set of rules that must be satisfied before a task may be completed.

### Blocker
A condition that prevents task completion or stage advancement.

### Escalation
A triggered event caused by delay, missed deadline, missing document, or unresolved blocker.

## Workflow Lifecycle

Workflow instance statuses:
- Not Started
- Active
- Blocked
- Completed
- Cancelled
- Archived

Workflow stage statuses:
- Pending
- Active
- Blocked
- Completed
- Skipped
- Cancelled

Workflow task statuses:
- Pending
- Assigned
- In Progress
- Blocked
- Completed
- Cancelled

## Controlled Task Completion
Controlled Task Completion is a core differentiator.

Users may not mark critical tasks complete unless the platform verifies that the required work was actually performed.

Example task:

**Send Carrier Notification Packet**

Required completion conditions:
- Signed public adjusting agreement exists
- Required disclosure form exists
- Letter of representation generated
- Carrier contact method exists
- Packet sent or queued
- Completion note entered
- User identity, date, and timestamp captured

If any condition is missing, the task remains blocked and the system displays the missing requirement.

## Completion Gate Types

### Field Gate
Requires a specific data field to be populated.

Examples:
- Carrier name
- Policy number
- Claim number
- Date of loss
- Property address

### Document Gate
Requires a specific document type to exist.

Examples:
- Signed PA agreement
- Disclosure form
- Policy
- Estimate
- Inspection report

### Communication Gate
Requires a communication to be sent, queued, or logged.

Examples:
- Carrier notification
- Client onboarding email
- Follow-up letter
- Demand package

### Approval Gate
Requires approval from a user or role.

Examples:
- Manager approval
- Legal approval
- Estimate review approval

### Task Dependency Gate
Requires another task to be complete first.

### Rule Gate
Executes a business rule.

Example:
- A claim cannot enter appraisal unless a coverage dispute exists.

## Rules Engine Requirements
The workflow engine should support declarative rule definitions.

Rules should be stored as structured configuration where possible.

Example rule structure:

```json
{
  "gateType": "DocumentExists",
  "documentType": "SignedPaAgreement",
  "required": true,
  "failureMessage": "Signed PA Agreement is required before carrier notification."
}
```

Future versions may support a rules DSL, visual workflow designer, or marketplace workflow templates.

## Next Action Engine
The Next Action Engine calculates what should happen next on every file.

It evaluates:
- Active workflow
- Active stage
- Open tasks
- Due dates
- Dependencies
- Blockers
- Required documents
- Assigned owner
- Escalation status

Output:
- Next action
- Owner
- Deadline
- Priority
- Reason
- Blocking conditions

This output should be visible on claim dashboards, user work queues, and management reports.

## Escalation Architecture
Escalations should be generated automatically when:
- A task is overdue
- A stage exceeds expected duration
- A claim has no next action
- Required documents are missing
- Carrier response is overdue
- Client response is overdue
- A file is blocked for too long

Escalations may create:
- Alerts
- Follow-up tasks
- Emails
- Dashboard warnings
- Management review items

## Audit Requirements
Every workflow event must be auditable.

Audit log fields:
- Organization ID
- Entity type
- Entity ID
- User ID
- Action
- Previous state
- New state
- Timestamp
- Notes
- System-generated flag

Audit logs should be immutable.

## Integration Points

### Claims Module
The Claims module owns the claim record. The Workflow module controls claim progression.

### Documents Module
Completion gates query the Documents module to verify required files.

### Communications Module
Completion gates verify required letters, emails, or notices.

### AI Module
AI may recommend next actions, identify missing evidence, and detect claim readiness issues.

### Reporting Module
Workflow status feeds operational dashboards.

## MVP Scope
MVP workflow architecture should include:
- Workflow definitions
- Workflow instances
- Stages
- Tasks
- Task assignment
- Due dates
- Completion gates
- Blockers
- Audit logs
- Dashboard indicators
- Basic escalation rules

Excluded from MVP:
- Visual workflow designer
- Workflow marketplace
- AI-generated workflows
- Complex rules DSL

## Agentic Development Notes
Code generation agents should treat this document as the source of truth for:
- Workflow aggregate design
- Task state machine
- Completion gate evaluation
- Escalation services
- Audit logging
- Workflow-related APIs
