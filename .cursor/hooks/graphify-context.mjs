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

const payload = readPayload();
const cwd = typeof payload.cwd === "string" && payload.cwd ? payload.cwd : process.cwd();
const graphPath = path.join(cwd, "graphify-out", "graph.json");

if (existsSync(graphPath)) {
  console.log(
    JSON.stringify({
      systemMessage: "graphify: Knowledge graph exists. Read graphify-out/GRAPH_REPORT.md for god nodes and community structure before searching raw files.",
    }),
  );
}
