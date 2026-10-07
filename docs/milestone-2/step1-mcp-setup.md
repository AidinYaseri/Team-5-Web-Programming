# Step 1: Connect Copilot to GitHub with MCP

Owner: Aiden

## What we set up

Copilot Agent mode talks to GitHub through the official GitHub MCP server. The config is committed in [`.vscode/mcp.json`](../../.vscode/mcp.json), so everyone on the team gets the same server when they open the repo:

```json
{
  "servers": {
    "github": {
      "type": "http",
      "url": "https://api.githubcopilot.com/mcp/"
    }
  }
}
```

This is GitHub's hosted (remote) MCP server. It signs in with your GitHub account through OAuth, so there is no personal access token in the file and nothing secret gets committed.

## How to turn it on

**VS Code**

1. Install the GitHub Copilot and GitHub Copilot Chat extensions and sign in with your GitHub account.
2. Open the repo folder. VS Code finds `.vscode/mcp.json` on its own.
3. Open `.vscode/mcp.json` and click **Start** above the `github` server (or run `MCP: List Servers` from the command palette and start it).
4. Accept the GitHub sign-in prompt.
5. Open Copilot Chat, switch the mode to **Agent**, and click the tools icon. The `github` tools (issues, pull requests, search, and so on) should be listed and checked.

**Visual Studio 2022 (17.14 or newer)**

1. Open `Team-5-Web-Programming.slnx`. Visual Studio also reads `.vscode/mcp.json` from the solution folder.
2. Open Copilot Chat, pick **Agent** mode, and click the tools icon.
3. Enable the `github` server and sign in when asked.

## How we checked it works

Ran these prompts in Agent mode. Each one has to call a GitHub MCP tool, not just answer from memory:

| Prompt | Expected result |
|---|---|
| `List the open issues in AidinYaseri/Team-5-Web-Programming.` | Calls the list issues tool and shows the current issues. |
| `Search the code in this repo for IPetService.` | Calls the code search tool and finds `src/Animal.Domain/Services/IPetService.cs`. |
| `List the last 5 pull requests in this repo.` | Shows PRs #6 down to #2. |

## Evidence

- [ ] Screenshot: `github` server running in the MCP server list
- [ ] Screenshot: Agent mode tool picker with the GitHub tools enabled
- [ ] Screenshot: one of the test prompts above with the MCP tool call visible

## Problems we hit

- The tool call has to be approved the first time. Pick "Allow in this workspace" so later steps don't keep asking.
- If the tools don't show up, check that Chat is in **Agent** mode. Ask mode can't use MCP tools.
