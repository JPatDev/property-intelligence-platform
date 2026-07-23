# Workflow Architecture

## Status

Draft v2

## Purpose

The Workflow Architecture defines how ClaimsOS controls claim progression, enforces quality gates, assigns work, manages deadlines, and prevents claim files from becoming stagnant.

The workflow system is not a simple task list. It is the operational control layer of the platform.

Core principle:

> No file left behind.

Every active claim must have:

- Current workflow status
- Current stage or stages
- Next required action
- Responsible owner
- Deadline or an explicit reason why no deadline applies
- Completion requirements
- Blockers, if any
- Immutable audit trail

## Architectural Role

Workflow is a first-class bounded context. It coordinates activity across Claims, Documents, Communications, Property Intelligence, Weather Intelligence, AI Claim Strategy, Reporting, and Playbooks without taking ownership of those modules' source data.

The Workflow module owns:

- Runtime workflow instances
- Runtime stages and tasks
- Task assignments and deadlines
- Completion-gate definitions copied into a workflow
- Gate evaluation results
- Workflow blockers
- Escalations
- Calculated next actions
- Workflow audit events

The Workflow module does not own:

- Claim identity or claim facts
- Document contents or document classification
- Communication delivery
- User or role identity
- Playbook authoring and versioning
- AI-generated recommendations

## Playbook and Workflow Boundary

Playbooks are the design-time source of operational templates. Workflows are runtime executions of those templates.

- A `PlaybookVersion` defines stages, tasks, deadlines, gates, dependencies, and guidance.
- Applying a playbook version creates a `WorkflowInstance`.
- The workflow stores a snapshot of all execution-critical configuration.
- Publishing or editing a later playbook version must not change an active workflow.
- A workflow may retain references to its source playbook and version for traceability, but execution must not depend on mutable playbook records.
- Migrating an active workflow to another playbook version is an explicit, audited operation and is outside the MVP.

The term `WorkflowDefinition` should not be used as a second template model. If an internal compiled representation is needed, it is a value object produced from a `PlaybookVersion`, not a separately managed aggregate.

## Core Concepts

### Workflow Instance

A runtime execution created for a specific claim from a frozen playbook version. A claim may have one primary workflow and supplemental workflows for matters such as appraisal or litigation support.

### Stage

A major phase of work containing ordered or parallel tasks. Examples include Lead Intake, Contract Execution, Carrier Notification, Investigation, Inspection, Estimate Preparation, Submission, Negotiation, Escalation, Appraisal, and Closeout.

### Task

A specific action with an owner, status, priority, due date, dependencies, and completion requirements.

### Completion Gate

A deterministic requirement that must pass before a task, stage, or workflow transition is allowed.

### Blocker

A recorded condition that prevents task completion or workflow progression. A blocker may be produced automatically by a failed gate or entered manually by an authorized user.

### Escalation

An operational response to a time-sensitive or exceptional condition, such as a missed deadline, a prolonged blocker, or a claim without a valid next action.

### Next Action

The highest-priority actionable task calculated for a workflow, together with its owner, deadline, reason, and blocking conditions.

## Aggregate and Data Model

### Workflow Aggregate

`WorkflowInstance` is the aggregate root and consistency boundary for workflow progression.

Suggested aggregate members:

- `WorkflowInstance`
- `WorkflowStage`
- `WorkflowTask`
- `TaskDependency`
- `CompletionGate`
- `WorkflowBlocker`
- `WorkflowEscalation`
- `NextActionSnapshot`

Suggested `WorkflowInstance` fields:

- Workflow instance ID
- Organization ID
- Claim ID
- Source playbook ID and version ID
- Workflow type: primary or supplemental
- Status
- Started, completed, cancelled, and archived timestamps
- Current stage identifiers
- Snapshot schema version
- Concurrency token
- Created and updated timestamps

Stages and tasks require stable runtime identifiers in addition to source definition identifiers. External modules should reference runtime identifiers after workflow creation.

### Supporting Records

Audit events and reliable integration messages may be stored outside the aggregate transaction model:

