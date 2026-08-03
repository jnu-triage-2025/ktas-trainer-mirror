# MPPM 메모리 최적화 제안

## 개요

여러 Unity Multiplayer Play Mode(MPPM) Editor 세션을 동시에 실행할 때 세션마다 고해상도 텍스처, CPU 읽기 가능 메시, 애니메이션 원본 데이터가 중복 적재되어 시스템 메모리가 고갈되는 문제를 완화한다. 기본 MPPM 세션은 카메라와 화면을 유지해 사람이 조정할 수 있게 하며, 자동화 전용 세션만 `HeadlessLite` 태그로 표현을 끌 수 있다.

## 해결하려는 문제 상황

- 32GB 환경에서 추가 Unity Editor 프로세스가 각각 약 20GB를 사용해 복수 세션 운용이 어렵다.
- 2048~8192 크기의 텍스처와 HDRI, Reflection Probe가 세션마다 중복 로드된다.
- 런타임 접근이 필요하지 않은 Texture/Mesh의 Read/Write가 CPU 복사본을 유지한다.
- 기존 저메모리 모드는 추가 세션의 카메라까지 숨겨 사람이 세션을 확인하거나 조정할 수 없다.

## 사용자 경험 목표

- 별도 수작업 없이 프로젝트 전체 자산을 분석하고 안전한 항목을 일괄 최적화한다.
- 추가 MPPM 세션은 기본적으로 보이면서 낮은 렌더링 품질로 동작한다.
- 자동화 전용 세션은 명시적인 `HeadlessLite` 태그로 더 강한 절약 모드를 선택한다.
- 예외 자산은 Unity 라벨로 정책에서 제외할 수 있다.

## 제안

1. 일반 텍스처의 기본 최대 크기를 2048, HDRI/EXR/Cubemap/Reflection Probe를 1024로 제한한다.
2. Mipmap이 있는 일반/Normal Texture2D에 Streaming Mipmaps를 활성화한다. UI, Editor, Gizmos 자산은 제외한다.
3. 런타임 CPU 접근이 확인되지 않은 Texture와 Mesh의 Read/Write를 비활성화한다.
4. 모델의 polygon/vertex 최적화와 `Optimal` 애니메이션 압축을 활성화한다.
5. 분석과 적용을 분리한 Editor 배치 도구를 제공한다.
6. MPPM 기본 세션은 화면을 유지하고 `HeadlessLite` 세션에서만 Camera, Renderer, Light 등의 표현을 비활성화한다.

## 자세한 달성 목표

- 분석 대상: Texture 1,085개, Model 480개
- 초기 적용 대상: Texture 672개, Model 415개
- Streaming Mipmap 적용: 601개
- Read/Write Texture: 62개에서 런타임 필수 자산 1개로 축소
- MPPM 상태 전환은 종료 시 원래 Quality/Audio/visual 상태를 복구한다.
- 세션당 6GB는 운영 목표이며 하드 제한이 아니다. Unity Profiler와 OS 메모리로 재측정해 후속 씬 경량화 여부를 결정한다.

## 문서화

- 변경 기록에 배치 도구 사용법, 정책, 예외 라벨 및 검증 절차를 기록한다.
- 제안서와 예제 구현 문서를 함께 유지한다.
- 후속 단계에서 실제 세션 메모리 측정값과 추가 씬/셰이더 정책을 갱신한다.

## 가용성과 테스트

- Read/Write 비활성화는 `GetPixels`, `Mesh.vertices` 같은 CPU 접근을 깨뜨릴 수 있으므로 코드 검색과 라벨 예외가 필요하다.
- 텍스처/Lightmap 상한과 애니메이션 압축은 화질 저하 가능성이 있으므로 대표 씬을 육안 검증해야 한다.
- 적용 전 임시 프로젝트 복제본에서 전체 재임포트를 수행한다.
- MPPM PlayMode 테스트로 기본 화면 모드, Headless 모드, 상태 복구를 검증한다.

## 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 배치 도구가 반복 실행되어도 추가 변경이 없는 멱등 상태가 된다.
- 기본 MPPM 세션에서 카메라 출력과 조작이 유지된다.
- `HeadlessLite`에서만 시각 컴포넌트가 비활성화된다.
- MPPM PlayMode 테스트가 모두 통과한다.
- 동일 씬과 동일 플레이어 수에서 세션별 Resident/Allocated Memory가 기존보다 감소한다.

## 링크, 참고사항

- 구현 예제: [example-implementation.md](./example-implementation.md)
- 운영 문서: [MPPM 메모리 최적화 변경 기록](../../../Documents/changes/2026-08-03-mppm-memory-optimization.md)

