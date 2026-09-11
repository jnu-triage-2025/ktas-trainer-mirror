#!/usr/bin/env python3
"""Create a filtered, history-preserving Git mirror.

Run from the source repository root:
    python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --dry-run
    python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --push

The command reads commits reachable from either origin's remote-tracking branches
or local branches, plus local tags. It deliberately never uses the working tree,
so uncommitted files are not mirrored.
"""

from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
from collections.abc import Iterable
from functools import lru_cache
from pathlib import Path
from typing import Any
from urllib.parse import urlparse

try:
    import tomllib
except ModuleNotFoundError:  # pragma: no cover - Python 3.11+ is required.
    sys.exit("Python 3.11 or newer is required.")


class MirrorError(RuntimeError):
    pass


def run_git(args: list[str], *, input_bytes: bytes | None = None, env: dict[str, str] | None = None) -> bytes:
    command = ["git", *args]
    result = subprocess.run(command, input=input_bytes, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=env)
    if result.returncode:
        message = result.stderr.decode("utf-8", "replace").strip()
        raise MirrorError(f"{' '.join(command[:3])}: {message}")
    return result.stdout


def load_dotenv(path: Path) -> None:
    if not path.is_file():
        return
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip().strip("\"'")
        if key and key not in os.environ:
            os.environ[key] = value


def config_fingerprint(config: dict[str, Any]) -> str:
    relevant = {key: value for key, value in config.items() if key not in {"destination", "state_file"}}
    encoded = json.dumps(relevant, sort_keys=True, separators=(",", ":")).encode()
    return hashlib.sha256(encoded).hexdigest()


def load_config(config_path: Path) -> dict[str, Any]:
    config = tomllib.loads(config_path.read_text(encoding="utf-8"))
    generated_path = config_path.parent / config.get("generated_config", "code-mirror.gen.toml")
    generated: dict[str, Any] = {}
    if generated_path.is_file():
        generated = tomllib.loads(generated_path.read_text(encoding="utf-8"))
    config["generated_module_policies"] = generated.get("module_policies", {})
    return config


@lru_cache(maxsize=None)
def path_facts(path: str) -> tuple[str, str, tuple[str, ...]]:
    """Return the file name, lowercased suffix and path components of a path.

    Every rule check of every source commit needs these, and a long history
    repeats the same paths thousands of times. Parsing each distinct path once
    keeps a run that cannot reuse recorded rewrites from spending most of its
    time in the path parser.
    """
    parsed = Path(path)
    return parsed.name, parsed.suffix.lower(), parsed.parts


def module_name(path: str) -> str | None:
    parts = path_facts(path)[2]
    if len(parts) >= 3 and parts[0:2] == ("Assets", "Modules"):
        return parts[2]
    return None


def module_policy(config: dict[str, Any], path: str) -> str | None:
    name = module_name(path)
    if not name:
        return None
    manual = config.get("module_policies", {})
    generated = config.get("generated_module_policies", {})
    # A manual declaration always wins over generated classification.
    if name in manual.get("always_include", []):
        return "include"
    if name in manual.get("always_exclude", []):
        return "exclude"
    if name in generated.get("always_include", []):
        return "include"
    if name in generated.get("always_exclude", []):
        return "exclude"
    return None


def source_refs(source: dict[str, Any]) -> dict[str, str]:
    mode = source.get("mode", "remote_tracking")
    if mode == "remote_tracking":
        prefix = f"refs/remotes/{source.get('remote', 'origin')}/"
    elif mode == "local":
        prefix = "refs/heads/"
    else:
        raise MirrorError(f"Unknown source mode: {mode}")
    output = run_git(["for-each-ref", "--format=%(refname) %(objectname)", prefix, "refs/tags/"])
    refs: dict[str, str] = {}
    for row in output.decode().splitlines():
        ref, object_id = row.split(" ", 1)
        if mode == "remote_tracking" and ref == f"{prefix}HEAD":
            continue
        destination_ref = f"refs/heads/{ref[len(prefix):]}" if mode == "remote_tracking" and ref.startswith(prefix) else ref
        # An annotated tag points to a tag object, while rev-list and the commit
        # map operate on commits. The destination tag is deliberately lightweight.
        refs[destination_ref] = run_git(["rev-parse", "--verify", f"{ref}^{{commit}}"]).decode().strip()
    if not refs:
        raise MirrorError(f"No source branches or tags found for source mode '{mode}'.")
    return refs


