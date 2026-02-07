# Scenes

씬 로드 시나리오

1. IntroScene으로 시작
2. 세션 시작 시 IngameScene(빈 씬)으로 전환
3. IngameScene에서 SystemOverlayScene, OverworldScene 로드

## Getting Started

작업할 때 열어야 할 씬

- 월드맵 작업 시 OverworldScene 열어서 작업
- 시스템 작업 시 SystemOverlayScene 열어서 작업

## Scene Index

- IndevScene: 시스템 개발 중 사용
- IngameScene: 현재는 빈 씬 (로드 시나리오처럼 활용의도)
- IntroScene: 시작시 제일 처음 마주함, 세션 시작 관련 설정
- OverworldScene: 월드맵
- SampleScene: (자동 생성됨)
- SystemOverlayScene: 시스템
