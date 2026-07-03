# Product Brief

## Name

HomeScout Copilot

## One-Line Pitch

An AI assistant that helps homebuyers compare properties and neighbourhoods using public data, uploaded documents, saved preferences, and conversational analysis.

## MVP

The first version focuses on property and area comparison:

- Compare two or three postcodes or property listings.
- Show nearby amenities.
- Summarize crime data.
- Summarize school options.
- Estimate basic monthly ownership costs.
- Save comparison conversations.
- Remember buyer preferences such as budget, commute tolerance, school needs, and lifestyle priorities.

## Not In MVP

- Regulated mortgage advice.
- Specific mortgage product recommendations.
- Automated property valuation as a definitive price.
- Claims that one area is objectively safe or unsafe.

## Target User

A UK homebuyer, especially a first-time buyer, who wants a clearer view of neighbourhood tradeoffs before booking viewings or speaking to professionals.

## Core User Story

As a homebuyer, I want to compare properties and areas in one guided workspace so I can understand lifestyle fit, affordability tradeoffs, local amenities, crime context, schools, and follow-up questions before making a decision.

## Course Feature Mapping

The course starts with a generic chatbot. HomeScout turns that into a domain-specific assistant:

| Course Feature | HomeScout Feature |
| --- | --- |
| Basic chat | Property and area Q&A workspace |
| Tool calls | Crime, amenities, school, price, and cost tools |
| Reasoning output | Explainable comparison notes |
| Streaming | Live report generation |
| Conversation history | Saved property searches and comparisons |
| Image generation | Optional report visuals |
| Image/PDF input | Listing, survey, EPC, and floorplan uploads |
| User auth | Private saved searches |
| Memory | Buyer preferences |
| Speech input | Viewing notes and spoken search criteria |

## Success Criteria

- A user can compare two areas from a guided chat.
- The assistant can call at least one real data tool.
- The assistant can store and reload a comparison conversation.
- The assistant can remember at least three user preferences.
- The app has a clear disclaimer and avoids regulated advice.

