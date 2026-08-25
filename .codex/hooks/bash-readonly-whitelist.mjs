#!/usr/bin/env node
/**
 * Bash Read-Only Whitelist (L2 Hybrid Strategy)
 *
 * Decides whether a Bash command is read-only and therefore safe for meta-agent
 * execution. Used by enforce-agent-dispatch.mjs to gate meta-* identities away
 * from mutating commands while still letting them inspect repository state.
 *
 * Strategy:
 *   1. Command-name allowlist on the first 1-2 tokens (after env-var prefixes).
 *   2. Dangerous-argument blacklist that vetoes the whole command on any hit.
 *   3. Compound commands (pipes / && / ; / ||) are split and every segment must
 *      pass independently.
 *   4. Redirections (> / >>) are inspected at the token level: writes into
 *      /dev/null, NUL, or the OS temp dir are allowed; writes into the working
 *      tree or absolute non-temp paths are rejected. Append (>>) is always
 *      rejected to avoid persistent log mutation.
 *
 * Pure string parsing — no shell, no spawn, no FS. Cross-OS by design.
 *
 * Public:
 *   isReadOnlyBash(command: string): boolean
 *   classifyBashCommand(command: string): { readOnly, reason, segment }
 */
import os from "node:os";
import path from "node:path";

/**
 * Command-name allowlist.
 * Each entry is a sequence of leading tokens that, when matched in order at the
 * start of a command segment, marks that segment as read-only.
 *
 * Examples:
 *   ["ls"]              matches "ls -la"
 *   ["git", "status"]   matches "git status --short" but not "git push"
 *   ["npm", "run", "typecheck"] matches "npm run typecheck -- --watch"
 */
const COMMAND_ALLOWLIST = [
  // Shell navigation / setup
  // Safe as a segment in compound commands such as `cd repo && node --version`.
  // The command only mutates shell-local cwd for the current process and does
  // not touch repo state on disk.
  ["cd"],

  // Filesystem inspection
  ["ls"],
  ["dir"],
  ["pwd"],
  ["cat"],
  ["head"],
  ["tail"],
  ["less"],
  ["more"],
  ["wc"],
  ["stat"],
  ["file"],
  ["tree"],
  ["du"],
  ["df"],

  // Search
  ["find"],
  ["grep"],
  ["egrep"],
  ["fgrep"],
  ["rg"],
  ["ag"],

  // Environment / introspection
  ["echo"],
  ["printf"],
  ["which"],
  ["where"],
  ["whoami"],
  ["hostname"],
  ["date"],
  ["env"],
  ["printenv"],
  ["uname"],

  // Version probes
  ["node", "-v"],
  ["node", "--version"],
  ["npm", "-v"],
  ["npm", "--version"],
  ["pnpm", "-v"],
  ["pnpm", "--version"],
  ["yarn", "-v"],
  ["yarn", "--version"],
  ["python", "--version"],
  ["python3", "--version"],
  ["cargo", "--version"],
  ["rustc", "--version"],
  ["go", "version"],

  // Git (read-only subcommands only)
  ["git", "status"],
  ["git", "log"],
  ["git", "diff"],
  ["git", "show"],
  ["git", "branch"],
  ["git", "remote", "-v"],
  ["git", "remote", "show"],
  ["git", "rev-parse"],
  ["git", "ls-files"],
  ["git", "ls-tree"],
  ["git", "blame"],
  ["git", "describe"],
  ["git", "config", "--get"],
  ["git", "config", "--list"],
  ["git", "tag", "-l"],
  ["git", "tag", "--list"],
  ["git", "stash", "list"],
  ["git", "stash", "show"],

  // npm / pnpm / yarn read-only ops
  ["npm", "list"],
  ["npm", "ls"],
  ["npm", "view"],
  ["npm", "search"],
  ["pnpm", "list"],
  ["pnpm", "ls"],
  ["pnpm", "view"],
  ["pnpm", "why"],
  ["yarn", "list"],
  ["yarn", "info"],

  // Test / typecheck / lint (verification, not mutation)
  ["pnpm", "typecheck"],
  ["pnpm", "test"],
  ["pnpm", "lint"],
  ["pnpm", "exec", "tsc"],
  ["pnpm", "exec", "eslint"],
  ["pnpm", "exec", "prettier", "--check"],
  ["npm", "test"],
  ["npm", "run", "test"],
  ["npm", "run", "typecheck"],
  ["npm", "run", "lint"],
  ["yarn", "test"],
  ["yarn", "typecheck"],
  ["yarn", "lint"],
  ["tsc", "--noEmit"],

  // Cargo (read-only / dry checks)
  ["cargo", "check"],
  ["cargo", "metadata"],
  ["cargo", "tree"],

  // Process / OS inspection
  ["ps"],
  ["top"],
  ["tasklist"],
];

