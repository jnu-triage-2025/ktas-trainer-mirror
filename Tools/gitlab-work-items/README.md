# GitLab Work Items Tool

Dependency-free MCP server for GitLab's GraphQL API. Start it with:

```sh
GITLAB_TOKEN=glpat-... python3 Tools/gitlab-work-items/gitlab_work_items.py
```

The server exposes availability/init, an indexed paginated Work Item listing, load, create, update, and comment tools. `GITLAB_TOKEN` must have `read_api` for queries and `api` for mutations. Creating a Work Item requires its global `work_item_type_id` (for example, the Issue type ID). Set `GITLAB_GRAPHQL_URL` for a self-managed GitLab instance.
