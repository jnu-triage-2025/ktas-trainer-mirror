# AGENTS

This directory contains guidelines for AI agents.  

## INDEX

For information on writing proposals, existing proposals, and templates, please check the [`./Proposals`](./Proposals) directory.  

For reference when performing tasks using tools, plugins, skills, etc., please check the [`./SKILLS`](./SKILLS) directory.  

## RESTRICTIONS

Most human operator instructions pertain to the codebase and serialized data of the `MultiplayerInfrastructure` and `TriageTrainer` modules. In addition to these, there are some self-authored modules; however, the majority of modules are third-party modules, so modifying them is likely neither appropriate nor intended. (Reading or copying to outside the module is not included in this restriction and is therefore permitted.)  

If you determine that a third-party module must be modified, you must obtain confirmation from the human operator before proceeding with the modification, unless the instruction explicitly states in advance that the third-party module should be modified.  
