# Initial Database Design

## Properties

- Id
- Address
- City
- State
- Zip
- Latitude
- Longitude
- ParcelId
- County

## Claims

- Id
- PropertyId
- ClaimNumber
- Carrier
- DateOfLoss
- Status

## Documents

- Id
- ClaimId
- FileName
- StorageUrl
- DocumentType

## WeatherEvents

- Id
- EventType
- EventDate
- Severity
- Geometry

## PropertyTimelineEvents

- Id
- PropertyId
- EventType
- EventDate
- Source
- Description

## AI Document Chunks

- Id
- DocumentId
- ChunkText
- Embedding
