# AGENTS

If local environment is..
- Windows: Use PowerShell for command execution, winget for package installation
- MacOS: sh, zsh, bash for command execution, brew for package installation
  - Validate that local enviroment has Xcode Command Line Tools installed, if not, install it using `xcode-select --install`
  - Validate that local enviroment has Homebrew installed, if not, install it using `/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"`
- Linux: sh, bash for command execution, apt (or equivalent) for package installation

## Requirements

Check the local environment and install the following tools if they are not already installed: (`rg`, `fd`, `tree`, `ast-grep`, `yq`, `wget`, `node`, `dotnet`, `python`, `git`)
- It may be expected to use runtime version manager for node, dotnet, python, etc. (e.g., `nvm`, `asdf`, `pyenv`, etc.) check if the runtime version manager is installed, if not, install it using the official installation script for each runtime version manager.  
- When install `dotnet`, install runtime that supports C# 9.0. 

## Terminology

- Do not use `계약` or `contract` to mean a requirement, work objective, specification, interface expectation, validation rule, or acceptance criterion.
- Use a precise alternative such as `요구사항` (requirement), `명세` (specification), `규약` (interface convention), `검증 기준` (validation criterion), or `작업 목표` (work objective), according to context.
- `계약` is allowed only when it means an actual legal or commercial agreement.

## Index

- [Unity.md](./Unity.md) : When using Unity
