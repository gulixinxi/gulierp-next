import { readFile, writeFile, mkdir } from "node:fs/promises";
import { join, dirname, resolve, relative, isAbsolute } from "node:path";

const META_KIM_STATE_ROOT = ".meta-kim/state";
const DEFAULT_SPINE_STATE_DIR = ".meta-kim/state/default/spine";
const SPINE_STATE_FILE = "spine-state.json";
const ACTIVE_RUN_STATUS_FILE = "active-run.json";
const RUN_STATUS_FILE = "status.json";

export const STAGE_ORDER = [
  "critical",
  "fetch",
  "thinking",
  "execution",
  "review",
  "meta_review",
  "verification",
  "evolution",
];

export const STAGE_PUBLIC_LABELS = {
  critical: "Critical",
  fetch: "Fetch",
  thinking: "Thinking",
  execution: "Execution",
  review: "Review",
  meta_review: "Meta-Review",
  verification: "Verification",
  evolution: "Evolution",
};

export const CHOICE_SURFACE_STATES = [
  "not_allowed",
  "critical_clarification_allowed",
  "execution_confirmation_allowed",
  "completed",
];

const STAGE_PROGRESS_PERCENT = {
  critical: 12,
  fetch: 25,
  thinking: 38,
  execution: 50,
  review: 63,
  meta_review: 75,
  verification: 88,
  evolution: 100,
};

/**
 * Default read-only verifier whitelist (EB-002, v2.3.1).
 *
 * Stages that enable `readOnlyVerifierEnabled: true` allow Bash commands that
 * start with any of these prefixes to bypass the stage-requirements check.
 * The intent is to let verifier owners (meta-prism, meta-warden) inspect
 * git history, run validation scripts, and check artifacts without first
 * dispatching a downstream worker.
 *
 * NOTE: these are PREFIX matches (`cmd.startsWith(prefix)`). They are not
 * regexes and do not enforce command structure. The whitelist is intentionally
 * narrow — anything not listed falls through to the existing gates.
 */
const DEFAULT_READ_ONLY_VERIFIER_COMMANDS = [
  "git diff",
  "git diff --stat",
  "git log",
  "git show",
  "git status",
  "git rev-parse",
  "git tag --list",
  "npm run meta:check",
  "npm run meta:check:runtimes",
  "npm run meta:check:sync-coverage",
  "npm run meta:validate",
  "node --check",
  "node -e",
  "node --test",
  "gh release view",
  "gh pr view",
  "ls",
  "cat",
  "head",
  "tail",
  "wc",
];

// Fetch may gather evidence with targeted, read-only baseline verification
// commands. These commands are narrower than general execution and exist to
// let the operator inspect current health before route selection.
const DEFAULT_FETCH_READ_ONLY_VERIFIER_COMMANDS = [
  "node --test",
  "node scripts/run-node-tests.mjs",
  "npm test",
  "npm run ",
  "pnpm test",
  "pnpm run ",
  "yarn test",
  "yarn ",
];

const DEFAULT_READ_ONLY_INSPECTION_COMMANDS = [
  "git status",
  "git diff --stat",
  "git diff --check",
  "git diff -- ",
  "git rev-parse",
  "rg ",
  "rg --files",
  "ls",
  "Get-ChildItem",
  "Get-Content",
  "Select-String",
];

export const STAGE_META_AGENT_MAP = {
  critical: {
    required: [],
    label: "Critical (scope clarification)",
    readOnlyInspectionEnabled: true,
    readOnlyInspectionCommands: DEFAULT_READ_ONLY_INSPECTION_COMMANDS,
  },
  fetch: {
    required: [],
    label: "Fetch (capability discovery)",
    requiresFetchRecord: true,
    readOnlyInspectionEnabled: true,
    readOnlyInspectionCommands: DEFAULT_READ_ONLY_INSPECTION_COMMANDS,
    readOnlyVerifierEnabled: true,
    readOnlyVerifierCommands: DEFAULT_FETCH_READ_ONLY_VERIFIER_COMMANDS,
  },
  thinking: {
    required: [],
    label: "Thinking (route and loadout selection)",
    requiresFetchRecord: true,
  },
  execution: { required: [], label: "Execution", requiresAgentDispatch: true },
  review: {
    required: [],
    label: "Review (quality forensics)",
    readOnlyVerifierEnabled: true,
    readOnlyVerifierCommands: DEFAULT_READ_ONLY_VERIFIER_COMMANDS,
  },
  meta_review: {
    required: [],
    label: "Meta-Review (standards check)",
    readOnlyVerifierEnabled: true,
    readOnlyVerifierCommands: DEFAULT_READ_ONLY_VERIFIER_COMMANDS,
  },
  verification: {
    required: [],
    label: "Verification (closure)",
    readOnlyVerifierEnabled: true,
    readOnlyVerifierCommands: DEFAULT_READ_ONLY_VERIFIER_COMMANDS,
  },
  evolution: { required: [], label: "Evolution (writeback)" },
};

const META_AGENT_NAMES = [
  "meta-warden",
  "meta-conductor",
  "meta-genesis",
  "meta-artisan",
  "meta-sentinel",
  "meta-librarian",
  "meta-prism",
  "meta-scout",
];

function createRunId(timestamp = new Date().toISOString()) {
  return `meta-${timestamp.replace(/[:.]/g, "-")}`;
}

