# Challenge 2: Human in the Loop

## What you'll learn

- How `[WorkflowUpdate]` lets an outside caller change a running workflow *and get an answer back*
- How an update validator rejects bad input before it reaches the event history
- How `[WorkflowQuery]` exposes live state without disturbing the workflow
- How `Workflow.WaitConditionAsync` waits — for minutes or for weeks — at zero cost
- How to escalate on a timeout without taking the decision away from a human

## The starting point

Challenge 1 ran straight through. Real underwriting doesn't: once the automated
checks are done, a person has to rule on the application. That person might
answer in thirty seconds or next Tuesday.

The three activity calls are already wired up for you. Your job is the
human-in-the-loop part.

> **Why this is hard without Temporal.** You'd persist the half-finished
> application to a database, write a scheduler to notice stalled rows, build a
> callback endpoint for the underwriter, and reconstruct state when the answer
> arrives. Here the workflow just *waits*. It holds no thread and no memory on
> any worker — the state is on the server. You can restart or redeploy every
> worker while an application waits, and the decision still lands correctly.

---

## Part A — The update handler (TODO 1)

Decorate `SubmitDecisionAsync` with `[WorkflowUpdate]` and record the ruling.

**Update or signal?** A signal is fire-and-forget: the sender learns nothing.
An update is a round trip — the caller waits for the handler and gets its
return value. An underwriter submitting a ruling should find out whether it was
accepted, so this is an update.

## Part B — The validator (TODO 2)

Add a method decorated with `[WorkflowUpdateValidator(nameof(SubmitDecisionAsync))]`.

A validator runs *before* the update is accepted. Throw from it and the update
is rejected, the caller is told why, and **nothing is written to the event
history** — the workflow never sees the bad input. Reject any decision that
isn't `approve` or `decline`, and reject a second ruling on an application
that's already decided.

Validators must not mutate state and must not `await`.

## Part C — The query handler (TODO 3)

`GetStatus` is already written. It just needs `[WorkflowQuery]`.

Queries read state without changing it and never appear in the event history,
so you can poll one as often as you like. They must be synchronous.

## Part D — The wait and the escalation (TODO 4)

`Workflow.WaitConditionAsync` takes a predicate and an optional timeout. With a
timeout it returns `bool`: `true` if the condition came true, `false` if the
timeout won.

```csharp
var decided = await Workflow.WaitConditionAsync(
    () => _decision is not null,
    EscalationThreshold);
```

If it times out, set `_escalated`, log a warning, then **wait again with no
timeout**.

That second wait is the whole design. We don't fail the workflow and we don't
auto-decline — an application nobody looked at is not a declined application.
The timer raises a flag; it does not make the decision for the desk.

---

## Run it

**Terminal 1 — start the worker:**
```bash
dotnet run -- worker
```

**Terminal 2 — submit an application:**
```bash
dotnet run -- starter
```

It prints a workflow id like `loan-APP-976545DF`, runs the automated checks,
then stops at `awaiting-decision`.

**Terminal 3 — read its state without touching it:**
```bash
dotnet run -- status loan-APP-976545DF
```

```
Stage:        awaiting-decision
Credit score: 755
Risk tier:    near-prime
Decision:     (none yet)
Escalated:    False
```

**Terminal 3 — try an invalid ruling first.** Your validator should refuse it:
```bash
dotnet run -- decide loan-APP-976545DF maybe
```
```
[Decide] Rejected: Decision must be 'approve' or 'decline', got 'maybe'
```

**Now rule on it properly:**
```bash
dotnet run -- decide loan-APP-976545DF approve
```
```
[Decide] Accepted: Recorded 'approve' by underwriter-dana-k
```

Terminal 2 unblocks immediately and prints the result.

---

## See the escalation fire

`EscalationThreshold` is two minutes. Start another application and simply
leave it alone. After two minutes the worker logs:

```
warn: No decision on APP-XXXXXXXX after 2 minutes — escalating to the senior desk. Still waiting.
```

Query it again and `Escalated` is now `True` while `Stage` reads `escalated`.
The application is still live. Rule on it whenever you like — it will be
accepted exactly as before.

Impatient? Change `EscalationThreshold` to `TimeSpan.FromSeconds(20)` and
restart the worker.

---

## Observe in the Web UI

Open the **Temporal UI** tab and find your application. In the history you'll
see `WorkflowExecutionUpdateAccepted` and `WorkflowExecutionUpdateCompleted`
around your ruling.

What you will *not* see is your query, or the rejected `maybe` ruling. Queries
never touch history, and a validator rejection is turned away before it gets
there.

---

## Check your work

Click **Check** once an application has been decided.

Stuck? Click **Solve** to drop the reference solution into your editor.
