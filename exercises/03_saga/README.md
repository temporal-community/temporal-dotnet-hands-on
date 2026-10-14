# Challenge 3: Saga and Compensation

## What you'll learn

- Why Temporal's durability guarantee stops at your own system's edge
- How to structure compensating actions as a stack that unwinds in reverse
- Why you register a compensation **before** running the action it undoes
- Why that makes idempotent compensations mandatory, not merely polite

## The starting point

The application is approved. Funding it takes three steps, each touching a
different external system:

1. **Place a credit hold** — reserve the funds
2. **Request documents** — kick off the document package
3. **Fund the loan** — disburse the money

Temporal guarantees your workflow runs to completion. It cannot un-ring a bell
in someone else's system. If you placed a hold and the funding partner then
declines the loan, that hold is real and still sitting there. Undoing it is
your job.

The activities and their compensations are already written. Your job is the
orchestration.

---

## Your task

Open `LoanUnderwritingWorkflow.cs`. The body of `RunAsync` is one `TODO`.

```csharp
var compensations = new Stack<Func<Task>>();
try
{
    compensations.Push(() => Run(a => a.ReleaseCreditHoldAsync(app)));
    await Run(a => a.PlaceCreditHoldAsync(app));

    // ...documents, then funding, each the same way...
}
catch
{
    while (compensations.TryPop(out var undo)) await undo();
    throw;
}
```

`Run` is a small helper already defined in the class so the saga reads as the
pattern rather than as Temporal boilerplate.

### The rule that matters

**Push each compensation before you run the action it undoes.**

Push it afterwards and there is a window — the action succeeded, the push had
not happened yet, the worker died — where the undo is lost forever and the
money stays moved. Registering first closes that window.

The cost is that you will sometimes undo something that never happened. That is
the right trade, and it is **why every compensation here is idempotent**: an
undo for an action that never landed must be a harmless no-op.

A `Stack` pops in reverse order, which is exactly what you want: release the
funds before cancelling the documents before releasing the hold.

Re-throw at the end of the `catch`. A compensated failure is still a failure,
and the caller needs to find out.

---

## Run it

**Terminal 1 — start the worker:**
```bash
dotnet run -- worker
```

**Terminal 2 — fund a loan that succeeds:**
```bash
dotnet run -- starter
```

```
[Starter] APP-40FA96DB: Funded (FUND-40FA96DB)
```

**Terminal 2 — now one the funding partner refuses:**
```bash
dotnet run -- starter large
```

This asks for $72,000, over the partner's $50,000 per-loan limit. The hold and
the documents commit; funding is declined. Watch Terminal 1:

```
Placed credit hold HOLD-504D6811 for $72,000.00 on APP-504D6811
Requested document package DOC-504D6811 for APP-504D6811
Funding partner declined APP-504D6811: $72,000.00 exceeds the $50,000.00 limit
APP-504D6811 failed — running 3 compensation(s) in reverse
[Compensation] No funding for APP-504D6811 to reverse (no-op)
[Compensation] Cancelled document package for APP-504D6811
[Compensation] Released credit hold on APP-504D6811
```

**Read those last three lines carefully.** They are the whole exercise.

The compensations ran newest-first. And the first one was a **no-op** — you
registered `ReverseFundingAsync` before calling `FundLoanAsync`, and funding
never happened, so the undo had nothing to undo and said so.

That line is the cost of the push-before-action rule, and it is exactly why
these compensations must be idempotent. An implementation that assumed the
funding existed would have thrown here, in the middle of cleanup, and left the
credit hold stranded — the precise failure the saga was meant to prevent.

Note also that the funding decline is thrown as a **non-retryable**
`ApplicationFailureException`. A business refusal is not a transient fault:
retrying it three times would just reach the same answer more slowly while the
hold sat open.

---

## Observe in the Web UI

Open the **Temporal UI** tab and find the failed application. The history shows
the three forward activities, the failure, and then the compensating activities
scheduled *after* it — all inside one workflow execution.

Nothing external coordinated that cleanup. There is no orchestrator service, no
cleanup cron, no dead-letter queue. The unwind is ordinary code in your `catch`
block, made reliable because Temporal will keep that workflow alive until the
cleanup finishes.

---

## Check your work

Click **Check** once you have run both the successful and the declined paths.

Stuck? Click **Solve** to drop the reference solution into your editor.
