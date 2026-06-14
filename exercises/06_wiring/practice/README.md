# Exercise 6: Wiring Activities and Signals

Two-stage exercise. Complete Stage A first, then Stage B.

## Stage A — Wire activities and retry policies (20 min)

The workflow calls FraudService, PaymentService, and TitleService directly.
That is wrong: direct service calls inside a workflow break durability,
bypass Temporal's retry machinery, and leave no trace in the event history.

Your job:

1. **Move the service calls** from `VehicleTransactionWorkflow.cs` into
   `VehicleTransactionActivities.cs`. Add `[Activity]` to each method.

2. **Define two `ActivityOptions` fields** in the workflow:
   - `FraudOptions`: short timeout (10s), aggressive retry (1s → 10s, coefficient 2.0)
   - `PaymentOptions`: longer timeout (60s), conservative retry (2s → 60s, coefficient 2.0, max 5 attempts)

3. **Replace each direct call** with `Workflow.ExecuteActivityAsync(...)`,
   passing the right options for each activity.

4. **Register the activities** with the worker in `Program.cs`.

Think about why fraud and payment get different retry policies before you assign them.

## Stage B — Add a signal and approval gate (20 min)

High-value vehicle sales (over $50,000) require a manager to approve before
payment can proceed. Temporal can pause a workflow for hours or days, consuming
no resources, and resume the instant a signal arrives.

Your job:

1. **Add a signal handler** `ApproveTransactionAsync` decorated with `[WorkflowSignal]`.
   Set `_managerApproved = true` when it fires.

2. **Add a query handler** `GetStatus` decorated with `[WorkflowQuery]`.
   Return a `WorkflowStatus` with the three state fields. No `await`, no mutation.

3. **Add the approval gate** in `RunAsync`: after the fraud check, if
   `order.PurchasePrice > 50_000m`, set the state fields and call
   `await Workflow.WaitConditionAsync(() => _managerApproved)`.

## Running the exercise

```bash
# Terminal 1 — start the worker
dotnet run -- worker

# Terminal 2 — standard order (no approval needed)
dotnet run -- starter

# Terminal 2 — high-value order (pauses for approval)
dotnet run -- starter high

# Terminal 2 — query the paused workflow
dotnet run -- query vehicle-tx-VIN-2026-COXAUTO-006

# Terminal 2 — send the approval signal
dotnet run -- approve vehicle-tx-VIN-2026-COXAUTO-006
```

The solution is in `../solution/` if you need to compare.
