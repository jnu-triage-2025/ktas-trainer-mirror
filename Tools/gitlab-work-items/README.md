# GitLab Work Items Tool

`Tools/gitlab-work-items/gitlab_work_items.py` — Dependency-free utility for GitLab's GraphQL API. Start it with:

```sh
python Tools/gitlab-work-items/gitlab_work_items.py
```

If you want to use specific Token, you can set like:

```sh
GITLAB_TOKEN="your_token_here" python Tools/gitlab-work-items/gitlab_work_items.py
```

The server exposes availability/init, token verification, an indexed paginated Work Item listing, load, create, update, and comment tools. `GITLAB_TOKEN` must have `read_api` for queries and `api` for mutations. Creating a Work Item requires its global `work_item_type_id` (for example, the Issue type ID). Set `GITLAB_GRAPHQL_URL` for a self-managed GitLab instance.

## Token Verification

`gitlab_is_available` only checks whether the `GITLAB_TOKEN` environment variable is set — it does **not** prove the token is accepted by GitLab. To determine whether the active token is actually usable, call **`gitlab_verify_token`**. It sends a lightweight `currentUser` GraphQL query and returns:

```json
// Success
{"valid": true, "user": {"id": "...", "username": "...", "name": "..."}, "endpoint": "..."}
// Failure
{"valid": false, "reason": "..."}
```

> **Use `gitlab_verify_token` as the authoritative check for token usability.** If it returns `valid: false`, the token is expired, revoked, or lacks the required scopes — do not proceed with other tools until the issue is resolved.
