# Planning Workflow

Last updated: 2026-06-13

Development plans live in:

```text
log/plans/
```

The active plan is not discovered by scanning that directory. The active plan is the one linked from:

```text
PROJECT_STATE.md
```

## Planner Window

1. Read `PROJECT_STATE.md`.
2. Read `docs/wiki/architecture.md`.
3. Read relevant runtime docs and historical `log/` files only as needed.
4. Write or update `log/plans/YYYY-MM-DD-topic.md`.
5. Update `log/plans/index.md`.
6. Update `PROJECT_STATE.md` if a builder should execute the plan.

## Builder Window

1. Read `PROJECT_STATE.md`.
2. Read the active plan linked there.
3. Read only source/module docs needed for the files being changed.
4. Implement.
5. Verify.
6. Write changelog and update indexes.

The builder should not bulk-read old plans or choose between historical plans.
