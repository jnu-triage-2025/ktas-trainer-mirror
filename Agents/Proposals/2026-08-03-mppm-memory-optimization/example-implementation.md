# MPPM 메모리 최적화 예제 구현

## 배치 분석 및 적용

Unity Editor 메뉴에서 다음 순서로 실행한다.

1. `Tools > Performance > Analyze MPPM Asset Import Settings`
2. `Library/MppmAssetImportOptimizerReport.txt`의 후보 수와 경로를 검토한다.
3. CPU 접근이나 원본 품질이 필요한 자산에 예외 라벨을 설정한다.
4. `Tools > Performance > Apply MPPM Asset Import Optimization`을 실행한다.
5. 재임포트가 끝난 뒤 대표 씬과 MPPM 세션을 실행한다.

CI 또는 복제 프로젝트에서는 다음 진입점을 `-executeMethod`로 호출할 수 있다.

```text
MultiplayerInfrastructure.Editor.Performance.MppmAssetImportOptimizer.AnalyzeFromCommandLine
MultiplayerInfrastructure.Editor.Performance.MppmAssetImportOptimizer.ApplyFromCommandLine
```

## 예외 라벨

| 라벨 | 용도 |
|---|---|
| `KeepReadable` | Texture CPU 읽기/쓰기 유지 |
| `KeepMeshReadable` | Mesh CPU 읽기/쓰기 유지 |
| `KeepOriginalTextureSize` | 텍스처 최대 크기 정책 제외 |
| `KeepAnimationQuality` | 기존 애니메이션 압축 유지 |

## MPPM 세션 태그

- 태그 없음: 카메라가 보이는 저사양 MPPM 세션
- `FullClient`: MPPM Lite를 적용하지 않는 전체 품질 세션
- `HeadlessLite`: 자동화 전용으로 Camera/Renderer/Light를 숨기는 최소 표현 세션

## 검증 예시

동일한 씬을 Main Editor와 추가 MPPM 세션에서 실행한 후 다음 값을 기록한다.

- Activity Monitor의 프로세스별 메모리
- Unity Profiler Memory의 Total Used/Reserved
- Texture, Mesh, Render Texture 메모리
- 카메라 출력 및 입력 조작 가능 여부
- 콘솔의 Import/ReadWrite 관련 예외

