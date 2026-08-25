#!/usr/bin/env node
/**
 * Optional Stop-hook guard against premature "done" claims (weak heuristic).
 *
 * OFF by default (zero noise). Enable:
 *   META_KIM_STOP_COMPLETION_GUARD=hint   → stderr reminder only; stop proceeds
 *   META_KIM_STOP_COMPLETION_GUARD=block  → JSON { decision, reason } to continue until governance cues appear
 *
 * Always exits 0 when stop_hook_active is true (avoid Stop hook loops).
 * See https://code.claude.com/docs/en/hooks-guide — Stop hook input includes stop_hook_active and may include transcript_path.
 */

import { createReadStream } from "node:fs";
import { readFile } from "node:fs/promises";
import process from "node:process";
import readline from "node:readline";

const COMPLETION_RE =
  /(?:任务已全部完成|全部完成|已经完成|可以发布了|实施完成|工作已完成|圆满完成|大功告成|已全部搞定|done\.?\s*All tasks|implementation complete|fully complete|ready to (?:ship|publish)|all set(?:\s|$)|mission accomplished)/i;

const EVIDENCE_RE =
  /verificationPacket|summaryClosed|publicDisplay|validate:run|closeFindings|verified_closed|summaryPacket|npm run validate/;

async function readStdinJson() {
  const chunks = [];
  for await (const chunk of process.stdin) {
    chunks.push(chunk);
  }
  const raw = Buffer.concat(chunks).toString("utf8").trim();
  if (!raw) {
    return {};
  }
  try {
    return JSON.parse(raw);
  } catch {
    return {};
  }
}

/** Last ~16k chars of assistant-visible text from a JSONL transcript (best-effort). */
async function tailAssistantText(transcriptPath, maxBytes = 16_384) {
  const stat = await readFile(transcriptPath).catch(() => null);
  if (!stat) {
    return "";
  }
  const full = stat.toString("utf8");
  const lines = full.split("\n").filter(Boolean);
  const texts = [];
  for (const line of lines) {
    let row;
    try {
      row = JSON.parse(line);
    } catch {
      continue;
    }
    const msg = row.message ?? row;
    const role = msg.role ?? row.role;
    if (role !== "assistant") {
      continue;
    }
    const c = msg.content;
    if (typeof c === "string") {
      texts.push(c);
    } else if (Array.isArray(c)) {
      for (const part of c) {
        if (part?.type === "text" && typeof part.text === "string") {
          texts.push(part.text);
        }
      }
    }
  }
  const joined = texts.join("\n");
  if (joined.length <= maxBytes) {
    return joined;
  }
  return joined.slice(-maxBytes);
}

/** Streaming tail when file is huge: read last lines and grep assistant-ish JSON (fallback). */
async function tailTranscriptFallback(transcriptPath) {
  return new Promise((resolve) => {
    const parts = [];
    const rl = readline.createInterface({
      input: createReadStream(transcriptPath, { encoding: "utf8" }),
      crlfDelay: Infinity,
    });
    const buf = [];
    const maxLines = 400;
    rl.on("line", (line) => {
      buf.push(line);
      if (buf.length > maxLines) {
        buf.shift();
      }
    });
    rl.on("close", () => {
      for (const line of buf) {
        if (
          line.includes('"role":"assistant"') ||
          line.includes('"role": "assistant"')
        ) {
          parts.push(line);
        }
      }
      resolve(parts.join("\n").slice(-16_384));
    });
    rl.on("error", () => resolve(""));
  });
}

async function main() {
  const input = await readStdinJson();

  if (input.stop_hook_active === true) {
    process.exit(0);
  }

  const mode = (process.env.META_KIM_STOP_COMPLETION_GUARD || "")
    .trim()
    .toLowerCase();
  if (!mode || mode === "0" || mode === "off" || mode === "false") {
    process.exit(0);
  }

  const transcriptPath = input.transcript_path || input.transcriptPath;
  let sample = "";
  if (typeof transcriptPath === "string" && transcriptPath.length > 0) {
    try {
      sample = await tailAssistantText(transcriptPath);
      if (!sample) {
        sample = await tailTranscriptFallback(transcriptPath);
      }
    } catch {
      sample = "";
    }
  }

  if (!sample || sample.length < 20) {
    process.exit(0);
  }

  const claimsDone = COMPLETION_RE.test(sample);
  const hasEvidence = EVIDENCE_RE.test(sample);
  if (!claimsDone || hasEvidence) {
    process.exit(0);
  }

  const reason =
    "[Meta_Kim] You claimed completion but the last assistant text did not reference verification/summary/validate:run. " +
    "If this was a governed complex run: produce or update the run artifact, close findings, and align summaryPacket with config/contracts/workflow-contract.json publicDisplay gates.";

  if (mode === "block") {
    process.stdout.write(JSON.stringify({ decision: "block", reason }));
    process.exit(0);
  }

  if (mode === "hint") {
    process.stderr.write(`${reason}\n`);
    process.exit(0);
  }

  process.exit(0);
}

main().catch(() => process.exit(0));