/**
 * Dangerous-argument blacklist.
 * If any of these substrings appear inside a command segment, the whole segment
 * is rejected even if its command name is otherwise on the allowlist.
 * Match is case-insensitive and substring-based.
 */
const DANGEROUS_PATTERNS = [
  // Force / destructive flags
  "--force",
  "-rf",
  "-fr",
  "--hard",
  "--delete",
  "--purge",

  // Network mutation
  "npm publish",
  "npm install",
  "npm i ",
  "pnpm install",
  "pnpm add",
  "pnpm i ",
  "yarn add",
  "yarn install",
  "cargo build",
  "cargo install",
  "cargo publish",
  "cargo run",
  "pip install",
  "pip uninstall",

  // Git mutation
  "git push",
  "git pull",
  "git fetch",
  "git reset --hard",
  "git checkout --",
  "git restore .",
  "git clean -f",
  "git rebase",
  "git merge",
  "git commit",
  "git add ",
  "git rm",
  "git mv",

  // HTTP mutation
  "curl -x post",
  "curl --data",
  "curl -d ",
  "curl -t ",
  "curl --upload",
  "wget --post",

  // Shell escapes / chained execution
  "| sh",
  "| bash",
  "| zsh",
  "| pwsh",
  "; rm",
  "&& rm",
  "|| rm",
  "; del ",
  "&& del ",
  "eval ",
  "exec ",

  // FS mutation primitives
  "rm ",
  "rmdir",
  "del ",
  "mv ",
  "cp ",
  "touch ",
  "mkdir ",
  "chmod ",
  "chown ",
  "kill ",
  "killall",
];

/**
 * Token-level redirection inspection.
 *
 * Allowed (read-only side effect):
 *   - `cmd > /dev/null`           (POSIX null sink)
 *   - `cmd > NUL`                  (Windows null sink, case-insensitive)
 *   - `cmd > $(os.tmpdir())/...`  (system temp dir; any OS)
 *   - `cmd 2> /dev/null`           (stderr to null is also fine)
 *
 * Rejected (mutating):
 *   - `cmd >> anything`            (append = persistent log intent)
 *   - `cmd > ./file`               (writes into CWD / working tree)
 *   - `cmd > /var/log/x.log`       (absolute path outside tmp)
 *   - `cmd > relative/path.txt`    (relative path inside tree)
 *   - Heredoc / process-sub variants (`<()`, `>()`, `<<<`) are ignored here and
 *     fall through to the rest of the pipeline; the command-name allowlist will
 *     catch genuinely mutating cases.
 */
const NULL_SINKS = new Set(["/dev/null", "nul"]);

