#!/usr/bin/env node
import { existsSync, mkdirSync, readFileSync, statSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { spawn, spawnSync } from "node:child_process";

const scriptPath = fileURLToPath(import.meta.url);
const rootDir = dirname(scriptPath);
const graphifyPlatforms = [
  "claude",
  "codex",
  "cursor"
];
const autoMode = process.argv.includes("--auto");
const autoWorkerMode = process.argv.includes("--auto-worker");
const stateDir = join(rootDir, ".meta-kim", "state", "default");
const autoMarkerPath = join(stateDir, "post-copy-init.json");
const runningTtlMs = 10 * 60 * 1000;
const failedRetryMs = 60 * 60 * 1000;
const guideTargets = {
  claude: "CLAUDE.md",
  codex: "AGENTS.md",
  claw: "AGENTS.md",
  opencode: "AGENTS.md",
  aider: "AGENTS.md",
  droid: "AGENTS.md",
  trae: "AGENTS.md",
  "trae-cn": "AGENTS.md",
};

function scriptMtimeMs() {
  try {
    return statSync(scriptPath).mtimeMs;
  } catch {
    return null;
  }
}

function readAutoMarker() {
  try {
    return JSON.parse(readFileSync(autoMarkerPath, "utf8"));
  } catch {
    return null;
  }
}

function writeAutoMarker(status, extra = {}) {
  try {
    mkdirSync(stateDir, { recursive: true });
    writeFileSync(
      autoMarkerPath,
      JSON.stringify(
        {
          status,
          updatedAt: new Date().toISOString(),
          scriptMtimeMs: scriptMtimeMs(),
          ...extra,
        },
        null,
        2,
      ) + "\n",
      "utf8",
    );
  } catch {
    // Auto-init state is best-effort; never let it block meta-theory startup.
  }
}

function markerAgeMs(marker) {
  const raw = marker?.updatedAt || marker?.startedAt;
  const time = raw ? Date.parse(raw) : Number.NaN;
  return Number.isFinite(time) ? Date.now() - time : Number.POSITIVE_INFINITY;
}

function shouldSkipAutoLaunch() {
  const marker = readAutoMarker();
  const currentScriptMtimeMs = scriptMtimeMs();
  if (
    marker?.status === "passed" &&
    marker.scriptMtimeMs === currentScriptMtimeMs
  ) {
    return true;
  }
  if (marker?.status === "running" && markerAgeMs(marker) < runningTtlMs) {
    return true;
  }
  if (marker?.status === "failed" && markerAgeMs(marker) < failedRetryMs) {
    return true;
  }
  return false;
}

function launchAutoWorker() {
  if (process.env.META_KIM_POST_COPY_AUTO === "off") return;
  if (shouldSkipAutoLaunch()) return;

  const startedAt = new Date().toISOString();
  writeAutoMarker("running", {
    mode: "auto",
    startedAt,
  });

  try {
    const child = spawn(process.execPath, [scriptPath, "--auto-worker"], {
      cwd: rootDir,
      detached: true,
      stdio: "ignore",
      windowsHide: true,
      env: {
        ...process.env,
        META_KIM_POST_COPY_AUTO: "worker",
      },
    });
    child.unref();
  } catch (error) {
    writeAutoMarker("failed", {
      mode: "auto",
      message: error?.message || String(error),
    });
  }
}

if (autoMode && !autoWorkerMode) {
  launchAutoWorker();
  process.exit(0);
}

function fail(message, code = 1) {
  if (autoWorkerMode) {
    writeAutoMarker("failed", {
      mode: "auto",
      message,
    });
  }
  console.error(message);
  process.exit(code);
}

function pushUniqueCandidate(candidates, seen, command, prefixArgs = []) {
  const key = command + "\0" + prefixArgs.join("\0");
  if (seen.has(key)) return;
  seen.add(key);
  candidates.push({ command, prefixArgs });
}

function homebrewPythonCandidates() {
  if (process.platform !== "darwin" && process.platform !== "linux") {
    return [];
  }

  const prefixes = [];
  const addPrefix = (value) => {
    if (typeof value !== "string") return;
    const trimmed = value.trim();
    if (trimmed && !prefixes.includes(trimmed)) {
      prefixes.push(trimmed);
    }
  };

  addPrefix(process.env.HOMEBREW_PREFIX);
  if (process.platform === "darwin") {
    addPrefix("/opt/homebrew");
    addPrefix("/usr/local");
  } else {
    addPrefix("/home/linuxbrew/.linuxbrew");
  }

  const candidates = [];
  const seen = new Set();
  const supportedMinors = Array.from({ length: 11 }, (_, index) => 20 - index);

  for (const prefix of prefixes) {
    pushUniqueCandidate(candidates, seen, join(prefix, "bin", "python3"));
    pushUniqueCandidate(candidates, seen, join(prefix, "bin", "python"));

    for (const minor of supportedMinors) {
      const version = "3." + minor;
      pushUniqueCandidate(
        candidates,
        seen,
        join(prefix, "bin", "python" + version),
      );
      pushUniqueCandidate(
        candidates,
        seen,
        join(prefix, "opt", "python@" + version, "bin", "python" + version),
      );
      pushUniqueCandidate(
        candidates,
        seen,
        join(prefix, "opt", "python@" + version, "bin", "python3"),
      );
      pushUniqueCandidate(
        candidates,
        seen,
        join(prefix, "opt", "python@" + version, "libexec", "bin", "python3"),
      );
      pushUniqueCandidate(
        candidates,
        seen,
        join(prefix, "opt", "python@" + version, "libexec", "bin", "python"),
      );
    }
  }

  return candidates;
}

function pythonCandidates() {
  if (process.platform === "win32") {
    return [
      { command: "py", prefixArgs: ["-3"] },
      { command: "python", prefixArgs: [] },
      { command: "python3", prefixArgs: [] },
    ];
  }
  return [
    { command: "python3", prefixArgs: [] },
    { command: "python", prefixArgs: [] },
    ...homebrewPythonCandidates(),
  ];
}

function runCandidate(candidate, args, stdio = "inherit") {
  return spawnSync(candidate.command, [...candidate.prefixArgs, ...args], {
    cwd: rootDir,
    encoding: "utf8",
    stdio,
  });
}

function findPython() {
  for (const candidate of pythonCandidates()) {
    const result = runCandidate(
      candidate,
      [
        "-c",
        "import sys; raise SystemExit(0 if sys.version_info >= (3, 10) else 1)",
      ],
      "pipe",
    );
    if (result.status === 0) return candidate;
  }
  return null;
}

function runPython(python, args, { optional = false, stdio = "inherit" } = {}) {
  const result = runCandidate(python, args, stdio);
  if (result.status === 0) return true;
  if (optional) {
    console.warn("[Meta_Kim] Optional command failed:", args.join(" "));
    return false;
  }
  fail("[Meta_Kim] Command failed: " + args.join(" "), result.status || 1);
}

function guideAlreadyHasGraphifySection(platform) {
  const target = guideTargets[platform];
  if (!target) return false;
  const filePath = join(rootDir, target);
  if (!existsSync(filePath)) return false;
  return /^##\s+graphify\b/im.test(readFileSync(filePath, "utf8"));
}

const python = findPython();
if (!python) {
  fail(
    "[Meta_Kim] Python 3.10+ with pip is required for graphify. Install Python, then run this script again.",
  );
}

const pipShow = runCandidate(python, ["-m", "pip", "show", "graphifyy"], "pipe");
if (pipShow.status !== 0) {
  runPython(python, ["-m", "pip", "install", "graphifyy"]);
}
runPython(python, ["-m", "pip", "install", "--upgrade", "networkx>=3.4"], {
  optional: true,
});

if (existsSync(join(rootDir, ".git"))) {
  runPython(python, ["-m", "graphify", "hook", "install"]);
} else {
  console.log("[Meta_Kim] Skipping graphify git hook; no .git directory found.");
}

for (const platform of graphifyPlatforms) {
  if (guideAlreadyHasGraphifySection(platform)) {
    console.log(`[Meta_Kim] graphify ${platform} guide section already exists; skipping install.`);
    continue;
  }
  runPython(python, ["-m", "graphify", platform, "install"], {
    optional: true,
  });
}

runPython(python, ["-m", "graphify", "update", "."]);
writeAutoMarker("passed", {
  mode: autoWorkerMode ? "auto" : "manual",
  graphPath: join(rootDir, "graphify-out", "graph.json"),
});
console.log("[Meta_Kim] graphify is initialized for:", rootDir);