function isWithin(parent, target) {
  const rel = relative(parent, target);
  return rel === "" || (!rel.startsWith("..") && !isAbsolute(rel));
}

export function sanitizeStateProfile(input) {
  const value =
    typeof input === "string" && input.trim() ? input.trim() : "default";
  if (value === "." || value === ".." || value.length > 80) return "default";
  if (!/^[A-Za-z0-9._-]+$/.test(value)) return "default";
  return value;
}

export function resolveMetaKimStateRoot(cwd) {
  return resolve(cwd || process.cwd(), META_KIM_STATE_ROOT);
}

export function resolveRepoLocalStateDir(cwd, requestedPath, fallbackPath) {
  const repoRoot = resolve(cwd || process.cwd());
  const stateRoot = resolveMetaKimStateRoot(repoRoot);
  const fallback = resolve(repoRoot, fallbackPath || DEFAULT_SPINE_STATE_DIR);
  const raw =
    typeof requestedPath === "string" && requestedPath.trim()
      ? requestedPath.trim()
      : "";

  const candidate = raw
    ? resolve(isAbsolute(raw) ? raw : join(repoRoot, raw))
    : fallback;

  if (isWithin(stateRoot, candidate)) return candidate;
  return fallback;
}

export function resolveProfileStateDir(cwd, profile, ...segments) {
  const safeProfile = sanitizeStateProfile(profile);
  const stateRoot = resolveMetaKimStateRoot(cwd);
  const candidate = resolve(stateRoot, safeProfile, ...segments);
  if (!isWithin(stateRoot, candidate)) {
    return resolve(stateRoot, "default", ...segments);
  }
  return candidate;
}

export function extractMetaAgentName(description, prompt) {
  const text = `${description || ""} ${prompt || ""}`.toLowerCase();
  for (const name of META_AGENT_NAMES) {
    if (text.includes(name)) return name;
  }
  return null;
}

function spineStatePath(cwd) {
  return join(
    resolveRepoLocalStateDir(
      cwd,
      process.env.META_KIM_SPINE_STATE_DIR,
      DEFAULT_SPINE_STATE_DIR,
    ),
    SPINE_STATE_FILE,
  );
}

function ensureDir(filePath) {
  return mkdir(dirname(filePath), { recursive: true });
}

function normalizeStage(stageName) {
  if (typeof stageName !== "string") return "critical";
  const normalized = stageName.trim().toLowerCase().replace(/-/g, "_");
  return STAGE_ORDER.includes(normalized) ? normalized : "critical";
}

function profileFromState(state) {
  return sanitizeStateProfile(
    state?.profile || state?.stateProfile || process.env.META_KIM_STATE_PROFILE,
  );
}

function cleanLanguageTag(input) {
  return typeof input === "string" && input.trim() ? input.trim() : null;
}

function resolveOutputLanguage(state, options = {}) {
  const candidates = [
    ["tool_selected", options.toolSelectedLanguage || state?.toolSelectedLanguage],
    ["explicit_output_choice", options.outputLanguage || state?.outputLanguage],
    ["intent_gate", state?.intentGatePacket?.userLanguage],
    ["card_decision", state?.cardDecision?.userLanguage],
    ["delivery_shell", state?.deliveryShell?.userLanguage],
    ["latest_user_input", state?.latestUserInputLanguage],
    ["environment", process.env.META_KIM_OUTPUT_LANGUAGE || process.env.LANG],
  ];

  for (const [source, value] of candidates) {
    const language = cleanLanguageTag(value);
    if (language) return { language, source };
  }

  return { language: "undetermined", source: "not_resolved" };
}

function runStatusPaths(cwd, profile, runId) {
  const profileDir = resolveProfileStateDir(cwd, profile);
  return {
    activeRun: join(profileDir, ACTIVE_RUN_STATUS_FILE),
    runStatus: join(profileDir, "runs", runId, RUN_STATUS_FILE),
  };
}

export async function readSpineState(cwd) {
  const filePath = spineStatePath(cwd);
  try {
    const raw = await readFile(filePath, "utf-8");
    const status = JSON.parse(raw);
    return status?.active === false ? null : status;
  } catch {
    return null;
  }
}

export async function writeSpineState(cwd, state) {
  const filePath = spineStatePath(cwd);
  await ensureDir(filePath);
  await writeFile(filePath, JSON.stringify(state, null, 2), "utf-8");
  await writeMetaRunStatus(cwd, state);
}

export function createInitialState({ taskClassification, triggerReason }) {
  const triggeredAt = new Date().toISOString();
  return {
    active: true,
    version: 2,
    runId: createRunId(triggeredAt),
    triggeredAt,
    currentStage: "critical",
    stages: {
      critical: { status: "in_progress", completedAt: null },
      fetch: { status: "pending", completedAt: null },
      thinking: { status: "pending", completedAt: null },
      execution: { status: "pending", completedAt: null },
      review: { status: "pending", completedAt: null },
      meta_review: { status: "pending", completedAt: null },
      verification: { status: "pending", completedAt: null },
      evolution: { status: "pending", completedAt: null },
    },
    taskClassification: taskClassification || null,
    triggerReason: triggerReason || "user_invocation",
    dispatchedAgents: [],
    dispatchChain: {},
    controlState: "normal",
    gateState: "pending",
    surfaceState: "silent",
    choiceSurfaceState: "not_allowed",
    queryBypass: false,
    executionStarted: false,
    criticalFetchLoopCount: 0,
    criticalFetchLoopMax: 3,
    intentCard: null,
    intentConfirmationState: null,
    intentConfirmationTimestamp: null,
    intentCorrectionPayload: null,
    // Audit trail for skipped hooks
    skippedHooks: [],
  };
}

