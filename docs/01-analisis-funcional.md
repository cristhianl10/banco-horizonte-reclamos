# Functional Analysis — Banco Horizonte

## Problem and objective

Claims arrive through email, calls, forms, and independent spreadsheets. Information is fragmented, prioritization is subjective, SLA breaches are hard to detect, and supervisors lack reliable indicators. The application centralizes registration, automatic prioritization, assignment, resolution, audit, and supervision.

## Roles

| Role | Responsibilities |
|---|---|
| Operator | Register claims and review calculated code, score, priority, and SLA. |
| Analyst | View details, take unassigned cases, change status, and add observations. |
| Supervisor | Review queues, alerts, indicators, assignment, and reassignment. |

Supabase authentication is implemented. Public registration assigns `Operator`; other roles use the administrative script.

## Workflow

1. Record customer, channel, category, subcategory, description, optional amount, reception date, and digital availability.
2. Search for recent possible duplicates.
3. Generate a unique code and calculate score, priority, SLA, 75% alert, and deadline.
4. Assign or reassign an analyst.
5. Move `New` to `In analysis` with a required observation.
6. Finish as `Resolved` or `Rejected`; cases cannot be reopened.
7. Record creation, assignment, observations, recalculation, and status changes in history.
8. Recalculate risk in queue, detail, and dashboard views.

## Automatic prioritization

Rules are cumulative:

| Condition | Points |
|---|---:|
| Unrecognized transaction or purchase | +4 |
| Uncredited transfer or blocked access/channel | +3 |
| Amount at least USD 500 | +3 |
| Digital channel completely unavailable | +2 |
| Claim open more than 24 hours | +2 |

| Score | Priority | SLA |
|---:|---|---:|
| 0–2 | Low | 24 hours |
| 3–4 | Medium | 12 hours |
| 5–6 | High | 6 hours |
| 7+ | Critical | 2 hours |

`sla_deadline = reception_date + SLA hours`; the alert date is at 75% of the SLA. Valid transitions are `New → In analysis → Resolved/Rejected`, plus `New → Rejected`. Every status change requires an observation, timestamp, and actor.

## Functional traceability

RF-01 registration and validation; RF-02 automatic score/priority/SLA; RF-03 queue search and filters; RF-04 case detail and history; RF-05 assignment; RF-06 controlled transitions; RF-07 dashboard; RF-08 SLA alerts; RF-09 user-oriented errors; RF-10 idempotent synthetic demo data.

The implementation uses Angular and PostgreSQL/Supabase with ASP.NET Core as the only business-data access layer. The browser uses Supabase for authentication only.