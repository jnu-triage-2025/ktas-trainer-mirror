# Agent skills

The canonical directory name is `Agents/SKILLS` (uppercase).

On a case-insensitive filesystem, Git can retain the old `Agents/Skills` spelling in its index. To record the rename, run this once from the repository root:

```sh
git config core.ignorecase false
git mv -f Agents/Skills Agents/.skills-case-temp
git mv Agents/.skills-case-temp Agents/SKILLS
git add -A Agents/SKILLS
```

The temporary path is intentional: it forces Git to observe a case-only directory rename. Do not create a second `Agents/Skills` directory.
