# 2026-08-03 MPPM 메모리 최적화

## 변경 목적

복수 MPPM Editor 세션의 자산 및 렌더링 메모리를 줄이되, 사람이 조정해야 하는 세션의 카메라 출력은 유지한다.

## 적용 정책

| 대상 | 정책 |
|---|---|
| 일반 Texture | 최대 2048 |
| HDRI, EXR, Cubemap, Reflection Probe | 최대 1024 |
| Mipmap Texture2D | Streaming Mipmaps 활성화 |
| UI, Editor, Gizmos Texture | Streaming 대상 제외 |
| Texture Read/Write | CPU 접근 필수 자산 외 비활성화 |
| Mesh Read/Write | 명시적 예외 외 비활성화 |
| Mesh Import | polygon/vertex 최적화 활성화 |
| Animation Import | `Optimal` 압축 |

프로젝트의 Texture Streaming 전역 설정은 활성화되어 있으며 기본 메모리 예산은 256MB다. MPPM Lite 런타임은 더 낮은 프로파일을 적용한다.

## 배치 작업 사용법

1. Unity에서 `Tools > Performance > Analyze MPPM Asset Import Settings`를 실행한다.
2. `Library/MppmAssetImportOptimizerReport.txt`를 검토한다.
3. 필요한 자산에 예외 라벨을 붙인다.
4. `Tools > Performance > Apply MPPM Asset Import Optimization`을 실행한다.

배치 모드에서는 다음 메서드를 사용한다.

- `MultiplayerInfrastructure.Editor.Performance.MppmAssetImportOptimizer.AnalyzeFromCommandLine`
- `MultiplayerInfrastructure.Editor.Performance.MppmAssetImportOptimizer.ApplyFromCommandLine`

## 예외 처리

- `KeepReadable`: Texture Read/Write 유지
- `KeepMeshReadable`: Mesh Read/Write 유지
- `KeepOriginalTextureSize`: 원래 최대 텍스처 크기 유지
- `KeepAnimationQuality`: 원래 애니메이션 압축 유지

현재 `ColorTemperatureSample.png`는 런타임 `GetPixel` 사용이 확인되어 Read/Write를 유지한다.

## MPPM 표시 모드

- 일반 추가 세션: 저사양 설정을 적용하지만 카메라와 Renderer를 유지한다.
- `HeadlessLite`: 자동화 전용 세션이며 시각 컴포넌트를 비활성화한다.
- `FullClient`: 저사양 설정을 적용하지 않는다.

## 적용 및 검증 결과

- Texture 1,085개 분석, 672개 변경
- Model 480개 분석, 415개 변경
- Streaming Mipmap Texture 601개
- Read/Write Texture 1개 유지
- MPPM PlayMode 테스트 8개 통과
- `git diff --check` 통과

## 후속 렌더링 경량화

MPPM 저사양 설정을 Main Camera뿐 아니라 overlay 및 보조 Camera에도 적용한다. 모든 카메라에서 HDR, MSAA, Dynamic Resolution, URP Post Processing 상태를 현재 그래픽 프로파일에 맞춰 통일한다. MPPM의 VeryLow 프로파일에서는 카메라 자체는 유지하면서 HDR/MSAA 중간 RenderTexture와 후처리 버퍼 생성을 차단한다.

세션당 6GB는 현재 목표값이다. 자산 재임포트 후 동일 씬에서 실제 프로세스 메모리를 다시 측정하고, 초과 시 MPPM 전용 씬 오브젝트 및 렌더링 리소스 경량화를 수행한다.
