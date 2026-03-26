# .NET 10 Upgrade Plan — TheOmegaStrain Solution

## Table of Contents

- [1. Executive Summary](#1-executive-summary)
- [2. Migration Strategy](#2-migration-strategy)
- [3. Detailed Dependency Analysis](#3-detailed-dependency-analysis)
- [4. Project-by-Project Plans](#4-project-by-project-plans)
  - [4.1 BenchmarkSuite.csproj](#41-benchmarksuitecsproj)
  - [4.2 BenchmarkSuite4.csproj](#42-benchmarksuite4csproj)
  - [4.3 BenchmarkSuite5.csproj](#43-benchmarksuite5csproj)
- [5. Package Update Reference](#5-package-update-reference)
- [6. Breaking Changes Catalog](#6-breaking-changes-catalog)
- [7. Risk Management](#7-risk-management)
- [8. Testing & Validation Strategy](#8-testing--validation-strategy)
- [9. Complexity & Effort Assessment](#9-complexity--effort-assessment)
- [10. Source Control Strategy](#10-source-control-strategy)
- [11. Success Criteria](#11-success-criteria)

---

## 1. Executive Summary

### Scenario

Upgrade the **TheOmegaStrain** solution from mixed .NET 8 / .NET 10 targeting to fully .NET 10 (`net10.0-windows`).

### Scope

| Metric | Value |
|---|---|
| Total projects in solution | 10 |
| Projects already on .NET 10 | 7 (no changes needed) |
| Projects requiring upgrade | 3 (BenchmarkSuite, BenchmarkSuite4, BenchmarkSuite5) |
| Total NuGet packages | 10 — all compatible, no updates needed |
| API issues | 0 |
| Estimated code changes | 0 LOC |
| Security vulnerabilities | 0 |

### Selected Strategy

**All-At-Once Strategy** — All 3 remaining projects upgraded simultaneously in a single atomic operation.

**Rationale:**
- Only 3 projects require changes (small scope)
- All 3 are leaf-node benchmark projects with no dependants
- Zero package updates, zero API changes, zero code modifications needed
- All dependencies (Frontend, 3dSpesifics, CommonUtilities) already target net10.0-windows7.0
- Risk is minimal — this is purely a `TargetFramework` property change in 3 project files

### Complexity Classification

**Simple** — 3 projects, dependency depth ≤2, no high-risk items, no vulnerabilities, no code changes. Planned using fast batch approach (single detail iteration for all projects).

### Critical Issues

None identified. All packages are compatible, all APIs are compatible, and no security vulnerabilities were found.

## 2. Migration Strategy

### Approach: All-At-Once

All 3 benchmark projects will be upgraded simultaneously in a single coordinated operation. No intermediate states are needed.

### Justification

| Criterion | Assessment | Supports All-At-Once? |
|---|---|---|
| Project count (requiring changes) | 3 | ✅ Well under the 30-project threshold |
| Dependency complexity | Leaf nodes only, 0 dependants | ✅ Simplest possible structure |
| Package updates needed | 0 | ✅ No compatibility risk |
| API breaking changes | 0 | ✅ No code modifications |
| Security vulnerabilities | 0 | ✅ No urgency-driven ordering |
| All dependencies on target TFM | Yes — all already on net10.0-windows7.0 | ✅ No blocking dependencies |

An incremental strategy would add unnecessary overhead for this scope. The only change is a `TargetFramework` property update in 3 project files.

### Dependency-Based Ordering

Not applicable — all 3 projects are independent leaf nodes. They have no dependants and no inter-dependencies, so order does not matter. They will be updated as a single atomic batch.

### Execution Plan

**Phase 1: Atomic Upgrade** — Update all 3 project files, restore dependencies, build the entire solution, fix any compilation errors (none expected).

**Phase 2: Test Validation** — Run the test project `3DSpesificsUnitTests.csproj` to confirm no regressions.

## 3. Detailed Dependency Analysis

### Dependency Graph Summary

The 3 projects requiring upgrade are all **leaf nodes** (no other projects depend on them). Their dependencies all already target `net10.0-windows7.0`:

```
BenchmarkSuite.csproj (net8.0-windows)
├── Frontend.csproj (net10.0-windows7.0) ✅
└── CommonUtilities.csproj (net10.0-windows7.0) ✅

BenchmarkSuite4.csproj (net8.0-windows)
└── 3dSpesifics.csproj (net10.0-windows7.0) ✅

BenchmarkSuite5.csproj (net8.0-windows)
└── Frontend.csproj (net10.0-windows7.0) ✅
```

### Project Groupings

Since all 3 projects are independent leaf nodes with no inter-dependencies, they can all be upgraded in a **single atomic operation**. No phased migration is needed.

| Group | Projects | Rationale |
|---|---|---|
| Atomic Upgrade | BenchmarkSuite, BenchmarkSuite4, BenchmarkSuite5 | All leaf nodes, all dependencies already on .NET 10, no dependants |

### Critical Path

There is no critical path concern. All 3 projects are independent leaves — none blocks another.

### Circular Dependencies

None detected.

## 4. Project-by-Project Plans

### 4.1 BenchmarkSuite.csproj

**Current State:**
- Target Framework: `net8.0-windows`
- Project Kind: DotNetCoreApp (SDK-style)
- Dependencies: Frontend.csproj, CommonUtilities.csproj (both already net10.0-windows7.0)
- Dependants: None (leaf node)
- NuGet Packages: BenchmarkDotNet 0.15.2, Microsoft.VisualStudio.DiagnosticsHub.BenchmarkDotNetDiagnosers 18.3.36812.1
- Files: 11 | LOC: 611
- Risk Level: 🟢 Low

**Target State:**
- Target Framework: `net10.0-windows`
- Updated packages: 0 (all compatible)

**Migration Steps:**
1. **Prerequisites**: All dependencies (Frontend.csproj, CommonUtilities.csproj) already target net10.0-windows7.0 — no blockers.
2. **Framework Update**: Change `<TargetFramework>net8.0-windows</TargetFramework>` to `<TargetFramework>net10.0-windows</TargetFramework>` in `BenchmarkSuite1\BenchmarkSuite.csproj`.
3. **Package Updates**: None required — BenchmarkDotNet 0.15.2 and DiagnosticsHub diagnoser are compatible.
4. **Expected Breaking Changes**: None identified by assessment (0 API issues, 492 APIs analyzed — all compatible).
5. **Code Modifications**: None expected.
6. **Validation**:
   - [ ] Project builds without errors
   - [ ] No new warnings introduced

---

### 4.2 BenchmarkSuite4.csproj

**Current State:**
- Target Framework: `net8.0-windows`
- Project Kind: DotNetCoreApp (SDK-style)
- Dependencies: 3dSpesifics.csproj (already net10.0-windows7.0)
- Dependants: None (leaf node)
- NuGet Packages: BenchmarkDotNet 0.15.2, Microsoft.VisualStudio.DiagnosticsHub.BenchmarkDotNetDiagnosers 18.3.36812.1
- Files: 3 | LOC: 281
- Risk Level: 🟢 Low

**Target State:**
- Target Framework: `net10.0-windows`
- Updated packages: 0 (all compatible)

**Migration Steps:**
1. **Prerequisites**: Dependency (3dSpesifics.csproj) already targets net10.0-windows7.0 — no blockers.
2. **Framework Update**: Change `<TargetFramework>net8.0-windows</TargetFramework>` to `<TargetFramework>net10.0-windows</TargetFramework>` in `BenchmarkSuite4\BenchmarkSuite4.csproj`.
3. **Package Updates**: None required — all packages compatible.
4. **Expected Breaking Changes**: None identified (0 API issues, 301 APIs analyzed — all compatible).
5. **Code Modifications**: None expected.
6. **Validation**:
   - [ ] Project builds without errors
   - [ ] No new warnings introduced

---

### 4.3 BenchmarkSuite5.csproj

**Current State:**
- Target Framework: `net8.0-windows`
- Project Kind: DotNetCoreApp (SDK-style)
- Dependencies: Frontend.csproj (already net10.0-windows7.0)
- Dependants: None (leaf node)
- NuGet Packages: BenchmarkDotNet 0.15.2, Microsoft.VisualStudio.DiagnosticsHub.BenchmarkDotNetDiagnosers 18.3.36812.1
- Files: 3 | LOC: 335
- Risk Level: 🟢 Low

**Target State:**
- Target Framework: `net10.0-windows`
- Updated packages: 0 (all compatible)

**Migration Steps:**
1. **Prerequisites**: Dependency (Frontend.csproj) already targets net10.0-windows7.0 — no blockers.
2. **Framework Update**: Change `<TargetFramework>net8.0-windows</TargetFramework>` to `<TargetFramework>net10.0-windows</TargetFramework>` in `BenchmarkSuite5\BenchmarkSuite5.csproj`.
3. **Package Updates**: None required — all packages compatible.
4. **Expected Breaking Changes**: None identified (0 API issues, 320 APIs analyzed — all compatible).
5. **Code Modifications**: None expected.
6. **Validation**:
   - [ ] Project builds without errors
   - [ ] No new warnings introduced

## 5. Package Update Reference

No package updates are required. All 10 NuGet packages across the solution are compatible with net10.0:

| Package | Current Version | Target Version | Projects Affected | Status |
|---|---|---|---|---|
| BenchmarkDotNet | 0.15.2 | 0.15.2 (no change) | BenchmarkSuite, BenchmarkSuite4, BenchmarkSuite5 | ✅ Compatible |
| Microsoft.VisualStudio.DiagnosticsHub.BenchmarkDotNetDiagnosers | 18.3.36812.1 | 18.3.36812.1 (no change) | BenchmarkSuite, BenchmarkSuite4, BenchmarkSuite5 | ✅ Compatible |
| MouseKeyHook | 5.7.1 | — | CommonUtilities, Domain, GameAiAndControls | ✅ Compatible (already on net10.0) |
| NAudio | 2.2.1 | — | GameAudio | ✅ Compatible (already on net10.0) |
| NAudio.Core | 2.2.1 | — | GameAudio | ✅ Compatible (already on net10.0) |
| System.Drawing.Common | 9.0.2 | — | Domain | ✅ Compatible (already on net10.0) |
| coverlet.collector | 6.0.0 | — | 3DSpesificsUnitTests | ✅ Compatible (already on net10.0) |
| Microsoft.NET.Test.Sdk | 17.6.0 | — | 3DSpesificsUnitTests | ✅ Compatible (already on net10.0) |
| MSTest.TestAdapter | 3.0.4 | — | 3DSpesificsUnitTests | ✅ Compatible (already on net10.0) |
| MSTest.TestFramework | 3.0.4 | — | 3DSpesificsUnitTests | ✅ Compatible (already on net10.0) |

## 6. Breaking Changes Catalog

The assessment analyzed 1,113 APIs across the 3 affected projects and found **zero** breaking changes:

| Category | Count |
|---|---|
| 🔴 Binary Incompatible | 0 |
| 🟡 Source Incompatible | 0 |
| 🔵 Behavioral Change | 0 |
| ✅ Compatible | 1,113 |

### Potential Discovery Areas

While no breaking changes were detected, the following areas should be verified during the build step:

- **New .NET 10 analyzer warnings**: The .NET 10 SDK ships updated analyzers that may produce new warnings not present in .NET 8.
- **BenchmarkDotNet runtime host compatibility**: BenchmarkDotNet spawns child processes targeting the host framework; verify benchmark execution works correctly under net10.0.

## 7. Risk Management

### High-Risk Changes

None. All 3 projects have **Low** risk:

| Project | Risk Level | Description | Mitigation |
|---|---|---|---|
| BenchmarkSuite.csproj | 🟢 Low | TFM change only, 611 LOC, 0 package updates, 0 API issues | Build verification |
| BenchmarkSuite4.csproj | 🟢 Low | TFM change only, 281 LOC, 0 package updates, 0 API issues | Build verification |
| BenchmarkSuite5.csproj | 🟢 Low | TFM change only, 335 LOC, 0 package updates, 0 API issues | Build verification |

### Security Vulnerabilities

None identified in any NuGet packages.

### Contingency Plans

| Scenario | Mitigation |
|---|---|
| Unexpected build errors after TFM change | Inspect compiler errors; likely caused by implicit .NET 10 analyzer rules. Fix warnings/errors individually. |
| BenchmarkDotNet 0.15.2 runtime incompatibility | Verify BenchmarkDotNet supports net10.0. If not, update to latest compatible version. |
| Rollback needed | Revert the 3 project file changes (single commit) or reset to the `main` branch. |

## 8. Testing & Validation Strategy

### Phase 1 Validation: After Atomic Upgrade

**Build Verification** (automated):
- Restore all NuGet packages for the solution
- Build the entire solution
- Expected result: 0 errors across all 10 projects

### Phase 2 Validation: Test Execution

**Unit Tests** (automated):
- Run test project: `3DSpesificsUnitTests\3DSpesificsUnitTests.csproj`
- Expected result: All tests pass

### Test Project Summary

| Test Project | Framework | Dependencies Under Test |
|---|---|---|
| 3DSpesificsUnitTests.csproj | net10.0-windows7.0 (already migrated) | GameAiAndControls, 3dSpesifics |

> **Note**: The 3 benchmark projects (BenchmarkSuite, BenchmarkSuite4, BenchmarkSuite5) are performance benchmarking tools, not test projects. They do not participate in automated test validation.

## 9. Complexity & Effort Assessment

### Per-Project Complexity

| Project | Complexity | Dependencies | Package Changes | Code Changes | Risk |
|---|---|---|---|---|---|
| BenchmarkSuite.csproj | Low | 2 (both on net10.0) | 0 | 0 | 🟢 Low |
| BenchmarkSuite4.csproj | Low | 1 (on net10.0) | 0 | 0 | 🟢 Low |
| BenchmarkSuite5.csproj | Low | 1 (on net10.0) | 0 | 0 | 🟢 Low |

### Overall Assessment

| Dimension | Rating |
|---|---|
| Scope complexity | Low — 3 project files, TFM change only |
| Dependency complexity | Low — all leaf nodes, all dependencies already migrated |
| Package complexity | Low — zero updates needed |
| Code change complexity | Low — zero code modifications |
| Testing complexity | Low — one test project, already on net10.0 |

## 10. Source Control Strategy

### Branching

- **Source branch**: `main`
- **Upgrade branch**: `upgrade-to-NET10` (already created)
- All upgrade changes will be made on the `upgrade-to-NET10` branch

### Commit Strategy

**Single commit** for the entire atomic upgrade:
- Commit message: `Upgrade BenchmarkSuite projects from net8.0-windows to net10.0-windows`
- Includes all 3 project file TFM changes
- Committed only after the solution builds successfully and all tests pass

### Merge Process

- After successful upgrade and validation, merge `upgrade-to-NET10` into `main`
- Use a pull request if team review is desired
- No conflicts expected since only 3 `.csproj` files are modified

## 11. Success Criteria

### Technical Criteria

- [x] All 7 already-migrated projects remain on net10.0-windows7.0 (unchanged)
- [ ] BenchmarkSuite.csproj targets `net10.0-windows`
- [ ] BenchmarkSuite4.csproj targets `net10.0-windows`
- [ ] BenchmarkSuite5.csproj targets `net10.0-windows`
- [ ] Entire solution builds with 0 errors
- [ ] All unit tests in 3DSpesificsUnitTests pass
- [ ] No package dependency conflicts
- [ ] No security vulnerabilities

### Quality Criteria

- [ ] No new compiler warnings introduced by the TFM change
- [ ] All NuGet packages restore successfully

### Process Criteria

- [ ] All-At-Once strategy followed (single atomic upgrade)
- [ ] Changes committed on `upgrade-to-NET10` branch
- [ ] Single commit for the upgrade
