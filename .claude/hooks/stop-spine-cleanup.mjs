import process from "node:process";
import { unlink } from "node:fs/promises";
import { join } from "node:path";
import { readJsonFromStdin } from "./utils.mjs";
import { readSpineState } from "./spine-state.mjs";

// Stop hook: deactivate spine state on session end.
// Reads spine state, marks inactive, or deletes if evolution completed.

await readJsonFromStdin();

const cwd = process.cwd();
const SPINE_STATE_DIR =
  process.env.META_KIM_SPINE_STATE_DIR || ".meta-kim/state/default/spine";

try {
  const state = await readSpineState(cwd);
  if (!state || !state.active) {
    process.exit(0);
  }

  // If evolution completed, clean up the file
  if (state.stages?.evolution?.status === "completed") {
    const filePath = join(cwd, SPINE_STATE_DIR, "spine-state.json");
    try {
      await unlink(filePath);
    } catch {
      // File may already be gone, that's fine
    }
    process.stderr.write(
      "[spine-cleanup] evolution completed, spine state removed\n",
    );
  } else {
    // Mark inactive but keep for potential session recovery
    state.active = false;
    state.deactivatedAt = new Date().toISOString();
    state.deactivationReason = "session_stop";
    const { writeSpineState } = await import("./spine-state.mjs");
    await writeSpineState(cwd, state);
    process.stderr.write(
      `[spine-cleanup] spine deactivated at stage=${state.currentStage}, agents dispatched=${state.dispatchedAgents.length}\n`,
    );
  }
} catch {
  // Non-critical: never block session stop
}

process.exit(0);