export function createMetaRunStatusEnvelope(state, options = {}) {
  const currentStage = normalizeStage(
    options.currentStage || state?.currentStage || "critical",
  );
  const stageIndex = STAGE_ORDER.indexOf(currentStage) + 1;
  const stageTotal = STAGE_ORDER.length;
  const stages = state?.stages || {};
  const completed = STAGE_ORDER.filter(
    (stage) => stages?.[stage]?.status === "completed",
  ).map((stage) => STAGE_PUBLIC_LABELS[stage]);
  const nextStage =
    stageIndex < stageTotal ? STAGE_ORDER[stageIndex] : null;
  const startedAt =
    state?.triggeredAt || state?.startedAt || new Date().toISOString();
  const updatedAt = new Date().toISOString();
  const runId = state?.runId || createRunId(startedAt);
  const languageResolution = resolveOutputLanguage(state, options);
  const stagePurpose =
    state?.stagePurpose ||
    state?.stagePurposes?.[languageResolution.language] ||
    null;

  return {
    schemaVersion: 1,
    active: state?.active !== false,
    runId,
    triggeredBy:
      state?.triggerReason || state?.triggeredBy || "meta-theory",
    currentStage: STAGE_PUBLIC_LABELS[currentStage],
    currentStageKey: currentStage,
    stageIndex,
    stageTotal,
    percent: STAGE_PROGRESS_PERCENT[currentStage],
    completed,
    next: nextStage ? STAGE_PUBLIC_LABELS[nextStage] : null,
    blockedOn: state?.blockedOn || null,
    startedAt,
    updatedAt,
    lastUserVisibleNotice: state?.lastUserVisibleNotice || null,
    surfaceMode: "public",
    resolvedOutputLanguage: languageResolution.language,
    languageResolution,
    publicSurface: {
      primaryDisplay: "conversation_notice",
      nativeEnhancementAllowed: true,
      popupRequired: false,
      hiddenInternalFields: [
        "Preflight",
        "nativeChoiceSurface",
        "conversation_fallback",
        "packet_id",
        "protocol_trace",
      ],
    },
    publicLabels: state?.publicLabels || null,
    stagePurpose,
    stagePurposeKey: currentStage,
  };
}

export async function writeMetaRunStatus(cwd, state, options = {}) {
  if (!state || typeof state !== "object") return null;
  const envelope = createMetaRunStatusEnvelope(state, options);
  const profile = profileFromState(state);
  const paths = runStatusPaths(cwd, profile, envelope.runId);
  await ensureDir(paths.activeRun);
  await ensureDir(paths.runStatus);
  const serialized = JSON.stringify(envelope, null, 2);
  await Promise.all([
    writeFile(paths.activeRun, serialized, "utf-8"),
    writeFile(paths.runStatus, serialized, "utf-8"),
  ]);
  return envelope;
}

export async function readMetaRunStatus(cwd, profile) {
  const filePath = runStatusPaths(cwd, sanitizeStateProfile(profile), "latest")
    .activeRun;
  try {
    const raw = await readFile(filePath, "utf-8");
    const status = JSON.parse(raw);
    return status?.active === false ? null : status;
  } catch {
    return null;
  }
}

export function advanceStage(state, stageName) {
  const stageOrder = STAGE_ORDER;

  const idx = stageOrder.indexOf(stageName);
  if (idx === -1) return state;

  const newState = { ...state };

  for (let i = 0; i < idx; i++) {
    const prev = stageOrder[i];
    if (newState.stages[prev].status !== "completed") {
      newState.stages[prev] = {
        status: "completed",
        completedAt: new Date().toISOString(),
        autoCompleted: true,
        reason: `Advanced past by stage ${stageName}`,
      };
    }
  }

  newState.stages[stageName] = {
    status: "in_progress",
    completedAt: null,
    startedAt: new Date().toISOString(),
  };
  newState.currentStage = stageName;

  if (stageName === "execution") {
    newState.executionStarted = true;
  }

  return newState;
}

export function completeStage(state, stageName) {
  if (!state.stages[stageName]) return state;
  const newState = { ...state };
  newState.stages[stageName] = {
    status: "completed",
    completedAt: new Date().toISOString(),
  };

  const stageOrder = STAGE_ORDER;
  const idx = stageOrder.indexOf(stageName);
  if (idx < stageOrder.length - 1) {
    const nextStage = stageOrder[idx + 1];
    newState.currentStage = nextStage;
    newState.stages[nextStage] = {
      status: "in_progress",
      startedAt: new Date().toISOString(),
    };
  }

  return newState;
}

export function incrementCriticalFetchLoop(state) {
  const count = (state.criticalFetchLoopCount || 0) + 1;
  const max = state.criticalFetchLoopMax || 3;
  return {
    ...state,
    criticalFetchLoopCount: count,
    criticalFetchLoopBudgetExhausted: count >= max,
  };
}

