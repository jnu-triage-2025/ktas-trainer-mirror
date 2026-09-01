# <a id="TriageTrainer_UI"></a> Namespace TriageTrainer.UI

### Classes

 [TriageAssessmentPanelElement](TriageTrainer.UI.TriageAssessmentPanelElement.md)

트리아지 평가 패널: 가로로 늘어진 KTAS 색상 사각형들을 표시한다.

<p>
각 사각형은 (1) 해당 트리아지 등급 색으로 칠해지고 (2) 색 명칭과 이름(예: "KTAS 2", "2단계 긴급")이
표기된다. 사각형을 클릭하면 <xref href="TriageTrainer.UI.TriageAssessmentPanelElement.LevelSelected" data-throw-if-not-resolved="false"></xref> 이벤트로 선택된 등급을 통지한다.
</p>

 [TriageAssessmentUIController](TriageTrainer.UI.TriageAssessmentUIController.md)

트리아지 평가 전체화면 오버레이 UI 컨트롤러.

<p>
플레이어가 환자의 트리아지 인터랙션을 수행하면 <xref href="TriageTrainer.UI.TriageAssessmentUIController.Open(TriageTrainer.Entity.Patient.TriageLevel%2cSystem.Action%7bTriageTrainer.Entity.Patient.TriageLevel%7d)" data-throw-if-not-resolved="false"></xref> 으로 열리며, 가로로 늘어진 KTAS 색상
사각형(<xref href="TriageTrainer.UI.TriageAssessmentPanelElement" data-throw-if-not-resolved="false"></xref>)을 표시한다. 사각형을 클릭하면 선택 콜백으로 등급을
통지하고 패널을 닫는다.
</p>

<p>
<xref href="MultiplayerInfrastructure.UI.ProblemSheetUIController" data-throw-if-not-resolved="false"></xref> 와 동일한 <xref href="MultiplayerInfrastructure.UI.IUIOverlay" data-throw-if-not-resolved="false"></xref> / <xref href="MultiplayerInfrastructure.UI.UIOverlayStack" data-throw-if-not-resolved="false"></xref>
관례를 따른다.
</p>

