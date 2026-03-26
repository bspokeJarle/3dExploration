# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v10.0.

## Table of Contents

- [Executive Summary](#executive-Summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
- [Aggregate NuGet packages details](#aggregate-nuget-packages-details)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Projects Relationship Graph](#projects-relationship-graph)
- [Project Details](#project-details)

  - [3dRotations\3dSpesifics.csproj](#3drotations3dspesificscsproj)
  - [3DSpesificsUnitTests\3DSpesificsUnitTests.csproj](#3dspesificsunittests3dspesificsunittestscsproj)
  - [3dTesting\Frontend.csproj](#3dtestingfrontendcsproj)
  - [BenchmarkSuite1\BenchmarkSuite.csproj](#benchmarksuite1benchmarksuitecsproj)
  - [BenchmarkSuite4\BenchmarkSuite4.csproj](#benchmarksuite4benchmarksuite4csproj)
  - [BenchmarkSuite5\BenchmarkSuite5.csproj](#benchmarksuite5benchmarksuite5csproj)
  - [CommonUtilities\CommonUtilities.csproj](#commonutilitiescommonutilitiescsproj)
  - [Domain\Domain.csproj](#domaindomaincsproj)
  - [GameAi\GameAiAndControls.csproj](#gameaigameaiandcontrolscsproj)
  - [GameAudio\GameAudio.csproj](#gameaudiogameaudiocsproj)


## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 10 | 3 require upgrade |
| Total NuGet Packages | 10 | All compatible |
| Total Code Files | 138 |  |
| Total Code Files with Incidents | 3 |  |
| Total Lines of Code | 26941 |  |
| Total Number of Issues | 3 |  |
| Estimated LOC to modify | 0+ | at least 0,0% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Package Issues | API Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| [3dRotations\3dSpesifics.csproj](#3drotations3dspesificscsproj) | net10.0-windows7.0 | ✅ None | 0 | 0 |  | Wpf, Sdk Style = True |
| [3DSpesificsUnitTests\3DSpesificsUnitTests.csproj](#3dspesificsunittests3dspesificsunittestscsproj) | net10.0-windows7.0 | ✅ None | 0 | 0 |  | DotNetCoreApp, Sdk Style = True |
| [3dTesting\Frontend.csproj](#3dtestingfrontendcsproj) | net10.0-windows7.0 | ✅ None | 0 | 0 |  | Wpf, Sdk Style = True |
| [BenchmarkSuite1\BenchmarkSuite.csproj](#benchmarksuite1benchmarksuitecsproj) | net8.0-windows | 🟢 Low | 0 | 0 |  | DotNetCoreApp, Sdk Style = True |
| [BenchmarkSuite4\BenchmarkSuite4.csproj](#benchmarksuite4benchmarksuite4csproj) | net8.0-windows | 🟢 Low | 0 | 0 |  | DotNetCoreApp, Sdk Style = True |
| [BenchmarkSuite5\BenchmarkSuite5.csproj](#benchmarksuite5benchmarksuite5csproj) | net8.0-windows | 🟢 Low | 0 | 0 |  | DotNetCoreApp, Sdk Style = True |
| [CommonUtilities\CommonUtilities.csproj](#commonutilitiescommonutilitiescsproj) | net10.0-windows7.0 | ✅ None | 0 | 0 |  | ClassLibrary, Sdk Style = True |
| [Domain\Domain.csproj](#domaindomaincsproj) | net10.0-windows7.0 | ✅ None | 0 | 0 |  | Wpf, Sdk Style = True |
| [GameAi\GameAiAndControls.csproj](#gameaigameaiandcontrolscsproj) | net10.0-windows7.0 | ✅ None | 0 | 0 |  | Wpf, Sdk Style = True |
| [GameAudio\GameAudio.csproj](#gameaudiogameaudiocsproj) | net10.0-windows7.0 | ✅ None | 0 | 0 |  | ClassLibrary, Sdk Style = True |

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 10 | 100,0% |
| ⚠️ Incompatible | 0 | 0,0% |
| 🔄 Upgrade Recommended | 0 | 0,0% |
| ***Total NuGet Packages*** | ***10*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 1113 |  |
| ***Total APIs Analyzed*** | ***1113*** |  |

## Aggregate NuGet packages details

| Package | Current Version | Suggested Version | Projects | Description |
| :--- | :---: | :---: | :--- | :--- |
| BenchmarkDotNet | 0.15.2 |  | [BenchmarkSuite.csproj](#benchmarksuite1benchmarksuitecsproj)<br/>[BenchmarkSuite4.csproj](#benchmarksuite4benchmarksuite4csproj)<br/>[BenchmarkSuite5.csproj](#benchmarksuite5benchmarksuite5csproj) | ✅Compatible |
| coverlet.collector | 6.0.0 |  | [3DSpesificsUnitTests.csproj](#3dspesificsunittests3dspesificsunittestscsproj) | ✅Compatible |
| Microsoft.NET.Test.Sdk | 17.6.0 |  | [3DSpesificsUnitTests.csproj](#3dspesificsunittests3dspesificsunittestscsproj) | ✅Compatible |
| Microsoft.VisualStudio.DiagnosticsHub.BenchmarkDotNetDiagnosers | 18.3.36812.1 |  | [BenchmarkSuite.csproj](#benchmarksuite1benchmarksuitecsproj)<br/>[BenchmarkSuite4.csproj](#benchmarksuite4benchmarksuite4csproj)<br/>[BenchmarkSuite5.csproj](#benchmarksuite5benchmarksuite5csproj) | ✅Compatible |
| MouseKeyHook | 5.7.1 |  | [CommonUtilities.csproj](#commonutilitiescommonutilitiescsproj)<br/>[Domain.csproj](#domaindomaincsproj)<br/>[GameAiAndControls.csproj](#gameaigameaiandcontrolscsproj) | ✅Compatible |
| MSTest.TestAdapter | 3.0.4 |  | [3DSpesificsUnitTests.csproj](#3dspesificsunittests3dspesificsunittestscsproj) | ✅Compatible |
| MSTest.TestFramework | 3.0.4 |  | [3DSpesificsUnitTests.csproj](#3dspesificsunittests3dspesificsunittestscsproj) | ✅Compatible |
| NAudio | 2.2.1 |  | [GameAudio.csproj](#gameaudiogameaudiocsproj) | ✅Compatible |
| NAudio.Core | 2.2.1 |  | [GameAudio.csproj](#gameaudiogameaudiocsproj) | ✅Compatible |
| System.Drawing.Common | 9.0.2 |  | [Domain.csproj](#domaindomaincsproj) | ✅Compatible |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |

## Projects Relationship Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart LR
    P1["<b>📦&nbsp;Frontend.csproj</b><br/><small>net10.0-windows7.0</small>"]
    P2["<b>📦&nbsp;3dSpesifics.csproj</b><br/><small>net10.0-windows7.0</small>"]
    P3["<b>📦&nbsp;GameAiAndControls.csproj</b><br/><small>net10.0-windows7.0</small>"]
    P4["<b>📦&nbsp;Domain.csproj</b><br/><small>net10.0-windows7.0</small>"]
    P5["<b>📦&nbsp;3DSpesificsUnitTests.csproj</b><br/><small>net10.0-windows7.0</small>"]
    P6["<b>📦&nbsp;CommonUtilities.csproj</b><br/><small>net10.0-windows7.0</small>"]
    P7["<b>📦&nbsp;GameAudio.csproj</b><br/><small>net10.0-windows7.0</small>"]
    P8["<b>📦&nbsp;BenchmarkSuite.csproj</b><br/><small>net8.0-windows</small>"]
    P9["<b>📦&nbsp;BenchmarkSuite4.csproj</b><br/><small>net8.0-windows</small>"]
    P10["<b>📦&nbsp;BenchmarkSuite5.csproj</b><br/><small>net8.0-windows</small>"]
    P1 --> P7
    P1 --> P2
    P2 --> P4
    P2 --> P3
    P2 --> P6
    P3 --> P4
    P3 --> P6
    P5 --> P3
    P5 --> P2
    P6 --> P4
    P7 --> P4
    P7 --> P6
    P8 --> P1
    P8 --> P6
    P9 --> P2
    P10 --> P1
    click P1 "#3dtestingfrontendcsproj"
    click P2 "#3drotations3dspesificscsproj"
    click P3 "#gameaigameaiandcontrolscsproj"
    click P4 "#domaindomaincsproj"
    click P5 "#3dspesificsunittests3dspesificsunittestscsproj"
    click P6 "#commonutilitiescommonutilitiescsproj"
    click P7 "#gameaudiogameaudiocsproj"
    click P8 "#benchmarksuite1benchmarksuitecsproj"
    click P9 "#benchmarksuite4benchmarksuite4csproj"
    click P10 "#benchmarksuite5benchmarksuite5csproj"

```

## Project Details

<a id="3drotations3dspesificscsproj"></a>
### 3dRotations\3dSpesifics.csproj

#### Project Info

- **Current Target Framework:** net10.0-windows7.0✅
- **SDK-style**: True
- **Project Kind:** Wpf
- **Dependencies**: 3
- **Dependants**: 3
- **Number of Files**: 29
- **Lines of Code**: 12239
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (3)"]
        P1["<b>📦&nbsp;Frontend.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P5["<b>📦&nbsp;3DSpesificsUnitTests.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P9["<b>📦&nbsp;BenchmarkSuite4.csproj</b><br/><small>net8.0-windows</small>"]
        click P1 "#3dtestingfrontendcsproj"
        click P5 "#3dspesificsunittests3dspesificsunittestscsproj"
        click P9 "#benchmarksuite4benchmarksuite4csproj"
    end
    subgraph current["3dSpesifics.csproj"]
        MAIN["<b>📦&nbsp;3dSpesifics.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click MAIN "#3drotations3dspesificscsproj"
    end
    subgraph downstream["Dependencies (3"]
        P4["<b>📦&nbsp;Domain.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P3["<b>📦&nbsp;GameAiAndControls.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P6["<b>📦&nbsp;CommonUtilities.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P4 "#domaindomaincsproj"
        click P3 "#gameaigameaiandcontrolscsproj"
        click P6 "#commonutilitiescommonutilitiescsproj"
    end
    P1 --> MAIN
    P5 --> MAIN
    P9 --> MAIN
    MAIN --> P4
    MAIN --> P3
    MAIN --> P6

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="3dspesificsunittests3dspesificsunittestscsproj"></a>
### 3DSpesificsUnitTests\3DSpesificsUnitTests.csproj

#### Project Info

- **Current Target Framework:** net10.0-windows7.0✅
- **SDK-style**: True
- **Project Kind:** DotNetCoreApp
- **Dependencies**: 2
- **Dependants**: 0
- **Number of Files**: 3
- **Lines of Code**: 1
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["3DSpesificsUnitTests.csproj"]
        MAIN["<b>📦&nbsp;3DSpesificsUnitTests.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click MAIN "#3dspesificsunittests3dspesificsunittestscsproj"
    end
    subgraph downstream["Dependencies (2"]
        P3["<b>📦&nbsp;GameAiAndControls.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P2["<b>📦&nbsp;3dSpesifics.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P3 "#gameaigameaiandcontrolscsproj"
        click P2 "#3drotations3dspesificscsproj"
    end
    MAIN --> P3
    MAIN --> P2

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="3dtestingfrontendcsproj"></a>
### 3dTesting\Frontend.csproj

#### Project Info

- **Current Target Framework:** net10.0-windows7.0✅
- **SDK-style**: True
- **Project Kind:** Wpf
- **Dependencies**: 2
- **Dependants**: 2
- **Number of Files**: 16
- **Lines of Code**: 3460
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (2)"]
        P8["<b>📦&nbsp;BenchmarkSuite.csproj</b><br/><small>net8.0-windows</small>"]
        P10["<b>📦&nbsp;BenchmarkSuite5.csproj</b><br/><small>net8.0-windows</small>"]
        click P8 "#benchmarksuite1benchmarksuitecsproj"
        click P10 "#benchmarksuite5benchmarksuite5csproj"
    end
    subgraph current["Frontend.csproj"]
        MAIN["<b>📦&nbsp;Frontend.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click MAIN "#3dtestingfrontendcsproj"
    end
    subgraph downstream["Dependencies (2"]
        P7["<b>📦&nbsp;GameAudio.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P2["<b>📦&nbsp;3dSpesifics.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P7 "#gameaudiogameaudiocsproj"
        click P2 "#3drotations3dspesificscsproj"
    end
    P8 --> MAIN
    P10 --> MAIN
    MAIN --> P7
    MAIN --> P2

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="benchmarksuite1benchmarksuitecsproj"></a>
### BenchmarkSuite1\BenchmarkSuite.csproj

#### Project Info

- **Current Target Framework:** net8.0-windows
- **Proposed Target Framework:** net10.0--windows
- **SDK-style**: True
- **Project Kind:** DotNetCoreApp
- **Dependencies**: 2
- **Dependants**: 0
- **Number of Files**: 11
- **Number of Files with Incidents**: 1
- **Lines of Code**: 611
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["BenchmarkSuite.csproj"]
        MAIN["<b>📦&nbsp;BenchmarkSuite.csproj</b><br/><small>net8.0-windows</small>"]
        click MAIN "#benchmarksuite1benchmarksuitecsproj"
    end
    subgraph downstream["Dependencies (2"]
        P1["<b>📦&nbsp;Frontend.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P6["<b>📦&nbsp;CommonUtilities.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P1 "#3dtestingfrontendcsproj"
        click P6 "#commonutilitiescommonutilitiescsproj"
    end
    MAIN --> P1
    MAIN --> P6

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 492 |  |
| ***Total APIs Analyzed*** | ***492*** |  |

<a id="benchmarksuite4benchmarksuite4csproj"></a>
### BenchmarkSuite4\BenchmarkSuite4.csproj

#### Project Info

- **Current Target Framework:** net8.0-windows
- **Proposed Target Framework:** net10.0--windows
- **SDK-style**: True
- **Project Kind:** DotNetCoreApp
- **Dependencies**: 1
- **Dependants**: 0
- **Number of Files**: 3
- **Number of Files with Incidents**: 1
- **Lines of Code**: 281
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["BenchmarkSuite4.csproj"]
        MAIN["<b>📦&nbsp;BenchmarkSuite4.csproj</b><br/><small>net8.0-windows</small>"]
        click MAIN "#benchmarksuite4benchmarksuite4csproj"
    end
    subgraph downstream["Dependencies (1"]
        P2["<b>📦&nbsp;3dSpesifics.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P2 "#3drotations3dspesificscsproj"
    end
    MAIN --> P2

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 301 |  |
| ***Total APIs Analyzed*** | ***301*** |  |

<a id="benchmarksuite5benchmarksuite5csproj"></a>
### BenchmarkSuite5\BenchmarkSuite5.csproj

#### Project Info

- **Current Target Framework:** net8.0-windows
- **Proposed Target Framework:** net10.0--windows
- **SDK-style**: True
- **Project Kind:** DotNetCoreApp
- **Dependencies**: 1
- **Dependants**: 0
- **Number of Files**: 3
- **Number of Files with Incidents**: 1
- **Lines of Code**: 335
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["BenchmarkSuite5.csproj"]
        MAIN["<b>📦&nbsp;BenchmarkSuite5.csproj</b><br/><small>net8.0-windows</small>"]
        click MAIN "#benchmarksuite5benchmarksuite5csproj"
    end
    subgraph downstream["Dependencies (1"]
        P1["<b>📦&nbsp;Frontend.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P1 "#3dtestingfrontendcsproj"
    end
    MAIN --> P1

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 320 |  |
| ***Total APIs Analyzed*** | ***320*** |  |

<a id="commonutilitiescommonutilitiescsproj"></a>
### CommonUtilities\CommonUtilities.csproj

#### Project Info

- **Current Target Framework:** net10.0-windows7.0✅
- **SDK-style**: True
- **Project Kind:** ClassLibrary
- **Dependencies**: 1
- **Dependants**: 4
- **Number of Files**: 23
- **Lines of Code**: 2776
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (4)"]
        P2["<b>📦&nbsp;3dSpesifics.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P3["<b>📦&nbsp;GameAiAndControls.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P7["<b>📦&nbsp;GameAudio.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P8["<b>📦&nbsp;BenchmarkSuite.csproj</b><br/><small>net8.0-windows</small>"]
        click P2 "#3drotations3dspesificscsproj"
        click P3 "#gameaigameaiandcontrolscsproj"
        click P7 "#gameaudiogameaudiocsproj"
        click P8 "#benchmarksuite1benchmarksuitecsproj"
    end
    subgraph current["CommonUtilities.csproj"]
        MAIN["<b>📦&nbsp;CommonUtilities.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click MAIN "#commonutilitiescommonutilitiescsproj"
    end
    subgraph downstream["Dependencies (1"]
        P4["<b>📦&nbsp;Domain.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P4 "#domaindomaincsproj"
    end
    P2 --> MAIN
    P3 --> MAIN
    P7 --> MAIN
    P8 --> MAIN
    MAIN --> P4

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="domaindomaincsproj"></a>
### Domain\Domain.csproj

#### Project Info

- **Current Target Framework:** net10.0-windows7.0✅
- **SDK-style**: True
- **Project Kind:** Wpf
- **Dependencies**: 0
- **Dependants**: 4
- **Number of Files**: 29
- **Lines of Code**: 916
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (4)"]
        P2["<b>📦&nbsp;3dSpesifics.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P3["<b>📦&nbsp;GameAiAndControls.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P6["<b>📦&nbsp;CommonUtilities.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P7["<b>📦&nbsp;GameAudio.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P2 "#3drotations3dspesificscsproj"
        click P3 "#gameaigameaiandcontrolscsproj"
        click P6 "#commonutilitiescommonutilitiescsproj"
        click P7 "#gameaudiogameaudiocsproj"
    end
    subgraph current["Domain.csproj"]
        MAIN["<b>📦&nbsp;Domain.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click MAIN "#domaindomaincsproj"
    end
    P2 --> MAIN
    P3 --> MAIN
    P6 --> MAIN
    P7 --> MAIN

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="gameaigameaiandcontrolscsproj"></a>
### GameAi\GameAiAndControls.csproj

#### Project Info

- **Current Target Framework:** net10.0-windows7.0✅
- **SDK-style**: True
- **Project Kind:** Wpf
- **Dependencies**: 2
- **Dependants**: 2
- **Number of Files**: 18
- **Lines of Code**: 5644
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (2)"]
        P2["<b>📦&nbsp;3dSpesifics.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P5["<b>📦&nbsp;3DSpesificsUnitTests.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P2 "#3drotations3dspesificscsproj"
        click P5 "#3dspesificsunittests3dspesificsunittestscsproj"
    end
    subgraph current["GameAiAndControls.csproj"]
        MAIN["<b>📦&nbsp;GameAiAndControls.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click MAIN "#gameaigameaiandcontrolscsproj"
    end
    subgraph downstream["Dependencies (2"]
        P4["<b>📦&nbsp;Domain.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P6["<b>📦&nbsp;CommonUtilities.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P4 "#domaindomaincsproj"
        click P6 "#commonutilitiescommonutilitiescsproj"
    end
    P2 --> MAIN
    P5 --> MAIN
    MAIN --> P4
    MAIN --> P6

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="gameaudiogameaudiocsproj"></a>
### GameAudio\GameAudio.csproj

#### Project Info

- **Current Target Framework:** net10.0-windows7.0✅
- **SDK-style**: True
- **Project Kind:** ClassLibrary
- **Dependencies**: 2
- **Dependants**: 1
- **Number of Files**: 5
- **Lines of Code**: 678
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (1)"]
        P1["<b>📦&nbsp;Frontend.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P1 "#3dtestingfrontendcsproj"
    end
    subgraph current["GameAudio.csproj"]
        MAIN["<b>📦&nbsp;GameAudio.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click MAIN "#gameaudiogameaudiocsproj"
    end
    subgraph downstream["Dependencies (2"]
        P4["<b>📦&nbsp;Domain.csproj</b><br/><small>net10.0-windows7.0</small>"]
        P6["<b>📦&nbsp;CommonUtilities.csproj</b><br/><small>net10.0-windows7.0</small>"]
        click P4 "#domaindomaincsproj"
        click P6 "#commonutilitiescommonutilitiescsproj"
    end
    P1 --> MAIN
    MAIN --> P4
    MAIN --> P6

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

