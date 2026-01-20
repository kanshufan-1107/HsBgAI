# AI Coding Agent Instructions for HsBgAI

Welcome to the HsBgAI codebase! This document provides essential guidance for AI coding agents to be productive in this project. It covers the architecture, workflows, conventions, and integration points specific to this repository.

## Project Overview

HsBgAI is an artificial intelligence system for Hearthstone Battlegrounds, built on the BepInEx plugin framework. The project is structured to:
- Analyze game states and make strategic decisions.
- Integrate with Hearthstone via the BepInEx patching system.
- Provide extensible and modular AI logic.

### Key Components

1. **AI Logic** (`Ai/`):
   - Core decision-making logic resides in `MainProcess.cs`.
   - Implements strategies for various game scenarios.

2. **Game State Management** (`LibHearthstone/Mapping/`):
   - Handles mapping and representation of Hearthstone game states.
   - Example: `GameInfo.cs` contains the structure for game state data.

3. **Firestone Integration** (`Firestone/`):
   - Interfaces with Firestone for additional game data and statistics.
   - Example: `FirestoneClient.cs` manages communication with Firestone.

4. **Patching System** (`Patchers/`):
   - Hooks into Hearthstone using BepInEx patchers.
   - Example: `MainHubPatcher.cs` modifies the main game hub behavior.

5. **Utilities** (`Tools/`):
   - Common helper functions for logging, async operations, and argument parsing.
   - Example: `LogPrintUtils.cs` standardizes logging across the project.

## Developer Workflows

### Building the Project
- Use the `build.bat` script to compile the project.
- Ensure dependencies are resolved via NuGet.

### Testing and Debugging
- Debugging relies on attaching to the Hearthstone process.
- Use BepInEx logs to trace execution and identify issues.

### Adding New Features
- Follow the modular structure: place new AI logic in `Ai/` and utilities in `Tools/`.
- Update patchers in `Patchers/` to integrate new functionality.

## Project-Specific Conventions

- **Logging:** Use `LogPrintUtils` for consistent log formatting.
- **Game State Updates:** Modify `GameInfo.cs` for changes to game state representation.
- **AI Strategies:** Extend `HeroStrategies.cs` for hero-specific strategies.

## Integration Points

- **BepInEx:** Ensure compatibility with the BepInEx plugin framework.
- **Firestone:** Maintain synchronization with Firestone data structures.

## Examples

- **Adding a New Hero Strategy:**
  1. Create a new method in `HeroStrategies.cs`.
  2. Update `CompsStrategy.cs` to include the new strategy.

- **Logging a New Event:**
  1. Use `LogPrintUtils.LogInfo("Your message here");`.

## Key Files and Directories

- `Ai/MainProcess.cs`: Core AI logic.
- `LibHearthstone/Mapping/GameInfo.cs`: Game state representation.
- `Patchers/MainHubPatcher.cs`: Main game patcher.
- `Tools/LogPrintUtils.cs`: Logging utilities.

By following these guidelines, AI agents can contribute effectively to the HsBgAI project. For any questions, refer to the code comments and examples in the repository.