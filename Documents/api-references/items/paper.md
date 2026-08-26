# API 레퍼런스: `TriageTrainer.ItemDefinitions.Paper`

## 개요

`Paper`는 `MedicalItem`을 상속하는 일반 종이 아이템입니다. 식별자는 `paper`, 표시 이름은 `종이`, 기본 설명은 빈 문자열입니다.

`IntendedMissing3DModelAttribute`와 `IntendedMissingItemSpriteAttribute`가 적용되어 있으므로 3D 모델과 아이콘 스프라이트가 없는 상태가 의도된 구성입니다.

## 상속 구조

```text
MedicalItem
└─ Paper
   └─ ChecklistPaper
```

## 스택

`paper`는 최대 64개까지 쌓을 수 있습니다.

## 관련 항목

- [checklist-paper.md](./checklist-paper.md)
- [TriageTrainer.ItemDefinitions.MedicalItem.md](../TriageTrainer.ItemDefinitions.MedicalItem.md)