function isWritableTargetSafe(target) {
  if (!target) return false;
  // Strip surrounding quotes if present.
  const raw = target.replace(/^['"]|['"]$/g, "").trim();
  if (!raw) return false;
  if (NULL_SINKS.has(raw.toLowerCase())) return true;
  const tmpRoot = path.normalize(os.tmpdir());
  const normalized = path.normalize(raw);
  // Absolute path: only allow if inside the OS temp dir.
  if (path.isAbsolute(normalized)) {
    const rel = path.relative(tmpRoot, normalized);
    return rel !== "" && !rel.startsWith("..") && !path.isAbsolute(rel);
  }
  // Relative path is rejected — it would write into the working tree.
  return false;
}

/**
 * Find redirection operators by scanning whitespace-separated tokens.
 * Returns `null` (no offending redirection) or a `{ op, target }` describing
 * the first rejected redirection.
 *
 * `>>` is always rejected. `>` is rejected only when its target is not a
 * known-safe null sink or a path inside the OS temp dir.
 */
function inspectRedirections(segment) {
  // Tokenize on whitespace while preserving operator glyphs. We also split
  // operators that are glued to file targets, e.g. `>/dev/null` or `2>>x`.
  const expanded = segment
    .replace(/(\d?>>)/g, " $1 ")
    .replace(/(\d?>)(?!>)/g, " $1 ")
    .trim();
  const tokens = expanded.split(/\s+/).filter(Boolean);
  for (let i = 0; i < tokens.length; i++) {
    const tok = tokens[i];
    // Strip a leading fd digit (e.g. `2>` → `>`) for the operator check.
    const op = tok.replace(/^\d/, "");
    if (op === ">>") {
      return { op: tok, target: tokens[i + 1] || "" };
    }
    if (op === ">") {
      const target = tokens[i + 1] || "";
      if (/^&\d+$/.test(target)) {
        continue;
      }
      if (!isWritableTargetSafe(target)) {
        return { op: tok, target };
      }
    }
  }
  return null;
}

/**
 * Split a command line into segments based on shell chaining operators while
 * preserving quoted strings. This keeps patterns such as `grep -E "a|b"` from
 * being misread as a pipeline.
 */
function splitSegments(command) {
  if (typeof command !== "string" || !command.trim()) return [];
  const segments = [];
  let current = "";
  let quote = null;
  let escaped = false;

  for (let i = 0; i < command.length; i++) {
    const ch = command[i];
    const next = command[i + 1] || "";

    if (escaped) {
      current += ch;
      escaped = false;
      continue;
    }

    if (ch === "\\") {
      current += ch;
      escaped = true;
      continue;
    }

    if (quote) {
      current += ch;
      if (ch === quote) quote = null;
      continue;
    }

    if (ch === "'" || ch === '"') {
      quote = ch;
      current += ch;
      continue;
    }

    if (ch === ";" || ch === "|" || ch === "&") {
      const isDouble = (ch === "|" && next === "|") || (ch === "&" && next === "&");
      if (ch === "&" && !isDouble) {
        current += ch;
        continue;
      }
      if (current.trim()) segments.push(current.trim());
      current = "";
      if (isDouble) i++;
      continue;
    }

    current += ch;
  }

  if (current.trim()) segments.push(current.trim());
  return segments;
}

/**
 * Strip leading environment-variable assignments such as `FOO=bar BAZ=qux cmd`.
 * Returns the residual command (without the env prefixes).
 */
function stripEnvPrefix(segment) {
  const tokens = segment.split(/\s+/);
  let i = 0;
  while (
    i < tokens.length &&
    /^[A-Za-z_][A-Za-z0-9_]*=/.test(tokens[i] || "")
  ) {
    i++;
  }
  return tokens.slice(i);
}

function startsWithSequence(tokens, sequence) {
  if (sequence.length > tokens.length) return false;
  for (let i = 0; i < sequence.length; i++) {
    if ((tokens[i] || "").toLowerCase() !== sequence[i].toLowerCase()) {
      return false;
    }
  }
  return true;
}

function matchesAllowlist(tokens) {
  if (!tokens.length) return null;
  // Prefer longer prefixes first so that "git status" beats "git" if both ever
  // appeared. Currently the list has no bare "git", but ordering by length is
  // future-proof.
  const ordered = [...COMMAND_ALLOWLIST].sort(
    (a, b) => b.length - a.length,
  );
  for (const seq of ordered) {
    if (startsWithSequence(tokens, seq)) return seq;
  }
  return null;
}

function violatesBlacklist(segment) {
  const lowered = ` ${segment.toLowerCase()} `;
  for (const pat of DANGEROUS_PATTERNS) {
    if (lowered.includes(pat.toLowerCase())) return pat;
  }
  // Token-level redirection check: `>` to /dev/null, NUL, or tmpdir is allowed;
  // `>>` and `>` writing into the working tree are rejected.
  const redir = inspectRedirections(segment);
  if (redir) {
    return `redirect ${redir.op} ${redir.target || "<missing target>"}`.trim();
  }
  // Reject command substitution wrappers — they can hide arbitrary execution.
  if (/\$\([^)]*\)/.test(segment) || /`[^`]*`/.test(segment)) {
    return "command substitution";
  }
  return null;
}

/**
 * Classify a single segment.
 * @returns {{readOnly: boolean, reason: string, match: string|null}}
 */
function classifySegment(segment) {
  const trimmed = (segment || "").trim();
  if (!trimmed) return { readOnly: true, reason: "empty segment", match: null };

  const blacklisted = violatesBlacklist(trimmed);
  if (blacklisted) {
    return {
      readOnly: false,
      reason: `dangerous pattern: ${blacklisted}`,
      match: blacklisted,
    };
  }

  const tokens = stripEnvPrefix(trimmed);
  const matched = matchesAllowlist(tokens);
  if (!matched) {
    return {
      readOnly: false,
      reason: `command "${tokens[0] || trimmed}" not in read-only allowlist`,
      match: null,
    };
  }

  return {
    readOnly: true,
    reason: `matches allowlist: ${matched.join(" ")}`,
    match: matched.join(" "),
  };
}

/**
 * Classify a full command string. Reports the first failing segment.
 * @returns {{readOnly: boolean, reason: string, segment: string|null}}
 */
export function classifyBashCommand(command) {
  const segments = splitSegments(command);
  if (!segments.length) {
    return { readOnly: true, reason: "empty command", segment: null };
  }
  for (const seg of segments) {
    const verdict = classifySegment(seg);
    if (!verdict.readOnly) {
      return { readOnly: false, reason: verdict.reason, segment: seg };
    }
  }
  return { readOnly: true, reason: "all segments read-only", segment: null };
}

/**
 * Boolean convenience wrapper used by enforce-agent-dispatch.mjs.
 */
export function isReadOnlyBash(command) {
  return classifyBashCommand(command).readOnly;
}

/**
 * Exported for tests / diagnostics only.
 */
export const __internals = {
  COMMAND_ALLOWLIST,
  DANGEROUS_PATTERNS,
  NULL_SINKS,
  splitSegments,
  stripEnvPrefix,
  inspectRedirections,
  isWritableTargetSafe,
  classifySegment,
};
