# Exercise 4: Signals and Queries

## What you'll learn

- How to send external events into a running workflow using signals
- How to read workflow state from outside using queries
- How `Workflow.WaitConditionAsync` gates progress on a condition
- How signals and queries appear in the Temporal event history

## The starting point

High-value vehicle transactions (over $50,000) require manager approval before
payment can proceed. The fraud check runs first; if the order is high-value, the
workflow pauses and waits indefinitely for an approval signal before continuing.

Signals are how the outside world pushes state changes into a running workflow.
Queries are how the outside world reads workflow state without affecting it.

---

## Part A: Implement the signal handler

Open `VehicleTransactionWorkflow.cs`. Find the first `TODO`.

Add a method named `ApproveTransactionAsync` decorated with `[WorkflowSignal]`.
When called, it should set `_managerApproved = true`. Signal handlers are `async`
but typically have no body other than state mutation — they cannot return values
to the caller.

---

## Part B: Implement the query handler

Find the second `TODO`. Add a method named `GetStatus` decorated with
`[WorkflowQuery]`. It should return a `WorkflowStatus` record built from the three
private state fields. Query handlers must be synchronous — no `await`, no state
mutation.

---

## Part C: Add the approval gate

Find the third `TODO` in `RunAsync`. After setting `_stage = "awaiting-approval"`,
use `Workflow.WaitConditionAsync` to pause until `_managerApproved` is true. The
workflow will sit here — durably, with no polling — until the signal arrives.

---

## Run it

**Terminal 1 — start the worker:**
```bash
dotnet run -- worker
```

**Terminal 2 — start a workflow** (it will pause waiting for approval):
```bash
dotnet run -- starter
```

**Terminal 2 — query the workflow status** (in a new terminal or after the starter prints the workflow ID):
```bash
dotnet run -- query vehicle-tx-VIN-2026-COXAUTO-004
```

**Terminal 2 — send the approval signal:**
```bash
dotnet run -- approve vehicle-tx-VIN-2026-COXAUTO-004
```

Watch the starter terminal — the workflow resumes immediately after the signal
arrives and completes.

Open the **Temporal UI** and find the `WorkflowSignalReceived` event in the history.
Notice the workflow was paused between that event and the `ActivityTaskScheduled`
for payment.

---

## Check your work

Click **Check** when at least one workflow completes successfully after receiving
an approval signal.

Stuck? Click **Solve** to see the reference solution.
