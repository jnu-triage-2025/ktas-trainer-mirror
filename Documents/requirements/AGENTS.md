---
title: "Documents/requirements"
doc_type: requirement
status: active
updated: 2026-04-14
---

# Documents/requirements

기능 요구사항과 그 기능의 구체적인 구현, 그리고 구현의 진행 상황들을 문서화한 내용을 담고 있다. 이 폴더의 기대 독자는 비개발직종 일반 사용자 및 이 프로젝트의 비개발직종 연구자이므로, 기술적인 세부 사항보다는 일반인 이해에 집중하여야 한다. 기술적인 세부 사항은 [템플릿 `_template.md`](./_template.md)의 "기술적 세부 사항" 섹션과 같이 별도의 섹션을 만들거나, [API 레퍼런스](../api-references/) 문서에 작성하고 관련한 내용을 링크하고 참조를 다는 방식으로 작성하여라.

각 문서에 대해, 문서가 Markdown 파일이라면(각 폴더의 요구 사항에 따라 아닐 수도 있다.) YAML Front Matter로 다음과 같은 메타데이터를 포함하여 작성하여라.

```yaml
---
title: "(title)"
domain: "(domain)"
progress: "0-reserved", "1-designed", "2-implementing", or "3-implemented"
flags: ["refactor-required"]
---
```

- `title`: 요구사항의 제목을 작성한다.
- `domain`: 요구사항이 속하는 도메인을 작성한다. 예시로는 `content-definitions`, `ui` 등이 있다.
- `progress`: 요구사항의 진행 상황을 나타내는 값으로, "0-reserved", "1-designed", "2-implementing", "3-implemented" 중 하나를 작성한다.
  - `"0-reserved"`: 요구사항이 예약되었지만 아직 구체적인 기획, 설계가 이루어지지 않음
  - `"1-designed"`: 요구사항이 설계되었지만 아직 구현되지 않음
  - `"2-implementing"`: 요구사항이 구현 중임
  - `"3-implemented"`: 요구사항이 구현되었음
- `flags`: 요구사항과 관련된 플래그를 배열 형태로 작성한다. 예시로는 "refactor-required" 등이 있다.