def commit_graph(refs: Iterable[str]) -> list[tuple[str, list[str]]]:
    """Return every reachable commit in topological order with its parents.

    Reading the parents from the same traversal avoids spawning one `git show`
    per commit, which is a large share of the runtime on a long history.
    """
    rows = run_git(["rev-list", "--topo-order", "--reverse", "--parents", *refs]).decode().splitlines()
    graph: list[tuple[str, list[str]]] = []
    for row in rows:
        commit, *parents = row.split()
        graph.append((commit, parents))
    return graph


def existing_commits(object_ids: Iterable[str]) -> set[str]:
    """Report which of the recorded mirror commits the object database still has.

    A single batch query keeps this cheap even when the state file maps every
    commit of a long history.
    """
    unique = sorted(set(object_ids))
    if not unique:
        return set()
    output = run_git(["cat-file", "--batch-check=%(objectname) %(objecttype)"], input_bytes="\n".join(unique).encode() + b"\n")
    # A missing object is reported as "<query> missing", so requiring the commit
    # type also filters those out.
    return {parts[0] for parts in (line.split(" ") for line in output.decode().splitlines()) if len(parts) == 2 and parts[1] == "commit"}


def commit_metadata(commit: str) -> tuple[dict[str, str], bytes]:
    fmt = "%an%x00%ae%x00%aI%x00%cn%x00%ce%x00%cI%x00"
    fields = run_git(["show", "-s", f"--format={fmt}", commit]).split(b"\0")
    if len(fields) < 7:
        raise MirrorError(f"Unable to read metadata for {commit}.")
    env = {
        "GIT_AUTHOR_NAME": fields[0].decode(), "GIT_AUTHOR_EMAIL": fields[1].decode(),
        "GIT_AUTHOR_DATE": fields[2].decode(), "GIT_COMMITTER_NAME": fields[3].decode(),
        "GIT_COMMITTER_EMAIL": fields[4].decode(), "GIT_COMMITTER_DATE": fields[5].decode(),
    }
    message = run_git(["show", "-s", "--format=%B", commit])
    return env, message


def paths_at(commit: str) -> list[tuple[str, str, str, int]]:
    raw = run_git(["ls-tree", "-r", "-l", "-z", commit])
    entries: list[tuple[str, str, str, int]] = []
    for entry in raw.split(b"\0"):
        if not entry:
            continue
        header, raw_path = entry.split(b"\t", 1)
        mode, kind, _object_id, raw_size = header.decode().split(" ", 3)
        size_text = raw_size.strip()
        size = int(size_text) if size_text != "-" else 0
        entries.append((raw_path.decode("utf-8", "surrogateescape"), mode, kind, size))
    return entries


def is_descendant(commit: str, ancestor: str) -> bool:
    return subprocess.run(["git", "merge-base", "--is-ancestor", ancestor, commit]).returncode == 0


def applies_to_commit(scope: dict[str, Any], commit: str, branches: set[str]) -> bool:
    mode = scope.get("mode", "always")
    if mode == "always":
        return True
    if mode == "branches":
        requested = set(scope.get("names", []))
        return bool(requested & branches)
    if mode == "before":
        return is_descendant(str(scope["commit"]), commit)
    if mode == "after":
        return is_descendant(commit, str(scope["commit"]))
    if mode == "between":
        return is_descendant(commit, str(scope["from"])) and is_descendant(str(scope["to"]), commit)
    raise MirrorError(f"Unknown scope mode: {mode}")


