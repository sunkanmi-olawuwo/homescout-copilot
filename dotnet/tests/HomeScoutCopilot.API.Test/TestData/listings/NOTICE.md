# Listing PDF test fixtures — NOTICE

These PDFs are **real property listings**, saved by the developer from portal websites, used
**solely as private regression fixtures** for the extraction pipeline (`ListingExtractionTests`).

- They are the developer's own saved documents (the terms-safe capture path — nothing was scraped).
- They contain third-party copyrighted content (descriptions, photos, agent branding). They are kept
  here only to test that our extractor reads real-world layouts correctly, and are **not
  redistributed** as product content — HomeScout extracts *facts*, it does not re-serve listings
  (see `wiki/differentiation-and-data-strategy.md`).

**If this repository is ever made public, remove this directory** (and scrub it from history) and move
the corpus to a private eval store — committing branded portal listings to a public repo is not
appropriate. The extraction unit tests use synthetic fixtures and do not depend on these files.