export function recordIntentConfirmation(state, confirmationState, correctionPayload) {
  return {
    ...state,
    intentConfirmationState: confirmationState,
    intentConfirmationTimestamp: new Date().toISOString(),
    intentCorrectionPayload: correctionPayload || null,
  };
}

/**
 * Record a dispatch into spine state.
 *
 * Behavior matrix (HOOK-INFRA-001, v2.3.1):
 *   - When `metaName` is a known meta-agent name → append to
 *     `dispatchChain[currentStage]` (legacy/existing behavior).
 *   - When `metaName` is null but `toolInput.subagent_type` matches a
 *     `workerTaskPackets[]` entry by `taskPacketId` / `roleInstanceId`:
 *       * If the matched packet's `ownerAgent` is a meta-agent → append to
 *         `dispatchChain[currentStage]`.
 *       * Otherwise → append the worker identifier to
 *         `dispatchChain[currentStage]_supplementary`.
 *   - When no match either way → append the raw dispatch identifier into
 *     `dispatchChain[currentStage]_supplementary` so the chain stays
 *     auditable.
 *
 * Field shape: `dispatchChain` retains its existing
 * `{ stage: string[] }` shape; the supplementary entries use a parallel
 * key `${stage}_supplementary` to avoid mixing meta-owners with worker IDs
 * in the same array. Both fields are append-only and dedup-safe.
 *
 * @param {object} state - Current spine state.
 * @param {string} agentName - Human description / agent identifier.
 * @param {string|null} metaName - Resolved meta-agent name, if any.
 * @param {object} [toolInput] - The raw tool input (Agent dispatch payload).
 * @returns {object} New state with dispatch recorded.
 */
export function recordDispatch(state, agentName, metaName, toolInput) {
  const META_AGENT_NAME_SET = new Set(META_AGENT_NAMES);
  const newState = { ...state };
  if (!newState.dispatchedAgents.includes(agentName)) {
    newState.dispatchedAgents = [...newState.dispatchedAgents, agentName];
  }

  const chain = { ...newState.dispatchChain };
  const stage = newState.currentStage;
  const supplementaryKey = `${stage}_supplementary`;

  const appendToChain = (value) => {
    if (!value) return;
    if (!chain[stage]) chain[stage] = [];
    if (!chain[stage].includes(value)) {
      chain[stage] = [...chain[stage], value];
    }
  };

  const appendToSupplementary = (value) => {
    if (!value) return;
    if (!chain[supplementaryKey]) chain[supplementaryKey] = [];
    if (!chain[supplementaryKey].includes(value)) {
      chain[supplementaryKey] = [...chain[supplementaryKey], value];
    }
  };

  if (metaName && META_AGENT_NAME_SET.has(metaName)) {
    appendToChain(metaName);
  } else {
    const subagentType =
      (toolInput &&
        (toolInput.subagent_type || toolInput.agent_type || toolInput.type)) ||
      null;
    const dispatchText = toolInput
      ? [
          toolInput.description,
          toolInput.prompt,
          toolInput.message,
          toolInput.agent_type,
          toolInput.subagent_type,
          toolInput.type,
        ]
          .filter(Boolean)
          .join(" ")
      : "";
    const workerPackets = Array.isArray(newState.workerTaskPackets)
      ? newState.workerTaskPackets
      : [];

    const matchedPacket = workerPackets.find((packet) => {
      if (!packet || typeof packet !== "object") return false;
      if (
        subagentType &&
        (packet.businessRoleId === subagentType ||
          packet.roleDisplayName === subagentType ||
          packet.roleInstanceId === subagentType ||
          packet.taskPacketId === subagentType)
      ) {
        return true;
      }
      if (
        dispatchText &&
        ((packet.taskPacketId && dispatchText.includes(packet.taskPacketId)) ||
          (packet.roleInstanceId &&
            dispatchText.includes(packet.roleInstanceId)))
      ) {
        return true;
      }
      return false;
    });

    if (matchedPacket) {
      if (
        matchedPacket.ownerAgent &&
        META_AGENT_NAME_SET.has(matchedPacket.ownerAgent)
      ) {
        appendToChain(matchedPacket.ownerAgent);
      } else {
        appendToSupplementary(
          matchedPacket.roleInstanceId ||
            matchedPacket.taskPacketId ||
            matchedPacket.businessRoleId ||
            matchedPacket.roleDisplayName ||
            matchedPacket.ownerAgent ||
            agentName,
        );
      }
    } else {
      appendToSupplementary(subagentType || agentName);
    }
  }

  newState.dispatchChain = chain;
  return newState;
}

function isNonEmptyString(value) {
  return typeof value === "string" && value.trim().length > 0;
}

function isObject(value) {
  return !!value && typeof value === "object" && !Array.isArray(value);
}

function hasNonEmptyArray(value) {
  return Array.isArray(value) && value.length > 0;
}

function nonEmptyObject(value) {
  return isObject(value) && Object.keys(value).length > 0;
}

function firstString(...values) {
  return values.find((value) => isNonEmptyString(value));
}

function hasSignalValue(value) {
  return isNonEmptyString(value) || hasNonEmptyArray(value);
}

function hasAnySignal(object, keys) {
  if (!isObject(object)) return false;
  return keys.some((key) => hasSignalValue(object[key]));
}

