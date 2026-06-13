# Public Adjusting Operating Platform Requirements

## Status
Draft v1

## Purpose
ClaimsOS must support public adjusting firms as an operating platform, not merely as a CRM.

The system should guide users through the complete claim-handling process from lead intake to closeout.

Core principle:

> No file left behind.

## Product Positioning
ClaimsOS should function as a public adjusting business operating system.

It should provide:
- Claim file management
- Workflow control
- Document generation
- Task automation
- Role assignment
- Deadline tracking
- Management dashboards
- Training prompts
- Quality-control gates
- AI-guided claim strategy

Long-term, the platform may support a "Public Adjusting Business in a Box" model.

## Primary Users
- Firm owner
- Public adjuster
- Claims manager
- Intake coordinator
- Administrative assistant
- Estimator
- Appraiser
- Umpire
- Attorney
- Client / insured

## Lead Intake Requirements
Leads may originate from:
- Website intake form
- Manual entry
- Referral
- Contractor partner
- Phone call
- Email
- Campaign

Required intake fields:
- Contact name
- Contact phone
- Contact email
- Property address
- Loss type
- Date of loss, if known
- Insurance carrier, if known
- Policy number, if known
- Description of loss
- Source / referral channel

## Lead-to-Claim Conversion
A qualified lead may be converted into a claim file.

Conversion should:
- Create claim record
- Create property record if needed
- Associate contact
- Assign owner
- Apply claim playbook
- Generate initial workflow tasks
- Identify required documents
- Trigger onboarding communications

## Claim File Requirements
Each claim file should include:
- Claim number
- Policy number
- Carrier
- Insured / client
- Property
- Date of loss
- Loss type
- Assigned adjuster
- Stage
- Status
- Deadlines
- Documents
- Communications
- Tasks
- Timeline
- Notes
- AI recommendations

## Workflow Requirements
The public adjusting workflow must support:
- Contract execution
- Required disclosures
- Carrier notification
- Inspection
- Estimate creation
- Estimate submission
- Follow-up pressure cycle
- Coverage review
- Negotiation
- Appraisal
- Umpire process
- Closeout

## Controlled Task Completion
Critical tasks may not be completed without required evidence.

Example:

A user cannot complete "Notify Carrier" unless:
- Signed PA agreement exists
- Required disclosure exists
- Carrier contact information exists
- Letter of representation is generated
- Notification was sent or queued
- Completion note is entered

## Document Generation Requirements
The system should support generation of:
- Public adjusting agreement
- Disclosure forms
- Letter of representation
- Carrier notification letter
- Client onboarding packet
- Estimate cover letter
- Follow-up letters
- Appraisal demand
- Umpire correspondence
- Closeout letter

## Dashboard Requirements
Management dashboard should show:
- Files with no next action
- Files with missed deadlines
- Files missing contracts
- Files missing disclosures
- Files missing carrier notification
- Files stalled by carrier response
- Files awaiting inspection
- Files awaiting estimate
- Files ready for escalation
- Files ready for closeout

## Training and Guidance
The platform should help users understand why each task exists.

Each critical task should include:
- Purpose
- Instructions
- Required evidence
- Best practices
- Common mistakes
- Escalation guidance

## MVP Requirements
The MVP should support:
1. Website or manual lead intake
2. Lead-to-claim conversion
3. Role assignment
4. Claim stages
5. Stage task creation
6. Required document tracking
7. Contract and letter template generation
8. Basic communication tracking
9. Follow-up task generation
10. Completion gates
11. Management dashboard exceptions

## Agentic Development Notes
Agents should use this document to generate:
- Public adjusting workflows
- Claim lifecycle APIs
- Dashboard queries
- Required document rules
- Completion gate definitions
- Template metadata
- Role and permission models
