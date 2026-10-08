# Step 9: Reflection

Owner: Chloe (with input from Darcy and Aiden)


## What the AI flow got right

Having everything start from a GitHub issue helped a lot. Once issue #7 had clear inputs, outputs and acceptance criteria, Copilot's restatement in step 4 matched it almost word for word, and in step 7 it could map every acceptance criterion to a test. The MCP connection was also useful for triage. Copilot could search our issues, PRs and code in one go, which showed us pretty fast that nobody else was working on pets and that no controller existed yet.

The priming step was worth it, but not on the first try. Copilot's first answer only listed files and then offered to start coding, so we had to ask again for the restatement and questions. The second answer was good and asked 12 questions, including a real conflict we hadn't thought about: our rules say async but `IPetService` is all sync. Because we put our answers straight into the planning prompt, the plan from the fresh session already had the right files and shapes. It still left out the verification section the prompt file asked for, so we added that ourselves.

Writing `copilot-instructions.md` before starting also paid off. In the issue draft, the restatement and the plan, Copilot kept the filter logic out of the controller and used DTOs instead of entities without us repeating it in the prompt.

Darcy: doing the plan one step at a time paid off. I ran each step as its own prompt and built after it, and the Domain, Data, Contracts and Controller steps all built and passed without me having to change anything. The only step that needed a fix was the DI wiring, and that's because it compiled fine but did the wrong thing at runtime.

## What it got wrong

Copilot also got confused about where it was reading from. In step 2 it read our local branch and said `SearchPets` "already exists", which wasn't true on `main`. In triage it said no branch was working on pet filtering because it only looked at branch names. After that we had to say "use main on GitHub" in every prompt and check its claims with git. Setting up MCP was harder than expected too: in Visual Studio the `github` server was there but switched off, and Copilot just said it had no GitHub tools without saying why.

In step 7 its list of 13 edge cases was half useful. We kept 5 that came from the issue's own rules (like `maxAge=100` and a 50 character species) and dropped the rest because they were out of scope or would have meant inventing rules. One of the tests it wrote ended with `Assert.NotNull` on something that could never be null, so it didn't test anything until we changed it.

The biggest problem was a bug that every test missed. The DI registration in `Program.cs` looked normal, but the container picked the constructor that takes a list of pets and passed in an empty one. So the real API always returned an empty array. The unit tests created the service themselves and the endpoint tests swapped in their own data, so nothing ever went through the real setup. We only noticed when we ran the API and called it with curl. We fixed it with a factory registration and added a test that uses the real startup code.

The class diagram and the `.http` file were also left out of date at first, even though our rules say to update them. And the new method is synchronous even though our own rules say I/O should be async.

Aiden: we didn't get the PR review we planned. PR #8 was opened without the description and merged before anyone ran the review prompt or left a review on GitHub. So the review findings in step 8 come from our own checks in step 7, not from a review on the PR. The process only works if someone actually stops at the PR, and we skipped that.

## What still needed a human

A lot of the decisions were ours, not Copilot's. We picked exact species matching instead of partial, the 0 to 100 age range, and what was out of scope. We also decided which review findings to fix now and which to push to later, like making the interfaces async. That one would have changed six files for no real gain today.

Testing needed judgment too. Passing tests didn't mean the feature worked. Running the app by hand is what found the DI bug, and the mutation check showed which tests actually catch mistakes.

Chloe: next time I'd check the MCP tools work and tell Copilot to read `main` on GitHub in the very first prompt, because most of our corrections came from it reading the wrong place. I'd also wait for the review on the PR before merging, since I merged #8 before the review step was done.
