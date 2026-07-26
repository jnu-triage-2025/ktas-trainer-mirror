# Scenario Graph Layout Progress — Example Implementation

```text
if editorSidecar is missing:
    try:
        ShowProgress("자동 배치를 준비하는 중", 0.02)
        AnalyzeEdges()
        ShowProgress("노드 레이어를 계산하는 중", 0.34)
        for each crossing-reduction pass:
            ReduceCrossings()
            UpdateProgress(pass / passCount)
        AssignCoordinates()
        ShowProgress("완료", 1.0)
    finally:
        ClearProgress()
```

팝업 수명주기는 자동 배치 호출 내부에서 관리해 호출 이후 단계에서 예외가 발생하더라도 자동 배치가 끝난 즉시 닫히도록 한다.