def branch_membership(refs: dict[str, str]) -> dict[str, set[str]]:
    membership: dict[str, set[str]] = {}
    for ref, tip in refs.items():
        if not ref.startswith("refs/heads/"):
            continue
        branch = ref.removeprefix("refs/heads/")
        for commit in run_git(["rev-list", tip]).decode().splitlines():
            membership.setdefault(commit, set()).add(branch)
    return membership


@lru_cache(maxsize=None)
def normalized_extensions(values: tuple[str, ...]) -> frozenset[str]:
    return frozenset(item.lower() if item.startswith(".") else f".{item.lower()}" for item in values)


def matches(rule: dict[str, Any], path: str, size: int) -> bool:
    filename, extension, parts = path_facts(path)
    directory_names = parts[:-1]
    if rule.get("preserve_meta", False) and extension == ".meta":
        return False
    selector = rule.get("match", {})
    if "max_size_bytes" in selector and size >= int(selector["max_size_bytes"]):
        return True
    return (
        any(fnmatch.fnmatchcase(part, pattern) for pattern in selector.get("directory_names", []) for part in directory_names)
        or any(fnmatch.fnmatchcase(filename, pattern) for pattern in selector.get("file_names", []))
        or extension in normalized_extensions(tuple(selector.get("extensions", [])))
        or any(fnmatch.fnmatchcase(path, pattern) for pattern in selector.get("paths", []))
    )


def allowed_by_default(config: dict[str, Any], path: str) -> bool:
    policy = module_policy(config, path)
    filename, extension, _parts = path_facts(path)
    if policy == "include":
        return True
    if policy == "exclude":
        return extension == ".meta" and bool(config.get("module_policies", {}).get("always_include_meta", True))
    include = config.get("include", {})
    extensions = normalized_extensions(tuple(include.get("extensions", [])))
    return extension in extensions or any(fnmatch.fnmatchcase(filename, pattern) for pattern in include.get("file_names", [])) or any(fnmatch.fnmatchcase(path, pattern) for pattern in include.get("paths", []))


def path_is_excluded(config: dict[str, Any], path: str, kind: str, size: int, active: list[dict[str, Any]]) -> bool:
    policy = module_policy(config, path)
    extension = path_facts(path)[1]
    excluded_module_meta = policy == "exclude" and extension == ".meta" and bool(config.get("module_policies", {}).get("always_include_meta", True))
    # Explicit module inclusion and retained excluded-module metadata ignore
    # generic size and extension rules.
    force_include = policy == "include" or excluded_module_meta
    force_exclude = any(rule.get("force", False) and matches(rule, path, size) for rule in active)
    # Keep explicitly allowed submodule gitlinks so directories such as
    # Tools remain visible in the filtered mirror. Other tree entry kinds
    # are still excluded because they cannot be represented safely here.
    explicitly_allowed_gitlink = kind == "commit" and allowed_by_default(config, path)
    return (kind != "blob" and not explicitly_allowed_gitlink) or force_exclude or not allowed_by_default(config, path) or (not force_include and any(matches(rule, path, size) for rule in active))


# Keyed by the set of active rules, then by the tree entry the verdict describes.
DECISION_CACHE: dict[tuple[int, ...], dict[tuple[str, str, int], bool]] = {}


def excluded_paths(config: dict[str, Any], commit: str, branches: set[str]) -> list[str]:
    rules = config.get("rules", [])
    active_indices = tuple(index for index, rule in enumerate(rules) if applies_to_commit(rule.get("scope", {}), commit, branches))
    active = [rules[index] for index in active_indices]
    # A verdict depends only on the tree entry and on which rules are active, so
    # every commit that carries the same entry reaches the same answer. Deciding
    # each distinct entry once is what keeps a run that cannot reuse recorded
    # rewrites from evaluating the rule set millions of times over a long
    # history, where nearly every path is repeated by nearly every commit.
    cache = DECISION_CACHE.setdefault(active_indices, {})
    result: list[str] = []
    for path, _mode, kind, size in paths_at(commit):
        entry = (path, kind, size)
        verdict = cache.get(entry)
        if verdict is None:
            verdict = path_is_excluded(config, path, kind, size, active)
            cache[entry] = verdict
        if verdict:
            result.append(path)
    return result


