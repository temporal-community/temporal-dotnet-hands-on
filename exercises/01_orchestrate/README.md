# Challenge 1: Orchestrating Distributed Work

## What you'll learn

- How to turn three unreliable service calls into a durable Temporal workflow
- How a `RetryPolicy` in `ActivityOptions` replaces hand-written retry loops
- What "durable execution" actually means — by killing the worker mid-run
- How to use `Workflow.Logger`, `Workflow.Info`, and `Workflow.UtcNow`
- How to build a retry-safe idempotency key from `ActivityExecutionContext`

## The domain

An auto-loan underwriting pipeline. Three steps run in order:

1. **Pull credit** — fetch the applicant's credit file from a bureau
2. **Check fraud** — screen the applicant against a fraud service
3. **Score risk** — turn the credit score into a tier and an interest rate

The credit bureau is unreliable. In this lab it fails its first two attempts
every time, so you can watch a retry policy do its job.

---

## Part A — Implement the activities

Open `LoanUnderwritingActivities.cs`. Three `TODO`s, one per activity.

The interesting one is `PullCreditAsync`. Inside an activity you can reach
`ActivityExecutionContext.Current`, which carries the activity's Temporal
identity:

```csharp
var info   = ActivityExecutionContext.Current.Info;
var logger = ActivityExecutionContext.Current.Logger;
```

`info.Attempt` is the current attempt number — log it, and you'll see retries
happen in real time. `info.WorkflowId` and `info.ActivityId` together make a
good idempotency key:

```csharp
var idempotencyKey = $"{info.WorkflowId}:{info.ActivityId}";
```

**Why those two fields?** `ActivityId` is stable across every retry of the same
call, but unique against every other activity call. Pass that key downstream and
a retried credit pull is recognised as a duplicate rather than charged twice.
A `Guid.NewGuid()` would change on every attempt and defeat the whole point.

For the fraud check, a flagged applicant is a *business* outcome, not a
transient error. Throw `ApplicationFailureException(..., nonRetryable: true)`
so Temporal stops immediately instead of retrying toward the same answer.

---

## Part B — Implement the workflow

Open `LoanUnderwritingWorkflow.cs`. Two `TODO`s.

**First — `ActivityOptions`.** Define `CreditOptions` for the flaky bureau
(initial interval 5s, backoff coefficient 3.0, max 5 attempts) and a plainer
`DefaultOptions` for fraud and risk. The credit policy puts attempts at roughly
t=0s, t=5s and t=20s. You want that 15-second gap — it's your window in Part C.

**Second — `RunAsync`.** Capture `Workflow.UtcNow`, log with
`Workflow.Logger`, then call the three activities in sequence with
`Workflow.ExecuteActivityAsync`.

> **Determinism rule:** inside workflow code, never use `DateTime.Now`,
> `Random`, `Guid.NewGuid`, or `Task.Delay`. The workflow replays from history
> after any crash, and those four return something different every replay. Use
> `Workflow.UtcNow`, `Workflow.Random`, `Workflow.NewGuid`, and
> `Workflow.DelayAsync` instead. Activities have no such restriction — that is
> where the messy, non-deterministic work belongs.

---

## Part C — Run it, then kill the worker

**Terminal 1 — start the worker:**
```bash
dotnet run -- worker
```

**Terminal 2 — submit an application:**
```bash
dotnet run -- starter
```

Watch Terminal 1. You should see the bureau fail twice and succeed on the third
attempt, with the same idempotency key each time:

```
19:35:34 info: Pulling credit for applicant-4471 (attempt 1, idempotency key loan-APP-...:1)
19:35:34 warn: Credit bureau returned 503 on attempt 1 — Temporal will retry
19:35:39 info: Pulling credit for applicant-4471 (attempt 2, idempotency key loan-APP-...:1)
19:35:54 info: Pulling credit for applicant-4471 (attempt 3, idempotency key loan-APP-...:1)
```

**Now the important part.** Run the starter again, and the moment you see the
*attempt 2* failure, kill the worker in Terminal 1 with `Ctrl+C`. You have about
fifteen seconds before the third attempt is due.

Count to five. Start the worker again:

```bash
dotnet run -- worker
```

The workflow finishes. Not restarts — *finishes*, from where it was. The
application state was never in the worker's memory; it was in the event
history on the server, and the new worker picked it up. That is durable
execution, and it is the entire point.

---

## Part D — Observe in the Web UI

Open the **Temporal UI** tab and find your workflow. In the event history,
open the `ActivityTaskStarted` event for `PullCredit` and look at `attempt`.

Notice what is *not* there: there is no event per failed attempt. Temporal
records the retries on the started event rather than writing three events, so
a chatty retry loop doesn't bloat your history.

---

## Check your work

Click **Check** once a workflow has completed.

Stuck? Click **Solve** to drop the reference solution into your editor.
