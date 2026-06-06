# Exercise 1: Converting a Workflow

## What you'll learn

- Why manual retry loops fail under real distributed system conditions
- How to extract activity logic from an ad-hoc pipeline into Temporal Activities
- How to write a Temporal Workflow that orchestrates sequential activities
- What durable execution looks like in the Temporal Web UI

## The starting point

Open `VehicleTransactionPipeline.cs`. It processes a vehicle purchase through three
steps: fraud check, payment, and title transfer. Read it before you start.

Notice what's wrong:
- Each step has its own hand-rolled retry loop with different logic
- All state lives in local variables — if this process crashes mid-transaction, the
  order is gone with no way to resume
- A title transfer failure after payment succeeds leaves the transaction half-applied
- There is no visibility into what happened or why

Your job is to replace this with a Temporal workflow so that Temporal owns retries,
state, and durability — and your code only describes the happy path.

---

## Part A: Implement the Activities

Open `VehicleTransactionActivities.cs`. The three `[Activity]`-annotated methods
correspond directly to the three steps in the pipeline. Each has a `TODO` describing
what to implement.

**The pattern:** an activity method calls one external service and returns its result.
Temporal retries the method automatically when it throws. For failures that should
*not* be retried (a business rule violation, not a transient error), throw
`ApplicationFailureException` with `nonRetryable: true`.

**What you're replacing:** the `for` loops, the `Task.Delay` backoff math, the
`if (attempt == N) throw` guards. None of that belongs in your code. The `[Activity]`
attribute and your retry policy (set in Part B) replace all of it.

---

## Part B: Implement the Workflow

Open `VehicleTransactionWorkflow.cs`. There are two `TODO`s.

**First TODO — ActivityOptions:** define a static `ActivityOptions` field with a
`StartToCloseTimeout` and a `RetryPolicy`. The timeout tells Temporal how long a
single activity attempt may run before it's considered stuck. The retry policy
replaces the backoff math in the pipeline.

**Second TODO — RunAsync:** call all three activities in sequence using
`Workflow.ExecuteActivityAsync`, then return a `TransactionResult`.

`Workflow.ExecuteActivityAsync` takes a lambda that identifies the activity method
and the options you defined. The lambda is never executed locally — it tells Temporal
*what to schedule*, and Temporal dispatches it to a worker.

---

## Part C: Run it

**Terminal 1 — start the worker:**
```bash
dotnet run --project VehicleTransaction.csproj -- worker
```

**Terminal 2 — start a workflow:**
```bash
dotnet run --project VehicleTransaction.csproj -- starter
```

Open the **Temporal Web UI** tab and find workflow `vehicle-tx-VIN-2026-COXAUTO-001`.

Try this: kill the worker (Ctrl+C in Terminal 1) while a workflow is in progress.
Restart it. What happens to the in-flight transaction?

Then look at the Event History for a completed workflow. Find where the retries
happened. How many attempts did each activity take?

---

## Check your work

Click **Check** when your workflow completes successfully. The checker will verify
that your activity methods are implemented and that the project builds cleanly.

Stuck? Click **Solve** to see the reference solution.
