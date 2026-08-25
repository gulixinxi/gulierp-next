#!/usr/bin/env node

/**
 * PostToolUse hook: auto-format JS/TS files after Edit/Write
 * Runs prettier on the modified file if it's a .js/.ts/.jsx/.tsx file
 *
 * Input: JSON on stdin (Claude Code hooks). See https://code.claude.com/docs/en/hooks
 */

import { execFile } from "node:child_process";
import process from "node:process";
import { readJsonFromStdin, extractFilePath } from "./utils.mjs";

const input = await readJsonFromStdin();
const toolName = input.tool_name || "";
const filePath = extractFilePath(input.tool_input || input);

if (!["Edit", "Write"].includes(toolName)) process.exit(0);
if (!filePath.match(/\.(js|ts|jsx|tsx|mjs|cjs)$/)) process.exit(0);

try {
  const command = process.platform === "win32" ? "npx.cmd" : "npx";
  execFile(
    command,
    ["prettier", "--write", filePath],
    {
      stdio: "ignore",
      timeout: 10000,
      cwd: input.cwd || process.cwd(),
      windowsHide: true,
    },
    () => {},
  );
} catch {
  // prettier not available or failed — no big deal
}
