# Exercise 3: Parallel Activities

## What you'll learn

- How to run independent activities concurrently using `Task.WhenAll`
- Why parallel activities are safe and deterministic in Temporal
- How to read the event history to confirm activities ran at the same time

## The starting point

The transaction now includes an inventory confirmation step: before charging the
buyer, we verify the vehicle is still available. The fraud check and inventory
confirmation are completely independent — neither needs the other's result to start.

Running them sequentially is wasteful. This exercise shows how to fan them out
in parallel and gate on both completing.

---

## Your task

Open `VehicleTransactionWorkflow.cs`. Find the `TODO` in `RunAsync`.

Start `CheckFraudAsync` and `ConfirmInventoryAsync` without immediately awaiting
either. This gives you two `Task` objects running concurrently. Then use
`Task.WhenAll` to wait for both to complete before continuing to payment.

If either activity fails (after all retries), the workflow fails at the
`Task.WhenAll` line — payment never proceeds.

---

## Run it

**Terminal 1 — start the worker:**
```bash
dotnet run -- worker
```

**Terminal 2 — start a workflow:**
```bash
dotnet run -- starter
```

Open the **Temporal UI** tab and look at the event history. You should see the
`ActivityTaskScheduled` events for both checks appear at nearly the same sequence
number — they were scheduled in the same workflow task.

---

## Check your work

Click **Check** when at least one workflow completes successfully.

Stuck? Click **Solve** to see the reference solution.
