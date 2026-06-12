# ADR-001: PostgreSQL as Primary Database

## Status

Accepted

## Context

The platform requires:

- Relational data
- Geospatial search
- Vector embeddings

## Decision

Use PostgreSQL with:

- PostGIS
- pgvector

## Consequences

Benefits:

- Single data platform
- Mature ecosystem
- Strong geospatial support

Tradeoffs:

- Requires PostgreSQL expertise