def filtered_tree(commit: str, removed: list[str], index_file: Path) -> str:
    env = os.environ.copy()
    env["GIT_INDEX_FILE"] = str(index_file)
    run_git(["read-tree", f"{commit}^{{tree}}"], env=env)
    if removed:
        run_git(["update-index", "--force-remove", "-z", "--stdin"], input_bytes=b"\0".join(p.encode("utf-8", "surrogateescape") for p in removed) + b"\0", env=env)
    return run_git(["write-tree"], env=env).decode().strip()


def create_commit(tree: str, parents: list[str], source_commit: str) -> str:
    metadata, message = commit_metadata(source_commit)
    env = os.environ.copy()
    env.update(metadata)
    return run_git(["commit-tree", tree, *[item for parent in parents for item in ("-p", parent)]], input_bytes=message, env=env).decode().strip()


def load_state(path: Path) -> dict[str, Any]:
    if not path.is_file():
        return {"version": 1, "commits": {}, "refs": {}}
    return json.loads(path.read_text(encoding="utf-8"))


def write_state(path: Path, state: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(state, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    temporary.replace(path)


ANCHOR_PREFIX = "refs/code-mirror/"


def anchor_refs(refs: dict[str, str], state: dict[str, Any]) -> None:
    """Point a private ref namespace at the rewritten commits.

    The rewritten commits are otherwise recorded only in the state file, so they
    stay unreachable in the source object database. A pruning `git gc` in a
    reused CI workspace then deletes them, and the next run has to write every
    rewritten tree again. Keeping them reachable also lets `git gc` pack them
    instead of leaving one loose object per rewritten tree.
    """
    desired = {ANCHOR_PREFIX + ref.removeprefix("refs/"): state["commits"][source] for ref, source in refs.items()}
    existing = run_git(["for-each-ref", "--format=%(refname)", ANCHOR_PREFIX]).decode().splitlines()
    commands = [f"update {name} {object_id}\n" for name, object_id in desired.items()]
    # A stale anchor would keep the rewritten history of a deleted source branch
    # reachable, which is exactly the garbage the prune is meant to reclaim.
    commands += [f"delete {name}\n" for name in existing if name not in desired]
    run_git(["update-ref", "--stdin"], input_bytes="".join(commands).encode())


def http_askpass(token_env: str, username: str) -> tuple[Path, dict[str, str]]:
    """Answer Git's credential prompts without the token reaching Git itself.

    Git runs GIT_ASKPASS as a program, so the helper needs a launcher the
    platform can execute on its own. A shebang naming an interpreter by command
    name is not one: Git on Windows reads the shebang, keeps only the
    interpreter's file name and looks that name up on PATH, which finds nothing
    when the run was started through the py launcher or through any interpreter
    that is not itself on PATH. Both launchers therefore spell out the running
    interpreter by full path.
    """
    directory = Path(tempfile.mkdtemp(prefix="code-mirror-askpass-"))
    script = directory / "askpass.py"
    script.write_text(
        "import os, sys\n"
        "prompt = sys.argv[1] if len(sys.argv) > 1 else ''\n"
        "print(os.environ[os.environ['CODE_MIRROR_TOKEN_ENV']] if 'Password' in prompt else os.environ['CODE_MIRROR_HTTP_USERNAME'])\n",
        encoding="utf-8")
    script.chmod(0o600)
    if sys.platform == "win32":
        launcher = directory / "askpass.bat"
        launcher.write_text(f'@echo off\r\n"{sys.executable}" "{script}" %*\r\n', encoding="utf-8")
    else:
        launcher = directory / "askpass.sh"
        launcher.write_text(f'#!/bin/sh\nexec "{sys.executable}" "{script}" "$@"\n', encoding="utf-8")
    launcher.chmod(0o700)
    env = os.environ.copy()
    env.update({"GIT_ASKPASS": str(launcher), "GIT_TERMINAL_PROMPT": "0", "CODE_MIRROR_TOKEN_ENV": token_env, "CODE_MIRROR_HTTP_USERNAME": username})
    return directory, env


def destination_git_env(destination: dict[str, Any]) -> tuple[Path | None, dict[str, str]]:
    url = str(destination["url"])
    token_name = destination.get("token_env")
    if token_name and urlparse(url).scheme == "https":
        return http_askpass(str(token_name), str(destination.get("http_username", "git")))
    return None, os.environ.copy()


def fetch_published_mirror(destination: dict[str, Any]) -> None:
    """Bring the published rewrite back into the anchor namespace.

    Reusing a recorded rewrite needs its commit to still be in the object
    database, and a workspace created on another agent has none of them even
    when the state file was restored: the mapping alone cannot recreate the
    objects. The destination holds exactly those commits, so one fetch is
    enough to make the mapping usable again.
    """
    askpass_dir, env = destination_git_env(destination)
    try:
        run_git(["fetch", "--force", str(destination["url"]),
                 f"+refs/heads/*:{ANCHOR_PREFIX}heads/*", f"+refs/tags/*:{ANCHOR_PREFIX}tags/*"], env=env)
    finally:
        if askpass_dir:
            shutil.rmtree(askpass_dir, ignore_errors=True)


def push_refs(destination: dict[str, Any], refs: dict[str, str], state: dict[str, Any]) -> None:
    """Publish the rewrite, replacing whatever the destination holds.

    The destination is a mirror, so the source repository is the only
    authority for what it should contain. A destination branch that diverged
    from the rewrite is therefore overwritten rather than reported: dropping
    commits published there loses nothing that the source does not still have.
    """
    askpass_dir, env = destination_git_env(destination)
    try:
        refspecs = [f"{state['commits'][source]}:{ref}" for ref, source in refs.items()]
        run_git(["push", "--force", str(destination["url"]), *refspecs], env=env)
    finally:
        if askpass_dir:
            shutil.rmtree(askpass_dir, ignore_errors=True)


def toml_array(values: list[str]) -> str:
    return ", ".join(json.dumps(value, ensure_ascii=False) for value in values)


def generate_config(config_path: Path) -> int:
    config = tomllib.loads(config_path.read_text(encoding="utf-8"))
    # Reading the checked-out commit rather than the working tree makes the
    # policy a function of that commit alone. The generated policy feeds the
    # filtering fingerprint, and a workspace shared across branches keeps module
    # directories another branch left behind, so a working-tree scan would let
    # that leftover change the fingerprint and stop the next run outright.
    listing = run_git(["ls-tree", "-r", "--name-only", "-z", "HEAD", "Assets/Modules/"]).decode().split("\0")
    manifests = sorted(path for path in listing if path.endswith("/package.json") and path.count("/") == 3)
    included: list[str] = []
    for manifest_path in manifests:
        try:
            manifest = json.loads(run_git(["cat-file", "blob", f"HEAD:{manifest_path}"]).decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError) as error:
            raise MirrorError(f"Cannot read package manifest {manifest_path}: {error}") from error
        if "unity" in manifest and str(manifest.get("license", "")).strip().upper() == "MIT":
            included.append(manifest_path.split("/")[2])
    output_path = config_path.parent / config.get("generated_config", "code-mirror.gen.toml")
    content = (
        "# Generated by code_mirror.py --generate-config. Do not edit manually.\n"
        "# Manual module_policies in code-mirror.toml take precedence.\n\n"
        "[module_policies]\n"
        f"always_include = [{toml_array(included)}]\n"
        "always_exclude = []\n"
    )
    output_path.write_text(content, encoding="utf-8")
    print(f"Generated {output_path} with {len(included)} MIT Unity package module(s).")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", required=True, type=Path)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--push", action="store_true")
    parser.add_argument("--rebuild", action="store_true", help="Allow a changed filtering configuration to rewrite mirror history.")
    parser.add_argument("--generate-config", action="store_true", help="Generate the lower-priority MIT Unity package module policy file and exit.")
    args = parser.parse_args()
    if args.dry_run and args.push:
        parser.error("--dry-run and --push cannot be used together.")
    config_path = args.config.resolve()
    if args.generate_config:
        return generate_config(config_path)
    config = load_config(config_path)
    load_dotenv((config_path.parent / config.get("dotenv_file", ".env")).resolve())
    refs = source_refs(config.get("source", {}))
    # Only branch-scoped rules can change a commit's filtering result without the
    # commit or the configuration itself changing, so everything below reuses the
    # recorded rewrite unless such a rule is configured.
    branch_scoped = any(rule.get("scope", {}).get("mode") == "branches" for rule in config.get("rules", []))
    memberships = branch_membership(refs) if branch_scoped else {}
    state_path = (config_path.parent / config.get("state_file", ".code-mirror-state.json")).resolve()
    state = load_state(state_path)
    fingerprint = config_fingerprint(config)
    if state.get("config_fingerprint") not in (None, fingerprint) and not args.rebuild:
        raise MirrorError("Filtering configuration changed. Review the result and rerun with --rebuild to rewrite the mirror.")
    graph = commit_graph(refs.values())
    # A rewritten commit is a pure function of its source commit and the
    # filtering configuration, so an unchanged fingerprint makes every recorded
    # rewrite still correct. Recomputing one that was pruned from the object
    # database yields the same hash again, which keeps its children valid too.
    reusable: set[str] = set()
    if not args.rebuild and not branch_scoped and state.get("config_fingerprint") == fingerprint:
        reusable = existing_commits(state["commits"].values())
        missing = {state["commits"][source] for source, _parents in graph if source in state["commits"]} - reusable
        # A dry run promises to leave refs alone, so it accepts the slower path.
        if missing and not args.dry_run:
            print(f"{len(missing)} recorded rewrites are missing locally; fetching the published mirror.", flush=True)
            try:
                fetch_published_mirror(config["destination"])
            except MirrorError as error:
                # An empty or unreachable destination only costs the rewrite the
                # fetch was meant to avoid, so it must not stop the run.
                print(f"Could not fetch the published mirror, rewriting instead: {error}", flush=True)
            else:
                reusable = existing_commits(state["commits"].values())
    removed_count = 0
    new_count = 0
    reused_count = 0
    with tempfile.TemporaryDirectory(prefix="code-mirror-index-") as tempdir:
        for index, (source, source_parents) in enumerate(graph, start=1):
            recorded = state["commits"].get(source)
            if recorded in reusable:
                reused_count += 1
            else:
                removed = excluded_paths(config, source, memberships.get(source, set()))
                removed_count += len(removed)
                index_file = Path(tempdir) / source
                tree = filtered_tree(source, removed, index_file)
                parents = [state["commits"][parent] for parent in source_parents]
                mirrored = create_commit(tree, parents, source)
                if recorded != mirrored:
                    new_count += 1
                state["commits"][source] = mirrored
            if index % 100 == 0 or index == len(graph):
                print(f"Processed {index}/{len(graph)} source commits.", flush=True)
    state.update({"version": 1, "config_fingerprint": fingerprint, "refs": refs})
    print(f"Source commits: {len(graph)}; reused rewrites: {reused_count}; excluded file instances: {removed_count}; changed mirror commits: {new_count}")
    if args.dry_run:
        print("Dry run completed. No state was saved and nothing was pushed.")
        return 0
    write_state(state_path, state)
    # Anchor before pushing so a failed push still leaves the rewritten objects
    # reachable for the next run.
    anchor_refs(refs, state)
    if args.push:
        push_refs(config["destination"], refs, state)
        print(f"Pushed {len(refs)} refs to the configured Git destination.")
    else:
        print(f"State saved to {state_path}. Use --push to publish the mirror.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except MirrorError as error:
        print(f"error: {error}", file=sys.stderr)
        raise SystemExit(1)
