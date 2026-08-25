#!/usr/bin/env node
/**
 * Shared utilities for Meta_Kim hooks.
 * DRY centralization — all hooks import from here instead of duplicating readJsonFromStdin.
 * Synced from canonical/runtime-assets/claude/hooks/utils.mjs to .claude/hooks/utils.mjs
 */

/**
 * Read and parse JSON from stdin.
 * Returns empty object on empty input or parse failure.
 */
export async function readJsonFromStdin() {
  let raw = "";
  for await (const chunk of process.stdin) {
    raw += chunk;
  }
  try {
    return raw.trim() ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

/**
 * Read and parse JSON from stdin, synchronously.
 * Returns empty object on empty input or parse failure.
 */
export function readJsonFromStdinSync() {
  let raw = "";
  for (const chunk of process.stdin) {
    raw += chunk;
  }
  try {
    return raw.trim() ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

/**
 * Extract the file path from a Claude Code hook input object.
 * Handles various field names across different hook types.
 */
export function extractFilePath(input) {
  return (
    input.file_path ||
    input.path ||
    input.tool_input?.file_path ||
    input.tool_input?.path ||
    input.tool_response?.filePath ||
    input.tool_response?.file_path ||
    ""
  );
}