- `WorkflowAuditEvent`
- `WorkflowOutboxMessage`
- Read-model projections for dashboards and work queues

Large histories and query projections should not be loaded as part of every aggregate operation.

### Invariants

The aggregate must enforce the following:

- Every workflow belongs to exactly one organization and one claim.
- Cross-organization references are invalid.
- A completed, cancelled, or archived workflow cannot accept normal task mutations.
- A task cannot complete while any required completion gate fails.
- A task cannot begin until required task dependencies are complete.
- A stage cannot complete while required tasks remain incomplete.
- A workflow cannot complete while required stages remain incomplete.
- Completed tasks retain who completed them and when.
- Skips, cancellations, deadline overrides, and manual blocker resolutions require a reason and actor.
- Runtime configuration remains unchanged when its source playbook changes.

## Lifecycle and State Machines

State changes occur only through workflow commands. Clients must not directly set status fields.

### Workflow Instance Status

- `NotStarted`
- `Active`
- `Blocked`
- `Completed`
- `Cancelled`
- `Archived`

Allowed transitions:

| From | To | Condition |
| --- | --- | --- |
| NotStarted | Active | Workflow is initialized and its entry stage can activate |
| NotStarted | Cancelled | Authorized cancellation with reason |
| Active | Blocked | No required work can proceed because of active blockers |
| Blocked | Active | At least one required path is actionable and blocking conditions are cleared |
| Active | Completed | All required terminal conditions pass |
| Active or Blocked | Cancelled | Authorized cancellation with reason |
| Completed or Cancelled | Archived | Retention and authorization rules pass |

`Completed`, `Cancelled`, and `Archived` are terminal for MVP. Reopening requires an explicit future command and policy.

### Stage Status

- `Pending`
- `Active`
- `Blocked`
- `Completed`
- `Skipped`
- `Cancelled`

A stage activates when its entry conditions and predecessor dependencies pass. Multiple stages may be active when the playbook permits parallel execution.

Only optional stages may be skipped. A skip requires an actor, reason, and authorization. A stage is blocked only when its required tasks cannot make progress; one blocked task does not necessarily block the entire stage.

### Task Status

- `Pending`
- `Assigned`
- `InProgress`
- `Blocked`
- `Completed`
- `Cancelled`

Allowed transitions:

| From | To | Condition |
| --- | --- | --- |
| Pending | Assigned | A valid owner is assigned |
| Pending or Assigned | InProgress | Dependencies pass and work begins |
| Pending, Assigned, or InProgress | Blocked | A blocking condition is recorded |
| Blocked | Assigned or InProgress | Blockers clear; return state is determined from work history |
| Assigned or InProgress | Completed | All required completion gates pass |
| Pending, Assigned, InProgress, or Blocked | Cancelled | Task is optional or parent workflow is cancelled; reason required |

Assignment and task status are related but distinct. Reassignment must not reset progress. An unassigned actionable task may remain `Pending`, but it must generate a visible ownership exception.

## Controlled Task Completion

Controlled Task Completion is a core differentiator. Critical tasks cannot be marked complete merely because a user selects a checkbox.

Example: **Send Carrier Notification Packet**

Required completion conditions:

- Signed public adjusting agreement exists
- Required disclosure form exists
- Letter of representation is generated
- Carrier contact method exists
- Packet is sent or durably queued
- Completion note is entered
- Actor identity and timestamp are captured

The completion command evaluates all gates against a consistent view of required evidence. If any required gate fails:

- The task remains in its current status or becomes `Blocked` according to task policy.
- No completion event is emitted.
- The response identifies every failed gate with a stable code and user-facing message.
- Evaluation results are recorded for diagnosis and audit.

Gate failure is an expected domain outcome, not a server error.

## Completion Gate Model

Every gate definition should contain:

- Gate ID and type
- Scope: task, stage, or workflow
- Required or advisory severity
- Structured parameters
- Stable failure code
- User-facing failure message
- Evaluation version
- Source definition identifier

MVP gate types:

### Field Gate

Requires a field owned by Claims or another authoritative module to satisfy a predicate.