function hasIntentSignal(state) {
  const intentKeys = ["realIntent", "intent", "outcome", "coreProblem"];
  const successKeys = ["successCriteria", "acceptanceCriteria", "qualityBar"];
  const records = [
    state,
    state?.intentPacket,
    state?.intentGatePacket,
    state?.criticalRecord,
  ];
  return records.some(
    (record) =>
      hasAnySignal(record, intentKeys) && hasAnySignal(record, successKeys),
  );
}

function hasThinkingRouteSignal(state) {
  if (nonEmptyObject(state?.dispatchBoard)) return true;
  if (hasNonEmptyArray(state?.workerTaskPackets)) return true;
  if (hasNonEmptyArray(state?.agentBlueprintPacket?.roles)) return true;
  const flow = state?.businessFlowBlueprintPacket;
  if (
    hasNonEmptyArray(flow?.requiredLanes) ||
    hasNonEmptyArray(flow?.optionalLanes)
  ) {
    return true;
  }
  if (nonEmptyObject(state?.ownerDiscoveryPacket)) return true;
  if (nonEmptyObject(state?.routeScoreBreakdown)) return true;
  return false;
}

function hasMemorySignal(state) {
  return !!firstString(
    state?.memoryMode,
    state?.memoryPolicy,
    state?.memoryStrategy,
    state?.memoryPlan,
    state?.dispatchEnvelopePacket?.memoryMode,
    state?.dispatchEnvelopePacket?.memoryPolicy,
    state?.dispatchEnvelopePacket?.memoryStrategy,
    state?.fetchRecord?.memoryMode,
    state?.fetchRecord?.memoryStrategy,
    state?.ownerDiscoveryPacket?.memoryMode,
    state?.ownerDiscoveryPacket?.memoryStrategy,
  );
}

function hasReviewStandardSignal(state) {
  if (
    firstString(
      state?.reviewStandard,
      state?.reviewPlan,
      state?.qualityBar,
      state?.dispatchEnvelopePacket?.reviewStandard,
      state?.dispatchEnvelopePacket?.reviewOwner,
      state?.dispatchBoard?.reviewStandard,
      state?.dispatchBoard?.reviewerAgent,
    )
  ) {
    return true;
  }

  if (hasNonEmptyArray(state?.workerTaskPackets)) {
    return state.workerTaskPackets.some((packet) =>
      firstString(
        packet?.reviewStandard,
        packet?.qualityBar,
        packet?.finalizationGate,
        packet?.handoffTarget,
        packet?.handoffContract?.handoffTo,
      ),
    );
  }

  return false;
}

function supportStatus(value) {
  return typeof value === "string" ? value.trim().toLowerCase() : "";
}

function containsUnsupportedStatus(value) {
  if (typeof value === "string") {
    return /^(known[-_ ]unsupported|unsupported|not_supported|blocked|deny|denied)$/.test(
      supportStatus(value),
    );
  }
  if (Array.isArray(value)) return value.some(containsUnsupportedStatus);
  if (isObject(value)) {
    if (containsUnsupportedStatus(value.status)) return true;
    return Object.values(value).some(containsUnsupportedStatus);
  }
  return false;
}

function hasKnownUnsupportedRuntimeOrOs(state) {
  return [
    state?.runtimeSupport,
    state?.osSupport,
    state?.runtimeSupportStatus,
    state?.osSupportStatus,
    state?.runtimeCompatibility,
    state?.osCompatibility,
    state?.runtimeMatrix,
    state?.osMatrix,
    state?.dispatchEnvelopePacket?.runtimeSupport,
    state?.dispatchEnvelopePacket?.osSupport,
    state?.dispatchBoard?.runtimeSupport,
    state?.dispatchBoard?.osSupport,
    state?.ownerDiscoveryPacket?.runtimeSupport,
    state?.ownerDiscoveryPacket?.osSupport,
  ].some(containsUnsupportedStatus);
}

function hasCapabilityProviderEvidence(state) {
  const fetchRecord = state?.fetchRecord;
  if (
    hasNonEmptyArray(fetchRecord?.capabilityMatches) ||
    hasNonEmptyArray(fetchRecord?.matchedCapabilities) ||
    hasNonEmptyArray(fetchRecord?.providerMatches) ||
    hasNonEmptyArray(fetchRecord?.capabilityProviders)
  ) {
    return true;
  }

  const ownerDiscovery = state?.ownerDiscoveryPacket;
  const providerFields = [
    ownerDiscovery?.candidateReusableCapabilityProviders,
    ownerDiscovery?.reusableCapabilityProviders,
    ownerDiscovery?.capabilityProviders,
    ownerDiscovery?.candidateSkills,
    ownerDiscovery?.candidateCommands,
    ownerDiscovery?.candidateMcpTools,
    ownerDiscovery?.candidateTools,
    ownerDiscovery?.candidatePrompts,
  ];
  if (providerFields.some(hasNonEmptyArray)) return true;

  const laneGroups = [
    state?.businessFlowBlueprintPacket?.requiredLanes,
    state?.businessFlowBlueprintPacket?.optionalLanes,
  ].filter(Array.isArray);
  return laneGroups.some((lanes) =>
    lanes.some((lane) =>
      hasNonEmptyArray(lane?.candidateSkills) ||
      hasNonEmptyArray(lane?.candidateCapabilities) ||
      hasNonEmptyArray(lane?.candidateCommands) ||
      hasNonEmptyArray(lane?.candidateMcpTools) ||
      hasNonEmptyArray(lane?.candidateTools) ||
      hasNonEmptyArray(lane?.candidatePrompts) ||
      hasNonEmptyArray(lane?.candidateOwners),
    ),
  );
}

