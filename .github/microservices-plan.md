# Microservices Planning Overview

## Purpose
This document is a high-level planning placeholder for splitting the project into microservices.

## Scope
- This file is not an implementation plan.
- It exists as a planning reference only.
- It describes possible service boundaries and responsibilities.

## Recommended microservices
1. `Auth Service`
   - Handles user authentication and authorization.
   - Manages login, registration, token generation, and user roles.
   - Exposes endpoints for login, register, refresh token, and user claims.

2. `User Service`
   - Manages user profiles and account data.
   - Handles user retrieval, updates, and user-related business logic.

3. `Gift Service`
   - Manages gifts and inventory.
   - Handles gift creation, updates, categories, and availability.

4. `Raffle Service`
   - Manages raffle events, tickets, and drawing logic.
   - Handles raffle creation, ticket purchase, results, and raffle status.

5. `Donor Service`
   - Manages donor information and donor-related operations.
   - Handles donor records, donation tracking, and donor statistics.

6. `Basket Service`
   - Manages shopping baskets or ticket baskets.
   - Handles basket contents, add/remove items, and checkout processes.

7. `Statistics Service`
   - Aggregates and exposes reporting data.
   - Handles summaries, charts, and performance metrics.

## Integration notes
- Each microservice should have a clear API contract.
- Shared data contracts should be isolated in a common schema or gateway.
- Avoid coupling controllers and repositories across services in the planning.

## Important
- This is a planning document only.
- Do not implement or use it as actual code yet.