Examples: carrier name exists, policy number exists, date of loss is valid, or property address is complete.

### Document Gate

Requires an authoritative document record matching a document type and lifecycle state. Mere file upload may be insufficient when classification, signature, or approval is required.

### Communication Gate

Requires a communication to be sent, durably queued, delivered, or logged, as specified by the gate.

### Approval Gate

Requires a valid approval from an authorized user or role. Approval evidence must identify the approver, decision, timestamp, and subject version.

### Task Dependency Gate

Requires another runtime task to be complete.

### Rule Gate

Evaluates a deterministic business predicate using structured facts. For example, a claim cannot enter appraisal unless the configured dispute condition exists.

Advisory gates may warn but cannot prevent completion. Required gates must pass unless a separately authorized override policy explicitly permits bypass. Gate overrides are not included in the MVP.

## Gate Evaluation

Gate definitions should be declarative, versioned, and evaluated by registered handlers rather than arbitrary executable expressions.

Example:

```json
{
  "gateType": "DocumentExists",
  "evaluationVersion": 1,
  "parameters": {
    "documentType": "SignedPaAgreement",
    "acceptedStatuses": ["Verified"]
  },
  "severity": "Required",
  "failureCode": "SIGNED_PA_AGREEMENT_REQUIRED",
  "failureMessage": "A verified signed PA Agreement is required before carrier notification."
}
```

Evaluation behavior:

- Each gate type has a typed parameter schema and handler.
- Unknown gate types or unsupported versions fail closed for required gates.
- Handlers query authoritative module contracts, not another module's database tables.
- Results include pass/fail, observed evidence references, evaluation time, and handler version.
- Commands should evaluate gates synchronously when authoritative data is available.
- Remote or eventually consistent evidence may return `Indeterminate`; a required indeterminate result prevents completion and triggers retry guidance.
- Gate evaluation must not mutate external modules.

A general-purpose rules DSL, arbitrary scripts, and a visual rules designer are outside the MVP.

## Deadlines and Business Time

Deadline definitions originate in the playbook snapshot and are materialized on runtime tasks or stages.

Supported MVP deadline anchors should include:

- Workflow start
- Stage activation
- Task activation
- Completion of another task
- Claim fact date, such as date of loss or inspection date

Deadline calculation must record:

- Source rule
- Anchor timestamp
- Calculated due timestamp
- Time zone
- Calendar mode: elapsed time or business time
- Calculation version
- Manual override, reason, actor, and timestamp

All stored timestamps use UTC. Organization or claim-local time zones are used for display and business-calendar calculations. Recalculation must be explicit and audited; changes to a playbook or calendar must not silently rewrite existing deadlines.

## Next Action Engine

The Next Action Engine calculates the best operational action for every active workflow.

It considers:

- Active stages
- Open tasks
- Task priority
- Due dates and overdue duration
- Dependencies
- Required gate failures
- Blockers
- Assignment state
- Escalation severity

Only tasks whose entry dependencies pass are actionable. Blocked tasks may be returned as the next required intervention when clearing the blocker is the most urgent action.

MVP ranking, in descending order:

1. Critical escalations and overdue statutory or carrier-response deadlines
2. Overdue unblocked tasks
3. Blocker-resolution actions
4. Tasks due soon
5. Higher configured task priority
6. Earlier workflow stage order
7. Earlier task order
8. Stable task ID as a deterministic tie-breaker

Output:

- Workflow and task ID
- Action label
- Owner or ownership exception
- Due timestamp
- Calculated priority
- Reason code and explanation
- Blocking conditions
- Calculation timestamp and version

The calculated result should be persisted as a replaceable projection for dashboards and queues. The underlying tasks remain the source of truth. Recalculation is triggered by relevant workflow events, integration events, deadline changes, and a periodic repair job.

An active workflow with neither a next action nor a valid waiting state is an operational exception and must create an escalation.

## Waiting States

Some claims legitimately wait on an external party. Waiting must be explicit rather than represented as inactivity.

A waiting state records:

- Waiting reason
- Responsible external party
- Start timestamp
- Expected response deadline
- Follow-up task
- Owner

