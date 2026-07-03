---
title: "트리아지 평가 UI"
doc_type: requirement
status: active
updated: 2026-07-03
---

# 트리아지 평가 UI

| 항목 | 내용 |
| :-: | :-- |
| ID | `miui_triage_assessment` |
| 깊이 | 1 |
| 다른 깊이 1의 UI 대체 표시 가능 여부 | 불가 |
| 진입 시 마우스 잠금 | 해제 |
| 진입조건 | 트리아지 평가 활성화 상태의 환자와 "트리아지 분류" 인터랙션 수행 시 |
| 탈출조건 | 등급 사각형 클릭(선택 완료), 취소 버튼 클릭 |

## 개요

플레이어가 환자의 트리아지를 분류할 때 사용하는 전체화면 오버레이 UI입니다. KTAS 5단계 등급을 각각 해당 색상으로 칠해진 사각형으로 가로 나열해 표시하며, 선택한 등급이 즉시 환자에 적용됩니다.

## 상세

- 화면 중앙에 반투명 어두운 backdrop 위로 타이틀("트리아지 분류"), 안내 문구, 등급 사각형 행(row)이 표시된다.
- 등급 사각형은 KTAS 1~5 순서로 좌→우 나열되며, 각 사각형에는 등급 색상, 짧은 라벨(`KTAS N`), 한국어 명칭이 표기된다.
- 이전에 부여된 등급이 있으면 해당 사각형이 진한 테두리로 강조 표시된다.
- 사각형에 마우스를 올리면 미세한 확대 효과로 반응한다.
- 등급 선택 시 UI가 닫히고 선택 결과가 환자에 서버 권위로 적용된다.
- 취소 버튼으로 선택 없이 닫을 수 있다.
- `UIOverlayStack`을 통해 관리되므로, 다른 오버레이 UI가 위에 올라오면 일시적으로 가려진다.

## 씬 배치

`TriageAssessmentUIController` 컴포넌트와 `UIDocument` 컴포넌트를 동일 GameObject에 배치해야 한다.

## 참조

- [api:TriageAssessmentUIController](../../api-references/TriageTrainer.UI.TriageAssessmentUIController.md)
- [req:환자 트리아지 분류](../patient/triage-classification-requirements.md)
