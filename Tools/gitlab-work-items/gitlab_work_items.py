#!/usr/bin/env python3
"""Dependency-free GitLab GraphQL Work Items tool (Tools/gitlab-work-items/)."""
import json, os, sys, urllib.error, urllib.request

ENDPOINT = os.getenv("GITLAB_GRAPHQL_URL", "https://gitlab.com/api/graphql")

def token():
    value = os.getenv("GITLAB_TOKEN", "")
    return value if value.strip() else None

def mask(value):
    return None if not value else value[:4] + "…" + value[-4:] if len(value) > 8 else "********"

def gql(query, variables=None):
    t = token()
    if not t: raise RuntimeError("GITLAB_TOKEN is not set or is blank")
    req = urllib.request.Request(ENDPOINT, json.dumps({"query": query, "variables": variables or {}}).encode(),
        {"Authorization": "Bearer " + t, "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=30) as response:
            result = json.load(response)
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", "replace")[:1000]
        raise RuntimeError(f"GitLab HTTP {exc.code}: {detail}") from exc
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError) as exc:
        raise RuntimeError(f"GitLab request failed: {exc}") from exc
    if result.get("errors"):
        raise RuntimeError("; ".join(e.get("message", "GraphQL error") for e in result["errors"]))
    return result.get("data")

def required(a, key):
    value = a.get(key)
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"{key} must be a non-empty string")
    return value.strip()

def mutation_result(data, operation):
    result = data.get(operation) if isinstance(data, dict) else None
    if not result:
        raise RuntimeError(f"GitLab returned no {operation} result")
    if result.get("errors"):
        raise RuntimeError("; ".join(result["errors"]))
    return result

ITEM = "id iid title state description webUrl workItemType { name } createdAt updatedAt"

def call(name, a):
    if name == "gitlab_is_available":
        return {"available": token() is not None, "env_var": "GITLAB_TOKEN", "endpoint": ENDPOINT}
    if name == "gitlab_init":
        supplied = (a.get("token") or "").strip()
        return {"configured": bool(supplied), "environment_variable": "GITLAB_TOKEN",
                "token_profile": mask(supplied), "note": "Set GITLAB_TOKEN in the host environment; this tool does not persist secrets."}
    if name == "gitlab_work_item_index":
        q = "query($path: ID!, $after: String, $first: Int) { project(fullPath: $path) { workItems(first: $first, after: $after) { nodes { " + ITEM + " } pageInfo { hasNextPage endCursor } } } }"
        first = int(a.get("first", 50))
        if not 1 <= first <= 100: raise ValueError("first must be between 1 and 100")
        data = gql(q, {"path": required(a, "project_path"), "first": first, "after": a.get("after")})
        return data["project"]["workItems"]
    if name == "gitlab_get_work_item":
        data = gql("query($id: WorkItemID!) { workItem(id: $id) { " + ITEM + " } }", {"id": required(a, "id")})
        return data["workItem"]
    if name == "gitlab_create_work_item":
        q = "mutation($input: WorkItemCreateInput!) { workItemCreate(input: $input) { workItem { " + ITEM + " } errors } }"
        inp = {"namespacePath": required(a, "namespace_path"), "title": required(a, "title"),
               "workItemTypeId": required(a, "work_item_type_id")}
        if "description" in a: inp["description"] = a["description"]
        return mutation_result(gql(q, {"input": inp}), "workItemCreate")
    if name == "gitlab_update_work_item":
        fields = {k: a[k] for k in ("title", "description") if k in a}
        if "state" in a: fields["stateEvent"] = a["state"]
        q = "mutation($input: WorkItemUpdateInput!) { workItemUpdate(input: $input) { workItem { " + ITEM + " } errors } }"
        if not fields: raise ValueError("at least one of title, description, or state is required")
        return mutation_result(gql(q, {"input": dict({"id": required(a, "id")}, **fields)}), "workItemUpdate")
    if name == "gitlab_comment_work_item":
        q = "mutation($input: CreateNoteInput!) { createNote(input: $input) { note { id body createdAt } errors } }"
        return mutation_result(gql(q, {"input": {"noteableId": required(a, "id"), "body": required(a, "body"), "internal": bool(a.get("internal", False))}}), "createNote")
    raise ValueError("Unknown tool: " + name)

TOOLS = [{"name":"gitlab_is_available","description":"Report whether non-blank GITLAB_TOKEN is available.","inputSchema":{"type":"object","properties":{}}},
{"name":"gitlab_init","description":"Validate and echo a masked GitLab token profile; does not persist the secret.","inputSchema":{"type":"object","properties":{"token":{"type":"string"}},"required":["token"]}},
{"name":"gitlab_work_item_index","description":"List project Work Items with pagination; use this as the issue index.","inputSchema":{"type":"object","properties":{"project_path":{"type":"string"},"first":{"type":"integer"},"after":{"type":"string"}},"required":["project_path"]}},
{"name":"gitlab_get_work_item","description":"Load one Work Item by GitLab global ID.","inputSchema":{"type":"object","properties":{"id":{"type":"string"}},"required":["id"]}},
{"name":"gitlab_create_work_item","description":"Create a GitLab Work Item. work_item_type_id is the global ID of the desired type (for example Issue).","inputSchema":{"type":"object","properties":{"namespace_path":{"type":"string"},"title":{"type":"string"},"description":{"type":"string"},"work_item_type_id":{"type":"string"}},"required":["namespace_path","title","work_item_type_id"]}},
{"name":"gitlab_update_work_item","description":"Update title, description, or state of a Work Item.","inputSchema":{"type":"object","properties":{"id":{"type":"string"},"title":{"type":"string"},"description":{"type":"string"},"state":{"type":"string","enum":["OPEN","CLOSE"]}},"required":["id"]}},
{"name":"gitlab_comment_work_item","description":"Add a comment/note to a Work Item.","inputSchema":{"type":"object","properties":{"id":{"type":"string"},"body":{"type":"string"},"internal":{"type":"boolean"}},"required":["id","body"]}}]

def reply(i, result=None, error=None, content=False):
    payload = {"content":[{"type":"text","text":json.dumps(result if error is None else {"error":error}, ensure_ascii=False)}]} if content else (result or {})
    out = {"jsonrpc":"2.0", "id":i, "result": payload}
    if error is not None:
        out["result"] = {"content":[{"type":"text","text":json.dumps({"error":error}, ensure_ascii=False)}], "isError":True}
    print(json.dumps(out), flush=True)

for line in sys.stdin:
    try:
        req=json.loads(line); method=req.get("method"); i=req.get("id")
        if method == "initialize": reply(i, {"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"serverInfo":{"name":"gitlab-work-items","version":"1.0.0"}})
        elif method == "tools/list": reply(i, {"tools":TOOLS})
        elif method == "tools/call": reply(i, call(req["params"]["name"], req["params"].get("arguments", {})), content=True)
        elif i is not None: reply(i, {})
    except Exception as e:
        if 'i' in locals(): reply(i, error=str(e))
