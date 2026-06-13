# Property Intelligence Requirements

## Status
Draft v1

## Purpose
Property Intelligence provides the evidence layer for ClaimsOS.

It helps users understand the property, its history, relevant storm events, permit activity, prior repairs, inspection records, and risk profile.

The goal is not merely to display property data. The goal is to help claim professionals answer:
- What happened to this property?
- When did it happen?
- What evidence supports the claim?
- What history may affect coverage or causation?
- What additional investigation is needed?

## Core Capabilities

### Property Search
Users must be able to search for a property by:
- Full address
- Parcel number
- Owner name, where available
- Claim association
- Geographic area

### Property Profile
The system should store and display:
- Property address
- County
- State
- ZIP code
- Parcel ID
- Latitude and longitude
- Property type
- Year built
- Square footage
- Roof type
- Roof age, where known
- Ownership information, where available
- Occupancy type, where available

### Permit History
The system should capture and normalize permit activity.

Important permit categories:
- Roof replacement
- Roof repair
- Electrical
- Plumbing
- Structural
- Windows / openings
- Remodel
- Addition
- Demolition

Permit records should include:
- Permit number
- Permit type
- Status
- Issue date
- Final date
- Contractor
- Description
- Source jurisdiction

### Weather Intelligence
The system should support weather event correlation.

Relevant event types:
- Hail
- Wind
- Hurricane
- Tornado
- Flood
- Lightning
- Severe thunderstorm

The platform should correlate weather events to property location using geospatial logic.

Example questions:
- Was hail reported within 1 mile of the property?
- What was the highest wind gust near the property?
- Did a hurricane track pass near the property?
- Did the event date align with the claimed date of loss?

### Historical Imagery
The platform should support references to:
- Aerial imagery
- Satellite imagery
- Street-level imagery
- Inspection photos
- Drone imagery

The first version may store references and metadata rather than directly integrating imagery providers.

### Property Timeline
Property Intelligence should feed the Timeline module.

Timeline events may include:
- Construction
- Permit issued
- Permit finalized
- Storm event
- Inspection
- Claim opened
- Document uploaded
- Roof replaced
- Repair performed

### Risk and Relevance Scoring
The system should eventually calculate scores such as:
- Claim relevance score
- Weather impact score
- Roof risk score
- Documentation completeness score
- Causation support score

## MVP Requirements
MVP property intelligence should include:
- Manual property creation
- Address search
- Property profile page
- Claim-to-property association
- Basic permit record storage
- Basic weather event storage
- Property timeline entries
- Document association
- Notes and audit history

External API integrations may be added after the core domain model is stable.

## Data Sources
Potential future data sources:
- County property appraiser records
- County GIS records
- Permit databases
- NOAA weather data
- Storm report datasets
- Aerial imagery providers
- Geocoding APIs
- Parcel boundary providers

## Acceptance Criteria
A user should be able to:
- Create or find a property
- Associate a property with a claim
- View property details
- View permit history
- View weather events related to the property
- View property timeline
- Upload or associate documents with the property
- See whether property intelligence supports or weakens a claim

## Agentic Development Notes
Agents should use this document to generate:
- Property aggregate
- Property APIs
- Property EF Core mappings
- PostGIS location fields
- Permit entities
- Property timeline integration
- Property intelligence dashboard components
