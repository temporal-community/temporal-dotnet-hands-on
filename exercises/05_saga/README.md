# Exercise 5: Saga / Compensation

## What you'll learn

- Why distributed transactions need explicit compensation logic
- How to implement the Saga pattern using a compensation stack
- How to configure separate retry policies for forward and compensation activities
- How to observe compensations running in the Temporal UI event history

## The starting point

Back in Exercise 1, a title transfer failure after successful payment left the
transaction half-applied — money charged, no title transferred, no way to undo it.

Temporal retries transient failures automatically, but it cannot magically roll
back side effects that already happened in external systems. That's your
responsibility. The Saga pattern gives you a structured way to do it.

The approach: after each step with an irreversible side effect, register a
compensation action on a stack. If any later step fails permanently, unwind the
stack — running compensations in reverse.

---

## Your task

Open `VehicleTransactionWorkflow.cs`. The entire body of `RunAsync` is a single
`TODO`.

Implement the Saga using a `Stack<Func<Task>>`:

1. Create an empty `compensations` stack and wrap all forward steps in a `try` block.
2. Run `CheckFraudAsync` — it's read-only, so no compensation is needed.
3. Run `ProcessPaymentAsync` and capture the confirmation string. Immediately push
   a `RefundPaymentAsync` compensation onto the stack.
4. Run `TransferTitleAsync`. Push a `RevertTitleTransferAsync` compensation.
5. In the `catch` block, iterate the stack and `await` each compensation in turn.
   Then re-throw — a compensated failure is still a failure.
6. After the `try/catch`, return a `TransactionResult` with `Status = "Completed"`.

Use `DefaultOptions` for forward activities and `CompensationOptions` for
compensations — both are already defined in the class.

---

## Run it

**Terminal 1 — start the worker:**
```bash
dotnet run -- worker
```

**Terminal 2 — start workflows** (run a few times — title transfer fails ~60%):
```bash
dotnet run -- starter
```

When a title transfer fails after payment, you should see in the worker terminal:
```
[Compensation] Refunded payment PAY-XXXXXXXXXXXX for buyer buyer-riley-w
```

Open the **Temporal UI** and look at a failed workflow's event history. Find the
`ActivityTaskScheduled` event for `RefundPaymentAsync` — it appears after the
title transfer failure, proving the compensation ran as part of the same workflow
execution.

---

## Check your work

Click **Check** when at least one workflow has run compensations successfully.
The checker looks for a `RefundPaymentAsync` activity in a failed workflow's history.

Stuck? Click **Solve** to see the reference solution.
