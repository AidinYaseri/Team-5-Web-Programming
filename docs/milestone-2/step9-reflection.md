# Step 9: Reflection

Owner: Chloe (with input from Darcy and Aiden)

<!-- This is a starting draft. Each person should check their part and add what actually
happened for them in Copilot. Replace the bracketed notes, then delete these comments.
Sentences ending in (check) describe what we expect Copilot to do. Keep them only if it
really happened, otherwise rewrite them. The DI bug part is real: it happened during Step 7. -->

## What the AI flow got right

Having everything start from a GitHub issue helped a lot. Once the issue had clear inputs, outputs and acceptance criteria, Copilot stayed on that feature and didn't wander off adding random stuff. The MCP connection was also useful for triage. Copilot could search our issues, PRs and code in one go, which showed us pretty fast that nobody else was working on pets and that no controller existed yet.

The priming step was worth it. Making Copilot read the files and say the problem back before writing anything caught a few wrong guesses early, like assuming we already had a DbContext. (check) Asking for the plan in a fresh session gave us a list of files and an order to do them in, so the implementation came in small steps we could actually check one at a time.

Writing `copilot-instructions.md` before starting also paid off. Copilot followed our layer rules, used DTOs and returned `ValidationProblem` without us having to repeat it in every prompt. (check)

[Darcy: add one thing Copilot did well during implementation.]

## What it got wrong

The biggest problem was a bug that every test missed. The DI registration in `Program.cs` looked normal, but the container picked the constructor that takes a list of pets and passed in an empty one. So the real API always returned an empty array. The unit tests created the service themselves and the endpoint tests swapped in their own data, so nothing ever went through the real setup. We only noticed when we ran the API and called it with curl. We fixed it with a factory registration and added a test that uses the real startup code.

The class diagram and the `.http` file were also left out of date at first, even though our rules say to update them. And the new method is synchronous even though our own rules say I/O should be async.

[Aiden: add anything Copilot's PR review flagged that was wrong or useless.]

## What still needed a human

A lot of the decisions were ours, not Copilot's. We picked exact species matching instead of partial, the 0 to 100 age range, and what was out of scope. We also decided which review findings to fix now and which to push to later, like making the interfaces async. That one would have changed six files for no real gain today.

Testing needed judgment too. Passing tests didn't mean the feature worked. Running the app by hand is what found the DI bug, and the mutation check showed which tests actually catch mistakes.

[Chloe: one or two sentences on what you would do differently next time.]
