import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import process from "node:process";

function readPayload() {
  try {
    const raw = readFileSync(0, "utf8");
    return raw.trim() ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

function commandFromPayload(payload) {
  const input = payload?.tool_input ?? payload?.toolInput ?? payload?.input ?? payload;
  return typeof input?.command === "string" ? input.command : "";
}

function isSearchCommand(command) {
  return /\b(grep|rg|ripgrep|find|fd|ack|ag)\b/i.test(command);
}

const payload = readPayload();
const cwd = typeof payload.cwd === "string" && payload.cwd ? payload.cwd : process.cwd();
const graphPath = path.join(cwd, "graphify-out", "graph.json");
const command = commandFromPayload(payload);

if (isSearchCommand(command) && existsSync(graphPath)) {
  console.log(
    JSON.stringify({
      hookSpecificOutput: {
        hookEventName: "PreToolUse",
        additionalContext:
          "graphify: knowledge graph at graphify-out/. For focused questions, run `graphify query \"<question>\"` first; read GRAPH_REPORT.md only for broad architecture context.",
      },
    }),
  );
}