function hasExecutionOwnerEvidence(state) {
  if (firstString(state?.dispatchBoard?.ownerAgent, state?.dispatchBoard?.owner)) {
    return true;
  }
  if (hasNonEmptyArray(state?.workerTaskPackets)) {
    return state.workerTaskPackets.some((packet) =>
      firstString(packet?.ownerAgent, packet?.owner, packet?.roleDisplayName),
    );
  }
  if (hasNonEmptyArray(state?.agentBlueprintPacket?.roles)) {
    return state.agentBlueprintPacket.roles.some((role) =>
      firstString(role?.ownerAgent, role?.owner, role?.roleDisplayName),
    );
  }
  return !!firstString(
    state?.ownerDiscoveryPacket?.selectedOwner,
    state?.ownerDiscoveryPacket?.ownerAgent,
    state?.selectedOwner,
    state?.ownerAgent,
  );
}

function hasLoadoutEvidence(state) {
  if (hasNonEmptyArray(state?.capabilityBindings)) return true;
  if (
    firstString(
      state?.dispatchBoard?.weapon,
      state?.dispatchBoard?.selectedWeapon,
      state?.selectedWeapon,
      state?.abstractPrompt,
      state?.promptRef,
      state?.dispatchEnvelopePacket?.abstractPrompt,
      state?.dispatchEnvelopePacket?.promptRef,
    )
  ) {
    return true;
  }

  if (hasNonEmptyArray(state?.workerTaskPackets)) {
    return state.workerTaskPackets.some((packet) =>
      hasNonEmptyArray(packet?.capabilityRequirements) ||
      hasNonEmptyArray(packet?.toolRequirements) ||
      hasNonEmptyArray(packet?.skillRequirements) ||
      hasNonEmptyArray(packet?.commandRequirements) ||
      hasNonEmptyArray(packet?.mcpRequirements) ||
      firstString(packet?.abstractPrompt, packet?.promptRef, packet?.weapon),
    );
  }

  if (hasNonEmptyArray(state?.agentBlueprintPacket?.roles)) {
    return state.agentBlueprintPacket.roles.some((role) =>
      hasNonEmptyArray(role?.matchedSkills) ||
      hasNonEmptyArray(role?.matchedCapabilities) ||
      hasNonEmptyArray(role?.capabilityBindings) ||
      firstString(role?.abstractPrompt, role?.promptRef, role?.weapon),
    );
  }

  return false;
}

const WORKER_WORK_ORDER_STRING_FIELDS = [
  "coreProblem",
  "todayTask",
  "output",
  "deliverableLink",
  "workType",
  "qualityBar",
  "referenceDirection",
  "lengthExpectation",
  "visualOrAssetPlan",
  "preDecisionOptionFrameRef",
  "finalizationGate",
];

const WORKER_WORK_ORDER_ARRAY_FIELDS = [
  "scopeFiles",
  "nonGoals",
  "acceptanceCriteria",
  "expertLensRefs",
  "evidenceRefs",
  "capabilityRequirements",
  "toolRequirements",
  "verifySteps",
];

const WORKER_HANDOFF_CONTRACT_FIELDS = [
  "handoffTo",
  "handoffWhen",
  "handoffPayload",
  "acceptanceSignal",
];

const PRE_EXECUTION_PACKET_STATUS_FIELDS = {
  productCompletenessPacket: "completenessStatus",
  experienceQualityPacket: "experienceStatus",
  testStrategyPacket: "testStatus",
  structureHygienePacket: "hygieneStatus",
  permissionMatrixPacket: "permissionStatus",
  sideEffectLedgerPacket: "sideEffectStatus",
  rollbackPlanPacket: "rollbackStatus",
};

const PRE_EXECUTION_ALLOWED_STATUSES = {
  productCompletenessPacket: ["pass"],
  experienceQualityPacket: ["pass", "not_applicable_with_reason"],
  testStrategyPacket: ["pass"],
  structureHygienePacket: ["pass"],
  permissionMatrixPacket: ["pass"],
  sideEffectLedgerPacket: ["none", "tracked"],
  rollbackPlanPacket: ["ready", "not_applicable_with_reason"],
};

const REQUIRED_PRE_EXECUTION_PACKETS = [
  "dispatchEnvelopePacket",
  "dispatchBoard",
  "orchestrationTaskBoardPacket",
  ...Object.keys(PRE_EXECUTION_PACKET_STATUS_FIELDS),
];

function collectPreExecutionReadinessGaps(state) {
  const missing = [];

  if (!hasIntentSignal(state)) {
    missing.push("intent signal (intentPacket or realIntent + successCriteria)");
  }
  if (!hasFetchEvidence(state)) {
    missing.push("Fetch evidence");
  }
  if (!hasThinkingRouteSignal(state)) {
    missing.push("Thinking route plan");
  }
  if (!hasMemorySignal(state)) {
    missing.push("memory strategy");
  }
  if (!hasReviewStandardSignal(state)) {
    missing.push("Review standard");
  }

  return missing;
}

