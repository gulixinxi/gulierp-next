#!/usr/bin/env node

/**
 * Stop hook: audit all modified files for console.log before session ends
 * Scans git diff for console.log statements
 */

import { execSync } from "node:child_process";
import { readFileSync } from "node:fs";

try {
  const diff = execSync("git diff --name-only HEAD", {
    encoding: "utf8",
    timeout: 5000,
    stdio: ["pipe", "pipe", "ignore"],
  }).trim();

  if (!diff) process.exit(0);

  const files = diff
    .split("\n")
    .filter((f) => f.match(/\.(js|ts|jsx|tsx|mjs|cjs)$/));

  const violations = [];

  for (const file of files) {
    try {
      const content = readFileSync(file, "utf8");
      const matches = content.match(/console\.(log|debug|info)\(/g);
      if (matches) {
        violations.push(`${file}: ${matches.length} console.log(s)`);
      }
    } catch {
      // file deleted or not readable
    }
  }

  if (violations.length > 0) {
    process.stderr.write(
      `[audit] console.log found in modified files:\n${violations.join("\n")}\n`
    );
  }
} catch {
  // git not available or not a repo
}