Waiting does not remove the next-action requirement; the follow-up task becomes the next action until the awaited event occurs.

## Escalation Architecture

Escalations are created automatically when:

- A task is overdue
- A stage exceeds its expected duration
- A workflow has no next action
- An actionable task has no owner
- Required evidence remains missing near a deadline
- A carrier or client response is overdue
- A blocker exceeds its allowed age
- Gate evaluation repeatedly remains indeterminate

Each escalation has:

- Type and stable deduplication key
- Severity
- Source entity
- Owner or target role
- Triggered timestamp
- Acknowledged and resolved timestamps
- Resolution reason

Escalation processing must be idempotent. Repeated evaluation of the same condition updates or preserves the open escalation rather than creating duplicates. Resolved escalations may recur as a new occurrence if the condition later becomes true again.

Escalations may produce alerts, follow-up tasks, emails, dashboard warnings, or management review items. Delivery through Communications occurs asynchronously through integration events.

## Commands and Domain Events

Representative commands:

- `CreateWorkflowFromPlaybook`
- `StartWorkflow`
- `AssignTask`
- `StartTask`
- `CompleteTask`
- `AddBlocker`
- `ResolveBlocker`
- `SkipStage`
- `OverrideDeadline`
- `CancelWorkflow`
- `ArchiveWorkflow`

Representative domain events:

- `WorkflowCreated`
- `WorkflowStarted`
- `StageActivated`
- `TaskAssigned`
- `TaskStarted`
- `TaskBlocked`
- `TaskCompleted`
- `StageCompleted`
- `WorkflowBlocked`
- `WorkflowCompleted`
- `DeadlineChanged`
- `EscalationRaised`
- `EscalationResolved`
- `NextActionChanged`

Domain events describe facts within the Workflow boundary. Events intended for other modules are converted to versioned integration events and published through an outbox after the workflow transaction commits.

## API Shape

Representative command endpoints:

- `POST /claims/{claimId}/workflows`
- `POST /workflows/{workflowId}/start`
- `POST /workflow-tasks/{taskId}/assign`
- `POST /workflow-tasks/{taskId}/start`
- `POST /workflow-tasks/{taskId}/complete`
- `POST /workflow-tasks/{taskId}/blockers`
- `POST /workflow-blockers/{blockerId}/resolve`
- `POST /workflows/{workflowId}/cancel`

Representative query endpoints:

- `GET /claims/{claimId}/workflows`
- `GET /workflows/{workflowId}`
- `GET /workflow-tasks/{taskId}/completion-readiness`
- `GET /work-queues/me`
- `GET /organizations/{organizationId}/workflow-exceptions`

Command requests should include an expected version or equivalent concurrency token. Retriable create and completion operations should accept an idempotency key. Conflict responses distinguish stale versions, failed gates, invalid transitions, and authorization failures.

## Consistency, Concurrency, and Reliability

- Workflow aggregate changes and outbox messages commit in one transaction.
- Optimistic concurrency prevents two actors from completing or changing the same task from stale state.
- Commands are idempotent where client or worker retries are expected.
- Event consumers deduplicate integration events by message ID.
- Cross-module integration is eventually consistent unless a completion command explicitly queries authoritative evidence.
- Scheduled deadline and escalation workers use stable deduplication keys and safe retries.
- Projection failures do not invalidate committed workflow state; repair jobs rebuild projections from authoritative records and events.

## Authorization and Tenancy

Every command and query is scoped by organization. Authorization policies should cover:

- Workflow creation and cancellation
- Task assignment and reassignment
- Task completion
- Stage skipping
- Deadline overrides
- Blocker resolution
- Escalation acknowledgement
- Audit-log access

The domain records the effective actor supplied by the authenticated application boundary. Background operations use a named system actor and set the system-generated flag.

## Audit Requirements

Every workflow command outcome and state transition must be auditable.

Audit fields:

- Audit event ID
- Organization ID
- Workflow, entity type, and entity ID
- Actor type and actor ID
- Action
- Previous and new state
- Reason or note
- Correlation and causation IDs
- Request idempotency key, when applicable
- Timestamp
- System-generated flag