export function checkPreExecutionReadiness(state) {
  if (!state || state.queryBypass) {
    return {
      met: true,
      missing: [],
      reason: "pre-execution readiness gate bypassed",
    };
  }

  const missing = collectPreExecutionReadinessGaps(state);
  return {
    met: missing.length === 0,
    missing,
    reason:
      missing.length === 0
        ? "minimum key-behavior pre-execution evidence is present"
        : "Pre-execution readiness requires the key behavior evidence only: intent, Fetch evidence, Thinking route/loadout, and memory strategy. Optional packet fields belong to validators, not hook blocking.",
  };
}

function collectCapabilityNodeBindingGaps(state) {
  const missing = [];

  const fetchRecord = state?.fetchRecord;
  if (!isObject(fetchRecord)) {
    missing.push("fetchRecord");
  } else {
    if (fetchRecord.capabilitySearchPerformed !== true) {
      missing.push("fetchRecord.capabilitySearchPerformed=true");
    }
  }

  if (!hasCapabilityProviderEvidence(state)) {
    missing.push("capability provider evidence (agent, skill, command, MCP, tool, or prompt)");
  }
  if (!hasExecutionOwnerEvidence(state)) {
    missing.push("execution owner");
  }
  if (!hasLoadoutEvidence(state)) {
    missing.push("owner loadout (skill, command, MCP, tool, or abstract prompt)");
  }
  if (!hasMemorySignal(state)) {
    missing.push("memory strategy");
  }
  if (!hasReviewStandardSignal(state)) {
    missing.push("Review standard");
  }
  if (hasKnownUnsupportedRuntimeOrOs(state)) {
    missing.push("runtime/OS support not known-unsupported");
  }

  return missing;
}

export function checkCapabilityNodeBindings(state) {
  if (!state || state.queryBypass) {
    return { met: true, missing: [], reason: "capability node binding gate bypassed" };
  }

  const missing = collectCapabilityNodeBindingGaps(state);
  return {
    met: missing.length === 0,
    missing,
    reason:
      missing.length === 0
        ? "minimum capability owner/loadout bindings present"
        : "Execution requires key capability evidence only: capability search, selected owner, usable loadout across skill/command/MCP/tool/prompt, and memory strategy. Exhaustive per-field work-order validation belongs to validators.",
  };
}

export function checkStageRequirements(state) {
  const stage = state.currentStage;
  const req = STAGE_META_AGENT_MAP[stage];
  if (!req) return { met: true, missing: [], reason: "no requirements" };

  const chain = state.dispatchChain || {};
  const dispatched = chain[stage] || [];

  const missing = req.required.filter((a) => !dispatched.includes(a));

  if (req.requiresAgentDispatch && state.dispatchedAgents.length === 0) {
    const nodeBindingGate = checkCapabilityNodeBindings(state);
    if (!nodeBindingGate.met) {
      return nodeBindingGate;
    }
  }

  // Verify fetchRecord exists when stage requires it
  if (req.requiresFetchRecord && !state.fetchRecord) {
    return {
      met: false,
      missing: ["fetchRecord in spine state"],
      reason:
        "Fetch stage must produce a fetchRecord before advancing to Thinking. " +
        "Complete capability search, write fetchRecord to spine state, then return to Thinking.",
    };
  }

  // Verify research validation when fetchRecord declares research required
  if (
    state.fetchRecord &&
    state.fetchRecord.researchRequired &&
    !state.fetchRecord.researchValidationPerformed
  ) {
    return {
      met: false,
      missing: ["research validation in fetchRecord"],
      reason:
        "Task requires research validation but researchValidationPerformed=false. " +
        "Discover web search tools via capability descriptors, search ≥5 source categories, " +
        "record in fetchRecord, then return to Thinking.",
    };
  }

  const choiceSurfaceGate = checkChoiceSurfaceGate(state);
  if (!choiceSurfaceGate.met) {
    return choiceSurfaceGate;
  }

  if (STAGE_ORDER.indexOf(stage) >= STAGE_ORDER.indexOf("execution")) {
    const readinessGate = checkPreExecutionReadiness(state);
    if (!readinessGate.met) {
      return readinessGate;
    }

    const nodeBindingGate = checkCapabilityNodeBindings(state);
    if (!nodeBindingGate.met) {
      return nodeBindingGate;
    }
  }

  return {
    met: missing.length === 0,
    missing,
    reason:
      missing.length > 0
        ? `Stage "${stage}" requires meta-agent(s): ${missing.join(", ")}. Dispatch them via Agent tool first.`
        : "requirements met",
  };
}

function normalizeChoiceSurfaceState(value) {
  return CHOICE_SURFACE_STATES.includes(value) ? value : "not_allowed";
}

function hasFetchEvidence(state) {
  return !!(
    state?.fetchRecord ||
    state?.fetchPacket ||
    state?.contentEvidencePacket ||
    state?.capabilityEvidencePacket
  );
}

function hasCandidateOptions(frame) {
  if (!frame || typeof frame !== "object") return false;
  const optionFields = [
    frame.candidatePaths,
    frame.solutionPaths,
    frame.options,
    frame.candidates,
    frame.cards,
  ];
  return optionFields.some((value) => Array.isArray(value) && value.length > 0);
}

