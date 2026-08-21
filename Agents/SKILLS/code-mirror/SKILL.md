---
name: code-mirror
description: 라이선스와 파일 규칙으로 Git 이력을 필터링해 일반 Git 원격 저장소에 지속 미러링할 때 사용합니다.
---

# Code Mirror

`Tools/code-mirror/`는 GitLab `origin`의 원격 추적 브랜치 또는 현재 저장소의 로컬 브랜치와 태그 이력을 필터링해, URL이나 로컬 bare 저장소 경로로 접근 가능한 일반 Git 원격 저장소에 복제합니다. 특정 호스팅 서비스에 종속되지 않습니다.

## 작업 절차

1. [Tools/code-mirror/README.md](../../../Tools/code-mirror/README.md)와 `code-mirror.toml`을 읽고, 포함·제외 규칙이 요청한 라이선스 범위를 충족하는지 확인합니다.
2. Unity 패키지의 MIT 자동 분류가 필요하거나 패키지 매니페스트가 바뀌었으면 `--generate-config`를 실행합니다. 생성된 `code-mirror.gen.toml`은 로컬 산출물이며, 수동 설정 파일보다 낮은 우선순위를 가집니다.
3. 실제 동기화 전에는 `--dry-run`을 실행해 변환 범위와 제외 결과를 확인합니다.
4. 대상 원격 저장소를 변경하지 않는 요청에서는 `--push`를 실행하지 않습니다. 실제 push 또는 `--rebuild --push`는 사용자가 명시적으로 요청한 경우에만 수행합니다.

## 구성 원칙

- `code-mirror.toml`의 수동 모듈 정책은 생성 설정과 일반 허용·제외 규칙보다 우선합니다.
- `always_include` 모듈은 확장자·용량 제외 규칙을 무시합니다. `always_exclude` 모듈은 `.meta` 파일만 유지합니다.
- 범위 규칙의 커밋 해시는 원본 GitLab 이력 기준입니다. 브랜치 범위는 그 브랜치 팁에서 도달 가능한 커밋에 적용합니다.
- 상태 파일의 원본·미러 커밋 매핑과 대상 참조 기록은 삭제하거나 수동 편집하지 않습니다. 필터링 설정 변경으로 이력이 달라질 때에는 `--rebuild`를 사용하기 전에 결과를 검토합니다.
- HTTPS 인증은 `destination.token_env`와 `destination.http_username`으로 구성합니다. 도구 본문에서 토큰 환경 변수 값을 읽지 않으며, Git askpass 보조 프로세스가 인증 시점에만 소비합니다. SSH 인증 또는 각 Git 서버의 토큰 인증 규약을 우선 사용하며, 토큰 값을 설정 파일이나 로그에 기록하지 않습니다.
- 원격이 없는 원본 저장소는 `[source] mode = "local"`로 실행합니다. 대상 로컬 bare 저장소는 `destination.url`에 절대 경로를 지정합니다.
