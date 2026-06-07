# Exercise 2: Child Workflows

## What you'll learn

- When a sub-process deserves its own event history and retry boundary
- How to define and register a second workflow class
- How to call a child workflow from a parent using `Workflow.ExecuteChildWorkflowAsync`
- How child workflows appear as separate entries in the Temporal UI

## The starting point

The transaction workflow from Exercise 1 now needs to generate documents after the
sale completes: a bill of sale and a title document. Document generation is a
multi-step process that another team might own — and if it fails, you don't want
those failures cluttering the parent workflow's event history.

Child workflows give a sub-process its own history, its own retry boundary, and
its own identity in the Temporal UI. The parent just waits for the result.

---

## Part A: Implement the child workflow

Open `DocumentGenerationWorkflow.cs`. It has one `TODO` in `RunAsync`.

Call the two document generation activities in sequence and return a `DocumentResult`
with both URLs. This is the same pattern as Exercise 1 — `Workflow.ExecuteActivityAsync`
with the shared `DefaultOptions`.

---

## Part B: Call the child workflow from the parent

Open `VehicleTransactionWorkflow.cs`. The three transaction activities are already
implemented. Find the `TODO` at the bottom of `RunAsync`.

Use `Workflow.ExecuteChildWorkflowAsync` to execute `DocumentGenerationWorkflow`.
Build a `DocumentRequest` from the order and `paymentConfirmation`, then capture
the `DocumentResult` it returns to construct the final `TransactionResult`.

Use `$"docs-{order.VehicleId}"` as the child workflow ID so it has a meaningful,
predictable name in the UI.

---

## Part C: Run it

**Terminal 1 — start the worker:**
```bash
dotnet run -- worker
```

**Terminal 2 — start a workflow:**
```bash
dotnet run -- starter
```

Open the **Temporal UI** tab. You should see two workflow entries: the parent
`vehicle-tx-VIN-2026-COXAUTO-002` and a child `docs-VIN-2026-COXAUTO-002`. Click
into each and compare their event histories — they're completely separate.

---

## Check your work

Click **Check** when at least one workflow completes successfully.

Stuck? Click **Solve** to see the reference solution.