function getPreDecisionOptionFrame(state) {
  return (
    state?.preDecisionOptionFrame ||
    state?.cardPlanPacket ||
    state?.businessFlowBlueprintPacket ||
    null
  );
}

const ALLOWED_CHOICE_GATE_SKIPS = new Set([
  "trivial",
  "no_branching_choice",
  "explicit_auto_proceed",
]);

function hasChoiceGateSkip(state) {
  const frame = getPreDecisionOptionFrame(state);
  const skip =
    state?.choiceGateSkip ||
    frame?.choiceGateSkip ||
    state?.intentGatePacket?.choiceGateSkip;
  if (!ALLOWED_CHOICE_GATE_SKIPS.has(skip)) return false;
  const skipSource = state?.skipSource || frame?.skipSource;
  const skipSafetyRationale =
    state?.skipSafetyRationale || frame?.skipSafetyRationale;
  return (
    typeof skipSource === "string" &&
    skipSource.trim().length > 0 &&
    typeof skipSafetyRationale === "string" &&
    skipSafetyRationale.trim().length > 0 &&
    !/fallback/i.test(skipSafetyRationale)
  );
}

export function checkChoiceSurfaceGate(state) {
  if (!state || state.queryBypass) {
    return { met: true, missing: [], reason: "choice surface gate bypassed" };
  }

  const stage = normalizeStage(state.currentStage);
  const stageIdx = STAGE_ORDER.indexOf(stage);
  const thinkingIdx = STAGE_ORDER.indexOf("thinking");
  const executionIdx = STAGE_ORDER.indexOf("execution");
  const choiceState = normalizeChoiceSurfaceState(state.choiceSurfaceState);
  const fetchEvidencePresent = hasFetchEvidence(state);
  const preDecisionFrame = getPreDecisionOptionFrame(state);
  const candidateOptionsPresent = hasCandidateOptions(preDecisionFrame);
  const skipRecorded = hasChoiceGateSkip(state);
  const decisionBasisPresent =
    fetchEvidencePresent && (candidateOptionsPresent || skipRecorded);

  if (
    stageIdx < thinkingIdx &&
    (choiceState === "execution_confirmation_allowed" ||
      choiceState === "completed")
  ) {
    return {
      met: false,
      missing: ["Fetch evidence", "Thinking candidate options"],
      reason:
        "Choice Surface Gate violation: execution confirmation appeared before Fetch and Thinking completed.",
    };
  }

  if (
    stage === "thinking" &&
    (choiceState === "execution_confirmation_allowed" ||
      choiceState === "completed") &&
    !decisionBasisPresent
  ) {
    return {
      met: false,
      missing: ["Fetch evidence", "preDecisionOptionFrame"],
      reason:
        "Choice Surface Gate violation: execution confirmation requires Fetch evidence and a Thinking option frame.",
    };
  }

  if (stageIdx >= executionIdx) {
    if (!decisionBasisPresent) {
      return {
        met: false,
        missing: ["Fetch evidence", "preDecisionOptionFrame"],
        reason:
          "Execution cannot start before Fetch evidence and Thinking candidate options are recorded.",
      };
    }

    if (choiceState !== "completed" && !skipRecorded) {
      return {
        met: false,
        missing: ["choiceSurfaceState=completed"],
        reason:
          "Execution cannot start before execution confirmation is completed or an explicit choiceGateSkip is recorded.",
      };
    }
  }

  return { met: true, missing: [], reason: "choice surface gate met" };
}

export function setQueryBypass(state, bypass) {
  return { ...state, queryBypass: bypass };
}

export function deactivateState(state) {
  return {
    ...state,
    active: false,
    deactivatedAt: new Date().toISOString(),
  };
}

export function isExecutionTool(toolName) {
  const execTools = [
    "Write",
    "Edit",
    "Bash",
    "MultiEdit",
    "NotebookEdit",
    "apply_patch",
  ];
  return execTools.includes(toolName);
}

export function isReadOnlyTool(toolName) {
  const readOnlyTools = [
    "Read",
    "Glob",
    "Grep",
    "LSPO",
    "TaskList",
    "TaskGet",
    "TaskOutput",
    "WebFetch",
    "WebSearch",
    "ListMcpResourcesTool",
    "ReadMcpResourceTool",
  ];
  return readOnlyTools.includes(toolName);
}

/**
 * Record a skipped hook to the audit trail
 * @param {object} state - Current spine state
 * @param {string} hookName - Name of the hook being skipped
 * @param {string} reason - Why the hook was skipped
 * @returns {object} - Updated state with skip record added
 */
export function recordSkippedHook(state, hookName, reason) {
  const record = {
    hook: hookName,
    reason,
    timestamp: new Date().toISOString(),
  };

  return {
    ...state,
    skippedHooks: [...(state.skippedHooks || []), record],
  };
}

/**
 * Get the current governance flow from task classification
 * Maps task classification to governance flow for hook skip decisions
 */
export function getGovernanceFlow(state) {
  const tc = state?.taskClassification;

  // Direct mapping from common classifications to governance flows
  const flowMap = {
    query: "query",
    simple_exec: "simple_exec",
    complex_dev: "complex_dev",
    meta_theory_auto: "complex_dev", // meta-theory is always complex
  };

  return flowMap[tc] || "simple_exec"; // Default to simple_exec
}