Audit records are append-only and immutable to application users. Sensitive claim or document contents should not be copied into workflow audit payloads; store identifiers and necessary decision evidence instead.

## Integration Contracts

### Claims Module

Claims owns claim identity and claim facts. Workflow controls operational progression. Claim closure may require workflow readiness, but Workflow must not directly change claim status.

### Playbooks Module

Playbooks supplies an immutable published version. Workflow validates and snapshots its execution-critical configuration when creating an instance.

### Documents Module

Documents supplies authoritative document metadata and emits document lifecycle events. Workflow evaluates document gates and recalculates readiness when relevant events arrive.

### Communications Module

Communications owns message composition and delivery state. Workflow may request communication work and evaluate communication evidence, but it does not mark a message delivered.

### Identity and Organization Modules

These modules own users, roles, teams, and organization settings. Workflow stores stable assignee references and validates assignments through published contracts.

### AI Module

AI may recommend next actions, identify missing evidence, and detect readiness issues. AI output is advisory and cannot complete tasks, bypass gates, or mutate workflow state without an authorized deterministic command.

### Reporting Module

Reporting consumes workflow integration events and read models for operational dashboards. Reporting projections are not authoritative workflow state.

## Observability

Operational telemetry should include:

- Command success, conflict, and gate-failure rates
- Gate evaluation latency and indeterminate results
- Workflows without next actions
- Unassigned actionable tasks
- Overdue tasks by severity
- Blocker and stage age
- Escalation creation and resolution rates
- Outbox and projection lag

Logs and traces should include organization-safe correlation identifiers without exposing sensitive document content.

## MVP Scope

MVP includes:

- Workflow creation from a published playbook snapshot
- Primary workflow instances
- Sequential stages with limited parallel-task support
- Task assignment, priority, and due dates
- Explicit task, stage, and workflow transitions
- Field, document, communication, approval, and dependency gates
- Controlled task completion
- Manual and gate-generated blockers
- Deterministic next-action calculation
- Explicit external waiting states
- Basic overdue, blocker-age, missing-owner, and no-next-action escalations
- Optimistic concurrency and idempotent workers
- Immutable audit records
- Outbox-based integration events
- Dashboard and personal work-queue projections

Excluded from MVP:

- Visual workflow or rules designer
- Workflow marketplace
- AI-generated workflows
- General-purpose rules DSL or executable scripts
- Active workflow migration between playbook versions
- Gate overrides
- Workflow reopening
- Complex conditional branching
- Multiple primary workflows for one claim

## MVP Acceptance Criteria

The architecture is successfully implemented when:

- Applying an active playbook version creates a complete, frozen runtime snapshot.
- Changing the playbook later does not alter the active workflow.
- Invalid state transitions return a domain conflict without partially changing state.
- A required failed or indeterminate gate prevents task completion and explains every unmet requirement.
- Completing the final required task advances its stage and workflow deterministically.
- Every active workflow has a next action or a recorded operational exception.
- Overdue, unassigned, long-blocked, and no-next-action conditions create one deduplicated open escalation each.
- Concurrent stale commands cannot silently overwrite newer workflow state.
- Retrying an idempotent command or worker does not duplicate completion, audit, or escalation effects.
- Every successful mutation records actor, time, previous state, new state, and reason where required.
- Cross-module messages are eventually published after commit through the outbox.
- Organization boundaries are enforced for every workflow command and query.

## Agentic Development Notes

Code generation agents should treat this document as the source of truth for:

- Workflow aggregate and invariants
- Runtime snapshot creation from playbooks
- Task, stage, and workflow state machines
- Completion-gate handlers and evaluation results
- Deadline calculations and waiting states
- Next-action ranking
- Escalation deduplication
- Optimistic concurrency and idempotency
- Audit and outbox persistence
- Workflow APIs and projections

Agents must not introduce a second mutable workflow-template aggregate, allow direct status-field updates, make AI recommendations authoritative, or bypass completion gates for convenience.
